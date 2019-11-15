using NPS.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[System.Serializable]
public class NPCache
{

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

    private bool _cacheInvalid = false;

    public bool IsCacheIsInvalid { get { return _cacheInvalid || this.UpdateDate > System.DateTime.Now.AddDays(-4); } }

    private static NPCache _i;
    public const int ver = 1;
    public System.DateTime UpdateDate;
    public List<NPS.Item> localDatabase = new List<NPS.Item>();
    public List<string> regions = new List<string>(), types = new List<string>();
    public List<Renascene> renasceneCache = new List<Renascene>();
    private static string path = "nps.cache";

    public static void Load()
    {
        if (System.IO.File.Exists(path))
        {
            var stream = File.OpenRead(path);
            var formatter = new BinaryFormatter();
            _i = (NPCache)formatter.Deserialize(stream);
            if (_i.renasceneCache == null) _i.renasceneCache = new List<Renascene>();
            stream.Close();
        }
        else _i = new NPCache(System.DateTime.MinValue);
    }

    public void InvalidateCache()
    {
        _cacheInvalid = true;
    }

    public void Save(System.DateTime updateDate)
    {
        this.UpdateDate = updateDate;
        Save();
    }
    public void Save()
    {
        using var stream = File.Create(path);
        var formatter = new BinaryFormatter();
        formatter.Serialize(stream, this);
    }

    public NPCache(System.DateTime creationDate)
    {
        this.UpdateDate = creationDate;
    }


}




