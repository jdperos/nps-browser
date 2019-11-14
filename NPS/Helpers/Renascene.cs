using System;
using System.Drawing;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace NPS.Helpers
{
    [System.Serializable]
    public class Renascene
    {
        public string imgUrl, genre, language, publish, developer, size, url;
        //public Image image;
        public Item itm;

        public Renascene(Item itm)
        {
            this.itm = itm;
            try
            {
                string titleId = SafeTitle(itm.TitleId);

                WebClient wc = new WebClient();
                wc.Encoding = System.Text.Encoding.UTF8;
                wc.Proxy = Settings.Instance.Proxy;

                var region = "";
                switch (itm.Region)
                {
                    case "EU": region = "GB/en"; break;
                    case "US": region = "CA/en"; break;
                    case "JP": region = "JP/ja"; break;
                    case "ASIA": region = "HK/en"; break;
                }

                

                var content = "";

                try
                {
                    content = wc.DownloadString(new Uri("https://store.playstation.com/chihiro-api/viewfinder/" + region + "/19/" + itm.ContentId));

                    var contentJson = JsonConvert.DeserializeObject<PSNJson>(content);
                    this.imgUrl = contentJson.cover;
                }
                catch
                {
                    this.imgUrl = null;
                }

                //if (itm.ItsPsx)
                //{
                //    content = wc.DownloadString(@"http://renascene.com/ps1/?target=search&srch=" + titleId + "&srchser=1");
                //    url = ExtractString(content, "<td class=\"l\">&nbsp; <a href=\"", "\">");
                //}
                //else if (itm.ItsPS3)
                //{
                //    content = wc.DownloadString(@"http://renascene.com/ps3/?target=search&srch=" + titleId + "&srchname=1&srchser=1&srchfold=1&srchfname=1");
                //    url = ExtractString(content, "<td></td><td><a href=\"", "\">");
                //}
                //else if (itm.ItsPS4)
                //{
                //    content = wc.DownloadString(@"http://renascene.com/ps4/?target=search&srch=" + titleId + "&srchname=1&srchser=1&srchfold=1&srchfname=1");
                //    url = ExtractString(content, "<td></td><td><a href=\"", "\">");
                //}
                //else if (itm.ItsPsp)
                //{
                //    content = wc.DownloadString(@"http://renascene.com/?target=search1&srch=" + titleId + "&srchser=1");
                //    url = ExtractString(content, "<tr class=\"defRows \" onclick=\"window.location.href='", "';\" >");
                //}
                //else
                //{
                //    content = wc.DownloadString(@"http://renascene.com/psv/?target=search&srch=" + titleId + "&srchser=1");
                //    url = ExtractString(content, "<td class=\"l\"><a href=\"", "\">");
                //}

                //content = wc.DownloadString(url);

                //this.imgUrl = ExtractString(content, "<td width=\"300pt\" style=\"vertical-align: top; padding: 0 0 0 5px;\">", "</td>");
                //this.imgUrl = ExtractString(imgUrl, "<img src=", ">");

                //if (!itm.ItsPS3 && !itm.ItsPS4)
                //{
                //    genre = ExtractString(content, "<td class=\"infLeftTd\">Genre</td>", "</tr>");
                //    genre = ExtractString(genre, "<td class=\"infRightTd\">", "</td>");
                //    genre = genre.Replace("Ã‚Â»", "/");


                //    language = ExtractString(content, "<td class=\"infLeftTd\">Language</td>", "</tr>");
                //    language = ExtractString(language, "<td class=\"infRightTd\">", "</td>");
                //}
                //if (!(itm.ItsPsx || itm.ItsPsp || itm.ItsPS3 || itm.ItsPS4))
                //{
                //    publish = ExtractString(content, "<td class=\"infLeftTd\">Publish Date</td>", "</tr>");
                //    publish = ExtractString(publish, "<td class=\"infRightTd\">", "</td>");

                //    developer = ExtractString(content, "<td class=\"infLeftTd\">Developer</td>", "</tr>");
                //    developer = ExtractString(developer, "<td class=\"infRightTd\">", "</td>");
                //}
            }
            catch
            {
                imgUrl = genre = language = publish = developer = size = null;
            }

            try
            {
                var webRequest = HttpWebRequest.Create(itm.pkg);
                webRequest.Proxy = Settings.Instance.Proxy;
                webRequest.Method = "HEAD";

                using (var webResponse = webRequest.GetResponse())
                {
                    var fileSize = webResponse.Headers.Get("Content-Length");
                    var fileSizeInMegaByte = Math.Round(Convert.ToDouble(fileSize) / 1024.0 / 1024.0, 2);
                    this.size = fileSizeInMegaByte + " MB";
                }
            }
            catch { }

            //try
            //{
            //    if (!string.IsNullOrEmpty(this.imgUrl))
            //    {
            //        WebClient wc = new WebClient();
            //        wc.Proxy = Settings.Instance.proxy;
            //        var img = wc.DownloadData(this.imgUrl);
            //        using (var ms = new MemoryStream(img))
            //        {
            //            image = Image.FromStream(ms);
            //        }
            //    }
            //}
            //catch { }


        }



        public override string ToString()
        {
            return string.Format(@"Size: {4}
Genre: {0}
Language: {1}
Published: {2}
Developer: {3}", this.genre, this.language, this.publish, this.developer, this.size);
        }


        private string SafeTitle(string title)
        {
            return title.Replace("(DLC)", "").Replace(" ", "");
        }

        private string ExtractString(string s, string start, string end)
        {
            int startIndex = s.IndexOf(start) + start.Length;
            int endIndex = s.IndexOf(end, startIndex);
            return s.Substring(startIndex, endIndex - startIndex);
        }
    }
}
