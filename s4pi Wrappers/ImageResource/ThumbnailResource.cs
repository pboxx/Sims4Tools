/***************************************************************************
 *  Copyright (C) 2014, 2016 by the Sims 4 Tools development team          *
 *                                                                         *
 *  Contributors:                                                          *
 *  Keyi Zhang, kz005@bucknell.edu                                         *
 *  CmarNYC                                                                *
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
using System.Diagnostics;

namespace s4pi.ImageResource
{
    public class ThumbnailResource : AResource
    {        
        const int recommendedApiVersion = 1;
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
        public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }

        private byte[] rawData;

        public ThumbnailResource(int APIversion, Stream s) : base(APIversion, s)  { Parse(s); }

        private void Parse(Stream s)
        {
            if (s == null || s.Length ==0)
            {
                this.rawData = new byte[0];
                this.image = new Bitmap(1, 1); // empty image
                return;
            }
            s.Position = 0;
            BinaryReader r = new BinaryReader(s);
            rawData = r.ReadBytes((int)s.Length);
            TransformToPNG();
        }

        protected override Stream UnParse()
        {
            if (this.rawData != null)
                return new MemoryStream(this.rawData);
            else
                return new MemoryStream();
        }

        public Stream ToJFIF()
        {
            Bitmap alpha;
            Bitmap img = ComputeAlpha(this.image, out alpha);
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);
            using (MemoryStream imgStream = new MemoryStream(), alphaStream = new MemoryStream())
            {
                img.Save(imgStream, ImageFormat.Jpeg);
                imgStream.Position = 0;
                BinaryReader r = new BinaryReader(imgStream);
                w.Write(r.ReadBytes(12));

                alpha.Save(alphaStream, ImageFormat.Png);
                alphaStream.Position = 0;
                int length = (int)alphaStream.Length;
                length = (int)((length & 0xFF000000) >> 24) | (int)((length & 0x00FF0000) >> 8) | (int)((length & 0x0000FF00) << 8) | (int)((length & 0x000000FF) << 24);

                w.Write(0x01000001U);
                w.Write(0x00000100U);
                w.Write((ushort)(0xE0FF));
                int newLength = (int)(alphaStream.Length + 10);
                newLength = newLength << 8 | newLength >> 8;
                w.Write((ushort)(newLength));
                w.Write(0x41464C41U);
                w.Write(length);
                w.Write(alphaStream.ToArray());
                w.Write(r.ReadBytes((int)imgStream.Length - 12));
            }
            ms.Position = 0;
            return ms;
        }

        public Stream ToImageStream()
        {
            if (this.image == null) return null;
            MemoryStream ms = new MemoryStream();
            image.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            return ms;
        }

        //private void TransformToPNG()
        //{
        //    using (MemoryStream ms = new MemoryStream(this.rawData))
        //    {
        //        BinaryReader r = new BinaryReader(ms);
        //        Bitmap colorImage;
        //        ms.Position = 0;
        //        using (MemoryStream colorStream = new MemoryStream())
        //        {
        //            colorStream.Write(r.ReadBytes(20), 0, 20);
        //            Bitmap alphaImage = null;
        //            if (r.ReadUInt16() == 0xE0FFU)
        //            {
        //                ushort length = r.ReadUInt16();
        //                length = (ushort)((length & 0xFF00) >> 8 | (length & 0x00FF) << 8);
        //                if (r.ReadUInt32() == 0x41464C41U)
        //                {
        //                    uint lengthAlpha = r.ReadUInt32();
        //                    lengthAlpha = (uint)(((lengthAlpha & 0xFF000000) >> 24) | ((lengthAlpha & 0x00FF0000) >> 8) | ((lengthAlpha & 0x0000FF00) << 8) | ((lengthAlpha & 0x000000FF) << 24));
        //                    using (MemoryStream alphaStream = new MemoryStream(r.ReadBytes((int)(lengthAlpha))))
        //                    {
        //                        alphaImage = new Bitmap(alphaStream);
        //                    }
        //                    ms.Position = 22 + length;
        //                }
        //                else
        //                {
        //                    ms.Position = 20;
        //                }
        //            }
        //            byte[] buffer = new byte[ms.Length];
        //            int len = ms.Read(buffer, 0, buffer.Length);
        //            colorStream.Write(buffer, 0, len);
        //            colorStream.Position = 0;
        //            colorImage = new Bitmap(colorStream);
        //            if (alphaImage != null)
        //            {
        //                if (colorImage.Width != alphaImage.Width || colorImage.Height != alphaImage.Height) throw new InvalidDataException("Not a proper TS4 Thumbnail image");
        //                colorImage = UpdateAlpha(colorImage, alphaImage);
        //            }
        //            ms.Position = 0;
        //            this.image = colorImage;
        //            return;
        //        }
        //    }
        //}

        private void TransformToPNG()
        {
            using (MemoryStream ms = new MemoryStream(this.rawData))
            {
                BinaryReader r = new BinaryReader(ms);
                Bitmap colorImage;
                ms.Position = 0;
                r.ReadBytes(24);
                if (r.ReadUInt32() == 0x41464C41U)
                {
                    int length = r.ReadInt32();
                    length = (int)((length & 0xFF000000) >> 24) | (int)((length & 0x00FF0000) >> 8) | (int)((length & 0x0000FF00) << 8) | (int)((length & 0x000000FF) << 24);
                    using (MemoryStream alphaStream = new MemoryStream(r.ReadBytes(length)))
                    {
                        Bitmap alphaImage = new Bitmap(alphaStream);
                        colorImage = new Bitmap(ms);
                        if (colorImage.Width != alphaImage.Width || colorImage.Height != alphaImage.Height) throw new InvalidDataException("Not a proper TS4 Thumbnail image");
                        colorImage = UpdateAlpha(colorImage, alphaImage);

                        this.image = colorImage;
                        return;
                    }
                }
                ms.Position = 0;
                colorImage = new Bitmap(ms);
                this.image = colorImage;
            }
        }

        private Bitmap image;
        public Bitmap Image {
            get { if (image == null) { return new Bitmap(1, 1); } else { return this.image; }; } 
            set { if (value != null) { this.image = value; this.rawData = null; } } }


        protected internal unsafe Bitmap ComputeAlpha(Bitmap source, out Bitmap alpha)
        {
            int width = source.Width;
            int height = source.Height;
            alpha = new Bitmap(width, height);
            Bitmap img = new Bitmap(width, height);

            BitmapData imgBitmapData = img.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb
            );
            BitmapData alphaBitmapData = alpha.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            BitmapData sourceBitmapData = source.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            ColorRGB* imgStartingPosition = (ColorRGB*)imgBitmapData.Scan0;
            ColorARGB* sourceStartingPosition = (ColorARGB*)sourceBitmapData.Scan0;
            ColorARGB* alphaStartingPosition = (ColorARGB*)alphaBitmapData.Scan0;


            for (int i = 0; i < height; i++)
                for (int j = 0; j < width; j++)
                {
                    ColorARGB* sourcePosition = sourceStartingPosition + j + i * width;
                    ColorARGB* alphaPosition = alphaStartingPosition + j + i * width;
                    ColorRGB* imgPosition = imgStartingPosition + j + i * width;
                    *imgPosition = new ColorRGB(sourcePosition);
                    *alphaPosition = new ColorARGB(255, sourcePosition->A, sourcePosition->A, sourcePosition->A);
                }
            try
            {
                img.UnlockBits(imgBitmapData);
                alpha.UnlockBits(alphaBitmapData);
                source.UnlockBits(sourceBitmapData);
                return img;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return new Bitmap(new MemoryStream(this.rawData));
            }
        }

        protected internal unsafe Bitmap UpdateAlpha(Bitmap source, Bitmap alpha)
        {
            int width = source.Width;
            int height = source.Height;
            Bitmap img = new Bitmap(width, height);

            BitmapData imgBitmapData = img.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            BitmapData sourceBitmapData = source.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            BitmapData alphaBitmapData = alpha.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            ColorARGB* imgStartingPosition = (ColorARGB*)imgBitmapData.Scan0;
            ColorARGB* sourceStartingPosition = (ColorARGB*)sourceBitmapData.Scan0;
            ColorARGB* alphaStartingPosition = (ColorARGB*)alphaBitmapData.Scan0;

            for (int i = 0; i < height; i++)
                for (int j = 0; j < width; j++)
                {
                    ColorARGB* sourcePosition = sourceStartingPosition + j + i * width;
                    ColorARGB* alphaPosition = alphaStartingPosition + j + i * width;
                    ColorARGB* imgPosition = imgStartingPosition + j + i * width;
                    *imgPosition = new ColorARGB(sourcePosition, alphaPosition);
                }

            img.UnlockBits(imgBitmapData);
            source.UnlockBits(sourceBitmapData);
            alpha.UnlockBits(alphaBitmapData);
            return img;
        }

        #region Sub-Type


        protected internal struct ColorRGB
        {

            public byte B;
            public byte G;
            public byte R;

            public unsafe ColorRGB(ColorARGB* original)
            {
                this.R = original->R;
                this.G = original->G;
                this.B = original->B;
            }
        }

        /// <summary>
        /// Basic ARGB color for Write into memory
        /// </summary>
        protected internal struct ColorARGB
        {
            public byte B;
            public byte G;
            public byte R;
            public byte A;
            public ColorARGB(Color color)
            {
                A = color.A;
                R = color.R;
                G = color.G;
                B = color.B;
            }

            public ColorARGB(ColorARGB color)
            {
                A = color.A;
                R = color.R;
                G = color.G;
                B = color.B;
            }

            public unsafe ColorARGB(ColorARGB* original, ColorARGB* alpha)
            {
                this.A = alpha->R;
                this.R = original->R;
                this.G = original->G;
                this.B = original->B;
            }

            public unsafe ColorARGB(ColorARGB* original)
            {
                this.A = 255;
                this.R = original->R;
                this.G = original->G;
                this.B = original->B;
            }

            public unsafe void UpdateAlha(ColorARGB* alpha)
            {
                this.A = alpha->R;
            }

            public ColorARGB(byte a, byte r, byte g, byte b)
            {
                A = a;
                R = r;
                G = g;
                B = b;
            }

            public ColorARGB(int color)
            {
                A = (byte)(color >> 24);
                R = (byte)((color & 0xFF0000) >> 16);
                G = (byte)((color & 0xFF00) >> 8);
                B = (byte)((color & 0XFF));
            }

            public Color ToColor()
            {
                return Color.FromArgb(A, R, G, B);
            }


        }
        #endregion
    }

    /// <summary>
    /// ResourceHandler for ThumbnailResource wrapper
    /// </summary>
    public class ThumbnailResourceHandler : AResourceHandler
    {
        public ThumbnailResourceHandler()
        {
            this.Add(typeof(ThumbnailResource), new List<string>(new string[] { "0x0D338A3A", "0x16CCF748", "0x3BD45407", "0x3C1AF1F2", "0x3C2A8647", "0x4D5B0931", "0x5B282D45", "0x8E71065D", "0xB67673A2", "0xC1ED6D35", "0xCD9DE247", "0xE18CAEE2", "0xE254AE6E", }));
        }
    }
}
