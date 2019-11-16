using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO.Compression;
using Avalonia.Threading;
using NPS.Helpers;
using ReactiveUI;

namespace NPS
{
    [Serializable]
    public class DownloadWorker
    {
        public Item currentDownload;

        //private WebClient webClient;
        private DateTime lastUpdate;
        private long lastBytes;

        public int progressValue = 0;
        //public bool isRunning { get; private set; }
        //public bool isCompleted { get; private set; }
        //public bool isCanceled { get; private set; }

        public WorkerStatus status { get; private set; }

        [NonSerialized] public DownloadWorkerItem lvi;
        [NonSerialized] private DispatcherTimer timer = new DispatcherTimer();

        public DownloadWorker(Item itm)
        {
            currentDownload = itm;
            lvi = new DownloadWorkerItem(this, itm.TitleName);
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
            lvi = new DownloadWorkerItem(this, currentDownload.TitleName);
            lvi.Speed = "Waiting";

            if (progressValue > 100) progressValue = 100;
            lvi.Progress = progressValue;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            if (status == WorkerStatus.Running)
                Start();
            else if (status == WorkerStatus.Downloaded)
            {
                Unpack();
            }
            else if (status == WorkerStatus.Completed)
            {
                lvi.Speed = "";
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
            lvi.Progress = progressValue;
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

        [NonSerialized] private Process unpackProcess = null;

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

            if (status == WorkerStatus.Downloaded || status == WorkerStatus.Completed)
            {
                lvi.Status = "Unpacking";
                lvi.ProgressIndeterminate = true;

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

                var a = new ProcessStartInfo
                {
                    WorkingDirectory = Settings.Instance.DownloadDir + Path.DirectorySeparatorChar,
                    FileName = Settings.Instance.PkgPath,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    Arguments = replacements.Aggregate(Settings.Instance.PkgParams.ToLower(),
                        (str, rep) => str.Replace(rep.Key, rep.Value)),
                    UseShellExecute = false,
                    RedirectStandardError = true
                };

                unpackProcess = new Process
                {
                    StartInfo = a,
                    EnableRaisingEvents = true
                };

                unpackProcess.Exited += Proc_Exited;
                unpackProcess.ErrorDataReceived += new DataReceivedEventHandler(UnpackProcess_ErrorDataReceived);
                errors = new List<string>();
                unpackProcess.Start();
                unpackProcess.BeginErrorReadLine();
            }
        }

        private void UnpackCompPack()
        {
            if (status == WorkerStatus.Downloaded || status == WorkerStatus.Completed)
            {
                status = WorkerStatus.Completed;
                try
                {
                    lvi.Status = "Processing";

                    //if (Directory.Exists(Path.Combine(Settings.Instance.downloadDir, "rePatch", currentDownload.TitleId)))
                    //    Directory.Delete(Path.Combine(Settings.Instance.downloadDir, "rePatch", currentDownload.TitleId), true);


                    using (var archive = ZipFile.OpenRead(
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
                    if (!Settings.Instance.HistoryInstance.completedDownloading.Contains(currentDownload))
                        Settings.Instance.HistoryInstance.completedDownloading.Add(currentDownload);

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
            if (status == WorkerStatus.Downloaded || status == WorkerStatus.Completed)
            {
                status = WorkerStatus.Completed;
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

                    if (!Settings.Instance.HistoryInstance.completedDownloading.Contains(currentDownload))
                        Settings.Instance.HistoryInstance.completedDownloading.Add(currentDownload);
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
            status = WorkerStatus.Completed;

            var proc = (sender as Process);
            if (proc.ExitCode == 0)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    lvi.Speed = "";
                    lvi.Status = "Completed";
                    lvi.ProgressIndeterminate = false;

                    if (!Settings.Instance.HistoryInstance.completedDownloading.Contains(currentDownload))
                        Settings.Instance.HistoryInstance.completedDownloading.Add(currentDownload);

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
                    lvi.ProgressIndeterminate = false;

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
        [NonSerialized] private Stream smRespStream;
        [NonSerialized] private FileStream saveFileStream;

        private void DownloadFile(string sSourceURL, string sDestinationPath)
        {
            try
            {
                long iFileSize = 0;
                int iBufferSize = 1024;
                iBufferSize *= 1000;
                long iExistLen = 0;

                if (File.Exists(sDestinationPath))
                {
                    FileInfo fINfo =
                        new FileInfo(sDestinationPath);
                    iExistLen = fINfo.Length;
                }

                ;
                if (iExistLen > 0)
                    saveFileStream = new FileStream(sDestinationPath,
                        FileMode.Append, FileAccess.Write,
                        FileShare.ReadWrite);
                else
                    saveFileStream = new FileStream(sDestinationPath,
                        FileMode.Create, FileAccess.Write,
                        FileShare.ReadWrite);

                HttpWebRequest hwRq;
                HttpWebResponse hwRes;
                var urr = new Uri(sSourceURL);
                hwRq = (HttpWebRequest) WebRequest.Create(urr);
                hwRq.Proxy = Settings.Instance.Proxy;
                hwRes = (HttpWebResponse) hwRq.GetResponse();

                long totalLength = hwRes.ContentLength;

                hwRes.Dispose();

                totalSize = totalLength;
                if (totalLength != iExistLen)
                {
                    hwRq = (HttpWebRequest) WebRequest.Create(urr);
                    hwRq.Proxy = Settings.Instance.Proxy;
                    hwRq.AddRange(iExistLen);

                    hwRes = (HttpWebResponse) hwRq.GetResponse();
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
                    Pause();
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

            status = WorkerStatus.Downloaded;

            lvi.Speed = "";


            Unpack();

            progressValue = 100;
            lvi.Progress = progressValue;
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
                    lvi.Progress = progressValue;
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

    public class DownloadWorkerItem : ReactiveObject
    {
        private string _speed = "";
        private string _status = "";
        private double _progress;
        private bool _progressIndeterminate;

        public DownloadWorkerItem(DownloadWorker worker, string title)
        {
            Worker = worker;
            Title = title;
        }

        public DownloadWorker Worker { get; }
        public string Title { get; }

        public string Speed
        {
            get => _speed;
            set => this.RaiseAndSetIfChanged(ref _speed, value);
        }

        public string Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        public double Progress
        {
            get => _progress;
            set => this.RaiseAndSetIfChanged(ref _progress, value);
        }

        public bool ProgressIndeterminate
        {
            get => _progressIndeterminate;
            set => this.RaiseAndSetIfChanged(ref _progressIndeterminate, value);
        }
    }
}