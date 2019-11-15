using System;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using NPS.Helpers;
using ReactiveUI;

namespace NPS
{
    public class Options : Window
    {
        private readonly NpsBrowser _mainForm;
        private bool needResync = false;

        public Options() : this(null)
        {
        }

        public Options(NpsBrowser main)
        {
            InitializeComponent();
            _mainForm = main;
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            LoadSettings();
        }

        private void LoadSettings()
        {
            // Settings
            textDownload.Text = Settings.Instance.DownloadDir;
            textPKGPath.Text = Settings.Instance.PkgPath;
            textParams.Text = Settings.Instance.PkgParams;
            deleteAfterUnpack.IsChecked = Settings.Instance.DeleteAfterUnpack;
            simultaneousDl.Value = Settings.Instance.SimultaneousDl;

            // Game URIs
            tb_psvuri.Text = Settings.Instance.PsvUri;
            tb_psmuri.Text = Settings.Instance.PsmUri;
            tb_psxuri.Text = Settings.Instance.PsxUri;
            tb_pspuri.Text = Settings.Instance.PspUri;
            tb_ps3uri.Text = Settings.Instance.Ps3Uri;
            tb_ps4uri.Text = Settings.Instance.Ps4Uri;

            // Avatar URIs
            tb_ps3avataruri.Text = Settings.Instance.Ps3AvatarUri;

            // DLC URIs
            tb_psvdlcuri.Text = Settings.Instance.PsvDlcUri;
            tb_pspdlcuri.Text = Settings.Instance.PspDlcUri;
            tb_ps3dlcuri.Text = Settings.Instance.Ps3DlcUri;
            tb_ps4dlcuri.Text = Settings.Instance.Ps4DlcUri;

            // Theme URIs
            tb_psvthmuri.Text = Settings.Instance.PsvThemeUri;
            tb_pspthmuri.Text = Settings.Instance.PspThemeUri;
            tb_ps3thmuri.Text = Settings.Instance.Ps3ThemeUri;
            tb_ps4thmuri.Text = Settings.Instance.Ps4ThemeUri;

            // Update URIs
            tb_psvupduri.Text = Settings.Instance.PsvUpdateUri;
            tb_ps4upduri.Text = Settings.Instance.Ps4UpdateUri;
            hmacTB.Text = Settings.Instance.HmacKey;
            tb_compPack.Text = Settings.Instance.CompPackUrl;
            tb_compackPatch.Text = Settings.Instance.CompPackPatchUrl;

            chkbx_proxy.IsChecked = Settings.Instance.Proxy != null;
            if (Settings.Instance.Proxy != null)
            {
                tb_proxyPort.Text = Settings.Instance.Proxy.Address.Port.ToString();
                tb_proxyServer.Text = Settings.Instance.Proxy.Address.Host;
            }

            lblCacheDate.Text = "Cache date: " + NPCache.I.UpdateDate.ToString(CultureInfo.CurrentCulture);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var fbd = new OpenFileDialog();
            var res = await fbd.ShowAsync((Window) this.GetVisualRoot());
            if (res == null)
            {
                return;
            }

            Settings.Instance.DownloadDir = res[0];
            textDownload.Text = res[0];
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            var fbd = new OpenFileDialog();
            // TODO: File filters, *.exe only on windows.
            var res = await fbd.ShowAsync((Window) this.GetVisualRoot());
            if (res == null)
            {
                return;
            }

            Settings.Instance.PkgPath = res[0];
            textPKGPath.Text = res[0];
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            UpdateSettings(true);
        }

        private async void UpdateSettings(bool withStoring)
        {
            needResync = needResync || Settings.Instance.PsvUri != tb_psvuri.Text ||
                         Settings.Instance.PsmUri != tb_psmuri.Text ||
                         Settings.Instance.PsxUri != tb_psxuri.Text ||
                         Settings.Instance.PspUri != tb_pspuri.Text ||
                         Settings.Instance.Ps3Uri != tb_ps3uri.Text ||
                         Settings.Instance.Ps4Uri != tb_ps4uri.Text ||
                         Settings.Instance.PsvThemeUri != tb_psvthmuri.Text ||
                         Settings.Instance.PsvDlcUri != tb_psvdlcuri.Text ||
                         Settings.Instance.PspDlcUri != tb_pspdlcuri.Text ||
                         Settings.Instance.Ps3DlcUri != tb_ps3dlcuri.Text ||
                         Settings.Instance.Ps4DlcUri != tb_ps4dlcuri.Text;

            // Settings
            Settings.Instance.DownloadDir = textDownload.Text;
            Settings.Instance.PkgPath = textPKGPath.Text;
            Settings.Instance.PkgParams = textParams.Text;
            Settings.Instance.DeleteAfterUnpack = deleteAfterUnpack.IsChecked.Value;
            Settings.Instance.SimultaneousDl = (int) simultaneousDl.Value;

            // Game URIs
            Settings.Instance.PsvUri = tb_psvuri.Text;
            Settings.Instance.PsmUri = tb_psmuri.Text;
            Settings.Instance.PsxUri = tb_psxuri.Text;
            Settings.Instance.PspUri = tb_pspuri.Text;
            Settings.Instance.Ps3Uri = tb_ps3uri.Text;
            Settings.Instance.Ps4Uri = tb_ps4uri.Text;

            // Avatar URIs
            Settings.Instance.Ps3AvatarUri = tb_ps3avataruri.Text;

            // DLC URIs
            Settings.Instance.PsvDlcUri = tb_psvdlcuri.Text;
            Settings.Instance.PspDlcUri = tb_pspdlcuri.Text;
            Settings.Instance.Ps3DlcUri = tb_ps3dlcuri.Text;
            Settings.Instance.Ps4DlcUri = tb_ps4dlcuri.Text;

            // Theme URIs
            Settings.Instance.PsvThemeUri = tb_psvthmuri.Text;
            Settings.Instance.PspThemeUri = tb_pspthmuri.Text;
            Settings.Instance.Ps3ThemeUri = tb_ps3thmuri.Text;
            Settings.Instance.Ps4ThemeUri = tb_ps4thmuri.Text;

            // Update URIs
            Settings.Instance.PsvUpdateUri = tb_psvupduri.Text;
            Settings.Instance.Ps4UpdateUri = tb_ps4upduri.Text;
            Settings.Instance.HmacKey = hmacTB.Text;
            if (Settings.Instance.CompPackUrl != tb_compPack.Text ||
                Settings.Instance.CompPackPatchUrl != tb_compackPatch.Text)
                CompPack.compPackChanged = true;

            Settings.Instance.CompPackUrl = tb_compPack.Text;
            Settings.Instance.CompPackPatchUrl = tb_compackPatch.Text;

            if (chkbx_proxy.IsChecked.Value)
            {
                Settings.Instance.Proxy = new WebProxy(tb_proxyServer.Text, int.Parse(tb_proxyPort.Text));
                Settings.Instance.Proxy.Credentials = CredentialCache.DefaultCredentials;
            }
            else Settings.Instance.Proxy = null;

            if (withStoring)
            {
                Settings.Instance.Save();
                if (needResync)
                {
                    await _mainForm.Sync();
                }
            }
        }

        private void deleteAfterUnpack_CheckedChanged()
        {
            Settings.Instance.DeleteAfterUnpack = deleteAfterUnpack.IsChecked.Value;
        }

        private void simultaneous_ValueChanged()
        {
            Settings.Instance.SimultaneousDl = (int) simultaneousDl.Value;
        }

        private void linkLabel1_LinkClicked()
        {
            MessageBox.Show(@"Here you can give parameters to pass to your pkg dec tool. Available variables are: 
- {zRifKey}
- {pkgFile}
- {gameTitle}
- {region}
- {titleID}
- {fwversion}");
        }


        private async void btnSyncNow_Click()
        {
            await _mainForm.Sync();

            lblCacheDate.Text = "Cache date: " + NPCache.I.UpdateDate.ToString(CultureInfo.CurrentCulture);
        }

        private void textBox2_KeyPress()
        {
            // TODO: this
/*            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }*/
        }

        private void chkbx_proxy_CheckedChanged()
        {
            tb_proxyPort.IsEnabled = tb_proxyServer.IsEnabled = chkbx_proxy.IsChecked.Value;
        }

        // ReSharper disable InconsistentNaming IdentifierTypo
        private BrowseableUriEntry tb_psvuri;
        private BrowseableUriEntry tb_psmuri;
        private BrowseableUriEntry tb_psxuri;
        private BrowseableUriEntry tb_pspuri;
        private BrowseableUriEntry tb_ps3uri;
        private BrowseableUriEntry tb_ps4uri;

        private BrowseableUriEntry tb_ps3avataruri;

        private BrowseableUriEntry tb_psvdlcuri;
        private BrowseableUriEntry tb_pspdlcuri;
        private BrowseableUriEntry tb_ps3dlcuri;
        private BrowseableUriEntry tb_ps4dlcuri;

        private BrowseableUriEntry tb_psvthmuri;
        private BrowseableUriEntry tb_pspthmuri;
        private BrowseableUriEntry tb_ps3thmuri;
        private BrowseableUriEntry tb_ps4thmuri;

        private BrowseableUriEntry tb_psvupduri;
        private BrowseableUriEntry tb_ps4upduri;

        private BrowseableUriEntry textDownload;
        private BrowseableUriEntry textPKGPath;
        private TextBox textParams;

        private CheckBox deleteAfterUnpack;

        private NumericUpDown simultaneousDl;

        private TextBox hmacTB;
        private TextBox tb_compPack;
        private TextBox tb_compackPatch;

        private CheckBox chkbx_proxy;
        private TextBox tb_proxyServer;
        private TextBox tb_proxyPort;
        private TextBlock lblCacheDate;
        private Button btnSyncNow;

        // ReSharper restore InconsistentNaming IdentifierTypo

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            tb_psvuri = this.FindControl<BrowseableUriEntry>("tb_psvuri");
            tb_psmuri = this.FindControl<BrowseableUriEntry>("tb_psmuri");
            tb_psxuri = this.FindControl<BrowseableUriEntry>("tb_psxuri");
            tb_pspuri = this.FindControl<BrowseableUriEntry>("tb_pspuri");
            tb_ps3uri = this.FindControl<BrowseableUriEntry>("tb_ps3uri");
            tb_ps4uri = this.FindControl<BrowseableUriEntry>("tb_ps4uri");

            tb_ps3avataruri = this.FindControl<BrowseableUriEntry>("tb_ps3avataruri");

            tb_psvdlcuri = this.FindControl<BrowseableUriEntry>("tb_psvdlcuri");
            tb_pspdlcuri = this.FindControl<BrowseableUriEntry>("tb_pspdlcuri");
            tb_ps3dlcuri = this.FindControl<BrowseableUriEntry>("tb_ps3dlcuri");
            tb_ps4dlcuri = this.FindControl<BrowseableUriEntry>("tb_ps4dlcuri");

            tb_psvthmuri = this.FindControl<BrowseableUriEntry>("tb_psvthmuri");
            tb_pspthmuri = this.FindControl<BrowseableUriEntry>("tb_pspthmuri");
            tb_ps3thmuri = this.FindControl<BrowseableUriEntry>("tb_ps3thmuri");
            tb_ps4thmuri = this.FindControl<BrowseableUriEntry>("tb_ps4thmuri");

            tb_psvupduri = this.FindControl<BrowseableUriEntry>("tb_psvupduri");
            tb_ps4upduri = this.FindControl<BrowseableUriEntry>("tb_ps4upduri");

            textDownload = this.FindControl<BrowseableUriEntry>("textDownload");
            textPKGPath = this.FindControl<BrowseableUriEntry>("textPKGPath");
            textParams = this.FindControl<TextBox>("textParams");
            deleteAfterUnpack = this.FindControl<CheckBox>("deleteAfterUnpack");
            simultaneousDl = this.FindControl<NumericUpDown>("simultaneousDl");

            hmacTB = this.FindControl<TextBox>("hmacTB");
            tb_compPack = this.FindControl<TextBox>("tb_compPack");
            tb_compackPatch = this.FindControl<TextBox>("tb_compackPatch");

            chkbx_proxy = this.FindControl<CheckBox>("chkbx_proxy");
            tb_proxyServer = this.FindControl<TextBox>("tb_proxyServer");
            tb_proxyPort = this.FindControl<TextBox>("tb_proxyPort");
            lblCacheDate = this.FindControl<TextBlock>("lblCacheDate");
            btnSyncNow = this.FindControl<Button>("btnSyncNow");

            btnSyncNow.Command = ReactiveCommand.Create(btnSyncNow_Click);
            this.WhenAnyValue(x => x.simultaneousDl.Value)
                .Skip(1) // Skip initial value
                .Subscribe(_ => simultaneous_ValueChanged());

            this.WhenAnyValue(x => x.deleteAfterUnpack.IsChecked)
                .Skip(1) // Skip initial value
                .Subscribe(_ => deleteAfterUnpack_CheckedChanged());

            this.WhenAnyValue(x => x.chkbx_proxy.IsChecked)
                .Skip(1) // Skip initial value
                .Subscribe(_ => chkbx_proxy_CheckedChanged());
        }
    }
}