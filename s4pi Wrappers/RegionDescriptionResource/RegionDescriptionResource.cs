/***************************************************************************
 *  Copyright (C) 2017 by Peter L Jones                                    *
 *  pljones@users.sf.net                                                   *
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
using System.Xml;
using System.Linq;
using s4pi.Interfaces;

namespace RegionDescriptionResource
{
    public class RegionDescriptionResource : AResource
    {
        static bool checking = s4pi.Settings.Settings.Checking;
        const Int32 recommendedApiVersion = 1;

        #region Attributes
        UInt32 version;

        UInt32 regionNameKey;//                <format=hex>
        UInt32 regionDescriptionKey;//         <format=hex>
        UInt32 simoleonPrice;
        UInt32 mDevCategoryFlags;//            <format=hex>
        UInt64 regionImageFileNameHash;//      <format=hex>
        UInt64 ambienceFileInstanceId;//       <format=hex>

        Single thumbnailCameraPitch;
        Single thumbnailCameraYaw;
        Single thumbnailCameraWidth;
        Byte thumbnailTimeOfDayHour;
        Byte thumbnailTimeOfDayMinute;

        UInt64 regionThumbnailFileNameHash;//  <format=hex>
        UInt64 mapBackgroundFileNameHash;//    <format=hex>

        Single lightRotationOffset;

        Byte isDestinationWorld;
        Byte isPlayerFacing;

        ParallaxLayerImageFile parallaxLayerImageFile;//[5] <format=hex>
        UInt64 overlayImageFileNameHash;// <format=hex>

        UInt64 gfxInstance;            //version 11
        SimpleList<UInt64> hholdDescInstance;      //version 11
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of the resource
        /// </summary>
        /// <param name="APIversion">Requested API version</param>
        /// <param name="s">Data stream to use, or null to create from scratch</param>
        public RegionDescriptionResource(int APIversion, Stream s) : base(APIversion, s) { if (stream == null) { stream = UnParse(); dirty = true; } stream.Position = 0; Parse(stream); }
        #endregion

        #region AApiVersionedFields
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

        #endregion

        #region Data I/O
        void Parse(Stream s)
        {
            BinaryReader br = new BinaryReader(s);

            version = br.ReadUInt32();
          //  if (checking) if (version != 9)
          //          throw new InvalidDataException(String.Format("{0}: unsupported 'version'.  Read '0x{1:X8}', supported: '0x00000009'", this.GetType().Name, version));

            regionNameKey = br.ReadUInt32();//                <format=hex>
            regionDescriptionKey = br.ReadUInt32();//         <format=hex>
            simoleonPrice = br.ReadUInt32();
            mDevCategoryFlags = br.ReadUInt32();//            <format=hex>
            regionImageFileNameHash = br.ReadUInt64();//      <format=hex>
            ambienceFileInstanceId = br.ReadUInt64();//       <format=hex>

            thumbnailCameraPitch = br.ReadSingle();
            thumbnailCameraYaw = br.ReadSingle();
            thumbnailCameraWidth = br.ReadSingle();
            thumbnailTimeOfDayHour = br.ReadByte();
            thumbnailTimeOfDayMinute = br.ReadByte();

            regionThumbnailFileNameHash = br.ReadUInt64();//  <format=hex>
            mapBackgroundFileNameHash = br.ReadUInt64();//    <format=hex>

            lightRotationOffset = br.ReadSingle();

            if (version > 7)
            {
                isDestinationWorld = br.ReadByte();
                isPlayerFacing = br.ReadByte();

                parallaxLayerImageFile = new ParallaxLayerImageFile(requestedApiVersion, OnResourceChanged, s);
                overlayImageFileNameHash = br.ReadUInt64();// <format=hex>
            }

            if (version >= 11)
            {
                gfxInstance = br.ReadUInt64();
                byte count = br.ReadByte();
                hholdDescInstance = new SimpleList<ulong>(null);
                for (int i = 0; i < count; i++) hholdDescInstance.Add(br.ReadUInt64());
            }
        }

        protected override Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(version);

            bw.Write(regionNameKey);
            bw.Write(regionDescriptionKey);
            bw.Write(simoleonPrice);
            bw.Write(mDevCategoryFlags);
            bw.Write(regionImageFileNameHash);
            bw.Write(ambienceFileInstanceId);

            bw.Write(thumbnailCameraPitch);
            bw.Write(thumbnailCameraYaw);
            bw.Write(thumbnailCameraWidth);
            bw.Write(thumbnailTimeOfDayHour);
            bw.Write(thumbnailTimeOfDayMinute);

            bw.Write(regionThumbnailFileNameHash);
            bw.Write(mapBackgroundFileNameHash);

            bw.Write(lightRotationOffset);

            if (version > 7)
            {
                bw.Write(isDestinationWorld);
                bw.Write(isPlayerFacing);

                if (parallaxLayerImageFile == null) parallaxLayerImageFile = new ParallaxLayerImageFile(1, null);
                parallaxLayerImageFile.UnParse(ms);
                bw.Write(overlayImageFileNameHash);
            }

            if (version >= 11)
            {
                bw.Write(this.gfxInstance);
                if (hholdDescInstance == null) hholdDescInstance = new SimpleList<ulong>(null);
                byte count = (byte)this.hholdDescInstance.Count;
                bw.Write(count);
                for (int i = 0; i < count; i++) bw.Write(hholdDescInstance[i]);
            }
            
            bw.Flush();
            return ms;
        }
        #endregion

        #region Sub-types
        public class ParallaxLayerImageFile : AHandlerElement, IEquatable<ParallaxLayerImageFile>
        {
            #region Attributes
            UInt64 hash_1;
            UInt64 hash_2;
            UInt64 hash_3;
            UInt64 hash_4;
            UInt64 hash_5;
            #endregion

            #region Constructors
            public ParallaxLayerImageFile(int apiVersion, EventHandler handler) : this(apiVersion, handler, 0, 0, 0, 0, 0) { }
            public ParallaxLayerImageFile(int apiVersion, EventHandler handler, ParallaxLayerImageFile basis) : this(apiVersion, handler, basis.hash_1, basis.hash_2, basis.hash_3, basis.hash_4, basis.hash_5) { }
            public ParallaxLayerImageFile(int apiVersion, EventHandler handler, UInt64 hash_1, UInt64 hash_2, UInt64 hash_3, UInt64 hash_4, UInt64 hash_5) : base(apiVersion, handler)
            {
                this.hash_1 = hash_1;
                this.hash_2 = hash_2;
                this.hash_3 = hash_3;
                this.hash_4 = hash_4;
                this.hash_5 = hash_5;
            }
            public ParallaxLayerImageFile(int APIversion, EventHandler handler, Stream s) : base(APIversion, handler) { Parse(s); }
            #endregion

            #region Data I/O
            void Parse(Stream s)
            {
                BinaryReader r = new BinaryReader(s);

                hash_1 = r.ReadUInt64();
                hash_2 = r.ReadUInt64();
                hash_3 = r.ReadUInt64();
                hash_4 = r.ReadUInt64();
                hash_5 = r.ReadUInt64();
            }

            internal void UnParse(Stream s)
            {
                BinaryWriter w = new BinaryWriter(s);

                w.Write(hash_1);
                w.Write(hash_2);
                w.Write(hash_3);
                w.Write(hash_4);
                w.Write(hash_5);
            }
            #endregion

            #region AHandlerElement Members
            public override int RecommendedApiVersion { get { return recommendedApiVersion; } }
            public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
            #endregion

            #region IEquatable<Obj> Members

            public bool Equals(ParallaxLayerImageFile other)
            {
                return hash_1.Equals(other.hash_1)
                    && hash_2.Equals(other.hash_2)
                    && hash_3.Equals(other.hash_3)
                    && hash_4.Equals(other.hash_4)
                    && hash_5.Equals(other.hash_5)
                    ;
            }

            public override bool Equals(object obj)
            {
                return obj as ParallaxLayerImageFile != null ? this.Equals(obj as ParallaxLayerImageFile) : false;
            }

            public override int GetHashCode()
            {
                return hash_1.GetHashCode()
                    ^ hash_2.GetHashCode()
                    ^ hash_3.GetHashCode()
                    ^ hash_4.GetHashCode()
                    ^ hash_5.GetHashCode()
                    ;
            }

            #endregion

            #region Content Fields
            [MinimumVersion(1)]
            [MaximumVersion(recommendedApiVersion)]
            [ElementPriority(1)]
            public UInt64 Hash_1 { get { return hash_1; } set { if (hash_1 != value) { hash_1 = value; OnElementChanged(); } } }
            [ElementPriority(2)]
            public UInt64 Hash_2 { get { return hash_2; } set { if (hash_2 != value) { hash_2 = value; OnElementChanged(); } } }
            [ElementPriority(3)]
            public UInt64 Hash_3 { get { return hash_3; } set { if (hash_3 != value) { hash_3 = value; OnElementChanged(); } } }
            [ElementPriority(4)]
            public UInt64 Hash_4 { get { return hash_4; } set { if (hash_4 != value) { hash_4 = value; OnElementChanged(); } } }
            [ElementPriority(5)]
            public UInt64 Hash_5 { get { return hash_5; } set { if (hash_5 != value) { hash_5 = value; OnElementChanged(); } } }
            #endregion

            public string Value { get { return ValueBuilder; } }
        }

        #endregion

        #region Content Fields
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        [ElementPriority(1)]
        public UInt32 Version { get { return version; } set { if (version != value) { version = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(2)]
        public UInt32 RegionNameKey { get { return regionNameKey; } set { if (regionNameKey != value) { regionNameKey = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(3)]
        public UInt32 RegionDescriptionKey { get { return regionDescriptionKey; } set { if (regionDescriptionKey != value) { regionDescriptionKey = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(4)]
        public UInt32 SimoleonPrice { get { return simoleonPrice; } set { if (simoleonPrice != value) { simoleonPrice = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(5)]
        public UInt32 MDevCategoryFlags { get { return mDevCategoryFlags; } set { if (mDevCategoryFlags != value) { mDevCategoryFlags = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(6)]
        public UInt64 RegionImageFileNameHash { get { return regionImageFileNameHash; } set { if (regionImageFileNameHash != value) { regionImageFileNameHash = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(7)]
        public UInt64 AmbienceFileInstanceId { get { return ambienceFileInstanceId; } set { if (ambienceFileInstanceId != value) { ambienceFileInstanceId = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(8)]
        public Single ThumbnailCameraPitch { get { return thumbnailCameraPitch; } set { if (thumbnailCameraPitch != value) { thumbnailCameraPitch = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(9)]
        public Single ThumbnailCameraYaw { get { return thumbnailCameraYaw; } set { if (thumbnailCameraYaw != value) { thumbnailCameraYaw = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(10)]
        public Single ThumbnailCameraWidth { get { return thumbnailCameraWidth; } set { if (thumbnailCameraWidth != value) { thumbnailCameraWidth = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(11)]
        public Byte ThumbnailTimeOfDayHour { get { return thumbnailTimeOfDayHour; } set { if (thumbnailTimeOfDayHour != value) { thumbnailTimeOfDayHour = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(12)]
        public Byte ThumbnailTimeOfDayMinute { get { return thumbnailTimeOfDayMinute; } set { if (thumbnailTimeOfDayMinute != value) { thumbnailTimeOfDayMinute = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(13)]
        public UInt64 RegionThumbnailFileNameHash { get { return regionThumbnailFileNameHash; } set { if (regionThumbnailFileNameHash != value) { regionThumbnailFileNameHash = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(14)]
        public UInt64 MapBackgroundFileNameHash { get { return mapBackgroundFileNameHash; } set { if (mapBackgroundFileNameHash != value) { mapBackgroundFileNameHash = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(15)]
        public Single LightRotationOffset { get { return lightRotationOffset; } set { if (lightRotationOffset != value) { lightRotationOffset = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(16)]
        public Boolean IsDestinationWorld { get { return isDestinationWorld != 0; } set { if (IsDestinationWorld != value) { isDestinationWorld = (Byte)(value ? -1 : 0); OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(17)]
        public Boolean IsPlayerFacing { get { return isPlayerFacing != 0; } set { if (IsPlayerFacing != value) { isPlayerFacing = (Byte)(value ? -1 : 0); OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(18)]
        public ParallaxLayerImageFile ParallaxLayerImageFileNameHash { get { return parallaxLayerImageFile; } set { if (!parallaxLayerImageFile.Equals(value)) { parallaxLayerImageFile = new ParallaxLayerImageFile(requestedApiVersion, OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(19)]
        public UInt64 OverlayImageFileNameHash { get { return overlayImageFileNameHash; } set { if (overlayImageFileNameHash != value) { overlayImageFileNameHash = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(20)]
        public UInt64 MapFlashInstanceID { get { return gfxInstance; } set { if (gfxInstance != value) { gfxInstance = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(21)]
        public SimpleList<UInt64> HouseHoldDescriptionInstanceID { get { return hholdDescInstance; } set { if (hholdDescInstance != value) { hholdDescInstance = value; OnResourceChanged(this, EventArgs.Empty); } } }
        #endregion

        public string Value { get { return this.ValueBuilder; } }

        public override List<string> ContentFields
        {
            get
            {
                var res = base.ContentFields;
                if (this.version <= 7)
                {
                    res.Remove("IsDestinationWorld");
                    res.Remove("IsPlayerFacing");
                    res.Remove("ParallaxLayerImageFileNameHash");
                    res.Remove("OverlayImageFileNameHash");
                }
                if (this.version < 11)
                {
                    res.Remove("MapFlashInstanceID");
                    res.Remove("HouseHoldDescriptionInstanceID");
                }
                return res;
            }
        }
    }

    public class RegionDescriptionResourceHandler : AResourceHandler
    {
        /// <summary>
        /// Create the content of the Dictionary.
        /// </summary>
        public RegionDescriptionResourceHandler()
        {
            this.Add(typeof(RegionDescriptionResource), new List<string>(new string[] { "0xD65DAFF9", }));
        }
    }
}
