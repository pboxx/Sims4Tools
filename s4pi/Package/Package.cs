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
using System.Linq;
using System.Text;
using System.IO;
using s4pi.Interfaces;

namespace s4pi.Package
{
    /// <summary>
    /// Implementation of a package
    /// </summary>
    public class Package : APackage
    {
        static bool checking = Settings.Settings.Checking;

        const Int32 recommendedApiVersion = 1;

        #region AApiVersionedFields
        /// <summary>
        /// The version of the API in use
        /// </summary>
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

        //No ContentFields override as we don't want to make anything more public than APackage provides
        #endregion

        #region APackage
        #region Whole package
        /// <summary>
        /// Tell the package to save itself to wherever it believes it came from
        /// </summary>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override void SavePackage()
        {
            if (checking) if (packageStream == null)
                    throw new InvalidOperationException("Package has no stream to save to");
            if (!packageStream.CanWrite)
                throw new InvalidOperationException("Package is read-only");

            // Lock the header while we save to prevent other processes saving concurrently
            // if it's not a file, it's probably safe not to lock it...
            FileStream fs = packageStream as FileStream;
            string tmpfile = Path.GetTempFileName();
            try
            {
                SaveAs(tmpfile);

                if (fs != null) fs.Lock(0, header.Length);

                BinaryReader r = new BinaryReader(new FileStream(tmpfile, FileMode.Open));
                BinaryWriter w = new BinaryWriter(packageStream);
                packageStream.Position = 0;
                w.Write(r.ReadBytes((int)r.BaseStream.Length));
                packageStream.SetLength(packageStream.Position);
                w.Flush();
                r.Close();
            }
            finally { File.Delete(tmpfile); if (fs != null) fs.Unlock(0, header.Length); }

            packageStream.Position = 0;
            header = (new BinaryReader(packageStream)).ReadBytes(header.Length);
            if (checking) CheckHeader();

            bool wasnull = index == null;
            index = null;
            if (!wasnull) OnResourceIndexInvalidated(this, EventArgs.Empty);
        }

        /// <summary>
        /// Save the package to a given stream
        /// </summary>
        /// <param name="s">Stream to save to</param>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override void SaveAs(Stream s)
        {
            BinaryWriter w = new BinaryWriter(s);
            w.Write(header);

            List<uint> lT = new List<uint>();
            List<uint> lG = new List<uint>();
            List<uint> lIh = new List<uint>();
            this.Index.ForEach(x =>
            {
                if (!lT.Contains(x.ResourceType)) lT.Add(x.ResourceType);
                if (!lG.Contains(x.ResourceGroup)) lG.Add(x.ResourceGroup);
                if (!lIh.Contains((uint)(x.Instance >> 32))) lIh.Add((uint)(x.Instance >> 32));
            });

            uint indexType = (uint)(lIh.Count <= 1 ? 0x04 : 0x00) | (uint)(lG.Count <= 1 ? 0x02 : 0x00) | (uint)(lT.Count <= 1 ? 0x01 : 0x00);


            PackageIndex newIndex = new PackageIndex(indexType);
            foreach (IResourceIndexEntry ie in this.Index)
            {
                if (ie.IsDeleted) continue;

                ResourceIndexEntry newIE = (ie as ResourceIndexEntry).Clone();
                ((List<IResourceIndexEntry>)newIndex).Add(newIE);
                byte[] value = packedChunk(ie as ResourceIndexEntry);

                newIE.Chunkoffset = (uint)s.Position;
                w.Write(value);
                w.Flush();
                //cgm
                //if (value.Length < newIE.Memsize)
                if (ie.Compressed != 0x0000u)
                {
                    // Move to TS4
                    newIE.Compressed = 0x5A42;
                    newIE.Filesize = (uint)value.Length;
                }
                else
                {
                    newIE.Compressed = 0x0000;
                    newIE.Filesize = newIE.Memsize;
                }
            }
            long indexpos = s.Position;
            newIndex.Save(w);
            setIndexcount(w, newIndex.Count);
            setIndexsize(w, newIndex.Size);
            setIndexposition(w, (int)indexpos);
            setUnused4(w, unused4);
            s.Flush();
        }

        /// <summary>
        /// Save the package to a given file
        /// </summary>
        /// <param name="path">File to save to - will be overwritten or created</param>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override void SaveAs(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Create);
            SaveAs(fs);
            fs.Close();
        }

        // Static so cannot be defined on the interface

        /// <summary>
        /// Initialise a new, empty package and return the IPackage reference
        /// </summary>
        /// <param name="APIversion">(unused)</param>
        /// <returns>IPackage reference to an empty package</returns>
        public static new IPackage NewPackage(int APIversion)
        {
            return new Package(APIversion);
        }

        /// <summary>
        /// Initialise a new, empty package and return the IPackage reference
        /// </summary>
        /// <param name="APIversion">(unused)</param>
        /// <param name="major">Major version for the DBPF package.</param>
        /// <returns>IPackage reference to an empty package</returns>
        public static new IPackage NewPackage(int APIversion, int major)
        {
            return new Package(APIversion, major);
        }

        /// <summary>
        /// Open an existing package by filename, read only
        /// </summary>
        /// <param name="APIversion">(unused)</param>
        /// <param name="packagePath">Fully qualified filename of the package</param>
        /// <returns>IPackage reference to an existing package on disk</returns>
        /// <exception cref="ArgumentNullException"><paramref name="packagePath"/> is null.</exception>
        /// <exception cref="FileNotFoundException">The file cannot be found.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="packagePath"/> is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="PathTooLongException">
        /// <paramref name="packagePath"/>, or a component of the file name, exceeds the system-defined maximum length.
        /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="packagePath"/> is an empty string (""), contains only white space, or contains one or more invalid characters.
        /// <br/>-or-<br/>
        /// <paramref name="packagePath"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc. in an NTFS environment.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="packagePath"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc. in a non-NTFS environment.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="InvalidDataException">Thrown if the package header is malformed.</exception>
        public static new IPackage OpenPackage(int APIversion, string packagePath) { return OpenPackage(APIversion, packagePath, false); }
        /// <summary>
        /// Open an existing package by filename, optionally readwrite
        /// </summary>
        /// <param name="APIversion">(unused)</param>
        /// <param name="packagePath">Fully qualified filename of the package</param>
        /// <param name="readwrite">True to indicate read/write access required</param>
        /// <returns>IPackage reference to an existing package on disk</returns>
        /// <exception cref="ArgumentNullException"><paramref name="packagePath"/> is null.</exception>
        /// <exception cref="FileNotFoundException">The file cannot be found.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="packagePath"/> is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="PathTooLongException">
        /// <paramref name="packagePath"/>, or a component of the file name, exceeds the system-defined maximum length.
        /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="packagePath"/> is an empty string (""), contains only white space, or contains one or more invalid characters.
        /// <br/>-or-<br/>
        /// <paramref name="packagePath"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc. in an NTFS environment.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="packagePath"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc. in a non-NTFS environment.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// The access requested is not permitted by the operating system for <paramref name="packagePath"/>,
        /// such as when access is ReadWrite and the file or directory is set for read-only access.
        /// </exception>
        /// <exception cref="InvalidDataException">Thrown if the package header is malformed.</exception>
        public static new IPackage OpenPackage(int APIversion, string packagePath, bool readwrite)
        {
            return new Package(APIversion, new FileStream(packagePath, FileMode.Open, readwrite ? FileAccess.ReadWrite : FileAccess.Read, FileShare.ReadWrite));
        }

        /// <summary>
        /// Releases any internal references associated with the given package
        /// </summary>
        /// <param name="APIversion">(unused)</param>
        /// <param name="pkg">IPackage reference to close</param>
        [Obsolete("Package.ClosePackage is deprecated, please use using statement or Dispose instead")]
        public static new void ClosePackage(int APIversion, IPackage pkg)
        {
            Package p = pkg as Package;
            if (p == null) return;
            if (p.packageStream != null) { try { p.packageStream.Close(); } catch { } p.packageStream = null; }
            p.header = null;
            p.index = null;
        }
        #endregion

        #region Package header
        /// <summary>
        /// Package header: "DBPF" bytes
        /// </summary>
        [ElementPriority(1)]
        public override byte[] Magic { get { byte[] res = new byte[4]; Array.Copy(header, 0, res, 0, res.Length); return res; } }
        /// <summary>
        /// Package header: 0x00000002
        /// </summary>
        [ElementPriority(2)]
        public override int Major { get { return BitConverter.ToInt32(header, 4); } }
        /// <summary>
        /// Package header: 0x00000001
        /// </summary>
        [ElementPriority(3)]
        public override int Minor { get { return BitConverter.ToInt32(header, 8); } }
        /// <summary>
        /// Package header: 0x00000000
        /// </summary>
        [ElementPriority(4)]
        public override int UserVersionMajor { get { byte[] res = new byte[4]; Array.Copy(header, 0xc, res, 0, res.Length); return BitConverter.ToInt32(res, 0); } }
        /// <summary>
        /// Package header: 0x00000000
        /// </summary>
        [ElementPriority(4)]
        public override int UserVersionMinor { get { byte[] res = new byte[4]; Array.Copy(header, 0x10, res, 0, res.Length); return BitConverter.ToInt32(res, 0); } }
        /// <summary>
        /// Package header: unused
        /// </summary>
        [ElementPriority(4)]
        public override int Unused1 { get { byte[] res = new byte[4]; Array.Copy(header, 0x14, res, 0, res.Length); return BitConverter.ToInt32(res, 0); } }
        /// <summary>
        /// Package header: typically, not set
        /// </summary>
        [ElementPriority(4)]
        public override int CreationTime { get { byte[] res = new byte[4]; Array.Copy(header, 0x18, res, 0, res.Length); return BitConverter.ToInt32(res, 0); } }
        /// <summary>
        /// Package header: typically, not set
        /// </summary>
        [ElementPriority(4)]
        public override int UpdatedTime { get { byte[] res = new byte[4]; Array.Copy(header, 0x1c, res, 0, res.Length); return BitConverter.ToInt32(res, 0); } }
        /// <summary>
        /// Package header: unused
        /// </summary>
        [ElementPriority(4)]
        public override int Unused2 { get { byte[] res = new byte[4]; Array.Copy(header, 0x20, res, 0, res.Length); return BitConverter.ToInt32(res, 0); } }
        /// <summary>
        /// Package header: number of entries in the package index
        /// </summary>
        [ElementPriority(5)]
        public override int Indexcount { get { return BitConverter.ToInt32(header, 36); } }
        /// <summary>
        /// Package header: unused
        /// </summary>
        [ElementPriority(6)]
        public override int IndexRecordPositionLow { get { byte[] res = new byte[4]; Array.Copy(header, 40, res, 0, res.Length); return BitConverter.ToInt32(res, 0); } }
        /// <summary>
        /// Package header: index size on disk in bytes
        /// </summary>
        [ElementPriority(7)]
        public override int Indexsize { get { return BitConverter.ToInt32(header, 44); } }
        /// <summary>
        /// Package header: unused
        /// </summary>
        [ElementPriority(8)]
        public override int Unused3 { get { byte[] res = new byte[12]; Array.Copy(header, 48, res, 0, res.Length); return BitConverter.ToInt32(res, 0); } }
        /// <summary>
        /// Package header: always 3?
        /// </summary>
        [ElementPriority(9)]
        public override int Unused4 { get { return BitConverter.ToInt32(header, 60); } }
        /// <summary>
        /// Package header: index position in file
        /// </summary>
        [ElementPriority(10)]
        public override int Indexposition { get { int i = BitConverter.ToInt32(header, 64); return i != 0 ? i : BitConverter.ToInt32(header, 40); } }
        /// <summary>
        /// Package header: unused
        /// </summary>
        [ElementPriority(11)]
        public override byte[] Unused5 { get { byte[] res = new byte[28]; Array.Copy(header, 68, res, 0, res.Length); return res; } }

        /// <summary>
        /// A MemoryStream covering the package header bytes
        /// </summary>
        [ElementPriority(12)]
        public override Stream HeaderStream { get { throw new NotImplementedException(); } }
        #endregion

        #region Package index
        /// <summary>
        /// Package index: the index format in use
        /// </summary>
        [ElementPriority(13)]
        public override UInt32 Indextype { get { return (GetResourceList as PackageIndex).Indextype; } }

        /// <summary>
        /// Package index: the index
        /// </summary>
        [ElementPriority(14)]
        public override List<IResourceIndexEntry> GetResourceList { get { return Index; } }

        static bool FlagMatch(uint flags, IResourceIndexEntry values, IResourceIndexEntry target)
        {
            if (flags == 0) return true;

            for (int i = 0; i < sizeof(uint); i++)
            {
                uint j = (uint)(1 << i);
                if ((flags & j) == 0) continue;
                string f = values.ContentFields[i];
                if (!target.ContentFields.Contains(f)) return false;
                if (!values[f].Equals(target[f])) return false;
            }
            return true;
        }

        static bool NameMatch(string[] names, TypedValue[] values, IResourceIndexEntry target)
        {
            for (int i = 0; i < names.Length; i++) if (!target.ContentFields.Contains(names[i]) || !values[i].Equals(target[names[i]])) return false;
            return true;
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by <paramref name="flags"/> and <paramref name="values"/>,
        /// and returns the first occurrence within the package index./>.
        /// </summary>
        /// <param name="flags">True bits enable matching against numerically equivalent <paramref name="values"/> entry</param>
        /// <param name="values">Fields to compare against</param>
        /// <returns>The first match, if any; otherwise null.</returns>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        [Obsolete("Please use Find(Predicate<IResourceIndexEntry> Match)")]
        public override IResourceIndexEntry Find(uint flags, IResourceIndexEntry values) { return Index.Find(x => !x.IsDeleted && FlagMatch(flags, values, x)); }

        /// <summary>
        /// Searches for an element that matches the conditions defined by <paramref name="names"/> and <paramref name="values"/>,
        /// and returns the first occurrence within the package index./>.
        /// </summary>
        /// <param name="names">Names of fields to compare</param>
        /// <param name="values">Fields to compare against</param>
        /// <returns>The first match, if any; otherwise null.</returns>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        [Obsolete("Please use Find(Predicate<IResourceIndexEntry> Match)")]
        public override IResourceIndexEntry Find(string[] names, TypedValue[] values) { return Index.Find(x => !x.IsDeleted && NameMatch(names, values, x)); }

        /// <summary>
        /// Searches the entire <see cref="IPackage"/>
        /// for the first <see cref="IResourceIndexEntry"/> that matches the conditions defined by
        /// the <c>Predicate&lt;IResourceIndexEntry&gt;</c> <paramref name="Match"/>.
        /// </summary>
        /// <param name="Match"><c>Predicate&lt;IResourceIndexEntry&gt;</c> defining matching conditions.</param>
        /// <returns>The first matching <see cref="IResourceIndexEntry"/>, if any; otherwise null.</returns>
        /// <remarks>Note that entries marked as deleted will not be returned.</remarks>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override IResourceIndexEntry Find(Predicate<IResourceIndexEntry> Match) { return Index.Find(x => !x.IsDeleted && Match(x)); }

        /// <summary>
        /// Searches the entire <see cref="IPackage"/>
        /// for all <see cref="IResourceIndexEntry"/>s that matches the conditions defined by
        /// <paramref name="flags"/> and <paramref name="values"/>.
        /// </summary>
        /// <param name="flags">True bits enable matching against numerically equivalent <paramref name="values"/> entry.</param>
        /// <param name="values">Field values to compare against.</param>
        /// <returns>An <c>IList&lt;IResourceIndexEntry&gt;</c> of zero or more matches.</returns>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        [Obsolete("Please use FindAll(Predicate<IResourceIndexEntry> Match)")]
        public override List<IResourceIndexEntry> FindAll(uint flags, IResourceIndexEntry values) { return Index.FindAll(x => !x.IsDeleted && FlagMatch(flags, values, x)); }

        /// <summary>
        /// Searches the entire <see cref="IPackage"/>
        /// for all <see cref="IResourceIndexEntry"/>s that matches the conditions defined by
        /// <paramref name="names"/> and <paramref name="values"/>.
        /// </summary>
        /// <param name="names">Names of <see cref="IResourceIndexEntry"/> fields to compare.</param>
        /// <param name="values">Field values to compare against.</param>
        /// <returns>An <c>IList&lt;IResourceIndexEntry&gt;</c> of zero or more matches.</returns>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        [Obsolete("Please use FindAll(Predicate<IResourceIndexEntry> Match)")]
        public override List<IResourceIndexEntry> FindAll(string[] names, TypedValue[] values) { return Index.FindAll(x => !x.IsDeleted && NameMatch(names, values, x)); }

        /// <summary>
        /// Searches the entire <see cref="IPackage"/>
        /// for all <see cref="IResourceIndexEntry"/>s that matches the conditions defined by
        /// the <c>Predicate&lt;IResourceIndexEntry&gt;</c> <paramref name="Match"/>.
        /// </summary>
        /// <param name="Match"><c>Predicate&lt;IResourceIndexEntry&gt;</c> defining matching conditions.</param>
        /// <returns>Zero or more matches.</returns>
        /// <remarks>Note that entries marked as deleted will not be returned.</remarks>
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        public override List<IResourceIndexEntry> FindAll(Predicate<IResourceIndexEntry> Match) { return Index.FindAll(x => !x.IsDeleted && Match(x)); }
        #endregion

        #region Package content
        /// <summary>
        /// Add a resource to the package
        /// </summary>
        /// <param name="rk">The resource key</param>
        /// <param name="stream">The stream that contains the resource data</param>
        /// <param name="rejectDups">If true, fail if the resource key already exists</param>
        /// <returns>Null if rejectDups and the resource key exists; else the new IResourceIndexEntry</returns>
        public override IResourceIndexEntry AddResource(IResourceKey rk, Stream stream, bool rejectDups)
        {
            if (rejectDups && Index[rk] != null && !Index[rk].IsDeleted) return null;
            IResourceIndexEntry newrc = Index.Add(rk);
            if (stream != null) (newrc as ResourceIndexEntry).ResourceStream = stream;

            return newrc;
        }
        /// <summary>
        /// Tell the package to replace the data for the resource indexed by <paramref name="rc"/>
        /// with the data from the resource <paramref name="res"/>
        /// </summary>
        /// <param name="rc">Target resource index</param>
        /// <param name="res">Source resource</param>
        public override void ReplaceResource(IResourceIndexEntry rc, IResource res) { (rc as ResourceIndexEntry).ResourceStream = res.Stream; }
        /// <summary>
        /// Tell the package to delete the resource indexed by <paramref name="rc"/>
        /// </summary>
        /// <param name="rc">Target resource index</param>
        public override void DeleteResource(IResourceIndexEntry rc)
        {
            if (!rc.IsDeleted)
                (rc as ResourceIndexEntry).Delete();
        }
        #endregion

        #region IDisposable
        private bool disposed = false;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing">Set to true if the package is going to be released</param>
        protected void Dispose(bool disposing)
        {
            if(!disposed)
            {
                if(disposing)
                {
                    if (packageStream != null) { try { packageStream.Close(); } catch { } packageStream = null; }
                    header = null;
                    index = null;
                }
                disposed = true;
            }
        }

        /// <summary>
        /// Releases all the resources used by <see cref="s4pi.Package"/>
        /// </summary>
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #endregion


        #region Package implementation
        Stream packageStream = null;

        private Package(int requestedVersion, int major = 2)
        {
            if (!majors.Contains(major))
                throw new InvalidDataException("Expected major version(s) '" + string.Join(", ", majors) + "'.  Found '" + major.ToString() + "'.");

            this.requestedApiVersion = requestedVersion;
            using (MemoryStream ms = new MemoryStream(new byte[headerSize]))
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(stringToBytes(magic));
                bw.Write(major);
                bw.Write(minor);
                setIndexsize(bw, (new PackageIndex()).Size);
                setIndexversion(bw);
                setIndexposition(bw, headerSize);
                setUnused4(bw, unused4);
                header = ms.ToArray();
            }
        }

        private Package(int requestedVersion, Stream s)
        {
            this.requestedApiVersion = requestedVersion;
            packageStream = s;
            s.Position = 0;
            header = (new BinaryReader(s)).ReadBytes(header.Length);
            if (checking) CheckHeader();
        }

        private byte[] packedChunk(ResourceIndexEntry ie)
        {
            byte[] chunk = null;
            if (ie.IsDirty)
            {
                Stream res = GetResource(ie);
                BinaryReader r = new BinaryReader(res);

                res.Position = 0;
                chunk = r.ReadBytes((int)ie.Memsize);
                if (checking) if (chunk.Length != (int)ie.Memsize)
                        throw new OverflowException(String.Format("packedChunk, dirty resource - T: 0x{0:X}, G: 0x{1:X}, I: 0x{2:X}: Length expected: 0x{3:X}, read: 0x{4:X}",
                            ie.ResourceType, ie.ResourceGroup, ie.Instance, ie.Memsize, chunk.Length));

                byte[] comp = ie.Compressed != 0 ? Compression.CompressStream(chunk) : chunk;
                //if (comp.Length < chunk.Length)
                    chunk = comp;
                //cgm
                //else
                //    ie.Compressed = 0x0000;
            }
            else
            {
                if (checking) if (packageStream == null)
                        throw new InvalidOperationException(String.Format("Clean resource with undefined \"current package\" - T: 0x{0:X}, G: 0x{1:X}, I: 0x{2:X}",
                            ie.ResourceType, ie.ResourceGroup, ie.Instance));
                packageStream.Position = ie.Chunkoffset;
                chunk = (new BinaryReader(packageStream)).ReadBytes((int)ie.Filesize);
                if (checking) if (chunk.Length != (int)ie.Filesize)
                        throw new OverflowException(String.Format("packedChunk, clean resource - T: 0x{0:X}, G: 0x{1:X}, I: 0x{2:X}: Length expected: 0x{3:X}, read: 0x{4:X}",
                            ie.ResourceType, ie.ResourceGroup, ie.Instance, ie.Filesize, chunk.Length));
            }
            return chunk;
        }
        #endregion

        #region Header implementation
        static byte[] stringToBytes(string s) { byte[] bytes = new byte[s.Length]; int i = 0; foreach (char c in s) bytes[i++] = (byte)c; return bytes; }
        static string bytesToString(byte[] bytes) { string s = ""; foreach (byte b in bytes) s += (char)b; return s; }

        const string magic = "DBPF";
        static int[] majors = { 2 };
        const int minor = 1;
        const int unused4 = 3;
        const int headerSize = 96;

        byte[] header = new byte[96];

        void setIndexcount(BinaryWriter w, int c) { w.BaseStream.Position = 36; w.Write(c); }
        void setIndexsize(BinaryWriter w, int c) { w.BaseStream.Position = 44; w.Write(c); }
        void setIndexversion(BinaryWriter w) { w.BaseStream.Position = 60; w.Write(3); }
        void setIndexposition(BinaryWriter w, int c) { w.BaseStream.Position = 40; w.Write((int)0); w.BaseStream.Position = 64; w.Write(c); }
        void setUnused4(BinaryWriter w, int c) { w.BaseStream.Position = 0x3c; w.Write(c); }

        void CheckHeader()
        {
            if (header.Length != 96)
                throw new InvalidDataException("Hit unexpected end of file.");

            if (bytesToString(Magic) != magic)
                throw new InvalidDataException("Expected magic tag '" + magic + "'.  Found '" + bytesToString(Magic) + "'.");

            if (!majors.Contains(Major))
                throw new InvalidDataException("Expected major version(s) '" + string.Join(", ", majors) + "'.  Found '" + Major.ToString() + "'.");

            if (Minor != minor)
                throw new InvalidDataException("Expected minor version '" + minor + "'.  Found '" + Minor.ToString() + "'.");
        }
        #endregion

        #region Index implementation
        PackageIndex index = null;
        private PackageIndex Index
        {
            get
            {
                if (index == null)
                {
                    index = new PackageIndex(packageStream, Indexposition, Indexsize, Indexcount);
                    OnResourceIndexInvalidated(this, EventArgs.Empty);
                }
                return index;
            }
        }
        #endregion


        // Required by API, not user tools

        /// <summary>
        /// Required internally by s4pi - <b>not</b> for use in user tools.
        /// Please use <c>WrapperDealer.GetResource(int, IPackage, IResourceIndexEntry)</c> instead.
        /// </summary>
        /// <param name="rc">IResourceIndexEntry of resource</param>
        /// <returns>The resource data (uncompressed, if necessary)</returns>
        /// <remarks>Used by WrapperDealer to get the data for a resource.</remarks>
        public override Stream GetResource(IResourceIndexEntry rc)
        {
            ResourceIndexEntry rie = rc as ResourceIndexEntry;
            if (rie == null) return null;
            if (rie.ResourceStream != null) return rie.ResourceStream;

            if (rc.Chunkoffset == 0xffffffff) return null;
            packageStream.Position = rc.Chunkoffset;

            byte[] data = null;
            if (rc.Filesize == 1 && rc.Memsize == 0xFFFFFFFF) return null;//{ data = new byte[0]; }
                //cgm
            //else if (rc.Filesize == rc.Memsize)
            else if (rie.OriginalCompression == 0x0000)
            {
                data = (new BinaryReader(packageStream)).ReadBytes((int)rc.Filesize);
            }
            else
            {
                data = Compression.UncompressStream(packageStream, (int)rc.Filesize, (int)rc.Memsize);
            }

            MemoryStream ms = new MemoryStream();
            ms.Write(data, 0, data.Length);
            ms.Position = 0;
            return ms;
        }

    }
}
