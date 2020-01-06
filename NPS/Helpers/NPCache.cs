using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace NPS.Helpers
{
    [System.Serializable]
    public class NPCache
    {
        private const string Path = "nps.cache";

        public static NPCache I
        {
            get
            {
                if (_i == null)
                {
                    Load();
                }

                return _i;
            }
        }

        private bool _cacheInvalid;

        public bool IsCacheIsInvalid => _cacheInvalid || UpdateDate > System.DateTime.Now.AddDays(-4);

        private static NPCache _i;

        public System.DateTime UpdateDate;
        public List<Item> localDatabase = new List<Item>();
        public List<Renascene> renasceneCache = new List<Renascene>();

        public static void Load()
        {
            try
            {
                using var stream = File.OpenRead(Path);
                var formatter = new BinaryFormatter();
                _i = (NPCache) formatter.Deserialize(stream);
                _i.renasceneCache ??= new List<Renascene>();
                return;
            }
            catch (SerializationException)
            {
                // Nada.
            }

            _i = new NPCache(System.DateTime.MinValue);
        }

        public void InvalidateCache()
        {
            _cacheInvalid = true;
        }

        public void Save(System.DateTime updateDate)
        {
            UpdateDate = updateDate;
            Save();
        }

        public void Save()
        {
            using var fileStream = File.Create(Path);
            var formatter = new BinaryFormatter();
            formatter.Serialize(fileStream, this);
        }

        public NPCache(System.DateTime creationDate)
        {
            UpdateDate = creationDate;
        }
    }
}