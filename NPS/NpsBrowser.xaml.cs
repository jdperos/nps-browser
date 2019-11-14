using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Newtonsoft.Json;
using NPS.Helpers;
using ReactiveUI;

namespace NPS
{
    public partial class NpsBrowser : Window
    {
        public const string version = "0.94"; //Dyrqrap
        private List<Item> currentDatabase = new List<Item>();

        private List<Item> databaseAll = new List<Item>();

        private List<Item> avatarsDbs = new List<Item>();

        //List<Item> dlcsDbs = new List<Item>();
        //List<Item> themesDbs = new List<Item>();
        private List<Item> updatesDbs = new List<Item>();

        private HashSet<string> types = new HashSet<string>();
        private HashSet<string> regions = new HashSet<string>();
        private int currentOrderColumn = 0;
        private bool currentOrderInverted = false;

        private List<DownloadWorker> downloads = new List<DownloadWorker>();
        private Release[] releases = null;

        public NpsBrowser()
        {
            InitializeComponent();

            Title += " " + version;
        }

        public async void Start()
        {
            NewVersionCheck();

            if (string.IsNullOrEmpty(Settings.Instance.PsvUri) && string.IsNullOrEmpty(Settings.Instance.PsvDlcUri))
            {
                MessageBox.Show(
                    "Application did not provide any links to external files or decrypt mechanism.\r\nYou need to specify tsv (tab splitted text) file with your personal links to pkg files on your own.\r\n\r\nFormat: TitleId Region Name Pkg Key",
                    "Disclaimer!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                var o = new Options(this);
                await o.ShowDialog(this);
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            foreach (var hi in Settings.Instance.HistoryInstance.currentlyDownloading)
            {
                hi.Recreate();
                // TODO:
                /*
                _downloadStatusList
                lstDownloadStatus.Items.Add(hi.lvi);
                lstDownloadStatus.AddEmbeddedControl(hi.progress, 3, lstDownloadStatus.Items.Count - 1);*/
                downloads.Add(hi);
            }

            ServicePointManager.DefaultConnectionLimit = 30;
            LoadAllDatabases(null, null);
        }

        private void LoadAllDatabases(object sender, EventArgs e)
        {
            avatarsDbs.Clear();
            //dlcsDbs.Clear();
            databaseAll.Clear();
            //themesDbs.Clear();
            updatesDbs.Clear();

            if (NPCache.I.IsCacheIsInvalid)
            {
                databaseAll = NPCache.I.localDatabase;
                types = new HashSet<string>(NPCache.I.types);
                regions = new HashSet<string>(NPCache.I.regions);

                FinalizeDBLoad();
            }
            else
            {
                Sync(null);
            }
        }

        public void Sync(object sender, EventArgs e)
        {
            Sync(null);
        }

        public void Sync(Action result)
        {
/*
            SyncDB sync = new SyncDB();
            sync.Owner = this;
            sync.Show();

            sync.Sync((g) =>
            {
                databaseAll = g;
                Invoke(new Action(() =>
                {
                    FinalizeDBLoad();

                    NPCache.I.localDatabase = databaseAll;
                    NPCache.I.types = types.ToList();
                    NPCache.I.regions = regions.ToList();
                    NPCache.I.Save(DateTime.Now);
                    if (result != null) result.Invoke();
                    sync.Close();
                }));
            });
*/
        }

        private List<Item> GetDatabase(string type = "GAME")
        {
            if (type == "DLC")
                return databaseAll.Where((i) => (i.IsDLC == true && i.IsTheme == false)).ToList();
            if (type == "THEME")
                return databaseAll.Where((i) => (i.IsTheme == true && i.IsDLC == false)).ToList();
            if (type == "GAME")
                return databaseAll.Where((i) => (i.IsDLC == false && i.IsTheme == false)).ToList();

            return new List<Item>();
        }

        private void FinalizeDBLoad()
        {
/*
            //var tempList = new List<Item>(dlcsDbs);
            //tempList.AddRange(gamesDbs);
            rbnDLC.Enabled = false;
            rbnGames.Enabled = false;

            foreach (var itm in databaseAll)
            {
                regions.Add(itm.Region);
                types.Add(itm.contentType);

                if (itm.IsDLC) rbnDLC.Enabled = true;
                else rbnGames.Enabled = true;
                if (itm.IsTheme) rbnThemes.Enabled = true;
            }


            if (avatarsDbs.Count > 0)
                rbnAvatars.Enabled = true;
            else rbnAvatars.Enabled = false;

            if (updatesDbs.Count > 0)
                rbnUpdates.Enabled = true;
            else rbnUpdates.Enabled = false;

            rbnGames.Checked = true;

            currentDatabase = GetDatabase();

            cmbType.Items.Clear();
            cmbRegion.Items.Clear();


            foreach (string s in types)
                cmbType.Items.Add(s);

            foreach (string s in regions)
                cmbRegion.Items.Add(s);


            int countSelected = Settings.Instance.SelectedRegions.Count;
            foreach (var a in cmbRegion.CheckBoxItems)
            {
                if (countSelected > 0)
                {
                    if (Settings.Instance.SelectedRegions.Contains(a.Text)) a.Checked = true;
                }
                else
                    a.Checked = true;
            }

            countSelected = Settings.Instance.SelectedTypes.Count;

            foreach (var a in cmbType.CheckBoxItems)
            {
                if (countSelected > 0)
                {
                    if (Settings.Instance.SelectedTypes.Contains(a.Text)) a.Checked = true;
                }
                else
                    a.Checked = true;
            }

            var dlcsDbs = GetDatabase("DLC").ToArray();

            foreach (var itm in databaseAll)
            {
                if (!itm.IsAvatar && !itm.IsDLC && !itm.IsTheme && !itm.IsUpdate && !itm.ItsPsx)
                    //if (dbType == DatabaseType.Vita || dbType == DatabaseType.PSP || dbType == DatabaseType.PS3 || dbType == DatabaseType.PS4)
                    itm.CalculateDlCs(dlcsDbs);
            }
            // Populate DLC Parent Titles
            //var gamesDb = GetDatabase();
            //var dlcDb = GetDatabase(true);
            //foreach (var item in dlcDb)
            //{
            //    var result = gamesDb.FirstOrDefault(i => i.TitleId.StartsWith(item.TitleId.Substring(0, 9)))?.TitleName;
            //    item.ParentGameTitle = result ?? string.Empty;
            //}

            cmbRegion.CheckBoxCheckedChanged += txtSearch_TextChanged;
            cmbType.CheckBoxCheckedChanged += txtSearch_TextChanged;
            txtSearch_TextChanged(null, null);
*/
        }

        private void SetCheckboxState(List<Item> list, int id)
        {
/*
            if (list.Count == 0)
            {
                cmbType.CheckBoxItems[id].Enabled = false;
                cmbType.CheckBoxItems[id].Checked = false;
            }
            else
            {
                cmbType.CheckBoxItems[id].Enabled = true;
                cmbType.CheckBoxItems[id].Checked = true;
            }
*/
        }

/*        private void CmbRegion_CheckBoxCheckedChanged(object sender, EventArgs e)
        {
            txtSearch_TextChanged(null, null);
        }*/

        private async void NewVersionCheck()
        {
            if (version.Contains("beta"))
            {
                return;
            }

            try
            {
                using var client = new HttpClient(new SocketsHttpHandler
                {
                    Proxy = Settings.Instance.Proxy,
                    Credentials = CredentialCache.DefaultCredentials
                });
                client.DefaultRequestHeaders.Add("user-agent", "MyPersonalApp");
                var content = await client.GetStringAsync("https://nopaystation.com/vita/npsReleases/version.json");

                releases = JsonConvert.DeserializeObject<Release[]>(content);

                var newVer = releases[0].version;
                if (version != newVer)
                {
                    DownloadUpdateMenuItem.IsVisible = true;
                    Title += $"         (!! new version {newVer} available !!)";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while checking for new version:\n{0}", e);
            }
        }


        private void RefreshList(List<Item> items)
        {
/*
            List<ListViewItem> list = new List<ListViewItem>();

            foreach (var item in items)
            {
                var a = new ListViewItem(item.TitleId);
                if (Settings.Instance.HistoryInstance.completedDownloading.Contains(item))
                {
                    int newdlc = 0;
                    foreach (var i in item.DlcItm)
                        if (!Settings.Instance.HistoryInstance.completedDownloading.Contains(i))
                            newdlc++;

                    if (newdlc > 0) a.BackColor = ColorTranslator.FromHtml("#E700E7");
                    else a.BackColor = ColorTranslator.FromHtml("#B7FF7C");
                }

                a.SubItems.Add(item.Region);
                a.SubItems.Add(item.TitleName);
                a.SubItems.Add(item.contentType);
                if (item.DLCs > 0)
                    a.SubItems.Add(item.DLCs.ToString());
                else a.SubItems.Add("");
                if (item.lastModifyDate != DateTime.MinValue)
                    a.SubItems.Add(item.lastModifyDate.ToString());
                else a.SubItems.Add("");
                a.Tag = item;
                list.Add(a);
            }

            lstTitles.BeginUpdate();
            if (rbnDLC.Checked) lstTitles.Columns[4].Width = 0;
            else lstTitles.Columns[4].Width = 60;
            lstTitles.Items.Clear();
            lstTitles.Items.AddRange(list.ToArray());

            lstTitles.ListViewItemSorter = new ListViewItemComparer(2, false);
            lstTitles.Sort();

            lstTitles.EndUpdate();

            string type = "";
            if (rbnGames.Checked) type = "Games";
            else if (rbnAvatars.Checked) type = "Avatars";
            else if (rbnDLC.Checked) type = "DLCs";
            else if (rbnThemes.Checked) type = "Themes";
            else if (rbnUpdates.Checked) type = "Updates";
            //else if (rbnPSM.Checked) type = "PSM Games";
            //else if (rbnPSX.Checked) type = "PSX Games";

            lblCount.Text = $"{list.Count}/{currentDatabase.Count} {type}";
*/
        }

/*
        // Form
        private void NPSBrowser_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Instance.HistoryInstance.currentlyDownloading.Clear();

            foreach (var lstItm in lstDownloadStatus.Items)
            {
                DownloadWorker dw = ((lstItm as ListViewItem).Tag as DownloadWorker);

                Settings.Instance.HistoryInstance.currentlyDownloading.Add(dw);
            }

            Settings.Instance.SelectedRegions.Clear();
            foreach (var a in cmbRegion.CheckBoxItems)
                if (a.Checked)
                    Settings.Instance.SelectedRegions.Add(a.Text);

            Settings.Instance.SelectedTypes.Clear();

            foreach (var a in cmbType.CheckBoxItems)
                if (a.Checked)
                    Settings.Instance.SelectedTypes.Add(a.Text)
                        ;

            Settings.Instance.Save();
            NPCache.I.Save();
        }
*/

        // Menu
        private void optionsToolStripMenuItem_Click()
        {
            var o = new Options(this);
            o.ShowDialog(this);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void downloadUpdateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = releases?[0]?.url;
            if (!string.IsNullOrEmpty(url))
                System.Diagnostics.Process.Start(url);
        }

        public void updateSearch()
        {
/*
            List<Item> itms = new List<Item>();
            String[] splitStr = txtSearch.Text.Split(' ');

            foreach (var item in currentDatabase)
            {
                bool dirty = false;


                foreach (String i in splitStr)
                {
                    if (i.Length == 0) continue;
                    if (i.StartsWith("-") == true)
                    {
                        if ((item.TitleName.ToLower().Contains(i.Substring(1).ToLower()) == true) ||
                            (item.ContentId.ToLower().Contains(i.Substring(1).ToLower()) == true))
                        {
                            dirty = true;
                            break;
                        }
                    }
                    else if ((item.TitleName.ToLower().Contains(i.ToLower()) == false) &&
                             (item.TitleId.ToLower().Contains(i.ToLower()) == false))
                    {
                        dirty = true;
                        break;
                    }
                }


                if (dirty == false)
                {
                    if (rbnDLC.Checked == true)
                    {
                        if ((rbnUndownloaded.Checked == true) &&
                            (Settings.Instance.HistoryInstance.completedDownloading.Contains(item))) dirty = true;
                        if ((rbnDownloaded.Checked == true) &&
                            (!Settings.Instance.HistoryInstance.completedDownloading.Contains(item))) dirty = true;
                    }
                    else
                    {
                        if ((!Settings.Instance.HistoryInstance.completedDownloading.Contains(item)) &&
                            (rbnDownloaded.Checked == true)) dirty = true;
                        else if (Settings.Instance.HistoryInstance.completedDownloading.Contains(item))
                        {
                            if ((rbnUndownloaded.Checked == true) && (chkUnless.Checked == false)) dirty = true;

                            else if ((rbnUndownloaded.Checked == true) && (chkUnless.Checked == true))
                            {
                                int newDLC = 0;

                                foreach (var item2 in item.DlcItm)
                                {
                                    if (!Settings.Instance.HistoryInstance.completedDownloading.Contains(item2))
                                        newDLC++;
                                }

                                if (newDLC == 0) dirty = true;
                            }
                        }
                    }
                }

                if ((dirty == false) && ContainsCmbBox(cmbRegion, item.Region) &&
                    ContainsCmbBox(cmbType, item.contentType)
                ) /*(cmbRegion.Text == "ALL" || item.Region.Contains(cmbRegion.Text)))#1# itms.Add(item);
            }

            RefreshList(itms);
*/
        }


/*
        // Search
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            updateSearch();
        }

        private bool ContainsCmbBox(PresentationControls.CheckBoxComboBox chkbcmb, string item)
        {
            foreach (var itm in chkbcmb.CheckBoxItems)
            {
                if (itm.Checked && item.Contains(itm.Text))
                    return true;
            }

            return false;
        }

        // Browse
        private void rbnGames_CheckedChanged(object sender, EventArgs e)
        {
            downloadAllToolStripMenuItem.Enabled = rbnGames.Checked;

            if (rbnGames.Checked)
            {
                currentDatabase = GetDatabase();
                txtSearch_TextChanged(null, null);
            }
        }

        private void rbnAvatars_CheckedChanged(object sender, EventArgs e)
        {
            if (rbnAvatars.Checked)
            {
                currentDatabase = avatarsDbs;
                txtSearch_TextChanged(null, null);
            }
        }

        private void rbnDLC_CheckedChanged(object sender, EventArgs e)
        {
            if (rbnDLC.Checked)
            {
                currentDatabase = GetDatabase("DLC");
                txtSearch_TextChanged(null, null);
            }
        }

        private void rbnThemes_CheckedChanged(object sender, EventArgs e)
        {
            if (rbnThemes.Checked)
            {
                currentDatabase = GetDatabase("THEME");
                txtSearch_TextChanged(null, null);
            }
        }

        private void rbnUpdates_CheckedChanged(object sender, EventArgs e)
        {
            if (rbnUpdates.Checked)
            {
                currentDatabase = updatesDbs;
                txtSearch_TextChanged(null, null);
            }
        }

        //private void rbnPSM_CheckedChanged(object sender, EventArgs e)
        //{
        //    if (rbnPSM.Checked)
        //    {
        //        currentDatabase = psmDbs;
        //        txtSearch_TextChanged(null, null);
        //    }
        //}

        //private void rbnPSX_CheckedChanged(object sender, EventArgs e)
        //{
        //    if (rbnPSX.Checked)
        //    {
        //        currentDatabase = psxDbs;
        //        txtSearch_TextChanged(null, null);
        //    }
        //}

        // Download
        private void btnDownload_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Settings.Instance.DownloadDir) || string.IsNullOrEmpty(Settings.Instance.PkgPath))
            {
                MessageBox.Show("You don't have a proper configuration.", "Whoops!", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Options o = new Options(this);
                o.ShowDialog();
                return;
            }


            if (lstTitles.SelectedItems.Count == 0) return;
            List<Item> toDownload = new List<Item>();

            foreach (ListViewItem itm in lstTitles.SelectedItems)
            {
                var a = (itm.Tag as Item);

                if (a.pkg.EndsWith(".json"))
                {
                    WebClient p4client = new WebClient();
                    p4client.Credentials = CredentialCache.DefaultCredentials;
                    p4client.Headers.Add("user-agent", "MyPersonalApp :)");
                    string json = p4client.DownloadString(a.pkg);

                    JsonObject fields = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(json);
                    JsonArray pieces = fields["pieces"] as JsonArray;
                    foreach (JsonObject piece in pieces)
                    {
                        Item inneritm = new Item()
                        {
                            TitleId = a.TitleId,
                            Region = a.Region,
                            TitleName = a.TitleName + " (Offset " + piece["fileOffset"].ToString() + ")",
                            offset = piece["fileOffset"].ToString(),
                            pkg = piece["url"].ToString(),
                            zRif = a.zRif,
                            ContentId = a.ContentId,
                            lastModifyDate = a.lastModifyDate,

                            ItsPsp = a.ItsPsp,
                            ItsPS3 = a.ItsPS3,
                            ItsPS4 = a.ItsPS4,
                            ItsPsx = a.ItsPsx,
                            contentType = a.contentType,

                            IsAvatar = a.IsAvatar,
                            IsDLC = a.IsDLC,
                            IsTheme = a.IsTheme,
                            IsUpdate = a.IsUpdate,

                            DlcItm = a.DlcItm,
                            ParentGameTitle = a.ParentGameTitle,
                        };

                        toDownload.Add(inneritm);
                    }
                }
                else
                    toDownload.Add(a);
            }

            foreach (var a in toDownload)
            {
                bool contains = false;
                foreach (var d in downloads)
                    if (d.currentDownload == a)
                    {
                        contains = true;
                        break; //already downloading
                    }

                if (!contains)
                {
                    if (a.IsDLC)
                    {
                        var gamesDb = GetDatabase();
                        var result = gamesDb.FirstOrDefault(i => i.TitleId.StartsWith(a.TitleId.Substring(0, 9)))
                            ?.TitleName;
                        a.ParentGameTitle = result ?? string.Empty;
                    }

                    DownloadWorker dw = new DownloadWorker(a, this);
                    lstDownloadStatus.Items.Add(dw.lvi);
                    lstDownloadStatus.AddEmbeddedControl(dw.progress, 3, lstDownloadStatus.Items.Count - 1);
                    downloads.Add(dw);
                }
            }
        }

        private void lnkOpenRenaScene_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            //var u = new Uri("https://www.youtube.com/results?search_query=dead or alive");
            System.Diagnostics.Process.Start(lnkOpenRenaScene.Tag.ToString());
        }

        // lstTitles
        private void lstTitles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstTitles.SelectedItems.Count > 0)
            {
                var itm = (lstTitles.SelectedItems[0].Tag as Item);
                if (itm.ItsPS3 || itm.ItsPS4)
                {
                    if (string.IsNullOrEmpty(itm.zRif))
                    {
                        lb_ps3licenseType.BackColor = Color.LawnGreen;
                        lb_ps3licenseType.Text = "RAP NOT REQUIRED, use ReActPSN/PSNPatch";
                    }
                    else if (itm.zRif.ToLower().Contains("UNLOCK/LICENSE BY DLC".ToLower()))
                        lb_ps3licenseType.Text = "UNLOCK BY DLC";
                    else lb_ps3licenseType.Text = "";
                }
                else
                {
                    lb_ps3licenseType.Text = "";
                }
            }
        }

        private void lstTitles_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (currentOrderColumn == e.Column)
                currentOrderInverted = !currentOrderInverted;
            else
            {
                currentOrderColumn = e.Column;
                currentOrderInverted = false;
            }

            this.lstTitles.ListViewItemSorter = new ListViewItemComparer(currentOrderColumn, currentOrderInverted);
            // Call the sort method to manually sort.
            lstTitles.Sort();
        }

        private void lstTitles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Control)
            {
                //listView1.MultiSelect = true;
                foreach (ListViewItem item in lstTitles.Items)
                {
                    item.Selected = true;
                }
            }
        }

        private void downloadAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnDownload_Click(null, null);
            downloadAllDlcsToolStripMenuItem_Click(null, null);
        }

        // lstTitles Menu Strip
        private void showTitleDlcToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstTitles.SelectedItems.Count == 0) return;


            Item t = (lstTitles.SelectedItems[0].Tag as Item);
            if (t.DLCs > 0)
            {
                rbnDLC.Checked = true;
                txtSearch.Text = t.TitleId;
                rbnAll.Checked = true;
            }
        }

        private void downloadAllDlcsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem itm in lstTitles.SelectedItems)
            {
                var parrent = (itm.Tag as Item);

                foreach (var a in parrent.DlcItm)
                {
                    a.ParentGameTitle = parrent.TitleName;
                    bool contains = false;
                    foreach (var d in downloads)
                        if (d.currentDownload == a)
                        {
                            contains = true;
                            break; //already downloading
                        }

                    if (!contains)
                    {
                        DownloadWorker dw = new DownloadWorker(a, this);
                        lstDownloadStatus.Items.Add(dw.lvi);
                        lstDownloadStatus.AddEmbeddedControl(dw.progress, 3, lstDownloadStatus.Items.Count - 1);
                        downloads.Add(dw);
                    }
                }
            }
        }

        // lstDownloadStatus
        private void lstDownloadStatus_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Control)
            {
                //listView1.MultiSelect = true;
                foreach (ListViewItem item in lstDownloadStatus.Items)
                {
                    item.Selected = true;
                }
            }
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstDownloadStatus.SelectedItems.Count == 0) return;
            foreach (ListViewItem a in lstDownloadStatus.SelectedItems)
            {
                DownloadWorker itm = (a.Tag as DownloadWorker);
                itm.Pause();
            }
        }

        private void resumeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstDownloadStatus.SelectedItems.Count == 0) return;
            foreach (ListViewItem a in lstDownloadStatus.SelectedItems)
            {
                DownloadWorker itm = (a.Tag as DownloadWorker);
                itm.Resume();
            }
        }

        // lstDownloadStatus Menu Strip
        private void cancelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstDownloadStatus.SelectedItems.Count == 0) return;
            foreach (ListViewItem a in lstDownloadStatus.SelectedItems)
            {
                DownloadWorker itm = (a.Tag as DownloadWorker);
                itm.Cancel();
                //itm.DeletePkg();
            }
        }

        private void retryUnpackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstDownloadStatus.SelectedItems.Count == 0) return;
            foreach (ListViewItem a in lstDownloadStatus.SelectedItems)
            {
                DownloadWorker itm = (a.Tag as DownloadWorker);
                itm.Unpack();
            }
        }

        private void clearCompletedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<DownloadWorker> toDel = new List<DownloadWorker>();
            List<ListViewItem> toDelLVI = new List<ListViewItem>();

            foreach (var i in downloads)
            {
                if (i.status == WorkerStatus.Canceled || i.status == WorkerStatus.Completed)
                    toDel.Add(i);
            }

            foreach (ListViewItem i in lstDownloadStatus.Items)
            {
                if (toDel.Contains(i.Tag as DownloadWorker))
                    toDelLVI.Add(i);
            }

            foreach (var i in toDel)
                downloads.Remove(i);
            toDel.Clear();

            foreach (var i in toDelLVI)
                lstDownloadStatus.Items.Remove(i);
            toDelLVI.Clear();
        }

        // Timers
        private void timer1_Tick(object sender, EventArgs e)
        {
            int workingThreads = 0;
            int workingCompPack = 0;

            foreach (var dw in downloads)
            {
                if (dw.status == WorkerStatus.Running)
                {
                    workingThreads++;
                    if (dw.currentDownload.ItsCompPack)
                        workingCompPack++;
                }
            }

            if (workingThreads < Settings.Instance.SimultaneousDl)
            {
                foreach (var dw in downloads)
                {
                    if (dw.status == WorkerStatus.Queued)
                    {
                        if (dw.currentDownload.ItsCompPack && workingCompPack > 0)
                            break;
                        else
                        {
                            dw.Start();
                            break;
                        }
                    }
                }
            }
        }

        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private Item previousSelectedItem = null;

        private void timer2_Tick(object sender, EventArgs e)
        {
            // Update view

            if (lstTitles.SelectedItems.Count == 0) return;
            Item itm = (lstTitles.SelectedItems[0].Tag as Item);

            if (itm != previousSelectedItem)
            {
                previousSelectedItem = itm;

                tokenSource.Cancel();
                tokenSource = new CancellationTokenSource();

                Helpers.Renascene myRena = null;

                foreach (var ren in NPCache.I.renasceneCache)
                {
                    if (itm.Equals(ren.itm)) myRena = ren;
                }

                Task.Run(() =>
                {
                    if (myRena == null) myRena = new Helpers.Renascene(itm);

                    if (myRena.imgUrl != null)
                    {
                        if (!NPCache.I.renasceneCache.Contains(myRena))
                        {
                            NPCache.I.renasceneCache.Add(myRena);
                        }

                        Invoke(new Action(() =>
                        {
                            //  ptbCover.Image = myRena.image;
                            ptbCover.LoadAsync(myRena.imgUrl);
                            label5.Text = myRena.ToString();
                            lnkOpenRenaScene.Tag =
                                "https://www.google.com/search?safe=off&source=lnms&tbm=isch&sa=X&biw=785&bih=698&q=" +
                                itm.TitleName + "%20" + itm.contentType; // r.url;
                            lnkOpenRenaScene.Visible = true;
                        }));
                    }
                    else
                    {
                        Invoke(new Action(() =>
                        {
                            ptbCover.Image = null;
                            label5.Text = "";
                            lnkOpenRenaScene.Visible = false;
                        }));
                    }
                }, tokenSource.Token);
            }
        }

        private void PauseAllBtnClick(object sender, EventArgs e)
        {
            foreach (ListViewItem itm in lstDownloadStatus.Items)
            {
                (itm.Tag as DownloadWorker).Pause();
            }
        }

        private void ResumeAllBtnClick(object sender, EventArgs e)
        {
            foreach (ListViewItem itm in lstDownloadStatus.Items)
            {
                (itm.Tag as DownloadWorker).Resume();
            }
        }


        private void lstTitles_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var a = (sender as ListView);
                if (a.SelectedItems.Count > 0)
                {
                    var itm = (a.SelectedItems[0].Tag as Item);
                    if (itm.DLCs == 0)
                    {
                        showTitleDlcToolStripMenuItem.Enabled = false;
                        downloadAllDlcsToolStripMenuItem.Enabled = false;
                    }
                    else
                    {
                        showTitleDlcToolStripMenuItem.Enabled = true;
                        downloadAllDlcsToolStripMenuItem.Enabled = true;
                    }
                }
            }
        }

        private void ShowDescriptionPanel(object sender, EventArgs e)
        {
            Desc d = new Desc(lstTitles);
            d.Show();
        }

        private void cmbType_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (lstDownloadStatus.SelectedItems.Count == 0) return;
            var worker = lstDownloadStatus.SelectedItems[0];
            DownloadWorker itm = (worker.Tag as DownloadWorker);

            if (File.Exists(itm.Pkg))
            {
                System.Diagnostics.Process.Start("explorer.exe", "/select, " + itm.Pkg);
            }
        }

        private void ts_changeLog_Click(object sender, EventArgs e)
        {
            if (releases == null) return;
            foreach (var r in releases)
            {
                if (r.version == version)
                {
                    string s = "";
                    foreach (var c in r.changelog)
                        s += c + Environment.NewLine;

                    MessageBox.Show(s, "Changelog " + r.version);
                }
            }
        }

        private void changelogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (releases == null) return;
            Release r = releases[0];
            string result = "";
            foreach (var s in r.changelog)
                result += s + Environment.NewLine;

            MessageBox.Show(result, "Changelog " + r.version);
        }

        private void checkForPatchesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Settings.Instance.HmacKey))
            {
                MessageBox.Show("No hmackey");
                return;
            }

            if (lstTitles.SelectedItems.Count == 0) return;

            GamePatches gp = new GamePatches(lstTitles.SelectedItems[0].Tag as Item, (item) =>
            {
                DownloadWorker dw = new DownloadWorker(item, this);
                lstDownloadStatus.Items.Add(dw.lvi);
                lstDownloadStatus.AddEmbeddedControl(dw.progress, 3, lstDownloadStatus.Items.Count - 1);
                downloads.Add(dw);
            });

            gp.AskForUpdate();
        }

        private void toggleDownloadedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstTitles.SelectedItems.Count == 0) return;

            for (int i = 0; i < lstTitles.SelectedItems.Count; i++)
            {
                if (Settings.Instance.HistoryInstance.completedDownloading.Contains(
                    lstTitles.SelectedItems[i].Tag as Item))

                {
                    //lstTitles.SelectedItems[i].BackColor = ColorTranslator.FromHtml("#FFFFFF");
                    Settings.Instance.HistoryInstance.completedDownloading.Remove(
                        lstTitles.SelectedItems[i].Tag as Item);
                }
                else
                {
                    //lstTitles.SelectedItems[i].BackColor = ColorTranslator.FromHtml("#B7FF7C");
                    Settings.Instance.HistoryInstance.completedDownloading.Add(lstTitles.SelectedItems[i].Tag as Item);
                }
            }

            updateSearch();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void splList_Panel2_Paint(object sender, PaintEventArgs e)
        {
        }


        private void chkHideDownloaded_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void chkUnless_CheckedChanged(object sender, EventArgs e)
        {
            updateSearch();
        }

        private void lblUnless_Click(object sender, EventArgs e)
        {
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void rbnDownloaded_CheckedChanged(object sender, EventArgs e)
        {
            updateSearch();
        }

        private void rbnUndownloaded_CheckedChanged(object sender, EventArgs e)
        {
            chkUnless.Enabled = rbnUndownloaded.Checked;
            updateSearch();
        }

        private void rbnAll_CheckedChanged(object sender, EventArgs e)
        {
            updateSearch();
        }


        private void libraryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Library l = new Library(databaseAll);
            l.Show();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Settings.Instance.CompPackUrl))
            {
                MessageBox.Show("No CompPack url");
                return;
            }

            if (lstTitles.SelectedItems.Count == 0) return;

            CompPack cp = new CompPack(this, lstTitles.SelectedItems[0].Tag as Item, (item) =>
            {
                foreach (var itm in item)
                {
                    DownloadWorker dw = new DownloadWorker(itm, this);
                    lstDownloadStatus.Items.Add(dw.lvi);
                    lstDownloadStatus.AddEmbeddedControl(dw.progress, 3, lstDownloadStatus.Items.Count - 1);
                    downloads.Add(dw);
                }
            });
            cp.ShowDialog();
        }
*/

        // ReSharper disable InconsistentNaming
        private MenuItem DownloadUpdateMenuItem;
        private DataGrid DownloadStatusList;
        private MenuItem OptionsMenuItem;
        // ReSharper restore InconsistentNaming

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            DownloadUpdateMenuItem = this.Find<MenuItem>("DownloadUpdateMenuItem");
            DownloadStatusList = this.Find<DataGrid>("DownloadStatusList");
            OptionsMenuItem = this.Find<MenuItem>("OptionsMenuItem");
            OptionsMenuItem.Command = ReactiveCommand.Create(optionsToolStripMenuItem_Click);
        }
    }


    internal class Release
    {
        public string version = "";
        public string url = "";
        public string[] changelog;
    }


    internal enum DatabaseType
    {
        // PSV
        Vita,
        VitaDLC,
        VitaTheme,
        VitaUpdate,

        // PSP
        PSP,
        PSPDLC,
        PSPTheme,

        // PS3
        PS3,
        PS3Avatar,
        PS3DLC,
        PS3Theme,

        // PS4
        PS4,
        PS4DLC,
        PS4Theme,
        PS4Update,

        // Others
        ItsPsm,
        ItsPSX,
    }
}