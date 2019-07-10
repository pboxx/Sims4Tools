/***************************************************************************
 *  Copyright (C) 2014 by Keyi Zhang                                       *
 *  kz005@bucknell.edu                                                     *
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
using s4pi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CASPartResource
{
    public class SimModifierResource : AResource
    {
        const int recommendedApiVersion = 1;
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

        static bool checking = s4pi.Settings.Settings.Checking;

        private uint contextVersion;
        private TGIBlock[] publicKey { get; set; }
        private TGIBlock[] externalKey { get; set; }
        private TGIBlockList delayLoadKey { get; set; }
        private ObjectData[] objects { get; set; }
        private uint version { get; set; }
        private AgeGenderFlags gender { get; set; }
        private SimRegion region { get; set; }
        private SimSubRegion subRegion { get; set; }
        private LinkTags linkTag { get; set; }
        private TGIBlock bonePoseKey { get; set; }
        private TGIBlock deformerMapShapeKey { get; set; }
        private TGIBlock deformerMapNormalKey { get; set; }
        private BoneEntryLIst boneEntryList { get; set; }

        
        public SimModifierResource(int APIversion, Stream s) : base(APIversion, s) { if (stream == null) { stream = UnParse(); OnResourceChanged(this, EventArgs.Empty); } stream.Position = 0; Parse(stream); }

        #region Data I/O
        void Parse(Stream s)
        {
            BinaryReader r = new BinaryReader(s);
            this.contextVersion = r.ReadUInt32();
            uint publicKeyCount = r.ReadUInt32();
            uint externalKeyCount = r.ReadUInt32();
            uint delayLoadKeyCount = r.ReadUInt32();
            uint objectCount = r.ReadUInt32();
            this.publicKey = new TGIBlock[publicKeyCount];
            for (int i = 0; i < publicKeyCount; i++)
            {
                this.publicKey[i] = new TGIBlock(recommendedApiVersion, OnResourceChanged, "ITG", s);
            }
            this.externalKey = new TGIBlock[externalKeyCount];
            for (int i = 0; i < externalKeyCount; i++)
            {
                this.externalKey[i] = new TGIBlock(recommendedApiVersion, OnResourceChanged, "ITG", s);
            }
            this.delayLoadKey = new TGIBlockList(OnResourceChanged);
            for (int i = 0; i < delayLoadKeyCount; i++)
            {
                this.delayLoadKey.Add(new TGIBlock(recommendedApiVersion, OnResourceChanged, "ITG", s));
            }
            this.objects = new ObjectData[objectCount];
            for (int i = 0; i < objectCount; i++)
            {
                objects[i] = new ObjectData(recommendedApiVersion, OnResourceChanged, s);
            }
            this.version = r.ReadUInt32();
            this.gender = (AgeGenderFlags)r.ReadUInt32();
            this.region = (SimRegion)r.ReadUInt32();
            if (this.version >= 144) this.subRegion = (SimSubRegion)r.ReadUInt32();
            this.linkTag = (LinkTags)r.ReadUInt32();
            this.bonePoseKey = new TGIBlock(recommendedApiVersion, OnResourceChanged, "ITG", s);
            this.deformerMapShapeKey = new TGIBlock(recommendedApiVersion, OnResourceChanged, "ITG", s);
            this.deformerMapNormalKey = new TGIBlock(recommendedApiVersion, OnResourceChanged, "ITG", s);
            this.boneEntryList = new BoneEntryLIst(OnResourceChanged, s);
        }

        protected override Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);
            w.Write(this.contextVersion);
            w.Write(this.publicKey.Length);
            w.Write(this.externalKey.Length);
            w.Write(this.delayLoadKey.Count);
            w.Write(this.objects.Length);
            if (this.publicKey == null) this.publicKey = new TGIBlock[0];
            for (int i = 0; i < publicKey.Length; i++)
            {
                this.publicKey[i].UnParse(ms);
            }
            if (this.externalKey == null) this.externalKey = new TGIBlock[0];
            for (int i = 0; i < externalKey.Length; i++)
            {
                this.externalKey[i].UnParse(ms);
            }
            if (this.delayLoadKey == null) this.delayLoadKey = new TGIBlockList(OnResourceChanged);
            for (int i = 0; i < delayLoadKey.Count; i++)
            {

                w.Write(this.delayLoadKey[i].Instance);
                w.Write(this.delayLoadKey[i].ResourceType);
                w.Write(this.delayLoadKey[i].ResourceGroup);
            }
            if (this.objects == null) this.objects = new ObjectData[0];
            for (int i = 0; i < this.objects.Length; i++)
            {
                this.objects[i].UnParse(ms);
            }
            w.Write(this.version);
            w.Write((uint)this.gender);
            w.Write((uint)this.region);
            if (this.version >= 144) w.Write((uint)this.subRegion);
            w.Write((uint)this.linkTag);
            if (this.bonePoseKey == null) this.bonePoseKey = new TGIBlock(recommendedApiVersion, OnResourceChanged);
            this.bonePoseKey.UnParse(ms);
            if (this.deformerMapShapeKey == null) this.deformerMapShapeKey = new TGIBlock(recommendedApiVersion, OnResourceChanged);
            this.deformerMapShapeKey.UnParse(ms);
            if (this.deformerMapNormalKey == null) this.deformerMapNormalKey = new TGIBlock(recommendedApiVersion, OnResourceChanged);
            this.deformerMapNormalKey.UnParse(ms);
            if (this.boneEntryList == null) this.boneEntryList = new BoneEntryLIst(OnResourceChanged);
            this.boneEntryList.UnParse(ms);
            return ms;
        }
        #endregion

        #region Sub Class
        public class ObjectData : AHandlerElement, IEquatable<ObjectData>
        {
            public uint position { get; set; }
            public uint length { get; set; }
            public ObjectData(int apiVersion, EventHandler handler, Stream s) : base(apiVersion, handler) { Parse(s); }
            public void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                this.position = r.ReadUInt32();
                this.length = r.ReadUInt32();
            }

            public void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(this.position);
                w.Write(this.length);
            }

            #region AHandlerElement Members
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region IEquatable
            public bool Equals(ObjectData other)
            {
                return this.position == other.position && this.length == other.length;
            }
            #endregion

            public string Value { get { return ValueBuilder; } }
        }

        public class BoneEntryLIst:DependentList<BoneEntry>
        {
            public BoneEntryLIst(EventHandler handler) : base(handler) { }
            public BoneEntryLIst(EventHandler handler, Stream s) : base(handler) { Parse(s); }

            #region Data I/O
            protected override void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                uint count = r.ReadUInt32();
                for (uint i = 0; i < count; i++) this.Add(new BoneEntry(recommendedApiVersion, handler, s));
            }

            public override void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(this.Count);
                foreach (var bone in this) bone.UnParse(s);
            }

            protected override BoneEntry CreateElement(Stream s) { return new BoneEntry(1, handler, s); }
            protected override void WriteElement(Stream s, BoneEntry element) { element.UnParse(s); }
            #endregion
        }

        public class BoneEntry: AHandlerElement, IEquatable<BoneEntry>
        {
            public uint boneHash { get; set; }
            public float multiplier { get; set; }

            public BoneEntry(int apiVersion, EventHandler handler, Stream s) : base(apiVersion, handler) { Parse(s); }
            public BoneEntry(int apiVersion, EventHandler handler) : base(apiVersion, handler) { }

            public void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);
                this.boneHash = r.ReadUInt32();
                this.multiplier = r.ReadSingle();
            }

            public void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);
                w.Write(this.boneHash);
                w.Write(this.multiplier);
            }

            #region AHandlerElement Members
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region IEquatable
            public bool Equals(BoneEntry other)
            {
                return this.boneHash == other.boneHash && this.multiplier == other.multiplier;
            }
            #endregion

            public string Value { get { return ValueBuilder; } }
        }
        #endregion

        #region Content Fields
        [ElementPriority(0)]
        public uint ContextVersion { get { return this.contextVersion; } set { if (!this.contextVersion.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.contextVersion = value; } } }
        [ElementPriority(1)]
        public TGIBlock[] PublicKey { get { return this.publicKey; } set { if (!this.publicKey.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.publicKey = value; } } }
        [ElementPriority(2)]
        public TGIBlock[] ExternalKey { get { return this.externalKey; } set { if (!this.externalKey.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.externalKey = value; } } }
        [ElementPriority(3)]
        public TGIBlockList BlendGeometry_Key { get { return this.delayLoadKey; } set { if (!this.delayLoadKey.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.delayLoadKey = value; } } }
        [ElementPriority(4)]
        public ObjectData[] ObjectInfo { get { return this.objects; } set { if (!this.objects.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.objects = value; } } }
        [ElementPriority(5)]
        public uint Version { get { return this.version; } set { if (!this.version.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.version = value; } } }
        [ElementPriority(6)]
        public AgeGenderFlags AgeGender { get { return this.gender; } set { if (!this.gender.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.gender = value; } } }
        [ElementPriority(7)]
        public SimRegion Region { get { return this.region; } set { if (!this.region.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.region = value; } } }
        [ElementPriority(8)]
        public SimSubRegion SubRegion { get { return this.subRegion; } set { if (!this.subRegion.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.subRegion = value; } } }
        [ElementPriority(9)]
        public LinkTags LinkTag { get { return this.linkTag; } set { if (!this.linkTag.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.linkTag = value; } } }
        [ElementPriority(10)]
        public TGIBlock BonePoseKey { get { return this.bonePoseKey; } set { if (!this.bonePoseKey.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.bonePoseKey = value; } } }
        [ElementPriority(11)]
        public TGIBlock DeformerMapShapeKey { get { return this.deformerMapShapeKey; } set { if (!this.deformerMapShapeKey.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.deformerMapShapeKey = value; } } }
        [ElementPriority(12)]
        public TGIBlock DeformerMapNormalKey { get { return this.deformerMapNormalKey; } set { if (!this.deformerMapNormalKey.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.deformerMapNormalKey = value; } } }
        [ElementPriority(13)]
        public BoneEntryLIst BoneEntryList { get { return this.boneEntryList; } set { if (!this.boneEntryList.Equals(value)) { OnResourceChanged(this, EventArgs.Empty); this.boneEntryList = value; } } }
        public string Value { get { return ValueBuilder; } }
        public override List<string> ContentFields
        {
            get
            {
                var res = GetContentFields(requestedApiVersion, this.GetType());
                if (this.version < 144)
                {
                    res.Remove("Reserved0");
                }
                return res;
            }
        } 
        #endregion
    }

    public enum LinkTags : uint
    {
        None = 0x00000000,
        UseBlendLink = 0x30000001
    }

    public class SimModifierHandler : AResourceHandler
    {
        public SimModifierHandler()
        {
            if (s4pi.Settings.Settings.IsTS4)
                this.Add(typeof(SimModifierResource), new List<string>(new string[] { "0xC5F6763E", }));
        }
    }
}
