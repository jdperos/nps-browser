using System;

namespace NPS
{
    internal class PSNJson
    {
        public string cover => images.Length > 0 ? images[0].url : null;


        public string picture1
        {
            get
            {
                if (promomedia.Length <= 0)
                {
                    return null;
                }

                if (promomedia[0].materials.Length <= 0)
                {
                    return null;
                }

                if (promomedia[0].materials[0].urls != null && promomedia[0].materials[0].urls.Length > 0)
                {
                    return promomedia[0].materials[0].urls[0].url;
                }

                return null;
            }
        }
        public string picture2
        {
            get
            {
                if (promomedia.Length <= 0)
                {
                    return null;
                }

                if (promomedia[0].materials.Length <= 1)
                {
                    return null;
                }

                if (promomedia[0].materials[1].urls != null && promomedia[0].materials[1].urls.Length > 0)
                {
                    return promomedia[0].materials[1].urls[0].url;
                }

                return null;
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

    internal class NPSImage
    {
        public int type;
        public string url;
    }

    internal class Promomedia
    {
        public Material[] materials;
    }

    internal class Material
    {
        public NPSImage[] urls;
    }

    internal class Star
    {
        public string score;
    }
}