/***************************************************************************
 *  Copyright (C) 2016 by Cmar, Peter Jones                                *
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
using System.Linq;
using s4pi.Interfaces;

namespace s4pi.Animation
{
    public class S3CLIP : AHandlerElement, IEquatable<S3CLIP>
    {
        char[] formatToken;
        uint version;
        uint flags;
        float tickLength;
        ushort numTicks;
        ushort padding;
        string clipName;            //null-terminated string
        string sourceAssetName;     //null-terminated string
        F1PaletteList f1Palette;
        S3ChannelList channels;

        public S3CLIP(int apiVersion, EventHandler handler) : base(apiVersion, handler) 
        {
            this.formatToken = new char[8];
            this.f1Palette = new F1PaletteList(handler);
            this.channels = new S3ChannelList(handler);
        }
        public S3CLIP(int apiVersion, EventHandler handler, Stream s)
            : base(apiVersion, handler)
        {
            this.Parse(s);
        }

        public override int RecommendedApiVersion
        {
            get { return 1; }
        }

        public void Parse(Stream s)
        {
            BinaryReader br = new BinaryReader(s);
            long startPos = s.Position;
            formatToken = br.ReadChars(8);
            version = br.ReadUInt32();
            flags = br.ReadUInt32();
            tickLength = br.ReadSingle();
            numTicks = br.ReadUInt16();
            padding = br.ReadUInt16();
            uint channelCount = br.ReadUInt32();
            uint f1PaletteSize = br.ReadUInt32();
            uint channelDataOffset = br.ReadUInt32();
            uint f1PaletteDataOffset = br.ReadUInt32();
            uint nameOffset = br.ReadUInt32();
            uint sourceAssetNameOffset = br.ReadUInt32();
            long currPos = s.Position;
            s.Position = startPos + nameOffset;
            this.clipName = br.ReadZString();
            s.Position = startPos + sourceAssetNameOffset;
            this.sourceAssetName = br.ReadZString();
            s.Position = startPos + f1PaletteDataOffset;
            this.f1Palette = new F1PaletteList(handler, s, f1PaletteSize);
            s.Position = startPos + channelDataOffset;
            this.channels = new S3ChannelList(handler, s, channelCount, startPos);
            s.Position = startPos;
        }

        public void UnParse(Stream s)
        {
            BinaryWriter bw = new BinaryWriter(s);
            long startPos = s.Position;
            bw.Write(formatToken);
            bw.Write(version);
            bw.Write(flags);
            bw.Write(tickLength);
            bw.Write(numTicks);
            bw.Write(padding);
            if (this.channels == null) this.channels = new S3ChannelList(handler);
            if (this.f1Palette == null) this.f1Palette = new F1PaletteList(handler);
            bw.Write((uint)channels.Count);
            bw.Write((uint)f1Palette.Count);
            long offsetsPos = s.Position;
            bw.Write(0U);           //channel data offset
            bw.Write(0U);           //pallette data offset
            bw.Write(0U);           //name offset
            bw.Write(0U);           //source asset name offset

            long channelPos = s.Position;
            for (int i = 0; i < this.channels.Count; i++)
            {
                this.channels[i].UnParse(s);
            }

            long namePos = s.Position;
            if (this.clipName == null) this.clipName = "";
            bw.WriteZString(clipName);

            long sourcePos = s.Position;
            if (this.sourceAssetName == null) this.sourceAssetName = "";
            bw.WriteZString(sourceAssetName);

            long palettePos = s.Position;
            f1Palette.UnParse(s);

            long channelsPos = channelPos;
            for (int i = 0; i < this.channels.Count; i++)
            {
                long dataPos = s.Position;
                s.Position = channelsPos;
                this.channels[i].UpdateOffset(s, startPos, dataPos);
                channelsPos = s.Position;
                s.Position = dataPos;
                this.channels[i].UnParseFrames(s);
            }
            long currPos = s.Position;

            s.Position = offsetsPos;
            bw.Write((uint)(channelPos - startPos));   //channelDataOffset
            bw.Write((uint)(palettePos - startPos));    //f1PaletteDataOffset
            bw.Write((uint)(namePos - startPos));       //nameOffset
            bw.Write((uint)(sourcePos - startPos));     //sourceAssetNameOffset

            s.Position = currPos;
        }

        public bool Equals(S3CLIP other)
        {
            return (Enumerable.SequenceEqual(this.formatToken, other.formatToken) &&
                    this.version == other.version &&
                    this.flags == other.flags &&
                    this.tickLength == other.tickLength &&
                    this.numTicks == other.numTicks &&
                    this.padding == other.padding &&
                    String.Compare(this.clipName, other.clipName) == 0 &&
                    String.Compare(this.sourceAssetName, other.sourceAssetName) == 0 &&
                    Enumerable.SequenceEqual(this.f1Palette, other.f1Palette) &&
                    Enumerable.SequenceEqual(this.channels, other.channels));
        }

        [ElementPriority(0)]
        public char[] FormatToken
        {
            get { return this.formatToken; }
            set { if (this.formatToken != value) { this.formatToken = value; OnElementChanged(); } }
        }
        [ElementPriority(1)]
        public UInt32 Version
        {
            get { return this.version; }
            set { if (this.version != value) { this.version = value; OnElementChanged(); } }
        }
        [ElementPriority(2)]
        public uint Flags
        {
            get { return this.flags; }
            set { if (this.flags != value) { this.flags = value; OnElementChanged(); } }
        }
        [ElementPriority(3)]
        public float TickLength
        {
            get { return this.tickLength; }
            set { if (this.tickLength != value) { this.tickLength = value; OnElementChanged(); } }
        }
        [ElementPriority(4)]
        public ushort NumTicks
        {
            get { return this.numTicks; }
            set { if (this.numTicks != value) { this.numTicks = value; OnElementChanged(); } }
        }
        [ElementPriority(5)]
        public string ClipName
        {
            get { return this.clipName; }
            set { if (this.clipName != value) { this.clipName = value; OnElementChanged(); } }
        }
        [ElementPriority(6)]
        public string SourceAssetName
        {
            get { return this.sourceAssetName; }
            set { if (this.sourceAssetName != value) { this.sourceAssetName = value; OnElementChanged(); } }
        }
        [ElementPriority(7)]
        public F1PaletteList F1_Palette
        {
            get { return this.f1Palette; }
            set { if (this.f1Palette != value) { this.f1Palette = value; OnElementChanged(); } }
        }
        [ElementPriority(8)]
        public S3ChannelList Channels
        {
            get { return this.channels; }
            set { if (this.channels != value) { this.channels = value; OnElementChanged(); } }
        }

        public override List<string> ContentFields
        {
            get { return GetContentFields(requestedApiVersion, GetType()); }
        }
        public string Value { get { return ValueBuilder; } }
      //  public string Value
      //  {
      //      get
      //      {
      //          string tmp = this.channelDataOffset.ToString() + ", " + this.nameOffset.ToString() + ", " +
      //              this.sourceAssetNameOffset.ToString() + ", " + this.f1PaletteDataOffset.ToString() + Environment.NewLine;
      //          for (int i = 0; i < this.channels.Length; i++)
      //          {
      //              tmp += this.channels[i].DataOffset.ToString() + Environment.NewLine;
      //          }
      //          return tmp;
      //      }
      //  }

        #region sub-types
        public class F1PaletteList : DependentList<F1Palette>
        {
            public F1PaletteList(EventHandler handler)
                : base(handler)
            {
            }

            public F1PaletteList(EventHandler handler, Stream s, uint paletteSize)
                : base(handler)
            {
                this.Parse(s, paletteSize);
            }

            protected void Parse(Stream s, uint paletteSize)
            {
                for (var i = 0; i < paletteSize; i++)
                {
                    this.Add(new F1Palette(1, handler, s));
                }
            }

            public override void UnParse(Stream s)
            {
                foreach (F1Palette p in this)
                {
                    p.UnParse(s);
                }
            }

            protected override F1Palette CreateElement(Stream s)
            {
                return new F1Palette(1, handler, s);
            }

            protected override void WriteElement(Stream s, F1Palette element)
            {
                element.UnParse(s);
            }

        }

        public class F1Palette : AHandlerElement, IEquatable<F1Palette>
        {
            private float palette;

            public float Palette
            {
                get { return palette; }
                set { if (this.palette != value) { this.palette = value; OnElementChanged(); } }
            }

            public string Value
            {
                get { return palette.ToString(); }
            }

            #region AHandlerElement Members

            public override int RecommendedApiVersion
            {
                get { return this.requestedApiVersion; }
            }

            public override List<string> ContentFields
            {
                get { return GetContentFields(this.requestedApiVersion, this.GetType()); }
            }

            #endregion

            public bool Equals(F1Palette other)
            {
                return this.palette == other.palette;
            }

            public F1Palette(int apiVersion, EventHandler handler)
                : base(apiVersion, handler)
            {
            }
            public F1Palette(int apiVersion, EventHandler handler, Stream s)
                : base(apiVersion, handler)
            {
                this.Parse(s);
            }
            public F1Palette(int apiVersion, EventHandler handler, F1Palette basis)
                : base(apiVersion, handler)
            {
                this.palette = basis.palette;
            }

            public void Parse(Stream s)
            {
                BinaryReader br = new BinaryReader(s);
                this.palette = br.ReadSingle();
            }
            public void UnParse(Stream s)
            {
                BinaryWriter bw = new BinaryWriter(s);
                bw.Write(this.palette);
            }
        }

        public class S3ChannelList : DependentList<S3Channel>
        {
            public S3ChannelList(EventHandler handler)
                : base(handler)
            {
            }

            public S3ChannelList(EventHandler handler, Stream s, uint channelCount, long startPos)
                : base(handler)
            {
                this.Parse(s, channelCount, startPos);
            }

            protected void Parse(Stream s, uint channelCount, long startPos)
            {
                for (var i = 0; i < channelCount; i++)
                {
                    this.Add(new S3Channel(1, handler, s, startPos));
                }
            }

            public override void UnParse(Stream s)
            {
                foreach (S3Channel p in this)
                {
                    p.UnParse(s);
                }
            }

            protected override S3Channel CreateElement(Stream s)
            {
                return new S3Channel(1, handler);
            }

            protected override void WriteElement(Stream s, S3Channel element)
            {
                element.UnParse(s);
            }

        }
        
        public class S3Channel : AHandlerElement, IEquatable<S3Channel>
        {            
            public S3Channel(int apiVersion, EventHandler handler)
                : base(apiVersion, handler)
            {
                this.channelType = ChannelType.ChannelType_Unknown;
                this.channelSubTarget = SubTargetType.SubTarget_Unknown;
                this.frames = new FrameList(handler, this);
            }

            public S3Channel(int apiVersion, EventHandler handler, Stream s, long startPos)
                : base(apiVersion, handler)
            {
                Parse(s, startPos);
            }

            public S3Channel(int apiVersion, EventHandler handler, S3Channel basis)
                : this(apiVersion, handler, basis.target, basis.offset, basis.scale, basis.channelType, basis.channelSubTarget, basis.frames)
            {
            }

            public S3Channel(int APIversion, EventHandler handler, uint target, float offset, float scale, ChannelType channelType, SubTargetType channelSubTarget, FrameList frames)
                : base(APIversion, handler)
            {
                this.target = target;
                this.offset = offset;
                this.scale = scale;
                this.channelType = channelType;
                this.channelSubTarget = channelSubTarget;
                this.frames = frames;
            }

            public string Value
            {
                get { return ValueBuilder; }
            }

            uint target;
            float offset;
            float scale;
            ChannelType channelType;
            SubTargetType channelSubTarget;
            FrameList frames;

            [ElementPriority(0)]
            public uint Target
            {
                get { return target; }
                set { if (this.target != value) { this.target = value; OnElementChanged(); } }
            }
            [ElementPriority(1)]
            public float Offset
            {
                get { return offset; }
                set { if (this.offset != value) { this.offset = value; OnElementChanged(); } }
            }
            [ElementPriority(2)]
            public float Scale
            {
                get { return scale; }
                set { if (this.scale != value) { this.scale = value; OnElementChanged(); } }
            }
            [ElementPriority(3)]
            public ChannelType Channel_Type
            {
                get { return channelType; }
                set { if (this.channelType != value) { this.channelType = value; OnElementChanged(); } }
            }
            [ElementPriority(4)]
            public SubTargetType ChannelSubTarget
            {
                get { return channelSubTarget; }
                set { if (this.channelSubTarget != value) { this.channelSubTarget = value; OnElementChanged(); } }
            }
            [ElementPriority(5)]
            public FrameList Frames
            {
                get { return frames; }
                set { if (this.frames != value) { this.frames = value; OnElementChanged(); } }
            }

            public void Parse(Stream s, long startPos)
            {
                var br = new BinaryReader(s);
                uint dataOffset = br.ReadUInt32();
                target = br.ReadUInt32();
                offset = br.ReadSingle();
                scale = br.ReadSingle();
                ushort numFrames = br.ReadUInt16();
                channelType = (ChannelType)br.ReadByte();
                channelSubTarget = (SubTargetType)br.ReadByte();
                long currentPos = s.Position;
                uint width = GetWidthForChannelType(channelType);
                uint count = GetCountForChannelType(channelType);
                s.Position = startPos + dataOffset;
                frames = new FrameList(handler, s, numFrames, this);
                s.Position = currentPos;
            }

            public void UnParse(Stream s)
            {
                var bw = new BinaryWriter(s);
                bw.Write(0U);           //dataOffset
                bw.Write(target);
                bw.Write(offset);
                bw.Write(scale);
                bw.Write((ushort)frames.Count);
                bw.Write((byte)channelType);
                bw.Write((byte)channelSubTarget);
            }
            public void UpdateOffset(Stream s, long startPos, long dataPos)
            {
                var bw = new BinaryWriter(s);
                bw.Write((uint)(dataPos - startPos));           //dataOffset
                s.Position = s.Position + 16;
            }
            public void UnParseFrames(Stream s)
            {
                uint width = GetWidthForChannelType(channelType);
                if (frames == null) frames = new FrameList(handler);
                for (int i = 0; i < frames.Count; i++)
                {
                    frames[i].UnParse(s, width);
                }
            }

            public override int RecommendedApiVersion
            {
                get { return 1; }
            }

            public override List<string> ContentFields
            {
                get { return GetContentFields(requestedApiVersion, GetType()); }
            }

            public bool Equals(S3Channel other)
            {
                return false;
            }
        }

        public class FrameList : DependentList<Frame>
        {
            S3Channel parentChannel;

            public FrameList(EventHandler handler)
                : base(handler)
            {
            }
            public FrameList(EventHandler handler, S3Channel parentChannel)
                : base(handler)
            {
                this.parentChannel = parentChannel;
            }
            public FrameList(EventHandler handler, Stream s, uint numberFrames, S3Channel parentChannel)
                : base(handler)
            {
                this.parentChannel = parentChannel;
                uint width = GetWidthForChannelType(parentChannel.Channel_Type);
                uint count = GetCountForChannelType(parentChannel.Channel_Type);
                this.Parse(s, numberFrames, width, count);
            }

            protected void Parse(Stream s, uint numberFrames, uint width, uint count)
            {
                for (var i = 0; i < numberFrames; i++)
                {
                    this.Add(new Frame(1, handler, s, width, count));
                }
            }

            public void UnParse(Stream s, uint width)
            {
                foreach (Frame f in this)
                {
                    f.UnParse(s, width);
                }
            }

            public override void Add()
            {
                uint width = GetWidthForChannelType(parentChannel.Channel_Type);
                uint count = GetCountForChannelType(parentChannel.Channel_Type);
                base.Add(new Frame(1, handler, width, count));
            }

            protected override Frame CreateElement(Stream s)
            {
                uint width = GetWidthForChannelType(parentChannel.Channel_Type);
                uint count = GetCountForChannelType(parentChannel.Channel_Type);
                return new Frame(1, handler, width, count);
            }

            protected override void WriteElement(Stream s, Frame element)
            {
                throw new NotImplementedException();
            }

            public String Value
            {
                get
                {
                    var sb = new StringBuilder();
                    sb.AppendLine();
                    foreach (var item in this)
                    {
                        sb.AppendLine(item.Value);
                    }
                    sb.AppendLine();
                    return sb.ToString();
                }
            }
        }
        
        public class Frame : AHandlerElement, IEquatable<Frame>
        {
            public Frame(int apiVersion, EventHandler handler, uint width, uint count)
                : base(apiVersion, handler)
            {
                this.indices = new ushort[count];
            }

            public Frame(int apiVersion, EventHandler handler, Stream s, uint width, uint count)
                : base(apiVersion, handler)
            {
                Parse(s, width, count);
            }

            protected Frame(int apiVersion, EventHandler handler, Frame basis)
                : this(apiVersion, handler, basis.startTick, basis.flags, basis.indices)
            {
            }

            protected Frame(int APIversion, EventHandler handler, ushort startTick, ushort flags, ushort[] indices)
                : base(APIversion, handler)
            {
                this.startTick = startTick;
                this.flags = flags;
                this.indices = indices;
            }

            public string Value
            {
                get { return ValueBuilder; }
            }

            ushort startTick;
            ushort flags;
            private ushort[] indices;

            [ElementPriority(0)]
            public ushort StartTick
            {
                get { return startTick; }
                set { if (this.startTick != value) { this.startTick = value; OnElementChanged(); } }
            }
            [ElementPriority(1)]
            public ushort Flags
            {
                get { return flags; }
                set { if (this.flags != value) { this.flags = value; OnElementChanged(); } }
            }
            [ElementPriority(2)]
            public ushort[] Indices
            {
                get { return indices; }
                set { if (this.indices != value) { this.indices = value; OnElementChanged(); } }
            }

            public void Parse(Stream s, uint width, uint count)
            {
                var br = new BinaryReader(s);

                this.startTick = br.ReadUInt16();
                this.flags = br.ReadUInt16();

                switch (width)
                {
                    case 1:
                        this.indices = new ushort[count];
                        for (int i = 0; i < count; i++)
                        {
                            this.indices[i] = br.ReadByte();
                        }
                        if ((4 - count) > 0) br.ReadBytes((int)(4 - count));
                        break;
                    case 2:
                        this.indices = new ushort[count];
                        for (int i = 0; i < count; i++)
                        {
                            this.indices[i] = br.ReadUInt16();
                        }
                        if (count % 2 != 0) br.ReadBytes(2);
                        break;
                    case 10:
                        uint data = br.ReadUInt32();
                        indices = new ushort[3];
                        indices[2] = (ushort)((data << 2) >> 22);
                        indices[1] = (ushort)((data << 12) >> 22);
                        indices[0] = (ushort)((data << 22) >> 22);
                        break;
                    default:
                        this.indices = new ushort[0];
                        break;
                }
            }

            public void UnParse(Stream s, uint width)
            {
                var bw = new BinaryWriter(s);
                bw.Write(this.startTick);
                bw.Write(this.flags);

                switch (width)
                {
                    case 1:
                        for (int i = 0; i < this.indices.Length; i++)
                        {
                            bw.Write((byte)this.indices[i]);
                        }
                        for (int i = 0; i < 4 - this.indices.Length; i++)
                        {
                            bw.Write((byte)0);
                        }
                        break;
                    case 2:
                        for (int i = 0; i < this.indices.Length; i++)
                        {
                            bw.Write(this.indices[i]);
                        }
                        if (this.indices.Length % 2 != 0) bw.Write(new byte[] { 0, 0 });
                        break;
                    case 10:
                        uint data = ((uint)(indices[2] << 20)) + ((uint)(indices[1] << 10)) + ((uint)(indices[0]));
                        bw.Write(data);
                        break;
                    default:
                        break;
                }
            }

            public override int RecommendedApiVersion
            {
                get { return 1; }
            }

            public override List<string> ContentFields
            {
                get { return GetContentFields(requestedApiVersion, GetType()); }
            }

            public bool Equals(Frame other)
            {
                return this.StartTick == other.StartTick && this.Flags == other.Flags && Enumerable.SequenceEqual(this.indices, other.indices);
            }
        }
        #endregion

        #region enums

        public enum ChannelType : byte
        {
            ChannelType_Unknown = 0,
            F1,
            F2,
            F3,
            F4,

            F1_Normalized,
            F2_Normalized,
            F3_Normalized,
            F4_Normalized,

            F1_Zero,
            F2_Zero,
            F3_Zero,
            F4_Zero,

            F1_One,
            F2_One,
            F3_One,
            F4_One,

            F4_QuaternionIdentity,

            F3_HighPrecisionNormalized,
            F4_HighPrecisionNormalized_Quaternion,
            F4_SuperHighPrecision_Quaternion,
            F3_HighPrecisionNormalized_Quaternion,// channel type does not support offset or scale

        }

        public enum SubTargetType : byte
        {
            SubTarget_Unknown = 0,
            Translation,
            Orientation,
            Scale,

            Translation_X,  // UNSUPPORTED
            Translation_Y,  // UNSUPPORTED
            Translation_Z,  // UNSUPPORTED

            Orientation_X,  // UNSUPPORTED
            Orientation_Y,  // UNSUPPORTED
            Orientation_Z,  // UNSUPPORTED
            Orientation_W,  // UNSUPPORTED

            Scale_X,  // UNSUPPORTED
            Scale_Y,  // UNSUPPORTED
            Scale_Z,  // UNSUPPORTED

            IK_TargetWeight_World,
            IK_TargetWeight_1,
            IK_TargetWeight_2,
            IK_TargetWeight_3,
            IK_TargetWeight_4,
            IK_TargetWeight_5,
            IK_TargetWeight_6,
            IK_TargetWeight_7,
            IK_TargetWeight_8,
            IK_TargetWeight_9,
            IK_TargetWeight_10,

            IK_TargetOffset_Translation_World,
            IK_TargetOffset_Orientation_World,
            IK_TargetOffset_Translation_1,
            IK_TargetOffset_Orientation_1,
            IK_TargetOffset_Translation_2,
            IK_TargetOffset_Orientation_2,
            IK_TargetOffset_Translation_3,
            IK_TargetOffset_Orientation_3,
            IK_TargetOffset_Translation_4,
            IK_TargetOffset_Orientation_4,
            IK_TargetOffset_Translation_5,
            IK_TargetOffset_Orientation_5,
            IK_TargetOffset_Translation_6,
            IK_TargetOffset_Orientation_6,
            IK_TargetOffset_Translation_7,
            IK_TargetOffset_Orientation_7,
            IK_TargetOffset_Translation_8,
            IK_TargetOffset_Orientation_8,
            IK_TargetOffset_Translation_9,
            IK_TargetOffset_Orientation_9,
            IK_TargetOffset_Translation_10,
            IK_TargetOffset_Orientation_10,

        }
        #endregion

        #region utilities
        internal static uint GetWidthForChannelType(ChannelType t)
        {
            switch (t)
            {
                case ChannelType.F1_Normalized:
                case ChannelType.F2_Normalized:
                case ChannelType.F3_Normalized:
                case ChannelType.F4_Normalized:
                    return 1;

                case ChannelType.F3_HighPrecisionNormalized:
                case ChannelType.F4_HighPrecisionNormalized_Quaternion:
                case ChannelType.F3_HighPrecisionNormalized_Quaternion:
                    return 10;

                case ChannelType.F1:
                case ChannelType.F2:
                case ChannelType.F3:
                case ChannelType.F4:
                case ChannelType.F4_SuperHighPrecision_Quaternion:
                    return 2;

                case ChannelType.F1_Zero:
                case ChannelType.F2_Zero:
                case ChannelType.F3_Zero:
                case ChannelType.F4_Zero:
                case ChannelType.F1_One:
                case ChannelType.F2_One:
                case ChannelType.F3_One:
                case ChannelType.F4_One:
                case ChannelType.F4_QuaternionIdentity:
                    return 0;

                default:
                    return 0;
            }
        }

        internal static uint GetCountForChannelType(ChannelType t)
        {
            switch (t)
            {
                case ChannelType.F1:
                case ChannelType.F1_Normalized:
                    return 1;

                case ChannelType.F2:
                case ChannelType.F2_Normalized:
                    return 2;

                case ChannelType.F3:
                case ChannelType.F3_Normalized:
                    return 3;

                case ChannelType.F4:
                case ChannelType.F4_Normalized:
                case ChannelType.F4_SuperHighPrecision_Quaternion:
                case ChannelType.F4_HighPrecisionNormalized_Quaternion:
                case ChannelType.F3_HighPrecisionNormalized_Quaternion:
                    return 4;

                case ChannelType.F3_HighPrecisionNormalized:
                    return 3;

                case ChannelType.F1_Zero:
                case ChannelType.F2_Zero:
                case ChannelType.F3_Zero:
                case ChannelType.F4_Zero:
                case ChannelType.F1_One:
                case ChannelType.F2_One:
                case ChannelType.F3_One:
                case ChannelType.F4_One:
                case ChannelType.F4_QuaternionIdentity:
                    return 0;

                default:
                    return 0;
            }
        }
        #endregion
    }
}
