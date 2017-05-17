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
using s4pi.Interfaces;

namespace LotDescriptionResource
{
    public class LotDescriptionResource : AResource
    {
        static bool checking = s4pi.Settings.Settings.Checking;
        const Int32 recommendedApiVersion = 1;

        #region Attributes
        UInt32 version = 9;

        UInt64 worldDescriptionInstanceId; // World this lot belongs to   <format=hex>
        UInt32 lotId;//                        <format=hex>
        UInt32 simoleonPrice;
        SByte lotSizeX;
        SByte lotSizeZ;
        SByte isEditable;

        UInt64 ambienceFileInstanceId;//       <format=hex>
        Byte enabledForAutoTest;

        Byte hasOverrideAmbience;
        UInt64 audioEffectFileInstanceId;//    <format=hex>

        Byte disableBuildBuy;
        Byte hideFromLotPicker;

        UInt32 buildingNameKey;//              <format=hex>

        Vertex cameraPos;
        Vertex cameraTarget;

        UInt64 lotRequirementsVenue;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of the resource
        /// </summary>
        /// <param name="APIversion">Requested API version</param>
        /// <param name="s">Data stream to use, or null to create from scratch</param>
        public LotDescriptionResource(int APIversion, Stream s) : base(APIversion, s) { if (stream == null) { stream = UnParse(); dirty = true; } stream.Position = 0; Parse(stream); }
        #endregion

        #region AApiVersionedFields
        public override int RecommendedApiVersion { get { return recommendedApiVersion; } }

        public override List<string> ContentFields { get { return GetContentFields(requestedApiVersion, this.GetType()); } }
        #endregion

        #region Data I/O
        void Parse(Stream s)
        {
            BinaryReader br = new BinaryReader(s);

            version = br.ReadUInt32();
            if (checking) if (version != 9)
                    throw new InvalidDataException(String.Format("{0}: unsupported 'version'.  Read '0x{1:X8}', supported: '0x00000009'", this.GetType().Name, version));

            worldDescriptionInstanceId = br.ReadUInt64();
            lotId = br.ReadUInt32();//                        <format=hex>
            simoleonPrice = br.ReadUInt32();
            lotSizeX = br.ReadSByte();
            lotSizeZ = br.ReadSByte();
            isEditable = br.ReadSByte();

            ambienceFileInstanceId = br.ReadUInt64();//       <format=hex>
            enabledForAutoTest = br.ReadByte();

            hasOverrideAmbience = br.ReadByte();
            audioEffectFileInstanceId = br.ReadUInt64();//    <format=hex>

            disableBuildBuy = br.ReadByte();
            hideFromLotPicker = br.ReadByte();

            buildingNameKey = br.ReadUInt32();//              <format=hex>

            cameraPos = new Vertex(recommendedApiVersion, OnResourceChanged, s);
            cameraTarget = new Vertex(recommendedApiVersion, OnResourceChanged, s);

            lotRequirementsVenue = br.ReadUInt64();
        }

        protected override Stream UnParse()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(version);

            bw.Write(worldDescriptionInstanceId);
            bw.Write(lotId);//                        <format=hex>
            bw.Write(simoleonPrice);
            bw.Write(lotSizeX);
            bw.Write(lotSizeZ);
            bw.Write(isEditable);

            bw.Write(ambienceFileInstanceId);//       <format=hex>
            bw.Write(enabledForAutoTest);

            bw.Write(hasOverrideAmbience);
            bw.Write(audioEffectFileInstanceId);//    <format=hex>

            bw.Write(disableBuildBuy);
            bw.Write(hideFromLotPicker);

            bw.Write(buildingNameKey);//              <format=hex>

            cameraPos.UnParse(ms);
            cameraTarget.UnParse(ms);

            bw.Write(lotRequirementsVenue);
            
            bw.Flush();
            return ms;
        }
        #endregion

        #region Content Fields
        [MinimumVersion(1)]
        [MaximumVersion(recommendedApiVersion)]
        [ElementPriority(1)]
        public UInt32 Version { get { return version; } set { if (version != value) { version = value; OnResourceChanged(this, EventArgs.Empty); } } }

        [ElementPriority(2)]
        public UInt64 WorldDescriptionInstanceId { get { return worldDescriptionInstanceId; } set { if (worldDescriptionInstanceId != value) { worldDescriptionInstanceId = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(3)]
        public UInt32 LotId { get { return lotId; } set { if (lotId != value) { lotId = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(4)]
        public UInt32 SimoleonPrice { get { return simoleonPrice; } set { if (simoleonPrice != value) { simoleonPrice = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(5)]
        public SByte LotSizeX { get { return lotSizeX; } set { if (lotSizeX != value) { lotSizeX = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(6)]
        public SByte LotSizeZ { get { return lotSizeZ; } set { if (lotSizeZ != value) { lotSizeZ = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(7)]
        public SByte IsEditable { get { return isEditable; } set { if (isEditable != value) { isEditable = value; OnResourceChanged(this, EventArgs.Empty); } } }

        [ElementPriority(8)]
        public UInt64 AmbienceFileInstanceId { get { return ambienceFileInstanceId; } set { if (ambienceFileInstanceId != value) { ambienceFileInstanceId = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(9)]
        public Byte EnabledForAutoTest { get { return enabledForAutoTest; } set { if (enabledForAutoTest != value) { enabledForAutoTest = value; OnResourceChanged(this, EventArgs.Empty); } } }

        [ElementPriority(10)]
        public Byte HasOverrideAmbience { get { return hasOverrideAmbience; } set { if (hasOverrideAmbience != value) { hasOverrideAmbience = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(11)]
        public UInt64 AudioEffectFileInstanceId { get { return audioEffectFileInstanceId; } set { if (audioEffectFileInstanceId != value) { audioEffectFileInstanceId = value; OnResourceChanged(this, EventArgs.Empty); } } }

        [ElementPriority(12)]
        public Byte DisableBuildBuy { get { return disableBuildBuy; } set { if (disableBuildBuy != value) { disableBuildBuy = value; OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(13)]
        public Byte HideFromLotPicker { get { return hideFromLotPicker; } set { if (hideFromLotPicker != value) { hideFromLotPicker = value; OnResourceChanged(this, EventArgs.Empty); } } }

        [ElementPriority(14)]
        public UInt32 BuildingNameKey { get { return buildingNameKey; } set { if (buildingNameKey != value) { buildingNameKey = value; OnResourceChanged(this, EventArgs.Empty); } } }

        [ElementPriority(15)]
        public Vertex CameraPos { get { return cameraPos; } set { if (!cameraPos.Equals(value)) { cameraPos = new Vertex(requestedApiVersion, OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }
        [ElementPriority(16)]
        public Vertex CameraTarget { get { return cameraTarget; } set { if (!cameraTarget.Equals(value)) { cameraTarget = new Vertex(requestedApiVersion, OnResourceChanged, value); OnResourceChanged(this, EventArgs.Empty); } } }

        [ElementPriority(17)]
        public UInt64 LotRequirementsVenue { get { return lotRequirementsVenue; } set { if (lotRequirementsVenue != value) { lotRequirementsVenue = value; OnResourceChanged(this, EventArgs.Empty); } } }
        #endregion
    }

    public class LotDescriptionResourceHandler : AResourceHandler
    {
        /// <summary>
        /// Create the content of the Dictionary.
        /// </summary>
        public LotDescriptionResourceHandler()
        {
            this.Add(typeof(LotDescriptionResource), new List<string>(new string[] { "0x01942E2C", }));
        }
    }
}
