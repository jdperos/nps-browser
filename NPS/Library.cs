using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NPS
{
    public partial class Library : Form
    {

        List<Item> db;

        public Library(List<Item> db)
        {
            InitializeComponent();
            this.db = db;
        }

        private void Library_Load(object sender, EventArgs e)
        {
            listView1.Items.Clear();

            label1.Text = Settings.Instance.downloadDir;

            string[] apps = new string[0];
            string[] dlcs = new string[0];
            string[] files = Directory.GetFiles(Settings.Instance.downloadDir, "*.pkg");

            if (Directory.Exists(Settings.Instance.downloadDir + "\\packages"))
            {
                var lst = files.ToList();
                lst.AddRange(Directory.GetFiles(Settings.Instance.downloadDir + "\\packages", "*.pkg"));
                files = lst.ToArray();
            }

            if (Directory.Exists(Settings.Instance.downloadDir + "\\app"))
            {
                apps = Directory.GetDirectories(Settings.Instance.downloadDir + "\\app");
            }
            if (Directory.Exists(Settings.Instance.downloadDir + "\\addcont"))
            {
                dlcs = Directory.GetDirectories(Settings.Instance.downloadDir + "\\addcont");
            }

            List<string> imagesToLoad = new List<string>();

            foreach (string s in files)
            {
                var f = Path.GetFileNameWithoutExtension(s);

                bool found = false;
                foreach (var itm in db)
                {
                    if (f.Equals(itm.DownloadFileName))
                    {
                        ListViewItem lvi = new ListViewItem(itm.TitleName + " (PKG)");

                        listView1.Items.Add(lvi);

                        foreach (var r in NPCache.I.renasceneCache)
                            if (itm.Equals(r.itm))
                            {
                                imagesToLoad.Add(r.imgUrl);
                                lvi.ImageKey = r.imgUrl;
                                break;
                            }
                        LibraryItem library = new LibraryItem();
                        library.itm = itm;
                        library.path = s;
                        library.isPkg = true;
                        lvi.Tag = library;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    ListViewItem lvi = new ListViewItem(f + " (UNKNOWN PKG)");

                    listView1.Items.Add(lvi);

                    LibraryItem library = new LibraryItem();
                    library.path = s;
                    library.isPkg = true;
                    lvi.Tag = library;
                }
            }

            foreach (string s in apps)
            {
                string d = Path.GetFullPath(s).TrimEnd(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar).Last();

                bool found = false;
                foreach (var itm in db)
                {
                    if (!itm.IsDLC)
                        if (itm.TitleId.Equals(d))
                        {
                            ListViewItem lvi = new ListViewItem(itm.TitleName);

                            listView1.Items.Add(lvi);

                            foreach (var r in NPCache.I.renasceneCache)
                                if (itm.Equals(r.itm))
                                {
                                    imagesToLoad.Add(r.imgUrl);
                                    lvi.ImageKey = r.imgUrl;
                                    break;
                                }
                            LibraryItem library = new LibraryItem();
                            library.itm = itm;
                            library.path = s;
                            library.isPkg = false;
                            lvi.Tag = library;
                            found = true;
                            break;
                        }
                }

                if (!found)
                {
                    ListViewItem lvi = new ListViewItem(d + " UNKNOWN");

                    listView1.Items.Add(lvi);

                    LibraryItem library = new LibraryItem();
                    library.path = s;
                    library.isPkg = false;
                    lvi.Tag = library;
                }
            }



            //foreach (string s in dlcs)
            //{
            //    string d = Path.GetFullPath(s).TrimEnd(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar).Last();
            //    foreach (var itm in db)
            //    {
            //        if (itm.IsDLC && itm.TitleId.Equals(d))
            //        {
            //            ListViewItem lvi = new ListViewItem(itm.TitleName);

            //            listView1.Items.Add(lvi);

            //            foreach (var r in NPCache.I.renasceneCache)
            //                if (itm == r.itm)
            //                {
            //                    imagesToLoad.Add(r.imgUrl);
            //                    lvi.ImageKey = r.imgUrl;
            //    break;
            //                }
            //            LibraryItem library = new LibraryItem();
            //            library.itm = itm;
            //            library.patch = s;
            //            library.isPkg = false;
            //            lvi.Tag = library;
            //break;
            //        }
            //    }
            //}


            Task.Run(() =>
            {
                foreach (string url in imagesToLoad)
                {
                    WebClient wc = new WebClient();
                    wc.Proxy = Settings.Instance.proxy;
                    wc.Encoding = Encoding.UTF8;
                    var img = wc.DownloadData(url);
                    using (var ms = new MemoryStream(img))
                    {
                        Image image = Image.FromStream(ms);
                        image = getThumb(image);
                        Invoke(new Action(() =>
                        {
                            imageList1.Images.Add(url, image);
                        }));
                    }
                }


            });


        }

        public Bitmap getThumb(Image image)
        {
            int tw, th, tx, ty;
            int w = image.Width;
            int h = image.Height;
            double whRatio = (double)w / h;

            if (image.Width >= image.Height)
            {
                tw = 100;
                th = (int)(tw / whRatio);
            }
            else
            {
                th = 100;
                tw = (int)(th * whRatio);
            }
            tx = (100 - tw) / 2;
            ty = (100 - th) / 2;
            Bitmap thumb = new Bitmap(100, 100, PixelFormat.Format24bppRgb);
            Graphics g = Graphics.FromImage(thumb);
            g.Clear(Color.White);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.DrawImage(image,
            new Rectangle(tx, ty, tw, th),
            new Rectangle(0, 0, w, h),
            GraphicsUnit.Pixel);
            return thumb;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            string path = (listView1.SelectedItems[0].Tag as LibraryItem).path;
            System.Diagnostics.Process.Start("explorer.exe", "/select, " + path);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            var itm = (listView1.SelectedItems[0].Tag as LibraryItem); ;

            try
            {
                if (itm.isPkg)
                    File.Delete(itm.path);
                else Directory.Delete(itm.path, true);

                Library_Load(null, null);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            var itm = (listView1.SelectedItems[0].Tag as LibraryItem);
            if (itm.isPkg == false) return;
            if (itm.itm == null)
            {
                MessageBox.Show("Can't unpack unknown pkg");
                return;
            }

            if (itm.itm.ItsPS3 && itm.path.ToLower().Contains("packages")) File.Move(itm.path, Settings.Instance.downloadDir + Path.DirectorySeparatorChar + Path.GetFileName(itm.path));

            DownloadWorker dw = new DownloadWorker(itm.itm, this);
            dw.Start();

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            var itm = (listView1.SelectedItems[0].Tag as LibraryItem);
            button3.Enabled = itm.isPkg;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Library_Load(null, null);
        }
    }

    class LibraryItem
    {
        public Item itm;
        public bool isPkg = false;
        public string path;
    }
}
