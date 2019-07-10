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

    public class SimPreset : AResource
    {
        private const int recommendedApiVersion = 1;

        public override int RecommendedApiVersion
        {
            get { return SimPreset.recommendedApiVersion; }
        }

        private uint version;
        private AgeGenderFlags ageGender;
        private Species species;
        private uint isCASPreset;
        private float sortPriority;
        private ushort unknown2;
        private uint presetID;
        private uint unknown3;
        private uint[] coatSwatches;
        private ulong coatPatternLink;
        private ulong simOutfitLink;
        private FlagList flagList; //Same as CASP flags

        public SimPreset(int APIversion, Stream s)
            : base(APIversion, s)
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
            this.species = (Species)r.ReadUInt32();
            this.isCASPreset = r.ReadUInt32();
            this.sortPriority = r.ReadSingle();
            this.unknown2 = r.ReadUInt16();
            this.presetID = r.ReadUInt32();
            this.unknown3 = r.ReadUInt32();
            byte count = r.ReadByte();
            this.coatSwatches = new uint[count];
            for (int i = 0; i < count; i++)
            {
                this.coatSwatches[i] = r.ReadUInt32();
            }
            this.coatPatternLink = r.ReadUInt64();
            this.simOutfitLink = r.ReadUInt64();
            this.flagList = new FlagList(this.OnResourceChanged, s);
        }

        protected override Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);
            w.Write(this.version);
            w.Write((uint)this.ageGender);
            w.Write((uint)this.species);
            w.Write(this.isCASPreset);
            w.Write(this.sortPriority);
            w.Write(this.unknown2);
            w.Write(this.presetID);
            w.Write(this.unknown3);
            w.Write((byte)this.coatSwatches.Length);
            for (int i = 0; i < this.coatSwatches.Length; i++)
            {
                w.Write(this.coatSwatches[i]);
            }
            w.Write(this.coatPatternLink);
            w.Write(this.simOutfitLink);
            this.flagList.UnParse(ms);
            return ms;
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
        public Species Species
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
        public uint IsCASPreset
        {
            get { return this.isCASPreset; }
            set
            {
                if (!this.isCASPreset.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.isCASPreset = value;
                }
            }
        }

        [ElementPriority(4)]
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

        [ElementPriority(5)]
        public ushort Unknown2
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

        [ElementPriority(6)]
        public uint PresetID
        {
            get { return this.presetID; }
            set
            {
                if (!this.presetID.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.presetID = value;
                }
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

        [ElementPriority(9)]
        public uint[] CoatColorsARGB
        {
            get { return this.coatSwatches; }
            set
            {
                if (!this.coatSwatches.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.coatSwatches = value;
                }
            }
        }

        [ElementPriority(10)]
        public ulong CoatSwatchLink
        {
            get { return this.coatPatternLink; }
            set
            {
                if (!this.coatPatternLink.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.coatPatternLink = value;
                }
            }
        }

        [ElementPriority(11)]
        public ulong SimOutfitLink
        {
            get { return this.simOutfitLink; }
            set
            {
                if (!this.simOutfitLink.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.simOutfitLink = value;
                }
            }
        }


        [ElementPriority(12)]
        public FlagList FlagsList
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

    public class AnimalBreedResourceHandler : AResourceHandler
    {
        public AnimalBreedResourceHandler()
        {
            this.Add(typeof(SimPreset), new List<string>(new string[] { "0x105205BA", }));
        }
    }
}