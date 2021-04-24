/***************************************************************************
 *  Copyright (C) 2014 by Inge Jones                                       *
 *                                                                         *
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
using System.IO;

using s4pi.Interfaces;

namespace CatalogResource
{
    public class CSPNResource : AResource
    {
        const int kRecommendedApiVersion = 1;
        public override int RecommendedApiVersion { get { return kRecommendedApiVersion; } }

        #region Attributes

        private uint version = 0x07;
        private CatalogCommon commonA;
        private SpnFenMODLEntryList modlEntryList01;
        private SpnFenMODLEntryList modlEntryList02;
        private SpnFenMODLEntryList modlEntryList03;
        private SpnFenMODLEntryList modlEntryList04;
        private float unknown1;
        private float unknown2;
        private byte unknown3;
        private Gp7references refList;
        private uint materialVariant;
        private ulong unkIID01;
        private ColorList colors;

        #endregion

        #region Content Fields

        [ElementPriority(0)]
        public string TypeOfResource
        {
            get { return "CeilingRail(Spandrel)"; }
        }
        [ElementPriority(1)]
        public uint Version
        {
            get { return version; }
            set { if (version != value) { version = value; this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(2)]
        public CatalogCommon CommonBlock
        {
            get { return commonA; }
            set { if (commonA != value) { commonA = new CatalogCommon(kRecommendedApiVersion, this.OnResourceChanged, value); this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(6)]
        public SpnFenMODLEntryList MODLEntryList01
        {
            get { return modlEntryList01; }
            set { if (modlEntryList01 != value) { modlEntryList01 = new SpnFenMODLEntryList(this.OnResourceChanged, value); this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(7)]
        public SpnFenMODLEntryList MODLEntryList02
        {
            get { return modlEntryList02; }
            set { if (modlEntryList02 != value) { modlEntryList02 = new SpnFenMODLEntryList(this.OnResourceChanged, value); this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(8)]
        public SpnFenMODLEntryList MODLEntryList03
        {
            get { return modlEntryList03; }
            set { if (modlEntryList03 != value) { modlEntryList03 = new SpnFenMODLEntryList(this.OnResourceChanged, value); this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(9)]
        public SpnFenMODLEntryList MODLEntryList04
        {
            get { return modlEntryList04; }
            set { if (modlEntryList04 != value) { modlEntryList04 = new SpnFenMODLEntryList(this.OnResourceChanged, value); this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(10)]
        public float Unknown1
        {
            get { return unknown1; }
            set { if (unknown1 != value) { unknown1 = value; this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(11)]
        public float Unknown2
        {
            get { return unknown2; }
            set { if (unknown2 != value) { unknown2 = value; this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(12)]
        public byte Unknown3
        {
            get { return unknown3; }
            set { if (unknown3 != value) { unknown3 = value; this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(13)]
        public Gp7references ReferenceList
        {
            get { return refList; }
            set { if (refList != value) { refList = new Gp7references(kRecommendedApiVersion,this.OnResourceChanged, value); this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(14)]
        public uint MaterialVariant
        {
            get { return materialVariant; }
            set { if (materialVariant != value) { materialVariant = value; this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(15)]
        public ulong SwatchGrouping//UnkIID01
        {
            get { return unkIID01; }
            set { if (unkIID01 != value) { unkIID01 = value; this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(16)]
        public ColorList Colors
        {
            get { return colors; }
            set { if (colors != value) { colors = new ColorList(this.OnResourceChanged, value); this.OnResourceChanged(this, EventArgs.Empty); } }
        }

        public string Value { get { return ValueBuilder; } }

        public override List<string> ContentFields
        {
            get
            {
                List<string> res = base.ContentFields;
                if (this.version < 8)
                {
                    res.Remove("Unknown1");
                    res.Remove("Unknown2");
                    res.Remove("Unknown3");
                }
                return res;
            }
        }

        #endregion

        #region Data I/O

        void Parse(Stream s)
        {
            var br = new BinaryReader(s);
            this.version = br.ReadUInt32();
            this.commonA = new CatalogCommon(kRecommendedApiVersion, this.OnResourceChanged, s);
            this.modlEntryList01 = new SpnFenMODLEntryList(this.OnResourceChanged, s);
            this.modlEntryList02 = new SpnFenMODLEntryList(this.OnResourceChanged, s);
            this.modlEntryList03 = new SpnFenMODLEntryList(this.OnResourceChanged, s);
            this.modlEntryList04 = new SpnFenMODLEntryList(this.OnResourceChanged, s);
            if (version >= 8)
            {
                this.unknown1 = br.ReadSingle();
                this.unknown2 = br.ReadSingle();
                this.unknown3 = br.ReadByte();
            }
            this.refList = new Gp7references(kRecommendedApiVersion, this.OnResourceChanged, s);
            this.materialVariant = br.ReadUInt32();
            this.unkIID01 = br.ReadUInt64();
            this.colors = new ColorList(this.OnResourceChanged, s);
        }

        protected override Stream UnParse()
        {
            var s = new MemoryStream();
            var bw = new BinaryWriter(s);
            bw.Write(this.version);
            if (this.commonA == null) { this.commonA = new CatalogCommon(kRecommendedApiVersion, this.OnResourceChanged); }
            this.commonA.UnParse(s);
            if (modlEntryList01 == null) { modlEntryList01 = new SpnFenMODLEntryList(this.OnResourceChanged); }
            this.modlEntryList01.UnParse(s);
            if (modlEntryList02 == null) { modlEntryList02 = new SpnFenMODLEntryList(this.OnResourceChanged); }
            this.modlEntryList02.UnParse(s);
            if (modlEntryList03 == null) { modlEntryList03 = new SpnFenMODLEntryList(this.OnResourceChanged); }
            this.modlEntryList03.UnParse(s);
            if (modlEntryList04 == null) { modlEntryList04 = new SpnFenMODLEntryList(this.OnResourceChanged); }
            this.modlEntryList04.UnParse(s);
            if (version >= 8)
            {
                bw.Write(this.unknown1);
                bw.Write(this.unknown2);
                bw.Write(this.unknown3);
            }
            if (this.refList == null) { this.refList = new Gp7references(kRecommendedApiVersion, this.OnResourceChanged); }
            this.refList.UnParse(s);
            bw.Write(this.materialVariant);
            bw.Write(this.unkIID01);
            if (this.colors == null) { this.colors = new ColorList(this.OnResourceChanged); }
            this.colors.UnParse(s);
            return s;
        }


        #endregion

        #region Constructors

        public CSPNResource(int APIversion, Stream s)
            : base(APIversion, s)
        {
            if (s == null || s.Length == 0) 
            {
                s = UnParse(); 
                OnResourceChanged(this, EventArgs.Empty); 
            } 
            s.Position = 0; 
            this.Parse(s);
        }

        #endregion

        public class CSPNResourceHandler : AResourceHandler
        {
            public CSPNResourceHandler()
            {
                this.Add(typeof(CSPNResource), new List<string>(new[] { "0x3F0C529A" }));
            }
        }
    }
}