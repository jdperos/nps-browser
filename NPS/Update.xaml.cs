using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NPS.Helpers;
using ReactiveUI;

namespace NPS
{
    public class GamePatches : Window
    {
        private Item title;
        private Item newItem = null;

        public GamePatches(Item title)
        {
            InitializeComponent();
            this.title = title;
        }

        public GamePatches() : this(null)
        {

        }

        public async Task<Item> AskForUpdate(Window parent)
        {
            ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;

            try
            {
                var updateUrl = GetUpdateLink(title.TitleId);

                var wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                var content = await wc.DownloadStringTaskAsync(updateUrl);

                var ver = "";
                var pkgUrl = "";
                var contentId = "";

                var doc = new XmlDocument();
                doc.LoadXml(content);
                var packages = doc.DocumentElement.SelectNodes("/titlepatch/tag/package");

                var lastPackage = packages[packages.Count - 1];
                ver = lastPackage.Attributes["version"].Value;
                var sysVer = lastPackage.Attributes["psp2_system_ver"].Value;

                var changeinfo = lastPackage.SelectSingleNode("changeinfo");
                var changeInfoUrl = changeinfo.Attributes["url"].Value;

                var hybrid_package = lastPackage.SelectSingleNode("hybrid_package");

                if (hybrid_package != null)
                {
                    lastPackage = hybrid_package;
                }

                pkgUrl = lastPackage.Attributes["url"].Value;
                var size = lastPackage.Attributes["size"].Value;
                contentId = lastPackage.Attributes["content_id"].Value;

                var contentChangeset = wc.DownloadString(changeInfoUrl);

                doc.LoadXml(contentChangeset);
                var changesList = doc.DocumentElement.SelectNodes("/changeinfo/changes");

                var changesString = "";
                foreach (XmlNode itm in changesList)
                {
                    changesString += itm.Attributes["app_ver"].Value + "\n";
                    changesString += itm.InnerText + "\n";
                }

                button1.Content = "Download patch: " + ver;
                //byte[] bytes = Encoding.Default.GetBytes(changesString);
                //changesString = Encoding.UTF8.GetString(bytes);
                //webBrowser1.DocumentText = changesString;
                Changes.Text = changesString;

                newItem = new Item();

                newItem.ContentId = contentId + "_patch_" + ver;
                newItem.pkg = pkgUrl;
                newItem.TitleId = title.TitleId;
                newItem.Region = title.Region;
                newItem.TitleName = title.TitleName + " Patch " + ver;
                newItem.IsUpdate = true;

                sysVer = long.Parse(sysVer).ToString("X").Substring(0, 3).Insert(1, ".");

                button1.Content += $" (FW: {sysVer})";

                return await ShowDialog<Item>(parent);
            }
            catch (WebException error)
            {
                var response = (error.Response as HttpWebResponse);
                if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                    MessageBox.Show("No patches for title");
                else
                {
                    MessageBox.Show("Unknown error");
                    Console.WriteLine(error);
                }
                this.Close();
            }
            catch (Exception err)
            {
                MessageBox.Show("Unknown error");
                Console.WriteLine(err);
                this.Close();
            }

            return null;
        }

        public async Task<Item> DownloadUpdateNoAsk(Window parent)
        {
            ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;

            try
            {
                var updateUrl = GetUpdateLink(title.TitleId);

                var wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                var content = await wc.DownloadStringTaskAsync(updateUrl);
                if(content == "")
                {
                    Console.WriteLine($"No patch found for {title.TitleId}");
                    return null;
                }

                var ver = "";
                var pkgUrl = "";
                var contentId = "";

                var doc = new XmlDocument();
                doc.LoadXml(content);
                var packages = doc.DocumentElement.SelectNodes("/titlepatch/tag/package");

                var lastPackage = packages[packages.Count - 1];
                ver = lastPackage.Attributes["version"].Value;
                var sysVer = lastPackage.Attributes["psp2_system_ver"].Value;

                var changeinfo = lastPackage.SelectSingleNode("changeinfo");
                var changeInfoUrl = changeinfo.Attributes["url"].Value;

                var hybrid_package = lastPackage.SelectSingleNode("hybrid_package");

                if (hybrid_package != null)
                {
                    lastPackage = hybrid_package;
                }

                pkgUrl = lastPackage.Attributes["url"].Value;
                var size = lastPackage.Attributes["size"].Value;
                contentId = lastPackage.Attributes["content_id"].Value;

                newItem = new Item();

                newItem.ContentId = contentId + "_patch_" + ver;
                newItem.pkg = pkgUrl;
                newItem.TitleId = title.TitleId;
                newItem.Region = title.Region;
                newItem.TitleName = title.TitleName + " Patch " + ver;
                newItem.IsUpdate = true;

                return newItem;

            }
            catch (WebException error)
            {
                MessageBox.Show("Unknown error");
                Console.WriteLine(error);
                this.Close();
            }
            catch (Exception err)
            {
                MessageBox.Show("Unknown error");
                Console.WriteLine(err);
                this.Close();
            }

            return null;
        }


        private static string GetUpdateLink(string title)
        {
            var url = "https://gs-sec.ww.np.dl.playstation.net/pl/np/{0}/{1}/{0}-ver.xml";
            var key = "0x" + Settings.HmacKey;

            var binary = new List<byte>();
            for (var i = 2; i < key.Length; i += 2)
            {
                var s = new string(new[] {key[i], key[i + 1]});
                binary.Add(byte.Parse(s, NumberStyles.HexNumber));
            }

            var hmac = new HMACSHA256(binary.ToArray());
            var byte_hash = hmac.ComputeHash(Encoding.ASCII.GetBytes("np_" + title));

            var hash = "";
            foreach (var k in byte_hash)
                hash += k.ToString("X2");
            hash = hash.ToLower();

            return string.Format(url, title, hash);
        }

        private void button2_Click()
        {
            this.Close();
        }

        private void button1_Click()
        {
            if (newItem == null) MessageBox.Show("Unable to download. Some error occured");
            this.Close(newItem);
        }

        // ReSharper disable InconsistentNaming
        private Button button1;

        private Button button2;

        private TextBlock Changes;
        // ReSharper restore InconsistentNaming

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            button2 = this.FindControl<Button>("button2");
            button1 = this.FindControl<Button>("button1");
            Changes = this.FindControl<TextBlock>("Changes");

            button2.Command = ReactiveCommand.Create(button2_Click);
            button1.Command = ReactiveCommand.Create(button1_Click);
        }
    }
}