/***************************************************************************
 *  Copyright (C) 2014, 2016 by the Sims 4 Tools development team          *
 *                                                                         *
 *  Contributors:                                                          *
 *  Peter Jones                                                            *
 *  Keyi Zhang                                                             *
 *  CmarNYC                                                                *
 *  Buzzler                                                                *  
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

// This resource is based on EA binary template

using System;
using System.Collections.Generic;
using System.IO;
using s4pi.Interfaces;
using CASPartResource.Lists;


namespace CASPartResource
{
    public class CASPreset : AResource
    {
        private const int recommendedApiVersion = 1;

        public override int RecommendedApiVersion
        {
            get { return CASPreset.recommendedApiVersion; }
        }

        private uint version;
        private AgeGenderFlags ageGender;
        private AgeGenderFlags bodyFrameGender;
        private Species species;
        private SimRegion region;
        private SimSubRegion subRegion;
        private ArchetypeFlags archetype;
        private float displayIndex;
        private uint presetNameKey;
        private uint presetDescKey;
        private SculptList sculpts;
        private ModifierList modifiers;
        private bool isPhysiqueSet;
        private float heavyValue;
        private float fitValue;
        private float leanValue;
        private float bonyValue;
        private bool isPartSet;
        private ulong partSetInstance;
        private uint partSetBodyType;
        private float chanceForRandom;
        private FlagList flagList; 

        public CASPreset(int APIversion, Stream s) : base(APIversion, s)
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
            BinaryReader reader = new BinaryReader(s);
            s.Position = 0;
            this.version = reader.ReadUInt32();
            this.ageGender = (AgeGenderFlags)reader.ReadUInt32();
            if (this.version >= 11)
            {
                this.bodyFrameGender = (AgeGenderFlags)reader.ReadUInt32();
            }
            if (this.version >= 8)
            {
                this.species = (Species)reader.ReadUInt32();
            }
            this.region = (SimRegion)reader.ReadUInt32();
            if (this.version >= 9)
            {
                this.subRegion = (SimSubRegion)reader.ReadUInt32();
            }
            this.archetype = (ArchetypeFlags)reader.ReadUInt32();
            this.displayIndex = reader.ReadSingle();
            this.presetNameKey = reader.ReadUInt32();
            this.presetDescKey = reader.ReadUInt32();
            this.sculpts = new SculptList(OnResourceChanged, s, this.version);
            this.modifiers = new ModifierList(OnResourceChanged, s, this.version);
            this.isPhysiqueSet = reader.ReadBoolean();
            if (this.isPhysiqueSet)
            {
                this.heavyValue = reader.ReadSingle();
                this.fitValue = reader.ReadSingle();
                this.leanValue = reader.ReadSingle();
                this.bonyValue = reader.ReadSingle();
            }
            if (this.version >= 12)
            {
                this.isPartSet = reader.ReadBoolean();
                if (this.isPartSet)
                {
                    this.partSetInstance = reader.ReadUInt64();
                    this.partSetBodyType = reader.ReadUInt32();
                }
            }
            this.chanceForRandom = reader.ReadSingle();
            if (this.version >= 10)
            {
                this.flagList = new FlagList(this.OnResourceChanged, s);
            }
            else
            {
                this.flagList = FlagList.CreateWithUInt16Flags(this.OnResourceChanged, s, CASPreset.recommendedApiVersion);
            }
        }

        protected override Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);
            w.Write(this.version);
            w.Write((uint)this.ageGender);
            if (this.version >= 11)
            {
                w.Write((uint)this.bodyFrameGender);
            }
            if (this.version >= 8)
            {
                w.Write((uint)this.species);
            }
            w.Write((uint)this.region);
            if (this.version >= 9)
            {
                w.Write((uint)this.subRegion);
            }
            w.Write((uint)this.archetype);
            w.Write(this.displayIndex);
            w.Write(this.presetNameKey);
            w.Write(this.presetDescKey);
            if (this.sculpts == null) this.sculpts = new SculptList(OnResourceChanged, this.version);
            this.sculpts.UnParse(ms);
            if (this.modifiers == null) this.modifiers = new ModifierList(OnResourceChanged, this.version);
            this.modifiers.UnParse(ms);
            w.Write(this.isPhysiqueSet);
            if (this.isPhysiqueSet)
            {
                w.Write(this.heavyValue);
                w.Write(this.fitValue);
                w.Write(this.leanValue);
                w.Write(this.bonyValue);
            }
            if (this.version >= 12)
            {
                w.Write(this.isPartSet);
                if (this.isPartSet)
                {
                    w.Write(this.partSetInstance);
                    w.Write(this.partSetBodyType);
                }
            }
            w.Write(this.chanceForRandom);
            if (this.flagList == null) this.flagList = new FlagList(OnResourceChanged);
            if (this.version >= 10)
            {
                this.flagList.UnParse(ms);
            }
            else
            {
                this.flagList.WriteUInt16Flags(ms);
            }
            return ms;
        }

        #endregion

        #region Sub Class

        public class Sculpt : AHandlerElement, IEquatable<Sculpt>
        {
            private uint parentVersion;
            private ulong instance;
            private uint region;

            public Sculpt(int apiVersion, EventHandler handler) : base(apiVersion, handler)
            {
            }

            public Sculpt(int apiVersion, EventHandler handler, Stream s, uint version)
                : base(apiVersion, handler)
            {
                BinaryReader r = new BinaryReader(s);
                this.parentVersion = version;
                this.instance = r.ReadUInt64();
                if (version < 9)
                {
                    this.region = r.ReadUInt32();
                }
            }

            public void UnParse(Stream s, uint version)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(this.instance);
                if (version < 9)
                {
                    w.Write(this.region);
                }
            }

            #region AHandlerElement Members

            public override int RecommendedApiVersion
            {
                get { return CASPreset.recommendedApiVersion; }
            }

            public string Value
            {
                get { return this.ValueBuilder; }
            }

            public override List<string> ContentFields
            {
                get
                {
                    var res = GetContentFields(this.requestedApiVersion, this.GetType());
                    if (this.parentVersion >= 9)
                    {
                        res.Remove("Region");
                    }
                    return res;
                }
            }

            #endregion

            public bool Equals(Sculpt other)
            {
                return this.instance == other.instance && this.region == other.region;
            }

            [ElementPriority(0)]
            public ulong Instance
            {
                get { return this.instance; }
                set
                {
                    if (this.instance != value)
                    {
                        this.OnElementChanged();
                        this.instance = value;
                    }
                }
            }

            [ElementPriority(1)]
            public uint Region
            {
                get { return this.region; }
                set
                {
                    if (this.region != value)
                    {
                        this.OnElementChanged();
                        this.region = value;
                    }
                }
            }
        }

        public class SculptList : DependentList<Sculpt>
        {
            private uint parentVersion;

            public SculptList(EventHandler handler) : base(handler)
            {
            }

            public SculptList(EventHandler handler, uint version)
                : base(handler)
            {
                this.parentVersion = version;
            }

            public SculptList(EventHandler handler, Stream s, uint version)
                : base(handler)
            {
                this.parentVersion = version;
                this.Parse(s);
            }

            #region Data I/O

            protected override void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                int count = r.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    this.Add(new Sculpt(1, this.handler, s, parentVersion));
                }
            }

            public override void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(this.Count);
                foreach (var reference in this)
                {
                    reference.UnParse(s, parentVersion);
                }
            }

            #endregion

            protected override Sculpt CreateElement(Stream s)
            {
                return new Sculpt(1, this.handler, s, parentVersion);
            }

            protected override void WriteElement(Stream s, Sculpt element)
            {
                element.UnParse(s, parentVersion);
            }
        }

        public class Modifier : AHandlerElement, IEquatable<Modifier>
        {
            private uint parentVersion;
            private ulong instance;
            private float weight;
            private uint region;

            public Modifier(int apiVersion, EventHandler handler)
                : base(apiVersion, handler)
            {
            }

            public Modifier(int apiVersion, EventHandler handler, Stream s, uint version)
                : base(apiVersion, handler)
            {
                BinaryReader r = new BinaryReader(s);
                this.parentVersion = version;
                this.instance = r.ReadUInt64();
                this.weight = r.ReadSingle();
                if (version < 9)
                {
                    this.region = r.ReadUInt32();
                }
            }

            public void UnParse(Stream s, uint version)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(this.instance);
                w.Write(this.weight);
                if (version < 9)
                {
                    w.Write(this.region);
                }
            }

            #region AHandlerElement Members

            public override int RecommendedApiVersion
            {
                get { return CASPreset.recommendedApiVersion; }
            }

            public string Value
            {
                get { return this.ValueBuilder; }
            }

            public override List<string> ContentFields
            {
                get
                {
                    var res = GetContentFields(this.requestedApiVersion, this.GetType());
                    if (this.parentVersion >= 9)
                    {
                        res.Remove("Region");
                    }
                    return res;
                }
            }

            #endregion

            public bool Equals(Modifier other)
            {
                return this.instance == other.instance && this.weight == other.weight && this.region == other.region;
            }

            [ElementPriority(0)]
            public ulong Instance
            {
                get { return this.instance; }
                set
                {
                    if (this.instance != value)
                    {
                        this.OnElementChanged();
                        this.instance = value;
                    }
                }
            }

            [ElementPriority(1)]
            public float Weight
            {
                get { return this.weight; }
                set
                {
                    if (this.weight != value)
                    {
                        this.OnElementChanged();
                        this.weight = value;
                    }
                }
            }

            [ElementPriority(2)]
            public uint Region
            {
                get { return this.region; }
                set
                {
                    if (this.region != value)
                    {
                        this.OnElementChanged();
                        this.region = value;
                    }
                }
            }
        }

        public class ModifierList : DependentList<Modifier>
        {
            private uint parentVersion;

            public ModifierList(EventHandler handler)
                : base(handler)
            {
            }

            public ModifierList(EventHandler handler, uint version)
                : base(handler)
            {
                this.parentVersion = version;
            }

            public ModifierList(EventHandler handler, Stream s, uint version)
                : base(handler)
            {
                this.parentVersion = version;
                this.Parse(s);
            }

            #region Data I/O

            protected override void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                int count = r.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    this.Add(new Modifier(1, this.handler, s, parentVersion));
                }
            }

            public override void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(this.Count);
                foreach (var reference in this)
                {
                    reference.UnParse(s, parentVersion);
                }
            }

            #endregion

            protected override Modifier CreateElement(Stream s)
            {
                return new Modifier(1, this.handler, s, parentVersion);
            }

            protected override void WriteElement(Stream s, Modifier element)
            {
                element.UnParse(s, parentVersion);
            }
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
        public AgeGenderFlags BodyFrameGender
        {
            get { return this.bodyFrameGender; }
            set
            {
                if (!this.bodyFrameGender.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.bodyFrameGender = value;
                }
            }
        }

        [ElementPriority(3)]
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

        [ElementPriority(4)]
        public SimRegion Region
        {
            get { return this.region; }
            set
            {
                if (!this.region.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.region = value;
                }
            }
        }

        [ElementPriority(5)]
        public SimSubRegion SubRegion
        {
            get { return this.subRegion; }
            set
            {
                if (!this.subRegion.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.subRegion = value;
                }
            }
        }

        [ElementPriority(6)]
        public ArchetypeFlags Archetype
        {
            get { return this.archetype; }
            set
            {
                if (!this.archetype.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.archetype = value;
                }
            }
        }

        [ElementPriority(7)]
        public float DisplayIndex
        {
            get { return this.displayIndex; }
            set
            {
                if (!this.displayIndex.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.displayIndex = value;
                }
            }
        }

        [ElementPriority(8)]
        public uint PresetNameKey
        {
            get { return this.presetNameKey; }
            set
            {
                if (!this.presetNameKey.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.presetNameKey = value;
                }
            }
        }

        [ElementPriority(9)]
        public uint PresetDescriptionKey
        {
            get { return this.presetDescKey; }
            set
            {
                if (!this.presetDescKey.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.presetDescKey = value;
                }
            }
        }

        [ElementPriority(10)]
        public SculptList Sculpts
        {
            get { return this.sculpts; }
            set
            {
                if (!this.sculpts.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.sculpts = value;
                }
            }
        }

        [ElementPriority(11)]
        public ModifierList Modifiers
        {
            get { return this.modifiers; }
            set
            {
                if (!this.modifiers.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.modifiers = value;
                }
            }
        }

        [ElementPriority(12)]
        public bool IsPhysiqueSet
        {
            get { return this.isPhysiqueSet; }
            set
            {
                if (!this.isPhysiqueSet.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.isPhysiqueSet = value;
                }
            }
        }

        [ElementPriority(13)]
        public float HeavyValue
        {
            get { return this.heavyValue; }
            set
            {
                if (!this.heavyValue.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.heavyValue = value;
                }
            }
        }

        [ElementPriority(14)]
        public float FitValue
        {
            get { return this.fitValue; }
            set
            {
                if (!this.fitValue.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.fitValue = value;
                }
            }
        }

        [ElementPriority(15)]
        public float LeanValue
        {
            get { return this.leanValue; }
            set
            {
                if (!this.leanValue.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.leanValue = value;
                }
            }
        }

        [ElementPriority(16)]
        public float BonyValue
        {
            get { return this.bonyValue; }
            set
            {
                if (!this.bonyValue.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.bonyValue = value;
                }
            }
        }

        [ElementPriority(17)]
        public Boolean IsPartSet
        {
            get { return this.isPartSet; }
            set
            {
                if (!this.isPartSet.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.isPartSet = value;
                }
            }
        }

        [ElementPriority(18)]
        public UInt64 PartSetInstance
        {
            get { return this.partSetInstance; }
            set
            {
                if (!this.partSetInstance.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.partSetInstance = value;
                }
            }
        }

        [ElementPriority(19)]
        public UInt32 PartSetBodyType
        {
            get { return this.partSetBodyType; }
            set
            {
                if (!this.partSetBodyType.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.partSetBodyType = value;
                }
            }
        }

        [ElementPriority(20)]
        public float ChanceForRandom
        {
            get { return this.chanceForRandom; }
            set
            {
                if (!this.chanceForRandom.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.chanceForRandom = value;
                }
            }
        }

        [ElementPriority(21)]
        public FlagList PresetFlagList
        {
            get { return this.flagList; }
            set
            {
                if (!value.Equals(this.flagList))
                {
                    this.flagList = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        public string Value
        {
            get { return this.ValueBuilder; }
        }

        public override List<string> ContentFields
        {
            get
            {
                var res = base.ContentFields;
                if (this.version < 8)
                {
                    res.Remove("ReservedSetTo1");
                }
                if (this.version < 9)
                {
                    res.Remove("ReservedSetTo0");
                }
                if (!this.isPhysiqueSet)
                {
                    res.Remove("HeavyValue");
                    res.Remove("FitValue");
                    res.Remove("LeanValue");
                    res.Remove("BonyValue");
                }
                if (this.version < 11)
                {
                    res.Remove("BodyFrameGender");
                }
                if (this.version < 12)
                {
                    res.Remove("Unknown");
                }
                return res;
            }
        }

        #endregion
    }

    public class CASPresetResourceHandler : AResourceHandler
    {
        public CASPresetResourceHandler()
        {
            this.Add(typeof(CASPreset), new List<string>(new string[] { "0xEAA32ADD", }));
        }
    }
}
