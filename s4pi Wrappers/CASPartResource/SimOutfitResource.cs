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

// This resource is based on Keyi Zhang's and Snaitf's analysis

using System;
using System.Collections.Generic;
using System.IO;
using s4pi.Interfaces;

namespace CASPartResource
{
    public class SimOutfitResource : AResource
    {
        private uint version;
        private float heavyValue;
        private float fitValue;
        private float leanValue;
        private float bonyValue;
        private float hipsWideValue;
        private float hipsNarrowValue;
        private float waistWideValue;
        private float waistNarrowValue;
        private AgeGenderFlags age;
        private AgeGenderFlags gender;
        private Species species;
        private uint unknown1;
        private ulong skinToneReference;
        private CoatOverlayReferenceList coatLayers;

        private ByteIndexList sculptReference;
        private CountedTGIBlockList tgiList;
        private SliderReferenceList sliderReferencesFace;
        private SliderReferenceList sliderReferencesBody;
        private uint voiceActor;
        private float voicePitch;
        private ulong voiceEffect;
        private uint unknown2;
        private uint unknown3;
        private OutfitBlockList outfitsList;
                                                    //genetic info
        private ByteIndexList sculptReference2;
        private SliderReferenceList sliderReferencesFace2;
        private SliderReferenceList sliderReferencesBody2;
        private float heavyValue2;
        private float fitValue2;
        private float leanValue2;
        private float bonyValue2;
        private CaspReferenceList caspReferences2;
        private uint voiceActor2;
        private float voicePitch2;

        private byte flags;
        private ulong aspirationDataReference;
        private SimpleList<ulong> traitsDataReferenceList;


        public SimOutfitResource(int APIversion, Stream s) : base(APIversion, s) { if (s == null) { OnResourceChanged(this, EventArgs.Empty); } else { Parse(s); } }

        public void Parse(Stream s)
        {
            s.Position = 0;
            BinaryReader r = new BinaryReader(s);

            this.version = r.ReadUInt32();
            uint tgiOffset = r.ReadUInt32() + 8;

            // get TGI list
            long tempPosition = s.Position;
            s.Position = tgiOffset;
            TGIBlock[] _tgilist = new TGIBlock[r.ReadByte()];
            for (int i = 0; i < _tgilist.Length; i++) _tgilist[i] = new TGIBlock(1, OnResourceChanged, "IGT", s);
            this.tgiList = new CountedTGIBlockList(OnResourceChanged, _tgilist);
            s.Position = tempPosition;

            this.heavyValue = r.ReadSingle();
            this.fitValue = r.ReadSingle();
            this.leanValue = r.ReadSingle();
            this.bonyValue = r.ReadSingle();
            this.hipsWideValue = r.ReadSingle();
            this.hipsNarrowValue = r.ReadSingle();
            this.waistWideValue = r.ReadSingle();
            this.waistNarrowValue = r.ReadSingle();

            this.age = (AgeGenderFlags)r.ReadUInt32();
            this.gender = (AgeGenderFlags)r.ReadUInt32();
            if (version > 18)
            {
                this.species = (Species)r.ReadUInt32();
                this.unknown1 = r.ReadUInt32();
            }
            this.skinToneReference = r.ReadUInt64();
            if (this.version >= 24)
            {
                this.coatLayers = new CoatOverlayReferenceList(OnResourceChanged, s);
            }
            byte[] tmp = new byte[r.ReadByte()];
            for (int i = 0; i < tmp.Length; i++) tmp[i] = r.ReadByte();
            this.sculptReference = new ByteIndexList(OnResourceChanged, tmp, this.tgiList);

            sliderReferencesFace = new SliderReferenceList(OnResourceChanged, s, tgiList);
            sliderReferencesBody = new SliderReferenceList(OnResourceChanged, s, tgiList);

            this.voiceActor = r.ReadUInt32();
            this.voicePitch = r.ReadSingle();
            this.voiceEffect = r.ReadUInt64();
            this.unknown2 = r.ReadUInt32();
            this.unknown3 = r.ReadUInt32();
            this.outfitsList = new OutfitBlockList(OnResourceChanged, s, this.tgiList, this.version);

            tmp = new byte[r.ReadByte()];
            for (int i = 0; i < tmp.Length; i++) tmp[i] = r.ReadByte();
            this.sculptReference2 = new ByteIndexList(OnResourceChanged, tmp, this.tgiList);

            sliderReferencesFace2 = new SliderReferenceList(OnResourceChanged, s, tgiList);
            sliderReferencesBody2 = new SliderReferenceList(OnResourceChanged, s, tgiList);

            this.heavyValue2 = r.ReadSingle();
            this.fitValue2 = r.ReadSingle();
            this.leanValue2 = r.ReadSingle();
            this.bonyValue2 = r.ReadSingle();
            this.caspReferences2 = new CaspReferenceList(OnResourceChanged, tgiList);
            byte count = r.ReadByte();
            for (int i = 0; i < count; i++) caspReferences2.Add(new CaspReference(RecommendedApiVersion, OnResourceChanged, s, tgiList));
            this.voiceActor2 = r.ReadUInt32();
            this.voicePitch2 = r.ReadSingle();

            this.flags = r.ReadByte();
            this.aspirationDataReference = r.ReadUInt64();
            this.traitsDataReferenceList = new SimpleList<ulong>(OnResourceChanged);
            int count2 = r.ReadByte();
            for (int i = 0; i < count2; i++)
                this.traitsDataReferenceList.Add(r.ReadUInt64());
        }

        protected override Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);
            w.Write(this.version);
            long tgiOffsetPosition = ms.Position;
            w.Write(0);
            w.Write(heavyValue);
            w.Write(fitValue);
            w.Write(leanValue);
            w.Write(bonyValue);
            w.Write(hipsWideValue);
            w.Write(hipsNarrowValue);
            w.Write(waistWideValue);
            w.Write(waistNarrowValue);
            w.Write((uint)age);
            w.Write((uint)gender);
            if (version > 18)
            {
                w.Write((uint)this.species);
                w.Write(this.unknown1);
            }
            w.Write(skinToneReference);
            if (this.version >= 24)
            {
                this.coatLayers.UnParse(ms);
            }
            w.Write((byte)this.sculptReference.Count);
            foreach (var value in this.sculptReference) w.Write(value);
            sliderReferencesFace.UnParse(ms);
            sliderReferencesBody.UnParse(ms);

            w.Write(this.voiceActor);
            w.Write(this.voicePitch);
            w.Write(this.voiceEffect);
            w.Write(this.unknown2);
            w.Write(this.unknown3);

            this.outfitsList.UnParse(ms);

            w.Write((byte)this.sculptReference2.Count);
            foreach (var value in this.sculptReference2) w.Write(value);

            sliderReferencesFace2.UnParse(ms);
            sliderReferencesBody2.UnParse(ms);

            w.Write(this.heavyValue2);
            w.Write(this.fitValue2);
            w.Write(this.leanValue2);
            w.Write(this.bonyValue2);
            w.Write((byte)caspReferences2.Count);
            for (int i = 0; i < caspReferences2.Count; i++) caspReferences2[i].UnParse(ms);

            w.Write(this.voiceActor2);
            w.Write(this.voicePitch2);

            w.Write(this.flags);
            w.Write(this.aspirationDataReference);
            w.Write((byte)this.traitsDataReferenceList.Count);
            foreach (var i in this.traitsDataReferenceList) w.Write(i);

            long tmpPostion = ms.Position;
            ms.Position = tgiOffsetPosition;
            w.Write((uint)tmpPostion - 8);
            ms.Position = tmpPostion;
            w.Write((byte)tgiList.Count);
            foreach (var tgi in this.tgiList)
            {
                w.Write(tgi.Instance);
                w.Write(tgi.ResourceGroup);
                w.Write(tgi.ResourceType);
            }

            ms.Position = 0;
            return ms;
        }


        #region Sub-Type
        public class SliderReference : AHandlerElement, IEquatable<SliderReference>
        {
            public CountedTGIBlockList ParentTGIList { get; private set; }
            private byte index;
            private float sliderValue;

            public SliderReference(int apiVersion, EventHandler handler, CountedTGIBlockList tgiList) : base(apiVersion, handler) { this.ParentTGIList = tgiList; }
            public SliderReference(int apiVersion, EventHandler handler, Stream s, CountedTGIBlockList tgiList) : base(apiVersion, handler) { this.ParentTGIList = tgiList; Parse(s); }

            public void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                this.index = r.ReadByte();
                this.sliderValue = r.ReadSingle();
            }

            public void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(this.index);
                w.Write(this.sliderValue);
            }


            const int recommendedApiVersion = 1;
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { var res = GetContentFields(requestedApiVersion, this.GetType()); res.Remove("ParentTGIList"); return res; } }
            public string Value { get { return ValueBuilder; } }

            public bool Equals(SliderReference other)
            {
                return this.index == other.index && this.sliderValue == other.sliderValue;
            }
            [ElementPriority(0), TGIBlockListContentField("ParentTGIList")]
            public byte Index { get { return this.index; } set { if (!this.index.Equals(value)) { OnElementChanged(); this.index = value; } } }
            [ElementPriority(1)]
            public float SliderValue { get { return this.sliderValue; } set { if (!this.sliderValue.Equals(value)) { OnElementChanged(); this.sliderValue = value; } } }
        }


        public class SliderReferenceList : DependentList<SliderReference>
        {
            private CountedTGIBlockList tgiList;
            public SliderReferenceList(EventHandler handler, CountedTGIBlockList tgiList) : base(handler) { this.tgiList = tgiList; }
            public SliderReferenceList(EventHandler handler, Stream s, CountedTGIBlockList tgiList) : this(handler, tgiList) { Parse(s, tgiList); }

            #region Data I/O
            protected void Parse(Stream s, CountedTGIBlockList tgiList)
            {
                int count = s.ReadByte();
                for (int i = 0; i < count; i++) this.Add(new SliderReference(recommendedApiVersion, handler, s, this.tgiList));
            }

            public override void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write((byte)this.Count);
                foreach (var entry in this) entry.UnParse(s);
            }

            protected override SliderReference CreateElement(Stream s) { throw new NotImplementedException(); }
            protected override void WriteElement(Stream s, SliderReference element) { throw new NotImplementedException(); }
            #endregion
        }

        public class OutfitReference : AHandlerElement, IEquatable<OutfitReference>
        {
            private CountedTGIBlockList tgiList;
            private uint version;
            private ulong outfitID;
            private ulong outfitFlags;
            private ulong outfitCreated;
            private bool matchHair;
          //  private DataBlobHandler unknownBlock;
            private CaspReferenceList CasPartList;

            public OutfitReference(int apiVersion, EventHandler handler, CountedTGIBlockList tgiList, uint version) : base(apiVersion, handler) { this.tgiList = tgiList; this.version = version; }
            public OutfitReference(int apiVersion, EventHandler handler, Stream s, CountedTGIBlockList tgiList, uint version) : base(apiVersion, handler) { this.tgiList = tgiList; this.version = version; Parse(s); }

            public void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                if (this.version >= 24)
                {
                    this.outfitID = r.ReadUInt64();
                    this.outfitFlags = r.ReadUInt64();
                    this.outfitCreated = r.ReadUInt64();
                    this.matchHair = r.ReadBoolean();
                  //  this.unknownBlock = new DataBlobHandler(recommendedApiVersion, handler, r.ReadBytes(25));
                }
                else
                {
                    this.outfitID = r.ReadUInt64();
                    this.outfitFlags = r.ReadUInt64();
                  //  this.outfitCreated = r.ReadUInt64();
                    this.matchHair = r.ReadBoolean();
                  //  this.unknownBlock = new DataBlobHandler(recommendedApiVersion, handler, r.ReadBytes(17));
                }
                this.CasPartList = new CaspReferenceList(handler, s, tgiList);
            }

            public void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                if (this.version >= 24)
                {
                    w.Write(this.outfitID);
                    w.Write(this.outfitFlags);
                    w.Write(this.outfitCreated);
                    w.Write(this.matchHair);
                    //  this.unknownBlock = new DataBlobHandler(recommendedApiVersion, handler, r.ReadBytes(25));
                }
                else
                {
                    w.Write(this.outfitID);
                    w.Write(this.outfitFlags);
                    //  w.Write(this.outfitCreated);
                    w.Write(this.matchHair);
                    //  this.unknownBlock = new DataBlobHandler(recommendedApiVersion, handler, r.ReadBytes(17));
                }
              //  this.unknownBlock.UnParse(s);
                this.CasPartList.UnParse(s);
            }


            public bool Equals(OutfitReference other)
            {
                return this.outfitID.Equals(other.outfitID) && this.outfitFlags.Equals(other.outfitFlags) && this.outfitCreated.Equals(other.outfitCreated) 
                    && this.matchHair.Equals(other.matchHair) && this.CasPartList.Equals(other.CasPartList);
            }
            const int recommendedApiVersion = 1;
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            [ElementPriority(0)]
            public ulong OutfitID { get { return this.outfitID; } set { if (!this.outfitID.Equals(value)) { OnElementChanged(); this.outfitID = value; } } }
            [ElementPriority(1)]
            public ulong OutfitFlags { get { return this.outfitFlags; } set { if (!this.outfitFlags.Equals(value)) { OnElementChanged(); this.outfitFlags = value; } } }
            [ElementPriority(2)]
            public ulong OutfitCreated { get { return this.outfitCreated; } set { if (!this.outfitCreated.Equals(value)) { OnElementChanged(); this.outfitCreated = value; } } }
            [ElementPriority(3)]
            public bool MatchHair { get { return this.matchHair; } set { if (!this.matchHair.Equals(value)) { OnElementChanged(); this.matchHair = value; } } }
            [ElementPriority(4)]
            public CaspReferenceList CASPartList { get { return this.CasPartList; } set { if (!this.CasPartList.Equals(value)) { OnElementChanged(); this.CasPartList = value; } } }
            public string Value { get { return ValueBuilder; } }
            protected override List<string> ValueBuilderFields
            {
                get
                {
                    List<string> fields = base.ValueBuilderFields;
                    if (this.version < 24)
                    {
                        fields.Remove("OutfitCreated");
                    }
                    return fields;
                }
            }
        }

        public class CaspReference : AHandlerElement, IEquatable<CaspReference>
        {
            public CountedTGIBlockList ParentTGIList { get; private set; }
            public CaspReference(int apiVersion, EventHandler handler, CountedTGIBlockList tgiList) : base(apiVersion, handler) { this.ParentTGIList = tgiList; }
            public CaspReference(int apiVersion, EventHandler handler, Stream s, CountedTGIBlockList tgiList) : base(apiVersion, handler) { this.ParentTGIList = tgiList; Parse(s); }
            private byte index;
            private BodyType bodyType; 

            public void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                this.index = r.ReadByte();
                this.bodyType = (BodyType) r.ReadUInt32();
            }

            public void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(this.index);
                w.Write((uint)this.bodyType);
            }

            const int recommendedApiVersion = 1;
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { var res = GetContentFields(requestedApiVersion, this.GetType()); res.Remove("ParentTGIList"); return res; } }
            [ElementPriority(0), TGIBlockListContentField("ParentTGIList")]
            public byte TGIReference { get { return this.index; } set { if (!this.index.Equals(value)) { this.index = value; } } }
            [ElementPriority(1)]
            public BodyType CASP_BodyType { get { return this.bodyType; } set { if (!this.bodyType.Equals(value)) { this.bodyType = value; } } }
            public string Value { get { return ValueBuilder; } }

            #region IEquatable
            public bool Equals(CaspReference other)
            {
                return this.index == other.index && this.bodyType == other.bodyType;
            }
            #endregion
        }

        public class OutfitBlock : AHandlerElement, IEquatable<OutfitBlock>
        {

            private OutfitCategories category;
            private uint unknown1;
            private OutfitReferenceList unknownReferenceList;
            private CountedTGIBlockList tgiList;
            private uint version;

            public OutfitBlock(int apiVersion, EventHandler handler, CountedTGIBlockList tgiList, uint version) : base(apiVersion, handler) { this.tgiList = tgiList; this.version = version; }
            public OutfitBlock(int apiVersion, EventHandler handler, Stream s, CountedTGIBlockList tgiList, uint version) : base(apiVersion, handler) { this.tgiList = tgiList; this.version = version; Parse(s, tgiList, version); }
            
            protected void Parse(Stream s, CountedTGIBlockList tgiList, uint version)
            {
                BinaryReader r = new BinaryReader(s);
                this.category = (OutfitCategories)r.ReadByte();
                this.unknown1 = r.ReadUInt32();
                this.unknownReferenceList = new OutfitReferenceList(handler, s, tgiList, version);
            }

            public void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write((byte)this.category);
                w.Write(this.unknown1);
                this.unknownReferenceList.UnParse(s);
            }


            #region Content Fields
            [ElementPriority(0)]
            public OutfitCategories Category { get { return this.category; } set { if (!this.category.Equals(value)) { this.category = value; } } }
            [ElementPriority(1)]
            public uint Unknown1 { get { return this.unknown1; } set { if (!this.unknown1.Equals(value)) { this.unknown1 = value; } } }
            [ElementPriority(2)]
            public OutfitReferenceList UnknownReferenceList { get { return this.unknownReferenceList; } set { if (!this.unknownReferenceList.Equals(value)) { this.unknownReferenceList = value; } } }
            const int recommendedApiVersion = 1;
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            public string Value { get { return ValueBuilder; } }
            #endregion

            #region IEquatable
            public bool Equals(OutfitBlock other)
            {
                return this.category == other.category && this.unknown1 == other.unknown1 && this.unknownReferenceList.Equals(other.unknownReferenceList);
            }
            #endregion

            public enum OutfitCategories : byte
            {
                EveryDay = 0,
                Formal = 1,
                Athletic = 2,
                Sleep = 3,
                Party = 4,
                Bathing = 5,
                Career = 6,
                Situation = 7,
                Special = 8,
                Swimwear = 9
            }
        }

        public class OutfitBlockList: DependentList<OutfitBlock>
        {
            private CountedTGIBlockList tgiList;
            private uint version;
            public OutfitBlockList(EventHandler handler, CountedTGIBlockList tgiList, uint version) : base(handler) { this.tgiList = tgiList; this.version = version; }
            public OutfitBlockList(EventHandler handler, Stream s, CountedTGIBlockList tgiList, uint version) : this(handler, tgiList, version) { Parse(s, tgiList, version); }

            #region Data I/O
            protected void Parse(Stream s, CountedTGIBlockList tgiList, uint version)
            {
                BinaryReader r = new BinaryReader(s);
                int count = r.ReadInt32();
                for (int i = 0; i < count; i++) this.Add(new OutfitBlock(recommendedApiVersion, handler, s, this.tgiList, this.version));
            }

            public override void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(this.Count);
                foreach (var entry in this) entry.UnParse(s);
            }

            protected override OutfitBlock CreateElement(Stream s) { throw new NotImplementedException(); }
            protected override void WriteElement(Stream s, OutfitBlock element) { throw new NotImplementedException(); }
            #endregion
        }

        public class CaspReferenceList : DependentList<CaspReference>
        {
            private CountedTGIBlockList tgiList;
            public CaspReferenceList(EventHandler handler, CountedTGIBlockList tgiList) : base(handler) { this.tgiList = tgiList; }
            public CaspReferenceList(EventHandler handler, Stream s, CountedTGIBlockList tgiList) : this(handler, tgiList) { Parse(s, tgiList); }

            #region Data I/O
            protected void Parse(Stream s, CountedTGIBlockList tgiList)
            {
                BinaryReader r = new BinaryReader(s);
                int count = r.ReadInt32();
                for (int i = 0; i < count; i++) this.Add(new CaspReference(recommendedApiVersion, handler, s, this.tgiList));
            }

            public override void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(this.Count);
                foreach (var entry in this) entry.UnParse(s);
            }

            protected override CaspReference CreateElement(Stream s) { throw new NotImplementedException(); }
            protected override void WriteElement(Stream s, CaspReference element) { throw new NotImplementedException(); }
            #endregion
        }

        public class OutfitReferenceList : DependentList<OutfitReference>
        {
            private CountedTGIBlockList tgiList;
            private uint version;
            public OutfitReferenceList(EventHandler handler, CountedTGIBlockList tgiList, uint version) : base(handler) { this.tgiList = tgiList; this.version = version; }
            public OutfitReferenceList(EventHandler handler, Stream s, CountedTGIBlockList tgiList, uint version) : this(handler, tgiList, version) { Parse(s, tgiList, version); }

            #region Data I/O
            protected void Parse(Stream s, CountedTGIBlockList tgiList, uint version)
            {
                BinaryReader r = new BinaryReader(s);
                int count = r.ReadInt32();
                for (int i = 0; i < count; i++) this.Add(new OutfitReference(recommendedApiVersion, handler, s, this.tgiList, this.version));
            }

            public override void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(this.Count);
                foreach (var entry in this) entry.UnParse(s);
            }

            protected override OutfitReference CreateElement(Stream s) { throw new NotImplementedException(); }
            protected override void WriteElement(Stream s, OutfitReference element) { throw new NotImplementedException(); }
            #endregion

        }

        public class CoatOverlayReferenceList : DependentList<CoatOverlayReference>
        {
            public CoatOverlayReferenceList(EventHandler handler) : base(handler) { }
            public CoatOverlayReferenceList(EventHandler handler, Stream s) : this(handler) { Parse(s); }

            #region Data I/O
            protected void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                byte count = r.ReadByte();
                for (int i = 0; i < count; i++) this.Add(new CoatOverlayReference(recommendedApiVersion, handler, s));
            }

            public override void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write((byte)this.Count);
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
            private byte[] rgba;

            public void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                this.instance = r.ReadUInt64();
                this.rgba = r.ReadBytes(4);
            }

            public void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(this.instance);
                w.Write(this.rgba);
            }

            const int recommendedApiVersion = 1;
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { var res = GetContentFields(requestedApiVersion, this.GetType()); return res; } }
            [ElementPriority(0)]
            public ulong Instance { get { return this.instance; } set { if (!this.instance.Equals(value)) { this.instance = value; } } }
            [ElementPriority(1)]
            public byte[] Color_RGBA { get { return this.rgba; } set { if (!this.rgba.Equals(value)) { this.rgba = value; } } }
            public string Value { get { return ValueBuilder; } }

            #region IEquatable
            public bool Equals(CoatOverlayReference other)
            {
                return this.instance == other.instance && this.rgba == other.rgba;
            }
            #endregion
        }
        #endregion

        public string Value { get { return ValueBuilder; } }

        const int recommendedApiVersion = 1;
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
        public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }


        #region Content Fields
        [ElementPriority(0)]
        public uint Version { get { return this.version; } set { if (!this.version.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.version = value; } } }
        [ElementPriority(1)]
        public float HeavyValue { get { return this.heavyValue; } set { if (!this.heavyValue.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.heavyValue = value; } } }
        [ElementPriority(2)]
        public float FitValue { get { return this.fitValue; } set { if (!this.fitValue.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.fitValue = value; } } }
        [ElementPriority(3)]
        public float LeanValue { get { return this.leanValue; } set { if (!this.leanValue.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.leanValue = value; } } }
        [ElementPriority(4)]
        public float BonyValue { get { return this.bonyValue; } set { if (!this.bonyValue.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.bonyValue = value; } } }
        [ElementPriority(5)]
        public float HipsWideValue { get { return this.hipsWideValue; } set { if (!this.hipsWideValue.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.hipsWideValue = value; } } }
        [ElementPriority(6)]
        public float HipsNarrowValue { get { return this.hipsNarrowValue; } set { if (!this.hipsNarrowValue.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.hipsNarrowValue = value; } } }
        [ElementPriority(7)]
        public float WaistWideValue { get { return this.waistWideValue; } set { if (!this.waistWideValue.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.waistWideValue = value; } } }
        [ElementPriority(8)]
        public float WaistNarrowValue { get { return this.waistNarrowValue; } set { if (!this.waistNarrowValue.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.waistNarrowValue = value; } } }
        [ElementPriority(9)]
        public AgeGenderFlags Age { get { return this.age; } set { if (!this.age.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.age = value; } } }
        [ElementPriority(10)]
        public AgeGenderFlags Gender { get { return this.gender; } set { if (!this.gender.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.gender = value; } } }
        [ElementPriority(11)]
        public Species Species { get { return this.species; } set { if (!this.species.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.species = value; } } }
        [ElementPriority(12)]
        public uint Unknown1 { get { return this.unknown1; } set { if (!this.unknown1.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.unknown1 = value; } } }
        [ElementPriority(13)]
        public ulong SkinToneReference { get { return this.skinToneReference; } set { if (!this.skinToneReference.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.skinToneReference = value; } } }
        [ElementPriority(15)]
        public CoatOverlayReferenceList CoatLayers { get { return this.coatLayers; } set { if (this.coatLayers != value) { OnResourceChanged(this, EventArgs.Empty); this.coatLayers = value; } } }
        [ElementPriority(16)]
        public ByteIndexList SculptReference { get { return this.sculptReference; } set { if (!this.sculptReference.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.sculptReference = value; } } }
        [ElementPriority(17)]
        public SliderReferenceList SimModifierReferencesFace { get { return this.sliderReferencesFace; } set { if (!this.sliderReferencesFace.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.sliderReferencesFace = value; } } }
        [ElementPriority(18)]
        public SliderReferenceList SimModifierReferencesBody { get { return this.sliderReferencesBody; } set { if (!this.sliderReferencesBody.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.sliderReferencesBody = value; } } }
        [ElementPriority(19)]
        public uint VoiceActor { get { return this.voiceActor; } set { if (!this.voiceActor.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.voiceActor = value; } } }
        [ElementPriority(20)]
        public float VoicePitch { get { return this.voicePitch; } set { if (!this.voicePitch.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.voicePitch = value; } } }
        [ElementPriority(21)]
        public ulong VoiceEffect { get { return this.voiceEffect; } set { if (!this.voiceEffect.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.voiceEffect = value; } } }
        [ElementPriority(22)]
        public uint Unknown2 { get { return this.unknown2; } set { if (!this.unknown2.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.unknown2 = value; } } }
        [ElementPriority(23)]
        public uint Unknown3 { get { return this.unknown3; } set { if (!this.unknown3.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.unknown3 = value; } } }
        [ElementPriority(24)]
        public OutfitBlockList OutfitsList { get { return this.outfitsList; } set { if (!this.outfitsList.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.outfitsList = value; } } }
        [ElementPriority(25)]
        public ByteIndexList SculptReference2 { get { return this.sculptReference2; } set { if (!this.sculptReference2.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.sculptReference2 = value; } } }
        [ElementPriority(26)]
        public SliderReferenceList SimModifierReferencesFace2 { get { return this.sliderReferencesFace2; } set { if (!this.sliderReferencesFace2.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.sliderReferencesFace2 = value; } } }
        [ElementPriority(27)]
        public SliderReferenceList SimModifierReferencesBody2 { get { return this.sliderReferencesBody2; } set { if (!this.sliderReferencesBody2.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.sliderReferencesBody2 = value; } } }
        [ElementPriority(28)]
        public float HeavyValue2 { get { return this.heavyValue2; } set { if (!this.heavyValue2.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.heavyValue2 = value; } } }
        [ElementPriority(29)]
        public float FitValue2 { get { return this.fitValue2; } set { if (!this.fitValue2.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.fitValue2 = value; } } }
        [ElementPriority(30)]
        public float LeanValue2 { get { return this.leanValue2; } set { if (!this.leanValue2.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.leanValue2 = value; } } }
        [ElementPriority(31)]
        public float BonyValue2 { get { return this.bonyValue2; } set { if (!this.bonyValue2.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.bonyValue2 = value; } } }
        [ElementPriority(32)]
        public CaspReferenceList CASPReferences { get { return this.caspReferences2; } set { if (!this.caspReferences2.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.caspReferences2 = value; } } }
        [ElementPriority(33)]
        public uint VoiceActor2 { get { return this.voiceActor2; } set { if (!this.voiceActor2.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.voiceActor2 = value; } } }
        [ElementPriority(34)]
        public float VoicePitch2 { get { return this.voicePitch2; } set { if (!this.voicePitch2.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.voicePitch2 = value; } } }
        [ElementPriority(35)]
        public byte Flags { get { return this.flags; } set { if (!this.flags.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.flags = value; } } }
        [ElementPriority(36)]
        public ulong AspirationDataReference { get { return this.aspirationDataReference; } set { if (!this.aspirationDataReference.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.aspirationDataReference = value; } } }
        [ElementPriority(37)]
        public SimpleList<ulong> TraitsDataReferenceList { get { return this.traitsDataReferenceList; } set { if (!this.traitsDataReferenceList.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.traitsDataReferenceList = value; } } }
        [ElementPriority(38)]
        public CountedTGIBlockList TGIList { get { return this.tgiList; } set { if (!this.tgiList.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.tgiList = value; } } }

        protected override List<string> ValueBuilderFields
        {
            get
            {
                List<string> fields = base.ValueBuilderFields;
                if (version <= 18)
                {
                    fields.Remove("Species");
                    fields.Remove("Unknown1");
                }
                if (this.version < 24)
                {
                    fields.Remove("CoatLayers");
                }
                return fields;
            }
        }

        #endregion
    }

    public class SimOutfitHandler : AResourceHandler
    {
        public SimOutfitHandler()
        {
            if (s4pi.Settings.Settings.IsTS4)
                this.Add(typeof(SimOutfitResource), new List<string>(new string[] { "0x025ED6F4", }));
        }
    }
}

