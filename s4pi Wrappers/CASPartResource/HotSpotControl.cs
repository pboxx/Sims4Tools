/***************************************************************************
 *  Copyright (C) 2014 by the s4pe team                                    *
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

using System.IO;
using s4pi.Interfaces;
using s4pi.Settings;
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Linq;
using System.Collections;

using CASPartResource.Lists;

namespace CASPartResource
{
    public class HotSpotControl : AResource
    {
        const int recommendedApiVersion = 1;
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
        static bool checking = s4pi.Settings.Settings.Checking;

        private uint version;
        private AgeGenderFlags ageGender;
        private Species species;
        private HotSpotLevel zoomLevel;
        private byte sliderID;
        private SliderCursor cursor;
        private SimRegion region;
        private SimSubRegion subRegion;
        private RestrictToGender restrictToGender;
        private ulong textureReference;
        private uint unknown4;
        private SliderList sliderDescriptions;

        public HotSpotControl(int APIversion, Stream s) : base(APIversion, s) { if (stream == null || stream.Length == 0) { stream = UnParse(); OnResourceChanged(this, EventArgs.Empty); } stream.Position = 0; Parse(stream); }

        #region Data I/O
        void Parse(Stream s)
        {
            BinaryReader r = new BinaryReader(s);
            s.Position = 0;
            this.version = r.ReadUInt32();
            this.ageGender = (AgeGenderFlags)r.ReadUInt32();
            if (version >= 0x0E) this.species = (Species)r.ReadUInt32();
            this.zoomLevel = (HotSpotLevel)r.ReadByte();
            this.sliderID = r.ReadByte();
            this.cursor = (SliderCursor)r.ReadByte();
            this.region = (SimRegion)r.ReadUInt32();
            if (version >= 0x0E)
            {
                this.subRegion = (SimSubRegion)r.ReadUInt32();
                this.restrictToGender = (RestrictToGender)r.ReadByte();
            }
            this.textureReference = r.ReadUInt64();
            this.unknown4 = r.ReadUInt32();
            byte sliderCount = r.ReadByte();
            this.sliderDescriptions = new SliderList(OnResourceChanged);
            for (int i = 0; i < sliderCount; i++)
            {
                this.sliderDescriptions.Add(new SliderDesc(recommendedApiVersion, OnResourceChanged, s));
            }
        }

        protected override Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);
            w.Write(this.version);
            w.Write((uint)this.ageGender);
            if (version >= 0x0E) w.Write((uint)this.species);
            w.Write((byte)this.zoomLevel);
            w.Write(this.sliderID);
            w.Write((byte)this.cursor);
            w.Write((uint)this.region);
            if (version >= 0x0E)
            {
                w.Write((uint)this.subRegion);
                w.Write((byte)this.restrictToGender);
            }
            w.Write(this.textureReference);
            w.Write(this.unknown4);
            w.Write((byte)this.sliderDescriptions.Count);
            if (sliderDescriptions == null) sliderDescriptions = new SliderList(OnResourceChanged);
            for (int i = 0; i < this.sliderDescriptions.Count; i++)
            {
                this.sliderDescriptions[i].UnParse(ms);
            }
            return ms;
        }
        #endregion

        #region Content Fields
        [ElementPriority(0)]
        public uint Version { get { return this.version; } set { if (!this.version.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.version = value; } } }
        [ElementPriority(1)]
        public AgeGenderFlags AgeFrame { get { return this.ageGender; } set { if (!this.ageGender.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.ageGender = value; } } }
        [ElementPriority(2)]
        public Species Species { get { return this.species; } set { if (!this.species.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.species = value; } } }
        [ElementPriority(3)]
        public HotSpotLevel DetailLevel { get { return this.zoomLevel; } set { if (!this.zoomLevel.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.zoomLevel = value; } } }
        [ElementPriority(4)]
        public byte SliderID { get { return this.sliderID; } set { if (!this.sliderID.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.sliderID = value; } } }
        [ElementPriority(5)]
        public SliderCursor CursorSymbol { get { return this.cursor; } set { if (!this.cursor.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.cursor = value; } } }
        [ElementPriority(6)]
        public SimRegion Region { get { return this.region; } set { if (!this.region.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.region = value; } } }
        [ElementPriority(7)]
        public SimSubRegion SubRegion { get { return this.subRegion; } set { if (!this.subRegion.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.subRegion = value; } } }
        [ElementPriority(8)]
        public RestrictToGender RestricttoGender { get { return this.restrictToGender; } set { if (!this.restrictToGender.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.restrictToGender = value; } } }
        [ElementPriority(9)]
        public ulong TextureReference { get { return this.textureReference; } set { if (!this.textureReference.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.textureReference = value; } } }
        [ElementPriority(10)]
        public uint Unknown4 { get { return this.unknown4; } set { if (!this.unknown4.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.unknown4 = value; } } }
        [ElementPriority(11)]
        public SliderList SliderDescription { get { return this.sliderDescriptions; } set { if (!this.sliderDescriptions.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.sliderDescriptions = value; } } }
        
        public string Value { get { return ValueBuilder; } }

        public override List<string> ContentFields
        {
            get
            {
                var res = base.ContentFields;
                if (this.version < 0x0E)
                {
                    res.Remove("SubRegion");
                    res.Remove("FrameGender");
                }
                return res;
            }
        }

        #endregion

        #region Sub-Types
        public class SliderDesc : AHandlerElement, IEquatable<SliderDesc>
        {
            const int recommendedApiVersion = 1;

            #region Attributes
            private ViewAngle angle;
            private bool flip;
            private float[] unknown7;       //2 floats
            private ulong simModifierLeft;      //Instance IDs
            private ulong simModifierRight;
            private ulong simModifierUp;
            private ulong simModifierDown;
            #endregion

            public SliderDesc(int apiVersion, EventHandler handler) : base(apiVersion, handler) { }
            public SliderDesc(int apiVersion, EventHandler handler, Stream s) : base(apiVersion, handler) { Parse(s); }
            public SliderDesc(int apiVersion, EventHandler handler, SliderDesc basis)
                : this(apiVersion, handler, basis.angle, basis.flip, basis.unknown7, 
                basis.simModifierLeft, basis.simModifierRight, basis.simModifierUp, basis.simModifierDown) { }
            public SliderDesc(int apiVersion, EventHandler handler, ViewAngle angle, bool flip, float[] unknown7, 
                ulong simModifierLeft, ulong simModifierRight, ulong simModifierUp, ulong simModifierDown)
                : base(apiVersion, handler)
            {
                this.angle = angle;
                this.flip = flip;
                this.unknown7 = new float[unknown7.Length];
                for (int i = 0; i < this.unknown7.Length; i++) this.unknown7[i] = unknown7[i];
                this.simModifierLeft = simModifierLeft;
                this.simModifierRight = simModifierRight;
                this.simModifierUp = simModifierUp;
                this.simModifierDown = simModifierDown;
            }

            private void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                this.angle = (ViewAngle)r.ReadByte();
                this.flip = r.ReadBoolean();
                this.unknown7 = new float[2];
                for (int i = 0; i < 2; i++) { this.unknown7[i] = r.ReadSingle(); }
                this.simModifierLeft = r.ReadUInt64();
                this.simModifierRight = r.ReadUInt64();
                this.simModifierUp = r.ReadUInt64();
                this.simModifierDown = r.ReadUInt64();
            }

            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write((byte)this.angle);
                w.Write(this.flip);
                if (this.unknown7 == null) this.unknown7 = new float[2];
                for (int i = 0; i < 2; i++) { w.Write(this.unknown7[i]); }
                w.Write(this.simModifierLeft);
                w.Write(this.simModifierRight);
                w.Write(this.simModifierUp);
                w.Write(this.simModifierDown);
            }

            #region AHandlerElement
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region IEquatable<Vector>
            public bool Equals(SliderDesc other)
            {
                return this.angle == other.angle && this.flip == other.flip && this.unknown7.SequenceEqual(other.unknown7) && 
                    this.simModifierLeft == other.simModifierLeft && this.simModifierRight == other.simModifierRight && this.simModifierUp == other.simModifierUp && this.simModifierDown == other.simModifierDown;
            }

            public override bool Equals(object obj) { return obj is SliderDesc && Equals(obj as SliderDesc); }

            public override int GetHashCode() { return this.angle.GetHashCode() + this.flip.GetHashCode() + this.unknown7.GetHashCode() + 
                this.simModifierLeft.GetHashCode() + this.simModifierRight.GetHashCode() + this.simModifierUp.GetHashCode() + this.simModifierDown.GetHashCode(); }
            #endregion

            [ElementPriority(10)]
            public ViewAngle ViewingAngle { get { return this.angle; } set { if (!this.angle.Equals(value)) { this.angle = value; OnElementChanged(); } } }
            [ElementPriority(11)]
            public bool FlipHorizontal { get { return this.flip; } set { if (!this.flip.Equals(value)) { this.flip = value; OnElementChanged(); } } }
            [ElementPriority(12)]
            public float[] Unknown7 { get { return this.unknown7; } set { if (!this.unknown7.Equals(value)) { this.unknown7 = value; OnElementChanged(); } } }
            [ElementPriority(13)]
            public ulong SimModifierLeft { get { return this.simModifierLeft; } set { if (!this.simModifierLeft.Equals(value)) { this.simModifierLeft = value; OnElementChanged(); } } }
            [ElementPriority(14)]
            public ulong SimModifierRight { get { return this.simModifierRight; } set { if (!this.simModifierRight.Equals(value)) { this.simModifierRight = value; OnElementChanged(); } } }
            [ElementPriority(15)]
            public ulong SimModifierUp { get { return this.simModifierUp; } set { if (!this.simModifierUp.Equals(value)) { this.simModifierUp = value; OnElementChanged(); } } }
            [ElementPriority(16)]
            public ulong SimModifierDown { get { return this.simModifierDown; } set { if (!this.simModifierDown.Equals(value)) { this.simModifierDown = value; OnElementChanged(); } } }

            public string Value { get { return ValueBuilder; } }
        }
        public class SliderList : DependentList<SliderDesc>
        {
            #region Constructors
            public SliderList(EventHandler handler) : base(handler) { }
            public SliderList(EventHandler handler, Stream s) : base(handler, s) { }
            public SliderList(EventHandler handler, IEnumerable<SliderDesc> le) : base(handler, le) { }
            #endregion

         //   protected override int ReadCount(Stream s) { return base.ReadCount(s) / 3; }
            protected override SliderDesc CreateElement(Stream s) { return new SliderDesc(0, elementHandler, s); }
            //  protected override void WriteCount(Stream s, int count) { base.WriteCount(s, (int)(count * 3)); }
            protected override void WriteElement(Stream s, SliderDesc element) { element.UnParse(s); }
        }

        public enum HotSpotLevel : byte
        {
            Macro = 0,
            Micro = 1,
            Special = 2
        }

        [Flags]
        public enum ViewAngle : byte
        {
            None = 0,
            Front = 1<<0,
            Threequarter_right = 1<<1,
            Threequarter_left = 1<<2,
            Profile_right = 1<<3,
            Profile_left = 1<<4,
            Back = 1<<5,
            Unknown = 1 << 6
        }

        [Flags]
        public enum RestrictToGender : byte
        {
            None = 0,
            Male = 1,
            Female = 2
        }

        public enum SliderCursor : byte
        {
            None = 0,
            HorizontalAndVerticalArrows = 1,
            HorizontalArrows = 2,
            VerticalArrows = 3,
            Diagonal = 4,
            Rotation = 5
        }
        #endregion
    }

    public class HotSpotControlResourceHandler : AResourceHandler
    {
        public HotSpotControlResourceHandler()
        {
            this.Add(typeof(HotSpotControl), new List<string>(new string[] { "0x8B18FF6E", }));
        }
    }
}

