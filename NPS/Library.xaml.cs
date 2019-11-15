using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using NPS.Helpers;
using ReactiveUI;
using Image = System.Drawing.Image;

namespace NPS
{
    public partial class Library : Window
    {
        private List<Item> db;

        public Library(List<Item> db)
        {
            InitializeComponent();

            this.db = db;
        }

        public Library() : this(null)
        {
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            Refresh();
        }

        private void Refresh()
        {
            listView1.Items = null;

            label1.Text = Settings.Instance.DownloadDir;

            var apps = Array.Empty<string>();
            var dlcs = Array.Empty<string>();

            IEnumerable<string> files = Directory.GetFiles(Settings.Instance.DownloadDir, "*.pkg");

            var packageDir = Path.Combine(Settings.Instance.DownloadDir, "packages");
            if (Directory.Exists(packageDir))
            {
                files = files.Concat(Directory.GetFiles(packageDir, "*.pkg"));
            }

            var appDir = Path.Combine(Settings.Instance.DownloadDir, "app");
            if (Directory.Exists(appDir))
            {
                apps = Directory.GetDirectories(appDir);
            }

            var addContDir = Path.Combine(Settings.Instance.DownloadDir, "addcont");
            if (Directory.Exists(addContDir))
            {
                dlcs = Directory.GetDirectories(addContDir);
            }

            var imagesToLoad = new List<string>();

            var items = new List<LibraryItem>();

            foreach (var s in files)
            {
                var f = Path.GetFileNameWithoutExtension(s);

                var found = false;
                foreach (var itm in db)
                {
                    if (!f.Equals(itm.DownloadFileName))
                    {
                        continue;
                    }

                    var library = new LibraryItem
                    {
                        Text = itm.TitleName + " (PKG)",
                        itm = itm,
                        path = s,
                        isPkg = true
                    };

                    items.Add(library);

                    foreach (var r in NPCache.I.renasceneCache)
                    {
                        if (!itm.Equals(r.itm))
                        {
                            continue;
                        }

                        imagesToLoad.Add(r.imgUrl);
                        // TODO: This
                        //lvi.ImageKey = r.imgUrl;
                        break;
                    }

                    found = true;
                    break;
                }

                if (!found)
                {
                    var library = new LibraryItem
                    {
                        Text = f + " (UNKNOWN PKG)",
                        path = s,
                        isPkg = true
                    };

                    items.Add(library);
                }
            }

            foreach (var s in apps)
            {
                var d = Path.GetFullPath(s).TrimEnd(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar)
                    .Last();

                var found = false;
                foreach (var itm in db)
                {
                    if (itm.IsDLC)
                    {
                        continue;
                    }

                    if (!itm.TitleId.Equals(d))
                    {
                        continue;
                    }

                    foreach (var r in NPCache.I.renasceneCache)
                    {
                        if (itm.Equals(r.itm))
                        {
                            imagesToLoad.Add(r.imgUrl);
                            // TODO: This
                            //lvi.ImageKey = r.imgUrl;
                            break;
                        }
                    }

                    var library = new LibraryItem
                    {
                        itm = itm,
                        path = s,
                        isPkg = false,
                        Text = itm.TitleName
                    };

                    items.Add(library);

                    found = true;
                    break;
                }

                if (!found)
                {
                    var library = new LibraryItem
                    {
                        path = s,
                        isPkg = false,
                        Text = d + " UNKNOWN"
                    };

                    items.Add(library);
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
                    wc.Proxy = Settings.Instance.Proxy;
                    wc.Encoding = Encoding.UTF8;
                    var img = wc.DownloadData(url);
                    using (var ms = new MemoryStream(img))
                    {
                        Image image = Image.FromStream(ms);
                        image = getThumb(image);
                        //Dispatcher.UIThread.Post((new Action(() => { imageList1.Images.Add(url, image); })));
                    }
                }
            });

            listView1.Items = items;
        }

        public Bitmap getThumb(Image image)
        {
            int tw, th, tx, ty;
            int w = image.Width;
            int h = image.Height;
            double whRatio = (double) w / h;

            if (image.Width >= image.Height)
            {
                tw = 100;
                th = (int) (tw / whRatio);
            }
            else
            {
                th = 100;
                tw = (int) (th * whRatio);
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

        private void button1_Click()
        {
            if (listView1.SelectedItems.Count == 0) return;
            var path = ((LibraryItem) listView1.SelectedItems[0]).path;
            System.Diagnostics.Process.Start("explorer.exe", "/select, " + path);
        }

        private void button2_Click()
        {
            if (listView1.SelectedItems.Count == 0) return;
            var itm = (LibraryItem) listView1.SelectedItems[0];

            try
            {
                if (itm.isPkg)
                    File.Delete(itm.path);
                else Directory.Delete(itm.path, true);

                Refresh();
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void button3_Click()
        {
            if (listView1.SelectedItems.Count == 0) return;
            var itm = (LibraryItem) listView1.SelectedItems[0];
            if (itm.isPkg == false) return;
            if (itm.itm == null)
            {
                MessageBox.Show("Can't unpack unknown pkg");
                return;
            }

            if (itm.itm.ItsPS3 && itm.path.ToLower().Contains("packages"))
                File.Move(itm.path,
                    Settings.Instance.DownloadDir + Path.DirectorySeparatorChar + Path.GetFileName(itm.path));

            var dw = new DownloadWorker(itm.itm);
            dw.Start();
        }

        private void listView1_SelectedIndexChanged()
        {
            if (listView1.SelectedItems.Count == 0) return;
            var itm = (LibraryItem) listView1.SelectedItems[0];
            button3.IsEnabled = itm.isPkg;
        }

        private void button4_Click()
        {
            Refresh();
        }

        // ReSharper disable InconsistentNaming
        private Button button1;
        private Button button2;
        private Button button3;
        private Button button4;

        private DataGrid listView1;
        private TextBlock label1;

        // ReSharper restore InconsistentNaming

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            button1 = this.FindControl<Button>("button1");
            button2 = this.FindControl<Button>("button2");
            button3 = this.FindControl<Button>("button3");
            button4 = this.FindControl<Button>("button4");

            listView1 = this.FindControl<DataGrid>("listView1");
            label1 = this.FindControl<TextBlock>("label1");

            button1.Command = ReactiveCommand.Create(button1_Click);
            button2.Command = ReactiveCommand.Create(button2_Click);
            button3.Command = ReactiveCommand.Create(button3_Click);
            button4.Command = ReactiveCommand.Create(button4_Click);

            this.WhenAnyValue(x => x.listView1.SelectedIndex)
                .Subscribe(_ => listView1_SelectedIndexChanged());
        }
    }

    public class LibraryItem
    {
        public string Text { get; set; }
        public Item itm;
        public bool isPkg = false;
        public string path;
    }
}