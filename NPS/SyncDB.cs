using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NPS
{
    public partial class SyncDB : Form
    {
        List<Item> dlcsDbs = new List<Item>(), gamesDbs = new List<Item>();
        int dbCounter = 0;//17

        public SyncDB()
        {
            InitializeComponent();
        }

        private void SyncDB_Load(object sender, EventArgs e)
        {
            progressBar1.Maximum = (17 + 1) * 100;
        }

        public void Sync(Action<List<Item>> result)
        {
            this.TopMost = true;
            LoadDatabase(Settings.Instance.PSVUpdateUri, (psvupd) =>
            {

                //  updatesDbs.AddRange(psvupd);

                LoadDatabase(Settings.Instance.PS4UpdateUri, (ps4upd) =>
                {
                    //  updatesDbs.AddRange(ps4upd);

                    // Theme DBs
                    LoadDatabase(Settings.Instance.PSVThemeUri, (psvthm) =>
                    {
                        gamesDbs.AddRange(psvthm);

                        LoadDatabase(Settings.Instance.PSPThemeUri, (pspthm) =>
                        {
                            //    themesDbs.AddRange(pspthm);

                            LoadDatabase(Settings.Instance.PS3ThemeUri, (ps3thm) =>
                            {
                                //    themesDbs.AddRange(ps3thm);

                                LoadDatabase(Settings.Instance.PS4ThemeUri, (ps4thm) =>
                                {
                                    //      themesDbs.AddRange(ps4thm);

                                    // DLC DBs
                                    LoadDatabase(Settings.Instance.PSVDLCUri, (db) =>
                                    {
                                        dlcsDbs.AddRange(db);

                                        LoadDatabase(Settings.Instance.PSPDLCUri, (pspdlc) =>
                                        {
                                            dlcsDbs.AddRange(pspdlc);

                                            LoadDatabase(Settings.Instance.PS3DLCUri, (ps3dlc) =>
                                            {
                                                dlcsDbs.AddRange(ps3dlc);

                                                LoadDatabase(Settings.Instance.PS4DLCUri, (ps4dlc) =>
                                                {
                                                    dlcsDbs.AddRange(ps4dlc);

                                                    // Avatar DBs
                                                    LoadDatabase(Settings.Instance.PS3AvatarUri, (ps3avatar) =>
                                                    {
                                                        // avatarsDbs.AddRange(ps3avatar);

                                                        // Game DBs
                                                        LoadDatabase(Settings.Instance.PSVUri, (vita) =>
                                                        {
                                                            gamesDbs.AddRange(vita);

                                                            LoadDatabase(Settings.Instance.PSMUri, (psm) =>
                                                            {
                                                                gamesDbs.AddRange(psm);

                                                                LoadDatabase(Settings.Instance.PSXUri, (psx) =>
                                                                {
                                                                    gamesDbs.AddRange(psx);

                                                                    LoadDatabase(Settings.Instance.PSPUri, (psp) =>
                                                                    {
                                                                        gamesDbs.AddRange(psp);

                                                                        LoadDatabase(Settings.Instance.PS3Uri, (ps3) =>
                                                                        {
                                                                            gamesDbs.AddRange(ps3);

                                                                            LoadDatabase(Settings.Instance.PS4Uri, (ps4) =>
                                                                            {
                                                                                gamesDbs.AddRange(ps4);
                                                                                gamesDbs.AddRange(dlcsDbs);
                                                                                result.Invoke(gamesDbs);

                                                                                // Game DBs
                                                                            }, DatabaseType.PS4);
                                                                        }, DatabaseType.PS3);
                                                                    }, DatabaseType.PSP);
                                                                }, DatabaseType.ItsPSX);
                                                            }, DatabaseType.ItsPsm);
                                                        }, DatabaseType.Vita);

                                                        // Avatar DBs
                                                    }, DatabaseType.PS3Avatar);

                                                    // DLC DBs
                                                }, DatabaseType.PS4DLC);
                                            }, DatabaseType.PS3DLC);
                                        }, DatabaseType.PSPDLC);
                                    }, DatabaseType.VitaDLC);

                                    // Theme DBs
                                }, DatabaseType.PS4Theme);
                            }, DatabaseType.PS3Theme);
                        }, DatabaseType.PSPTheme);
                    }, DatabaseType.VitaTheme);

                    // Update DBs
                }, DatabaseType.PS4Update);
            }, DatabaseType.VitaUpdate);

        }


        private void LoadDatabase(string path, Action<List<Item>> result, DatabaseType dbType)
        {
            dbCounter++;
            List<Item> dbs = new List<Item>();
            if (string.IsNullOrEmpty(path))
                result.Invoke(dbs);
            else
            {
                Task.Run(() =>
                {
                    path = new Uri(path).ToString();

                    try
                    {
                        WebClient wc = new WebClient();
                        wc.Encoding = System.Text.Encoding.UTF8;
                        wc.Proxy = Settings.Instance.proxy;
                        wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
                        string content = wc.DownloadStringTaskAsync(new Uri(path)).Result;
                        wc.Dispose();
                        //content = Encoding.UTF8.GetString(Encoding.Default.GetBytes(content));

                        string[] lines = content.Split(new string[] { "\r\n", "\n\r", "\n", "\r" }, StringSplitOptions.None);

                        for (int i = 1; i < lines.Length; i++)
                        {
                            var a = lines[i].Split('\t');

                            if (a.Length < 2)
                            {
                                continue;
                            }

                            var itm = new Item()
                            {
                                TitleId = a[0],
                                Region = a[1],
                                TitleName = a[2],
                                pkg = a[3],
                                zRif = a[4],
                                ContentId = a[5],
                            };

                            // PSV
                            if (dbType == DatabaseType.Vita)
                            {
                                itm.contentType = "VITA";

                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                            }
                            else if (dbType == DatabaseType.VitaDLC)
                            {
                                itm.contentType = "VITA";
                                itm.IsDLC = true;

                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                            }
                            else if (dbType == DatabaseType.VitaTheme)
                            {
                                itm.contentType = "VITA";
                                itm.IsTheme = true;
                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                            }
                            else if (dbType == DatabaseType.VitaUpdate)
                            {
                                itm.contentType = "VITA";
                                itm.IsUpdate = true;

                                itm.ContentId = null;
                                itm.zRif = "";
                                itm.TitleName = a[2] + " (" + a[3] + ")";
                                itm.pkg = a[5];
                                DateTime.TryParse(a[7], out itm.lastModifyDate);
                            }

                            // PSP
                            else if (dbType == DatabaseType.PSP)
                            {
                                itm.ItsPsp = true;
                                itm.contentType = "PSP";

                                itm.contentType = a[2];
                                itm.TitleName = a[3];
                                itm.pkg = a[4];
                                itm.ContentId = a[5];
                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                                itm.zRif = a[7];
                            }
                            else if (dbType == DatabaseType.PSPDLC)
                            {
                                itm.ItsPsp = true;
                                itm.contentType = "PSP";
                                itm.IsDLC = true;

                                itm.ContentId = a[4];
                                DateTime.TryParse(a[5], out itm.lastModifyDate);
                                itm.zRif = a[6];
                            }
                            else if (dbType == DatabaseType.PSPTheme)
                            {
                                itm.ItsPsp = true;
                                itm.contentType = "PSP";
                                itm.IsTheme = true;

                                itm.zRif = "";
                                itm.ContentId = a[4];
                                DateTime.TryParse(a[5], out itm.lastModifyDate);
                            }

                            // PS3
                            else if (dbType == DatabaseType.PS3)
                            {
                                itm.contentType = "PS3";
                                itm.ItsPS3 = true;

                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                            }
                            else if (dbType == DatabaseType.PS3Avatar)
                            {
                                itm.ItsPS3 = true;
                                itm.contentType = "PS3";
                                itm.IsAvatar = true;

                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                            }
                            else if (dbType == DatabaseType.PS3DLC)
                            {
                                itm.ItsPS3 = true;
                                itm.contentType = "PS3";
                                itm.IsDLC = true;

                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                            }
                            else if (dbType == DatabaseType.PS3Theme)
                            {
                                itm.ItsPS3 = true;
                                itm.contentType = "PS3";
                                itm.IsTheme = true;

                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                            }

                            // PS4
                            else if (dbType == DatabaseType.PS4)
                            {
                                itm.ItsPS4 = true;
                                itm.contentType = "PS4";

                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                            }
                            else if (dbType == DatabaseType.PS4DLC)
                            {
                                itm.ItsPS4 = true;
                                itm.contentType = "PS4";
                                itm.IsDLC = true;

                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                            }
                            else if (dbType == DatabaseType.PS4Theme)
                            {
                                itm.ItsPS4 = true;
                                itm.contentType = "PS4";
                                itm.IsTheme = true;

                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                            }
                            else if (dbType == DatabaseType.PS4Update)
                            {
                                itm.ItsPS4 = true;
                                itm.contentType = "PS4";
                                itm.IsUpdate = true;

                                itm.ContentId = null;
                                itm.zRif = "";
                                itm.TitleName = a[2] + " (" + a[3] + ")";
                                itm.pkg = a[5];
                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                            }

                            // Others
                            else if (dbType == DatabaseType.ItsPsm)
                            {
                                itm.contentType = "PSM";

                                itm.ContentId = null;
                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                            }
                            else if (dbType == DatabaseType.ItsPSX)
                            {
                                itm.contentType = "PSX";
                                itm.ItsPsx = true;

                                itm.zRif = "";
                                itm.ContentId = a[4];
                                DateTime.TryParse(a[5], out itm.lastModifyDate);
                            }

                            if ((itm.pkg.ToLower().Contains("http://") || itm.pkg.ToLower().Contains("https://")) && !itm.zRif.ToLower().Contains("missing"))
                            {
                                if (itm.zRif.ToLower().Contains("not required")) itm.zRif = "";


                                itm.Region = itm.Region.Replace(" ", "");
                                dbs.Add(itm);

                            }

                        }
                    }
                    catch (Exception err) { }
                    result.Invoke(dbs);
                });
            }
        }

        private void SyncDB_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            //progressBar1.Maximum = e.TotalBytesToReceive;
            try
            {
                Invoke(new Action(() =>
            {

                progressBar1.Value = e.ProgressPercentage + (dbCounter * 100);

            }));
            }
            catch { }

        }
    }
}
