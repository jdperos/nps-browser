using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
public class Settings
{
    private const string CONFIG_PATH = "npsSettings.dat";
    private static readonly Lazy<Settings> _instance = new Lazy<Settings>(Load);

    // Settings
    public string PkgPath { get; set; }
    public string PkgParams { get; set; } = "-x {pkgFile} \"{zRifKey}\"";
    public bool DeleteAfterUnpack { get; set; } = false;
    public int SimultaneousDl { get; set; } = 2;

    // Game URIs
    public string PsvUri { get; set; }
    public string PsmUri { get; set; }
    public string PsxUri { get; set; }
    public string PspUri { get; set; }
    public string Ps3Uri { get; set; }
    public string Ps4Uri { get; set; }

    // Avatar URIs
    public string Ps3AvatarUri { get; set; }

    // DLC URIs
    public string PsvDlcUri { get; set; }
    public string PspDlcUri { get; set; }
    public string Ps3DlcUri { get; set; }
    public string Ps4DlcUri { get; set; }

    // Theme URIs
    public string PsvThemeUri { get; set; }
    public string PspThemeUri { get; set; }
    public string Ps3ThemeUri { get; set; }
    public string Ps4ThemeUri { get; set; }

    public string HmacKey { get; set; } = "";

    // Update URIs
    public string PsvUpdateUri { get; set; }
    public string Ps4UpdateUri { get; set; }

    public List<string> SelectedRegions { get; } = new List<string>();
    public List<string> SelectedTypes { get; } = new List<string>();

    public WebProxy Proxy { get; set; }
    public History HistoryInstance { get; } = new History();
    public string CompPackUrl { get; set; }
    public string CompPackPatchUrl { get; set; }

    public static Settings Instance => _instance.Value;

    public string DownloadDir { get; set; }

    public static Settings Load()
    {
        if (File.Exists(CONFIG_PATH))
        {
            using var stream = File.OpenRead(CONFIG_PATH);
            var formatter = new BinaryFormatter();
            return (Settings) formatter.Deserialize(stream);
        }

        return new Settings();
    }

    public void Save()
    {
        using var stream = File.Create(CONFIG_PATH);
        var formatter = new BinaryFormatter();
        formatter.Serialize(stream, this);
    }
}