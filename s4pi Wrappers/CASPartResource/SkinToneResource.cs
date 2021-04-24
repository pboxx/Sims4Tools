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

    public class SkinToneResource : AResource
    {
        private const int recommendedApiVersion = 1;

        public override int RecommendedApiVersion
        {
            get { return SkinToneResource.recommendedApiVersion; }
        }

        private uint version;
        private ulong rleInstance;      //version < 10
        private SkinSetList skinSets;   //version >= 10
        private OverlayReferenceList overlayList;
        private ushort colorizeSaturation;
        private ushort colorizeHue;
        private uint pass2Opacity;
        private FlagList flagList; //Same as CASP flags
        private float makeupOpacity;
        private SwatchColorList swatchList;
        private float sortOrder;
        private float makeupOpacity2;
        private ulong tuningInstance;
        private SkintoneType skinType;
        private float sliderLow;
        private float sliderHigh;
        private float sliderIncrement;

        public SkinToneResource(int APIversion, Stream s) : base(APIversion, s)
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
            if (this.version >= 10)
            {
                this.skinSets = new SkinSetList(this.OnResourceChanged, s);
            }
            else
            {
                this.rleInstance = reader.ReadUInt64();
            }
            this.overlayList = new OverlayReferenceList(this.OnResourceChanged, s);
            this.colorizeSaturation = reader.ReadUInt16();
            this.colorizeHue = reader.ReadUInt16();
            this.pass2Opacity = reader.ReadUInt32();
            if (this.version > 6)
            {
                this.flagList = new FlagList(this.OnResourceChanged, s);
            }
            else
            {
                this.flagList = FlagList.CreateWithUInt16Flags(this.OnResourceChanged, s, SkinToneResource.recommendedApiVersion);
            }
            if (this.version < 10) this.makeupOpacity = reader.ReadSingle();
            this.swatchList = new SwatchColorList(this.OnResourceChanged, s);
            this.sortOrder = reader.ReadSingle();
            if (this.version < 10) this.makeupOpacity2 = reader.ReadSingle();
            if (this.version >= 8)
            {
                this.tuningInstance = reader.ReadUInt64();
            }
            if (this.version > 10)
            {
                this.skinType = (SkintoneType)reader.ReadUInt16();
                this.sliderLow = reader.ReadSingle();
                this.sliderHigh = reader.ReadSingle();
                this.sliderIncrement = reader.ReadSingle();
            }
        }

        protected override Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);
            w.Write(this.version);
            if (this.version >= 10)
            {
                if (this.skinSets == null) this.skinSets = new SkinSetList(this.OnResourceChanged);
                this.skinSets.UnParse(ms);
            }
            else
            {
                w.Write(this.rleInstance);
            }
            if (this.overlayList == null)
            {
                this.overlayList = new OverlayReferenceList(this.OnResourceChanged);
            }
            this.overlayList.UnParse(ms);
            w.Write(this.colorizeSaturation);
            w.Write(this.colorizeHue);
            w.Write(this.pass2Opacity);

            this.flagList = this.flagList ?? new FlagList(this.OnResourceChanged);
            if (this.version > 6)
            {
                this.flagList.UnParse(ms);
            }
            else
            {
                this.flagList.WriteUInt16Flags(ms);
            }
            if (this.version < 10) w.Write(this.makeupOpacity);
            if (this.swatchList == null)
            {
                this.swatchList = new SwatchColorList(this.OnResourceChanged);
            }
            this.swatchList.UnParse(ms);
            w.Write(this.sortOrder);
            if (this.version < 10) w.Write(this.makeupOpacity2);
            if (this.version >= 8)
            {
                w.Write(this.tuningInstance);
            }
            if (this.version > 10)
            {
                w.Write((ushort)this.skinType);
                w.Write(this.sliderLow);
                w.Write(this.sliderHigh);
                w.Write(this.sliderIncrement);
            }
            return ms;
        }

        #endregion

        #region Sub Class

        public class SkinSet : AHandlerElement, IEquatable<SkinSet>
        {
            private ulong textureReference;
            private ulong overlayReference;
            private float overlayMultiplier;
            private float makeupOpacity;
            private float makeupOpacity2;

            public SkinSet(int apiVersion, EventHandler handler)
                : base(apiVersion, handler)
            {
            }

            public SkinSet(int apiVersion, EventHandler handler, Stream s)
                : base(apiVersion, handler)
            {
                BinaryReader r = new BinaryReader(s);
                this.textureReference = r.ReadUInt64();
                this.overlayReference = r.ReadUInt64();
                this.overlayMultiplier = r.ReadSingle();
                this.makeupOpacity = r.ReadSingle();
                this.makeupOpacity2 = r.ReadSingle();
            }

            public void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(this.textureReference);
                w.Write(this.overlayReference);
                w.Write(this.overlayMultiplier);
                w.Write(this.makeupOpacity);
                w.Write(this.makeupOpacity2);
            }

            #region AHandlerElement Members

            public override int RecommendedApiVersion
            {
                get { return SkinToneResource.recommendedApiVersion; }
            }

            public override List<string> ContentFields
            {
                get { return AApiVersionedFields.GetContentFields(this.requestedApiVersion, this.GetType()); }
            }

            #endregion

            public bool Equals(SkinSet other)
            {
                return this.textureReference == other.textureReference && this.overlayReference == other.overlayReference &&
                    this.overlayMultiplier == other.overlayMultiplier && this.makeupOpacity == other.makeupOpacity && this.makeupOpacity2 == other.makeupOpacity2;
            }

            public string Value
            {
                get { return this.ValueBuilder; }
            }

            [ElementPriority(0)]
            public ulong TextureReference 
            {
                get { return this.textureReference; }
                set { if (this.textureReference != value) { this.OnElementChanged(); this.textureReference = value; } }
            }
            [ElementPriority(1)]
            public ulong OverlayReference
            {
                get { return this.overlayReference; }
                set { if (this.overlayReference != value) { this.OnElementChanged(); this.overlayReference = value; } }
            }
            [ElementPriority(2)]
            public float OverlayMultiplier
            {
                get { return this.overlayMultiplier; }
                set { if (this.overlayMultiplier != value) { this.OnElementChanged(); this.overlayMultiplier = value; } }
            }
            [ElementPriority(3)]
            public float MakeupOpacity
            {
                get { return this.makeupOpacity; }
                set { if (this.makeupOpacity != value) { this.OnElementChanged(); this.makeupOpacity = value; } }
            }
            [ElementPriority(4)]
            public float MakeupOpacity2
            {
                get { return this.makeupOpacity2; }
                set { if (this.makeupOpacity2 != value) { this.OnElementChanged(); this.makeupOpacity2 = value; } }
            }
        }

        public class SkinSetList : DependentList<SkinSet>
        {
            public SkinSetList(EventHandler handler)
                : base(handler)
            {
            }

            public SkinSetList(EventHandler handler, Stream s)
                : base(handler)
            {
                this.Parse(s);
            }

            #region Data I/O

            protected override void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                byte count = r.ReadByte();
                for (int i = 0; i < count; i++)
                {
                    this.Add(new SkinSet(1, this.handler, s));
                }
            }

            public override void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write((byte)this.Count);
                foreach (var reference in this)
                {
                    reference.UnParse(s);
                }
            }

            #endregion

            protected override SkinSet CreateElement(Stream s)
            {
                return new SkinSet(1, this.handler, s);
            }

            protected override void WriteElement(Stream s, SkinSet element)
            {
                element.UnParse(s);
            }
        }

        public class OverlayReference : AHandlerElement, IEquatable<OverlayReference>
        {
            private AgeGenderFlags ageGender;
            private ulong textureReference;

            public OverlayReference(int apiVersion, EventHandler handler) : base(apiVersion, handler)
            {
            }

            public OverlayReference(int apiVersion, EventHandler handler, Stream s) : base(apiVersion, handler)
            {
                BinaryReader r = new BinaryReader(s);
                this.ageGender = (AgeGenderFlags)r.ReadUInt32();
                this.textureReference = r.ReadUInt64();
            }

            public void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write((uint)this.ageGender);
                w.Write(this.textureReference);
            }

            #region AHandlerElement Members

            public override int RecommendedApiVersion
            {
                get { return SkinToneResource.recommendedApiVersion; }
            }

            public override List<string> ContentFields
            {
                get { return AApiVersionedFields.GetContentFields(this.requestedApiVersion, this.GetType()); }
            }

            #endregion

            public bool Equals(OverlayReference other)
            {
                return this.textureReference == other.textureReference && this.ageGender == other.ageGender;
            }

            public string Value
            {
                get { return this.ValueBuilder; }
            }

            [ElementPriority(0)]
            public AgeGenderFlags AgeGender
            {
                get { return this.ageGender; }
                set
                {
                    if (this.ageGender != value)
                    {
                        this.OnElementChanged();
                        this.ageGender = value;
                    }
                }
            }

            [ElementPriority(1)]
            public ulong TextureReference
            {
                get { return this.textureReference; }
                set
                {
                    if (this.textureReference != value)
                    {
                        this.OnElementChanged();
                        this.textureReference = value;
                    }
                }
            }
        }

        public class OverlayReferenceList : DependentList<OverlayReference>
        {
            public OverlayReferenceList(EventHandler handler) : base(handler)
            {
            }

            public OverlayReferenceList(EventHandler handler, Stream s) : base(handler)
            {
                this.Parse(s);
            }

            #region Data I/O

            protected override void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                int count = r.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    this.Add(new OverlayReference(1, this.handler, s));
                }
            }

            public override void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(this.Count);
                foreach (var reference in this)
                {
                    reference.UnParse(s);
                }
            }

            #endregion

            protected override OverlayReference CreateElement(Stream s)
            {
                return new OverlayReference(1, this.handler, s);
            }

            protected override void WriteElement(Stream s, OverlayReference element)
            {
                element.UnParse(s);
            }
        }

        public enum SkintoneType : ushort
        {
            Warm = 1,
            Neutral = 2,
            Cool = 3,
            Miscellaneous = 4
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
        public SkinSetList SkinSets
        {
            get { return this.skinSets; }
            set
            {
                if (!this.skinSets.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.skinSets = value;
                }
            }
        }

        [ElementPriority(1)]
        public ulong TextureInstance
        {
            get { return this.rleInstance; }
            set
            {
                if (!this.rleInstance.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.rleInstance = value;
                }
            }
        }

        [ElementPriority(2)]
        public OverlayReferenceList OverlayList
        {
            get { return this.overlayList; }
            set
            {
                if (!this.overlayList.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.overlayList = value;
                }
            }
        }

        [ElementPriority(3)]
        public ushort ColorizeSaturation
        {
            get { return this.colorizeSaturation; }
            set
            {
                if (!this.colorizeSaturation.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.colorizeSaturation = value;
                }
            }
        }

        [ElementPriority(4)]
        public ushort ColorizeHue
        {
            get { return this.colorizeHue; }
            set
            {
                if (!this.colorizeHue.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.colorizeHue = value;
                }
            }
        }

        [ElementPriority(5)]
        public uint SecondPassOpacity
        {
            get { return this.pass2Opacity; }
            set
            {
                if (!this.pass2Opacity.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.pass2Opacity = value;
                }
            }
        }

        [ElementPriority(6)]
        public FlagList TONEFlagList
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

        [ElementPriority(7)]
        public float MakeupOpacity
        {
            get { return this.makeupOpacity; }
            set
            {
                if (!this.makeupOpacity.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.makeupOpacity = value;
                }
            }
        }

        [ElementPriority(8)]
        public SwatchColorList SwatchList
        {
            get { return this.swatchList; }
            set
            {
                if (!this.swatchList.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.swatchList = value;
                }
            }
        }

        [ElementPriority(9)]
        public float SortOrder
        {
            get { return this.sortOrder; }
            set
            {
                if (!this.sortOrder.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.sortOrder = value;
                }
            }
        }

        [ElementPriority(10)]
        public float MakeupOpacity2
        {
            get { return this.makeupOpacity2; }
            set
            {
                if (!this.makeupOpacity2.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.makeupOpacity2 = value;
                }
            }
        }

        [ElementPriority(11)]
        public ulong TuningDatafileInstance
        {
            get { return this.tuningInstance; }
            set
            {
                if (!this.tuningInstance.Equals(value))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.tuningInstance = value;
                }
            }
        }
        [ElementPriority(12)]
        public SkintoneType SkinType
        {
            get { return this.skinType; }
            set
            {
                if (!this.skinType.Equals(value))
                {
                    this.skinType = value;
                    this.OnResourceChanged(this, EventArgs.Empty);
                }
            }
        }
        [ElementPriority(13)]
        public float SliderLow
        {
            get { return this.sliderLow; }
            set
            {
                if (!this.sliderLow.Equals(value))
                {
                    this.sliderLow = value;
                    this.OnResourceChanged(this, EventArgs.Empty);
                }
            }
        }

        [ElementPriority(14)]
        public float SliderHigh
        {
            get { return this.sliderHigh; }
            set
            {
                if (!this.sliderHigh.Equals(value))
                {
                    this.sliderHigh = value;
                    this.OnResourceChanged(this, EventArgs.Empty);
                }
            }
        }

        [ElementPriority(15)]
        public float SliderIncrement
        {
            get { return this.sliderIncrement; }
            set
            {
                if (!this.sliderIncrement.Equals(value))
                {
                    this.sliderIncrement = value;
                    this.OnResourceChanged(this, EventArgs.Empty);
                }
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
                    res.Remove("TuningDatafileInstance");
                }
                if (this.version >= 10)
                {
                    res.Remove("TextureInstance");
                    res.Remove("MakeupOpacity");
                    res.Remove("MakeupOpacity2");
                }
                else
                {
                    res.Remove("SkinSets");
                }
                if (this.version < 11)
                {
                    res.Remove("SkinType");
                    res.Remove("SliderLow");
                    res.Remove("SliderHigh");
                    res.Remove("SliderIncrement");
                }
                return res;
            }
        }

        #endregion
    }

    public class SkinToneResourceHandler : AResourceHandler
    {
        public SkinToneResourceHandler()
        {
            this.Add(typeof (SkinToneResource), new List<string>(new string[] { "0x0354796A", }));
        }
    }
}