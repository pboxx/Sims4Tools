/***************************************************************************
 *  Copyright (C) 2009, 2016 by the Sims 4 Tools development team          *
 *                                                                         *
 *  Contributors:                                                          *
 *  Peter L Jones (pljones@users.sf.net)                                   *
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

namespace S4PIDemoFE
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Windows.Forms;
    using s4pi.Commons.Extensions;
    using s4pi.Extensions;
    using s4pi.Interfaces;
    using s4pi.Package;
    using s4pi.WrapperDealer;

    partial class MainForm
    {
        private const string DataFormatSingleFile = "x-application/s3pe.singleFile";
        private const string DataFormatBatch = "x-application/s3pe.batch";

        private static readonly string[] packageExtensions =
        {
            "package",
            "world",
            "dbc",
            "nhd"
        };

        private static readonly List<uint> xmlList = new List<uint>(new uint[]
                                                                    {
                                                                        0x025C95B6, //xml: UI Layout definitions
                                                                        0x025ED6F4, //xml: Sim Outfit
                                                                        //-Anach: do not allow duplicates 0x0333406C, //xml: XML Resource (Uses include tuning constants)
                                                                        0x03B33DDF,
                                                                        //xml: Interaction Tuning (These are like the stuff you had in TTAB. Motive advertising and autonomy etc)
                                                                        0x044AE110, //xml: Complate Preset
                                                                        0x0604ABDA //xml:
                                                                        //-Anach: delete these 0x73E93EEB, //xml: Package manifest
                                                                    });

        private static readonly List<uint> stblList = new List<uint>(new uint[]
                                                                     {
                                                                         0x220557DA //STBL
                                                                     });

        private static readonly List<uint> nmapList = new List<uint>(new uint[]
                                                                     {
                                                                         0x0166038C //NMAP
                                                                     });

        // See http://dino.drealm.info/den/denforum/index.php?topic=244.0

        private static readonly List<uint> deleteList = new List<uint>(new uint[]
                                                                       {
                                                                           0x73E93EEB //xml: sims3pack manifest
                                                                           //http://dino.drealm.info/den/denforum/index.php?topic=724.0
                                                                           //-Anach: no, this is needed by CAS 0x626F60CD, //THUM: sims3pack
                                                                           //http://dino.drealm.info/den/denforum/index.php?topic=253.msg1234#msg1234
                                                                           //-Anach: no, this is needed by roofing 0x2E75C765, //ICON: sims3pack
                                                                       });

        private static readonly List<uint> allowList = new List<uint>();

        private enum AutoSaveState
        {
            Never,
            Ask,
            Always
        }

        [Serializable]
        public struct MyDataFormat
        {
            public TGIN tgin;
            public byte[] data;
        }

        private void ResourceImport()
        {
            bool useNames = this.controlPanel1.UseNames;
            try
            {
                this.Enabled = false;
                this.browserWidget1.Visible = false;
                this.controlPanel1.UseNames = false;
                DialogResult dr = this.importResourcesDialog.ShowDialog();
                if (dr != DialogResult.OK)
                {
                    return;
                }

                if (this.importResourcesDialog.FileNames.Length > 1)
                {
                    this.ImportBatch(this.importResourcesDialog.FileNames, this.importResourcesDialog.Title);
                }
                else
                {
                    this.ImportSingle(this.importResourcesDialog.FileName, this.importResourcesDialog.Title);
                }
            }
            finally
            {
                this.controlPanel1.UseNames = useNames;
                this.browserWidget1.Visible = true;
                this.Enabled = true;
            }
        }

        private void ResourceImportPackages()
        {
            try
            {
                this.Enabled = false;
                DialogResult dr = this.importPackagesDialog.ShowDialog();
                if (dr != DialogResult.OK)
                {
                    return;
                }

                ImportBatch ib = new ImportBatch(this.importPackagesDialog.FileNames, S4PIDemoFE.ImportBatch.Mode.package);
                dr = ib.ShowDialog();
                if (dr != DialogResult.OK)
                {
                    return;
                }

                this.ImportPackagesCommon(ib.Batch,
                    this.importPackagesDialog.Title,
                    ib.Replace ? DuplicateHandling.Replace : DuplicateHandling.Reject,
                    ib.Compress,
                    ib.UseNames,
                    rename: ib.Rename);
            }
            finally
            {
                this.Enabled = true;
            }
        }

        private void ResourceReplaceFrom()
        {
            var savedTitle = this.importPackagesDialog.Title;
            try
            {
                this.Enabled = false;
                this.importPackagesDialog.Title = @"Replace Selected Resources from Package(s)";
                DialogResult dr = this.importPackagesDialog.ShowDialog();
                if (dr != DialogResult.OK)
                {
                    return;
                }

                ImportBatch ib = new ImportBatch(this.importPackagesDialog.FileNames, S4PIDemoFE.ImportBatch.Mode.replaceFrom);
                dr = ib.ShowDialog();
                if (dr != DialogResult.OK)
                {
                    return;
                }

                this.ImportPackagesCommon(ib.Batch,
                    this.importPackagesDialog.Title,
                    DuplicateHandling.Replace,
                    ib.Compress,
                    selection: this.browserWidget1.SelectedResources);
            }
            finally
            {
                this.Enabled = true;
                this.importPackagesDialog.Title = savedTitle;
            }
        }

        private void ResourceImportAsDbc()
        {
            if (MainForm.allowList.Count == 0)
            {
                MainForm.allowList.AddRange(MainForm.xmlList);
                MainForm.allowList.AddRange(MainForm.stblList);
                MainForm.allowList.AddRange(MainForm.nmapList);
            }
            try
            {
                this.Enabled = false;
                DialogResult dr = this.importPackagesDialog.ShowDialog();
                if (dr != DialogResult.OK)
                {
                    return;
                }

                AutoSaveState autoSaveState = AutoSaveState.Always;
                if (Properties.Settings.Default.AskDBCAutoSave)
                {
                    autoSaveState = AutoSaveState.Ask;
                }
                this.ImportPackagesCommon(this.importPackagesDialog.FileNames,
                    this.importPackagesDialog.Title,
                    DuplicateHandling.Allow,
                    true,
                    useNames: true,
                    dupsList: MainForm.allowList,
                    autoSaveState: autoSaveState);

                this.browserWidget1.Visible = false;
                this.lbProgress.Text = @"Doing DBC clean up...";

                Application.DoEvents();
                DateTime now = DateTime.UtcNow;
                System.Threading.Tasks.Task tt = new System.Threading.Tasks.Task(delegate
                {
                    IList<IResourceIndexEntry> lrie =
                        this.DupsOnly(this.CurrentPackage.FindAll(x => MainForm.stblList.Contains(x.ResourceType)));
               

                foreach (IResourceIndexEntry dup in lrie)
                {
                    IList<IResourceIndexEntry> ldups =
                        this.CurrentPackage.FindAll(rie => ((IResourceKey)dup).Equals(rie));
                    IResourceIndexEntry newRie = this.NewResource(dup, null, DuplicateHandling.Allow, true);
                    IDictionary<ulong, string> newStbl =
                        (IDictionary<ulong, string>)WrapperDealer.GetResource(0, this.CurrentPackage, newRie);
                    foreach (IResourceIndexEntry rie in ldups)
                    {
                        IDictionary<ulong, string> oldStbl =
                            (IDictionary<ulong, string>)WrapperDealer.GetResource(0, this.CurrentPackage, rie);
                        foreach (var kvp in oldStbl)
                        {
                            if (!newStbl.ContainsKey(kvp.Key))
                            {
                                newStbl.Add(kvp);
                            }
                        }
                        rie.IsDeleted = true;
                        if (now.AddMilliseconds(100) < DateTime.UtcNow)
                        {
                            Application.DoEvents();
                            now = DateTime.UtcNow;
                        }
                    }
                    this.CurrentPackage.ReplaceResource(newRie, (IResource)newStbl);
                    this.browserWidget1.Invoke(new Action(() => 
                    {
                        this.browserWidget1.Add(newRie, false);
                    }));

                }
                });

                tt.Start();
                // Get rid of Sims3Pack resource that sneak in
                this.CurrentPackage.FindAll(x =>
                                            {
                                                if (now.AddMilliseconds(100) < DateTime.UtcNow)
                                                {
                                                    Application.DoEvents();
                                                    now = DateTime.UtcNow;
                                                }
                                                if (MainForm.deleteList.Contains(x.ResourceType))
                                                {
                                                    x.IsDeleted = true;
                                                    return false;
                                                }
                                                return false;
                                            });

                // If there are any remaining duplicate XMLs, give up - they're too messy to fix automatically
                if (this.DupsOnly(this.CurrentPackage.FindAll(x => MainForm.xmlList.Contains(x.ResourceType))).Count > 0)
                {
                    CopyableMessageBox.Show("Manual merge of XML files required.");
                }
            }
            finally
            {
                this.browserWidget1.Visible = true;
                this.lbProgress.Text = "";
                Application.DoEvents();
                this.Enabled = true;
            }
        }

        private IList<IResourceIndexEntry> DupsOnly(IEnumerable<IResourceIndexEntry> list)
        {
            List<IResourceKey> seen = new List<IResourceKey>();
            List<IResourceIndexEntry> res = new List<IResourceIndexEntry>();
            foreach (IResourceIndexEntry rie in list)
            {
                if (!seen.Contains(rie))
                {
                    seen.Add(rie);
                }
                else if (!res.Contains(rie))
                {
                    res.Add(rie);
                }
            }
            return res;
        }

        private void ImportPackagesCommon(string[] packageList,
                                          string title,
                                          DuplicateHandling dups,
                                          bool compress,
                                          bool useNames = false,
                                          bool rename = false,
                                          List<uint> dupsList = null,
                                          AutoSaveState autoSaveState = AutoSaveState.Ask,
                                          IList<IResourceIndexEntry> selection = null
            )
        {
            bool cpUseNames = this.controlPanel1.UseNames;
            DateTime now = DateTime.UtcNow;

            bool autoSave = false;
            if (autoSaveState == AutoSaveState.Ask)
            {
                switch (CopyableMessageBox.Show("Auto-save current package after each package imported?",
                    title,
                    CopyableMessageBoxButtons.YesNoCancel,
                    CopyableMessageBoxIcon.Question))
                {
                    case 0:
                        autoSave = true;
                        break;
                    case 2:
                        return;
                }
            }
            else
            {
                autoSave = autoSaveState == AutoSaveState.Always;
            }

            try
            {
                this.browserWidget1.Visible = false;
                this.controlPanel1.UseNames = false;

                bool skipAll = false;
                foreach (string filename in packageList)
                {
                    if (!string.IsNullOrEmpty(this.Filename)
                        && Path.GetFullPath(this.Filename).Equals(Path.GetFullPath(filename)))
                    {
                        CopyableMessageBox.Show("Skipping current package.", this.importPackagesDialog.Title);
                        continue;
                    }

                    this.lbProgress.Text = "Importing " + Path.GetFileNameWithoutExtension(filename) + "...";
                    Application.DoEvents();
                    IPackage imppkg = null;
                    try
                    {
                        imppkg = Package.OpenPackage(0, filename);
                    }
                    catch (InvalidDataException ex)
                    {
                        if (skipAll)
                        {
                            continue;
                        }
                        int btn =
                            CopyableMessageBox.Show(
                                string.Format("Could not open package {0}.\n{1}", Path.GetFileName(filename), ex.Message),
                                title,
                                CopyableMessageBoxIcon.Error,
                                new List<string>(new[] { "Skip this", "Skip all", "Abort" }),
                                0,
                                0);
                        if (btn == 0)
                        {
                            continue;
                        }
                        if (btn == 1)
                        {
                            skipAll = true;
                            continue;
                        }
                        break;
                    }
                    try
                    {

                        List<Tuple<MyDataFormat, DuplicateHandling>> limp =
                            new List<Tuple<MyDataFormat, DuplicateHandling>>();
                        List<IResourceIndexEntry> lrie = selection == null
                            ? imppkg.GetResourceList
                            : imppkg.FindAll(rie => selection.Any(tgt => ((AResourceKey)tgt).Equals(rie)));
                        this.progressBar1.Value = 0;
                        this.progressBar1.Maximum = lrie.Count;


                        Application.DoEvents();

                        for (int r = 0; r < lrie.Count; r++)
                        {
                            try
                            {
                                if (lrie[r].ResourceType == 0x0166038C) //NMAP
                                {
                                    if (useNames)
                                    {
                                        this.browserWidget1.Invoke(new Action(() =>
                                        {
                                            this.browserWidget1.MergeNamemap(
                                              WrapperDealer.GetResource(0, imppkg, lrie[r]) as IDictionary<ulong, string>,
                                              true,
                                              rename);
                                        }));
                                         
                                       

                                    }
                                }
                                else
                                {
                                    //-- this area is doing the heavy lifting.
                                    Application.DoEvents();
                                    IResource res = WrapperDealer.GetResource(0, imppkg, lrie[r], true);
                                    MyDataFormat impres = new MyDataFormat()
                                    {
                                        tgin = lrie[r] as AResourceIndexEntry,
                                        data = res.AsBytes
                                    };
                                    // dups Replace | Reject | Allow
                                    // dupsList null | list of allowable dup types
                                    DuplicateHandling dupThis =
                                            dups == DuplicateHandling.Allow
                                                ? dupsList == null || dupsList.Contains(lrie[r].ResourceType)
                                                    ? DuplicateHandling.Allow
                                                    : DuplicateHandling.Replace
                                                : dups;
                                    
                                    limp.Add(Tuple.Create(impres, dupThis));
                                    this.progressBar1.Invoke(new Action(() =>
                                    {
                                        //-- apparently showing app is doing something.   
                                        this.progressBar1.Value++;
                                        if (now.AddMilliseconds(100) < DateTime.UtcNow)
                                        {
                                            Application.DoEvents();
                                            now = DateTime.UtcNow;
                                        }
                                    }));
                          
                                }

                       
                            }
                            catch (Exception ex)
                            {
                                string rk = "";
                                if (lrie[r] != null)
                                {
                                    rk = "(RK: " + lrie[r] + ")\n";
                                }
                                else
                                {
                                    rk = "(RK is null)\n";
                                }

                                CopyableMessageBox.IssueException(ex,
                                    "Could not import all resources - aborting.\n" + rk,
                                    title);
                                throw new IgnoredException(ex);
                            }

                        }

                        this.progressBar1.Value = 0;
                   
                            this.browserWidget1.Invoke(new Action(() =>
                            {
                                IEnumerable<IResourceIndexEntry> rieList = limp
                                .Select(
                                    x =>
                                        this.NewResource((AResourceKey)x.Item1.tgin,
                                            new MemoryStream(x.Item1.data),
                                            x.Item2,
                                            compress))
                                .Where(x => x != null);
                           
                                this.browserWidget1.AddRange(rieList);
                   
                            }));

                    }
                    catch (IgnoredException)
                    {
                        //just the thrown exception, stop looping
                        break;
                    }
                    catch (Exception ex)
                    {
                        CopyableMessageBox.IssueException(ex, "Could not import all resources - aborting.\n", title);
                        break;
                    }
                    finally
                    {
                        imppkg.Dispose();
                    }
                    if (autoSave && !this.FileSave())
                    {
                        break;
                    }
                }
            }
            finally
            {
                this.lbProgress.Text = "";
                this.progressBar1.Value = 0;
                this.progressBar1.Maximum = 0;
                this.controlPanel1.UseNames = cpUseNames;
                this.browserWidget1.Visible = true;
                ForceFocus.Focus(Application.OpenForms[0]);
                Application.DoEvents();
            }
        }

        private class IgnoredException : Exception
        {
            public IgnoredException(Exception innerException) : base("Ignore this", innerException)
            {
            }
        }

        private void ResourcePaste()
        {
            try
            {
                this.Enabled = false;
                if (Clipboard.ContainsData(MainForm.DataFormatSingleFile))
                {
                    IFormatter formatter = new BinaryFormatter();
                    Stream stream = Clipboard.GetData(MainForm.DataFormatSingleFile) as MemoryStream;
                    MyDataFormat d = (MyDataFormat)formatter.Deserialize(stream);
                    stream.Close();

                    this.ImportSingle(d);
                }
                else if (Clipboard.ContainsData(MainForm.DataFormatBatch))
                {
                    IFormatter formatter = new BinaryFormatter();
                    Stream stream = Clipboard.GetData(MainForm.DataFormatBatch) as MemoryStream;
                    List<MyDataFormat> l = (List<MyDataFormat>)formatter.Deserialize(stream);
                    stream.Close();

                    this.ImportBatch(l);
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    StringCollection fileDrop = Clipboard.GetFileDropList();
                    if (fileDrop.Count == 0)
                    {
                        return;
                    }

                    if (fileDrop.Count == 1)
                    {
                        this.ImportSingle(fileDrop[0], "Resource->Paste");
                    }
                    else
                    {
                        string[] batch = fileDrop.OfType<string>().ToArray();

                        this.ImportBatch(batch, "Resource->Paste");
                    }
                }
            }
            finally
            {
                this.Enabled = true;
            }
        }

        private void browserWidget1_DragDrop(object sender, DragEventArgs e)
        {
            string[] fileDrop = e.Data.GetData("FileDrop") as string[];
            if (fileDrop == null || fileDrop.Length == 0)
            {
                return;
            }

            Application.DoEvents();
            try
            {
                this.Enabled = false;
                if (fileDrop.Length > 1)
                {
                    this.ImportBatch(fileDrop, "File(s)->Drop");
                }
                else if (Directory.Exists(fileDrop[0]))
                {
                    this.ImportBatch(fileDrop, "File(s)->Drop");
                }
                else
                {
                    this.ImportSingle(fileDrop[0], "File(s)->Drop");
                }
            }
            finally
            {
                this.Enabled = true;
            }
        }

        private void ImportSingle(string filename, string title)
        {
            if (this.CurrentPackage == null)
            {
                this.FileNew();
            }

            if (packageExtensions.Contains(filename.FileExtension()))
            {
                ImportBatch ib = new ImportBatch(new[] { filename }, S4PIDemoFE.ImportBatch.Mode.package);
                DialogResult dr = ib.ShowDialog();
                if (dr != DialogResult.OK)
                {
                    return;
                }

                this.ImportPackagesCommon(new[] { filename },
                    title,
                    ib.Replace ? DuplicateHandling.Replace : DuplicateHandling.Reject,
                    ib.Compress,
                    ib.UseNames);
            }
            else
            {
                ResourceDetails ir = new ResourceDetails(true, true) { Filename = filename };
                DialogResult dr = ir.ShowDialog();
                if (dr != DialogResult.OK)
                {
                    return;
                }

                this.ImportFile(ir.Filename,
                    ir,
                    ir.UseName,
                    ir.AllowRename,
                    ir.Compress,
                    ir.Replace ? DuplicateHandling.Replace : DuplicateHandling.Reject,
                    true);
            }
        }

        private void ImportSingle(MyDataFormat data)
        {
            ResourceDetails ir = new ResourceDetails(true, true) { Filename = data.tgin };
            DialogResult dr = ir.ShowDialog();
            if (dr != DialogResult.OK)
            {
                return;
            }

            data.tgin = ir;
            this.ImportStream(data,
                ir.UseName,
                ir.AllowRename,
                ir.Compress,
                ir.Replace ? DuplicateHandling.Replace : DuplicateHandling.Reject,
                true);
        }

        private IEnumerable<string> GetFiles(string folder)
        {
            if (!Directory.Exists(folder))
            {
                return null;
            }
            List<string> files = new List<string>();
            foreach (string dir in Directory.GetDirectories(folder))
            {
                files.AddRange(this.GetFiles(dir));
            }
            files.AddRange(Directory.GetFiles(folder));
            return files.ToArray();
        }

        private void ImportBatch(string[] batch, string title)
        {
            if (this.CurrentPackage == null)
            {
                this.FileNew();
            }

            List<string> allFiles = new List<string>();
            foreach (string path in batch)
            {
                if (Directory.Exists(path))
                {
                    allFiles.AddRange(this.GetFiles(path));
                }
                else if (File.Exists(path))
                {
                    allFiles.Add(path);
                }
            }
            batch = allFiles.ToArray();

            ImportBatch importBatch = new ImportBatch(batch) { Text = title };
            DialogResult result = importBatch.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }

            List<string> resources = batch.Where(p => !packageExtensions.Contains(p.FileExtension())).ToList();
            List<string> packages = batch.Where(p => packageExtensions.Contains(p.FileExtension())).ToList();

            try
            {
                if (packages.Count > 0)
                {
                    this.ImportPackagesCommon(packages.ToArray(),
                        title,
                        importBatch.Replace ? DuplicateHandling.Replace : DuplicateHandling.Reject,
                        importBatch.Compress,
                        importBatch.UseNames);
                }

                bool nmOK = true;
                foreach (string file in resources)
                {
                    nmOK = this.ImportFile(file,
                        file,
                        nmOK && importBatch.UseNames,
                        importBatch.Rename,
                        importBatch.Compress,
                        importBatch.Replace ? DuplicateHandling.Replace : DuplicateHandling.Reject,
                        false);
                    Application.DoEvents();
                }
            }
            catch (Exception ex)
            {
                CopyableMessageBox.IssueException(ex, "Could not import all resources - aborting.\n", title);
            }
        }

        private void ImportBatch(IList<MyDataFormat> ldata)
        {
            ImportBatch importBatch = new ImportBatch(ldata);
            DialogResult result = importBatch.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }

            List<MyDataFormat> output = new List<MyDataFormat>();
            foreach (string b in importBatch.Batch)
            {
                foreach (MyDataFormat data in ldata)
                {
                    if (data.tgin == b)
                    {
                        output.Add(data);
                        break;
                    }
                }
            }

            if (output.Count == 0)
            {
                return;
            }
            if (output.Count == 1)
            {
                this.ImportSingle(output[0]);
                return;
            }

            try
            {
                foreach (MyDataFormat data in output)
                {
                    this.ImportStream(data,
                        importBatch.UseNames,
                        importBatch.Rename,
                        importBatch.Compress,
                        importBatch.Replace ? DuplicateHandling.Replace : DuplicateHandling.Reject,
                        false);
                    Application.DoEvents();
                }
            }
            catch (Exception ex)
            {
                CopyableMessageBox.IssueException(ex, "Could not import all resources.\n", "Aborting import");
            }
        }

        private bool ImportFile(string filename,
                                TGIN tgin,
                                bool useName,
                                bool rename,
                                bool compress,
                                DuplicateHandling dups,
                                bool select)
        {
            IResourceKey rk = (TGIBlock)tgin;
            string resName = tgin.ResName;
            bool nmOK = true;
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);
            BinaryReader r = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read));
            w.Write(r.ReadBytes((int)r.BaseStream.Length));
            r.Close();
            w.Flush();

            if (useName && !string.IsNullOrEmpty(resName))
            {
                nmOK = this.browserWidget1.ResourceName(rk.Instance, resName, true, rename);
            }

            IResourceIndexEntry rie = this.NewResource(rk, ms, dups, compress);
            if (rie != null)
            {
                this.browserWidget1.Add(rie, select);
            }
            return nmOK;
        }

        private void ImportStream(MyDataFormat data,
                                  bool useName,
                                  bool rename,
                                  bool compress,
                                  DuplicateHandling dups,
                                  bool select)
        {
            if (useName && !string.IsNullOrEmpty(data.tgin.ResName))
            {
                this.browserWidget1.ResourceName(data.tgin.ResInstance, data.tgin.ResName, true, rename);
            }

            IResourceIndexEntry rie = this.NewResource((TGIBlock)data.tgin, new MemoryStream(data.data), dups, compress);
            if (rie != null)
            {
                this.browserWidget1.Add(rie, select);
            }
        }
    }
}