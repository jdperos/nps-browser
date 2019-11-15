using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Text;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NPS.Helpers;
using ReactiveUI;

namespace NPS
{
    public partial class CompPack : Window
    {
        public static bool compPackChanged = false;
        private readonly ObservableCollection<CompPackItem> _comboBoxItems = new ObservableCollection<CompPackItem>();

        private NpsBrowser mainForm;
        private Item item;
        private static List<CompPackItem> compPackList = null;

        public CompPack(NpsBrowser mainForm, Item item)
        {
            InitializeComponent();

            this.mainForm = mainForm;
            this.item = item;
        }

        public CompPack() : this(null, null)
        {
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            try
            {
                if (compPackList == null || compPackChanged)
                {
                    compPackChanged = false;
                    compPackList = LoadCompPacks(Settings.Instance.CompPackUrl);
                    //   Settings.Instance.compPackPatchUrl = "";
                    if (!string.IsNullOrEmpty(Settings.Instance.CompPackPatchUrl))
                        compPackList.AddRange(LoadCompPacks(Settings.Instance.CompPackPatchUrl));
                }

                var result = new List<CompPackItem>();
                foreach (var cp in compPackList)
                {
                    if (cp.titleId.Equals(item.TitleId))
                    {
                        result.Add(cp);
                        _comboBoxItems.Add(cp);
                    }
                }

                if (result.Count == 0)
                {
                    MessageBox.Show("No comp pack found");
                    this.Close();
                }
            }
            catch (Exception er)
            {
                MessageBox.Show(er.Message);
                this.Close();
            }
        }

        private List<CompPackItem> LoadCompPacks(string url)
        {
            List<CompPackItem> list = new List<CompPackItem>();
            WebClient wc = new WebClient();
            wc.Proxy = Settings.Instance.Proxy;
            wc.Encoding = Encoding.UTF8;
            string content = wc.DownloadString(new Uri(url));
            wc.Dispose();
            content = Encoding.UTF8.GetString(Encoding.Default.GetBytes(content));

            string[] lines = content.Split(new string[] {"\r\n", "\n\r", "\n", "\r"}, StringSplitOptions.None);
            foreach (string s in lines)
            {
                if (!string.IsNullOrEmpty(s))
                    list.Add(new CompPackItem(s));
            }

            return list;
        }

        private void button1_Click()
        {
            if (comboBox1.SelectedItem == null) return;

            List<Item> res = new List<Item>();

            CompPackItem cpi = (comboBox1.SelectedItem as CompPackItem);
            if (!cpi.ver.Equals("01.00"))
            {
                var cpiBase = (_comboBoxItems[0] as CompPackItem);
                if (cpiBase.ver.Equals("01.00"))
                {
                    res.Add(cpiBase.ToItem());
                }
            }

            res.Add(cpi.ToItem());


            this.Close(res.ToArray());
            //DownloadWorker dw = new DownloadWorker(itm, mainForm);
            //dw.Start();
        }

        private ComboBox comboBox1;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            comboBox1 = this.FindControl<ComboBox>("comboBox1");
            this.FindControl<Button>("button1").Command = ReactiveCommand.Create(button1_Click);

            comboBox1.Items = _comboBoxItems;
        }
    }

    internal class CompPackItem
    {
        public CompPackItem(string unparsedRow)
        {
            var t = unparsedRow.Split('=');
            this.url = t[0];
            this.title = t[1];
            t = t[0].Split('/');
            this.titleId = t[t.Length - 2];
            this.ver = t[t.Length - 1].Split('-')[2].Replace("_", "."); /*.Replace(".ppk", "")*/
            ;
        }

        public string titleId;
        public string ver;
        public string title;
        public string url;

        public override string ToString()
        {
            return "ver: " + this.ver + " " + this.title;
        }

        public Item ToItem()
        {
            Item i = new Item();
            i.ItsCompPack = true;
            i.TitleId = this.titleId;

            i.TitleName = this.title + " CompPack_" + this.ver;
            ;
            var urlArr = Settings.Instance.CompPackUrl.Split('/');

            string url = "";
            for (int c = 0; c < urlArr.Length - 1; c++)
            {
                url += urlArr[c] + "/";
            }

            url += this.url;
            //string url = Settings.Instance.compPackUrl.Replace("entries.txt", this.url);
            i.pkg = url;


            return i;
        }
    }
}