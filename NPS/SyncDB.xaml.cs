using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace NPS
{
    public class SyncDB : Window
    {
        private int dbCounter = 0; //17

        public SyncDB()
        {
            InitializeComponent();

            progressBar1.Maximum = (17 + 1) * 100;
        }

        public Task<List<Item>> Sync()
        {
            // TODO: Does Avalonia support this?
            //this.TopMost = true;

            return Task.Run(async () =>
            {
                var games = new List<Item>();
                var dlcs = new List<Item>();

                await LoadDatabase(Settings.Instance.PsvUpdateUri, DatabaseType.VitaUpdate);
                await LoadDatabase(Settings.Instance.Ps4UpdateUri, DatabaseType.PS4Update);

                games.AddRange(await LoadDatabase(Settings.Instance.PsvThemeUri, DatabaseType.VitaTheme));
                await LoadDatabase(Settings.Instance.PspThemeUri, DatabaseType.PSPTheme);
                await LoadDatabase(Settings.Instance.Ps3ThemeUri, DatabaseType.PS3Theme);
                await LoadDatabase(Settings.Instance.Ps4ThemeUri, DatabaseType.PS4Theme);

                dlcs.AddRange(await LoadDatabase(Settings.Instance.PsvDlcUri, DatabaseType.VitaDLC));
                dlcs.AddRange(await LoadDatabase(Settings.Instance.PspDlcUri, DatabaseType.PSPDLC));
                dlcs.AddRange(await LoadDatabase(Settings.Instance.Ps3DlcUri, DatabaseType.PS3DLC));
                dlcs.AddRange(await LoadDatabase(Settings.Instance.Ps4DlcUri, DatabaseType.PS4DLC));

                await LoadDatabase(Settings.Instance.Ps3AvatarUri, DatabaseType.PS3Avatar);

                games.AddRange(await LoadDatabase(Settings.Instance.PsvUri, DatabaseType.Vita));
                games.AddRange(await LoadDatabase(Settings.Instance.PsmUri, DatabaseType.ItsPsm));
                games.AddRange(await LoadDatabase(Settings.Instance.PsxUri, DatabaseType.ItsPSX));
                games.AddRange(await LoadDatabase(Settings.Instance.PspUri, DatabaseType.PSP));
                games.AddRange(await LoadDatabase(Settings.Instance.Ps3Uri, DatabaseType.PS3));
                games.AddRange(await LoadDatabase(Settings.Instance.Ps4Uri, DatabaseType.PS4));

                games.AddRange(dlcs);

                return games;
            });
        }

        private Task<List<Item>> LoadDatabase(string path, DatabaseType dbType)
        {
            dbCounter++;

            var dbs = new List<Item>();
            if (string.IsNullOrEmpty(path))
                return Task.FromResult(dbs);

            return Task.Run(async () =>
            {
                path = new Uri(path).ToString();

                try
                {
                    using var wc = new WebClient
                    {
                        Encoding = System.Text.Encoding.UTF8,
                        Proxy = Settings.Instance.Proxy
                    };

                    wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
                    var content = await wc.DownloadStringTaskAsync(new Uri(path));
                    //content = Encoding.UTF8.GetString(Encoding.Default.GetBytes(content));

                    var lines = content.Split(new[] {"\r\n", "\n\r", "\n", "\r"}, StringSplitOptions.None);

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

                        if ((itm.pkg.ToLower().Contains("http://") || itm.pkg.ToLower().Contains("https://")) &&
                            !itm.zRif.ToLower().Contains("missing"))
                        {
                            if (itm.zRif.ToLower().Contains("not required")) itm.zRif = "";


                            itm.Region = itm.Region.Replace(" ", "");
                            dbs.Add(itm);
                        }
                    }
                }
                catch (Exception err)
                {
                }

                return dbs;
            });
        }

        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            //progressBar1.Maximum = e.TotalBytesToReceive;
            try
            {
                Dispatcher.UIThread.Post(() => { progressBar1.Value = e.ProgressPercentage + (dbCounter * 100); });
            }
            catch
            {
            }
        }

        // ReSharper disable once InconsistentNaming
        private ProgressBar progressBar1;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            progressBar1 = this.FindControl<ProgressBar>("progressBar1");
        }
    }
}