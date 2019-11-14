using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO.Compression;
using Avalonia.Controls;
using Avalonia.Threading;
using NPS.Helpers;

namespace NPS
{
    [System.Serializable]
    public class DownloadWorker
    {
        public Item currentDownload;

        //private WebClient webClient;
        private DateTime lastUpdate;
        private long lastBytes;
        [System.NonSerialized] public ProgressBar progress = new ProgressBar();
        public DownloadWorkerItem lvi;

        public int progressValue = 0;
        //public bool isRunning { get; private set; }
        //public bool isCompleted { get; private set; }
        //public bool isCanceled { get; private set; }

        public WorkerStatus status { get; private set; }
        [System.NonSerialized] private DispatcherTimer timer = new DispatcherTimer();


        public DownloadWorker(Item itm)
        {
            currentDownload = itm;
            lvi = new DownloadWorkerItem(itm.TitleName);
            lvi.Speed = "Waiting";
            //isRunning = false;
            //isCanceled = false;
            //isCompleted = false;

            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            status = WorkerStatus.Queued;
        }

        public void Recreate()
        {
            progress = new ProgressBar();
            if (progressValue > 100) progressValue = 100;
            progress.Value = progressValue;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            if (status == WorkerStatus.Running)
                Start();
            else if (this.status == WorkerStatus.Downloaded)
            {
                Unpack();
            }
            else if (this.status == WorkerStatus.Completed)
            {
                lvi.Status = "";
                lvi.Status = "Completed";
            }
        }


        public void Start()
        {
            Console.WriteLine("start process " + currentDownload.TitleName);
            timer.Start();
            //isRunning = true;
            status = WorkerStatus.Running;

            Task.Run(() =>
            {
                DownloadFile(currentDownload.pkg,
                    Path.Combine(Settings.Instance.DownloadDir,
                        currentDownload.DownloadFileName + currentDownload.extension));
            });
        }

        public void Cancel()
        {
            timer.Stop();
            if (status == WorkerStatus.Completed) return;

            status = WorkerStatus.Canceled;

            if (smRespStream != null)
                smRespStream.Close();
            if (saveFileStream != null)
                saveFileStream.Close();
            if (unpackProcess != null && !unpackProcess.HasExited)
            {
                unpackProcess.Kill();
            }

            lvi.Speed = "";
            lvi.Status = "Canceled";
            progressValue = 0;
            progress.Value = progressValue;
            DeletePkg();
        }

        public void Pause()
        {
            if (status == WorkerStatus.Running || status == WorkerStatus.Queued)
            {
                timer.Stop();

                status = WorkerStatus.Paused;

                if (smRespStream != null)
                {
                    smRespStream.Close();
                }

                if (saveFileStream != null)
                    saveFileStream.Close();
                if (unpackProcess != null && !unpackProcess.HasExited)
                {
                    unpackProcess.Kill();
                }

                lvi.Speed = "Paused";
            }

            //progress.Value = 0;
        }

        public void Resume()
        {
            if (status == WorkerStatus.Paused || status == WorkerStatus.DownloadError)
            {
                lvi.Speed = "Queued";
                status = WorkerStatus.Queued;
            }
        }

        public string Pkg
        {
            get
            {
                return Path.Combine(Settings.Instance.DownloadDir,
                    currentDownload.DownloadFileName + currentDownload.extension);
            }
        }

        public void DeletePkg()
        {
            if (currentDownload != null)
            {
                for (int i = 0; i < 1; i++)
                {
                    try
                    {
                        if (File.Exists(Path.Combine(Settings.Instance.DownloadDir,
                            currentDownload.DownloadFileName + currentDownload.extension)))
                        {
                            System.Threading.Thread.Sleep(400);
                            File.Delete(Path.Combine(Settings.Instance.DownloadDir,
                                currentDownload.DownloadFileName + currentDownload.extension));
                        }
                    }
                    catch
                    {
                        i = 5;
                    }
                }
            }
        }

        [System.NonSerialized] private Process unpackProcess = null;

        public void Unpack()
        {
            if (currentDownload.ItsPS3)
            {
                UnpackPS3();
                return;
            }

            if (currentDownload.ItsCompPack)
            {
                UnpackCompPack();
                return;
            }

            if (this.status == WorkerStatus.Downloaded || this.status == WorkerStatus.Completed)
            {
                lvi.Status = "Unpacking";

                String tempName = "";
                string dlc = "";
                if (currentDownload.IsDLC == true)
                {
                    //dlc = "[DLC]";
                    tempName = "[DLC] " + currentDownload.ParentGameTitle;
                }
                else tempName = currentDownload.TitleName;

                String fwVersion = "3.60";
                if (tempName.Contains("3.61") /*currentDownload.TitleName.Contains("3.61")*/) fwVersion = "3.61";
                String[] tempStr = tempName.Split();
                tempName = "";

                foreach (var i in tempStr)
                {
                    if ((i.Contains("3.6")) && (!i.Contains("3.61+"))) fwVersion = i;
                    if (!i.Contains("3.6")) tempName += i + " ";
                }


                tempName = Regex.Replace(tempName, "[/:\"*?<>|]+", " ");
                tempName = Regex.Replace(tempName, "\\r\\n", string.Empty);
                tempName = tempName.Trim();

                var replacements = new Dictionary<string, string>
                {
                    ["{pkgfile}"] = "\"" + Path.Combine(Settings.Instance.DownloadDir,
                                        currentDownload.DownloadFileName + currentDownload.extension) + "\"",
                    ["{titleid}"] = currentDownload.TitleId.Substring(0, 9),
                    ["{gametitle}"] = tempName,
                    ["{region}"] = currentDownload.Region,
                    ["{zrifkey}"] = currentDownload.zRif,
                    ["{fwversion}"] = fwVersion,
                    ["{dlc}"] = dlc,
                    ["  "] = " "
                };

                ProcessStartInfo a = new ProcessStartInfo();
                a.WorkingDirectory = Settings.Instance.DownloadDir + Path.DirectorySeparatorChar;
                a.FileName = string.Format("\"{0}\"", Settings.Instance.PkgPath);
                a.WindowStyle = ProcessWindowStyle.Hidden;
                a.CreateNoWindow = true;
                a.Arguments = replacements.Aggregate(Settings.Instance.PkgParams.ToLower(),
                    (str, rep) => str.Replace(rep.Key, rep.Value));
                unpackProcess = new Process();
                unpackProcess.StartInfo = a;

                a.UseShellExecute = false;
                a.RedirectStandardError = true;

                unpackProcess.EnableRaisingEvents = true;
                unpackProcess.Exited += Proc_Exited;
                unpackProcess.ErrorDataReceived += new DataReceivedEventHandler(UnpackProcess_ErrorDataReceived);
                errors = new List<string>();
                unpackProcess.Start();
                unpackProcess.BeginErrorReadLine();
            }
        }

        private void UnpackCompPack()
        {
            if (this.status == WorkerStatus.Downloaded || this.status == WorkerStatus.Completed)
            {
                this.status = WorkerStatus.Completed;
                try
                {
                    lvi.Status = "Processing";

                    //if (Directory.Exists(Path.Combine(Settings.Instance.downloadDir, "rePatch", currentDownload.TitleId)))
                    //    Directory.Delete(Path.Combine(Settings.Instance.downloadDir, "rePatch", currentDownload.TitleId), true);


                    using (var archive = System.IO.Compression.ZipFile.OpenRead(
                        Path.Combine(Settings.Instance.DownloadDir,
                            currentDownload.DownloadFileName + currentDownload.extension)))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (entry.Length == 0) continue;

                            string file = Path.Combine(Settings.Instance.DownloadDir, "rePatch",
                                currentDownload.TitleId, entry.FullName);
                            var dir = Path.GetDirectoryName(file);

                            if (!Directory.Exists(dir))
                                Directory.CreateDirectory(dir);

                            entry.ExtractToFile(file, true);
                        }
                    }

                    //System.IO.Compression.ZipFile.ExtractToDirectory(Path.Combine(Settings.Instance.downloadDir, currentDownload.DownloadFileName + currentDownload.extension), Path.Combine(Settings.Instance.downloadDir, "rePatch", currentDownload.TitleId));
                    lvi.Speed = "";
                    lvi.Status = "Completed";
                    if (!Settings.Instance.HistoryInstance.completedDownloading.Contains(this.currentDownload))
                        Settings.Instance.HistoryInstance.completedDownloading.Add(this.currentDownload);

                    if (Settings.Instance.DeleteAfterUnpack)
                        DeletePkg();
                }
                catch (Exception err)
                {
                    lvi.Speed = "Error!";
                    lvi.Status = err.Message;
                }
            }
        }

        private void UnpackPS3()
        {
            if (this.status == WorkerStatus.Downloaded || this.status == WorkerStatus.Completed)
            {
                this.status = WorkerStatus.Completed;
                try
                {
                    lvi.Status = "Processing";
                    string path = Settings.Instance.DownloadDir + Path.DirectorySeparatorChar + "packages";
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    File.Move(
                        Settings.Instance.DownloadDir + Path.DirectorySeparatorChar + currentDownload.DownloadFileName +
                        currentDownload.extension,
                        path + Path.DirectorySeparatorChar + currentDownload.DownloadFileName +
                        currentDownload.extension);

                    path = Settings.Instance.DownloadDir + Path.DirectorySeparatorChar + "exdata";

                    if (!string.IsNullOrEmpty(currentDownload.ContentId) &&
                        currentDownload.ContentId.ToLower() != "missing" &&
                        currentDownload.zRif.ToLower() != "NOT REQUIRED".ToLower() &&
                        currentDownload.zRif.Length % 2 == 0)
                    {
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }

                        byte[] array = new byte[currentDownload.zRif.Length / 2];
                        for (int i = 0; i < currentDownload.zRif.Length / 2; i++)
                        {
                            array[i] = Convert.ToByte(currentDownload.zRif.Substring(i * 2, 2), 16);
                        }

                        File.WriteAllBytes(path + Path.DirectorySeparatorChar + currentDownload.ContentId + ".rap",
                            array);
                    }

                    lvi.Speed = "";
                    lvi.Status = "Completed";

                    if (!Settings.Instance.HistoryInstance.completedDownloading.Contains(this.currentDownload))
                        Settings.Instance.HistoryInstance.completedDownloading.Add(this.currentDownload);
                }
                catch (Exception err)
                {
                    lvi.Speed = "Error!";
                    lvi.Status = err.Message;
                }
            }
        }

        private List<string> errors = new List<string>();

        private void UnpackProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            errors.Add(e.Data);
        }

        private void Proc_Exited(object sender, EventArgs e)
        {
            this.status = WorkerStatus.Completed;

            var proc = (sender as Process);
            if (proc.ExitCode == 0)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    lvi.Speed = "";
                    lvi.Status = "Completed";

                    if (!Settings.Instance.HistoryInstance.completedDownloading.Contains(this.currentDownload))
                        Settings.Instance.HistoryInstance.completedDownloading.Add(this.currentDownload);

                    if (Settings.Instance.DeleteAfterUnpack)
                        DeletePkg();
                });
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    lvi.Speed = "PKG decrypt err!";
                    lvi.Status = "";

                    errors.Remove(null);
                    if (errors.Count > 0)
                    {
                        if (errors[0].Contains("pkg_dec - PS Vita PKG decryptor/unpacker"))
                            errors.Remove(errors[0]);
                        if (errors.Count > 0)
                            lvi.Status = errors[0];
                    }
                });
            }
        }


        private long totalSize = 0;
        private long completedSize = 0;
        [System.NonSerialized] private System.IO.Stream smRespStream;
        [System.NonSerialized] private System.IO.FileStream saveFileStream;

        private void DownloadFile(string sSourceURL, string sDestinationPath)
        {
            try
            {
                long iFileSize = 0;
                int iBufferSize = 1024;
                iBufferSize *= 1000;
                long iExistLen = 0;

                if (System.IO.File.Exists(sDestinationPath))
                {
                    System.IO.FileInfo fINfo =
                        new System.IO.FileInfo(sDestinationPath);
                    iExistLen = fINfo.Length;
                }

                ;
                if (iExistLen > 0)
                    saveFileStream = new System.IO.FileStream(sDestinationPath,
                        System.IO.FileMode.Append, System.IO.FileAccess.Write,
                        System.IO.FileShare.ReadWrite);
                else
                    saveFileStream = new System.IO.FileStream(sDestinationPath,
                        System.IO.FileMode.Create, System.IO.FileAccess.Write,
                        System.IO.FileShare.ReadWrite);

                HttpWebRequest hwRq;
                System.Net.HttpWebResponse hwRes;
                var urr = new Uri(sSourceURL);
                hwRq = (System.Net.HttpWebRequest) System.Net.HttpWebRequest.Create(urr);
                hwRq.Proxy = Settings.Instance.Proxy;
                hwRes = (System.Net.HttpWebResponse) hwRq.GetResponse();
                hwRes.Close();

                long totalLength = hwRes.ContentLength;
                totalSize = totalLength;
                if (totalLength != iExistLen)
                {
                    hwRq = (System.Net.HttpWebRequest) System.Net.HttpWebRequest.Create(urr);
                    hwRq.Proxy = Settings.Instance.Proxy;
                    hwRq.AddRange(iExistLen);

                    hwRes = (System.Net.HttpWebResponse) hwRq.GetResponse();
                    smRespStream = hwRes.GetResponseStream();

                    iFileSize = hwRes.ContentLength;
                    //   totalSize += hwRes.ContentLength;

                    byte[] downBuffer = new byte[iBufferSize];
                    int iByteSize;
                    while ((iByteSize = smRespStream.Read(downBuffer, 0, downBuffer.Length)) > 0)
                    {
                        if (status == WorkerStatus.Paused || status == WorkerStatus.Canceled) return;

                        saveFileStream.Write(downBuffer, 0, iByteSize);

                        completedSize = saveFileStream.Position;

                        if (lastBytes == 0)
                        {
                            lastUpdate = DateTime.Now;
                            lastBytes = completedSize;
                        }
                        else
                        {
                            var now = DateTime.Now;
                            var timeSpan = now - lastUpdate;
                            var bytesChange = completedSize - lastBytes;
                            if (timeSpan.Seconds != 0)
                            {
                                bytesPerSecond = bytesChange / timeSpan.Seconds;
                                lastBytes = completedSize;
                                lastUpdate = now;
                            }
                        }
                    }

                    smRespStream.Close();
                }


                saveFileStream.Close();

                Dispatcher.UIThread.Post(DownloadCompleted);
            }
            catch (Exception err)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    this.Pause();
                    MessageBox.Show(
                        "Unable to download \"" + currentDownload.TitleName + "\"." + Environment.NewLine + err.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
        }


        private long bytesPerSecond = 0;


        private void DownloadCompleted()
        {
            timer.Stop();

            this.status = WorkerStatus.Downloaded;

            lvi.Speed = "";


            Unpack();

            progressValue = 100;
            progress.Value = progressValue;
        }


        private void Timer_Tick(object sender, EventArgs e)
        {
            string speed = "";
            bytesPerSecond = bytesPerSecond / 1024;
            if (bytesPerSecond < 1500)
                speed = bytesPerSecond.ToString() + " KB/s";
            else
            {
                speed = ((float) ((float) bytesPerSecond / 1024)).ToString("0.00") + " MB/s";
            }

            lvi.Speed = speed;
            var prgs = (float) completedSize / (float) totalSize;

            try
            {
                if (prgs != float.NaN)
                {
                    progressValue = Convert.ToInt32(prgs * 100);
                    progress.Value = progressValue;
                }
            }
            catch
            {
            }

            lvi.Status = completedSize / (1024 * 1024) + "MB/" + totalSize / (1024 * 1024) + "MB";
        }
    }

    public enum WorkerStatus
    {
        Queued,
        Running,
        Paused,
        Completed,
        Downloaded,
        Canceled,
        DownloadError
    }

    public class DownloadWorkerItem
    {
        public DownloadWorkerItem(string title)
        {
            Title = title;
        }

        public string Title { get; }
        public string Speed { get; set; } = "";
        public string Status { get; set; } = "";
        public double Progress { get; set; } = 0;
    }
}