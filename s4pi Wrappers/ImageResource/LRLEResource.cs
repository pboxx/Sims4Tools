/***************************************************************************
 *  Copyright (C) 2014, 2016 by the Sims 4 Tools development team          *
 *                                                                         *
 *  Contributors:                                                          *
 *  Keyi Zhang, kz005@bucknell.edu   
 *  
 *  LRLE specific code by CmarNYC and tau532                               *
 *                                                                         *
 *  This file is part of the Sims 4 Package Interface (s4pi)               *
 *                                                                         *
 *  s4pi is free software: you can redistribute it and/or modify           *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  s4pi is distributed in the hope that it will be useful,                *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with s4pi.  If not, see <http://www.gnu.org/licenses/>.          *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using s4pi.Interfaces;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace s4pi.ImageResource
{
    public class LRLEResource : AResource
    {        
        const int recommendedApiVersion = 1;
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
        public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

        uint magic;
        uint version;
        ushort width;
        ushort height;
        long[] mipPositions;
        byte[][] pixelArray;
        byte[][] mips;

        public ushort Width { get { return this.width; } }
        public ushort Height { get { return this.height; } }
        public byte[] RawData { 
            get {
                MemoryStream m = new MemoryStream();
                this.UnParse().CopyTo(m);
                return m.ToArray();
            } 
        }

        public LRLEResource(int APIversion, Stream s) : base(APIversion, s) { Parse(s); }

        private void Parse(Stream s)
        {
            if (s == null || s.Length == 0)
            {
                this.image = new Bitmap(1, 1); // empty image
                return;
            }
            s.Position = 0;
            BinaryReader br = new BinaryReader(s);

            magic = br.ReadUInt32();
            if (magic != 0x454C524C)
            {
                throw new ApplicationException("Wrong file type!");
            }
            version = br.ReadUInt32();
            if (!(version == 0x32303056 || version == 0x0))
            {
                throw new ApplicationException("Unsupported LRLE version!");
            }

            width = br.ReadUInt16();
            height = br.ReadUInt16();
            uint numMipMaps = br.ReadUInt32();
            int[] mipMapOffsets = new int[numMipMaps];
            for (int i = 0; i < numMipMaps; i++)
            {
                mipMapOffsets[i] = br.ReadInt32();
            }
            if (version == 0x32303056)
            {
                uint numPixels = br.ReadUInt32();
                pixelArray = new byte[numPixels][];
                for (int i = 0; i < numPixels; i++)
                {
                    pixelArray[i] = br.ReadBytes(4);
                }
            }
            mips = new byte[numMipMaps][];
            mipPositions = new long[numMipMaps];
            for (int i = 0; i < numMipMaps; i++)
            {
                mipPositions[i] = s.Position;
                if (i < numMipMaps - 1)
                {
                    mips[i] = br.ReadBytes(mipMapOffsets[i + 1] - mipMapOffsets[i]);
                }
                else
                {
                    mips[i] = br.ReadBytes((int)(s.Length - s.Position));
                }
            }

            if (version == 0x0)
            {
                this.image = ReadMipV1(mips[0], 0);
            }
            else if (version == 0x32303056)
            {
                this.image = ReadMipV2(mips[0], 0);
            }
            else
            {
                this.image = null;
            }
        }

        protected override Stream UnParse()
        {
            Stream s = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(s);
            bw.Write(magic);
            bw.Write(version);
            bw.Write(width);
            bw.Write(height);
            int numMips = mips.GetLength(0);
            bw.Write(numMips);
            long offsetsLocation = s.Position;
            uint[] offsets = new uint[numMips];
            for (int i = 0; i < numMips; i++) { bw.Write(0U); }
            if (version == 0x32303056)
            {
                int numPixels = Math.Min(pixelArray.GetLength(0), 0x10000);
                bw.Write(numPixels);
                for (int i = 0; i < numPixels; i++)
                {
                    bw.Write(pixelArray[i], 0, 4);
                }
            }
            long offsetsStart = s.Position;
            for (int i = 0; i < numMips; i++)
            {
                offsets[i] = (uint)(s.Position - offsetsStart);
                bw.Write(mips[i]);
            }
            s.Position = offsetsLocation;
            for (int i = 0; i < numMips; i++) { bw.Write(offsets[i]); }
            s.Position = 0;

            return s;
        }

        public Stream ToImageStream()
        {
            if (this.image == null) return null;
            MemoryStream ms = new MemoryStream();
            image.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            return ms;
        }

        private Bitmap image;
        public Bitmap Image {
            get { if (image == null) { return new Bitmap(1, 1); } else { return this.image; }; } 
           // set { if (value != null) { this.image = value; } } 
        }

        public void CreateFromImage(Bitmap image)
        {
            magic = 0x454C524C;
            version = 0x32303056;
            int imageWidth = image.Width;
            int imageHeight = image.Height;
            width = (ushort)imageWidth;
            height = (ushort)imageHeight;

            List<Command>[] commandLists = new List<Command>[9];
            EncodingState currentState = EncodingState.StartNew;
            uint runLength = 0;
            List<byte[]> colorArray = new List<byte[]>();
            byte[] lastPixel = new byte[4];
            ColorTable colors = new ColorTable();
            List<byte[]> runColors = new List<byte[]>();
            for (int m = 0; m < 9; m++)
            {
                byte[] mipPixels = GetMipBytes(image);
                commandLists[m] = new List<Command>();
                Array.Copy(mipPixels, 0, lastPixel, 0, 4);
                currentState = EncodingState.StartNew;
                int x = 0, y = 0;
                int w1 = image.Width * 4;
                for (int i = 0; i < mipPixels.Length; i += 64)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        for (int k = 0; k < 16; k += 4)
                        {
                            byte[] tmp = new byte[4];
                            Array.Copy(mipPixels, (y * w1) + x + k, tmp, 0, 4);
                            switch (currentState)
                            {
                                case EncodingState.StartNew:
                                    currentState = EncodingState.Unknown;
                                    runLength = 1;
                                    colorArray = new List<byte[]>();
                                    colorArray.Add(tmp);
                                    break;
                                case EncodingState.Unknown:
                                    if (tmp.SequenceEqual(lastPixel))
                                    {
                                        currentState = EncodingState.RepeatRun;
                                    }
                                    else
                                    {
                                        currentState = EncodingState.ColorRun;
                                    }
                                    colorArray.Add(tmp);
                                    Array.Copy(tmp, 0, lastPixel, 0, 4);
                                    runLength++;
                                    break;
                                case EncodingState.ColorRun:
                                    if (!tmp.SequenceEqual(lastPixel))
                                    {
                                        runLength++;
                                    }
                                    else
                                    {
                                        colorArray.RemoveAt(colorArray.Count - 1);
                                        runLength--;
                                        commandLists[m].Add(new Command(currentState, runLength, colorArray));
                                        foreach (var item in colorArray)
                                        {
                                            colors.AddColor(item);
                                        }
                                        runLength = 2;
                                        colorArray = new List<byte[]>();
                                        currentState = EncodingState.RepeatRun;

                                    }
                                    Array.Copy(tmp, 0, lastPixel, 0, 4);
                                    colorArray.Add(tmp);

                                    break;
                                case EncodingState.RepeatRun:
                                    if (tmp.SequenceEqual(lastPixel))
                                    {
                                        runLength++;
                                        Array.Copy(tmp, 0, lastPixel, 0, 4);
                                    }
                                    else
                                    {
                                        commandLists[m].Add(new Command(currentState, runLength, colorArray));
                                        foreach (var item in colorArray)
                                        {
                                            runColors.Add(item);
                                        }
                                        Array.Copy(tmp, 0, lastPixel, 0, 4);
                                        runLength = 1;
                                        colorArray = new List<byte[]>();
                                        colorArray.Add(tmp);
                                        currentState = EncodingState.Unknown;
                                    }
                                    break;
                                default:
                                    break;

                            }
                        }
                        y++;
                    }
                    x += 16;
                    if (x >= w1)
                    {
                        x = 0;
                    }
                    else
                    {
                        y -= 4;
                    }
                }
                commandLists[m].Add(new Command(currentState, runLength, colorArray));
                foreach (var item in colorArray)
                {
                    colors.AddColor(item);
                }
                mipPixels = null;

                imageWidth /= 2;
                imageHeight /= 2;

                if (m < 1)
                {
                    Bitmap mip = new Bitmap(imageWidth, imageHeight);
                    using (Graphics g = Graphics.FromImage(mip))
                    {
                        g.SmoothingMode = SmoothingMode.Default;
                        g.InterpolationMode = InterpolationMode.High;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.CompositingQuality = CompositingQuality.HighQuality;
                        g.DrawImage(image, new Rectangle(0, 0, imageWidth, imageHeight));
                    }
                    image = mip;
                }
                else if (m < 3)
                {
                    image = new Bitmap(image, imageWidth, imageHeight);
                }
                else
                {
                    Bitmap mip = new Bitmap(imageWidth, imageHeight);
                    using (Graphics g = Graphics.FromImage(mip))
                    {
                        g.SmoothingMode = SmoothingMode.Default;
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                        g.CompositingQuality = CompositingQuality.HighSpeed;
                        g.DrawImage(image, new Rectangle(0, 0, imageWidth, imageHeight));
                    }
                    image = mip;
                }
            }
            colors.SortColors();
            foreach (var item in runColors)
            {
                if (!colors.HasColor(item))
                {
                    colors.AddColor(item);
                }
            }

            List<byte[]> tmpPixels = new List<byte[]>();
            foreach (var item in colors.colors.OrderBy(x => x.Value.Index).Where(x => x.Value.Index < 0x10000).Select(x => x.Key))
            {
                tmpPixels.Add(item.color);
            }
            pixelArray = tmpPixels.ToArray();

            mips = new byte[9][];
            for (int i = 0; i < 9; i++)
            {
                List<byte> tmpCommands = new List<byte>();
                foreach (var item in commandLists[i])
                {
                    tmpCommands.AddRange(item.ToOps(colors).ToArray());
                }
                mips[i] = tmpCommands.ToArray();
            }
        }

        private byte[] GetMipBytes(Bitmap image)
        {
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            System.Drawing.Imaging.BitmapData bmpData = image.LockBits(rect, ImageLockMode.ReadWrite, image.PixelFormat);

            IntPtr ptr;
            if (bmpData.Stride > 0)
            {
                ptr = bmpData.Scan0;
            }
            else
            {
                ptr = bmpData.Scan0 + bmpData.Stride * (image.Height - 1);
            }

            int bytes = Math.Abs(bmpData.Stride) * image.Height;
            byte[] blockPixels = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(ptr, blockPixels, 0, bytes);
            image.UnlockBits(bmpData);
            return blockPixels;
        }

        /// <summary>
        /// Reads version 1
        /// </summary>
        /// <param name="mip">Byte array of mip data</param>
        /// <param name="mipLevel">zero-based mip level</param>
        private Bitmap ReadMipV1(byte[] mip, int mipLevel)
        {

            int tot = 0;
            int pointer = 0;
            int pixelPointer = 0;
            int w = width;
            int h = height;
            for (int i = 0; i < mipLevel; i++)
            {
                w = w / 2;
                h = h / 2;
            }
            byte[] pixels = new byte[w * h * 4];
            int currentInstruction = 0;
            int previousInstruction = 0;
            int count = 0;

            while (pointer < mip.Length)
            {
                switch (mip[pointer] & 3) //Run of embedded colors
                {

                    case 1:
                        previousInstruction = currentInstruction;
                        currentInstruction = pointer;
                        count = mip[pointer] >> 2;
                        pointer++;


                        tot += count;

                        Array.Copy(mip, pointer, pixels, pixelPointer, 4 * count);
                        pixelPointer += 4 * count;
                        pointer += 4 * count;
                        break;

                    case 2://run of single embedded color
                        previousInstruction = currentInstruction;
                        currentInstruction = pointer;
                        count = GetPixelRunLength(mip, ref pointer);
                        tot += count;
                        pointer++;
                        for (int i = 0; i < count; i++)
                        {
                            Array.Copy(mip, pointer, pixels, pixelPointer, 4);
                            pixelPointer += 4;
                        }
                        pointer += 4;
                        break;
                    case 0://run of zero's
                        previousInstruction = currentInstruction;
                        currentInstruction = pointer;
                        count = GetPixelRunLength(mip, ref pointer);
                        tot += count;
                        pointer++;
                        for (int i = 0; i < count * 4; i++)
                        {
                            pixels[pixelPointer] = 0;
                            pixelPointer++;
                        }
                        break;
                    case 3:

                        //Another type of RLE to parse, great.
                        previousInstruction = currentInstruction;
                        currentInstruction = pointer;
                        count = mip[pointer] >> 2;
                        pointer++;
                        var newPixels = ReadEmbeddedRLE(mip, ref count, ref pointer);
                        for (int i = 0; i < count; i++)
                        {
                            pixels[pixelPointer] = newPixels[i];
                            pixels[pixelPointer + 1] = newPixels[i + count];
                            pixels[pixelPointer + 2] = newPixels[i + 2 * count];
                            pixels[pixelPointer + 3] = newPixels[i + 3 * count];
                            pixelPointer += 4;
                        }
                        tot += count;

                        break;
                    default:

                        throw new ApplicationException("Unknown instruction: " + mip[pointer].ToString("X") +
                            " Offset: " + (pointer + 56).ToString() +
                            " Last good instruction: " + (currentInstruction + 56).ToString());
                }
            }

            byte[] blockPixels = new byte[pixels.Length];
            int x = 0, y = 0;
            int w1 = w * 4;
            for (int i = 0; i < pixels.Length; i += 64)
            {
                for (int j = 0; j < 4; j++)
                {
                    Array.Copy(pixels, i + (j * 16), blockPixels, (y * w1) + x, 16);
                    y++;
                }
                x += 16;
                if (x >= w1)
                {
                    x = 0;
                }
                else
                {
                    y -= 4;
                }
            }

            Bitmap image = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            System.Drawing.Imaging.BitmapData bmpData = image.LockBits(rect, ImageLockMode.ReadWrite, image.PixelFormat);

            IntPtr ptr;
            if (bmpData.Stride > 0)
            {
                ptr = bmpData.Scan0;
            }
            else
            {
                ptr = bmpData.Scan0 + bmpData.Stride * (image.Height - 1);
            }

            int bytes = Math.Abs(bmpData.Stride) * image.Height;

            System.Runtime.InteropServices.Marshal.Copy(blockPixels, 0, ptr, bytes);
            image.UnlockBits(bmpData);
            return image;
        }

        private byte[] ReadEmbeddedRLE(byte[] data, ref int pixelCount, ref int pointer)
        {
            byte[] result = new byte[pixelCount * 4];
            int resultPtr = 0;
            while (resultPtr < pixelCount * 4)
            {
                if ((data[pointer] & 1) == 1)//run of bytes
                {
                    var count = (data[pointer] & 0x7F) >> 1;

                    if ((data[pointer] & 0x80) == 0x80)
                    {
                        pointer++;
                        count += ((data[pointer]) << 6);
                    }
                    pointer++;
                    Array.Copy(data, pointer, result, resultPtr, count);
                    resultPtr += count;
                    pointer += count;
                }
                else if ((data[pointer] & 2) == 2) //repeating run
                {
                    var count = (data[pointer] & 0x7F) >> 2;
                    if ((data[pointer] & 0x80) == 0x80)
                    {
                        pointer++;
                        count += (data[pointer]) << 5;
                    }

                    pointer++;
                    for (int i = 0; i < count; i++)
                    {
                        result[resultPtr] = data[pointer];
                        resultPtr++;
                    }
                    pointer++;
                }

                else //run of zero's
                {
                    var count = (data[pointer] & 0x7F) >> 2;
                    if ((data[pointer] & 0x80) == 0x80)
                    {
                        pointer++;
                        count += (data[pointer]) << 5;
                    }
                    for (int i = 0; i < count; i++)
                    {
                        result[resultPtr] = 0;
                        resultPtr++;
                    }
                    pointer++;
                }
            }
            return result;
        }

        /// <summary>
        /// Reads version 2
        /// </summary>
        /// <param name="mip">Byte array of mip data</param>
        /// <param name="mipLevel">Zero-based mip level</param>
        /// <returns></returns>
        private Bitmap ReadMipV2(byte[] mip, int mipLevel)
        {
            int tot = 0;
            int pointer = 0;
            int pixelPointer = 0;
            int w = width;
            int h = height;
            for (int i = 0; i < mipLevel; i++)
            {
                w = w / 2;
                h = h / 2;
            }
            byte[] pixels = new byte[w * h * 4];
            int lastInstruction = 0;
            try
            {
                while (pointer < mip.Length)
                {
                    if ((mip[pointer] & 0x01) > 0 && (mip[pointer] & 0x02) > 0)         // bits 1 & 2 set - copy following pixel values
                    {
                        lastInstruction = pointer;
                        int count = GetPixelRunLength(mip, ref pointer);
                        tot += count;
                        pointer++;
                        for (int i = 0; i < count; i++)
                        {
                            Array.Copy(mip, pointer, pixels, pixelPointer, 4);
                            pixelPointer += 4;
                            pointer += 4;
                        }
                    }
                    else if ((mip[pointer] & 0x01) == 0 && (mip[pointer] & 0x02) > 0 && (mip[pointer] & 0x04) > 0)  //bits 2 & 4 set - repeat following pixel
                    {
                        lastInstruction = pointer;
                        int count = GetRepeatRunLength(mip, ref pointer);
                        tot += count;
                        pointer++;
                        for (int i = 0; i < count; i++)
                        {
                            Array.Copy(mip, pointer, pixels, pixelPointer, 4);
                            pixelPointer += 4;
                        }
                        pointer += 4;
                    }
                    else if ((mip[pointer] & 0x01) > 0 && (mip[pointer] & 0x02) == 0)        //copy pixels for following indexes
                    {
                        lastInstruction = pointer;
                        int count = GetPixelRunLength(mip, ref pointer);
                        tot += count;
                        pointer++;
                        for (int i = 0; i < count; i++)
                        {
                            // int index = (mip[pointer] >= 0x80) ? IndexReader(mip, ref pointer) : mip[pointer];
                            int index = GetColorIndex(mip, ref pointer);
                            Array.Copy(pixelArray[index], 0, pixels, pixelPointer, 4);
                            pixelPointer += 4;
                            pointer++;
                        }
                    }
                    else if ((mip[pointer] & 0x02) > 0 && (mip[pointer] & 0x01) == 0 && (mip[pointer] & 0x04) == 0)    //repeat count, one byte index
                    {
                        lastInstruction = pointer;
                        int count = GetRepeatRunLength(mip, ref pointer);
                        // int count = (mip[pointer] >= 0x80) ? RunReader(mip, ref pointer) : mip[pointer] / 8;
                        tot += count;
                        pointer++;
                        int index = mip[pointer];
                        for (int i = 0; i < count; i++)
                        {
                            Array.Copy(pixelArray[index], 0, pixels, pixelPointer, 4);
                            pixelPointer += 4;
                        }
                        pointer++;
                    }
                    else if ((mip[pointer] & 0x04) > 0 && (mip[pointer] & 0x01) == 0 && (mip[pointer] & 0x02) == 0)        //repeat count, two byte index
                    {
                        lastInstruction = pointer;
                        int count = GetRepeatRunLength(mip, ref pointer);
                        // int count = (mip[pointer] >= 0x80) ? RunReader(mip, ref pointer) : mip[pointer] / 8;
                        tot += count;
                        pointer++;
                        int index = BitConverter.ToUInt16(mip, pointer);
                        for (int i = 0; i < count; i++)
                        {
                            Array.Copy(pixelArray[index], 0, pixels, pixelPointer, 4);
                            pixelPointer += 4;
                        }
                        pointer += 2;
                    }
                    else
                    {
                        throw new ApplicationException("Unknown instruction: " + mip[pointer].ToString("X") +
                        " Position: " + ((pointer + mipPositions[mipLevel]).ToString() +
                        " Last good instruction: " + (lastInstruction + mipPositions[mipLevel]).ToString()));
                    }
                }
            }
            catch
            { }

            byte[] blockPixels = new byte[pixels.Length];
            int x = 0, y = 0;
            int w1 = w * 4;
            for (int i = 0; i < pixels.Length; i += 64)
            {
                for (int j = 0; j < 4; j++)
                {
                    Array.Copy(pixels, i + (j * 16), blockPixels, (y * w1) + x, 16);
                    y++;
                }
                x += 16;
                if (x >= w1)
                {
                    x = 0;
                }
                else
                {
                    y -= 4;
                }
            }

            Bitmap image = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            System.Drawing.Imaging.BitmapData bmpData = image.LockBits(rect, ImageLockMode.ReadWrite, image.PixelFormat);

            IntPtr ptr;
            if (bmpData.Stride > 0)
            {
                ptr = bmpData.Scan0;
            }
            else
            {
                ptr = bmpData.Scan0 + bmpData.Stride * (image.Height - 1);
            }

            int bytes = Math.Abs(bmpData.Stride) * image.Height;

            System.Runtime.InteropServices.Marshal.Copy(blockPixels, 0, ptr, bytes);
            image.UnlockBits(bmpData);
            return image;
        }

        public int GetPixelRunLength(byte[] mip0, ref int pointer)
        {
            int count = ((mip0[pointer] & 0x7f) >> 2);
            int shift = 5;
            while ((mip0[pointer] & 0x80) != 0)
            {
                pointer++;
                count += (((mip0[pointer] & 0x7f)) << shift);
                shift += 7;
            }
            return count;
        }
        private int GetRepeatRunLength(byte[] mip0, ref int pointer)
        {
            int count = ((mip0[pointer] & 0x7F) >> 3);
            int shift = 4;
            while ((mip0[pointer] & 0x80) != 0)
            {
                pointer++;
                count += (((mip0[pointer] & 0x7f)) << shift);
                shift += 7;
            }
            return count;
        }

        private int GetColorIndex(byte[] mip0, ref int pointer)
        {
            int count = ((mip0[pointer] & 0x7f));
            int shift = 7;
            while ((mip0[pointer] & 0x80) != 0)
            {
                pointer++;
                count += (((mip0[pointer] & 0x7f)) << shift);
                shift += 7;
            }
            return count;
        }

        public static byte[] SetPixelRunLength(uint runLen)
        {
            List<byte> run = new List<byte>();
            byte tmp = (byte)((runLen & 0x1F) << 2);
            runLen >>= 5;
            while (runLen != 0)
            {
                tmp |= 0x80;
                run.Add(tmp);
                tmp = (byte)(runLen & 0x7F);
                runLen >>= 7;
            }
            run.Add(tmp);
            return run.ToArray();
        }

        public static byte[] SetRepeatRunLength(uint runLen)
        {
            List<byte> run = new List<byte>();
            byte tmp = (byte)((runLen & 0x0F) << 3);
            runLen >>= 4;
            while (runLen != 0)
            {
                tmp |= 0x80;
                run.Add(tmp);
                tmp = (byte)(runLen & 0x7F);
                runLen >>= 7;
            }
            run.Add(tmp);
            return run.ToArray();
        }
        public static byte[] SetColorIndex(uint runLen)
        {
            List<byte> run = new List<byte>();
            byte tmp = (byte)((runLen & 0x7F));
            runLen >>= 7;
            while (runLen != 0)
            {
                tmp |= 0x80;
                run.Add(tmp);
                tmp = (byte)(runLen & 0x7F);
                runLen >>= 7;
            }
            run.Add(tmp);
            return run.ToArray();
        }

        //Created under the assumption that colors were sorted by count. 
        //It is just based on apparences in the LRLE data not the raw image
        class ColorTable
        {
            public class ColorTableEntryData
            {
                public int Count;
                public int Index;
                public ColorTableEntryData(int ind)
                {
                    Index = ind;
                    Count = 1;
                }
            }

            public struct ColorTableEntry
            {

                public byte[] color;
                public override bool Equals(object obj)
                {
                    if (obj is ColorTableEntry)
                    {
                        ColorTableEntry other = (ColorTableEntry)obj;
                        return color.SequenceEqual(other.color);
                    }
                    return false;

                }
                public override int GetHashCode()
                {
                    return BitConverter.ToInt32(color, 0);
                }
            }

            public Dictionary<ColorTableEntry, ColorTableEntryData> colors;
            public bool HasColor(byte[] color)
            {
                if (color[3] == 0)
                {
                    color[0] = 0;
                    color[1] = 0;
                    color[2] = 0;
                }
                ColorTableEntry temp;
                temp.color = color;
                return colors.ContainsKey(temp);
            }
            public void AddColor(byte[] color)
            {
                //assumes byte 3 will be alpha and sets the color to 0 if it has an alpha of 0

                if (color[3] == 0)
                {
                    color[0] = 0;
                    color[1] = 0;
                    color[2] = 0;
                }
                ColorTableEntry temp;
                temp.color = color;
                if (!colors.ContainsKey(temp))
                {
                    colors.Add(temp, new ColorTableEntryData(colors.Count));
                }

                else
                {
                    colors[temp].Count++;
                }

            }
            public int GetColorIndex(byte[] color)
            {
                //assumes byte 3 will be alpha and sets the color to 0 if it has an alpha of 0
                if (color[3] == 0)
                {
                    color[0] = 0;
                    color[1] = 0;
                    color[2] = 0;
                }
                ColorTableEntry temp;
                temp.color = color;
                return colors[temp].Index;
            }
            public void SortColors()
            {
                int count = 0;
                foreach (var item in colors.OrderBy(x => x.Value.Count).Reverse())
                {
                    colors[item.Key].Index = count;
                    count++;
                }
            }
            public ColorTable()
            {
                colors = new Dictionary<ColorTableEntry, ColorTableEntryData>();
            }
        }

        //To make things easier to follow and debug
        enum EncodingState
        {
            Unknown,
            ColorRun,
            RepeatRun,
            StartNew
        }
        class Command
        {
            public EncodingState state;
            public uint length;
            public List<byte[]> colors;
            public Command(EncodingState s, uint len, List<byte[]> cols)
            {
                state = s;
                length = len;
                colors = cols;
            }
            public List<byte> ToOps(ColorTable colorTable)
            {
                var outbytes = new List<byte>();
                if (state == EncodingState.RepeatRun)
                {
                    outbytes.AddRange(SetRepeatRunLength(length));
                    var cindex = colorTable.GetColorIndex(colors[0]);
                    if (cindex >= 0x10000)
                    {
                        outbytes[0] |= (byte)0x6;
                        outbytes.AddRange(colors[0]);
                    }
                    else if (cindex >= 0x100)
                    {
                        outbytes[0] |= (byte)0x4;
                        outbytes.AddRange(BitConverter.GetBytes((ushort)cindex));
                    }
                    else
                    {

                        outbytes[0] |= (byte)0x2;
                        outbytes.Add((byte)cindex);
                    }
                    //  GetRepeatPixels(runLength, repeatIndex, opsList[mip], blockPixels);
                }
                else
                {
                    outbytes.AddRange(SetPixelRunLength(length));
                    List<int> indexes = new List<int>();
                    bool overmax = false;
                    foreach (var item in colors)
                    {
                        var ind = colorTable.GetColorIndex(item);
                        if (ind >= 0x10000)
                        {
                            overmax = true;
                        }
                        indexes.Add(ind);

                    }
                    if (overmax)
                    {
                        outbytes[0] |= (byte)0x3;
                        foreach (var c in colors)
                        {
                            outbytes.AddRange(c);
                        }
                    }
                    else
                    {
                        outbytes[0] |= (byte)0x1;
                        foreach (var c in indexes)
                        {
                            outbytes.AddRange(SetColorIndex((uint)c));
                        }
                    }

                }
                return outbytes;
            }
        }
    }


    /// <summary>
    /// ResourceHandler for LRLEResource wrapper
    /// </summary>
    public class LRLEResourceHandler : AResourceHandler
    {
        public LRLEResourceHandler()
        {
            this.Add(typeof(LRLEResource), new List<string>(new string[] { "0x2BC04EDF" }));
        }
    }
}
