/***************************************************************************
 *  Copyright (C) 2009, 2016 by the Sims 4 Tools development team          *
 *                                                                         *
 *  Contributors:                                                          *
 *  Peter L Jones (pljones@users.sf.net)                                   *
 *  Keyi Zhang                                                             *
 *  Buzzler                                                                *
 *  Cmar                                                                   *
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

// This code is based on Snaitf's analyze

namespace CASPartResource
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using global::CASPartResource.Lists;
    using s4pi.Interfaces;

    public class AnimalCoatResource : AResource
    {
        private const int recommendedApiVersion = 1;

        public override int RecommendedApiVersion
        {
            get { return AnimalCoatResource.recommendedApiVersion; }
        }

        private uint version;
        private AgeGenderFlags ageGender;
        private SpeciesFlags species;
        private float sortPriority;
        private ushort secondarySortIndex;
        private uint propertyID;
        private uint unknown2;
        private uint unknown3;
        private uint unknown4;
        private bool allowRandom;
        private CoatOverlayReferenceList coatLayers;
        private FlagList flagList; //Same as CASP flags

        public AnimalCoatResource(int APIversion, Stream s) : base(APIversion, s)
        {
            if (this.stream == null || this.stream.Length == 0)
            {
                this.stream = this.UnParse();
                this.OnResourceChanged(this, EventArgs.Empty);
            }
            this.stream.Position = 0;
            this.Parse(this.stream);
        }

        #region Data I/O

        private void Parse(Stream s)
        {
            BinaryReader r = new BinaryReader(s);
            this.version = r.ReadUInt32();
            this.ageGender = (AgeGenderFlags)r.ReadUInt32();
            this.species = (SpeciesFlags)r.ReadUInt32();
            this.sortPriority = r.ReadSingle();
            this.secondarySortIndex = r.ReadUInt16();
            this.propertyID = r.ReadUInt32();
            this.unknown2 = r.ReadUInt32();
            this.unknown3 = r.ReadUInt32();
            this.unknown4 = r.ReadUInt32();
            this.allowRandom = r.ReadBoolean();
            this.coatLayers = new CoatOverlayReferenceList(this.OnResourceChanged, s);
            this.flagList = new FlagList(this.OnResourceChanged, s);
        }

        protected override Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);
            w.Write(this.version);
            w.Write((uint)this.ageGender);
            w.Write((uint)this.species);
            w.Write(this.sortPriority);
            w.Write(this.secondarySortIndex);
            w.Write(this.propertyID);
            w.Write(this.unknown2);
            w.Write(this.unknown3);
            w.Write(this.unknown4);
            w.Write(this.allowRandom);
            if (this.coatLayers == null) this.coatLayers = new CoatOverlayReferenceList(null);
            this.coatLayers.UnParse(ms);
            if (this.flagList == null) this.flagList = new FlagList(null);
            this.flagList.UnParse(ms);
            return ms;
        }

        #endregion

        #region Sub Class

        public class CoatOverlayReferenceList : DependentList<CoatOverlayReference>
        {
            public CoatOverlayReferenceList(EventHandler handler) : base(handler) { }
            public CoatOverlayReferenceList(EventHandler handler, Stream s) : this(handler) { Parse(s); }

            #region Data I/O
            protected override void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                uint count = r.ReadUInt32();
                for (int i = 0; i < count; i++) this.Add(new CoatOverlayReference(recommendedApiVersion, handler, s));
            }

            public override void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(this.Count);
                foreach (var entry in this) entry.UnParse(s);
            }

            protected override CoatOverlayReference CreateElement(Stream s) { throw new NotImplementedException(); }
            protected override void WriteElement(Stream s, CoatOverlayReference element) { throw new NotImplementedException(); }
            #endregion
        }
        
        public class CoatOverlayReference : AHandlerElement, IEquatable<CoatOverlayReference>
        {
            public CoatOverlayReference(int apiVersion, EventHandler handler) : base(apiVersion, handler) { }
            public CoatOverlayReference(int apiVersion, EventHandler handler, Stream s) : base(apiVersion, handler) { Parse(s); }
            private ulong instance;
            private uint argb;

            public void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                this.instance = r.ReadUInt64();
                this.argb = r.ReadUInt32();
            }

            public void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(this.instance);
                w.Write(this.argb);
            }

            const int recommendedApiVersion = 1;
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { var res = GetContentFields(requestedApiVersion, this.GetType()); return res; } }
            [ElementPriority(0)]
            public ulong Instance { get { return this.instance; } set { if (!this.instance.Equals(value)) { this.instance = value; } } }
            [ElementPriority(1)]
            public uint Color_ARGB { get { return this.argb; } set { if (!this.argb.Equals(value)) { this.argb = value; } } }
            public string Value { get { return ValueBuilder; } }

            #region IEquatable
            public bool Equals(CoatOverlayReference other)
            {
                return this.instance == other.instance && this.argb == other.argb;
            }
            #endregion
        }
        #endregion

        #region Content Fields

        [ElementPriority(0)]
        public uint Version
        {
            get { return this.version; }
            set
            {
                if (!this.version.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.version = value;
                }
            }
        }

        [ElementPriority(1)]
        public AgeGenderFlags AgeGender
        {
            get { return this.ageGender; }
            set
            {
                if (!this.ageGender.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.ageGender = value;
                }
            }
        }

        [ElementPriority(2)]
        public SpeciesFlags SpeciesEnabled
        {
            get { return this.species; }
            set
            {
                if (!this.species.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.species = value;
                }
            }
        }

        [ElementPriority(3)]
        public float SortPriority
        {
            get { return this.sortPriority; }
            set
            {
                if (!this.sortPriority.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.sortPriority = value;
                }
            }
        }

        [ElementPriority(4)]
        public ushort SecondarySortIndex
        {
            get { return this.secondarySortIndex; }
            set
            {
                if (!this.secondarySortIndex.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.secondarySortIndex = value;
                }
            }
        }

        [ElementPriority(5)]
        public uint PropertyID
        {
            get { return this.propertyID; }
            set
            {
                if (!this.propertyID.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.propertyID = value;
                }
            }
        }

        [ElementPriority(6)]
        public uint Unknown2
        {
            get { return this.unknown2; }
            set
            {
                if (!value.Equals(this.unknown2))
                {
                    this.unknown2 = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(7)]
        public uint Unknown3
        {
            get { return this.unknown3; }
            set
            {
                if (!this.unknown3.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.unknown3 = value;
                }
            }
        }

        [ElementPriority(8)]
        public uint Unknown4
        {
            get { return this.unknown4; }
            set
            {
                if (!this.unknown4.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.unknown4 = value;
                }
            }
        }

        [ElementPriority(9)]
        public bool AllowRandom
        {
            get { return this.allowRandom; }
            set
            {
                if (!this.allowRandom.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.allowRandom = value;
                }
            }
        }

        [ElementPriority(10)]
        public CoatOverlayReferenceList CoatOverlays
        {
            get { return this.coatLayers; }
            set
            {
                if (!this.coatLayers.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.coatLayers = value;
                }
            }
        }

        [ElementPriority(11)]
        public FlagList CoatFlagList
        {
            get { return this.flagList; }
            set
            {
                if (!this.flagList.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.flagList = value;
                }
            }
        }

        public string Value
        {
            get { return this.ValueBuilder; }
        }

        #endregion
    }

    public class AnimalCoatResourceHandler : AResourceHandler
    {
        public AnimalCoatResourceHandler()
        {
            this.Add(typeof(AnimalCoatResource), new List<string>(new string[] { "0xC4DFAE6D", }));
        }
    }
}