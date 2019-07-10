/*Copyright (c) 2014 Rick (rick 'at' gibbed 'dot' us)

This software is provided 'as-is', without any express or implied
warranty. In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not
claim that you wrote the original software. If you use this software
in a product, an acknowledgment in the product documentation would be
appreciated but is not required.

2. Altered source versions must be plainly marked as such, and must not be
misrepresented as being the original software.

3. This notice may not be removed or altered from any source
distribution.*/

/*
 * This wrapper is based on Rick's code and is transformed into s4pi wrapper by Keyi Zhang.
 */
/*
 * This wrapper has been updated to import RLES images by cmarNYC.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using s4pi.Interfaces;

namespace s4pi.ImageResource
{
    public class RLEResource : AResource, IDisposable
    {
        const int recommendedApiVersion = 1;
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

        static bool checking = s4pi.Settings.Settings.Checking;

        #region Attributes
        private RLEInfo info;
        private MipHeader[] MipHeaders;
        private byte[] data;

        public uint MipCount { get { return this.info.mipCount; } }
        #endregion

        public RLEResource(int APIversion, Stream s) : base(APIversion, s) { if (s == null) { OnResourceChanged(this, EventArgs.Empty); } else { Parse(s); } }

        public int Width { get { return this.info.Width; } }
        public int Height { get { return this.info.Height; } }

        public void Dispose()
        {
            info = null;
            MipHeaders = null;
            data = null;
        }

        #region Data I/O
        public void Parse(Stream s)
        {
            if (s == null || s.Length == 0) { this.data = new byte[0]; return; }
            s.Position = 0;
            BinaryReader r = new BinaryReader(s);
            info = new RLEInfo(s);
            this.MipHeaders = new MipHeader[this.info.mipCount + 1];

            for (int i = 0; i < this.info.mipCount; i++)
            {
                if (info.notFourCC == NotFourCC.L8)
                {
                    var header = new MipHeader
                    {
                        CommandOffset = r.ReadInt32(),
                        Offset0 = r.ReadInt32()
                    };
                    MipHeaders[i] = header;
                }
                else
                {
                    var header = new MipHeader
                    {
                        CommandOffset = r.ReadInt32(),
                        Offset2 = r.ReadInt32(),
                        Offset3 = r.ReadInt32(),
                        Offset0 = r.ReadInt32(),
                        Offset1 = r.ReadInt32(),
                    };
                    if (this.info.Version == RLEVersion.RLES) header.Offset4 = r.ReadInt32();
                    MipHeaders[i] = header;
                }
            }

            if (info.notFourCC == NotFourCC.L8)
            {
                this.MipHeaders[this.info.mipCount] = new MipHeader
                {
                    CommandOffset = MipHeaders[0].Offset0,
                    Offset0 = (int)s.Length
                };
            }
            else
            {
                this.MipHeaders[this.info.mipCount] = new MipHeader
                {
                    CommandOffset = MipHeaders[0].Offset2,
                    Offset2 = MipHeaders[0].Offset3,
                    Offset3 = MipHeaders[0].Offset0,
                    Offset0 = MipHeaders[0].Offset1,
                };

                if (this.info.Version == RLEVersion.RLES)
                {
                    this.MipHeaders[this.info.mipCount].Offset1 = this.MipHeaders[0].Offset4;
                    this.MipHeaders[this.info.mipCount].Offset4 = (int)s.Length;
                }
                else
                {
                    this.MipHeaders[this.info.mipCount].Offset1 = (int)s.Length;
                }
            }

            s.Position = 0;
            this.data = r.ReadBytes((int)s.Length);
        }


        protected override Stream UnParse()
        {
            if (this.data == null || this.data.Length == 0) { return new MemoryStream(); }
            else { return new MemoryStream(this.data); }
        }

        public Stream ToDDS()
        {
            if (this.info == null) return null;
            MemoryStream s = new MemoryStream();
            BinaryWriter w = new BinaryWriter(s);
            w.Write(RLEInfo.Signature);
            this.info.UnParse(s);

            // NEED TO BE WRITTEN IN STATIC
            var fullDark = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            var fullBright = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            var fullTransparentAlpha = new byte[] { 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            var fullTransparentWhite = new byte[] { 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            var fullTransparentBlack = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            var fullOpaqueAlpha = new byte[] { 0x00, 0x05, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

            if (this.info.notFourCC == NotFourCC.L8)
            {
                int width = this.info.Width;
                int height = this.info.Height;
                for (int i = 0; i < this.info.mipCount; i++)
                {
                    MemoryStream s0 = new MemoryStream();
                    MemoryStream s1 = new MemoryStream();
                    MemoryStream s2 = new MemoryStream();
                    MemoryStream s3 = new MemoryStream();
                    var mipHeader = this.MipHeaders[i];
                    var nextMipHeader = MipHeaders[i + 1];

                    int blockOffset0;
                    blockOffset0 = mipHeader.Offset0;

                    for (int commandOffset = mipHeader.CommandOffset;
                        commandOffset < nextMipHeader.CommandOffset;
                        commandOffset += 2)
                    {
                        var command = BitConverter.ToUInt16(this.data, commandOffset);

                        var op = command & 3;
                        var count = command >> 2;

                        if (op == 0)
                        {
                            for (int j = 0; j < count; j++)
                            {
                                s0.Write(fullDark, 0, 4);
                                s1.Write(fullDark, 0, 4);
                                s2.Write(fullDark, 0, 4);
                                s3.Write(fullDark, 0, 4);
                            }
                        }
                        else if (op == 1)
                        {
                            for (int j = 0; j < count; j++)
                            {
                                s0.Write(this.data, blockOffset0, 4);
                                blockOffset0 += 4;
                                s1.Write(this.data, blockOffset0, 4);
                                blockOffset0 += 4;
                                s2.Write(this.data, blockOffset0, 4);
                                blockOffset0 += 4;
                                s3.Write(this.data, blockOffset0, 4);
                                blockOffset0 += 4;
                            }
                        }
                        else if (op == 2)
                        {
                            for (int j = 0; j < count; j++)
                            {
                                s0.Write(fullBright, 0, 4);
                                s1.Write(fullBright, 0, 4);
                                s2.Write(fullBright, 0, 4);
                                s3.Write(fullBright, 0, 4);
                            }
                        }
                        else
                        {
                            throw new NotSupportedException("OpCode: " + op.ToString() + ", Count: " + count.ToString());
                        }
                    }

                    if (blockOffset0 != nextMipHeader.Offset0)
                    {
                        throw new InvalidOperationException();
                    }
                    byte[] b0 = s0.ToArray();
                    byte[] b1 = s1.ToArray();
                    byte[] b2 = s2.ToArray();
                    byte[] b3 = s3.ToArray();
                    int wOffset = 0;
                    for (int h = 0; h < height; h += 4)
                    {
                        s.Write(b0, wOffset, width);
                        s.Write(b1, wOffset, width);
                        s.Write(b2, wOffset, width);
                        s.Write(b3, wOffset, width);
                        wOffset += width;
                    }
                    width = Math.Max(width / 2, 4);
                    height = Math.Max(height / 2, 4);
                }
            }
            else if (this.info.Version == RLEVersion.RLE2)
            {
                for (int i = 0; i < this.info.mipCount; i++)
                {
                    var mipHeader = this.MipHeaders[i];
                    var nextMipHeader = MipHeaders[i + 1];

                    int blockOffset2, blockOffset3, blockOffset0, blockOffset1;
                    blockOffset2 = mipHeader.Offset2;
                    blockOffset3 = mipHeader.Offset3;
                    blockOffset0 = mipHeader.Offset0;
                    blockOffset1 = mipHeader.Offset1;

                    for (int commandOffset = mipHeader.CommandOffset;
                        commandOffset < nextMipHeader.CommandOffset;
                        commandOffset += 2)
                    {
                        var command = BitConverter.ToUInt16(this.data, commandOffset);

                        var op = command & 3;
                        var count = command >> 2;

                        if (op == 0)
                        {
                            for (int j = 0; j < count; j++)
                            {
                                w.Write(fullTransparentAlpha, 0, 8);
                                w.Write(fullTransparentWhite, 0, 8);
                            }
                        }
                        else if (op == 1)
                        {
                            for (int j = 0; j < count; j++)
                            {
                                w.Write(this.data, blockOffset0, 2);
                                w.Write(this.data, blockOffset1, 6);
                                w.Write(this.data, blockOffset2, 4);
                                w.Write(this.data, blockOffset3, 4);
                                blockOffset2 += 4;
                                blockOffset3 += 4;
                                blockOffset0 += 2;
                                blockOffset1 += 6;
                            }
                        }
                        else if (op == 2)
                        {
                            for (int j = 0; j < count; j++)
                            {
                                w.Write(fullOpaqueAlpha, 0, 8);
                                w.Write(this.data, blockOffset2, 4);
                                w.Write(this.data, blockOffset3, 4);
                                blockOffset2 += 4;
                                blockOffset3 += 4;
                            }
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }

                    if (blockOffset0 != nextMipHeader.Offset0 ||
                        blockOffset1 != nextMipHeader.Offset1 ||
                        blockOffset2 != nextMipHeader.Offset2 ||
                        blockOffset3 != nextMipHeader.Offset3)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
            else            //rles
            {
                for (int i = 0; i < this.info.mipCount; i++)
                {
                    var mipHeader = this.MipHeaders[i];
                    var nextMipHeader = MipHeaders[i + 1];

                    int blockOffset2, blockOffset3, blockOffset0, blockOffset1, blockOffset4;
                    blockOffset2 = mipHeader.Offset2;
                    blockOffset3 = mipHeader.Offset3;
                    blockOffset0 = mipHeader.Offset0;
                    blockOffset1 = mipHeader.Offset1;
                    blockOffset4 = mipHeader.Offset4;
                    for (int commandOffset = mipHeader.CommandOffset;
                        commandOffset < nextMipHeader.CommandOffset;
                        commandOffset += 2)
                    {
                        var command = BitConverter.ToUInt16(this.data, commandOffset);

                        var op = command & 3;
                        var count = command >> 2;

                        if (op == 0)
                        {
                            for (int j = 0; j < count; j++)
                            {
                                w.Write(fullTransparentAlpha, 0, 8);
                                w.Write(fullTransparentBlack, 0, 8);
                            }
                        }
                        else if (op == 1)
                        {
                            for (int j = 0; j < count; j++)
                            {
                                w.Write(this.data, blockOffset0, 2);
                                w.Write(this.data, blockOffset1, 6);
                                blockOffset0 += 2;
                                blockOffset1 += 6;

                                w.Write(this.data, blockOffset2, 4);
                                w.Write(this.data, blockOffset3, 4);
                                blockOffset2 += 4;
                                blockOffset3 += 4;

                                blockOffset4 += 16;
                            }
                        }
                        else if (op == 2)
                        {
                            for (int j = 0; j < count; j++)
                            {
                                w.Write(this.data, blockOffset0, 2);
                                w.Write(this.data, blockOffset1, 6);
                                w.Write(this.data, blockOffset2, 4);
                                w.Write(this.data, blockOffset3, 4);
                                blockOffset2 += 4;
                                blockOffset3 += 4;
                                blockOffset0 += 2;
                                blockOffset1 += 6;
                            }
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }

                    if (blockOffset0 != nextMipHeader.Offset0 ||
                        blockOffset1 != nextMipHeader.Offset1 ||
                        blockOffset2 != nextMipHeader.Offset2 ||
                        blockOffset3 != nextMipHeader.Offset3 ||
                        blockOffset4 != nextMipHeader.Offset4)
                    {
                        throw new InvalidOperationException();
                    }
                }

            }
            s.Position = 0;
            return s;
        }

        /// <summary>
        /// Returns bitmap of specular map mask
        /// </summary>
        /// <returns>Bitmap mask image</returns>
        public Bitmap ToSpecularMaskImage()
        {
            if (this.info == null) return null;
            if (this.info.Version == RLEVersion.RLE2)
            {
                return null;
            }
            var mipHeader = this.MipHeaders[0];
            var nextMipHeader = MipHeaders[1];

            byte[] argbValues = GetMaskMipARGBarray(mipHeader, nextMipHeader, this.info.Width, this.info.Height);

            Bitmap mask = new Bitmap(this.info.Width, this.info.Height);
            Rectangle rect = new Rectangle(0, 0, mask.Width, mask.Height);
            BitmapData bmpData = mask.LockBits(rect, ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            IntPtr ptr = bmpData.Scan0;
            if (argbValues.Length != bmpData.Height * bmpData.Stride)
                throw new ApplicationException("ARGB array length does not match bitmap. ArrayLen: " + argbValues.Length.ToString() +
                    " Bitmap: height " + bmpData.Height.ToString() + " stride " + bmpData.Stride.ToString());

            // Copy the ARGB values to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(argbValues, 0, ptr, argbValues.Length);

            // Unlock the bits.
            mask.UnlockBits(bmpData);

            return mask;
        }

        /// <summary>
        /// Returns specular mask as DDS stream. The DDS is uncompressed.
        /// </summary>
        /// <returns>Stream of uncompressed DDS image</returns>
        public Stream ToSpecularMaskDDS()
        {
            if (this.info == null) return null;
            if (this.info.Version == RLEVersion.RLE2)
            {
                return null;
            }

            MemoryStream s = new MemoryStream();
            BinaryWriter w = new BinaryWriter(s);
            w.Write(RLEInfo.Signature);
            RLEInfo info = new RLEInfo(this.info);
            info.fourCC = FourCC.None;
            info.notFourCC = NotFourCC.None;
            info.pixelFormat = new PixelFormat(FourCC.None);
            info.UnParse(s);

            List<byte> argbValues = new List<byte>();
            int width = this.info.Width;
            int height = this.info.Height;

            for (int i = 0; i < this.info.mipCount; i++)
            {
                var mipHeader = this.MipHeaders[i];
                var nextMipHeader = MipHeaders[i + 1];

                argbValues.AddRange(GetMaskMipARGBarray(mipHeader, nextMipHeader, width, height));

                width = Math.Max(width / 2, 1);
                height = Math.Max(height / 2, 1);
            }

            w.Write(argbValues.ToArray());

            s.Position = 0;
            return s;
        }

        internal byte[] GetMaskMipARGBarray(MipHeader mipHeader, MipHeader nextMipHeader, int mipWidth, int mipHeight)
        {
            var maskBlack = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            var maskWhite = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

            List<byte[]> blocks = new List<byte[]>();

            int blockOffset2, blockOffset3, blockOffset0, blockOffset1, blockOffset4;
            blockOffset2 = mipHeader.Offset2;
            blockOffset3 = mipHeader.Offset3;
            blockOffset0 = mipHeader.Offset0;
            blockOffset1 = mipHeader.Offset1;
            blockOffset4 = mipHeader.Offset4;
            for (int commandOffset = mipHeader.CommandOffset;
                commandOffset < nextMipHeader.CommandOffset;
                commandOffset += 2)
            {
                var command = BitConverter.ToUInt16(this.data, commandOffset);

                var op = command & 3;
                var count = command >> 2;

                if (op == 0)
                {
                    for (int j = 0; j < count; j++)
                    {
                        blocks.Add(maskBlack);
                    }
                }
                else if (op == 1)
                {
                    for (int j = 0; j < count; j++)
                    {
                        blockOffset0 += 2;
                        blockOffset1 += 6;
                        blockOffset2 += 4;
                        blockOffset3 += 4;

                        byte[] tmp = new byte[16];
                        Array.Copy(this.data, blockOffset4, tmp, 0, 16);
                        blocks.Add(tmp);
                        blockOffset4 += 16;
                    }
                }
                else if (op == 2)
                {
                    for (int j = 0; j < count; j++)
                    {
                        blocks.Add(maskWhite);

                        blockOffset2 += 4;
                        blockOffset3 += 4;
                        blockOffset0 += 2;
                        blockOffset1 += 6;
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            if (blockOffset0 != nextMipHeader.Offset0 ||
                blockOffset1 != nextMipHeader.Offset1 ||
                blockOffset2 != nextMipHeader.Offset2 ||
                blockOffset3 != nextMipHeader.Offset3 ||
                blockOffset4 != nextMipHeader.Offset4)
            {
                throw new InvalidOperationException();
            }

            //Shuffle blocks of bytes into array
            byte[] imageData = new byte[blocks.Count * 16];
            int height = mipHeight;
            int width = mipWidth;
            int blockOffset = 0;
            for (int h = 0; h < height; h += 4)
            {
                for (int w = 0; w < width; w += 4)
                {
                    Array.Copy(blocks[blockOffset], 0, imageData, (h * width) + w, 4);
                    Array.Copy(blocks[blockOffset], 4, imageData, ((h + 1) * width) + w, 4);
                    Array.Copy(blocks[blockOffset], 8, imageData, ((h + 2) * width) + w, 4);
                    Array.Copy(blocks[blockOffset], 12, imageData, ((h + 3) * width) + w, 4);
                    blockOffset++;
                }
            }

            int bytesPerPixel = 4;
            int numBytes = imageData.Length * bytesPerPixel;
            byte[] argbValues = new byte[numBytes];

            for (int i = 0; i < imageData.Length; i++)
            {
                // argbValues is in format BGRA (Blue, Green, Red, Alpha)
                argbValues[i * 4] = imageData[i];
                argbValues[(i * 4) + 1] = imageData[i];
                argbValues[(i * 4) + 2] = imageData[i];
                argbValues[(i * 4) + 3] = 0xFF;
            }

            return argbValues;
        }


        /// <summary>
        /// Imports to RLE from DDS stream. Default is RLE2. If RLES is specified the mask is copied from the alpha of the DDS.
        /// </summary>
        /// <param name="input">DDS stream to import</param>
        /// <param name="rleVersion">Optional RLE version (RLE2 or RLES)</param>
        public void ImportToRLE(Stream input, RLEVersion rleVersion = RLEVersion.RLE2)
        {
            input.Position = 0;
            var fullOpaqueAlpha = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

            MemoryStream output = new MemoryStream();
            BinaryReader r = new BinaryReader(input);
            BinaryWriter w = new BinaryWriter(output);

            this.info = new RLEInfo();
            this.info.Parse(input);
            this.info.Version = rleVersion;
            if (this.info.pixelFormat.Fourcc != FourCC.DXT5 & this.info.notFourCC != NotFourCC.L8) throw new InvalidDataException(string.Format("Not a DXT5 or L8 format DDS, read FourCC: {0}, format: {0}", this.info.pixelFormat.Fourcc, this.info.notFourCC));

            if (this.info.Depth == 0) this.info.Depth = 1;

            if (this.info.fourCC == FourCC.DXT5)
            {
                w.Write((uint)FourCC.DXT5);
            }
            else if (this.info.notFourCC == NotFourCC.L8)
            {
                w.Write((uint)NotFourCC.L8);
            }
            if (rleVersion == RLEVersion.RLE2)
            {
                w.Write((uint)0x32454C52);
            }
            else
            {
                w.Write((uint)0x53454C52);
            }
            w.Write((ushort)this.info.Width);
            w.Write((ushort)this.info.Height);
            w.Write((ushort)this.info.mipCount);
            w.Write((ushort)0);

            if (this.info.notFourCC == NotFourCC.L8)
            {
                var headerOffset = 16;
                var dataOffset = 16 + (8 * this.info.mipCount);
                this.MipHeaders = new MipHeader[this.info.mipCount];

                using (var commandData = new MemoryStream())
                using (var block0Data = new MemoryStream())
                {
                    BinaryWriter commonDataWriter = new BinaryWriter(commandData);
                    for (int mipIndex = 0; mipIndex < this.info.mipCount; mipIndex++)
                    {
                        this.MipHeaders[mipIndex] = new MipHeader()
                        {
                            CommandOffset = (int)commandData.Length,
                            Offset0 = (int)block0Data.Length,
                        };

                        var mipWidth = Math.Max(4, this.info.Width >> mipIndex);
                        var mipHeight = Math.Max(4, this.info.Height >> mipIndex);
                        var mipDepth = Math.Max(1, this.info.Depth >> mipIndex);

                        byte[][] lines = new byte[((mipHeight + 3) / 4) * 4][];
                        for (int h = 0; h < mipHeight; h++)
                        {
                            lines[h] = new byte[((mipWidth + 3) / 4) * 4];
                            Array.Copy(r.ReadBytes(mipWidth), lines[h], mipWidth);
                        }
                        var mipSize = Math.Max(1, (mipWidth + 3) / 4) * Math.Max(1, (mipHeight + 3) / 4) * 16;
                        var mipData = new byte[mipSize];
                        int mipCounter = 0;
                        for (int h = 0; h < mipHeight; h += 4)
                        {
                            for (int l = 0; l < mipWidth; l += 4)
                            {
                                for (int h1 = h; h1 < h + 4; h1++)
                                {
                                    for (int l1 = l; l1 < l + 4; l1++)
                                    {
                                        mipData[mipCounter++] = lines[h1][l1];
                                    }
                                }
                            }
                        }

                        for (int offset = 0; offset < mipSize; )
                        {
                            ushort darkCount = 0;
                            while (darkCount < 0x3FFF &&
                                   offset < mipSize &&
                                   TestDarkAll(mipData, offset) == true)
                            {
                                darkCount++;
                                offset += 16;
                            }

                            if (darkCount > 0)
                            {
                                darkCount <<= 2;
                                darkCount |= 0;
                                commonDataWriter.Write(darkCount);
                                continue;
                            }

                            var lightOffset = offset;
                            ushort lightCount = 0;
                            while (lightCount < 0x3FFF &&
                                   offset < mipSize &&
                                   TestLightAll(mipData, offset) == true)
                            {
                                lightCount++;
                                offset += 16;
                            }

                            if (lightCount > 0)
                            {
                                lightCount <<= 2;
                                lightCount |= 2;
                                commonDataWriter.Write(lightCount);
                                continue;
                            }

                            var grayOffset = offset;
                            ushort grayCount = 0;
                            while (grayCount < 0x3FFF &&
                                   offset < mipSize &&
                                   TestDarkAll(mipData, offset) == false &&
                                   TestLightAll(mipData, offset) == false)
                            {
                                grayCount++;
                                offset += 16;
                            }

                            if (grayCount > 0)
                            {
                                for (int i = 0; i < grayCount; i++, grayOffset += 16)
                                {
                                    block0Data.Write(mipData, grayOffset, 16);
                                }

                                grayCount <<= 2;
                                grayCount |= 1;
                                commonDataWriter.Write(grayCount);
                                continue;
                            }

                            throw new NotImplementedException();
                        }
                    }

                    output.Position = dataOffset;

                    commandData.Position = 0;
                    var commandOffset = (int)output.Position;
                    output.Write(commandData.ToArray(), 0, (int)commandData.Length);

                    block0Data.Position = 0;
                    var block0Offset = (int)output.Position;
                    output.Write(block0Data.ToArray(), 0, (int)block0Data.Length);

                    output.Position = headerOffset;
                    for (int i = 0; i < this.info.mipCount; i++)
                    {
                        var mipHeader = this.MipHeaders[i];
                        w.Write(mipHeader.CommandOffset + commandOffset);
                        w.Write(mipHeader.Offset0 + block0Offset);
                    }
                }
            } 

            else if (rleVersion == RLEVersion.RLE2)
            {
                var headerOffset = 16;
                var dataOffset = 16 + (20 * this.info.mipCount);
                this.MipHeaders = new MipHeader[this.info.mipCount];

                using (var commandData = new MemoryStream())
                using (var block2Data = new MemoryStream())
                using (var block3Data = new MemoryStream())
                using (var block0Data = new MemoryStream())
                using (var block1Data = new MemoryStream())
                {
                    BinaryWriter commonDataWriter = new BinaryWriter(commandData);
                    for (int mipIndex = 0; mipIndex < this.info.mipCount; mipIndex++)
                    {
                        this.MipHeaders[mipIndex] = new MipHeader()
                        {
                            CommandOffset = (int)commandData.Length,
                            Offset2 = (int)block2Data.Length,
                            Offset3 = (int)block3Data.Length,
                            Offset0 = (int)block0Data.Length,
                            Offset1 = (int)block1Data.Length,
                        };

                        var mipWidth = Math.Max(4, this.info.Width >> mipIndex);
                        var mipHeight = Math.Max(4, this.info.Height >> mipIndex);
                        var mipDepth = Math.Max(1, this.info.Depth >> mipIndex);

                        var mipSize = Math.Max(1, (mipWidth + 3) / 4) * Math.Max(1, (mipHeight + 3) / 4) * 16;
                        var mipData = r.ReadBytes(mipSize);

                        for (int offset = 0; offset < mipSize; )
                        {
                            ushort transparentCount = 0;
                            while (transparentCount < 0x3FFF &&
                                   offset < mipSize &&
                                   TestAlphaAny(mipData, offset, a => a != 0) == false)
                            {
                                transparentCount++;
                                offset += 16;
                            }

                            if (transparentCount > 0)
                            {
                                transparentCount <<= 2;
                                transparentCount |= 0;
                                commonDataWriter.Write(transparentCount);
                                continue;
                            }

                            var opaqueOffset = offset;
                            ushort opaqueCount = 0;
                            while (opaqueCount < 0x3FFF &&
                                   offset < mipSize &&
                                   TestAlphaAll(mipData, offset, a => a == 0xFF) == true)
                            {
                                opaqueCount++;
                                offset += 16;
                            }

                            if (opaqueCount > 0)
                            {
                                for (int i = 0; i < opaqueCount; i++, opaqueOffset += 16)
                                {
                                    block2Data.Write(mipData, opaqueOffset + 8, 4);
                                    block3Data.Write(mipData, opaqueOffset + 12, 4);
                                }

                                opaqueCount <<= 2;
                                opaqueCount |= 2;
                                commonDataWriter.Write(opaqueCount);
                                continue;
                            }

                            var translucentOffset = offset;
                            ushort translucentCount = 0;
                            while (translucentCount < 0x3FFF &&
                                   offset < mipSize &&
                                   TestAlphaAny(mipData, offset, a => a != 0) == true &&
                                   TestAlphaAll(mipData, offset, a => a == 0xFF) == false)
                            {
                                translucentCount++;
                                offset += 16;
                            }

                            if (translucentCount > 0)
                            {
                                for (int i = 0; i < translucentCount; i++, translucentOffset += 16)
                                {
                                    block0Data.Write(mipData, translucentOffset + 0, 2);
                                    block1Data.Write(mipData, translucentOffset + 2, 6);
                                    block2Data.Write(mipData, translucentOffset + 8, 4);
                                    block3Data.Write(mipData, translucentOffset + 12, 4);
                                }

                                translucentCount <<= 2;
                                translucentCount |= 1;
                                commonDataWriter.Write(translucentCount);
                                continue;
                            }

                            throw new NotImplementedException();
                        }
                    }

                    output.Position = dataOffset;

                    commandData.Position = 0;
                    var commandOffset = (int)output.Position;
                    output.Write(commandData.ToArray(), 0, (int)commandData.Length);

                    block2Data.Position = 0;
                    var block2Offset = (int)output.Position;
                    output.Write(block2Data.ToArray(), 0, (int)block2Data.Length);

                    block3Data.Position = 0;
                    var block3Offset = (int)output.Position;
                    output.Write(block3Data.ToArray(), 0, (int)block3Data.Length);

                    block0Data.Position = 0;
                    var block0Offset = (int)output.Position;
                    output.Write(block0Data.ToArray(), 0, (int)block0Data.Length);

                    block1Data.Position = 0;
                    var block1Offset = (int)output.Position;
                    output.Write(block1Data.ToArray(), 0, (int)block1Data.Length);

                    output.Position = headerOffset;
                    for (int i = 0; i < this.info.mipCount; i++)
                    {
                        var mipHeader = this.MipHeaders[i];
                        w.Write(mipHeader.CommandOffset + commandOffset);
                        w.Write(mipHeader.Offset2 + block2Offset);
                        w.Write(mipHeader.Offset3 + block3Offset);
                        w.Write(mipHeader.Offset0 + block0Offset);
                        w.Write(mipHeader.Offset1 + block1Offset);
                    }
                }
            }
            else        //rles with mask generated from alpha
            {
                var headerOffset = 16;
                var dataOffset = 16 + (24 * this.info.mipCount);
                this.MipHeaders = new MipHeader[this.info.mipCount];

                using (var commandData = new MemoryStream())
                using (var block2Data = new MemoryStream())
                using (var block3Data = new MemoryStream())
                using (var block0Data = new MemoryStream())
                using (var block1Data = new MemoryStream())
                using (var block4Data = new MemoryStream())
                {
                    BinaryWriter commonDataWriter = new BinaryWriter(commandData);
                    for (int mipIndex = 0; mipIndex < this.info.mipCount; mipIndex++)
                    {
                        this.MipHeaders[mipIndex] = new MipHeader()
                        {
                            CommandOffset = (int)commandData.Length,
                            Offset2 = (int)block2Data.Length,
                            Offset3 = (int)block3Data.Length,
                            Offset0 = (int)block0Data.Length,
                            Offset1 = (int)block1Data.Length,
                            Offset4 = (int)block4Data.Length
                        };

                        var mipWidth = Math.Max(4, this.info.Width >> mipIndex);
                        var mipHeight = Math.Max(4, this.info.Height >> mipIndex);
                        var mipDepth = Math.Max(1, this.info.Depth >> mipIndex);

                        var mipSize = Math.Max(1, (mipWidth + 3) / 4) * Math.Max(1, (mipHeight + 3) / 4) * 16;
                        var mipData = r.ReadBytes(mipSize);

                        for (int offset = 0; offset < mipSize; )
                        {
                            ushort transparentCount = 0;
                            while (transparentCount < 0x3FFF &&
                                   offset < mipSize &&
                                   TestAlphaAny(mipData, offset, a => a != 0) == false)
                            {
                                transparentCount++;
                                offset += 16;
                            }

                            if (transparentCount > 0)
                            {
                                transparentCount <<= 2;
                                transparentCount |= 0;
                                commonDataWriter.Write(transparentCount);
                                continue;
                            }

                            var opaqueOffset = offset;
                            ushort opaqueCount = 0;
                            while (opaqueCount < 0x3FFF &&
                                   offset < mipSize &&
                                   TestAlphaAll(mipData, offset, a => a == 0xFF) == true)
                            {
                                opaqueCount++;
                                offset += 16;
                            }

                            if (opaqueCount > 0)
                            {
                                for (int i = 0; i < opaqueCount; i++, opaqueOffset += 16)
                                {
                                    block0Data.Write(mipData, opaqueOffset + 0, 2);
                                    block1Data.Write(mipData, opaqueOffset + 2, 6);
                                    block2Data.Write(mipData, opaqueOffset + 8, 4);
                                    block3Data.Write(mipData, opaqueOffset + 12, 4);
                                   // block4Data.Write(fullOpaqueAlpha, 0, 8);
                                }

                                opaqueCount <<= 2;
                                opaqueCount |= 2;
                                commonDataWriter.Write(opaqueCount);
                                continue;
                            }

                            var translucentOffset = offset;
                            ushort translucentCount = 0;
                            while (translucentCount < 0x3FFF &&
                                   offset < mipSize &&
                                   TestAlphaAny(mipData, offset, a => a != 0) == true &&
                                   TestAlphaAll(mipData, offset, a => a == 0xFF) == false)
                            {
                                translucentCount++;
                                offset += 16;
                            }

                            if (translucentCount > 0)
                            {
                                for (int i = 0; i < translucentCount; i++, translucentOffset += 16)
                                {
                                    block0Data.Write(mipData, translucentOffset + 0, 2);
                                    block1Data.Write(mipData, translucentOffset + 2, 6);
                                    block2Data.Write(mipData, translucentOffset + 8, 4);
                                    block3Data.Write(mipData, translucentOffset + 12, 4);
                                    block4Data.Write(fullOpaqueAlpha, 0, 8);
                                    block4Data.Write(fullOpaqueAlpha, 0, 8);
                                }

                                translucentCount <<= 2;
                                translucentCount |= 1;
                                commonDataWriter.Write(translucentCount);
                                continue;
                            }

                            throw new NotImplementedException();
                        }
                    }

                    output.Position = dataOffset;

                    commandData.Position = 0;
                    var commandOffset = (int)output.Position;
                    output.Write(commandData.ToArray(), 0, (int)commandData.Length);

                    block2Data.Position = 0;
                    var block2Offset = (int)output.Position;
                    output.Write(block2Data.ToArray(), 0, (int)block2Data.Length);

                    block3Data.Position = 0;
                    var block3Offset = (int)output.Position;
                    output.Write(block3Data.ToArray(), 0, (int)block3Data.Length);

                    block0Data.Position = 0;
                    var block0Offset = (int)output.Position;
                    output.Write(block0Data.ToArray(), 0, (int)block0Data.Length);

                    block1Data.Position = 0;
                    var block1Offset = (int)output.Position;
                    output.Write(block1Data.ToArray(), 0, (int)block1Data.Length);

                    block4Data.Position = 0;
                    var block4Offset = (int)output.Position;
                    output.Write(block4Data.ToArray(), 0, (int)block4Data.Length);

                    output.Position = headerOffset;
                    for (int i = 0; i < this.info.mipCount; i++)
                    {
                        var mipHeader = this.MipHeaders[i];
                        w.Write(mipHeader.CommandOffset + commandOffset);
                        w.Write(mipHeader.Offset2 + block2Offset);
                        w.Write(mipHeader.Offset3 + block3Offset);
                        w.Write(mipHeader.Offset0 + block0Offset);
                        w.Write(mipHeader.Offset1 + block1Offset);
                        w.Write(mipHeader.Offset4 + block4Offset);
                    }
                }
            }

            //     this.data = output.ToArray();
            output.Position = 0;
            Parse(output);
        }

        /// <summary>
        /// Imports to RLES using DDS specular stream and Bitmap mask
        /// </summary>
        /// <param name="specularTexture">DDS stream of main specular</param>
        /// <param name="specularMask">Bitmap of specular mask</param>
        public void ImportToRLESwithMask(Stream specularTexture, Bitmap specularMask)
        {
            specularTexture.Position = 0;
            var fullOpaqueAlpha = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

            MemoryStream output = new MemoryStream();
            BinaryReader r = new BinaryReader(specularTexture);
            BinaryWriter w = new BinaryWriter(output);

            this.info = new RLEInfo();
            this.info.Parse(specularTexture);
            if (this.info.Height != specularMask.Height || this.info.Width != specularMask.Width)
                throw new InvalidOperationException("Specular texture and mask dimension do not match");

            Bitmap maskMip = specularMask;

            this.info.Version = RLEResource.RLEVersion.RLES;
            if (this.info.pixelFormat.Fourcc != FourCC.DXT5) throw new InvalidDataException(string.Format("Specular not a DXT5 format DDS, read FourCC: {0}, format: {0}", this.info.pixelFormat.Fourcc, this.info.notFourCC));

            if (this.info.Depth == 0) this.info.Depth = 1;

            if (this.info.fourCC == FourCC.DXT5)
            {
                w.Write((uint)FourCC.DXT5);
            }
            w.Write((uint)0x53454C52);
            w.Write((ushort)this.info.Width);
            w.Write((ushort)this.info.Height);
            w.Write((ushort)this.info.mipCount);
            w.Write((ushort)0);

            var headerOffset = 16;
            var dataOffset = 16 + (24 * this.info.mipCount);
            this.MipHeaders = new MipHeader[this.info.mipCount];

            using (var commandData = new MemoryStream())
            using (var block2Data = new MemoryStream())
            using (var block3Data = new MemoryStream())
            using (var block0Data = new MemoryStream())
            using (var block1Data = new MemoryStream())
            using (var block4Data = new MemoryStream())
            {
                BinaryWriter commonDataWriter = new BinaryWriter(commandData);
                for (int mipIndex = 0; mipIndex < this.info.mipCount; mipIndex++)
                {
                    this.MipHeaders[mipIndex] = new MipHeader()
                    {
                        CommandOffset = (int)commandData.Length,
                        Offset2 = (int)block2Data.Length,
                        Offset3 = (int)block3Data.Length,
                        Offset0 = (int)block0Data.Length,
                        Offset1 = (int)block1Data.Length,
                        Offset4 = (int)block4Data.Length
                    };

                    var mipWidth = Math.Max(4, this.info.Width >> mipIndex);
                    var mipHeight = Math.Max(4, this.info.Height >> mipIndex);
                    var mipDepth = Math.Max(1, this.info.Depth >> mipIndex);

                    var mipSize = Math.Max(1, (mipWidth + 3) / 4) * Math.Max(1, (mipHeight + 3) / 4) * 16;
                    var mipData = r.ReadBytes(mipSize);

                    byte[] maskData = GetMaskMipBlocks(maskMip);
                    if (maskMip.Width > 1 || maskMip.Height > 1)
                    {
                        int newWidth = Math.Max(maskMip.Width / 2, 1);
                        int newHeight = Math.Max(maskMip.Height / 2, 1);
                        Bitmap newMip = new Bitmap(maskMip, newWidth, newHeight);
                        maskMip = newMip;
                    }

                    for (int offset = 0; offset < mipSize; )
                    {
                        ushort transparentCount = 0;
                        while (transparentCount < 0x3FFF &&
                                offset < mipSize &&
                                TestDarkAll(maskData, offset) == true)
                        {
                            transparentCount++;
                            offset += 16;
                        }

                        if (transparentCount > 0)
                        {
                            transparentCount <<= 2;
                            transparentCount |= 0;
                            commonDataWriter.Write(transparentCount);
                            continue;
                        }

                        var opaqueOffset = offset;
                        ushort opaqueCount = 0;
                        while (opaqueCount < 0x3FFF &&
                                offset < mipSize &&
                                TestLightAll(maskData, offset) == true)
                        {
                            opaqueCount++;
                            offset += 16;
                        }

                        if (opaqueCount > 0)
                        {
                            for (int i = 0; i < opaqueCount; i++, opaqueOffset += 16)
                            {
                                block0Data.Write(mipData, opaqueOffset + 0, 2);
                                block1Data.Write(mipData, opaqueOffset + 2, 6);
                                block2Data.Write(mipData, opaqueOffset + 8, 4);
                                block3Data.Write(mipData, opaqueOffset + 12, 4);
                            }

                            opaqueCount <<= 2;
                            opaqueCount |= 2;
                            commonDataWriter.Write(opaqueCount);
                            continue;
                        }

                        var translucentOffset = offset;
                        ushort translucentCount = 0;
                        while (translucentCount < 0x3FFF &&
                                offset < mipSize &&
                                TestDarkAll(maskData, offset) == false &&
                                TestLightAll(maskData, offset) == false)
                        {
                            translucentCount++;
                            offset += 16;
                        }

                        if (translucentCount > 0)
                        {
                            for (int i = 0; i < translucentCount; i++, translucentOffset += 16)
                            {
                                block0Data.Write(mipData, translucentOffset + 0, 2);
                                block1Data.Write(mipData, translucentOffset + 2, 6);
                                block2Data.Write(mipData, translucentOffset + 8, 4);
                                block3Data.Write(mipData, translucentOffset + 12, 4);
                                block4Data.Write(maskData, translucentOffset, 16);
                            }

                            translucentCount <<= 2;
                            translucentCount |= 1;
                            commonDataWriter.Write(translucentCount);
                            continue;
                        }

                        throw new NotImplementedException();
                    }
                }

                output.Position = dataOffset;

                commandData.Position = 0;
                var commandOffset = (int)output.Position;
                output.Write(commandData.ToArray(), 0, (int)commandData.Length);

                block2Data.Position = 0;
                var block2Offset = (int)output.Position;
                output.Write(block2Data.ToArray(), 0, (int)block2Data.Length);

                block3Data.Position = 0;
                var block3Offset = (int)output.Position;
                output.Write(block3Data.ToArray(), 0, (int)block3Data.Length);

                block0Data.Position = 0;
                var block0Offset = (int)output.Position;
                output.Write(block0Data.ToArray(), 0, (int)block0Data.Length);

                block1Data.Position = 0;
                var block1Offset = (int)output.Position;
                output.Write(block1Data.ToArray(), 0, (int)block1Data.Length);

                block4Data.Position = 0;
                var block4Offset = (int)output.Position;
                output.Write(block4Data.ToArray(), 0, (int)block4Data.Length);

                output.Position = headerOffset;
                for (int i = 0; i < this.info.mipCount; i++)
                {
                    var mipHeader = this.MipHeaders[i];
                    w.Write(mipHeader.CommandOffset + commandOffset);
                    w.Write(mipHeader.Offset2 + block2Offset);
                    w.Write(mipHeader.Offset3 + block3Offset);
                    w.Write(mipHeader.Offset0 + block0Offset);
                    w.Write(mipHeader.Offset1 + block1Offset);
                    w.Write(mipHeader.Offset4 + block4Offset);
                }
            }

            output.Position = 0;
            Parse(output);
        }
        
        /// <summary>
        /// Imports to RLES using DXT5 DDS specular stream and uncompressed DDS mask stream
        /// </summary>
        /// <param name="specularTexture">DDS stream of main specular</param>
        /// <param name="specularMask">DDS stream of specular mask - must be uncompressed DDS</param>
        public void ImportToRLESwithMask(Stream specularTexture, Stream specularMask)
        {
            specularTexture.Position = 0;
            var fullOpaqueAlpha = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

            MemoryStream output = new MemoryStream();
            BinaryReader r = new BinaryReader(specularTexture);
            BinaryReader m = new BinaryReader(specularMask);

            BinaryWriter w = new BinaryWriter(output);

            this.info = new RLEInfo();
            this.info.Parse(specularTexture);
            RLEInfo maskInfo = new RLEInfo();
            maskInfo.Parse(specularMask);

            if (this.info.Height != maskInfo.Height || this.info.Width != maskInfo.Width) 
                throw new InvalidOperationException("Specular texture and mask dimension do not match");

            this.info.Version = RLEResource.RLEVersion.RLES;
            if (this.info.pixelFormat.Fourcc != FourCC.DXT5) throw new InvalidDataException(string.Format("Specular not a DXT5 format DDS, read FourCC: {0}, format: {0}", this.info.pixelFormat.Fourcc, this.info.notFourCC));

            if (this.info.Depth == 0) this.info.Depth = 1;

            if (this.info.fourCC == FourCC.DXT5)
            {
                w.Write((uint)FourCC.DXT5);
            }
            w.Write((uint)0x53454C52);
            w.Write((ushort)this.info.Width);
            w.Write((ushort)this.info.Height);
            w.Write((ushort)this.info.mipCount);
            w.Write((ushort)0);

            var headerOffset = 16;
            var dataOffset = 16 + (24 * this.info.mipCount);
            this.MipHeaders = new MipHeader[this.info.mipCount];

            using (var commandData = new MemoryStream())
            using (var block2Data = new MemoryStream())
            using (var block3Data = new MemoryStream())
            using (var block0Data = new MemoryStream())
            using (var block1Data = new MemoryStream())
            using (var block4Data = new MemoryStream())
            {
                BinaryWriter commonDataWriter = new BinaryWriter(commandData);
                for (int mipIndex = 0; mipIndex < this.info.mipCount; mipIndex++)
                {
                    this.MipHeaders[mipIndex] = new MipHeader()
                    {
                        CommandOffset = (int)commandData.Length,
                        Offset2 = (int)block2Data.Length,
                        Offset3 = (int)block3Data.Length,
                        Offset0 = (int)block0Data.Length,
                        Offset1 = (int)block1Data.Length,
                        Offset4 = (int)block4Data.Length
                    };

                    var mipWidth = Math.Max(4, this.info.Width >> mipIndex);
                    var mipHeight = Math.Max(4, this.info.Height >> mipIndex);
                    var mipWidthMask = Math.Max(1, this.info.Width >> mipIndex);
                    var mipHeightMask = Math.Max(1, this.info.Height >> mipIndex);

                    var mipSize = Math.Max(1, (mipWidth + 3) / 4) * Math.Max(1, (mipHeight + 3) / 4) * 16;
                    var mipSizeMask = Math.Max(1, mipWidthMask * mipHeightMask * 4);
                    var mipData = r.ReadBytes(mipSize);
                    byte[] tmp = m.ReadBytes(mipSizeMask);
                    var mipMask = GetMaskMipBlocks(tmp, mipHeightMask, mipWidthMask);

                    for (int offset = 0; offset < mipSize; )
                    {
                        ushort transparentCount = 0;
                        while (transparentCount < 0x3FFF &&
                                offset < mipSize &&
                                TestDarkAll(mipMask, offset) == true)
                        {
                            transparentCount++;
                            offset += 16;
                        }

                        if (transparentCount > 0)
                        {
                            transparentCount <<= 2;
                            transparentCount |= 0;
                            commonDataWriter.Write(transparentCount);
                            continue;
                        }

                        var opaqueOffset = offset;
                        ushort opaqueCount = 0;
                        while (opaqueCount < 0x3FFF &&
                                offset < mipSize &&
                                TestLightAll(mipMask, offset) == true)
                        {
                            opaqueCount++;
                            offset += 16;
                        }

                        if (opaqueCount > 0)
                        {
                            for (int i = 0; i < opaqueCount; i++, opaqueOffset += 16)
                            {
                                block0Data.Write(mipData, opaqueOffset + 0, 2);
                                block1Data.Write(mipData, opaqueOffset + 2, 6);
                                block2Data.Write(mipData, opaqueOffset + 8, 4);
                                block3Data.Write(mipData, opaqueOffset + 12, 4);
                            }

                            opaqueCount <<= 2;
                            opaqueCount |= 2;
                            commonDataWriter.Write(opaqueCount);
                            continue;
                        }

                        var translucentOffset = offset;
                        ushort translucentCount = 0;
                        while (translucentCount < 0x3FFF &&
                                offset < mipSize &&
                                TestDarkAll(mipMask, offset) == false &&
                                TestLightAll(mipMask, offset) == false)
                        {
                            translucentCount++;
                            offset += 16;
                        }

                        if (translucentCount > 0)
                        {
                            for (int i = 0; i < translucentCount; i++, translucentOffset += 16)
                            {
                                block0Data.Write(mipData, translucentOffset + 0, 2);
                                block1Data.Write(mipData, translucentOffset + 2, 6);
                                block2Data.Write(mipData, translucentOffset + 8, 4);
                                block3Data.Write(mipData, translucentOffset + 12, 4);
                                block4Data.Write(mipMask, translucentOffset, 16);
                            }

                            translucentCount <<= 2;
                            translucentCount |= 1;
                            commonDataWriter.Write(translucentCount);
                            continue;
                        }

                        throw new NotImplementedException();
                    }
                }

                output.Position = dataOffset;

                commandData.Position = 0;
                var commandOffset = (int)output.Position;
                output.Write(commandData.ToArray(), 0, (int)commandData.Length);

                block2Data.Position = 0;
                var block2Offset = (int)output.Position;
                output.Write(block2Data.ToArray(), 0, (int)block2Data.Length);

                block3Data.Position = 0;
                var block3Offset = (int)output.Position;
                output.Write(block3Data.ToArray(), 0, (int)block3Data.Length);

                block0Data.Position = 0;
                var block0Offset = (int)output.Position;
                output.Write(block0Data.ToArray(), 0, (int)block0Data.Length);

                block1Data.Position = 0;
                var block1Offset = (int)output.Position;
                output.Write(block1Data.ToArray(), 0, (int)block1Data.Length);

                block4Data.Position = 0;
                var block4Offset = (int)output.Position;
                output.Write(block4Data.ToArray(), 0, (int)block4Data.Length);

                output.Position = headerOffset;
                for (int i = 0; i < this.info.mipCount; i++)
                {
                    var mipHeader = this.MipHeaders[i];
                    w.Write(mipHeader.CommandOffset + commandOffset);
                    w.Write(mipHeader.Offset2 + block2Offset);
                    w.Write(mipHeader.Offset3 + block3Offset);
                    w.Write(mipHeader.Offset0 + block0Offset);
                    w.Write(mipHeader.Offset1 + block1Offset);
                    w.Write(mipHeader.Offset4 + block4Offset);
                }
            }

            output.Position = 0;
            Parse(output);
        }

        private byte[] GetMaskMipBlocks(Bitmap mip)
        {
            //Get mask image data and shuffle into blocks
            Rectangle rect = new Rectangle(0, 0, mip.Width, mip.Height);
            BitmapData bmpData = mip.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            IntPtr ptr = bmpData.Scan0;
            int bytesPerPixel = 4;
            byte[] pixels = new byte[mip.Width * mip.Height * bytesPerPixel];
            // Copy the ARGB values to data array
            System.Runtime.InteropServices.Marshal.Copy(ptr, pixels, 0, pixels.Length);

            //reduce pixels to byte array, argb -> single byte grayscale
            byte[] data = new byte[mip.Width * mip.Height];
            for (int i = 0; i < pixels.Length; i += 4)
            {
                data[i / 4] = (byte)((pixels[i] + pixels[i + 1] + pixels[i + 2]) / 3);
            }

            if (data.Length <= 16)
            {
                byte[] paddedData = new byte[16];
                Array.Copy(data, 0, paddedData, 0, data.Length);
                mip.UnlockBits(bmpData);
                return paddedData;
            }

            List<byte[]> blocks = new List<byte[]>();
            for (int h = 0; h < mip.Height; h += 4)
            {
                for (int w = 0; w < mip.Width; w += 4)
                {
                    byte[] block = new byte[16];
                    Array.Copy(data, (h * mip.Width) + w, block, 0, 4);
                    Array.Copy(data, ((h + 1) * mip.Width) + w, block, 4, 4);
                    Array.Copy(data, ((h + 2) * mip.Width) + w, block, 8, 4);
                    Array.Copy(data, ((h + 3) * mip.Width) + w, block, 12, 4);
                    blocks.Add(block);
                }
            }

            // Unlock the bits.
            mip.UnlockBits(bmpData);

            byte[] maskData = new byte[blocks.Count * 16];
            for (int i = 0; i < blocks.Count; i++)
            {
                Array.Copy(blocks[i], 0, maskData, i * 16, 16);
            }
            return maskData;
        }

        private byte[] GetMaskMipBlocks(byte[] pixels, int mipHeight, int mipWidth)
        {
            //Transforms mask image argb pixel data and return it reduced to one byte per pixel and shuffled into 16-byte blocks

            //reduce pixels to byte array, argb -> single byte grayscale
            byte[] mipData = new byte[pixels.Length / 4];
            for (int i = 0; i < pixels.Length; i += 4)
            {
                mipData[i / 4] = (byte)((pixels[i] + pixels[i + 1] + pixels[i + 2]) / 3);
            }

            if (mipData.Length <= 16)
            {
                byte[] paddedData = new byte[16];
                Array.Copy(mipData, 0, paddedData, 0, mipData.Length);
                return paddedData;
            }

            List<byte[]> blocks = new List<byte[]>();
            for (int h = 0; h < mipHeight; h += 4)
            {
                for (int w = 0; w < mipWidth; w += 4)
                {
                    byte[] block = new byte[16];
                    Array.Copy(mipData, (h * mipWidth) + w, block, 0, 4);
                    Array.Copy(mipData, ((h + 1) * mipWidth) + w, block, 4, 4);
                    Array.Copy(mipData, ((h + 2) * mipWidth) + w, block, 8, 4);
                    Array.Copy(mipData, ((h + 3) * mipWidth) + w, block, 12, 4);
                    blocks.Add(block);
                }
            }

            byte[] maskData = new byte[blocks.Count * 16];
            for (int i = 0; i < blocks.Count; i++)
            {
                Array.Copy(blocks[i], 0, maskData, i * 16, 16);
            }
            return maskData;
        }

        private static bool TrueForAny<T>(T[] array, int offset, int count, Predicate<T> match)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            if (offset < 0 || offset > array.Length)
            {
                throw new IndexOutOfRangeException();
            }

            var end = offset + count;
            if (end < 0 || end > array.Length)
            {
                throw new IndexOutOfRangeException();
            }

            for (int index = offset; index < end; index++)
            {
                if (match(array[index]) == true)
                {
                    return true;
                }
            }
            return false;
        }

        private static unsafe void UnpackAlpha(byte[] array, int offset, byte* alpha, out ulong bits)
        {
            alpha[0] = array[offset + 0];
            alpha[1] = array[offset + 1];

            if (alpha[0] > alpha[1])
            {
                alpha[2] = (byte)((6 * alpha[0] + 1 * alpha[1] + 3) / 7);
                alpha[3] = (byte)((5 * alpha[0] + 2 * alpha[1] + 3) / 7);
                alpha[4] = (byte)((4 * alpha[0] + 3 * alpha[1] + 3) / 7);
                alpha[5] = (byte)((3 * alpha[0] + 4 * alpha[1] + 3) / 7);
                alpha[6] = (byte)((2 * alpha[0] + 5 * alpha[1] + 3) / 7);
                alpha[7] = (byte)((1 * alpha[0] + 6 * alpha[1] + 3) / 7);
            }
            else
            {
                alpha[2] = (byte)((4 * alpha[0] + 1 * alpha[1] + 2) / 5);
                alpha[3] = (byte)((3 * alpha[0] + 2 * alpha[1] + 2) / 5);
                alpha[4] = (byte)((2 * alpha[0] + 3 * alpha[1] + 2) / 5);
                alpha[5] = (byte)((1 * alpha[0] + 4 * alpha[1] + 2) / 5);
                alpha[6] = 0x00;
                alpha[7] = 0xFF;
            }

            bits = 0;
            for (int i = 7; i >= 2; i--)
            {
                bits <<= 8;
                bits |= array[offset + i];
            }
        }

        private static bool TestLightAll(byte[] array, int offset)
        {
            for (int i = 0; i < 16; i++)
            {
                if (array[offset + i] != 0xFF)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool TestDarkAll(byte[] array, int offset)
        {
            for (int i = 0; i < 16; i++)
            {
                if (array[offset + i] != 0x00)
                {
                    return false;
                }
            }
            return true;
        }
        
        private static unsafe bool TestAlphaAll(byte[] array, int offset, Func<byte, bool> test)
        {
            var alpha = stackalloc byte[16];
            ulong bits;

            UnpackAlpha(array, offset, alpha, out bits);

            for (int i = 0; i < 16; i++)
            {
                if (test(alpha[bits & 7]) == false)
                {
                    return false;
                }

                bits >>= 3;
            }

            return true;
        }

        private static unsafe bool TestAlphaAny(byte[] array, int offset, Func<byte, bool> test)
        {
            var alpha = stackalloc byte[16];
            ulong bits;

            UnpackAlpha(array, offset, alpha, out bits);

            for (int i = 0; i < 16; i++)
            {
                if (test(alpha[bits & 7]) == true)
                {
                    return true;
                }

                bits >>= 3;
            }

            return false;
        }

        #endregion

        #region Content Fields
        public byte[] RawData { get { return this.data; } }
        #endregion

        #region Sub-Types

        public class MipHeader
        {
            public int CommandOffset { get; internal set; }
            public int Offset0 { get; internal set; }
            public int Offset1 { get; internal set; }
            public int Offset2 { get; internal set; }
            public int Offset3 { get; internal set; }
            public int Offset4 { get; internal set; }
        }

        public class RLEInfo
        {
            public const uint Signature = 0x20534444;
            public FourCC fourCC { get; internal set; }
            public NotFourCC notFourCC { get; internal set; }
            public uint size { get { return (18 * 4) + PixelFormat.StructureSize + (5 * 4); } }
            public HeaderFlags headerFlags { get; internal set; }
            public int Height { get; internal set; }
            public int Width { get; internal set; }
            public uint PitchOrLinearSize { get; internal set; }
            public int Depth = 1;
            //public uint mipMapCount { get; internal set; }
            private byte[] Reserved1 = new byte[11 * 4];
            public PixelFormat pixelFormat { get; internal set; }
            public uint surfaceFlags { get; internal set; }
            public uint cubemapFlags { get; internal set; }
            public byte[] reserved2 = new byte[3 * 4];
            public RLEVersion Version { get; internal set; }
            public RLEInfo() { this.pixelFormat = new PixelFormat(); }
            public bool HasSpecular { get { return this.Version == RLEVersion.RLES; } }
            public uint mipCount { get; internal set; }
            public ushort Unknown0E { get; internal set; }

            public RLEInfo(Stream s)
                : this(s, true) { }

            public RLEInfo(Stream s, bool check)
            {
                s.Position = 0;
                BinaryReader r = new BinaryReader(s);
                uint ddstype = r.ReadUInt32();
                if (ddstype == (uint)FourCC.DXT5)
                {
                    this.fourCC = (FourCC)ddstype;
                    this.notFourCC = NotFourCC.None;
                    this.pixelFormat = new PixelFormat(fourCC);
                }
                else if (ddstype == (uint)NotFourCC.L8)
                {
                    this.notFourCC = (NotFourCC)ddstype;
                    this.fourCC = FourCC.None;
                    this.pixelFormat = new PixelFormat(notFourCC);
                }
                else if (check) throw new NotImplementedException(string.Format("Expected format: 0x{0:X8} or 0x{0:X8}, read 0x{1:X8}", (uint)FourCC.DXT5, (uint)NotFourCC.L8, ddstype));
                this.Version = (RLEVersion)r.ReadUInt32();
                this.Width = r.ReadUInt16();
                this.Height = r.ReadUInt16();
                this.mipCount = r.ReadUInt16();
                this.Unknown0E = r.ReadUInt16();
                this.headerFlags = HeaderFlags.Texture;
                if (this.Unknown0E != 0) throw new InvalidDataException(string.Format("Expected 0, read 0x{0:X8}", this.Unknown0E));
            }

            public RLEInfo(RLEInfo other)
            {
                this.fourCC = other.fourCC;
                this.notFourCC = other.notFourCC;
                this.pixelFormat = new PixelFormat(fourCC);
                this.Version = other.Version;
                this.Width = other.Width;
                this.Height = other.Height;
                this.mipCount = other.mipCount;
                this.Unknown0E = other.Unknown0E;
                this.headerFlags = other.headerFlags;
            }
            
            public void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                uint signature = r.ReadUInt32();
                if (signature != Signature) throw new InvalidDataException(string.Format("Expected signature 0x{0:X8}, read 0x{1:X8}", Signature, signature));
                uint size = r.ReadUInt32();
                if (size != this.size) throw new InvalidDataException(string.Format("Expected size: 0x{0:X8}, read 0x{1:X8}", this.size, size));
                this.headerFlags = (HeaderFlags)r.ReadUInt32();
                if ((this.headerFlags & HeaderFlags.Texture) != HeaderFlags.Texture) throw new InvalidDataException(string.Format("Expected 0x{0:X8}, read 0x{1:X8}", (uint)HeaderFlags.Texture, (uint)this.headerFlags));
                this.Height = r.ReadInt32();
                this.Width = r.ReadInt32();
                if (this.Height > ushort.MaxValue || this.Width > ushort.MaxValue) throw new InvalidDataException("Invalid width or length");
                this.PitchOrLinearSize = r.ReadUInt32();
                this.Depth = r.ReadInt32();
                if (this.Depth != 0 && this.Depth != 1) throw new InvalidDataException(string.Format("Expected depth 1 or 0, read 0x{0:X8}", this.Depth));
                this.mipCount = r.ReadUInt32();
                if (this.mipCount > 16) throw new InvalidDataException(string.Format("Expected mini map count less than 16, read 0x{0:X8}", this.mipCount));
                r.ReadBytes(this.Reserved1.Length);
                this.pixelFormat = new PixelFormat();
                this.pixelFormat.Parse(s);
                this.surfaceFlags = r.ReadUInt32();
                this.cubemapFlags = r.ReadUInt32();
                r.ReadBytes(this.reserved2.Length);
                this.fourCC = this.pixelFormat.Fourcc;
                this.notFourCC = this.pixelFormat.pixelFormatFlag == PixelFormatFlags.Luminance ? NotFourCC.L8 : NotFourCC.None;
            }

            public void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(this.size);
                if (this.fourCC != FourCC.None)
                {
                    w.Write(this.mipCount > 1 ? (uint)this.headerFlags | (uint)HeaderFlags.Mipmap | (uint)HeaderFlags.LinearSize : (uint)this.headerFlags | (uint)HeaderFlags.LinearSize);
                }
                else
                {
                    w.Write(this.mipCount > 1 ? (uint)this.headerFlags | (uint)HeaderFlags.Mipmap | (uint)HeaderFlags.Pitch : (uint)this.headerFlags | (uint)HeaderFlags.Pitch);
                    this.PitchOrLinearSize = (uint)((this.pixelFormat.RGBBitCount / 8) * this.Width);
                }
                w.Write(this.Height);
                w.Write(this.Width);
                if (this.PitchOrLinearSize > 0)
                {
                    w.Write(this.PitchOrLinearSize);
                }
                else
                {
                    int blockSize = this.pixelFormat.Fourcc == FourCC.DST1 || this.pixelFormat.Fourcc == FourCC.DXT1 || this.pixelFormat.Fourcc == FourCC.ATI1 ? 8 : 16;
                    w.Write((uint)((Math.Max(1, ((this.Width + 3) / 4)) * blockSize) * (Math.Max(1, (this.Height + 3) / 4)))); //linear size
                }
                w.Write(this.Depth);
                w.Write((uint)this.mipCount);
                w.Write(this.Reserved1);
                this.pixelFormat.UnParse(s);
                w.Write(this.mipCount > 1 ? (uint)DDSCaps.DDSCaps_Complex | (uint)DDSCaps.DDSCaps_Mipmap | (uint)DDSCaps.DDSCaps_Texture : (uint)DDSCaps.DDSCaps_Texture);
                w.Write(this.cubemapFlags);
                w.Write(this.reserved2);
            }
        }

        public enum RLEVersion : uint
        {
            RLE2 = 0x32454C52,
            RLES = 0x53454C52
        }

        public enum HeaderFlags : uint
        {
            Texture = 0x00001007, // DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT 
            Mipmap = 0x00020000, // DDSD_MIPMAPCOUNT
            Volume = 0x00800000, // DDSD_DEPTH
            Pitch = 0x00000008, // DDSD_PITCH
            LinearSize = 0x00080000, // DDSD_LINEARSIZE
        }

        public enum DDSCaps : uint
        {
            DDSCaps_Complex = 0x8,
            DDSCaps_Mipmap = 0x400000,
            DDSCaps_Texture = 0x1000
        }
        
        #endregion
    }

    public class RLEResourceTS4Handler : AResourceHandler
    {
        public RLEResourceTS4Handler()
        {
            this.Add(typeof(RLEResource), new List<string>(new string[] { "0x3453CF95","0xBA856C78", }));
        }
    }
}
