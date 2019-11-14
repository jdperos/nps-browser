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
    public partial class Desc : Form
    {
        string contentId;
        string region;
        ListView lst;

        bool isLoading = false;
        string currentContentId = "";

        public Desc(ListView lst)
        {
            InitializeComponent();
            this.lst = lst;

        }


        private void ShowDescription(string contentId, string region)
        {
            pb_status.Visible = true;
            pb_status.Image = new Bitmap(Properties.Resources.menu_reload);

            switch (region)
            {
                case "EU": region = "GB/en"; break;
                case "US": region = "CA/en"; break;
                case "JP": region = "JP/ja"; break;
                case "ASIA": region = "JP/ja"; break;
            }

            Task.Run(() =>
{

    try
    {
        pictureBox1.Image = null;
        pictureBox2.Image = null;
        pictureBox3.Image = null;
        this.Invoke(new Action(() =>
        {
            label1.Text = "";
            richTextBox1.Text = "";

            if (contentId == null || contentId.ToLower().Equals("missing"))
            {
                isLoading = false;
                pb_status.Image = new Bitmap(Properties.Resources.menu_cancel);
                return;
            }
        }));

        WebClient wc = new WebClient();
        wc.Proxy = Settings.Instance.proxy;
        wc.Encoding = System.Text.Encoding.UTF8;
        string content = wc.DownloadString(new Uri("https://store.playstation.com/chihiro-api/viewfinder/" + region + "/19/" + contentId));
        wc.Dispose();
        //content = Encoding.UTF8.GetString(Encoding.Default.GetBytes(content));

        var contentJson = SimpleJson.SimpleJson.DeserializeObject<PSNJson>(content);
        pictureBox1.ImageLocation = contentJson.cover;
        pictureBox2.ImageLocation = contentJson.picture1;
        pictureBox3.ImageLocation = contentJson.picture2;
        this.Invoke(new Action(() =>
        {
            pb_status.Visible = false;
            richTextBox1.Text = contentJson.desc;
            label1.Text = contentJson.title_name + " (rating: " + contentJson.Stars + "/5.00)";
        }));
        isLoading = false;
    }
    catch (Exception err)
    {
        isLoading = false;
        this.Invoke(new Action(() =>
        {
            pb_status.Visible = true;
            pb_status.Image = new Bitmap(Properties.Resources.menu_cancel);
        }));
    }
});
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (isLoading) return;

            if (lst.SelectedItems.Count > 0)
            {
                var itm = (lst.SelectedItems[0].Tag as Item);
                if (itm.ContentId == currentContentId) return;

                isLoading = true;
                currentContentId = itm.ContentId;
                ShowDescription(itm.ContentId, itm.Region);
            }
        }

        private void pictureClicked(object sender, EventArgs e)
        {
            var a = (sender as PictureBox);
            if (a.Tag == null)
            {

                a.Tag = a.Location;
                a.Location = new Point(0, 0);
                a.Size = this.Size;

                foreach (Control c in this.Controls)
                {
                    if (a != c) c.Visible = false;
                }
            }
            else
            {
                a.Location = (a.Tag as Point?).Value;
                a.Tag = null;
                a.Size = new Size(280, 129);

                foreach (Control c in this.Controls)
                {
                    if (a != c && c!=pb_status) c.Visible = true;
                }
            }
        }

        private void Desc_Load(object sender, EventArgs e)
        {

        }
    }

    class PSNJson
    {
        public string cover
        {
            get
            {
                string r = null;
                if (images.Length > 0) r = images[0].url;
                return r;
            }
        }


        public string picture1
        {
            get
            {
                string r = null;
                if (promomedia.Length > 0)
                    if (promomedia[0].materials.Length > 0)
                        if (promomedia[0].materials[0].urls.Length > 0) r = promomedia[0].materials[0].urls[0].url;
                return r;
            }
        }
        public string picture2
        {
            get
            {
                string r = null;
                if (promomedia.Length > 0)
                    if (promomedia[0].materials.Length > 1)
                        if (promomedia[0].materials[1].urls.Length > 0) r = promomedia[0].materials[1].urls[0].url;
                return r;
            }
        }

        public string desc
        {
            get
            {
                return long_desc.Replace("<br>", Environment.NewLine);
            }
        }

        public string Stars
        {
            get { return star_rating.score; }
        }

        public NPSImage[] images;
        public string long_desc;
        public Promomedia[] promomedia;
        public Star star_rating;
        public string title_name;

    }
    class NPSImage
    {
        public int type;
        public string url;
    }
    class Promomedia
    {
        public Material[] materials;
    }
    class Material
    {
        public NPSImage[] urls;
    }

    class Star
    {
        public string score;
    }
}
