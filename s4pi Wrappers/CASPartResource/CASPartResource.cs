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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using CASPartResource.Lists;
using CASPartResource.Handlers;

using s4pi.Interfaces;
using s4pi.Settings;

namespace CASPartResource
{
    public class CASPartResource : AResource
    {
        internal const int recommendedApiVersion = 1;

        public override int RecommendedApiVersion
        {
            get { return recommendedApiVersion; }
        }

        #region Attributes

        public uint version;
        private uint presetCount;
        private string name;
        private float sortPriority;
        private ushort secondarySortIndex;
        private uint propertyID;
        private uint auralMaterialHash;
        private ParmFlag parmFlags;
        private ParmFlag2 parmFlags2;
        private ExcludePartFlag excludePartFlags;
        private ExcludePartFlag2 excludePartFlags2;           // cmar - added v 0x29
        private ExcludeModifierRegion excludeModifierRegionFlags;   // cmar - changed from uint to ulong with V 0x25
        private FlagList flagList;                  // property 16-bit tag / 32-bit value pairs
        private uint deprecatedPrice;               // deprecated
        private uint partTitleKey;
        private uint partDescriptionKey;
        private uint createDescriptionKey;          // added in v 0x2B
        private byte uniqueTextureSpace;
        private BodyType bodyType;
        private BodySubType bodySubType;            // cmar - changed from unused with V 0x25
        private AgeGenderFlags ageGender;
        private Species species;                     
        short packID;                               // cmar - added V 0x25
        PackFlag packFlags;                         // cmar - added V 0x25
        byte[] reserved2;                           // cmar - added V 0x25, nine bytes, set to 0
        byte unused2;                               // cmar - only if V < 0x25
        byte unused3;                               // cmar - only if V < 0x25
        private SwatchColorList swatchColorCode;
        private byte buffResKey;
        private byte varientThumbnailKey;
        private ulong voiceEffectHash;
        private byte usedMaterialCount;             // cmar - added V 0x1E
        private uint materialSetUpperBodyHash;      // cmar - added V 0x1E
        private uint materialSetLowerBodyHash;      // cmar - added V 0x1E
        private uint materialSetShoesHash;          // cmar - added V 0x1E
        private OccultTypesDisabled hideForOccultFlags; // cmar = added V 0x1F
        private UInt64 oppositeGenderPart;          // Version 0x28
        private UInt64 fallbackPart;                // Version 0x28
        OpacitySettings opacitySlider;     //V 0x2C
        SliderSettings hueSlider;           // "
        SliderSettings saturationSlider;    // "
        SliderSettings brightnessSlider;    // "                   // Version 0x2C - 11 floats
        private byte nakedKey;
        private byte parentKey;
        private int sortLayer;
        private LODBlockList lodBlockList;
        private SimpleList<byte> slotKey;
        private byte diffuseKey;
        private byte shadowKey;
        private byte compositionMethod;
        private byte regionMapKey;
        private OverrideList overrides;
        private byte normalMapKey;
        private byte specularMapKey;
        private BodyType sharedUVMapSpace;
        private byte emissionMapKey;                // cmar - added V 0x1E
        private byte reservedByte;                  // added V 0x2A
        private CountedTGIBlockList tgiList;

        #endregion

        public CASPartResource(int APIversion, Stream s)
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
            s.Position = 0;
            var r = new BinaryReader(s);
            this.version = r.ReadUInt32();
            this.TGIoffset = r.ReadUInt32() + 8;
            this.presetCount = r.ReadUInt32();
            if (this.presetCount != 0)
            {
                throw new Exception("Found non-zero one");
            }
            this.name = BigEndianUnicodeString.Read(s);

            this.sortPriority = r.ReadSingle();
            this.secondarySortIndex = r.ReadUInt16();
            this.propertyID = r.ReadUInt32();
            this.auralMaterialHash = r.ReadUInt32();
            this.parmFlags = (ParmFlag)r.ReadByte();
            if (this.version >= 39) parmFlags2 = (ParmFlag2)r.ReadByte();
            this.excludePartFlags = (ExcludePartFlag)r.ReadUInt64();
            if (this.version >= 41) this.excludePartFlags2 = (ExcludePartFlag2)r.ReadUInt64();
            if (this.version >= 36) this.excludeModifierRegionFlags = (ExcludeModifierRegion) r.ReadUInt64();
            else this.excludeModifierRegionFlags = (ExcludeModifierRegion)r.ReadUInt32();

            if (this.version >= 37)
                this.flagList = new FlagList(this.OnResourceChanged, s);
            else
            {
                this.flagList = FlagList.CreateWithUInt16Flags(this.OnResourceChanged, s, recommendedApiVersion);
            }

            this.deprecatedPrice = r.ReadUInt32();
            this.partTitleKey = r.ReadUInt32();
            this.partDescriptionKey = r.ReadUInt32();
            if (this.version >= 43) this.createDescriptionKey = r.ReadUInt32();
            this.uniqueTextureSpace = r.ReadByte();
            this.bodyType = (BodyType)r.ReadInt32();
            this.bodySubType = (BodySubType)r.ReadInt32();
            this.ageGender = (AgeGenderFlags)r.ReadUInt32();
            if (this.version >= 0x20)
            {
                this.species = (Species)r.ReadUInt32();
            }
            if (this.version >= 34)
            {
                this.packID = r.ReadInt16();
                this.packFlags = (PackFlag)r.ReadByte();
                this.reserved2 = r.ReadBytes(9);
            }
            else
            {
                this.packID = 0;
                this.unused2 = r.ReadByte();
                if (this.unused2 > 0) this.unused3 = r.ReadByte();
            }

            this.swatchColorCode = new SwatchColorList(this.OnResourceChanged, s);

            this.buffResKey = r.ReadByte();
            this.varientThumbnailKey = r.ReadByte();
            if (this.version >= 0x1C)
            {
                this.voiceEffectHash = r.ReadUInt64();
            }
            if (this.version >= 0x1E)
            {
                this.usedMaterialCount = r.ReadByte();
                if (this.usedMaterialCount > 0)
                {
                    this.materialSetUpperBodyHash = r.ReadUInt32();
                    this.materialSetLowerBodyHash = r.ReadUInt32();
                    this.materialSetShoesHash = r.ReadUInt32();
                }
            }
            if (this.version >= 0x1F)
            {
                this.hideForOccultFlags = (OccultTypesDisabled)r.ReadUInt32();
            }
            if (version >= 38)
            {
                oppositeGenderPart = r.ReadUInt64();
            }
            if (version >= 39)
            {
                fallbackPart = r.ReadUInt64();
            }
            if (version >= 44)
            {
                opacitySlider = new OpacitySettings(1, this.OnResourceChanged, s);
                hueSlider = new SliderSettings(1, this.OnResourceChanged, s);
                saturationSlider = new SliderSettings(1, this.OnResourceChanged, s);
                brightnessSlider = new SliderSettings(1, this.OnResourceChanged, s);
            }

            this.nakedKey = r.ReadByte();
            this.parentKey = r.ReadByte();
            this.sortLayer = r.ReadInt32();

            // Don't move any of this before the -----
            // TGI block list
            var currentPosition = r.BaseStream.Position;
            r.BaseStream.Position = this.TGIoffset;
            var count4 = r.ReadByte();
            this.tgiList = new CountedTGIBlockList(this.OnResourceChanged, "IGT", count4, s);
            r.BaseStream.Position = currentPosition;
            this.lodBlockList = new LODBlockList(null, s, this.tgiList);
            //-------------

            var count = r.ReadByte();
            this.slotKey = new SimpleList<byte>(null);
            for (byte i = 0; i < count; i++)
            {
                this.slotKey.Add(r.ReadByte());
            }

            this.diffuseKey = r.ReadByte();
            this.shadowKey = r.ReadByte();
            this.compositionMethod = r.ReadByte();
            this.regionMapKey = r.ReadByte();
            this.overrides = new OverrideList(null, s);
            this.normalMapKey = r.ReadByte();
            this.specularMapKey = r.ReadByte();
            if (this.version >= 0x1B)
            {
                this.sharedUVMapSpace = (BodyType)r.ReadUInt32();
            }
            if (this.version >= 0x1E)
            {
                this.emissionMapKey = r.ReadByte();
            }
            if (this.version >= 42)
            {
                this.reservedByte = r.ReadByte();
            }
        }

        protected override Stream UnParse()
        {
            var s = new MemoryStream();
            var w = new BinaryWriter(s);

            w.Write(this.version);
            w.Write(0); // tgi offset
            w.Write(this.presetCount);
            BigEndianUnicodeString.Write(s, this.name);
            w.Write(this.sortPriority);
            w.Write(this.secondarySortIndex);
            w.Write(this.propertyID);
            w.Write(this.auralMaterialHash);
            w.Write((byte)this.parmFlags);
            if (this.version >= 39) w.Write((byte)this.parmFlags2);
            w.Write((ulong)this.excludePartFlags);
            if (this.version >= 41) w.Write((ulong)this.excludePartFlags2);
            if (this.version >= 36)
            {
                w.Write((ulong)this.excludeModifierRegionFlags);
            }
            else
            {
                w.Write((uint)this.excludeModifierRegionFlags);
            }
            this.flagList = this.flagList ?? new FlagList(this.OnResourceChanged);
            if (this.version >= 37)
            {
                this.flagList.UnParse(s);
            }
            else
            {
                this.flagList.WriteUInt16Flags(s);
            }
            w.Write(this.deprecatedPrice);
            w.Write(this.partTitleKey);
            w.Write(this.partDescriptionKey);
            if (this.version >= 43) w.Write(this.createDescriptionKey);
            w.Write(this.uniqueTextureSpace);
            w.Write((uint)this.bodyType);
            w.Write((uint)this.bodySubType);
            w.Write((uint)this.ageGender);
            if (this.version >= 0x20)
            {
                w.Write((uint)this.species);
            }
            if (this.version >= 34)
            {
                w.Write(this.packID);
                w.Write((byte)this.packFlags);
                if (this.reserved2 == null) this.reserved2 = new byte[9];
                w.Write(this.reserved2);
            }
            else
            {
                w.Write(this.unused2);
                if (this.unused2 > 0) w.Write(this.unused3);
            }
            if (this.swatchColorCode == null)
            {
                this.swatchColorCode = new SwatchColorList(this.OnResourceChanged);
            }
            this.swatchColorCode.UnParse(s);
            w.Write(this.buffResKey);
            w.Write(this.varientThumbnailKey);
            if (this.version >= 0x1C)
            {
                w.Write(this.voiceEffectHash);
            }
            if (this.version >= 0x1E)
            {
                w.Write(this.usedMaterialCount);
                if (this.usedMaterialCount > 0)
                {
                    w.Write(this.materialSetUpperBodyHash);
                    w.Write(this.materialSetLowerBodyHash);
                    w.Write(this.materialSetShoesHash);
                }
            }
            if (this.version >= 0x1F)
            {
                w.Write((uint)this.hideForOccultFlags);
            }
            if (version >= 38)
            {
                w.Write(oppositeGenderPart);
            }
            if (version >= 39)
            {
                w.Write(fallbackPart);
            }
            if (version >= 44)
            {
                opacitySlider.UnParse(s);
                hueSlider.UnParse(s);
                saturationSlider.UnParse(s);
                brightnessSlider.UnParse(s);
            }
            w.Write(this.nakedKey);
            w.Write(this.parentKey);
            w.Write(this.sortLayer);
            if (this.lodBlockList == null)
            {
                this.lodBlockList = new LODBlockList(this.OnResourceChanged);
            }
            this.lodBlockList.UnParse(s);
            if (this.slotKey == null)
            {
                this.slotKey = new SimpleList<byte>(this.OnResourceChanged);
            }
            w.Write((byte)this.slotKey.Count);
            foreach (var b in this.slotKey)
            {
                w.Write(b);
            }
            w.Write(this.diffuseKey);
            w.Write(this.shadowKey);
            w.Write(this.compositionMethod);
            w.Write(this.regionMapKey);
            if (this.overrides == null)
            {
                this.overrides = new OverrideList(this.OnResourceChanged);
            }
            this.overrides.UnParse(s);
            w.Write(this.normalMapKey);
            w.Write(this.specularMapKey);
            if (this.version >= 0x1B)
            {
                w.Write((uint)this.sharedUVMapSpace);
            }
            if (this.version >= 0x1E)
            {
                w.Write(this.emissionMapKey);
            }
            if (this.version >= 42)
            {
                w.Write(this.reservedByte);
            }

            var tgiPosition = w.BaseStream.Position;
            w.BaseStream.Position = 4;
            w.Write(tgiPosition - 8);
            w.BaseStream.Position = tgiPosition;
            if (this.tgiList == null)
            {
                this.tgiList = new CountedTGIBlockList(this.OnResourceChanged);
            }
            w.Write((byte)this.tgiList.Count);
            foreach (var tgi in this.tgiList)
            {
                tgi.UnParse(s);
            }

            return s;
        }

        #endregion

        #region Subtypes

        public class OpacitySettings : AHandlerElement, IEquatable<OpacitySettings>
        {
            internal float minimum;
            internal float increment;

            public OpacitySettings(int apiVersion, EventHandler handler)
                : base(apiVersion, handler)
		    {
                this.minimum = .2f;
                this.increment = .05f;
		    }

            public OpacitySettings(int apiVersion, EventHandler handler, float minimum, float increment)
                : base(apiVersion, handler)
            {
                this.minimum = minimum;
                this.increment = increment;
            }

            public OpacitySettings(int apiVersion, EventHandler handler, Stream s)
			: base(apiVersion, handler)
		    {
			    this.Parse(s);
		    }

            public void Parse(Stream s)
            {
                BinaryReader br = new BinaryReader(s);
                this.minimum = br.ReadSingle();
                this.increment = br.ReadSingle();
            }

            public void UnParse(Stream s)
            {
                BinaryWriter bw = new BinaryWriter(s);
                bw.Write(this.minimum);
                bw.Write(this.increment);
            }

            #region AHandlerElement Members

            public override int RecommendedApiVersion
            {
                get { return recommendedApiVersion; }
            }

            public override List<string> ContentFields
            {
                get { return GetContentFields(this.requestedApiVersion, this.GetType()); }
            }

            #endregion

            #region Content Fields

            [ElementPriority(0)]
            public float Minimum
            {
                get { return this.minimum; }
                set
                {
                    if (!value.Equals(this.minimum))
                    {
                        this.minimum = value;
                        this.OnElementChanged();
                    }
                }
            }

            [ElementPriority(2)]
            public float Increment
            {
                get { return this.increment; }
                set
                {
                    if (!value.Equals(this.increment))
                    {
                        this.increment = value;
                        this.OnElementChanged();
                    }
                }
            }

            public string Value
            {
                get { return this.ValueBuilder; }
            }

            #endregion

            #region IEquatable

            public bool Equals(OpacitySettings other)
            {
                return this.minimum == other.minimum && this.increment == other.increment;
            }

            #endregion

        }

        public class SliderSettings : OpacitySettings
        {
            internal float maximum;

            public SliderSettings(int apiVersion, EventHandler handler)
                : base(apiVersion, handler)
		    {
                this.minimum = -.5f;
                this.maximum = .5f;
                this.increment = .05f;
		    }

            public SliderSettings(int apiVersion, EventHandler handler, float minimum, float maximum, float increment)
                : base(apiVersion, handler)
            {
                this.minimum = minimum;
                this.maximum = maximum;
                this.increment = increment;
            }

            public SliderSettings(int apiVersion, EventHandler handler, Stream s)
			: base(apiVersion, handler)
		    {
			    this.Parse(s);
		    }

            public void Parse(Stream s)
            {
                BinaryReader br = new BinaryReader(s);
                this.minimum = br.ReadSingle();
                this.maximum = br.ReadSingle();
                this.increment = br.ReadSingle();
            }

            public void UnParse(Stream s)
            {
                BinaryWriter bw = new BinaryWriter(s);
                bw.Write(this.minimum);
                bw.Write(this.maximum);
                bw.Write(this.increment);
            }

            #region Content Fields

            [ElementPriority(0)]
            public float Maximum
            {
                get { return this.maximum; }
                set
                {
                    if (!value.Equals(this.maximum))
                    {
                        this.maximum = value;
                        this.OnElementChanged();
                    }
                }
            }

            public string Value
            {
                get { return this.ValueBuilder; }
            }

            #endregion

            #region IEquatable

            public bool Equals(SliderSettings other)
            {
                return this.minimum == other.minimum && this.increment == other.increment && this.maximum == other.maximum;
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
                if (value != this.version)
                {
                    this.version = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(1)]
        public uint TGIoffset { get; private set; }

        [ElementPriority(2)]
        public uint PresetCount
        {
            get { return this.presetCount; }
            set
            {
                if (value != this.presetCount)
                {
                    this.presetCount = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(3)]
        public string Name
        {
            get { return this.name; }
            set
            {
                if (!value.Equals(this.name))
                {
                    this.name = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(4)]
        public float SortPriority
        {
            get { return this.sortPriority; }
            set
            {
                if (!value.Equals(this.sortPriority))
                {
                    this.sortPriority = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(5)]
        public ushort SecondarySortIndex
        {
            get { return this.secondarySortIndex; }
            set
            {
                if (!value.Equals(this.secondarySortIndex))
                {
                    this.secondarySortIndex = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(6)]
        public uint PropertyID
        {
            get { return this.propertyID; }
            set
            {
                if (!value.Equals(this.propertyID))
                {
                    this.propertyID = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(7)]
        public uint AuralMaterialHash
        {
            get { return this.auralMaterialHash; }
            set
            {
                if (!value.Equals(this.auralMaterialHash))
                {
                    this.auralMaterialHash = value;
                    this.OnResourceChanged(this, EventArgs.Empty);
                }
            }
        }

        [ElementPriority(8)]
        public ParmFlag ParameterFlags
        {
            get { return this.parmFlags; }
            set
            {
                if (!value.Equals(this.parmFlags))
                {
                    this.parmFlags = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(9)]
        public ParmFlag2 ParameterFlags2
        {
            get { return this.parmFlags2; }
            set
            {
                if (!value.Equals(this.parmFlags2))
                {
                    this.parmFlags2 = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(10)]
        public ExcludePartFlag ExcludePartFlags
        {
            get { return this.excludePartFlags; }
            set
            {
                if (!value.Equals(this.excludePartFlags))
                {
                    this.excludePartFlags = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(11)]
        public ExcludePartFlag2 ExcludePartFlags2
        {
            get { return this.excludePartFlags2; }
            set
            {
                if (!value.Equals(this.excludePartFlags2))
                {
                    this.excludePartFlags2 = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(12)]
        public ExcludeModifierRegion ExcludeModifierRegionFlags
        {
            get { return this.excludeModifierRegionFlags; }
            set
            {
                if (!value.Equals(this.excludeModifierRegionFlags))
                {
                    this.excludeModifierRegionFlags = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(13)]
        public FlagList CASFlagList
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

        [ElementPriority(14)]
        public uint DeprecatedPrice
        {
            get { return this.deprecatedPrice; }
            set
            {
                if (!value.Equals(this.deprecatedPrice))
                {
                    this.deprecatedPrice = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(15)]
        public uint PartTitleKey
        {
            get { return this.partTitleKey; }
            set
            {
                if (!value.Equals(this.partTitleKey))
                {
                    this.partTitleKey = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(16)]
        public uint PartDescriptionKey
        {
            get { return this.partDescriptionKey; }
            set
            {
                if (!value.Equals(this.partDescriptionKey))
                {
                    this.partDescriptionKey = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(17)]
        public uint CreationDescriptionKey
        {
            get { return this.createDescriptionKey; }
            set
            {
                if (!value.Equals(this.createDescriptionKey))
                {
                    this.createDescriptionKey = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(18)]
        public byte UniqueTextureSpace
        {
            get { return this.uniqueTextureSpace; }
            set
            {
                if (!value.Equals(this.uniqueTextureSpace))
                {
                    this.uniqueTextureSpace = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(19)]
        public BodyType BodyType
        {
            get { return this.bodyType; }
            set
            {
                if (!value.Equals(this.bodyType))
                {
                    this.bodyType = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(20)]
        public BodySubType BodySubType
        {
            get { return this.bodySubType; }
            set
            {
                if (!value.Equals(this.bodySubType))
                {
                    this.bodySubType = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(21)]
        public AgeGenderFlags AgeGender
        {
            get { return this.ageGender; }
            set
            {
                if (!value.Equals(this.ageGender))
                {
                    this.ageGender = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(22)]
        public Species Species
        {
            get { return this.species; }
            set
            {
                if (!value.Equals(this.species))
                {
                    this.species = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(23)]
        public short PackID
        {
            get { return this.packID; }
            set
            {
                if (!value.Equals(this.packID))
                {
                    this.packID = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(24)]
        public PackFlag PackFlags
        {
            get { return this.packFlags; }
            set
            {
                if (!value.Equals(this.packFlags))
                {
                    this.packFlags = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(25)]
        public byte[] Reserved2
        {
            get { return this.reserved2; }
            set
            {
                if (!value.Equals(this.reserved2)) this.reserved2 = value;
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(26)]
        public byte Unused2
        {
            get { return this.unused2; }
            set
            {
                if (!value.Equals(this.unused2))
                {
                    this.unused2 = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(27)]
        public byte Unused3
        {
            get { return this.unused3; }
            set
            {
                if (!value.Equals(this.unused3))
                {
                    this.unused3 = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(28)]
        public SwatchColorList SwatchColorCode
        {
            get { return this.swatchColorCode; }
            set
            {
                if (!this.swatchColorCode.Equals(value))
                {
                    this.swatchColorCode = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(29), TGIBlockListContentField("TGIList")]
        public byte BuffResKey
        {
            get { return this.buffResKey; }
            set
            {
                if (!value.Equals(this.buffResKey))
                {
                    this.buffResKey = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(30), TGIBlockListContentField("TGIList")]
        public byte VarientThumbnailKey
        {
            get { return this.varientThumbnailKey; }
            set
            {
                if (!value.Equals(this.varientThumbnailKey))
                {
                    this.varientThumbnailKey = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(31)]
        public ulong VoiceEffectHash
        {
            get { return this.voiceEffectHash; }
            set
            {
                if (!value.Equals(this.voiceEffectHash))
                {
                    this.voiceEffectHash = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(32)]
        public uint MaterialSetUpperBodyHash
        {
            get { return this.materialSetUpperBodyHash; }
            set
            {
                if (!value.Equals(this.materialSetUpperBodyHash))
                {
                    this.materialSetUpperBodyHash = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(33)]
        public uint MaterialSetLowerBodyHash
        {
            get { return this.materialSetLowerBodyHash; }
            set
            {
                if (!value.Equals(this.materialSetLowerBodyHash))
                {
                    this.materialSetLowerBodyHash = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(34)]
        public uint MaterialSetShoesHash
        {
            get { return this.materialSetShoesHash; }
            set
            {
                if (!value.Equals(this.materialSetShoesHash))
                {
                    this.materialSetShoesHash = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(35)]
        public OccultTypesDisabled HideForOccultFlags
        {
            get { return this.hideForOccultFlags; }
            set
            {
                if (!value.Equals(this.hideForOccultFlags))
                {
                    this.hideForOccultFlags = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(36)]
        public ulong OppositeGenderCASPart
        {
            get { return this.oppositeGenderPart; }
            set
            {
                if (!value.Equals(this.oppositeGenderPart))
                {
                    this.oppositeGenderPart = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(37)]
        public ulong FallbackCASPart
        {
            get { return this.fallbackPart; }
            set
            {
                if (!value.Equals(this.fallbackPart))
                {
                    this.fallbackPart = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(38)]
        public OpacitySettings OpacitySlider
        {
            get { return this.opacitySlider; }
            set
            {
                if (!value.Equals(this.opacitySlider))
                {
                    this.opacitySlider = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(39)]
        public SliderSettings HueSlider
        {
            get { return this.hueSlider; }
            set
            {
                if (!value.Equals(this.hueSlider))
                {
                    this.hueSlider = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(40)]
        public SliderSettings SaturationSlider
        {
            get { return this.saturationSlider; }
            set
            {
                if (!value.Equals(this.saturationSlider))
                {
                    this.saturationSlider = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(41)]
        public SliderSettings BrightnessSlider
        {
            get { return this.brightnessSlider; }
            set
            {
                if (!value.Equals(this.brightnessSlider))
                {
                    this.brightnessSlider = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(42), TGIBlockListContentField("TGIList")]
        public byte NakedKey
        {
            get { return this.nakedKey; }
            set
            {
                if (!value.Equals(this.nakedKey))
                {
                    this.nakedKey = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(43), TGIBlockListContentField("TGIList")]
        public byte ParentKey
        {
            get { return this.parentKey; }
            set
            {
                if (!value.Equals(this.parentKey))
                {
                    this.parentKey = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(44)]
        public int SortLayer
        {
            get { return this.sortLayer; }
            set
            {
                if (!value.Equals(this.sortLayer))
                {
                    this.sortLayer = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(45)]
        public LODBlockList LodBlockList
        {
            get { return this.lodBlockList; }
            set
            {
                if (!this.lodBlockList.Equals(value))
                {
                    this.lodBlockList = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(46), TGIBlockListContentField("TGIList")]
        public SimpleList<byte> SlotKey
        {
            get { return this.slotKey; }
            set
            {
                if (!value.Equals(this.slotKey))
                {
                    this.slotKey = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(47), TGIBlockListContentField("TGIList")]
        public byte DiffuseKey
        {
            get { return this.diffuseKey; }
            set
            {
                if (!value.Equals(this.diffuseKey))
                {
                    this.diffuseKey = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(48), TGIBlockListContentField("TGIList")]
        public byte ShadowKey
        {
            get { return this.shadowKey; }
            set
            {
                if (!value.Equals(this.shadowKey))
                {
                    this.shadowKey = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(49)]
        public byte CompositionMethod
        {
            get { return this.compositionMethod; }
            set
            {
                if (!value.Equals(this.compositionMethod))
                {
                    this.compositionMethod = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(50), TGIBlockListContentField("TGIList")]
        public byte RegionMapKey
        {
            get { return this.regionMapKey; }
            set
            {
                if (!value.Equals(this.regionMapKey))
                {
                    this.regionMapKey = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(51)]
        public OverrideList Overrides
        {
            get { return this.overrides; }
            set
            {
                if (!value.Equals(this.overrides))
                {
                    this.overrides = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(52), TGIBlockListContentField("TGIList")]
        public byte NormalMapKey
        {
            get { return this.normalMapKey; }
            set
            {
                if (!value.Equals(this.normalMapKey))
                {
                    this.normalMapKey = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(53), TGIBlockListContentField("TGIList")]
        public byte SpecularMapKey
        {
            get { return this.specularMapKey; }
            set
            {
                if (!value.Equals(this.specularMapKey))
                {
                    this.specularMapKey = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(54)]
        public BodyType SharedUVMapSpace
        {
            get
            {
                if (this.version < 0x1B)
                {
                    throw new InvalidOperationException("Version not supported");
                }
                return this.sharedUVMapSpace;
            }
            set
            {
                if (this.version < 0x1B)
                {
                    throw new InvalidOperationException("Version not Supported");
                }
                this.sharedUVMapSpace = value;
            }
        }

        [ElementPriority(55), TGIBlockListContentField("TGIList")]
        public byte EmissionMapKey
        {
            get { return this.emissionMapKey; }
            set
            {
                if (!value.Equals(this.emissionMapKey))
                {
                    this.emissionMapKey = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }
        [ElementPriority(56)]
        public byte ReservedByte
        {
            get { return this.reservedByte; }
            set
            {
                if (!value.Equals(this.reservedByte))
                {
                    this.reservedByte = value;
                }
                this.OnResourceChanged(this, EventArgs.Empty);
            }
        }

        [ElementPriority(57)]
        public CountedTGIBlockList TGIList
        {
            get { return this.tgiList; }
            set
            {
                if (!value.Equals(this.tgiList))
                {
                    this.OnResourceChanged(this, EventArgs.Empty);
                    this.tgiList = value;
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
                if (this.version < 0x1B)
                {
                    res.Remove("SharedUVMapSpace");
                }
                if (this.version < 0x1C)
                {
                    res.Remove("VoiceEffectHash");
                }
                if (this.version < 0x1E)
                {
                    res.Remove("MaterialSetUpperBodyHash");
                    res.Remove("MaterialSetLowerBodyHash");
                    res.Remove("MaterialSetShoesHash");
                    res.Remove("EmissionMapKey");
                }
                if (this.version < 0x1F)
                {
                    res.Remove("OccultBitField");
                }
                if (this.version < 0x20)
                {
                    res.Remove("Reserved1");
                }
                if (this.version < 0x25)
                {
                    res.Remove("PackID");
                    res.Remove("PackFlags");
                    res.Remove("Reserved2");
                }
                else
                {
                    res.Remove("Unused2");
                    res.Remove("Unused3");
                }
                if (this.version < 0x28)
                {
                    res.Remove("ParameterFlags2");
                    res.Remove("OppositeGenderPart");
                    res.Remove("FallbackPart");
                }
                if (this.version < 0x29)
                {
                    res.Remove("ExcludePartFlags2");
                }
                if (this.version < 0x2A)
                {
                    res.Remove("ReservedByte");
                }
                if (this.version < 0x2B)
                {
                    res.Remove("CreationDescriptionKey");
                }
                if (this.version < 0x2C)
                {
                    res.Remove("OpacitySlider");
                    res.Remove("HueSlider");
                    res.Remove("SaturationSlider");
                    res.Remove("BrightnessSlider");
                }
                return res;
            }
        }

        #endregion

    }

    public class CASPartResourceHandler : AResourceHandler
    {
        public CASPartResourceHandler()
        {
            if (Settings.IsTS4)
            {
                this.Add(typeof(CASPartResource), new List<string>(new[] { "0x034AEECB" }));
            }
        }
    }
}
