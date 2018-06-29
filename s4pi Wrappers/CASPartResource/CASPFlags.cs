/***************************************************************************
 *  Copyright (C) 2016 by Sims 4 Tools Development Team                    *
 *  Credits: Peter Jones, Snaitf, Keyi Zhang, Cmar                         *
 *                                                                         *
 *  This file is part of the Sims 4 Package Interface (s4pi)               *
 *                                                                         *
 *  s4pi is free software: you can redistribute it and/or modify           *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  s3pi is distributed in the hope that it will be useful,                *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with s4pi.  If not, see <http://www.gnu.org/licenses/>.          *
 ***************************************************************************/

using System;

namespace CASPartResource
{
    [Flags]
    public enum PackFlag : byte
    {
        HidePackIcon = 1
    }

    [Flags]
    public enum ParmFlag : byte
    {
        DisableForOppositeGender = 1 << 7,
        AllowForLiveRandom = 1 << 6,
        ShowInCASDemo = 1 << 5,
        ShowInSimInfoPanel = 1 << 4,
        ShowInUI = 1 << 3,
        AllowForCASRandom = 1 << 2,
        DefaultThumbnailPart = 1 << 1,
        DefaultForBodyTypeDeprecated = 1
    }
    [Flags]
    public enum ParmFlag2 : byte
    {
        DefaultForBodyTypeFemale = 1 << 2,
        DefaultForBodyTypeMale = 1 << 1,
        DisableForOppositeFrame = 1
    }

    [Flags]
    public enum AgeGenderFlags : uint
    {
        Baby = 0x00000001,
        Toddler = 0x00000002,
        Child = 0x00000004,
        Teen = 0x00000008,
        YoungAdult = 0x00000010,
        Adult = 0x00000020,
        Elder = 0x00000040,
        Male = 0x00001000,
        Female = 0x00002000
    }

    public enum Species : uint
    {
        Human = 1,
        Dog = 2,
        Cat = 3,
        LittleDog = 4
    }

    public enum BodyType : uint
    {
        All = 0,
        Hat = 1,
        Hair = 2,
        Head = 3,
        Face = 4,
        Body = 5,
        Top = 6,
        Bottom = 7,
        Shoes = 8,
        Accessories = 9,
        Earrings = 0x0A,
        Glasses = 0x0B,
        Necklace = 0x0C,
        Gloves = 0x0D,
        BraceletLeft = 0x0E,
        BraceletRight = 0x0F,
        LipRingLeft = 0x10,
        LipRingRight = 0x11,
        NoseRingLeft = 0x12,
        NoseRingRight = 0x13,
        BrowRingLeft = 0x14,
        BrowRingRight = 0x15,
        RingIndexLeft = 0x16,
        RingIndexRight = 0x17,
        RingThirdLeft = 0x18,
        RingThirdRight = 0x19,
        RingMidLeft = 0x1A,
        RingMidRight = 0x1B,
        FacialHair = 0x1C,
        Lipstick = 0x1D,
        Eyeshadow = 0x1E,
        Eyeliner = 0x1F,
        Blush = 0x20,
        Facepaint = 0x21,
        Eyebrows = 0x22,
        Eyecolor = 0x23,
        Socks = 0x24,
        Mascara = 0x25,
        ForeheadCrease = 0x26,
        Freckles = 0x27,
        DimpleLeft = 0x28,
        DimpleRight = 0x29,
        Tights = 0x2A,
        MoleLeftLip = 0x2B,
        MoleRightLip = 0x2C,
        TattooArmLowerLeft = 0x2D,
        TattooArmUpperLeft = 0x2E,
        TattooArmLowerRight = 0x2F,
        TattooArmUpperRight = 0x30,
        TattooLegLeft = 0x31,
        TattooLegRight = 0x32,
        TattooTorsoBackLower = 0x33,
        TattooTorsoBackUpper = 0x34,
        TattooTorsoFrontLower = 0x35,
        TattooTorsoFrontUpper = 0x36,
        MoleLeftCheek = 0x37,
        MoleRightCheek = 0x38,
        MouthCrease = 0x39,
        SkinOverlay = 0x3A,
        Fur = 0x3B,
        AnimalEars = 0x3C,
        Tail = 0x3D,
        NoseColor = 0x3E,
        SecondaryEyeColor = 0x3F,
        OccultBrow = 0x40,
        OccultEyeSocket = 0x41,
        OccultEyeLid = 0x42,
        OccultMouth = 0x43,
        OccultLeftCheek = 0x44,
        OccultRightCheek = 0x45,
        OccultNeckScar = 0x46
    }

    public enum BodySubType : uint
    {
        None = 0,
        EarsUp = 1,
        EarsDown = 2,
        TailLong = 3,
        TailRing = 4,
        TailScrew = 5,
        TailStub = 6
    }

    [Flags]
    public enum OccultTypesDisabled : uint
    {
        Vampire = 1 << 2,
        Alien = 1 << 1,
        Human = 1
    }

    [Flags]
    public enum ExcludePartFlag : ulong
    {
        BODYTYPE_NONE = 0,
        BODYTYPE_HAT = 1ul << 1,
        BODYTYPE_HAIR = 1ul << 2,
        BODYTYPE_HEAD = 1ul << 3,
        BODYTYPE_FACE = 1ul << 4,
        BODYTYPE_FULLBODY = 1ul << 5,
        BODYTYPE_UPPERBODY = 1ul << 6,
        BODYTYPE_LOWERBODY = 1ul << 7,
        BODYTYPE_SHOES = 1ul << 8,
        BODYTYPE_ACCESSORIES = 1ul << 9,
        BODYTYPE_EARRINGS = 1ul << 10,
        BODYTYPE_GLASSES = 1ul << 11,
        BODYTYPE_NECKLACE = 1ul << 12,
        BODYTYPE_GLOVES = 1ul << 13,
        BODYTYPE_WRISTLEFT = 1ul << 14,
        BODYTYPE_WRISTRIGHT = 1ul << 15,
        BODYTYPE_LIPRINGLEFT = 1ul << 16,
        BODYTYPE_LIPRINGRIGHT = 1ul << 17,
        BODYTYPE_NOSERINGLEFT = 1ul << 18,
        BODYTYPE_NOSERINGRIGHT = 1ul << 19,
        BODYTYPE_BROWRINGLEFT = 1ul << 20,
        BODYTYPE_BROWRINGRIGHT = 1ul << 21,
        BODYTYPE_INDEXFINGERLEFT = 1ul << 22,
        BODYTYPE_INDEXFINGERRIGHT = 1ul << 23,
        BODYTYPE_RINGFINGERLEFT = 1ul << 24,
        BODYTYPE_RINGFINGERRIGHT = 1ul << 25,
        BODYTYPE_MIDDLEFINGERLEFT = 1ul << 26,
        BODYTYPE_MIDDLEFINGERRIGHT = 1ul << 27,
        BODYTYPE_FACIALHAIR = 1ul << 28,
        BODYTYPE_LIPSTICK = 1ul << 29,
        BODYTYPE_EYESHADOW = 1ul << 30,
        BODYTYPE_EYELINER = 1ul << 31,
        BODYTYPE_BLUSH = 1ul << 32,
        BODYTYPE_FACEPAINT = 1ul << 33,
        BODYTYPE_EYEBROWS = 1ul << 34,
        BODYTYPE_EYECOLOR = 1ul << 35,
        BODYTYPE_SOCKS = 1ul << 36,
        BODYTYPE_MASCARA = 1ul << 37,
        BODYTYPE_SKINDETAIL_CREASEFOREHEAD = 1ul << 38,
        BODYTYPE_SKINDETAIL_FRECKLES = 1ul << 39,
        BODYTYPE_SKINDETAIL_DIMPLELEFT = 1ul << 40,
        BODYTYPE_SKINDETAIL_DIMPLERIGHT = 1ul << 41,
        BODYTYPE_TIGHTS = 1ul << 42,
        BODYTYPE_SKINDETAIL_MOLELIPLEFT = 1ul << 43,
        BODYTYPE_SKINDETAIL_MOLELIPRIGHT = 1ul << 44,
        BODYTYPE_TATTOO_ARMLOWERLEFT = 1ul << 45,
        BODYTYPE_TATTOO_ARMUPPERLEFT = 1ul << 46,
        BODYTYPE_TATTOO_ARMLOWERRIGHT = 1ul << 47,
        BODYTYPE_TATTOO_ARMUPPERRIGHT = 1ul << 48,
        BODYTYPE_TATTOO_LEGLEFT = 1ul << 49,
        BODYTYPE_TATTOO_LEGRIGHT = 1ul << 50,
        BODYTYPE_TATTOO_TORSOBACKLOWER = 1ul << 51,
        BODYTYPE_TATTOO_TORSOBACKUPPER = 1ul << 52,
        BODYTYPE_TATTOO_TORSOFRONTLOWER = 1ul << 53,
        BODYTYPE_TATTOO_TORSOFRONTUPPER = 1ul << 54,
        BODYTYPE_SKINDETAIL_MOLECHEEKLEFT = 1ul << 55,
        BODYTYPE_SKINDETAIL_MOLECHEEKRIGHT = 1ul << 56,
        BODYTYPE_SKINDETAIL_CREASEMOUTH = 1ul << 57,
        BODYTYPE_SKINOVERLAY = 1ul << 58
    }

    [Flags]
    public enum ExcludePartFlag2 : ulong
    {
        BODYTYPE_OCCULT_BROW = 1,
        BODYTYPE_OCCULT_EYE_SOCKET = 1ul << 1,
        BODYTYPE_OCCULT_EYE_LID = 1ul << 2,
        BODYTYPE_OCCULT_MOUTH = 1ul << 3,
        BODYTYPE_OCCULT_LEFT_CHEEK = 1ul << 4,
        BODYTYPE_OCCULT_RIGHT_CHEEK = 1ul << 5,
        BODYTYPE_OCCULT_NECK_SCAR = 1ul << 6
    }

    [Flags]
    public enum ExcludeModifierRegion : ulong        //used in CASP exclude modifier
    {
        Eyes = 1,
        Nose = 1ul << 1,
        Mouth = 1ul << 2,
        Cheeks = 1ul << 3,
        Chin = 1ul << 4,
        Jaw = 1ul << 5,
        Forehead = 1ul << 6,
        Brows = 1ul << 8,
        Ears = 1ul << 9,
        Head = 1ul << 10,
        FullFace = 1ul << 12,
        Chest = 1ul << 14,
        UpperChest = 1ul << 15,
        Neck = 1ul << 16,
        Shoulders = 1ul << 17,
        UpperArm = 1ul << 18,
        LowerArm = 1ul << 19,
        Hands = 1ul << 20,
        Waist = 1ul << 21,
        Hips = 1ul << 22,
        Belly = 1ul << 23,
        Butt = 1ul << 24,
        Thighs = 1ul << 25,
        LowerLeg = 1ul << 26,
        Feet = 1ul << 27,
        Body = 1ul << 28,
        UpperBody = 1ul << 29,
        LowerBody = 1ul << 30
    }

    [Flags]
    public enum CASPanelGroupType : uint
    {
        Unknown0 = 0,
        Unknown1 = 1,
        HeadAndEars = 2,
        Unknown3 = 4,

        Mouth = 8,
        Nose = 16,
        Unknown6 = 32,
        Eyelash = 64,

        Eyes = 128,
        Unknown9 = 256,
        UnknownA = 512,
        UnknownB = 1024,

        UnknownC = 2048,
        UnknownD = 4096,
        UnknownE = 8192,
        UnknownF = 16384
    }

    [Flags]
    public enum CASPanelSortType : uint
    {
        Unknown0 = 0,
        Unknown1 = 1,
        Unknown2 = 2,
        Unknown3 = 4,

        Unknown4 = 8,
        Unknown5 = 16,
        Unknown6 = 32,
        Unknown7 = 64,

        Unknown8 = 128,
        Unknown9 = 256,
        UnknownA = 512,
        UnknownB = 1024,

        UnknownC = 2048,
        UnknownD = 4096,
        UnknownE = 8192,
        UnknownF = 16384
    }

    public enum CASPartRegion : uint           //used in RegionMap / GEOMListResource
    {
        // "Base" sub-part by definition does not compete with any other subparts of other 
        // parts and is always shown.
        CASPARTREGION_BASE = 0,

        CASPARTREGION_ANKLE,
        CASPARTREGION_CALF,
        CASPARTREGION_KNEE,
        CASPARTREGION_HANDL,
        CASPARTREGION_WRISTL,  // left versions of hand-arm specific regions 
        CASPARTREGION_BICEPL,
        CASPARTREGION_BELTLOW,
        CASPARTREGION_BELTHIGH,
        CASPARTREGION_HAIRHATA,
        CASPARTREGION_HAIRHATB,
        CASPARTREGION_HAIRHATC,
        CASPARTREGION_HAIRHATD,
        CASPARTREGION_NECK,
        CASPARTREGION_CHEST,
        CASPARTREGION_STOMACH,
        CASPARTREGION_HANDR,   // right versions of hand-arm specific regions
        CASPARTREGION_WRISTR,
        CASPARTREGION_BICEPR,
        CASPARTREGION_NECKLACESHADOW, // controls hiding dropshadow for necklaces
        CASPARTREGION_EARSUP,          //20
        CASPARTREGION_EARSDOWN,
        CASPARTREGION_TAIL0,
        CASPARTREGION_TAIL1,
        CASPARTREGION_TAIL2,
        CASPARTREGION_TAIL3
    }

    [Flags]
    public enum ArchetypeFlags : uint       //used in CASPreset
    {
        ARCHETYPE_NONE = 0x00000000,

        ARCHETYPE_CAUCASIAN = 0x00000001,
        ARCHETYPE_AFRICAN = 0x00000002,
        ARCHETYPE_ASIAN = 0x00000004,
        ARCHETYPE_MIDDLE_EASTERN = 0x00000008,
        ARCHETYPE_NATIVE_AMERICAN = 0x00000010,

        ARCHETYPE_ALL = 0xffffffff
    };


    public enum SimRegion : uint        //used in CASPreset
    {
        SIMREGION_EYES = 0,
     //   SIMREGION_FACE_START = SIMREGION_EYES,
        SIMREGION_NOSE,
        SIMREGION_MOUTH,
        SIMREGION_CHEEKS,
        SIMREGION_CHIN,
        SIMREGION_JAW,
        SIMREGION_FOREHEAD,

        // Modifier-only face regions
        SIMREGION_BROWS = 8,
        SIMREGION_EARS,
        SIMREGION_HEAD,

        // Other face regions
        SIMREGION_FULLFACE = 12,
      //  SIMREGION_FACE_END = SIMREGION_FULLFACE,

        // Modifier body regions
        SIMREGION_CHEST = 14,
     //   SIMREGION_BODY_START = SIMREGION_CHEST,
        SIMREGION_UPPERCHEST,
        SIMREGION_NECK,
        SIMREGION_SHOULDERS,
        SIMREGION_UPPERARM,
        SIMREGION_LOWERARM,
        SIMREGION_HANDS,
        SIMREGION_WAIST,
        SIMREGION_HIPS,
        SIMREGION_BELLY,
        SIMREGION_BUTT,
        SIMREGION_THIGHS,
        SIMREGION_LOWERLEG,
        SIMREGION_FEET,

        // Other body regions
        SIMREGION_BODY,
        SIMREGION_UPPERBODY,
        SIMREGION_LOWERBODY,
      //  SIMREGION_BODY_END = SIMREGION_LOWERBODY,   // body end

        SIMREGION_ALL = SIMREGION_LOWERBODY + 1,     // all

        SIMREGION_INVALID = 32
    };
}
