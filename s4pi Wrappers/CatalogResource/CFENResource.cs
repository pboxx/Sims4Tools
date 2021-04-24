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
    public class CFENResource : AResource
    {
        const int kRecommendedApiVersion = 1;
        public override int RecommendedApiVersion { get { return kRecommendedApiVersion; } }

        #region Attributes

        private uint version = 0x0A;
        private CatalogCommon commonA;
        private SpnFenMODLEntryList modlEntryList01;
        private SpnFenMODLEntryList modlEntryList02;
        private SpnFenMODLEntryList modlEntryList03;
        private SpnFenMODLEntryList modlEntryList04;
        private float unknown1;
        private float unknown2;
        private byte unknown3;
        private Gp7references refList;
        private byte unk01;
        private uint unk02;
        private uint materialVariant;
        private ulong swatchGrouping;
        private TGIBlock slot;
        private WhateverList unkList01;
        private WhateverList unkList02;
        private WhateverList unkList03;
        private ColorList colors;
        private uint unk04;
        private byte unk05;

        #endregion

        #region Content Fields

        [ElementPriority(0)]
        public string TypeOfResource
        {
            get { return "Fence"; }
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
        public byte Unk01
        {
            get { return unk01; }
            set { if (unk01 != value) { unk01 = value; this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(15)]
        public uint Unk02
        {
            get { return unk02; }
            set { if (unk02 != value) { unk02 = value; this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(16)]
        public uint MaterialVariant
        {
            get { return materialVariant; }
            set { if (materialVariant != value) { materialVariant = value; this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(17)]
        public ulong SwatchGrouping//UnkIID01
        {
            get { return swatchGrouping; }
            set { if (swatchGrouping != value) { swatchGrouping = value; this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(18)]
        public TGIBlock Slot
        {
            get { return slot; }
            set { if (slot != value) { slot = new TGIBlock(kRecommendedApiVersion,this.OnResourceChanged, TGIBlock.Order.ITG, value.ResourceType,value.ResourceGroup,value.Instance); this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(19)]
        public WhateverList UnkList01
        {
            get { return unkList01; }
            set { if (unkList01 != value) { unkList01 = new WhateverList(this.OnResourceChanged, value); this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(20)]
        public WhateverList UnkList02
        {
            get { return unkList02; }
            set { if (unkList02 != value) { unkList02 = new WhateverList(this.OnResourceChanged, value); this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(21)]
        public WhateverList UnkList03
        {
            get { return unkList03; }
            set { if (unkList03 != value) { unkList03 = new WhateverList(this.OnResourceChanged, value); this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(22)]
        public ColorList Colors
        {
            get { return colors; }
            set { if (colors != value) { colors = new ColorList(this.OnResourceChanged, value); this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(23)]
        public uint Unk04
        {
            get { return unk04; }
            set { if (unk04 != value) { unk04 = value; this.OnResourceChanged(this, EventArgs.Empty); } }
        }
        [ElementPriority(24)]
        public byte Unk05
        {
            get { return unk05; }
            set { if (unk05 != value) { unk05 = value; this.OnResourceChanged(this, EventArgs.Empty); } }
        }

        public string Value { get { return ValueBuilder; } }

        public override List<string> ContentFields
        {
            get
            {
                List<string> res = base.ContentFields;
                if (this.version < 12)
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
            if (this.version >= 12)
            {
                this.unknown1 = br.ReadSingle();
                this.unknown2 = br.ReadSingle();
                this.unknown3 = br.ReadByte();
            }
            this.refList = new Gp7references(kRecommendedApiVersion, this.OnResourceChanged, s);
            this.unk01 = br.ReadByte();
            this.unk02 = br.ReadUInt32();
            this.materialVariant = br.ReadUInt32();
            this.swatchGrouping = br.ReadUInt64();
            this.slot = new TGIBlock(kRecommendedApiVersion, this.OnResourceChanged, TGIBlock.Order.ITG, s);
            this.unkList01 = new WhateverList(this.OnResourceChanged, s);
            this.unkList02 = new WhateverList(this.OnResourceChanged, s);
            this.unkList03 = new WhateverList(this.OnResourceChanged, s);
            this.colors = new ColorList(this.OnResourceChanged, s);
            this.unk04 = br.ReadUInt32();
            if (this.version >= 12) this.unk05 = br.ReadByte();
        }

        protected override Stream UnParse()
        {
            var s = new MemoryStream();
            var bw = new BinaryWriter(s);
            bw.Write(this.version);
            if (this.commonA == null) { commonA = new CatalogCommon(kRecommendedApiVersion, this.OnResourceChanged); }
            this.commonA.UnParse(s);
            if (this.modlEntryList01 == null) { this.modlEntryList01 = new SpnFenMODLEntryList(this.OnResourceChanged); }
            this.modlEntryList01.UnParse(s);
            if (this.modlEntryList02 == null) { this.modlEntryList02 = new SpnFenMODLEntryList(this.OnResourceChanged); }
            this.modlEntryList02.UnParse(s);
            if (this.modlEntryList03 == null) { this.modlEntryList03 = new SpnFenMODLEntryList(this.OnResourceChanged); }
            this.modlEntryList03.UnParse(s);
            if (this.modlEntryList04 == null) { this.modlEntryList04 = new SpnFenMODLEntryList(this.OnResourceChanged); }
            this.modlEntryList04.UnParse(s);
            if (this.refList == null) { this.refList = new Gp7references(kRecommendedApiVersion, this.OnResourceChanged); }
            if (this.version >= 12)
            {
                bw.Write(this.unknown1);
                bw.Write(this.unknown2);
                bw.Write(this.unknown3);
            }
            this.refList.UnParse(s);
            bw.Write(this.unk01);
            bw.Write(this.unk02);
            bw.Write(this.materialVariant);
            bw.Write(this.swatchGrouping);
            if (this.slot == null) { this.slot = new TGIBlock(kRecommendedApiVersion, this.OnResourceChanged, TGIBlock.Order.ITG); }
            this.slot.UnParse(s);
            if (this.unkList01 == null) { unkList01 = new WhateverList(this.OnResourceChanged); }
            this.unkList01.UnParse(s);
            if (this.unkList02 == null) { unkList02 = new WhateverList(this.OnResourceChanged); }
            this.unkList02.UnParse(s);
            if (this.unkList03 == null) { unkList03 = new WhateverList(this.OnResourceChanged); }
            this.unkList03.UnParse(s);
            if (this.colors == null) { this.colors = new ColorList(this.OnResourceChanged); }
            this.colors.UnParse(s);
            bw.Write(this.unk04);
            if (this.version >= 12) bw.Write(this.unk05);
            return s;
        }


        #endregion


        #region Constructors

        public CFENResource(int APIversion, Stream s)
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

        #region subclasses


        public class WhateverList : SimpleList<uint>
        {
            // BYTE count
            // DWORD value
            public WhateverList(EventHandler handler, Stream s)
                : base(handler, s, ReadItem, WriteItem, UInt32.MaxValue, ReadListCount, WriteListCount)
            {
            }
            public WhateverList(EventHandler handler) : base(handler, ReadItem, WriteItem, UInt32.MaxValue, ReadListCount, WriteListCount) { }
            public WhateverList(EventHandler handler, IEnumerable<UInt32> le) : base(handler, le, ReadItem, WriteItem, UInt32.MaxValue, ReadListCount, WriteListCount) { }

            static uint ReadItem(Stream s)
            {
                return new BinaryReader(s).ReadUInt32();
            }
            static void WriteItem(Stream s, uint value)
            {
                new BinaryWriter(s).Write(value);
            }
            static int ReadListCount(Stream s)
            {
                return new BinaryReader(s).ReadUInt16();
            }
            static void WriteListCount(Stream s, int count)
            {
                UInt16 ncount = Convert.ToUInt16(count);
                new BinaryWriter(s).Write(ncount);
            }
        }

        #endregion subclasses


        public class CFENResourceHandler : AResourceHandler
        {
            public CFENResourceHandler()
            {
                this.Add(typeof(CFENResource), new List<string>(new string[] { "0x0418FE2A", }));
            }
        }
    }
}