/***************************************************************************
 *  Copyright (C) 2009, 2014 by the Sims 4 Tools development team          *
 *                                                                         *
 *  Contributors:                                                          *
 *  Peter L Jones, pljones@users.sf.net                                    *
 *  Keyi Zhang, kz005@bucknell.edu                                         *
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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using s4pi.Interfaces;
using s4pi.Package;
using s4pi.Extensions;
using System.Text;
using s4pi.ImageResource;
namespace S4PIDemoFE
{
    public interface IBuiltInValueControl
    {
        bool IsAvailable { get; }
        Control ValueControl { get; }
        IEnumerable<ToolStripItem> GetContextMenuItems(EventHandler cbk);
    }

    public abstract class ABuiltInValueControl : IBuiltInValueControl
    {
        public abstract bool IsAvailable { get; }
        public abstract Control ValueControl { get; }
        public abstract IEnumerable<ToolStripItem> GetContextMenuItems(EventHandler cbk);

        protected ABuiltInValueControl(Stream s) { }

        static List<KeyValuePair<List<uint>, Type>> builtInValueControlLookup = new List<KeyValuePair<List<uint>, Type>>();
        static ABuiltInValueControl()
        {
            var types = typeof(ABuiltInValueControl).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(ABuiltInValueControl).IsAssignableFrom(t));
            foreach (var t in types)
            {
                var fi = t.GetField("resourceTypes", BindingFlags.NonPublic | BindingFlags.Static);
                if (fi == null)
                    continue;
                if (!fi.FieldType.HasElementType || fi.FieldType.GetElementType() != typeof(uint))
                    continue;
                builtInValueControlLookup.Add(new KeyValuePair<List<uint>, Type>(
                   new List<uint>((uint[])fi.GetValue(fi)), t));
            }
        }

        public static bool Exists(uint resourceType)
        {
            return builtInValueControlLookup
                .Where(x => x.Key.Contains(resourceType))
                .Select(x => Activator.CreateInstance(x.Value, new object[] { Stream.Null, }) as ABuiltInValueControl)
                .Where(x => x.IsAvailable)
                .FirstOrDefault() != null;
        }

        public static IBuiltInValueControl Lookup(uint resourceType, Stream s)
        {
            return builtInValueControlLookup
                .Where(x => x.Key.Contains(resourceType))
                .Select(x => Activator.CreateInstance(x.Value, new object[] { s, }) as ABuiltInValueControl)
                .Where(x => x.IsAvailable)
                .FirstOrDefault();
        }
    }

    class DDSControl : ABuiltInValueControl
    {
        static uint[] resourceTypes = new uint[] {
            0x00B2D882,
            0x8FFB80F6,
            0xB6C8B6A0
        };

        static bool channel1 = true, channel2 = true, channel3 = true, channel4 = true, invertch4 = false;
        Stream resStream;
        DDSPanel ddsPanel1;
        public DDSControl(Stream s)
            : base(s)
        {
            if (s == null || s == Stream.Null)
                return;

            resStream = s;
            if (s == null || s.Length == 0) return;
            s.Position = 0;
            RLEResource.RLEInfo header = new RLEResource.RLEInfo();
            header.Parse(s);
            if (header.pixelFormat.Fourcc == FourCC.DST1 || header.pixelFormat.Fourcc == FourCC.DST3 || header.pixelFormat.Fourcc == FourCC.DST5)
            {
                s.Position = 0;
                resStream = (new DSTResource(1, s)).ToDDS();
            }

            resStream.Position = 0;

            ddsPanel1 = new DDSPanel()
            {
                Fit = true,
                Channel1 = channel1,
                Channel2 = channel2,
                Channel3 = channel3,
                Channel4 = channel4,
                InvertCh4 = invertch4,
                Margin = new Padding(3),
            };
            ddsPanel1.Channel1Changed += (sn, e) => channel1 = ddsPanel1.Channel1;
            ddsPanel1.Channel2Changed += (sn, e) => channel2 = ddsPanel1.Channel2;
            ddsPanel1.Channel3Changed += (sn, e) => channel3 = ddsPanel1.Channel3;
            ddsPanel1.Channel4Changed += (sn, e) => channel4 = ddsPanel1.Channel4;
            ddsPanel1.InvertCh4Changed += (sn, e) => invertch4 = ddsPanel1.InvertCh4;
            ddsPanel1.DDSLoad(resStream);
        }

        public override bool IsAvailable { get { return S4PIDemoFE.Properties.Settings.Default.EnableDDSPreview; } }

        public override Control ValueControl { get { return ddsPanel1; } }

        public override IEnumerable<ToolStripItem> GetContextMenuItems(EventHandler cbk)
        {
            yield break;
            //const string ddsFiles = "DDS Images|*.dds";

            //yield return new ToolStripSeparator();

            //yield return new ToolStripMenuItem("Load DDS...", null, (s, e) => { if (getFilename("Load DDS...", ddsFiles).Any(loadDDS)) { cbk(s, e); } });
            //yield return new ToolStripMenuItem("Load Image...", null, (s, e) => { if (getFilename("Load Image...", "Image files|*.png;*.gif;*.jpg;*.bmp").Any(loadImage)) { cbk(s, e); } });
            //yield return new ToolStripMenuItem("Save DDS...", null, (s, e) => { if (getFilename("Save DDS...", ddsFiles, true).Any(saveDDS)) { } });
            //yield return new ToolStripMenuItem("Save Image...", null, (s, e) => { if (getFilename("Save Image...", "Portable Network Grapics files|*.png|Grapics Interchange Format files|*.gif|JPEG files|*.jpg|Bitmap files|*.bmp", true).Any(saveImage)) { } });
        }

        bool loadDDS(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                ddsPanel1.DDSLoad(fs, true);
                fs.Close();
            }
            ddsPanel1.Channel1 = channel1;
            ddsPanel1.Channel2 = channel2;
            ddsPanel1.Channel3 = channel3;
            ddsPanel1.Channel4 = channel4;
            return true;
        }

        bool loadImage(string filename)
        {
            ddsPanel1.Import(filename, true);
            ddsPanel1.Channel1 = channel1;
            ddsPanel1.Channel2 = channel2;
            ddsPanel1.Channel3 = channel3;
            ddsPanel1.Channel4 = channel4;
            return true;
        }

        bool saveDDS(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                ddsPanel1.DDSSave(fs);
                fs.Close();
            }
            CopyableMessageBox.Show("Saved.", "Save DDS...");
            return true;
        }

        static System.Drawing.Imaging.ImageFormat[] fmts = new[] {
                System.Drawing.Imaging.ImageFormat.Png,
                System.Drawing.Imaging.ImageFormat.Gif,
                System.Drawing.Imaging.ImageFormat.Jpeg,
                System.Drawing.Imaging.ImageFormat.Bmp,
            };
        bool saveImage(string filename)
        {
            var ext = Array.IndexOf(new[] { ".png", ".gif", ".jpg", ".bmp", }, Path.GetExtension(filename).ToLower());
            var fmt = ext >= 0 ? fmts[ext] : System.Drawing.Imaging.ImageFormat.Png;

            ddsPanel1.Image.Save(filename, fmt);
            CopyableMessageBox.Show("Saved.", "Save Image...");
            return true;
        }

        IEnumerable<string> getFilename(string Title, string Filter, bool save = false)
        {
            FileDialog fd = save ? (FileDialog)new SaveFileDialog() : (FileDialog)new OpenFileDialog();
            fd.Title = Title;
            fd.FileName = "";
            fd.FilterIndex = 0;
            fd.Filter = Filter + "|All files|*.*";
            DialogResult dr = fd.ShowDialog();
            if (dr != DialogResult.OK) yield break;
            yield return fd.FileName;
        }
    }

    class RLEControl : ABuiltInValueControl
    {
        static uint[] resourceTypes = new uint[] {
            0x3453CF95,
            0xBA856C78,
        };

        static bool channel1 = true, channel2 = true, channel3 = true, channel4 = true, invertch4 = false;
        Stream resStream;
        DDSPanel ddsPanel1;
        public RLEControl(Stream s)
            : base(s)
        {
            if (s == null || s == Stream.Null)
                return;

            resStream = s;
            ddsPanel1 = new DDSPanel()
            {
                Fit = true,
                Channel1 = channel1,
                Channel2 = channel2,
                Channel3 = channel3,
                Channel4 = channel4,
                InvertCh4 = invertch4,
                Margin = new Padding(3),
            };
            ddsPanel1.Channel1Changed += (sn, e) => channel1 = ddsPanel1.Channel1;
            ddsPanel1.Channel2Changed += (sn, e) => channel2 = ddsPanel1.Channel2;
            ddsPanel1.Channel3Changed += (sn, e) => channel3 = ddsPanel1.Channel3;
            ddsPanel1.Channel4Changed += (sn, e) => channel4 = ddsPanel1.Channel4;
            ddsPanel1.InvertCh4Changed += (sn, e) => invertch4 = ddsPanel1.InvertCh4;
            ddsPanel1.RLELoad(resStream);
        }

        public override bool IsAvailable { get { return S4PIDemoFE.Properties.Settings.Default.EnableDDSPreview; } }

        public override Control ValueControl { get { return ddsPanel1; } }

        public override IEnumerable<ToolStripItem> GetContextMenuItems(EventHandler cbk)
        {
            yield break;
        }
    }


    class ImageControl : ABuiltInValueControl
    {
        //TODO: static constructor read this from file
        //TODO: temporarily use the one from s4pi ImageResource wrapper source
        static uint[] resourceTypes = new uint[] {
            0x0580A2B4, //  THUM   .png
            0x0580A2B5, //  THUM   .png
            0x0580A2B6, //  THUM   .png
            0x0580A2CD, //  SNAP   .png
            0x0580A2CE, //  SNAP   .png
            0x0580A2CF, //  SNAP   .png
            0x0589DC44, //  THUM   .png
            0x0589DC45, //  THUM   .png
            0x0589DC46, //  THUM   .png
            0x0589DC47, //  THUM   .png
            0x05B17698, //  THUM   .png
            0x05B17699, //  THUM   .png
            0x05B1769A, //  THUM   .png
            0x05B1B524, //  THUM   .png
            0x05B1B525, //  THUM   .png
            0x05B1B526, //  THUM   .png
            0x0668F635, //  TWNI   .png 
            0x2653E3C8, //  THUM   .png
            0x2653E3C9, //  THUM   .png
            0x2653E3CA, //  THUM   .png
            0x2D4284F0, //  THUM   .png
            0x2D4284F1, //  THUM   .png
            0x2D4284F2, //  THUM   .png
            0x2E75C764, //  ICON   .png
            0x2E75C765, //  ICON   .png
            0x2E75C766, //  ICON   .png
            0x2E75C767, //  ICON   .png
            0x2F7D0002, //  IMAG   .jpg
            0x2F7D0004, //  IMAG   .png
            0x5DE9DBA0, //  THUM   .png
            0x5DE9DBA1, //  THUM   .png
            0x5DE9DBA2, //  THUM   .png
            0x626F60CC, //  THUM   .png
            0x626F60CD, //  THUM   .png
            0x626F60CE, //  THUM   .png
            0x6B6D837D, //  SNAP   .png
            0x6B6D837E, //  SNAP   .png
            0x6B6D837F, //  SNAP   .png
            0xAD366F95, //  THUM   .png
            0xAD366F96, //  THUM   .png
            0xD84E7FC5, //  ICON   .png
            0xD84E7FC6, //  ICON   .png
            0xD84E7FC7, //  ICON   .png
            0xFCEAB65B, //  THUM   .png
        };

        PictureBox pb = new PictureBox();

        public ImageControl(Stream s)
            : base(s)
        {
            if (s == null || s == Stream.Null)
                return;

            pb.Image = Image.FromStream(s);
        }

        public override bool IsAvailable
        {
            get { return true; }
        }

        public override Control ValueControl
        {
            get { return pb; }
        }

        public override IEnumerable<ToolStripItem> GetContextMenuItems(EventHandler cbk)
        {
            yield break;
        }
    }

    class DeformerMapControl : ABuiltInValueControl
    {
        static uint[] resourceTypes = new uint[] {
            0xDB43E069
        };

        Control control = new PictureBox();
        static Type dType = null;
        bool isStreamnull = false;

        static bool hasCasLoaded()
        {
            try
            {
                Assembly a = Assembly.LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "s4pi.CASPartResource.dll"));
                dType = a.GetType("CASPartResource.DeformerMapResource");
                return dType != null;
            }
            catch
            {
                return false;
            }
        }

        public DeformerMapControl(Stream s)
            : base(s)
        {
            if (s == null || s == Stream.Null)
            {
                isStreamnull = true;
            }
            else if (hasCasLoaded() && dType != null)
            {
                Object wrapper = null;
                try
                {
                    wrapper = Activator.CreateInstance(dType, new object[] { 1, s });
                    control = new PictureBox();
                    var bitmapStream = dType.GetMethod("GetSkinBitMap").Invoke(wrapper, null) as Stream;
                    var bitmap = Bitmap.FromStream(bitmapStream);
                    if (bitmap != null)
                    {
                        var pb = control as PictureBox;
                        pb.Image = bitmap;
                    }
                }
                catch
                {
                    control = new RichTextBox()
                    {
                        Font = new Font(FontFamily.GenericMonospace, 8),
                        Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                        ReadOnly = true,
                    };

                    var tbox = control as RichTextBox;
                    if(wrapper != null)
                    {
                        var value = dType.GetProperties().FirstOrDefault(p => p.Name == "WrapperValue").GetValue(wrapper, null);
                        tbox.Text = value.ToString();
                    }
                }
            }
            else { isStreamnull = true; }
            if (isStreamnull)
                control = new Control();
        }

        public override bool IsAvailable
        {
            get { return true; }
        }

        public override Control ValueControl
        {
            get { return control; }
        }

        public override IEnumerable<ToolStripItem> GetContextMenuItems(EventHandler cbk)
        {
            yield break;
        }
    }


    class ThumbnailControl : ABuiltInValueControl
    {
        //TODO: static constructor read this from file
        //TODO: temporarily use the one from s4pi ImageResource wrapper source
        static uint[] resourceTypes = new uint[] {
            0x0D338A3A,
            0x16CCF748,
            0x3BD45407,
            0x3C1AF1F2,
            0x3C2A8647,
            0x4D5B0931,
            0x56278554,
            0x5B282D45,
            0x8E71065D,
            0x9C925813,
            0xA1FF2FC4,
            0xB67673A2,
            0xC1ED6D35,
            0xCD9DE247,
            0xE18CAEE2,
            0xE254AE6E 
        };

        PictureBox pb = new PictureBox();

        public ThumbnailControl(Stream s)
            : base(s)
        {
            if (s == null || s == Stream.Null)
                return;
            ThumbnailResource r = new ThumbnailResource(1, s);
            pb.Image = r.Image;
          //  pb.Image = new Bitmap(r.ToImageStream());
        }

        public override bool IsAvailable
        {
            get { return true; }
        }

        public override Control ValueControl
        {
            get { return pb; }
        }

        public override IEnumerable<ToolStripItem> GetContextMenuItems(EventHandler cbk)
        {
            yield break;
        }
    }

    class LRLEControl : ABuiltInValueControl
    {
        static uint[] resourceTypes = new uint[] {
            0x2BC04EDF 
        };

        PictureBox pb = new PictureBox();

        public LRLEControl(Stream s)
            : base(s)
        {
            if (s == null || s == Stream.Null)
                return;
            LRLEResource r = new LRLEResource(1, s);
            int width = r.Width, height = r.Height;
            while (width > 1024 || height > 512) { width /= 2; height /= 2; }
            Bitmap tmp = new Bitmap(r.Image, width, height);
            pb.Image = tmp;
        }

        public override bool IsAvailable
        {
            get { return true; }
        }

        public override Control ValueControl
        {
            get { return pb; }
        }

        public override IEnumerable<ToolStripItem> GetContextMenuItems(EventHandler cbk)
        {
            yield break;
        }
    }

    //Used directly by MainForm, so needs to be public
    public class TextControl : ABuiltInValueControl
    {
        //TODO: static constructor read this from file
        //TODO: temporarily use the one from s4pi TextResource wrapper source
        static uint[] resourceTypes = new uint[] {
            0x024A0E52, // Spore UTF-8 with encoding bytes NameGeneration.package
            0x025C90A6, // Cascading Style Sheet
            0x025C95B6, // XML with encoding bytes -- <graph/>
            0x029E333B, // Audio controllers
            0x02C9EFF2, // Audio Submix
            0x024A0E52, // SimCity5 Config
            0x02FAC0B6, // Spore "Guide notes" from Text.package?
            0x0333406C, // XML with or without encoding bytes -- <base/> (with), various (without)
            0x03B33DDF, // XML without encoding bytes -- <base/>
            0x0604ABDA, // XML with encoding bytes -- <DreamTree/>
            0x0A98EAF0, // SimCity5 UI JavaScript Object
            0x1F886EAD, // Various configuration bits
            0x2B6CAB5F, // Spore Note in UI.package
            0x67771F5C, // SimCity5 UI JavaScript Object
            0x73E93EEB, // XML without encoding bytes -- <manifest/>
            0xA8D58BE5, // XML without encoding bytes -- <skills_store/>
            0xD4D9FBE5, // XML without encoding bytes -- <patternlist/>
            0xDD3223A7, // XML without encoding bytes -- <buff_store/>
            0xDD6233D6, // SimCity5 UI HTML
            0xE5105066, // ?
            0xE5105067, // XML without encoding bytes -- <RecipeMasterList_store/>
            0xE5105068, // ?
            0xF0FF5598, // Shortcut definitions
        };

        RichTextBox rtb = new RichTextBox()
        {
            Font = new Font(FontFamily.GenericMonospace, 8),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            ReadOnly = true,
        };

        public TextControl(Stream s)
            : base(s)
        {
            if (s == null || s == Stream.Null)
                return;

            rtb.Text = new StreamReader(s).ReadToEnd();
        }

        public TextControl(string s)
            : base(null)
        {
            if (s == null)
                return;

            rtb.Text = s;
        }

        public override bool IsAvailable { get { return true; } }

        public override Control ValueControl { get { return rtb; } }

        public override IEnumerable<ToolStripItem> GetContextMenuItems(EventHandler cbk) { yield break; }
    }

    //Used directly by MainForm, so needs to be public
    public class HexControl : ABuiltInValueControl
    {
        int rowLength = 16;

        RichTextBox rtb = new RichTextBox()
        {
            Font = new Font(FontFamily.GenericMonospace, 10),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            ReadOnly = true,
        };
 
        public HexControl(Stream s)
            : base(s)
        {
            if (s == null || s == Stream.Null)
                return;

            rtb.Text = GetHex(s);
        }

        public override bool IsAvailable { get { return true; } }

        public override Control ValueControl { get { return rtb; } }

        public override IEnumerable<ToolStripItem> GetContextMenuItems(EventHandler cbk) { yield break; }

        private String GetHex(Stream s)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            byte[] b = new byte[s.Length];
            s.Read(b, 0, b.Length);

            int padLength = 8;// +b.Length.ToString("X").Length;
            string rowFmt = "X" + padLength;

            sb.Append("".PadLeft(padLength + 2));
            for (int col = 0; col < rowLength; col++) sb.Append(col.ToString("X2") + " ");
            sb.AppendLine();
            sb.Append("".PadLeft(padLength + 2));
            for (int col = 0; col < rowLength; col++) sb.Append("---");
            sb.AppendLine();

            for (int row = 0; row < b.Length; row += rowLength)
            {
                sb.Append(row.ToString(rowFmt) + ": ");

                int col = 0;
                for (; col < rowLength && row + col < b.Length; col++) sb.Append(b[row + col].ToString("X2") + " ");
                for (; col < rowLength; col++) sb.Append("   ");

                sb.Append(" : ");
                for (col = 0; col < rowLength && row + col < b.Length; col++)
                    sb.Append(b[row + col] < 0x20 || b[row + col] > 0x7e ? '.' : (char)b[row + col]);

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}