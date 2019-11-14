/*
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
        private string contentId;
        private string region;
        private ListView lst;

        private bool isLoading = false;
        private string currentContentId = "";

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
        wc.Proxy = Settings.Instance.Proxy;
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


}
*/
