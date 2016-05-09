/***************************************************************************
 *  Copyright (C) 2009, 2010 by Peter L Jones                              *
 *  pljones@users.sf.net                                                   *
 *                                                                         *
 *  This file is part of the Sims 3 Package Interface (s3pi)               *
 *                                                                         *
 *  s3pi is free software: you can redistribute it and/or modify           *
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
 *  along with s3pi.  If not, see <http://www.gnu.org/licenses/>.          *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using s4pi.Interfaces;

namespace S4PIDemoFE
{
    public partial class BrowserWidget : UserControl
    {
        #region Attributes
        IList<string> fields = null;
        ProgressBar pb = null;
        Label pbLabel = null;
        IPackage pkg = null;
        ListViewColumnSorter lvwColumnSorter;
        string sortColumn = "Chunkoffset";
        bool displayResourceNames = false;
        bool displayResourceTags = false;
        IList<KeyValuePair<string, Regex>> filter = null;

        //used internally
        Dictionary<IResourceIndexEntry, ListViewItem> lookup = null;
        IList<IResourceIndexEntry> resourceList = null;
        List<IResource> nameMap = null;
        List<IResourceIndexEntry> nameMapRIEs = null;
        ListViewItem selectedResource = null;
        bool internalchg = false;
        #endregion

        const int tock = 250;//ms to update progress bar

        public BrowserWidget()
        {
            InitializeComponent();
            BrowserWidget_LoadListSettings();

            lvwColumnSorter = new ListViewColumnSorter();
            lookup = new Dictionary<IResourceIndexEntry, ListViewItem>();
            OnListUpdated(this, new EventArgs());
        }

        int[] columnWidths = new int[0];
        void BrowserWidget_LoadListSettings()
        {
            string cw = S4PIDemoFE.Properties.Settings.Default.ColumnWidths;
            if (cw != null && cw.Length > 0)
            {
                string[] cws = cw == null ? new string[] { } : cw.Split(':');

                int w;
                columnWidths = new int[cws.Length];
                for (int i = 0; i < cws.Length; i++)
                    columnWidths[i] = int.TryParse(cws[i], out w) && w > 0 ? w : -1;
            }
        }
        public void BrowserWidget_SaveSettings(object sender, EventArgs e)
        {
            lvwColumnSorter.ListViewColumnSorter_SaveSettings(sender, e);

            if (listView1.Columns.Count > 0)
            {
                string columnWidths = "";
                foreach (ColumnHeader ch in listView1.Columns)
                    columnWidths += ":" + ch.Width;
                S4PIDemoFE.Properties.Settings.Default.ColumnWidths = columnWidths.Trim(':');
            }
        }

        #region Methods
        public void Add(IResourceIndexEntry rie, bool select = true)
        {
            AddRange(new[] { rie, });
            if (select)
                SelectedResource = rie;
        }

        public void AddRange(IEnumerable<IResourceIndexEntry> range)
        {
            var cmp = listView1.ListViewItemSorter;
            string pbOldLabel = pbLabel.Text;
            int pbOldMaximum = -1;
            int pbOldValue = -1;
            if (pb != null)
            {
                pbOldMaximum = pb.Maximum;
                pbOldValue = pb.Value;
            }

            try
            {
                    listView1.SuspendLayout();
                    listView1.BeginUpdate();
                    listView1.ListViewItemSorter = null;

                    SelectedResource = null;//not restored for AddRange
                   
                    Application.DoEvents();

                 
                   
                        this.pbLabel.Invoke(new Action(() =>
                        {
                            pbLabel.Text = "Reading resources...";
                        }));

                
                    List<IResourceIndexEntry> lrie = range.ToList();

                    if (pb != null)
                    {
                        pb.Value = 0;
                        pb.Maximum = lrie.Count();
                    }

                    bool newNameMap = false;
                    int i = 0;
                    DateTime tick = DateTime.UtcNow.AddMilliseconds(tock);
                    List<Tuple<IResourceIndexEntry, ListViewItem>> list = new List<Tuple<IResourceIndexEntry, ListViewItem>>();
                    
                    System.Threading.Thread task = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(delegate
                    {
                        foreach (var pair in lrie.Select(x =>
                        {
                            i++;
                            return Tuple.Create(x, CreateItem(x));
                        }).Where(x => x.Item2 != null))
                        {
                            try
                            {
                                list.Add(pair);
                                lookup.Add(pair.Item1, pair.Item2);
                                if (pair.Item1.ResourceType == 0x0166038C) newNameMap = true;
                            }
                            finally
                            {
                                if (tick < DateTime.UtcNow)
                                {
                                    tick = DateTime.UtcNow.AddMilliseconds(tock);
                                    if (pb != null) { pb.Value = i; Application.DoEvents(); }
                                }
                            }
                        }
              

                    pb.Value = 0;
                    pbLabel.Invoke(new Action(() =>
                    {
                        pbLabel.Text = "Updating resource list...";
                    }));

                    Application.DoEvents();
                    listView1.Invoke(new Action(() =>
                    {
                        listView1.Items.AddRange(list.Select(x => x.Item2).ToArray());
                    }));

                    if (newNameMap) { ClearNameMap(); nameMap_ResourceChanged(null, null); }
               
                    }));
                    task.Start();

            }
            finally
            {
                pbLabel.Text = pbOldLabel;
                if (pb != null)
                {
                    pb.Value = 0;
                    pb.Maximum = pbOldMaximum;
                    pb.Value = pbOldValue;
                }
                listView1.ListViewItemSorter = cmp;
                listView1.EndUpdate();
                listView1.ResumeLayout();
            }
        }

        public bool MergeNamemap(IEnumerable<KeyValuePair<ulong, string>> newMap, bool create, bool replace)
        {
            if (nameMap == null) CreateNameMap();
            if (nameMap == null || nameMap.Count == 0 && create)
            {
                IResourceIndexEntry rie = pkg.AddResource(new TGIBlock(0, null, 0x0166038C, 0, 0), null, false);
                if (rie != null) Add(rie, false);
                CreateNameMap();
                if (nameMap == null)
                {
                    string s = "Resource names cannot be added.  Other than that, you should be fine.  Carry on.";
                    CopyableMessageBox.Show(s, "s3pe", CopyableMessageBoxButtons.OK, CopyableMessageBoxIcon.Warning);
                    return false;
                }
            }

            try
            {
                IDictionary<ulong, string> nmap = nameMap[0] as IDictionary<ulong, string>;
                if (nmap == null) return false;

                foreach (var kvp in newMap)
                {
                    if (nmap.ContainsKey(kvp.Key))
                    {
                        if (replace) nmap[kvp.Key] = kvp.Value;
                    }
                    else
                        nmap.Add(kvp.Key, kvp.Value);
                }

                pkg.ReplaceResource(nameMapRIEs[0], nameMap[0]);

                if (selectedResource != null && selectedResource.Tag as IResourceIndexEntry == nameMapRIEs[0])
                    OnSelectedResourceChanged(this, new ResourceChangedEventArgs(selectedResource));
            }
            catch (Exception ex)
            {
                string s = "Resource names cannot be added.  Other than that, you should be fine.  Carry on.";
                s += String.Format("\n\nError updating _KEY {0}\n", "" + nameMapRIEs[0]);
                MainForm.IssueException(ex, s);
                return false;
            }
            return true;
        }

        public bool ResourceName(ulong instance, string resourceName, bool create, bool replace)
        {
            return MergeNamemap(new[] { new KeyValuePair<ulong, string>(instance, resourceName), }, create, replace);
        }

        public string ResourceName(IResourceIndexEntry rie)
        {
            if (!displayResourceNames) return "";
            if (nameMap == null) return "";
            foreach (IDictionary<ulong, string> nmap in nameMap)
                if (nmap.ContainsKey(rie.Instance)) return nmap[rie.Instance];
            return "";
        }

        public void SelectAll()
        {
            if (((MainForm)ParentForm).IsClosing) return;

            string pbOldLabel = pbLabel.Text;
            int pbOldMaximum = -1;
            int pbOldValue = -1;
            if (pb != null)
            {
                pbOldMaximum = pb.Maximum;
                pbOldValue = pb.Value;
            }
            bool lvb = listView1.Visible;
            bool uwc = Application.UseWaitCursor;

            DateTime tick = DateTime.UtcNow.AddMilliseconds(tock);
            try
            {
                Application.UseWaitCursor = true;
                listView1.SuspendLayout();
                listView1.BeginUpdate();
                listView1.Visible = false;

                internalchg = true;

                SelectedResource = null;//not restored for SelectAll
                pbLabel.Text = "Selecting all resources...";
                Application.DoEvents();
                using (Splash splash = new Splash(pbLabel.Text))
                {
                    splash.Show();
                    Application.DoEvents();

                    if (pb != null)
                    {
                        pb.Value = 0;
                        pb.Maximum = listView1.Items.Count;
                    }

                    listView1.SelectedItems.Clear();

                    for (int i = 0; i < listView1.Items.Count; i++)
                    {
                        listView1.Items[i].Selected = true;
                        if (DateTime.UtcNow > tick)
                        {
                            if (((MainForm)ParentForm).IsClosing) return;

                            if (pb != null) { pb.Value = listView1.SelectedItems.Count; Application.DoEvents(); }
                            Application.DoEvents();
                            tick = DateTime.UtcNow.AddMilliseconds(tock);
                        }
                    }
                }
            }
            finally
            {
                pbLabel.Text = pbOldLabel;
                if (pb != null)
                {
                    pb.Value = 0;
                    pb.Maximum = pbOldMaximum;
                    pb.Value = pbOldValue;
                }
                listView1.Visible = lvb;
                listView1.EndUpdate();
                listView1.ResumeLayout();
                Application.UseWaitCursor = uwc;
                internalchg = false;
            }
            if (listView1.SelectedItems.Count == 1)
                listView1_SelectedIndexChanged(this, EventArgs.Empty);
        }
        #endregion

        #region Properties
        [Browsable(true)]
        [Category("Behavior")]
        [Description("Count of items in the widget")]
        public int Count { get { return listView1.Items.Count; } }

        [Browsable(true)]
        [Category("Behavior")]
        [DefaultValue(false)]
        [Description("Set to true to display resource names found in the package")]
        public bool DisplayResourceNames
        {
            get { return displayResourceNames; }
            set
            {
                if (displayResourceNames == value) return;
                displayResourceNames = value;

                if (pkg != null && displayResourceNames) { nameMap = null; nameMap_ResourceChanged(null, null); }
                else setResourceNameWidth();
            }
        }

        [Browsable(true)]
        [Category("Behavior")]
        [DefaultValue(false)]
        [Description("Set to true to display resource tags")]
        public bool DisplayResourceTags
        {
            get { return displayResourceTags; }
            set
            {
                if (displayResourceTags == value) return;
                displayResourceTags = value;

                if (listView1.Columns.Count == 0) return;
                listView1.BeginUpdate();
                listView1.Columns[1].Width = displayResourceTags ? listView1.Columns[1].Width == 0 ? (columnWidths.Length > 1 && columnWidths[1] >= 0 ? columnWidths[1] : 50) : listView1.Columns[1].Width : 0;
                listView1.EndUpdate();
            }
        }

        [Browsable(true)]
        [Category("Behavior")]
        [DefaultValue(true)]
        [Description("When true, clicking a column heading will sort on that column")]
        public bool Sortable
        {
            get { return this.listView1.ListViewItemSorter != null; }
            set
            {
                if (value == Sortable) return;

                bool uwc = Application.UseWaitCursor;
                Application.UseWaitCursor = true;
                listView1.BeginUpdate();
                pbLabel.Text = "Sorting display...";
                Application.DoEvents();

                try
                {
                    this.listView1.ListViewItemSorter = value ? lvwColumnSorter : null;
                    listView1.Sorting = value ? SortOrder.Ascending : SortOrder.None;
                    listView1.HeaderStyle = value ? ColumnHeaderStyle.Clickable : ColumnHeaderStyle.Nonclickable;
                }
                finally { listView1.EndUpdate(); pbLabel.Text = ""; Application.UseWaitCursor = uwc; Application.DoEvents(); }
            }
        }

        [Browsable(true)]
        [Category("Behavior")]
        [DefaultValue("Chunkoffset")]
        [Description("Specifies the column to sort on by default")]
        public string SortColumn
        {
            get { return sortColumn; }
            set
            {
                if (sortColumn == value) return;
                if (fields.IndexOf(sortColumn) >= 0)
                {
                    sortColumn = value;
                    ColumnClickEventArgs e = new ColumnClickEventArgs(fields.IndexOf(sortColumn));
                    listView1_ColumnClick(this, e);
                }
            }
        }


        //----

        /// <summary>
        /// Specifies the list of fields to display
        /// </summary>
        [Browsable(false)]
        public IList<string> Fields
        {
            get { return fields; }
            set
            {
                if (fields == value) return;
                fields = value;
                SetColumns();
            }
        }

        /// <summary>
        /// Specify how to filter the package resource list
        /// </summary>
        [Browsable(false)]
        public IList<KeyValuePair<string, Regex>> Filter
        {
            get { return filter; }
            set
            {
                if (filter == null && value == null) return;
                filter = value;
                pkg_ResourceIndexInvalidated(null, null);
            }
        }

        /// <summary>
        /// The current package
        /// </summary>
        [Browsable(false)]
        public IPackage Package
        {
            get { return null; }
            set
            {
                if (pkg == value) return;
                if (pkg != null)
                    pkg.ResourceIndexInvalidated -= new EventHandler(pkg_ResourceIndexInvalidated);
                pkg = value;
                pkg_ResourceIndexInvalidated(null, null);
                if (pkg != null)
                    pkg.ResourceIndexInvalidated += new EventHandler(pkg_ResourceIndexInvalidated);
            }
        }

        /// <summary>
        /// A progress bar to be used during long operations
        /// </summary>
        [Browsable(false)]
        public ProgressBar ProgressBar { get { return pb; } set { pb = value; } }

        /// <summary>
        /// A progress label to be used during long operations
        /// </summary>
        [Browsable(false)]
        public Label ProgressLabel { get { return pbLabel; } set { pbLabel = value; } }

        /// <summary>
        /// Set the Resource Key for a list entry
        /// </summary>
        [Browsable(false)]
        public IResourceKey ResourceKey
        {
            get { return SelectedResource; }
            set
            {
                if (selectedResource == null)
                {
                    if (value != null)
                        throw new InvalidOperationException();
                    else return;
                }

                IResourceIndexEntry rie = selectedResource.Tag as IResourceIndexEntry;
                ListViewItem lvi = lookup.ContainsKey(rie) ? lookup[rie] : null;
                if (lvi != null) lookup.Remove(rie);

                (rie as AResourceIndexEntry).ResourceIndexEntryChanged -= new EventHandler(BrowserWidget_ResourceIndexEntryChanged);
                rie.ResourceType = value.ResourceType;
                rie.ResourceGroup = value.ResourceGroup;
                rie.Instance = value.Instance;
                (rie as AResourceIndexEntry).ResourceIndexEntryChanged += new EventHandler(BrowserWidget_ResourceIndexEntryChanged);

                if (lvi != null) lookup.Add(rie, lvi);

                if (nameMapRIEs != null && nameMapRIEs.Contains(rie)) { ClearNameMap(); nameMap_ResourceChanged(null, null); }

                BrowserWidget_ResourceIndexEntryChanged(rie, EventArgs.Empty);
            }
        }

        /// <summary>
        /// The single resource selected
        /// </summary>
        [Browsable(false)]
        public IResourceIndexEntry SelectedResource
        {
            get { return selectedResource == null ? null : selectedResource.Tag as IResourceIndexEntry; }
            set
            {
                ListViewItem lvi = value != null && lookup != null && lookup.ContainsKey(value) ? lookup[value] : null;
                if (listView1.SelectedItems.Count == 0 && value == null) return;
                if (listView1.SelectedItems.Count == 1 && listView1.SelectedItems[0] == value) return;

                internalchg = true;
                listView1.SelectedItems.Clear();
                if (lvi != null) listView1.SelectedIndices.Add(listView1.Items.IndexOf(lvi));
                if (listView1.SelectedItems.Count == 1) listView1.SelectedItems[0].EnsureVisible();
                internalchg = false;
                listView1_SelectedIndexChanged(null, null);
            }
        }

        /// <summary>
        /// The list of resources selected
        /// </summary>
        [Browsable(false)]
        public IList<IResourceIndexEntry> SelectedResources
        {
            get
            {
                List<IResourceIndexEntry> res = new List<IResourceIndexEntry>();
                foreach (ListViewItem lvi in listView1.SelectedItems)
                    if (lvi.Tag as IResourceIndexEntry != null) res.Add(lvi.Tag as IResourceIndexEntry);
                return res;
            }
        }
        #endregion

        #region Events
        [Browsable(true)]
        [Category("Property Changed")]
        [Description("Raised when the list content is updated")]
        public event EventHandler ListUpdated;

        [Browsable(true)]
        [Category("Action")]
        [Description("Raised when an item is activated")]
        public event EventHandler ItemActivate;

        [Browsable(true)]
        [Category("Property Changed")]
        [Description("Raised before the selection changes")]
        public event EventHandler<ResourceChangingEventArgs> SelectedResourceChanging;

        [Browsable(true)]
        [Category("Property Changed")]
        [Description("Raised when the selection changes")]
        public event EventHandler<ResourceChangedEventArgs> SelectedResourceChanged;

        [Browsable(true)]
        [Category("Action")]
        [Description("Raise when the Delete key is pressed")]
        public event EventHandler DeletePressed;
        #endregion

        #region Sub-classes
        /// <summary>
        /// Passes the name of the currently select resource and allows handlers to cancel the change
        /// </summary>
        public class ResourceChangingEventArgs : EventArgs
        {
            public readonly string name;
            public bool Cancel = false;
            public ResourceChangingEventArgs(ListViewItem lvi) { name = lvi == null ? "" : lvi.Text; }
        }

        /// <summary>
        /// Passes the name of the newly selected resource
        /// </summary>
        public class ResourceChangedEventArgs : EventArgs
        {
            public readonly string name;
            public ResourceChangedEventArgs(ListViewItem lvi) { name = lvi == null ? "" : lvi.Text; }
        }
        #endregion


        void SetColumns()
        {
            try
            {
                listView1.BeginUpdate();
                listView1.Columns.Clear();
                if (fields == null) return;

                ColumnHeader[] ach = new ColumnHeader[fields.Count + 2];

                ach[0] = new ColumnHeader();
                ach[0].DisplayIndex = 0;
                ach[0].Text = ach[0].Name = "Name";
                ach[0].Width = displayResourceNames ? (columnWidths.Length > 0 && columnWidths[0] >= 0 ? columnWidths[0] : 80) : 0;
                listView1.Columns.Add(ach[0]);

                ach[1] = new ColumnHeader();
                ach[1].DisplayIndex = 0;
                ach[1].Text = ach[0].Name = "Tag";
                ach[1].Width = displayResourceTags ? (columnWidths.Length > 1 && columnWidths[1] >= 0 ? columnWidths[1] : 50) : 0;
                listView1.Columns.Add(ach[1]);

                for (int i = 2; i < ach.Length; i++)
                {
                    ach[i] = new ColumnHeader();
                    ach[i].DisplayIndex = i;
                    ach[i].Text = ach[i].Name = fields[i - 2].Replace("Resource", "");
                    ach[i].Width = columnWidths.Length > i && columnWidths[i] >= 0 ? columnWidths[i] : (ach[i].Text.Equals("Instance") ? 140 : 80);
                    listView1.Columns.Add(ach[i]);
                }
            }
            finally
            {
                listView1.EndUpdate();
            }
        }

        void UpdateList()
        {
            if (((MainForm)ParentForm).IsClosing) return;

            var cmp = listView1.ListViewItemSorter;
            IResourceIndexEntry sie = SelectedResource;

            bool vis = listView1.Visible;
            bool uwc = Application.UseWaitCursor;
            DateTime tick = DateTime.UtcNow.AddMilliseconds(tock);
            try
            {
                Application.UseWaitCursor = true;
                listView1.BeginUpdate();
                listView1.Visible = false;

                listView1.ListViewItemSorter = null;

                pbLabel.Text = "";
                SelectedResource = null;

                pbLabel.Text = "Clear resource list...";
                Application.DoEvents();
                using (Splash splash = new Splash("Clear resource list..."))
                {
                    splash.Show();
                    Application.DoEvents();
                    listView1.Items.Clear();// this can be slow and steals the main thread!
                }
                #region Old clear listView1...
                /*if (!pbLabel.Visible || listView1.Items.Count > 10000)
                {
                }
                else
                {
                    // this is slower but gives some feedback
                    pb.Maximum = listView1.Items.Count;
                    pb.Value = listView1.Items.Count;
                    try
                    {
                        int i = listView1.Items.Count;
                        while (i > 0)
                        {
                            i--;
                            try
                            {
                                listView1.Items.RemoveAt(i);
                            }
                            finally
                            {
                                if (tick < DateTime.UtcNow)
                                {
                                    tick = DateTime.UtcNow.AddMilliseconds(tock);
                                    if (pb != null) { pb.Value = i; Application.DoEvents(); }
                                }
                            }
                        }
                    }
                    finally { if (pb != null) pb.Value = 0; }
                }/**/
                #endregion

                lookup = new Dictionary<IResourceIndexEntry, ListViewItem>();
                if (resourceList == null) return;

                AddRange(resourceList);
            }
            finally
            {
                pbLabel.Text = "Restoring sorter...";
                Application.DoEvents();
                listView1.ListViewItemSorter = cmp;

                pbLabel.Text = "";
                Application.DoEvents();
                if (sie != null && lookup.ContainsKey(sie))
                    SelectedResource = sie;

                listView1.Visible = !this.Visible || !this.Created || vis;
                listView1.EndUpdate();
                Application.UseWaitCursor = uwc;
                Application.DoEvents();

                OnListUpdated(this, new EventArgs());
                if (this.Visible)
                    ForceFocus.Focus(Application.OpenForms[0]);
            }
        }

        ListViewItem CreateItem(IResourceIndexEntry ie)
        {
            ListViewItem lvi = new ListViewItem();
            if (ie.IsDeleted) lvi.Font = new Font(lvi.Font, lvi.Font.Style | FontStyle.Strikeout);
            for (int j = 0; j < fields.Count + 1; j++)
            {
                if (j == 0)
                    lvi.Text = ResourceName(ie);
                else if (j == 1)
                    lvi.SubItems.Add(ResourceTag(ie));
                else
                    lvi.SubItems.Add(ie[fields[j - 2]] + "");
            }
            lvi.Tag = ie;
            (ie as AResourceIndexEntry).ResourceIndexEntryChanged -= new EventHandler(BrowserWidget_ResourceIndexEntryChanged);
            (ie as AResourceIndexEntry).ResourceIndexEntryChanged += new EventHandler(BrowserWidget_ResourceIndexEntryChanged);
            return lvi;
        }

        void ClearNameMap()
        {
            if (nameMap == null) return;
            foreach (IResource res in nameMap)
                res.ResourceChanged -= new EventHandler(nameMap_ResourceChanged);
            nameMap.Clear();
            nameMap = null;

            if (nameMapRIEs == null) return;
            nameMapRIEs.Clear();
            nameMapRIEs = null;
        }

        void CreateNameMap()
        {
            if (pkg == null) return;
            if (nameMap != null) return;
            string oldLabel = pbLabel.Text;
            pbLabel.Text = "Loading name map resources...";
            Application.DoEvents();

            nameMap = new List<IResource>();
            nameMapRIEs = new List<IResourceIndexEntry>();

            IList<IResourceIndexEntry> lrie = pkg.FindAll(_key => _key.ResourceType == 0x0166038C);
            foreach (IResourceIndexEntry rie in lrie)
            {
                try
                {
                    IResource res = s4pi.WrapperDealer.WrapperDealer.GetResource(0, pkg, rie);
                    if (res == null) continue;
                    if (!typeof(IDictionary<ulong, string>).IsAssignableFrom(res.GetType())) continue;
                    nameMap.Add(res);
                    nameMapRIEs.Add(rie);
                    res.ResourceChanged += new EventHandler(nameMap_ResourceChanged);
                }
                catch (Exception ex)
                {
                    MainForm.IssueException(ex,
                        String.Format("Some resource names may not be displayed.\n\nError reading _KEY {0:X8}:{1:X8}:{2:X16}",
                        rie.ResourceType, rie.ResourceGroup, rie.Instance));
                }
            }

            pbLabel.Text = oldLabel;
            Application.DoEvents();
        }

        private void nameMap_ResourceChanged(object sender, EventArgs e)
        {
            if (!displayResourceNames) return;
            if (lookup == null) return;

            if (nameMap == null) CreateNameMap();
            if (nameMap == null) return;

            string oldLabel = pbLabel.Text;
            int oldValue = -1;
            int oldMax = -1;

            bool vis = listView1.Visible;
            try
            {
                pbLabel.Text = "Updating resource names...";
                Application.DoEvents();

                listView1.BeginUpdate();
                listView1.Visible = false;
                if (pb != null)
                {
                    oldValue = pb.Value;
                    pb.Value = 0;
                    oldMax = pb.Maximum;
                    pb.Maximum = lookup.Count;
                }

                int i = 0;
                bool hasNames = false;
                DateTime tick = DateTime.UtcNow.AddMilliseconds(tock);
                foreach (KeyValuePair<IResourceIndexEntry, ListViewItem> kvp in lookup)
                {
                    try
                    {
                        kvp.Value.Text = ResourceName(kvp.Key);
                        hasNames = hasNames || kvp.Value.Text.Length > 0;
                        i++;
                    }
                    finally
                    {
                        if (tick < DateTime.UtcNow)
                        {
                            tick = DateTime.UtcNow.AddMilliseconds(tock);
                            if (pb != null) { pb.Value = i; Application.DoEvents(); }
                        }
                    }
                }
                setResourceNameWidth();
            }
            finally
            {
                listView1.Visible = !this.Visible || !this.Created || vis;
                listView1.EndUpdate();

                pbLabel.Text = oldLabel;
                if (pb != null)
                {
                    pb.Value = 0;
                    pb.Maximum = oldMax;
                    pb.Value = oldValue;
                }
            }

            Application.DoEvents();
        }

        void setResourceNameWidth()
        {
            if (listView1.Columns.Count == 0) return;
            if (!displayResourceNames) { listView1.Columns[0].Width = 0; return; }
            if (listView1.Columns[0].Width != 0) return;
            if (columnWidths.Length > 0 && columnWidths[0] >= 0) { listView1.Columns[0].Width = columnWidths[0]; return; }
            listView1.Columns[0].Width = 80;
        }

        public string ResourceTag(IResourceIndexEntry rie)
        {
            string key = rie["ResourceType"];
            if (s4pi.Extensions.ExtList.Ext.ContainsKey(key)) return s4pi.Extensions.ExtList.Ext[key][0];
            if (s4pi.Extensions.ExtList.Ext.ContainsKey("*")) return s4pi.Extensions.ExtList.Ext["*"][0];
            return "";
        }



        protected virtual void OnListUpdated(object sender, EventArgs e) { if (ListUpdated != null) ListUpdated(sender, e); }

        protected virtual void OnItemActivate(object sender, EventArgs e) { if (ItemActivate != null) ItemActivate(sender, e); }

        protected virtual void OnDeletePressed(object sender, EventArgs e) { if (DeletePressed != null) DeletePressed(sender, e); }

        protected virtual void OnSelectedResourceChanging(object sender, ResourceChangingEventArgs e) { if (SelectedResourceChanging != null) SelectedResourceChanging(sender, e); }

        protected virtual void OnSelectedResourceChanged(object sender, ResourceChangedEventArgs e) { if (SelectedResourceChanged != null) SelectedResourceChanged(sender, e); }


        private void listView1_ItemActivate(object sender, EventArgs e) { OnItemActivate(sender, e); }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (internalchg) return;

            if (selectedResource != null)
            {
                ResourceChangingEventArgs rcea = new ResourceChangingEventArgs(selectedResource);
                OnSelectedResourceChanging(this, rcea);
                if (rcea.Cancel)
                {
                    internalchg = true;
                    listView1.SelectedItems.Clear();
                    listView1.SelectedIndices.Add(listView1.Items.IndexOf(selectedResource));
                    listView1.SelectedItems[0].EnsureVisible();
                    internalchg = false;
                    return;
                }
            }

            selectedResource = (listView1.SelectedItems.Count == 1) ? listView1.SelectedItems[0] : null;

            OnSelectedResourceChanged(this, new ResourceChangedEventArgs(selectedResource));
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }

            bool uwc = Application.UseWaitCursor;
            try
            {
                // Perform the sort with these new sort options.
                listView1.BeginUpdate();
                Application.UseWaitCursor = true;
                pbLabel.Text = "Sorting display...";
                Application.DoEvents();
                this.listView1.Sort();
            }
            finally
            {
                pbLabel.Text = "";
                Application.UseWaitCursor = uwc;
                Application.DoEvents();
                listView1.EndUpdate();
            }
            if (listView1.SelectedIndices.Count > 0)
                listView1.SelectedItems[0].EnsureVisible();
        }

        private void BrowserWidget_ResourceIndexEntryChanged(object sender, EventArgs e)
        {
            IResourceIndexEntry rie = sender as IResourceIndexEntry;
            if (rie == null) return;

            if (lookup == null) return;
            if (!lookup.ContainsKey(rie)) { (rie as AResourceIndexEntry).ResourceIndexEntryChanged -= new EventHandler(BrowserWidget_ResourceIndexEntryChanged); return; }

            ListViewItem lvi = lookup[rie];
            ListViewItem newlvi = CreateItem(rie);

            listView1.SuspendLayout();
            lvi.SubItems.Clear(); for (int i = 1; i < newlvi.SubItems.Count; i++) lvi.SubItems.Add(newlvi.SubItems[i]);
            lvi.Font = newlvi.Font;
            lvi.Text = newlvi.Text;
            listView1.ResumeLayout();
        }

        private void pkg_ResourceIndexInvalidated(object sender, EventArgs e)
        {
            bool vis = listView1.Visible;
            try
            {
                listView1.Visible = false;

                lookup = new Dictionary<IResourceIndexEntry, ListViewItem>();//makes nameMap_ResourceChanged go fast; gets fixed by UpdateList
                nameMap = null;
                nameMap_ResourceChanged(null, null); //CreateNameMap();

                resourceList = pkg == null ? null : filter == null ? pkg.GetResourceList : FilteredList();
                UpdateList();
            }
            finally { listView1.Visible = !this.Visible || !this.Created || vis; }
        }

        IList<IResourceIndexEntry> FilteredList()
        {
            string oldLabel = pbLabel.Text;
            int oldValue = pb.Value;
            int oldMaximum = pb.Maximum;
            try
            {
                DateTime tick = DateTime.UtcNow.AddMilliseconds(tock);
                pbLabel.Text = "Finding resources...";
                pb.Value = 0;
                pb.Maximum = pkg.GetResourceList.Count;

                int i = 0;
                Application.DoEvents();
                return pkg.FindAll(value =>
                {
                    try
                    {
                        ++i;

                        foreach (var kvp in filter)
                            if (!kvp.Value.IsMatch(kvp.Key.Equals("Name") ? ResourceName(value) : kvp.Key.Equals("Tag") ? ResourceTag(value) : value[kvp.Key].ToString("X"))) return false;
                        return true;

                    }
                    finally
                    {
                        if (tick < DateTime.UtcNow)
                        {
                            tick = DateTime.UtcNow.AddMilliseconds(tock);
                            if (pb != null) { pb.Value = i; Application.DoEvents(); }
                        }
                    }
                });
            }
            finally
            {
                pbLabel.Text = oldLabel;
                pb.Value = oldValue;
                pb.Maximum = oldMaximum;
            }
        }
    }
}
