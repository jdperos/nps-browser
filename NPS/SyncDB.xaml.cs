using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace NPS
{
    // ReSharper disable once InconsistentNaming
    public class SyncDB : Window
    {
        private int _dbCounter;
        private int _counterMax;

        public SyncDB()
        {
            InitializeComponent();

        }

        public async Task<List<Item>> Sync()
        {
            // TODO: Does Avalonia support this?
            //this.TopMost = true;

            var tasks = new[]
            {
                // TODO(jon): why are these commented out?
                LoadDatabase(Settings.Instance.PsvUpdateUri, DatabaseType.VitaUpdate),
                LoadDatabase(Settings.Instance.Ps4UpdateUri, DatabaseType.PS4Update),

                LoadDatabase(Settings.Instance.PsvUri, DatabaseType.Vita),
                LoadDatabase(Settings.Instance.PsmUri, DatabaseType.ItsPsm),
                LoadDatabase(Settings.Instance.PsxUri, DatabaseType.ItsPSX),
                LoadDatabase(Settings.Instance.PspUri, DatabaseType.PSP),
                LoadDatabase(Settings.Instance.Ps3Uri, DatabaseType.PS3),
                LoadDatabase(Settings.Instance.Ps4Uri, DatabaseType.PS4),

                LoadDatabase(Settings.Instance.PsvDlcUri, DatabaseType.VitaDLC),
                LoadDatabase(Settings.Instance.PspDlcUri, DatabaseType.PSPDLC),
                LoadDatabase(Settings.Instance.Ps3DlcUri, DatabaseType.PS3DLC),
                LoadDatabase(Settings.Instance.Ps4DlcUri, DatabaseType.PS4DLC),

                LoadDatabase(Settings.Instance.PsvThemeUri, DatabaseType.VitaTheme),
                LoadDatabase(Settings.Instance.Ps3AvatarUri, DatabaseType.PS3Avatar),
                LoadDatabase(Settings.Instance.PspThemeUri, DatabaseType.PSPTheme),
                LoadDatabase(Settings.Instance.Ps3ThemeUri, DatabaseType.PS3Theme),
                LoadDatabase(Settings.Instance.Ps4ThemeUri, DatabaseType.PS4Theme)
            };

            _counterMax = tasks.Length;
            progressBar1.Maximum = (_counterMax + 1) * 100;

            return (await Task.WhenAll(tasks)).SelectMany(p => p).ToList();
        }

        private async Task<List<Item>> LoadDatabase(string path, DatabaseType dbType)
        {
            var dbs = new List<Item>();
            if (string.IsNullOrEmpty(path))
            {
                OneDownloadDone();
                return dbs;
            }

            using var wc = new WebClient
            {
                Encoding = System.Text.Encoding.UTF8,
                Proxy = Settings.Instance.Proxy
            };

            var content = await wc.DownloadStringTaskAsync(new Uri(path));

            await Task.Run(() =>
            {
                path = new Uri(path).ToString();

                try
                {
                    var lines = content.Split(new[] {"\r\n", "\n\r", "\n", "\r"}, StringSplitOptions.None);

                    for (int i = 1; i < lines.Length; i++)
                    {
                        var a = lines[i].Split('\t');

                        if (a.Length < 2)
                        {
                            continue;
                        }

                        var itm = new Item
                        {
                            TitleId = a[0],
                            Region = a[1],
                            TitleName = a[2],
                            pkg = a[3],
                            zRif = a[4],
                            ContentId = a[5],
                        };

                        switch (dbType)
                        {
                            // PSV
                            case DatabaseType.Vita:
                                itm.contentType = "VITA";

                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                                break;
                            case DatabaseType.VitaDLC:
                                itm.contentType = "VITA";
                                itm.IsDLC = true;

                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                                break;
                            case DatabaseType.VitaTheme:
                                itm.contentType = "VITA";
                                itm.IsTheme = true;
                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                                break;
                            case DatabaseType.VitaUpdate:
                                itm.contentType = "VITA";
                                itm.IsUpdate = true;

                                itm.ContentId = null;
                                itm.zRif = "";
                                itm.TitleName = a[2] + " (" + a[3] + ")";
                                itm.pkg = a[5];
                                DateTime.TryParse(a[7], out itm.lastModifyDate);
                                break;
                            // PSP
                            case DatabaseType.PSP:
                                itm.ItsPsp = true;
                                itm.contentType = "PSP";

                                itm.contentType = a[2];
                                itm.TitleName = a[3];
                                itm.pkg = a[4];
                                itm.ContentId = a[5];
                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                                itm.zRif = a[7];
                                break;
                            case DatabaseType.PSPDLC:
                                itm.ItsPsp = true;
                                itm.contentType = "PSP";
                                itm.IsDLC = true;

                                itm.ContentId = a[4];
                                DateTime.TryParse(a[5], out itm.lastModifyDate);
                                itm.zRif = a[6];
                                break;
                            case DatabaseType.PSPTheme:
                                itm.ItsPsp = true;
                                itm.contentType = "PSP";
                                itm.IsTheme = true;

                                itm.zRif = "";
                                itm.ContentId = a[4];
                                DateTime.TryParse(a[5], out itm.lastModifyDate);
                                break;
                            // PS3
                            case DatabaseType.PS3:
                                itm.contentType = "PS3";
                                itm.ItsPS3 = true;

                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                                break;
                            case DatabaseType.PS3Avatar:
                                itm.ItsPS3 = true;
                                itm.contentType = "PS3";
                                itm.IsAvatar = true;

                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                                break;
                            case DatabaseType.PS3DLC:
                                itm.ItsPS3 = true;
                                itm.contentType = "PS3";
                                itm.IsDLC = true;

                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                                break;
                            // PS4
                            case DatabaseType.PS3Theme:
                                itm.ItsPS3 = true;
                                itm.contentType = "PS3";
                                itm.IsTheme = true;

                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                                break;
                            case DatabaseType.PS4:
                                itm.ItsPS4 = true;
                                itm.contentType = "PS4";

                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                                break;
                            case DatabaseType.PS4DLC:
                                itm.ItsPS4 = true;
                                itm.contentType = "PS4";
                                itm.IsDLC = true;

                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                                break;
                            case DatabaseType.PS4Theme:
                                itm.ItsPS4 = true;
                                itm.contentType = "PS4";
                                itm.IsTheme = true;

                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                                break;
                            case DatabaseType.PS4Update:
                                itm.ItsPS4 = true;
                                itm.contentType = "PS4";
                                itm.IsUpdate = true;

                                itm.ContentId = null;
                                itm.zRif = "";
                                itm.TitleName = $"{a[2]} ({a[3]})";
                                itm.pkg = a[5];
                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                                break;
                            // Others
                            case DatabaseType.ItsPsm:
                                itm.contentType = "PSM";

                                itm.ContentId = null;
                                DateTime.TryParse(a[6], out itm.lastModifyDate);
                                break;
                            case DatabaseType.ItsPSX:
                                itm.contentType = "PSX";
                                itm.ItsPsx = true;

                                itm.zRif = "";
                                itm.ContentId = a[4];
                                DateTime.TryParse(a[5], out itm.lastModifyDate);
                                break;
                        }

                        if ((itm.pkg.Contains("http://", StringComparison.OrdinalIgnoreCase) || itm.pkg.Contains("https://", StringComparison.OrdinalIgnoreCase)) &&
                            !itm.zRif.Contains("missing", StringComparison.OrdinalIgnoreCase))
                        {
                            if (itm.zRif.Contains("not required", StringComparison.OrdinalIgnoreCase)) itm.zRif = "";


                            itm.Region = itm.Region.Replace(" ", "");
                            dbs.Add(itm);
                        }
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine("Failed while loading database {0}:\n{1}", dbType, err);
                }
            });

            OneDownloadDone();

            return dbs;
        }

        private void OneDownloadDone()
        {
            Dispatcher.UIThread.Post(() =>
            {
                _dbCounter += 1;
                progressBar1.Value = _dbCounter * 100;
            });
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