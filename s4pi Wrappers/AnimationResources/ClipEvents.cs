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
    public class ClipEventList : AHandlerList<ClipEvent>, IGenericAdd
    {
        public ClipEventList(EventHandler handler, IEnumerable<ClipEvent> items)
            : base(handler, items)
        {
        }
        public ClipEventList(EventHandler handler)
            : base(handler)
        {
        }

        public void Add()
        {
            throw new NotImplementedException();
        }

        public void Add(Type instanceType)
        {
            base.Add((ClipEvent)Activator.CreateInstance(instanceType, 0, this.handler));
        }

        public String Value
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine();
                for (int i = 0; i < this.Count; i++)
                {
                    sb.AppendLine("--- ClipEvent[" + i.ToString("X2") + "] ---" + Environment.NewLine + this[i].Value);
                }
                sb.AppendLine();
                return sb.ToString();
            }
        } 
    }

    public class ClipEventParent : ClipEvent
    {
        private uint id;
        private byte[] data;

        [ElementPriority(4)]
        public uint NameSpaceHash
        {
            get { return id; }
            set { if (this.id != value) { this.id = value; OnElementChanged(); } }
        }
        [ElementPriority(5)]
        public byte[] Data
        {
            get { return data; }
            set { if (this.data != value) { this.data = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return 4 + (uint)this.data.Length; }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventParent)
            {
                ClipEventParent f = other as ClipEventParent;
                return this.id == f.id && data.SequenceEqual(f.data);
            }
            else
            {
                return false;
            }
        }

        public ClipEventParent(int apiVersion, EventHandler handler)
            : this(apiVersion, handler, ClipEventType.PARENT, 52)
        {
        }

        public ClipEventParent(int apiVersion, EventHandler handler, ClipEventType typeId, uint size)
            : base(apiVersion, handler, typeId)
        {
            this.data = new byte[size - 12 - 4];
        }

        public ClipEventParent(int apiVersion, EventHandler handler, ClipEventParent basis)
            : base(apiVersion, handler, basis)
        {
            this.id = basis.id;
            this.data = new byte[basis.data.Length];
            Array.Copy(basis.data, this.data, basis.data.Length);
        }

        protected override void ReadTypeData(Stream ms)
        {
            var br = new BinaryReader(ms);
            this.id = br.ReadUInt32();
            br.Read(this.data, 0, this.data.Length);
        }

        protected override void WriteTypeData(Stream ms)
        {
            var bw = new BinaryWriter(ms);
            bw.Write(this.id);
            ms.Write(this.data, 0, this.data.Length);
        }
    }
    
    public class ClipEventSound : ClipEvent
    {
        private string sound_name;

        [ElementPriority(4)]
        public string SoundName
        {
            get { return sound_name; }
            set { if (this.sound_name != value) { this.sound_name = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return IOExt.FixedStringLength; }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventSound)
            {
                ClipEventSound f = other as ClipEventSound;
                return String.Compare(this.sound_name, f.sound_name) == 0;
            }
            else
            {
                return false;
            }
        }

        public ClipEventSound(int apiVersion, EventHandler handler)
            : base(apiVersion, handler, ClipEventType.SOUND)
        {
            this.sound_name = "";
        }
        public ClipEventSound(int apiVersion, EventHandler handler, ClipEventSound basis)
            : base(apiVersion, handler, basis)
        {
            this.sound_name = basis.sound_name;
        }

        protected override void ReadTypeData(Stream ms)
        {
            var br = new BinaryReader(ms);
            this.sound_name = br.ReadStringFixed();
        }

        protected override void WriteTypeData(Stream ms)
        {
            var bw = new BinaryWriter(ms);
            bw.WriteStringFixed(this.sound_name);
        }
    }

    public class ClipEventScript : ClipEvent
    {
        protected override uint typeSize
        {
            get { return 0; }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventScript)
            {
                ClipEventScript f = other as ClipEventScript;
                return true;
            }
            else
            {
                return false;
            }
        }

        public ClipEventScript(int apiVersion, EventHandler handler)
            : base(apiVersion, handler, ClipEventType.SCRIPT)
        {
        }
        public ClipEventScript(int apiVersion, EventHandler handler, ClipEventScript basis)
            : base(apiVersion, handler, basis)
        {
        }

        protected override void ReadTypeData(Stream ms)
        {            
        }
        protected override void WriteTypeData(Stream ms)
        {            
        }
    }

    public class ClipEventEffect : ClipEvent
    {
        private string object_effect_name;
        private uint actor_name_hash;
        private uint slot_name_hash;
        private byte[] data;
        private string effect_name;

        [ElementPriority(4)]
        public string ObjectEffectName
        {
            get { return object_effect_name; }
            set { if (this.object_effect_name != value) { this.object_effect_name = value; OnElementChanged(); } }
        }
        [ElementPriority(5)]
        public uint ActorNameHash
        {
            get { return actor_name_hash; }
            set { if (this.actor_name_hash != value) { this.actor_name_hash = value; OnElementChanged(); } }
        }
        [ElementPriority(6)]
        public uint SlotNameHash
        {
            get { return slot_name_hash; }
            set { if (this.slot_name_hash != value) { this.slot_name_hash = value; OnElementChanged(); } }
        }
        [ElementPriority(7)]
        public byte[] Data
        {
            get { return data; }
            set { if (this.data != value) { this.data = value; OnElementChanged(); } }
        }
        [ElementPriority(8)]
        public string EffectName
        {
            get { return effect_name; }
            set { if (this.effect_name != value) { this.effect_name = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return (uint)((2 * IOExt.FixedStringLength) + 8 + data.Length); }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventEffect)
            {
                ClipEventEffect f = other as ClipEventEffect;
                return (String.Compare(this.object_effect_name, f.object_effect_name) == 0 && String.Compare(this.effect_name, f.effect_name) == 0 &&
                    this.actor_name_hash == f.actor_name_hash && this.slot_name_hash == f.slot_name_hash &&
                    Enumerable.SequenceEqual(this.data, f.data));
            }
            else
            {
                return false;
            }
        }

        public ClipEventEffect(int apiVersion, EventHandler handler)
            : this(apiVersion, handler, ClipEventType.EFFECT, (uint)((2 * IOExt.FixedStringLength) + 24 + 12))
        {
        }
        public ClipEventEffect(int apiVersion, EventHandler handler, ClipEventType typeID, uint size)
            : base(apiVersion, handler, typeID)
        {
            this.data = new byte[size - (2 * IOExt.FixedStringLength) - 12 - 8];
            this.effect_name = "";
            this.object_effect_name = "";
        }
        public ClipEventEffect(int apiVersion, EventHandler handler, ClipEventEffect basis)
            : base(apiVersion, handler, basis)
        {
            this.data = basis.data;
            this.slot_name_hash = basis.slot_name_hash;
            this.actor_name_hash = basis.actor_name_hash;
            this.effect_name = basis.effect_name;
            this.object_effect_name = basis.object_effect_name;
        }

        protected override void ReadTypeData(Stream ms)
        {
            var br = new BinaryReader(ms);
            this.object_effect_name = br.ReadStringFixed();
            this.actor_name_hash = br.ReadUInt32();
            this.slot_name_hash = br.ReadUInt32();
            this.data = br.ReadBytes(this.data.Length);
            this.effect_name = br.ReadStringFixed();
        }

        protected override void WriteTypeData(Stream ms)
        {
            var bw = new BinaryWriter(ms);
            bw.WriteStringFixed(this.object_effect_name);
            bw.Write(this.actor_name_hash);
            bw.Write(this.slot_name_hash);
            bw.Write(this.data);
            bw.WriteStringFixed(this.effect_name);
        }
    }

    public class ClipEventVisibility : ClipEvent
    {
        private uint id;
        private byte visible;

        [ElementPriority(4)]
        public uint NameSpaceHash
        {
            get { return id; }
            set { if (this.id != value) { this.id = value; OnElementChanged(); } }
        }

        [ElementPriority(5)]
        public byte Visible
        {
            get { return visible; }
            set { if (this.visible != value) { this.visible = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return 5; }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventVisibility)
            {
                ClipEventVisibility f = other as ClipEventVisibility;
                return this.id == f.id && this.visible == f.visible;
            }
            else
            {
                return false;
            }
        }

        public ClipEventVisibility(int apiVersion, EventHandler handler)
            : this(apiVersion, handler, ClipEventType.VISIBILITY, 12 + 5)
        {
        }
        public ClipEventVisibility(int apiVersion, EventHandler handler, ClipEventType typeId, uint size)
            : base(apiVersion, handler, typeId)
        {
        }
        public ClipEventVisibility(int apiVersion, EventHandler handler, ClipEventVisibility basis)
            : base(apiVersion, handler, basis)
        {
            this.id = basis.id;
            this.visible = basis.visible;
        }

        protected override void ReadTypeData(Stream ms)
        {
            BinaryReader br = new BinaryReader(ms);
            this.id = br.ReadUInt32();
            this.visible = br.ReadByte();
        }
        protected override void WriteTypeData(Stream ms)
        {
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(this.id);
            bw.Write(this.visible);
        }
    }

    public class ClipEventDestroyProp : ClipEvent
    {
        private uint id;

        [ElementPriority(4)]
        public uint NameSpaceHash
        {
            get { return id; }
            set { if (this.id != value) { this.id = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return 4; }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventDestroyProp)
            {
                ClipEventDestroyProp f = other as ClipEventDestroyProp;
                return this.id == f.id;
            }
            else
            {
                return false;
            }
        }

        public ClipEventDestroyProp(int apiVersion, EventHandler handler)
            : this(apiVersion, handler, ClipEventType.DESTROY_PROP, 12 + 4)
        {
        }
        public ClipEventDestroyProp(int apiVersion, EventHandler handler, ClipEventType typeId, uint size)
            : base(apiVersion, handler, typeId)
        {
            
        }
        public ClipEventDestroyProp(int apiVersion, EventHandler handler, ClipEventDestroyProp basis)
            : base(apiVersion, handler, basis)
        {
            this.id = basis.id;
        }

        protected override void ReadTypeData(Stream ms)
        {
            BinaryReader br = new BinaryReader(ms);
            this.id = br.ReadUInt32();
        }
        protected override void WriteTypeData(Stream ms)
        {
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(this.id);
        }
    }

    public class ClipEventStopEffect : ClipEvent
    {
        private uint effectNameHash;
        private byte[] data;

        [ElementPriority(4)]
        public uint EffectNameHash
        {
            get { return effectNameHash; }
            set { if (this.effectNameHash != value) { this.effectNameHash = value; OnElementChanged(); } }
        }
        [ElementPriority(5)]
        public byte[] Data
        {
            get { return data; }
            set { if (this.data != value) { this.data = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return (uint)this.data.Length + 4; }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventStopEffect)
            {
                ClipEventStopEffect f = other as ClipEventStopEffect;
                return (this.effectNameHash == f.effectNameHash) & Enumerable.SequenceEqual(this.data, f.data);
            }
            else
            {
                return false;
            }
        }

        public ClipEventStopEffect(int apiVersion, EventHandler handler)
            : this(apiVersion, handler, ClipEventType.STOP_EFFECT, 12 + 4 + 5)
        {
        }
        public ClipEventStopEffect(int apiVersion, EventHandler handler, ClipEventType typeId, uint size)
            : base(apiVersion, handler, typeId)
        {
            this.data = new byte[size - 12 - 4];
        }
        public ClipEventStopEffect(int apiVersion, EventHandler handler, ClipEventStopEffect basis)
            : base(apiVersion, handler, basis)
        {
            this.effectNameHash = basis.effectNameHash;
            this.data = new byte[basis.data.Length];
            Array.Copy(basis.data, this.data, basis.data.Length);
        }

        protected override void ReadTypeData(Stream ms)
        {
            var br = new BinaryReader(ms);
            this.effectNameHash = br.ReadUInt32();
            ms.Read(this.data, 0, this.data.Length);
        }
        protected override void WriteTypeData(Stream ms)
        {
            var bw = new BinaryWriter(ms);
            bw.Write(this.effectNameHash);
            ms.Write(this.data, 0, this.data.Length);
        }
    }

    public class ClipEventBlockTransition : ClipEvent
    {
        private float unknown3;

        [ElementPriority(4)]
        public float Unknown3
        {
            get { return unknown3; }
            set { if (this.unknown3 != value) { this.unknown3 = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return 4; }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventBlockTransition)
            {
                ClipEventBlockTransition f = other as ClipEventBlockTransition;
                return this.unknown3 == f.unknown3;
            }
            else
            {
                return false;
            }
        }

        public ClipEventBlockTransition(int apiVersion, EventHandler handler)
            : base(apiVersion, handler, ClipEventType.BLOCK_TRANSITION)
        {
        }
        public ClipEventBlockTransition(int apiVersion, EventHandler handler, ClipEventBlockTransition basis)
            : base(apiVersion, handler, basis)
        {
            this.unknown3 = basis.unknown3;
        }

        protected override void ReadTypeData(Stream ms)
        {
            var br = new BinaryReader(ms);
            this.unknown3 = br.ReadSingle();
        }

        protected override void WriteTypeData(Stream ms)
        {
            var bw = new BinaryWriter(ms);
            bw.Write(this.unknown3);
        }
    }

    public class ClipEventSNAP : ClipEvent
    {
        private byte[] data;

        [ElementPriority(4)]
        public byte[] Data
        {
            get { return data; }
            set { if (this.data != value) { this.data = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return (uint)this.data.Length; }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventSNAP)
            {
                ClipEventSNAP f = other as ClipEventSNAP;
                return Enumerable.SequenceEqual(this.data, f.data);
            }
            else
            {
                return false;
            }
        }

        public ClipEventSNAP(int apiVersion, EventHandler handler)
            : this(apiVersion, handler, ClipEventType.SNAP, 12 + 32)
        {
        }
        public ClipEventSNAP(int apiVersion, EventHandler handler, ClipEventType typeId, uint size)
            : base(apiVersion, handler, typeId)
        {
            this.data = new byte[size - 12];
        }
        public ClipEventSNAP(int apiVersion, EventHandler handler, ClipEventSNAP basis)
            : base(apiVersion, handler, basis)
        {
            this.data = new byte[basis.data.Length];
            Array.Copy(basis.data, this.data, basis.data.Length);
        }

        protected override void ReadTypeData(Stream ms)
        {
            ms.Read(this.data, 0, this.data.Length);
        }
        protected override void WriteTypeData(Stream ms)
        {
            ms.Write(this.data, 0, this.data.Length);
        }
    }

    public class ClipEventReaction : ClipEvent
    {
        private string unknown3;
        private string unknown4;

        [ElementPriority(4)]
        public string Unknown3
        {
            get { return unknown3; }
            set { if (this.unknown3 != value) { this.unknown3 = value; OnElementChanged(); } }
        }
        [ElementPriority(5)]
        public string Unknown4
        {
            get { return unknown4; }
            set { if (this.unknown4 != value) { this.unknown4 = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return 2 * IOExt.FixedStringLength; }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventReaction)
            {
                ClipEventReaction f = other as ClipEventReaction;
                return (String.Compare(this.unknown3, f.unknown3) == 0 &&
                    String.Compare(this.unknown4, f.unknown4) == 0);
            }
            else
            {
                return false;
            }
        }

        public ClipEventReaction(int apiVersion, EventHandler handler)
            : base(apiVersion, handler, ClipEventType.REACTION)
        {
            this.unknown3 = "";
            this.unknown4 = "";
        }
        public ClipEventReaction(int apiVersion, EventHandler handler, ClipEventReaction basis)
            : base(apiVersion, handler, basis)
        {
            this.unknown3 = basis.unknown3;
            this.unknown4 = basis.unknown4;
        }

        protected override void ReadTypeData(Stream ms)
        {
            var br = new BinaryReader(ms);
            this.unknown3 = br.ReadStringFixed();
            this.unknown4 = br.ReadStringFixed();
        }

        protected override void WriteTypeData(Stream ms)
        {
            var bw = new BinaryWriter(ms);
            bw.WriteStringFixed(this.unknown3);
            bw.WriteStringFixed(this.unknown4);
        }
    }

    public class ClipEventDoubleModifierSound : ClipEvent
    {
        private string unknown_3;
        private uint actor_name;
        private uint slot_name;

        [ElementPriority(4)]
        public string Unknown3
        {
            get { return unknown_3; }
            set { if (this.unknown_3 != value) { this.unknown_3 = value; OnElementChanged(); } }
        }
        [ElementPriority(5)]
        public uint ActorNameHash
        {
            get { return actor_name; }
            set { if (this.actor_name != value) { this.actor_name = value; OnElementChanged(); } }
        }
        [ElementPriority(6)]
        public uint SlotNameHash
        {
            get { return slot_name; }
            set { if (this.slot_name != value) { this.slot_name = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return IOExt.FixedStringLength + 8; }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventDoubleModifierSound)
            {
                ClipEventDoubleModifierSound f = other as ClipEventDoubleModifierSound;
                return (String.Compare(this.unknown_3, f.unknown_3) == 0 &&
                    this.actor_name == f.actor_name && this.slot_name == f.slot_name);
            }
            else
            {
                return false;
            }
        }

        public ClipEventDoubleModifierSound(int apiVersion, EventHandler handler)
            : base(apiVersion, handler, ClipEventType.DOUBLE_MODIFIER_SOUND)
        {
            this.unknown_3 = "";
        }
        public ClipEventDoubleModifierSound(int apiVersion, EventHandler handler, ClipEventDoubleModifierSound basis)
            : base(apiVersion, handler, basis)
        {
            this.slot_name = basis.slot_name;
            this.actor_name = basis.actor_name;
            this.unknown_3 = basis.unknown_3;
        }

        protected override void ReadTypeData(Stream ms)
        {
            var br = new BinaryReader(ms);
            this.unknown_3 = br.ReadStringFixed();
            this.actor_name = br.ReadUInt32();
            this.slot_name = br.ReadUInt32();
        }

        protected override void WriteTypeData(Stream ms)
        {
            var bw = new BinaryWriter(ms);
            bw.WriteStringFixed(this.unknown_3);
            bw.Write(this.actor_name);
            bw.Write(this.slot_name);
        }
    }

    public class ClipEventDspInterval : ClipEvent
    {
        private float unknown3;
        private string unknown4;

        [ElementPriority(4)]
        public float Unknown3
        {
            get { return unknown3; }
            set { if (this.unknown3 != value) { this.unknown3 = value; OnElementChanged(); } }
        }
        [ElementPriority(5)]
        public string Unknown4
        {
            get { return unknown4; }
            set { if (this.unknown4 != value) { this.unknown4 = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return IOExt.FixedStringLength + 4; }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventDspInterval)
            {
                ClipEventDspInterval f = other as ClipEventDspInterval;
                return (String.Compare(this.unknown4, f.unknown4) == 0 &&
                    this.unknown3 == f.unknown3);
            }
            else
            {
                return false;
            }
        }

        public ClipEventDspInterval(int apiVersion, EventHandler handler)
            : base(apiVersion, handler, ClipEventType.DSP_INTERVAL)
        {
            this.unknown4 = "";
        }
        public ClipEventDspInterval(int apiVersion, EventHandler handler, ClipEventDspInterval basis)
            : base(apiVersion, handler, basis)
        {
            this.unknown3 = basis.unknown3;
            this.unknown4 = basis.unknown4;
        }

        protected override void ReadTypeData(Stream ms)
        {
            var br = new BinaryReader(ms);
            this.unknown3 = br.ReadSingle();
            this.unknown4 = br.ReadStringFixed();
        }

        protected override void WriteTypeData(Stream ms)
        {
            var bw = new BinaryWriter(ms);
            bw.Write(this.unknown3);
            bw.WriteStringFixed(this.unknown4);
        }
    }

    public class ClipEventMaterialState : ClipEvent
    {
        private byte[] data;
        string unknown3;

        [ElementPriority(4)]
        public byte[] Data
        {
            get { return data; }
            set { if (this.data != value) { this.data = value; OnElementChanged(); } }
        }
        [ElementPriority(5)]
        public string Unknown3
        {
            get { return unknown3; }
            set { if (this.unknown3 != value) { this.unknown3 = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return (uint)(IOExt.FixedStringLength + this.data.Length); }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventMaterialState)
            {
                ClipEventMaterialState f = other as ClipEventMaterialState;
                return Enumerable.SequenceEqual(this.data, f.data) & (String.Compare(this.unknown3, f.unknown3) == 0);
            }
            else
            {
                return false;
            }
        }

        public ClipEventMaterialState(int apiVersion, EventHandler handler)
            : this(apiVersion, handler, ClipEventType.MATERIAL_STATE, 12 + IOExt.FixedStringLength + 4)
        {
        }
        public ClipEventMaterialState(int apiVersion, EventHandler handler, ClipEventType typeId, uint size)
            : base(apiVersion, handler, typeId)
        {
            this.data = new byte[size - IOExt.FixedStringLength - 12];
            this.unknown3 = "";
        }
        public ClipEventMaterialState(int apiVersion, EventHandler handler, ClipEventMaterialState basis)
            : base(apiVersion, handler, basis)
        {
            this.data = new byte[basis.data.Length];
            Array.Copy(basis.data, this.data, basis.data.Length);
            this.unknown3 = basis.unknown3;
        }

        protected override void ReadTypeData(Stream ms)
        {
            ms.Read(this.data, 0, this.data.Length);
            var br = new BinaryReader(ms);
            this.unknown3 = br.ReadStringFixed();
        }
        protected override void WriteTypeData(Stream ms)
        {
            ms.Write(this.data, 0, this.data.Length);
            var bw = new BinaryWriter(ms);
            bw.WriteStringFixed(this.unknown3);
        }
    }

    public class ClipEventFocusCompatibility : ClipEvent
    {
        private byte[] data;
        string unknown3;

        [ElementPriority(4)]
        public byte[] Data
        {
            get { return data; }
            set { if (this.data != value) { this.data = value; OnElementChanged(); } }
        }
        [ElementPriority(5)]
        public string Unknown3
        {
            get { return unknown3; }
            set { if (this.unknown3 != value) { this.unknown3 = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return (uint)(IOExt.FixedStringLength + this.data.Length); }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventFocusCompatibility)
            {
                ClipEventFocusCompatibility f = other as ClipEventFocusCompatibility;
                return Enumerable.SequenceEqual(this.data, f.data) & (String.Compare(this.unknown3, f.unknown3) == 0);
            }
            else
            {
                return false;
            }
        }

        public ClipEventFocusCompatibility(int apiVersion, EventHandler handler)
            : this(apiVersion, handler, ClipEventType.FOCUS_COMPATIBILITY, 12 + IOExt.FixedStringLength + 4)
        {
        }
        public ClipEventFocusCompatibility(int apiVersion, EventHandler handler, ClipEventType typeId, uint size)
            : base(apiVersion, handler, typeId)
        {
            this.data = new byte[size - IOExt.FixedStringLength - 12];
            this.unknown3 = "";
        }
        public ClipEventFocusCompatibility(int apiVersion, EventHandler handler, ClipEventFocusCompatibility basis)
            : base(apiVersion, handler, basis)
        {
            this.data = new byte[basis.data.Length];
            Array.Copy(basis.data, this.data, basis.data.Length);
            this.unknown3 = basis.unknown3;
        }

        protected override void ReadTypeData(Stream ms)
        {
            ms.Read(this.data, 0, this.data.Length);
            var br = new BinaryReader(ms);
            this.unknown3 = br.ReadStringFixed();
        }
        protected override void WriteTypeData(Stream ms)
        {
            ms.Write(this.data, 0, this.data.Length);
            var bw = new BinaryWriter(ms);
            bw.WriteStringFixed(this.unknown3);
        }
    }

    public class ClipEventSuppressLipSync : ClipEvent
    {
        private float unknown_3;
        private byte unknown_4;

        [ElementPriority(4)]
        public float Unknown3
        {
            get { return unknown_3; }
            set { if (this.unknown_3 != value) { this.unknown_3 = value; OnElementChanged(); } }
        }
        [ElementPriority(5)]
        public byte Unknown4
        {
            get { return unknown_4; }
            set { if (this.unknown_4 != value) { this.unknown_4 = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return 5; }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventSuppressLipSync)
            {
                ClipEventSuppressLipSync f = other as ClipEventSuppressLipSync;
                return this.unknown_3 == f.unknown_3 & this.unknown_4 == f.unknown_4;
            }
            else
            {
                return false;
            }
        }

        public ClipEventSuppressLipSync(int apiVersion, EventHandler handler)
            : base(apiVersion, handler, ClipEventType.SUPPRESS_LIP_SYNC)
        {
        }
        public ClipEventSuppressLipSync(int apiVersion, EventHandler handler, ClipEventSuppressLipSync basis)
            : base(apiVersion, handler, basis)
        {
            this.unknown_3 = basis.unknown_3;
            this.unknown_4 = basis.unknown_4;
        }

        protected override void ReadTypeData(Stream ms)
        {
            var br = new BinaryReader(ms);
            this.unknown_3 = br.ReadSingle();
            this.unknown_4 = br.ReadByte();
        }

        protected override void WriteTypeData(Stream ms)
        {

            var bw = new BinaryWriter(ms);
            bw.Write(this.unknown_3);
            bw.Write(this.unknown_4);
        }
    }

    public class ClipEventCensor : ClipEvent
    {
        private float unknown_3;

        [ElementPriority(4)]
        public float Unknown3
        {
            get { return unknown_3; }
            set { if (this.unknown_3 != value) { this.unknown_3 = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return 4; }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventCensor)
            {
                ClipEventCensor f = other as ClipEventCensor;
                return this.unknown_3 == f.unknown_3;
            }
            else
            {
                return false;
            }
        }

        public ClipEventCensor(int apiVersion, EventHandler handler)
            : base(apiVersion, handler, ClipEventType.CENSOR)
        {
        }
        public ClipEventCensor(int apiVersion, EventHandler handler, ClipEventCensor basis)
            : base(apiVersion, handler, basis)
        {
            this.unknown_3 = basis.unknown_3;
        }

        protected override void ReadTypeData(Stream ms)
        {
            var br = new BinaryReader(ms);
            this.unknown_3 = br.ReadSingle();
        }

        protected override void WriteTypeData(Stream ms)
        {

            var bw = new BinaryWriter(ms);
            bw.Write(this.unknown_3);
        }
    }

    public class ClipEventSimulationSoundStart : ClipEvent
    {
        private byte[] data;

        [ElementPriority(4)]
        public byte[] Data
        {
            get { return data; }
            set { if (this.data != value) { this.data = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return (uint)this.data.Length; }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventSimulationSoundStart)
            {
                ClipEventSimulationSoundStart f = other as ClipEventSimulationSoundStart;
                return Enumerable.SequenceEqual(this.data, f.data);
            }
            else
            {
                return false;
            }
        }

        public ClipEventSimulationSoundStart(int apiVersion, EventHandler handler)
            : this(apiVersion, handler, ClipEventType.SIMULATION_SOUND_START, 12 + 1)
        {
        }
        public ClipEventSimulationSoundStart(int apiVersion, EventHandler handler, ClipEventType typeId, uint size)
            : base(apiVersion, handler, typeId)
        {
            this.data = new byte[size - 12];
        }
        public ClipEventSimulationSoundStart(int apiVersion, EventHandler handler, ClipEventSimulationSoundStart basis)
            : base(apiVersion, handler, basis)
        {
            this.data = new byte[basis.data.Length];
            Array.Copy(basis.data, this.data, basis.data.Length);
        }

        protected override void ReadTypeData(Stream ms)
        {
            ms.Read(this.data, 0, this.data.Length);
        }
        protected override void WriteTypeData(Stream ms)
        {
            ms.Write(this.data, 0, this.data.Length);
        }
    }

    public class ClipEventSimulationSoundStop : ClipEvent
    {
        private byte[] data;
        string unknown3;

        [ElementPriority(4)]
        public byte[] Data
        {
            get { return data; }
            set { if (this.data != value) { this.data = value; OnElementChanged(); } }
        }
        [ElementPriority(5)]
        public string Unknown3
        {
            get { return unknown3; }
            set { if (this.unknown3 != value) { this.unknown3 = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return (uint)(IOExt.FixedStringLength + this.data.Length); }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventSimulationSoundStop)
            {
                ClipEventSimulationSoundStop f = other as ClipEventSimulationSoundStop;
                return Enumerable.SequenceEqual(this.data, f.data) & (String.Compare(this.unknown3, f.unknown3) == 0);
            }
            else
            {
                return false;
            }
        }

        public ClipEventSimulationSoundStop(int apiVersion, EventHandler handler)
            : this(apiVersion, handler, ClipEventType.SIMULATION_SOUND_STOP, 12 + IOExt.FixedStringLength + 4)
        {
        }
        public ClipEventSimulationSoundStop(int apiVersion, EventHandler handler, ClipEventType typeId, uint size)
            : base(apiVersion, handler, typeId)
        {
            this.data = new byte[size - IOExt.FixedStringLength - 12];
            this.unknown3 = "";
        }
        public ClipEventSimulationSoundStop(int apiVersion, EventHandler handler, ClipEventSimulationSoundStop basis)
            : base(apiVersion, handler, basis)
        {
            this.data = new byte[basis.data.Length];
            Array.Copy(basis.data, this.data, basis.data.Length);
            this.unknown3 = basis.unknown3;
        }

        protected override void ReadTypeData(Stream ms)
        {
            ms.Read(this.data, 0, this.data.Length);
            var br = new BinaryReader(ms);
            this.unknown3 = br.ReadStringFixed();
        }
        protected override void WriteTypeData(Stream ms)
        {
            ms.Write(this.data, 0, this.data.Length);
            var bw = new BinaryWriter(ms);
            bw.WriteStringFixed(this.unknown3);
        }
    }

    public class ClipEventEnableFacialOverlay : ClipEvent
    {
        private byte[] data;
        string unknown3;

        [ElementPriority(4)]
        public byte[] Data
        {
            get { return data; }
            set { if (this.data != value) { this.data = value; OnElementChanged(); } }
        }
        [ElementPriority(5)]
        public string Unknown3
        {
            get { return unknown3; }
            set { if (this.unknown3 != value) { this.unknown3 = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return (uint)(IOExt.FixedStringLength + this.data.Length); }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventEnableFacialOverlay)
            {
                ClipEventEnableFacialOverlay f = other as ClipEventEnableFacialOverlay;
                return Enumerable.SequenceEqual(this.data, f.data) & (String.Compare(this.unknown3, f.unknown3) == 0);
            }
            else
            {
                return false;
            }
        }

        public ClipEventEnableFacialOverlay(int apiVersion, EventHandler handler)
            : this(apiVersion, handler, ClipEventType.ENABLE_FACIAL_OVERLAY, 12 + IOExt.FixedStringLength + 4)
        {
        }
        public ClipEventEnableFacialOverlay(int apiVersion, EventHandler handler, ClipEventType typeId, uint size)
            : base(apiVersion, handler, typeId)
        {
            this.data = new byte[size - IOExt.FixedStringLength - 12];
            this.unknown3 = "";
        }
        public ClipEventEnableFacialOverlay(int apiVersion, EventHandler handler, ClipEventEnableFacialOverlay basis)
            : base(apiVersion, handler, basis)
        {
            this.data = new byte[basis.data.Length];
            Array.Copy(basis.data, this.data, basis.data.Length);
            this.unknown3 = basis.unknown3;
        }

        protected override void ReadTypeData(Stream ms)
        {
            ms.Read(this.data, 0, this.data.Length);
            var br = new BinaryReader(ms);
            this.unknown3 = br.ReadStringFixed();
        }
        protected override void WriteTypeData(Stream ms)
        {
            ms.Write(this.data, 0, this.data.Length);
            var bw = new BinaryWriter(ms);
            bw.WriteStringFixed(this.unknown3);
        }
    }

    public class ClipEventFadeObject : ClipEvent
    {
        private float unknown_3;

        [ElementPriority(4)]
        public float Unknown3
        {
            get { return unknown_3; }
            set { if (this.unknown_3 != value) { this.unknown_3 = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return 4; }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventFadeObject)
            {
                ClipEventFadeObject f = other as ClipEventFadeObject;
                return this.unknown_3 == f.unknown_3;
            }
            else
            {
                return false;
            }
        }

        public ClipEventFadeObject(int apiVersion, EventHandler handler)
            : base(apiVersion, handler, ClipEventType.FADE_OBJECT)
        {
        }
        public ClipEventFadeObject(int apiVersion, EventHandler handler, ClipEventFadeObject basis)
            : base(apiVersion, handler, basis)
        {
            this.unknown_3 = basis.unknown_3;
        }

        protected override void ReadTypeData(Stream ms)
        {
            var br = new BinaryReader(ms);
            this.unknown_3 = br.ReadSingle();
        }

        protected override void WriteTypeData(Stream ms)
        {

            var bw = new BinaryWriter(ms);
            bw.Write(this.unknown_3);
        }
    }

    public class ClipEventThighTargetOffset : ClipEvent
    {
        private float unknown_3;

        [ElementPriority(4)]
        public float Unknown3
        {
            get { return unknown_3; }
            set { if (this.unknown_3 != value) { this.unknown_3 = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return 4; }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventThighTargetOffset)
            {
                ClipEventThighTargetOffset f = other as ClipEventThighTargetOffset;
                return this.unknown_3 == f.unknown_3;
            }
            else
            {
                return false;
            }
        }

        public ClipEventThighTargetOffset(int apiVersion, EventHandler handler)
            : base(apiVersion, handler, ClipEventType.THIGH_TARGET_OFFSET)
        {
        }
        public ClipEventThighTargetOffset(int apiVersion, EventHandler handler, ClipEventThighTargetOffset basis)
            : base(apiVersion, handler, basis)
        {
            this.unknown_3 = basis.unknown_3;
        }

        protected override void ReadTypeData(Stream ms)
        {
            var br = new BinaryReader(ms);
            this.unknown_3 = br.ReadSingle();
        }

        protected override void WriteTypeData(Stream ms)
        {

            var bw = new BinaryWriter(ms);
            bw.Write(this.unknown_3);
        }
    }

    public class ClipEventUnknown28 : ClipEvent
    {
        string unknown3;
        private byte[] data;

        [ElementPriority(4)]
        public string Unknown3
        {
            get { return unknown3; }
            set { if (this.unknown3 != value) { this.unknown3 = value; OnElementChanged(); } }
        }
        [ElementPriority(5)]
        public byte[] Data
        {
            get { return data; }
            set { if (this.data != value) { this.data = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return (uint)(IOExt.FixedStringLength + this.data.Length); }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventUnknown28)
            {
                ClipEventUnknown28 f = other as ClipEventUnknown28;
                return Enumerable.SequenceEqual(this.data, f.data) & (String.Compare(this.unknown3, f.unknown3) == 0);
            }
            else
            {
                return false;
            }
        }

        public ClipEventUnknown28(int apiVersion, EventHandler handler)
            : this(apiVersion, handler, ClipEventType.UNKNOWN28, 12 + IOExt.FixedStringLength + 8)
        {
        }
        public ClipEventUnknown28(int apiVersion, EventHandler handler, ClipEventType typeId, uint size)
            : base(apiVersion, handler, typeId)
        {
            this.unknown3 = "";
            this.data = new byte[size - IOExt.FixedStringLength - 12];
        }
        public ClipEventUnknown28(int apiVersion, EventHandler handler, ClipEventUnknown28 basis)
            : base(apiVersion, handler, basis)
        {
            this.data = new byte[basis.data.Length];
            Array.Copy(basis.data, this.data, basis.data.Length);
            this.unknown3 = basis.unknown3;
        }

        protected override void ReadTypeData(Stream ms)
        {
            var br = new BinaryReader(ms);
            this.unknown3 = br.ReadStringFixed();
            ms.Read(this.data, 0, this.data.Length);
        }
        protected override void WriteTypeData(Stream ms)
        {
            var bw = new BinaryWriter(ms);
            bw.WriteStringFixed(this.unknown3);
            ms.Write(this.data, 0, this.data.Length);
        }
    }

    public class ClipEventUnknown30 : ClipEvent
    {
        float[] unknown3;
        uint[]  unknown4;

        [ElementPriority(4)]
        public float[] Unknown3
        {
            get { return unknown3; }
            set { if (this.unknown3 != value) { this.unknown3 = value; OnElementChanged(); } }
        }
        [ElementPriority(5)]
        public uint[] Unknown4
        {
            get { return unknown4; }
            set { if (this.unknown4 != value) { this.unknown4 = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return 32; }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventUnknown30)
            {
                ClipEventUnknown30 f = other as ClipEventUnknown30;
                return Enumerable.SequenceEqual(this.unknown3, f.unknown3) & Enumerable.SequenceEqual(this.unknown4, f.unknown4);
            }
            else
            {
                return false;
            }
        }

        public ClipEventUnknown30(int apiVersion, EventHandler handler)
            : base(apiVersion, handler, ClipEventType.UNKNOWN30)
        {
            this.unknown3 = new float[4];
            this.unknown4 = new uint[4];
        }
        public ClipEventUnknown30(int apiVersion, EventHandler handler, ClipEventUnknown30 basis)
            : base(apiVersion, handler, basis)
        {
            this.unknown3 = new float[basis.unknown3.Length];
            Array.Copy(basis.unknown3, this.unknown3, basis.unknown3.Length);
            this.unknown4 = new uint[basis.unknown4.Length];
            Array.Copy(basis.unknown4, this.unknown4, basis.unknown4.Length);
        }

        protected override void ReadTypeData(Stream ms)
        {
            var br = new BinaryReader(ms);
            for (int i = 0; i < 4; i++) this.unknown3[i] = br.ReadSingle();
            for (int i = 0; i < 4; i++) this.unknown4[i] = br.ReadUInt32();
        }
        protected override void WriteTypeData(Stream ms)
        {
            var bw = new BinaryWriter(ms);
            for (int i = 0; i < 4; i++) bw.Write(this.unknown3[i]);
            for (int i = 0; i < 4; i++) bw.Write(this.unknown4[i]);
        }
    }

    public class ClipEventUnknown : ClipEvent
    {
        private byte[] data;

        [ElementPriority(4)]
        public byte[] Data
        {
            get { return data; }
            set { if (this.data != value) { this.data = value; OnElementChanged(); } }
        }

        protected override uint typeSize
        {
            get { return (uint)this.data.Length; }
        }

        protected override bool isEqual(ClipEvent other)
        {
            if (other is ClipEventUnknown)
            {
                ClipEventUnknown f = other as ClipEventUnknown;
                return Enumerable.SequenceEqual(this.data, f.data);
            }
            else
            {
                return false;
            }
        }

        public ClipEventUnknown(int apiVersion, EventHandler handler)
            : this(apiVersion, handler, 0, 12)
        {
        }
        public ClipEventUnknown(int apiVersion, EventHandler handler, ClipEventType typeId, uint size)
            : base(apiVersion, handler, typeId)
        {
            this.data = new byte[(int)size - 12];
        }

        protected override void ReadTypeData(Stream ms)
        {
            BinaryReader br = new BinaryReader(ms);
            for (int i = 0; i < this.data.Length; i++) this.data[i] = br.ReadByte();
        }
        protected override void WriteTypeData(Stream ms)
        {
            BinaryWriter bw = new BinaryWriter(ms);
            for (int i = 0; i < this.data.Length; i++) bw.Write(this.data[i]);
        }
    }

    public abstract class ClipEvent : AHandlerElement, IEquatable<ClipEvent>
    {
        protected ClipEvent(int apiVersion, EventHandler handler, ClipEventType typeId)
            : this(apiVersion, handler, typeId, 0, 0, 0)
        {
        }

        protected ClipEvent(int apiVersion, EventHandler handler, ClipEventType typeId, Stream s)
            : this(apiVersion, handler, typeId)
        {
            Parse(s);
        }

        protected ClipEvent(int apiVersion, EventHandler handler, ClipEvent basis)
            : this(apiVersion, handler, basis.typeId, basis.unknown1, basis.unknown2, basis.timecode)
        {
        }

        protected ClipEvent(int APIversion, EventHandler handler, ClipEventType type, uint unknown1, uint unknown2, float timecode)
            : base(APIversion, handler)
        {
            this.typeId = type;
            this.unknown1 = unknown1;
            this.unknown2 = unknown2;
            this.timecode = timecode;
        }

        protected ClipEventType typeId;
        private uint unknown1;
        private uint unknown2;
        private float timecode;

        public string Value
        {
            get { return ValueBuilder; }
        }
        [ElementPriority(0)]
        public ClipEventType TypeId
        {
            get { return typeId; }
            set { if (this.typeId == ClipEventType.INVALID & this.typeId != value) this.typeId = value; OnElementChanged(); }
        }
        [ElementPriority(1)]
        public uint Unknown1
        {
            get { return unknown1; }
            set { if (this.unknown1 != value) { this.unknown1 = value; OnElementChanged(); } }
        }
        [ElementPriority(2)]
        public uint Unknown2
        {
            get { return unknown2; }
            set { if (this.unknown2 != value) { this.unknown2 = value; OnElementChanged(); } }
        }
        [ElementPriority(3)]
        public float Timecode
        {
            get { return timecode; }
            set { if (this.timecode != value) { this.timecode = value; OnElementChanged(); } }
        }

        protected abstract uint typeSize { get; }
        internal uint Size
        {
            get { return this.typeSize + 12; }
        }

        public void Parse(Stream s)
        {
            var br = new BinaryReader(s);
            this.unknown1 = br.ReadUInt32();
            this.unknown2 = br.ReadUInt32();
            this.timecode = br.ReadSingle();
            this.ReadTypeData(s);
        }
        protected abstract void ReadTypeData(Stream ms);

        public void UnParse(Stream s)
        {
            var bw = new BinaryWriter(s);
            bw.Write(this.unknown1);
            bw.Write(this.unknown2);
            bw.Write(this.timecode);
            this.WriteTypeData(s);

        }
        protected abstract void WriteTypeData(Stream ms);

        public override int RecommendedApiVersion
        {
            get { return 1; }
        }

        public override List<string> ContentFields
        {
            get { return GetContentFields(requestedApiVersion, GetType()); }
        }

        public bool Equals(ClipEvent other)
        {
            if (this.GetType() != other.GetType() || this.unknown1 != other.unknown1 || this.unknown2 != other.unknown2) return false;
            return this.isEqual(other);
        }
        protected abstract bool isEqual(ClipEvent other);

        public static ClipEvent Create(ClipEventType typeId, EventHandler handler, uint size)
        {
            switch ((uint)typeId)
            {
                case 1:
                    return new ClipEventParent(0, handler, typeId, size);
                case 3:
                    return new ClipEventSound(0, handler);
                case 4:
                    return new ClipEventScript(0, handler);
                case 5:
                    return new ClipEventEffect(0, handler, typeId, size);
                case 6:
                    return new ClipEventVisibility(0, handler, typeId, size);
                case 9:
                    return new ClipEventDestroyProp(0, handler, typeId, size);
                case 10:
                    return new ClipEventStopEffect(0, handler, typeId, size);
                case 11:
                    return new ClipEventBlockTransition(0, handler);
                case 12:
                    return new ClipEventSNAP(0, handler, typeId, size);
                case 13:
                    return new ClipEventReaction(0, handler);
                case 14:
                    return new ClipEventDoubleModifierSound(0, handler);
                case 15:
                    return new ClipEventDspInterval(0, handler);
                case 16:
                    return new ClipEventMaterialState(0, handler, typeId, size);
                case 17:
                    return new ClipEventFocusCompatibility(0, handler, typeId, size);
                case 18:
                    return new ClipEventSuppressLipSync(0, handler);
                case 19:
                    return new ClipEventCensor(0, handler);
                case 20:
                    return new ClipEventSimulationSoundStart(0, handler, typeId, size);
                case 21:
                    return new ClipEventSimulationSoundStop(0, handler, typeId, size);
                case 22:
                    return new ClipEventEnableFacialOverlay(0, handler, typeId, size);
                case 23:
                    return new ClipEventFadeObject(0, handler);
                case 25:
                    return new ClipEventThighTargetOffset(0, handler);
                case 28:
                    return new ClipEventUnknown28(0, handler, typeId, size);
                case 30:
                    return new ClipEventUnknown30(0, handler);
                default:
                    return new ClipEventUnknown(0, handler, typeId, size);
            }
        }
    }

    public enum ClipEventType : uint
    {
        INVALID = 0,
        PARENT,
        UNPARENT,
        SOUND,
        SCRIPT,
        EFFECT,
        VISIBILITY,
        DEPRECATED_6,
        CREATE_PROP,
        DESTROY_PROP,
        STOP_EFFECT,
        BLOCK_TRANSITION,
        SNAP,
        REACTION,
        DOUBLE_MODIFIER_SOUND,
        DSP_INTERVAL,
        MATERIAL_STATE,
        FOCUS_COMPATIBILITY,
        SUPPRESS_LIP_SYNC,
        CENSOR,
        SIMULATION_SOUND_START,
        SIMULATION_SOUND_STOP,
        ENABLE_FACIAL_OVERLAY,
        FADE_OBJECT,
        DISABLE_OBJECT_HIGHLIGHT,
        THIGH_TARGET_OFFSET,
        UNKNOWN26,
        UNKNOWN27,
        UNKNOWN28,
        UNKNOWN29,
        UNKNOWN30
    }
}
