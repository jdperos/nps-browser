using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[System.Serializable]
public class History
{


    public const int ver = 1;
    public List<NPS.DownloadWorker> currentlyDownloading = new List<NPS.DownloadWorker>();
    public List<NPS.Item> completedDownloading = new List<NPS.Item>();

  


}




