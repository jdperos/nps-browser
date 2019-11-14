/*
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace NPS
{
    public partial class GamePatches : Form
    {
        private Item title;
        private Action<Item> result;
        private Item newItem = null;

        public GamePatches(Item title, Action<Item> result)
        {
            InitializeComponent();
            this.title = title;
            this.result = result;
        }

        public void AskForUpdate()
        {
            ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;

            try
            {
                string updateUrl = GetUpdateLink(title.TitleId);

                WebClient wc = new WebClient();
                wc.Encoding = System.Text.Encoding.UTF8;
                string content = wc.DownloadString(updateUrl);

                string ver = "";
                string pkgUrl = "";
                string contentId = "";

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(content);
                var packages = doc.DocumentElement.SelectNodes("/titlepatch/tag/package");

                var lastPackage = packages[packages.Count - 1];
                ver = lastPackage.Attributes["version"].Value;
                string sysVer = lastPackage.Attributes["psp2_system_ver"].Value;

                var changeinfo = lastPackage.SelectSingleNode("changeinfo");
                string changeInfoUrl = changeinfo.Attributes["url"].Value;

                var hybrid_package = lastPackage.SelectSingleNode("hybrid_package");

                if (hybrid_package != null)
                {
                    lastPackage = hybrid_package;
                }

                pkgUrl = lastPackage.Attributes["url"].Value;
                string size = lastPackage.Attributes["size"].Value;
                contentId = lastPackage.Attributes["content_id"].Value;

                string contentChangeset = wc.DownloadString(changeInfoUrl);

                doc.LoadXml(contentChangeset);
                var changesList = doc.DocumentElement.SelectNodes("/changeinfo/changes");

                string changesString = "";
                foreach (XmlNode itm in changesList)
                {
                    changesString += itm.Attributes["app_ver"].Value + "</br>";
                    changesString += itm.InnerText + "</br>";
                }

                this.Show();
                button1.Text = "Download patch: " + ver;
                //byte[] bytes = Encoding.Default.GetBytes(changesString);
                //changesString = Encoding.UTF8.GetString(bytes);
                webBrowser1.DocumentText = changesString;


                newItem = new Item();

                newItem.ContentId = contentId + "_patch_" + ver;
                newItem.pkg = pkgUrl;
                newItem.TitleId = title.TitleId;
                newItem.Region = title.Region;
                newItem.TitleName = title.TitleName + " Patch " + ver;
                newItem.IsUpdate = true;

                sysVer = long.Parse(sysVer).ToString("X").Substring(0, 3).Insert(1, ".");

                button1.Text += $" (FW: {sysVer})";



            }
            catch (WebException error)
            {
                var response = (error.Response as HttpWebResponse);
                if (response != null && response.StatusCode == HttpStatusCode.NotFound) MessageBox.Show("No patches for title");
                else MessageBox.Show("Unknown error");
                this.Close();
            }
            catch (Exception err)
            {
                MessageBox.Show("Unknown error");
                this.Close();
            }
        }


        private static string GetUpdateLink(string title)
        {
            string url = "https://gs-sec.ww.np.dl.playstation.net/pl/np/{0}/{1}/{0}-ver.xml";
            string key = "0x" + Settings.Instance.HmacKey;

            var binary = new List<byte>();
            for (int i = 2; i < key.Length; i += 2)
            {
                string s = new string(new[] { key[i], key[i + 1] });
                binary.Add(byte.Parse(s, NumberStyles.HexNumber));
            }

            var hmac = new HMACSHA256(binary.ToArray());
            var byte_hash = hmac.ComputeHash(Encoding.ASCII.GetBytes("np_" + title));

            string hash = "";
            foreach (var k in byte_hash)
                hash += k.ToString("X2");
            hash = hash.ToLower();

            return string.Format(url, title, hash);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (newItem == null) MessageBox.Show("Unable to download. Some error occured");
            else result.Invoke(newItem);
            this.Close();
        }
    }
}
*/
