/***************************************************************************
 *  Copyright (C) 2009, 2010 by Peter L Jones                              *
 *  pljones@users.sf.net                                                   *
 *                                                                         *
 *  This file is part of the Sims 3 Package Interface (s3pi)               *
 *                                                                         *
 *  s3pi is free software: you can redistribute it and/or modify           *
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
 *  along with s3pi.  If not, see <http://www.gnu.org/licenses/>.          *
 ***************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace s4pi.Package
{
    /// <summary>
    /// Internal -- used by Package to handle compression routines
    /// </summary>
    internal static class Compression
    {
        static bool checking = Settings.Settings.Checking;

        public static byte[] UncompressStream(Stream stream, int filesize, int memsize)
        {
            if (memsize < 2) return new byte[0];
            BinaryReader r = new BinaryReader(stream);
            long end = stream.Position + filesize;

            byte[] header = r.ReadBytes(2);

            if (checking) if (header.Length != 2)
                    throw new InvalidDataException("Hit unexpected end of file at " + stream.Position);

            bool useDEFLATE = true;
            byte[] uncompressedData = null;

            if (header[0] == 0x78)
            {
                useDEFLATE = true;
            }
            else if (header[1] == 0xFB)
            {
                useDEFLATE = false;
            }
            else
            {
                throw new InvalidDataException("Unrecognized compression format, header: 0x" + header[0].ToString("X2") + header[1].ToString("X2"));
            }

            if (useDEFLATE)
            {
                byte[] data = new byte[filesize];
                stream.Position -= 2; // go back to header
                stream.Read(data, 0, filesize);
                using (MemoryStream source = new MemoryStream(data))
                {
                    using (InflaterInputStream decomp = new InflaterInputStream(source))
                    {
                        uncompressedData = new byte[memsize];
                        decomp.Read(uncompressedData, 0, memsize);
                    }
                }
            }
            else
            {
                uncompressedData = OldDecompress(stream, header[0]);
            }

            long realsize = uncompressedData.Length;

            if (checking) if (realsize != memsize)
                    throw new InvalidDataException(String.Format(
                        "Resource data indicates size does not match index at 0x{0}.  Read 0x{1}.  Expected 0x{2}.",
                        stream.Position.ToString("X8"), realsize.ToString("X8"), memsize.ToString("X8")));

            return uncompressedData;

        }

        internal static byte[] OldDecompress(Stream compressed, byte compressionType)
        {
            BinaryReader r = new BinaryReader(compressed);

            bool type = compressionType != 0x80;

            byte[] sizeArray = new byte[4];


            for (int i = type ? 2 : 3; i >= 0; i--)
                sizeArray[i] = r.ReadByte();

            byte[] Data = new byte[BitConverter.ToInt32(sizeArray, 0)];

            int position = 0;
            while (position < Data.Length)
            {
                byte byte0 = r.ReadByte();
                if (byte0 <= 0x7F)
                {
                    // Read info
                    byte byte1 = r.ReadByte();
                    int numPlainText = byte0 & 0x03;
                    int numToCopy = ((byte0 & 0x1C) >> 2) + 3;
                    int copyOffest = ((byte0 & 0x60) << 3) + byte1 + 1;

                    CopyPlainText(ref r, ref Data, numPlainText, ref position);

                    CopyCompressedText(ref r, ref Data, numToCopy, ref position, copyOffest);

                }
                else if (byte0 <= 0XBF && byte0 > 0x7F)
                {
                    // Read info
                    byte byte1 = r.ReadByte();
                    byte byte2 = r.ReadByte();
                    int numPlainText = ((byte1 & 0xC0) >> 6) & 0x03;
                    int numToCopy = (byte0 & 0x3F) + 4;
                    int copyOffest = ((byte1 & 0x3F) << 8) + byte2 + 1;

                    CopyPlainText(ref r, ref Data, numPlainText, ref position);

                    CopyCompressedText(ref r, ref Data, numToCopy, ref position, copyOffest);
                }
                else if (byte0 <= 0xDF && byte0 > 0xBF)
                {
                    // Read info
                    byte byte1 = r.ReadByte();
                    byte byte2 = r.ReadByte();
                    byte byte3 = r.ReadByte();
                    int numPlainText = byte0 & 0x03;
                    int numToCopy = ((byte0 & 0x0C) << 6) + byte3 + 5;
                    int copyOffest = ((byte0 & 0x10) << 12) + (byte1 << 8) + byte2 + 1;

                    CopyPlainText(ref r, ref Data, numPlainText, ref position);

                    CopyCompressedText(ref r, ref Data, numToCopy, ref position, copyOffest);
                }
                else if (byte0 <= 0xFB && byte0 > 0xDF)
                {
                    // Read info
                    int numPlainText = ((byte0 & 0x1F) << 2) + 4;

                    CopyPlainText(ref r, ref Data, numPlainText, ref position);

                }
                else if (byte0 <= 0xFF && byte0 > 0xFB)
                {
                    // Read info
                    int numPlainText = (byte0 & 0x03);

                    CopyPlainText(ref r, ref Data, numPlainText, ref position);
                }
            }

            return Data;
        }

        static void CopyPlainText(ref BinaryReader r, ref byte[] Data, int numPlainText, ref int position)
        {
            // Copy data one at a time
            for (int i = 0; i < numPlainText; position++, i++)
            {
                Data[position] = r.ReadByte();
            }
        }

        static void CopyCompressedText(ref BinaryReader r, ref byte[] Data, int numToCopy, ref int position, int copyOffest)
        {
            int currentPosition = position;
            // Copy data one at a time
            for (int i = 0; i < numToCopy; i++, position++)
            {
                Data[position] = Data[currentPosition - copyOffest + i];
            }
        }

        public static byte[] CompressStream(byte[] data)
        {

            byte[] result;
            using (MemoryStream ms = new MemoryStream(data))
            {
                bool smaller = _compress(ms, out result);
                //cgm
                //return smaller ? result : data;
                return result;
            }
        }

        internal static bool _compress(Stream uncompressed, out byte[] res)
        {
            using (MemoryStream result = new MemoryStream())
            {
                if(uncompressed.Length == 0)
                {
                    res = null;
                    return false;
                }
                BinaryWriter w = new BinaryWriter(result);

                using (DeflaterOutputStream ds = new DeflaterOutputStream(result) { IsStreamOwner = false })
                {
                    uncompressed.CopyTo(ds);
                }

                if (result.Length < uncompressed.Length)
                {

                    res = result.ToArray();
                    return true;
                }
                else
                {
                    //cgm
                    //res = null;
                    res = result.ToArray();
                    return false;
                }

                
            }
        }
    }
}
