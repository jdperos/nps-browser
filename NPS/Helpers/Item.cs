using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace NPS
{
    [Serializable]
    public class Item : IEquatable<Item>
    {
        public string TitleId;
        public string Region;
        public string TitleName;
        public string zRif;
        public string pkg;

        public DateTime lastModifyDate = DateTime.MinValue;

        public int DLCs => DlcItm?.Count ?? 0;
        [NonSerialized] [CanBeNull] public List<Item> DlcItm;

        public string extension => ItsCompPack ? ".ppk" : ".pkg";

        public bool ItsPsx = false;
        public bool ItsPsp = false;
        public bool ItsPS3 = false;
        public bool ItsPS4 = false;

        public bool ItsCompPack = false;
        public bool IsAvatar = false;
        public bool IsDLC = false;
        public bool IsTheme = false;
        public bool IsUpdate = false;

        public string ParentGameTitle = string.Empty;
        public string ContentId = null;
        public string offset = "";
        public string contentType = "";

        public string DownloadFileName
        {
            get
            {
                string res = "";
                if (ItsPS3 || ItsCompPack) res = TitleName;
                else if (string.IsNullOrEmpty(ContentId)) res = TitleId;
                else res = ContentId;

                if (!string.IsNullOrEmpty(offset)) res += "_" + offset;

                string regexSearch =
                    new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
                return r.Replace(res, "");
            }
        }

        public bool CompareName(string name)
        {
            if (TitleId.Contains(name, StringComparison.OrdinalIgnoreCase)) return true;
            if (TitleName.Contains(name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public bool Equals(Item other)
        {
            if (other == null) return false;

            //return this.TitleId == other.TitleId && this.Region == other.Region && this.TitleName == other.TitleName && this.zRif == other.zRif && this.pkg == other.pkg;
            return TitleId == other.TitleId && Region == other.Region && DownloadFileName == other.DownloadFileName;
        }
    }
}