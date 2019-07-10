/***************************************************************************
 *  Copyright (C) 2014 by Keyi Zhang                                       *
 *  kz005@bucknell.edu                                                     *
 *                                                                         *
 *  This file is part of the Sims 4 Package Interface (s4pi)               *
 *                                                                         *
 *  s4pi is free software: you can redistribute it and/or modify           *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  s3pi is distributed in the hope that it will be useful,                *
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
using System.IO;
using DDSHeader = s4pi.ImageResource.RLEResource.RLEInfo; // need to be improved
using s4pi.Interfaces; 

namespace s4pi.ImageResource
{
    public class DSTResource : AResource, IDisposable
    {
        const Int32 recommendedApiVersion = 1;

        #region AApiVersionedFields
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

        public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
        #endregion
        private DDSHeader header;
        private byte[] data;
        private bool isShuffled;

        public DSTResource(int APIversion, Stream s) : base(APIversion, s) { if (s == null) { OnResourceChanged(this, EventArgs.Empty); } else { Parse(s); } }

        public void Dispose()
        {
            header = null;
            data = null;
        }

        protected void Parse(Stream s)
        {
            if (s == null || s.Length == 0) { this.data = null; return; }
            s.Position = 0;
            this.header = new DDSHeader();
            this.header.Parse(s);
            if (header.pixelFormat.Fourcc != FourCC.DST1 &&
                         header.pixelFormat.Fourcc != FourCC.DST3 &&
                         header.pixelFormat.Fourcc != FourCC.DST5)
            {
                this.isShuffled = false;
            }
            else
            {
                this.isShuffled = true;
            }
            this.Width = header.Width;
            this.Height = header.Height;

            BinaryReader r = new BinaryReader(s);
            s.Position = 0;
            this.data = r.ReadBytes((int)s.Length);
        }

        public Stream ToDDS()
        {
            if (this.data == null) return null;
            if (!this.isShuffled)
            {
                return new MemoryStream(this.data);
            }
            else
            {
                using (MemoryStream ms = new MemoryStream(this.data))
                {
                    MemoryStream result = (MemoryStream)Unshuffle(this.header, ms);
                    result.Position = 0;
                    return result;
                }
            }
        }

        protected override Stream UnParse()
        {
            return this.data == null ? new MemoryStream() : new MemoryStream(this.data);
        }

        public void ImportToDST(Stream input)
        {
            input.Position = 0;
            var header = new DDSHeader();
            header.Parse(input);
            switch (header.pixelFormat.Fourcc)
            {
                case FourCC.DXT1:
                    header.pixelFormat.Fourcc = FourCC.DST1;
                    break;
                case FourCC.DXT3:
                    throw new Exception("No sample yet");
                case FourCC.DXT5:
                    header.pixelFormat.Fourcc = FourCC.DST5;
                    break;
                case FourCC.DST1:
                    this.isShuffled = true;
                    break;
                case FourCC.DST3:
                    this.isShuffled = true;
                    break;
                case FourCC.DST5:
                    this.isShuffled = true;
                    break;
                default:
                    throw new Exception("Not supported format. Read " + header.pixelFormat.Fourcc.ToString());
            }

            if(this.isShuffled)
            {
                input.Position = 0;
                BinaryReader r = new BinaryReader(input);
                this.data = r.ReadBytes((int)input.Length);
                return;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter w = new BinaryWriter(ms);
                w.Write(DDSHeader.Signature);
                header.UnParse(ms);
                this.header = header;
                this.Width = this.header.Width;
                this.Height = this.header.Height;
                Shuffle(this.header, input, ms);
                this.isShuffled = true;
                this.data = ms.ToArray();
            }
            
        }        

        private static void Shuffle(DDSHeader header, Stream input, Stream output)
        {
            BinaryWriter w = new BinaryWriter(output);
            BinaryReader r = new BinaryReader(input);
            input.Position = 128;
            if(header.pixelFormat.Fourcc == FourCC.DST1)
            {
                using(MemoryStream block1 = new MemoryStream(), block2 = new MemoryStream())
                {
                    BinaryWriter w1 = new BinaryWriter(block1), w2 = new BinaryWriter(block2);
                    int count = ((int)input.Length - 128) / 8;

                    for(int i = 0; i < count; i++)
                    {
                        w1.Write(r.ReadBytes(4));
                        w2.Write(r.ReadBytes(4));
                    }

                    w.Write(block1.ToArray());
                    w.Write(block2.ToArray());
                }
            }
            else if(header.pixelFormat.Fourcc == FourCC.DST3)
            {
                throw new Exception("No sample yet");
            }
            else if(header.pixelFormat.Fourcc == FourCC.DST5) // dst5
            {
                using(MemoryStream ms0 = new MemoryStream(), ms1 = new MemoryStream(), ms2 = new MemoryStream(), ms3 = new MemoryStream())
                {
                    BinaryWriter w0 = new BinaryWriter(ms0);
                    BinaryWriter w1 = new BinaryWriter(ms1);
                    BinaryWriter w2 = new BinaryWriter(ms2);
                    BinaryWriter w3 = new BinaryWriter(ms3);

                    int count = (int)(input.Length - 128) / 16;
                    for (int i = 0; i < count; i++)
                    {
                        w0.Write(r.ReadBytes(2));
                        w1.Write(r.ReadBytes(6));
                        w2.Write(r.ReadBytes(4));
                        w3.Write(r.ReadBytes(4));
                    }

                    w.Write(ms0.ToArray());
                    w.Write(ms2.ToArray());
                    w.Write(ms1.ToArray());
                    w.Write(ms3.ToArray());

                }
                
            }

        }

        private Stream Unshuffle(DDSHeader header, Stream s)
        {
            const int dataOffset = 128;
            var dataSize = (int)(s.Length - dataOffset);

            s.Position = dataOffset;
            BinaryReader r = new BinaryReader(s);

            MemoryStream result = new MemoryStream();
            BinaryWriter w = new BinaryWriter(result);
            var temp = r.ReadBytes(dataSize);

            if (header.pixelFormat.Fourcc == FourCC.DST1)
            {
                header.pixelFormat.Fourcc = FourCC.DXT1;
                w.Write(0x20534444); // DDS header
                header.UnParse(result);

                var blockOffset2 = 0;
                var blockOffset3 = blockOffset2 + (dataSize >> 1);

                // probably a better way to do this
                var count = (blockOffset3 - blockOffset2) / 4;

                for (int i = 0; i < count; i++)
                {
                    result.Write(temp, blockOffset2, 4);
                    result.Write(temp, blockOffset3, 4);
                    blockOffset2 += 4;
                    blockOffset3 += 4;
                }
            }
            else if (header.pixelFormat.Fourcc == FourCC.DST3) // DST3
            {
                header.pixelFormat.Fourcc = FourCC.DXT3;
                w.Write(0x20534444);
                header.UnParse(result);

                var blockOffset0 = 0;
                var blockOffset2 = blockOffset0 + (dataSize >> 1);
                var blockOffset3 = blockOffset2 + (dataSize >> 2);

                throw new NotImplementedException("no samples");
            }
            else if (header.pixelFormat.Fourcc ==  FourCC.DST5) // DST5
            {
                header.pixelFormat.Fourcc = FourCC.DXT5;
                w.Write(0x20534444);
                header.UnParse(result);

                var blockOffset0 = 0;
                var blockOffset2 = blockOffset0 + (dataSize >> 3);
                var blockOffset1 = blockOffset2 + (dataSize >> 2);
                var blockOffset3 = blockOffset1 + (6 * dataSize >> 4);

                // probably a better way to do this
                var count = (blockOffset2 - blockOffset0) / 2;

                for (int i = 0; i < count; i++)
                {
                    result.Write(temp, blockOffset0, 2);
                    result.Write(temp, blockOffset1, 6);
                    result.Write(temp, blockOffset2, 4);
                    result.Write(temp, blockOffset3, 4);

                    blockOffset0 += 2;
                    blockOffset1 += 6;
                    blockOffset2 += 4;
                    blockOffset3 += 4;
                }
            }
            result.Position = 0;
            return result;
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool IsShuffled { get { return isShuffled; } }
    }

    public class DSTResourceHandler : AResourceHandler
    {
        public DSTResourceHandler()
        {
            this.Add(typeof(DSTResource), new List<string>(new string[] { "0x00B2D882", "0xB6C8B6A0" }));
        }
    }
}
