using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;

[System.Serializable]
public class Settings
{

    static Settings _i;
    static string path = "npsSettings.dat";

    // Settings
    public string downloadDir, pkgPath, pkgParams = "-x {pkgFile} \"{zRifKey}\"";
    public bool deleteAfterUnpack = false;
    public int simultaneousDl = 2;

    // Game URIs
    public string PSVUri, PSMUri, PSXUri, PSPUri, PS3Uri, PS4Uri;

    // Avatar URIs
    public string PS3AvatarUri;

    // DLC URIs
    public string PSVDLCUri, PSPDLCUri, PS3DLCUri, PS4DLCUri;

    // Theme URIs
    public string PSVThemeUri, PSPThemeUri, PS3ThemeUri, PS4ThemeUri;

    public string HMACKey = "";
    // Update URIs
    public string PSVUpdateUri, PS4UpdateUri;
    public List<string> selectedRegions = new List<string>(), selectedTypes = new List<string>();

    public WebProxy proxy;
    public History history = new History();
    public string compPackUrl = null, compPackPatchUrl = null;

    public static Settings Instance
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

    public static void Load()
    {
        if (System.IO.File.Exists(path))
        {
            var stream = File.OpenRead(path);
            var formatter = new BinaryFormatter();
            _i = (Settings)formatter.Deserialize(stream);
            stream.Close();
        }
        else
        {
            _i = ImportOldSettings();
            if (File.Exists("history.dat"))
            {
                _i.history = ImportOldHistory();
                File.Delete("history.dat");
            }
        }
    }

    public void Save()
    {
        FileStream stream = File.Create(path);
        var formatter = new BinaryFormatter();
        formatter.Serialize(stream, this);
        stream.Close();
    }

    static History ImportOldHistory()
    {
        var stream = File.OpenRead("history.dat");
        var formatter = new BinaryFormatter();
        History h = (History)formatter.Deserialize(stream);
        stream.Close();
        return h;
    }

    static Settings ImportOldSettings()
    {
        Settings s = new Settings();

        if (System.Type.GetType("Mono.Runtime") == null)
        {
            string keyName = Path.Combine("HKEY_CURRENT_USER", "SOFTWARE", "NoPayStationBrowser");

            s.downloadDir = Registry.GetValue(keyName, "downloadDir", "")?.ToString();
            s.pkgPath = Registry.GetValue(keyName, "pkgPath", "")?.ToString();
            s.pkgParams = Registry.GetValue(keyName, "pkgParams", null)?.ToString();
            if (s.pkgParams == null) s.pkgParams = "-x {pkgFile} \"{zRifKey}\"";
            string deleteAfterUnpackString = Registry.GetValue(keyName, "deleteAfterUnpack", false)?.ToString();
            if (!string.IsNullOrEmpty(deleteAfterUnpackString))
                bool.TryParse(deleteAfterUnpackString, out s.deleteAfterUnpack);
            else s.deleteAfterUnpack = true;
            string simultanesulString = Registry.GetValue(keyName, "simultaneousDl", 2)?.ToString();
            if (!string.IsNullOrEmpty(simultanesulString))
                int.TryParse(simultanesulString, out s.simultaneousDl);
            else s.simultaneousDl = 2;
            s.PSVUri = Registry.GetValue(keyName, "GamesUri", "")?.ToString();
            s.PSMUri = Registry.GetValue(keyName, "PSMUri", "")?.ToString();
            s.PSXUri = Registry.GetValue(keyName, "PSXUri", "")?.ToString();
            s.PSPUri = Registry.GetValue(keyName, "PSPUri", "")?.ToString();
            s.PS3Uri = Registry.GetValue(keyName, "PS3Uri", "")?.ToString();
            s.PS4Uri = Registry.GetValue(keyName, "PS4Uri", "")?.ToString();
            s.PS3AvatarUri = Registry.GetValue(keyName, "PS3AvatarUri", "")?.ToString();
            s.PSVDLCUri = Registry.GetValue(keyName, "DLCUri", "")?.ToString();
            s.PSPDLCUri = Registry.GetValue(keyName, "PSPDLCUri", "")?.ToString();
            s.PS3DLCUri = Registry.GetValue(keyName, "PS3DLCUri", "")?.ToString();
            s.PS4DLCUri = Registry.GetValue(keyName, "PS4DLCUri", "")?.ToString();
            s.PSVThemeUri = Registry.GetValue(keyName, "ThemeUri", "")?.ToString();
            s.PSPThemeUri = Registry.GetValue(keyName, "PSPThemeUri", "")?.ToString();
            s.PS3ThemeUri = Registry.GetValue(keyName, "PS3ThemeUri", "")?.ToString();
            s.PS4ThemeUri = Registry.GetValue(keyName, "PS4ThemeUri", "")?.ToString();
            s.PSVUpdateUri = Registry.GetValue(keyName, "UpdateUri", "")?.ToString();
            s.PS4UpdateUri = Registry.GetValue(keyName, "PS4UpdateUri", "")?.ToString();
        }

        return s;
    }


}




