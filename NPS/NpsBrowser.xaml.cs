using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPS.Helpers;
using NPS.Helpers.CustomComponents;
using ReactiveUI;

namespace NPS
{
    public class NpsBrowser : Window
    {
        private const int ColumnDlcs = 5;
        private const int ColumnLastModified = 6;

        public static NpsBrowser MainWindow { get; private set; }

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

        private ObservableCollection<DownloadWorkerItem> _downloadWorkerItems
            = new ObservableCollection<DownloadWorkerItem>();

        private readonly DispatcherTimer _timer1 = new DispatcherTimer();

        public NpsBrowser()
        {
            InitializeComponent();

            Title += " " + version;
            lstDownloadStatus.Items = _downloadWorkerItems;

            _timer1.Interval = TimeSpan.FromSeconds(1);
            _timer1.Tick += timer1_Tick;

            // This is necessary to make DataGrid play nicer with context menus.
            // So that right clicking on the data grid still selects the relevant item.
            lstTitles.AddHandler(
                InputElement.PointerPressedEvent,
                (s, e) =>
                {
#pragma warning disable 618
                    if (e.MouseButton == MouseButton.Right)
#pragma warning restore 618
                    {
                        var row = ((IControl) e.Source).GetSelfAndVisualAncestors()
                            .OfType<DataGridRow>()
                            .FirstOrDefault();

                        // Don't change selection if already in the selection.
                        if (row != null && !lstTitles.SelectedItems.Contains(row.DataContext))
                        {
                            lstTitles.SelectedIndex = row.GetIndex();
                        }
                    }

                    UpdateTitlesContextMenu();
                },
                handledEventsToo: true);

            lstTitles.Columns[ColumnDlcs].SortMemberPath = nameof(TitleEntry.DlcCount);
            lstTitles.Columns[ColumnLastModified].SortMemberPath = nameof(TitleEntry.LastModifiedTime);
        }

        public async void Start()
        {
            NewVersionCheck();

            _timer1.Start();

            if (IsDownloadConfigIncorrect())
            {
                await MessageBox.ShowAsync(this,
                    "You need to specify a download location and PKG decryption tool in the options.",
                    "Disclaimer!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                var o = new Options(this);
                await o.ShowDialog(this);
            }
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            MainWindow = this;

            foreach (var hi in Settings.Instance.HistoryInstance.currentlyDownloading)
            {
                hi.Recreate();

                _downloadWorkerItems.Add(hi.lvi);
                downloads.Add(hi);
            }

            ServicePointManager.DefaultConnectionLimit = 30;
            LoadAllDatabases(null, null);
        }

        private async void LoadAllDatabases(object sender, EventArgs e)
        {
            avatarsDbs.Clear();
            //dlcsDbs.Clear();
            databaseAll.Clear();
            //themesDbs.Clear();
            updatesDbs.Clear();

            if (NPCache.I.IsCacheIsInvalid)
            {
                databaseAll = NPCache.I.localDatabase;

                FinalizeDBLoad();
            }
            else
            {
                await Sync();
            }
        }

        public async Task Sync()
        {
            var sync = new SyncDB {Owner = this};
            sync.Show();

            var g = await sync.Sync();

            databaseAll = g;

            FinalizeDBLoad();

            NPCache.I.localDatabase = databaseAll;
            NPCache.I.Save(DateTime.Now);
            sync.Close();
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
            //var tempList = new List<Item>(dlcsDbs);
            //tempList.AddRange(gamesDbs);

            var foundDlc = false;
            var foundGame = false;
            var foundTheme = false;

            foreach (var itm in databaseAll)
            {
                regions.Add(itm.Region);
                types.Add(itm.contentType);

                if (itm.IsDLC) foundDlc = true;
                else foundGame = true;
                if (itm.IsTheme) foundTheme = true;
            }

            rbnDLC.IsEnabled = foundDlc;
            rbnGames.IsEnabled = foundGame;
            rbnThemes.IsEnabled = foundTheme;

            rbnAvatars.IsEnabled = avatarsDbs.Count > 0;
            rbnUpdates.IsEnabled = updatesDbs.Count > 0;

            rbnGames.IsChecked = true;

            currentDatabase = GetDatabase();

            cmbType.Items.Clear();
            cmbRegion.Items.Clear();

            foreach (var s in types)
                cmbType.Items.Add(s);

            foreach (var s in regions)
                cmbRegion.Items.Add(s);

            int countSelected = Settings.Instance.SelectedRegions.Count;
            foreach (var a in cmbRegion.CheckBoxItems)
            {
                if (countSelected > 0)
                {
                    if (Settings.Instance.SelectedRegions.Contains(a.Content as string))
                    {
                        a.IsChecked = true;
                    }
                }
                else
                {
                    a.IsChecked = true;
                }
            }

            countSelected = Settings.Instance.SelectedTypes.Count;

            foreach (var a in cmbType.CheckBoxItems)
            {
                if (countSelected > 0)
                {
                    if (Settings.Instance.SelectedTypes.Contains(a.Content as string)) a.IsChecked = true;
                }
                else
                {
                    a.IsChecked = true;
                }
            }

            var dlcsDbs = GetDatabase("DLC");

            var dlcDict = new Dictionary<(string region, string titleId), List<Item>>();

            // Calculate DLC counts.
            foreach (var dlc in dlcsDbs)
            {
                var key = (dlc.Region, dlc.TitleId);

                if (!dlcDict.TryGetValue(key, out var list))
                {
                    list = new List<Item>();
                    dlcDict.Add(key, list);
                }

                list.Add(dlc);
            }

            foreach (var gameItem in databaseAll)
            {
                if (gameItem.IsAvatar || gameItem.IsDLC || gameItem.IsTheme || gameItem.IsUpdate || gameItem.ItsPsx)
                {
                    continue;
                }

                if (dlcDict.TryGetValue((gameItem.Region, gameItem.TitleId), out var dlcs))
                {
                    gameItem.DlcItm = dlcs;
                }
            }

            // Populate DLC Parent Titles
            //var gamesDb = GetDatabase();
            //var dlcDb = GetDatabase(true);
            //foreach (var item in dlcDb)
            //{
            //    var result = gamesDb.FirstOrDefault(i => i.TitleId.StartsWith(item.TitleId.Substring(0, 9)))?.TitleName;
            //    item.ParentGameTitle = result ?? string.Empty;
            //}

            cmbRegion.CheckBoxCheckedChanged += (sender, e) => txtSearch_TextChanged();
            cmbType.CheckBoxCheckedChanged += (sender, e) => txtSearch_TextChanged();

            txtSearch_TextChanged();
        }

        private void SetCheckboxState(List<Item> list, int id)
        {
            if (list.Count == 0)
            {
                cmbType.CheckBoxItems[id].IsEnabled = false;
                cmbType.CheckBoxItems[id].IsChecked = false;
            }
            else
            {
                cmbType.CheckBoxItems[id].IsEnabled = true;
                cmbType.CheckBoxItems[id].IsChecked = true;
            }
        }

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
            var list = new List<TitleEntry>();

            foreach (var item in items)
            {
                var a = new TitleEntry {TitleId = item.TitleId};
                if (Settings.Instance.HistoryInstance.completedDownloading.Contains(item))
                {
                    int newdlc = 0;
                    if (item.DlcItm != null)
                        foreach (var i in item.DlcItm)
                            if (!Settings.Instance.HistoryInstance.completedDownloading.Contains(i))
                                newdlc++;

                    if (newdlc > 0) a.BackColor = Color.Parse("#E700E7");
                    else a.BackColor = Color.Parse("#B7FF7C");
                }

                a.Region = item.Region;
                a.TitleName = item.TitleName;
                a.ContentType = item.contentType;

                a.DLCs = item.DLCs > 0 ? item.DLCs.ToString() : "";
                a.DlcCount = item.DLCs;

                if (item.lastModifyDate != DateTime.MinValue)
                {
                    a.LastModified = item.lastModifyDate.ToString(CultureInfo.CurrentCulture);
                }
                else
                {
                    a.LastModified = "";
                }

                a.LastModifiedTime = item.lastModifyDate;

                a.Item = item;

                list.Add(a);
            }

            lstTitles.Columns[ColumnDlcs].IsVisible = rbnDLC.IsChecked != true;
            lstTitles.Items = new DataGridCollectionView(list);

            var type = "";
            if (rbnGames.IsChecked == true) type = "Games";
            else if (rbnAvatars.IsChecked == true) type = "Avatars";
            else if (rbnDLC.IsChecked == true) type = "DLCs";
            else if (rbnThemes.IsChecked == true) type = "Themes";
            else if (rbnUpdates.IsChecked == true) type = "Updates";
            //else if (rbnPSM.IsChecked == true) type = "PSM Games";
            //else if (rbnPSX.IsChecked == true) type = "PSX Games";

            lblCount.Text = $"{list.Count}/{currentDatabase.Count} {type}";
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            Settings.Instance.HistoryInstance.currentlyDownloading.Clear();

            foreach (var lstItm in lstDownloadStatus.Items.Cast<DownloadWorkerItem>())
            {
                var dw = lstItm.Worker;

                Settings.Instance.HistoryInstance.currentlyDownloading.Add(dw);
            }

            Settings.Instance.SelectedRegions.Clear();
            foreach (var a in cmbRegion.CheckBoxItems)
                if (a.IsChecked == true)
                    Settings.Instance.SelectedRegions.Add(a.Content as string);

            Settings.Instance.SelectedTypes.Clear();

            foreach (var a in cmbType.CheckBoxItems)
                if (a.IsChecked == true)
                    Settings.Instance.SelectedTypes.Add(a.Content as string);

            Settings.Instance.Save();
        }

        // Menu
        private void optionsToolStripMenuItem_Click()
        {
            var o = new Options(this);
            o.ShowDialog(this);
        }

        private void exitToolStripMenuItem_Click()
        {
            this.Close();
        }

        private void downloadUpdateToolStripMenuItem_Click()
        {
            string url = releases?[0]?.url;
            if (!string.IsNullOrEmpty(url))
                Process.Start(url);
        }

        public void updateSearch()
        {
            var items = new List<Item>();
            var entries = txtSearch.Text?.Split('|') ?? Array.Empty<string>();

            foreach (var item in currentDatabase)
            {
                if(entries.Length != 0)
                {
                    foreach (var entry in entries)
                    {
                        var splitStr = entry?.Split(' ') ?? Array.Empty<string>();
                        foreach (var i in splitStr)
                        {
                            if (i.StartsWith("-"))
                            {
                                var sub = i.AsSpan()[1..];

                                if (item.TitleName.AsSpan().Contains(sub, StringComparison.OrdinalIgnoreCase) ||
                                    item.ContentId.AsSpan().Contains(sub, StringComparison.OrdinalIgnoreCase))
                                {
                                    goto notFound;
                                }
                            }
                            else if (!item.TitleName.Contains(i, StringComparison.OrdinalIgnoreCase) &&
                                    !item.TitleId.Contains(i, StringComparison.OrdinalIgnoreCase))
                            {
                                goto notFound;
                            }
                        }

                        if (rbnDLC.IsChecked == true)
                        {
                            if (rbnUndownloaded.IsChecked == true &&
                                Settings.Instance.HistoryInstance.completedDownloading.Contains(item))
                            {
                                continue;
                            }

                            if (rbnDownloaded.IsChecked == true &&
                                !Settings.Instance.HistoryInstance.completedDownloading.Contains(item))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (!Settings.Instance.HistoryInstance.completedDownloading.Contains(item) &&
                                rbnDownloaded.IsChecked == true)
                            {
                                continue;
                            }

                            if (Settings.Instance.HistoryInstance.completedDownloading.Contains(item))
                            {
                                if (rbnUndownloaded.IsChecked == true && chkUnless.IsChecked == false)
                                {
                                    continue;
                                }

                                else if (rbnUndownloaded.IsChecked == true && chkUnless.IsChecked == true)
                                {
                                    int newDLC = 0;

                                    if (item.DlcItm != null)
                                        foreach (var item2 in item.DlcItm)
                                        {
                                            if (!Settings.Instance.HistoryInstance.completedDownloading.Contains(item2))
                                                newDLC++;
                                        }

                                    if (newDLC == 0)
                                    {
                                        continue;
                                    }
                                }
                            }
                        }

                        if (ContainsCmbBox(cmbRegion, item.Region) &&
                            ContainsCmbBox(cmbType, item.contentType)
                        ) /*(cmbRegion.Text == "ALL" || item.Region.Contains(cmbRegion.Text)))*/
                        {
                            items.Add(item);
                        }

                        notFound: ;                    
                    }
                }
                else
                {
                    items.Add(item);
                }
            }

            RefreshList(items);
        }


        // Search
        private void txtSearch_TextChanged()
        {
            updateSearch();
        }

        private bool ContainsCmbBox(CheckBoxComboBox chkbcmb, string item)
        {
            foreach (var itm in chkbcmb.CheckBoxItems)
            {
                if (itm.IsChecked.Value && item.Contains((string) itm.Content))
                    return true;
            }

            return false;
        }


        // Browse
        private void rbnGames_CheckedChanged()
        {
            // downloadAllToolStripMenuItem.Enabled = rbnGames.IsChecked == true;

            if (rbnGames.IsChecked == true)
            {
                currentDatabase = GetDatabase();
                txtSearch_TextChanged();
            }
        }

        private void rbnAvatars_CheckedChanged()
        {
            if (rbnAvatars.IsChecked == true)
            {
                currentDatabase = avatarsDbs;
                txtSearch_TextChanged();
            }
        }

        private void rbnDLC_CheckedChanged()
        {
            if (rbnDLC.IsChecked == true)
            {
                currentDatabase = GetDatabase("DLC");
                txtSearch_TextChanged();
            }
        }

        private void rbnThemes_CheckedChanged()
        {
            if (rbnThemes.IsChecked == true)
            {
                currentDatabase = GetDatabase("THEME");
                txtSearch_TextChanged();
            }
        }

        private void rbnUpdates_CheckedChanged()
        {
            if (rbnUpdates.IsChecked == true)
            {
                currentDatabase = updatesDbs;
                txtSearch_TextChanged();
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
        private void btnDownload_Click()
        {
            if (IsDownloadConfigIncorrect())
            {
                MessageBox.Show("You don't have a proper configuration.", "Whoops!", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                var o = new Options(this);
                o.ShowDialog(this);
                return;
            }


            if (lstTitles.SelectedItems.Count == 0) return;
            var toDownload = new List<Item>();

            foreach (TitleEntry itm in lstTitles.SelectedItems)
            {
                var a = itm.Item;

                if (a.pkg.EndsWith(".json"))
                {
                    WebClient p4client = new WebClient();
                    p4client.Credentials = CredentialCache.DefaultCredentials;
                    p4client.Headers.Add("user-agent", "MyPersonalApp :)");
                    string json = p4client.DownloadString(a.pkg);

                    var fields = JObject.Parse(json);
                    var pieces = fields["pieces"] as JArray;
                    foreach (JObject piece in pieces)
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

                    StartDownload(a);
                }
            }
        }

        private static bool IsDownloadConfigIncorrect()
        {
            return string.IsNullOrEmpty(Settings.Instance.DownloadDir) ||
                   string.IsNullOrEmpty(Settings.Instance.PkgPath);
        }

        /*

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
                        lb_ps3licenseType.BackColor = Colors.LawnGreen;
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
        */

        private void downloadAllToolStripMenuItem_Click()
        {
            btnDownload_Click();
            downloadAllDlcsToolStripMenuItem_Click();
        }


        private void downloadAllWithPatchesToolStripMenuItem_Click()
        {
            btnDownload_Click();
            downloadAllDlcsToolStripMenuItem_Click();
            checkForPatchesAndDownload();
        }

        // lstTitles Menu Strip
        private void showTitleDlcToolStripMenuItem_Click()
        {
            if (lstTitles.SelectedItems.Count == 0) return;


            var t = ((TitleEntry) lstTitles.SelectedItems[0]).Item;
            if (t.DLCs > 0)
            {
                rbnDLC.IsChecked = true;
                txtSearch.Text = t.TitleId;
                rbnAll.IsChecked = true;
            }
        }

        private void downloadAllDlcsToolStripMenuItem_Click()
        {
            foreach (TitleEntry itm in lstTitles.SelectedItems)
            {
                var parrent = itm.Item;

                if (parrent.DlcItm == null)
                {
                    continue;
                }

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
                        StartDownload(a);
                    }
                }
            }
        }

        private void pauseToolStripMenuItem_Click()
        {
            if (lstDownloadStatus.SelectedItems.Count == 0) return;
            foreach (DownloadWorkerItem a in lstDownloadStatus.Items)
            {
                a.Worker.Pause();
            }
        }

        private void resumeToolStripMenuItem_Click()
        {
            if (lstDownloadStatus.SelectedItems.Count == 0) return;
            foreach (DownloadWorkerItem a in lstDownloadStatus.Items)
            {
                a.Worker.Resume();
            }
        }

        // lstDownloadStatus Menu Strip
        private void cancelToolStripMenuItem_Click()
        {
            if (lstDownloadStatus.SelectedItems.Count == 0) return;
            foreach (DownloadWorkerItem a in lstDownloadStatus.Items)
            {
                a.Worker.Cancel();
                //itm.DeletePkg();
            }
        }

        private void retryUnpackToolStripMenuItem_Click()
        {
            if (lstDownloadStatus.SelectedItems.Count == 0) return;
            foreach (DownloadWorkerItem a in lstDownloadStatus.Items)
            {
                a.Worker.Unpack();
            }
        }

        private void clearCompletedToolStripMenuItem_Click()
        {
            var toDel = new List<DownloadWorker>();
            var toDelLVI = new List<DownloadWorkerItem>();

            foreach (var i in downloads)
            {
                if (i.status == WorkerStatus.Canceled || i.status == WorkerStatus.Completed)
                    toDel.Add(i);
            }

            foreach (DownloadWorkerItem i in lstDownloadStatus.Items)
            {
                if (toDel.Contains(i.Worker))
                    toDelLVI.Add(i);
            }

            foreach (var i in toDel)
                downloads.Remove(i);
            toDel.Clear();

            foreach (var i in toDelLVI)
                _downloadWorkerItems.Remove(i);
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

/*
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
                }
                else
                {
                    if (!Settings.Instance.HistoryInstance.completedDownloading.Contains(item) &&
                        rbnDownloaded.IsChecked == true)
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
*/
        private void PauseAllBtnClick()
        {
            foreach (var itm in _downloadWorkerItems)
            {
                itm.Worker.Pause();
            }
        }

        private void ResumeAllBtnClick()
        {
            foreach (var itm in _downloadWorkerItems)
            {
                itm.Worker.Resume();
            }
        }

        private void UpdateTitlesContextMenu()
        {
            if (lstTitles.SelectedItems.Count <= 0)
            {
                return;
            }

            var item = ((TitleEntry) lstTitles.SelectedItems[0]).Item;

            if (item.DLCs == 0)
            {
                showTitleDlcToolStripMenuItem.IsEnabled = false;
                downloadAllDlcsToolStripMenuItem.IsEnabled = false;
            }
            else
            {
                showTitleDlcToolStripMenuItem.IsEnabled = true;
                downloadAllDlcsToolStripMenuItem.IsEnabled = true;
            }
        }

        private void ShowDescriptionPanel()
        {
            var d = new Desc(lstTitles);
            d.Show();
        }

        private void button5_Click()
        {
            if (lstDownloadStatus.SelectedItems.Count == 0)
            {
                return;
            }

            foreach (DownloadWorkerItem worker in lstDownloadStatus.SelectedItems)
            {
                var itm = worker.Worker;

                if (File.Exists(itm.Pkg))
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Process.Start("explorer.exe", "/select, " + itm.Pkg);
                    }
                    else
                    {
                        Process.Start(Path.GetDirectoryName(itm.Pkg));
                    }
                }
            }
        }

        private void ts_changeLog_Click()
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

        private void changelogToolStripMenuItem_Click()
        {
            if (releases == null) return;
            Release r = releases[0];
            string result = "";
            foreach (var s in r.changelog)
                result += s + Environment.NewLine;

            MessageBox.Show(result, "Changelog " + r.version);
        }

        private async void checkForPatchesToolStripMenuItem_Click()
        {
            if (string.IsNullOrEmpty(Settings.HmacKey))
            {
                MessageBox.Show("No hmackey");
                return;
            }

            if (lstTitles.SelectedItems.Count == 0) return;
            foreach (var entry in lstTitles.SelectedItems)
            {
                var item = ((TitleEntry) entry).Item;

                var gp = new GamePatches(item);

                var newItem = await gp.AskForUpdate(this);

                if (newItem == null)
                {
                    continue;
                }

                StartDownload(newItem);
            }
        }

        private async void checkForPatchesAndDownload()
        {

            if (string.IsNullOrEmpty(Settings.HmacKey))
            {
                MessageBox.Show("No hmackey");
                return;
            }

            foreach (var entry in lstTitles.SelectedItems)
            {
                var item = ((TitleEntry) entry).Item;

                var gp = new GamePatches(item);

                var newItem = await gp.DownloadUpdateNoAsk(this);

                if (newItem == null)
                {
                    continue;
                }

                StartDownload(newItem);
            }
        }

        private void StartDownload(Item newItem)
        {
            var dw = new DownloadWorker(newItem);
            _downloadWorkerItems.Add(dw.lvi);
            downloads.Add(dw);
        }

        private void toggleDownloadedToolStripMenuItem_Click()
        {
            if (lstTitles.SelectedItems.Count == 0) return;

            for (int i = 0; i < lstTitles.SelectedItems.Count; i++)
            {
                var item = ((TitleEntry) lstTitles.SelectedItems[i]).Item;

                if (Settings.Instance.HistoryInstance.completedDownloading.Contains(item))
                {
                    //lstTitles.SelectedItems[i].BackColor = ColorTranslator.FromHtml("#FFFFFF");
                    Settings.Instance.HistoryInstance.completedDownloading.Remove(item);
                }
                else
                {
                    //lstTitles.SelectedItems[i].BackColor = ColorTranslator.FromHtml("#B7FF7C");
                    Settings.Instance.HistoryInstance.completedDownloading.Add(item);
                }
            }

            updateSearch();
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

        private void rbnDownloaded_CheckedChanged()
        {
            updateSearch();
        }

        private void rbnUndownloaded_CheckedChanged()
        {
            chkUnless.IsEnabled = rbnUndownloaded.IsChecked == true;
            updateSearch();
        }

        private void rbnAll_CheckedChanged()
        {
            updateSearch();
        }

        private void libraryToolStripMenuItem_Click()
        {
            var l = new Library(databaseAll);
            l.Owner = this;
            l.Show();
        }

        private async void toolStripMenuItem1_Click()
        {
            if (string.IsNullOrEmpty(Settings.Instance.CompPackUrl))
            {
                MessageBox.Show("No CompPack url");
                return;
            }

            if (lstTitles.SelectedItems.Count == 0) return;

            var item = ((TitleEntry) lstTitles.SelectedItems[0]).Item;
            var cp = new CompPack(this, item);

            var res = await cp.DoDialog(this);
            if (res == null)
            {
                return;
            }

            foreach (var itm in res)
            {
                StartDownload(itm);
            }
        }


        // ReSharper disable InconsistentNaming
        private DataGrid lstDownloadStatus;
        private DataGrid lstTitles;

        private MenuItem DownloadUpdateMenuItem;
        private MenuItem OptionsMenuItem;
        private MenuItem SyncMenuItem;
        private MenuItem ChangelogMenuItem;
        private MenuItem ExitMenuItem;
        private MenuItem DownloadMenuItem;
        private MenuItem UpdateChangelogMenuItem;
        private MenuItem libraryToolStripMenuItem;
        private MenuItem showDescriptionPanelToolStripMenuItem;

        private RadioButton rbnGames;
        private RadioButton rbnAvatars;
        private RadioButton rbnDLC;
        private RadioButton rbnThemes;
        private RadioButton rbnUpdates;

        private CheckBoxComboBox cmbType;
        private CheckBoxComboBox cmbRegion;

        private TextBox txtSearch;

        private RadioButton rbnAll;
        private RadioButton rbnUndownloaded;
        private RadioButton rbnDownloaded;

        private CheckBox chkUnless;

        private TextBlock lblCount;

        private Button btnDownload;

        private Button btnResume;
        private Button btnPause;
        private Button btnCancel;
        private Button btnClear;
        private Button btnOpenFolder;
        private Button btnResumeAll;
        private Button btnPauseAll;

        private MenuItem downloadAndUnpackToolStripMenuItem;
        private MenuItem showTitleDlcToolStripMenuItem;
        private MenuItem downloadAllDlcsToolStripMenuItem;
        private MenuItem downloadAllToolStripMenuItem;
        private MenuItem downloadAllWithPatchesToolStripMenuItem;
        private MenuItem checkForPatchesToolStripMenuItem;
        private MenuItem toggleDownloadedToolStripMenuItem;
        private MenuItem toolStripMenuItem1;


        // ReSharper restore InconsistentNaming

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            lstDownloadStatus = this.Find<DataGrid>("DownloadStatusList");
            lstTitles = this.Find<DataGrid>("lstTitles");

            DownloadUpdateMenuItem = this.Find<MenuItem>("DownloadUpdateMenuItem");
            OptionsMenuItem = this.Find<MenuItem>("OptionsMenuItem");
            SyncMenuItem = this.Find<MenuItem>("SyncMenuItem");
            ChangelogMenuItem = this.FindControl<MenuItem>("ChangelogMenuItem");
            ExitMenuItem = this.FindControl<MenuItem>("ExitMenuItem");
            DownloadMenuItem = this.FindControl<MenuItem>("DownloadMenuItem");
            UpdateChangelogMenuItem = this.FindControl<MenuItem>("UpdateChangelogMenuItem");
            libraryToolStripMenuItem = this.FindControl<MenuItem>("libraryToolStripMenuItem");
            showDescriptionPanelToolStripMenuItem = this.FindControl<MenuItem>("showDescriptionPanelToolStripMenuItem");

            rbnGames = this.FindControl<RadioButton>("rbnGames");
            rbnAvatars = this.FindControl<RadioButton>("rbnAvatars");
            rbnDLC = this.FindControl<RadioButton>("rbnDLC");
            rbnThemes = this.FindControl<RadioButton>("rbnThemes");
            rbnUpdates = this.FindControl<RadioButton>("rbnUpdates");

            cmbType = this.FindControl<CheckBoxComboBox>("cmbType");
            cmbRegion = this.FindControl<CheckBoxComboBox>("cmbRegion");

            txtSearch = this.FindControl<TextBox>("txtSearch");

            rbnAll = this.FindControl<RadioButton>("rbnAll");
            rbnUndownloaded = this.FindControl<RadioButton>("rbnUndownloaded");
            rbnDownloaded = this.FindControl<RadioButton>("rbnDownloaded");

            chkUnless = this.FindControl<CheckBox>("chkUnless");

            lblCount = this.FindControl<TextBlock>("lblCount");

            btnDownload = this.FindControl<Button>("btnDownload");

            btnResume = this.FindControl<Button>("btnResume");
            btnPause = this.FindControl<Button>("btnPause");
            btnCancel = this.FindControl<Button>("btnCancel");
            btnClear = this.FindControl<Button>("btnClear");
            btnOpenFolder = this.FindControl<Button>("btnOpenFolder");
            btnResumeAll = this.FindControl<Button>("btnResumeAll");
            btnPauseAll = this.FindControl<Button>("btnPauseAll");

            downloadAndUnpackToolStripMenuItem = this.FindControl<MenuItem>("downloadAndUnpackToolStripMenuItem");
            showTitleDlcToolStripMenuItem = this.FindControl<MenuItem>("showTitleDlcToolStripMenuItem");
            downloadAllDlcsToolStripMenuItem = this.FindControl<MenuItem>("downloadAllDlcsToolStripMenuItem");
            downloadAllToolStripMenuItem = this.FindControl<MenuItem>("downloadAllToolStripMenuItem");
            downloadAllWithPatchesToolStripMenuItem = this.FindControl<MenuItem>("downloadAllWithPatchesToolStripMenuItem");
            checkForPatchesToolStripMenuItem = this.FindControl<MenuItem>("checkForPatchesToolStripMenuItem");
            toggleDownloadedToolStripMenuItem = this.FindControl<MenuItem>("toggleDownloadedToolStripMenuItem");
            toolStripMenuItem1 = this.FindControl<MenuItem>("toolStripMenuItem1");

            this.WhenAnyValue(x => x.txtSearch.Text)
                .Subscribe(_ => txtSearch_TextChanged());

            this.WhenAnyValue(x => x.rbnGames.IsChecked)
                .Subscribe(_ => rbnGames_CheckedChanged());

            this.WhenAnyValue(x => x.rbnAvatars.IsChecked)
                .Subscribe(_ => rbnAvatars_CheckedChanged());

            this.WhenAnyValue(x => x.rbnThemes.IsChecked)
                .Subscribe(_ => rbnThemes_CheckedChanged());

            this.WhenAnyValue(x => x.rbnDLC.IsChecked)
                .Subscribe(_ => rbnDLC_CheckedChanged());

            this.WhenAnyValue(x => x.rbnUpdates.IsChecked)
                .Subscribe(_ => rbnUpdates_CheckedChanged());

            this.WhenAnyValue(x => x.rbnDownloaded.IsChecked)
                .Subscribe(_ => rbnDownloaded_CheckedChanged());

            this.WhenAnyValue(x => x.rbnAll.IsChecked)
                .Subscribe(_ => rbnAll_CheckedChanged());

            this.WhenAnyValue(x => x.rbnUndownloaded.IsChecked)
                .Subscribe(_ => rbnUndownloaded_CheckedChanged());

            btnDownload.Command = ReactiveCommand.Create(btnDownload_Click);

            btnResume.Command = ReactiveCommand.Create(resumeToolStripMenuItem_Click);
            btnPause.Command = ReactiveCommand.Create(pauseToolStripMenuItem_Click);
            btnCancel.Command = ReactiveCommand.Create(cancelToolStripMenuItem_Click);
            btnClear.Command = ReactiveCommand.Create(clearCompletedToolStripMenuItem_Click);
            btnOpenFolder.Command = ReactiveCommand.Create(button5_Click);
            btnResumeAll.Command = ReactiveCommand.Create(ResumeAllBtnClick);
            btnPauseAll.Command = ReactiveCommand.Create(PauseAllBtnClick);

            downloadAndUnpackToolStripMenuItem.Command = ReactiveCommand.Create(btnDownload_Click);
            showTitleDlcToolStripMenuItem.Command = ReactiveCommand.Create(showTitleDlcToolStripMenuItem_Click);
            downloadAllDlcsToolStripMenuItem.Command = ReactiveCommand.Create(downloadAllDlcsToolStripMenuItem_Click);
            downloadAllToolStripMenuItem.Command = ReactiveCommand.Create(downloadAllToolStripMenuItem_Click);
            downloadAllWithPatchesToolStripMenuItem.Command = ReactiveCommand.Create(downloadAllWithPatchesToolStripMenuItem_Click);
            checkForPatchesToolStripMenuItem.Command = ReactiveCommand.Create(checkForPatchesToolStripMenuItem_Click);
            toggleDownloadedToolStripMenuItem.Command = ReactiveCommand.Create(toggleDownloadedToolStripMenuItem_Click);
            toolStripMenuItem1.Command = ReactiveCommand.Create(toolStripMenuItem1_Click);

            OptionsMenuItem.Command = ReactiveCommand.Create(optionsToolStripMenuItem_Click);
            SyncMenuItem.Command = ReactiveCommand.Create(Sync);
            ChangelogMenuItem.Command = ReactiveCommand.Create(ts_changeLog_Click);
            UpdateChangelogMenuItem.Command = ReactiveCommand.Create(changelogToolStripMenuItem_Click);
            ExitMenuItem.Command = ReactiveCommand.Create(exitToolStripMenuItem_Click);
            DownloadMenuItem.Command = ReactiveCommand.Create(downloadUpdateToolStripMenuItem_Click);
            libraryToolStripMenuItem.Command = ReactiveCommand.Create(libraryToolStripMenuItem_Click);
            showDescriptionPanelToolStripMenuItem.Command = ReactiveCommand.Create(ShowDescriptionPanel);
        }
    }

    public class TitleEntry
    {
        public string TitleId { get; set; }
        public string Region { get; set; }
        public string TitleName { get; set; }
        public string ContentType { get; set; }

        public string DLCs { get; set; }

        // Used for sorting.
        public int DlcCount { get; set; }
        public DateTime LastModifiedTime { get; set; }
        public string LastModified { get; set; }
        public Color BackColor { get; set; }
        public Item Item { get; set; }
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