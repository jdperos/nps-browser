using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NPS
{
    [System.Serializable]
    public class Item : IEquatable<Item>
    {
        public string TitleId, Region, TitleName, zRif, pkg;
        public System.DateTime lastModifyDate = System.DateTime.MinValue;
        public int DLCs { get { return DlcItm.Count; } }
        public List<Item> DlcItm = new List<Item>();
        public string extension
        {
            get
            {
                if (this.ItsCompPack) return ".ppk";
                return ".pkg";
            }
        }
        public bool ItsPsx = false, ItsPsp = false, ItsPS3 = false, ItsPS4 = false, ItsCompPack = false;
        public bool IsAvatar = false, IsDLC = false, IsTheme = false, IsUpdate = false;
        public string ParentGameTitle = string.Empty;
        public string ContentId = null;
        public string offset = "";
        public string contentType = "";
        public string DownloadFileName
        {
            get
            {
                string res = "";
                if (this.ItsPS3 || this.ItsCompPack) res = TitleName;
                else if (string.IsNullOrEmpty(ContentId)) res = TitleId;
                else res = ContentId;

                if (!string.IsNullOrEmpty(offset)) res += "_" + offset;

                string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
                return r.Replace(res, "");
            }
        }

        public Item() { }



        public void CalculateDlCs(Item[] dlcDbs)
        {
            this.DlcItm = new List<Item>();
            foreach (Item i in dlcDbs)
            {
                if (i.Region == this.Region && i.TitleId.Contains(this.TitleId))
                {
                    this.DlcItm.Add(i);
                }
            }
        }

        public bool CompareName(string name)
        {
            name = name.ToLower();

            if (this.TitleId.ToLower().Contains(name)) return true;
            if (this.TitleName.ToLower().Contains(name)) return true;
            return false;
        }

        public bool Equals(Item other)
        {
            if (other == null) return false;

            //return this.TitleId == other.TitleId && this.Region == other.Region && this.TitleName == other.TitleName && this.zRif == other.zRif && this.pkg == other.pkg;
            return this.TitleId == other.TitleId && this.Region == other.Region && this.DownloadFileName == other.DownloadFileName;
        }


    }


}
