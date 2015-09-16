using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace BzoneLauncher
{
    public interface LauncherTask
    {
        void StartWork();
    }

    public class PatchDownloadTask : LauncherTask
    {
        private BackgroundWorker worker;

        public List<string> Asset;
        public List<string> Base;

        public PatchDownloadTask()
        {
            Asset = new List<string>();
            Base = new List<string>();

            worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string PatchPath = @".\assets\patches\";
            WebClient client = new WebClient();
            int taskCount = Asset.Count + Base.Count;
            int counter = 0;
            Asset.ForEach(dr => {
                //worker.ReportProgress(counter / taskCount);
                if (!File.Exists(PatchPath + dr))
                {
                    client.DownloadFile("http://battlezone.videoventure.org/" + dr, PatchPath + dr);
                }
                counter++;
                //worker.ReportProgress(counter / taskCount);
            });
            Base.ForEach(dr => {
                //worker.ReportProgress(counter / taskCount);
                if (!File.Exists(PatchPath + dr))
                {
                    client.DownloadFile("http://battlezone.videoventure.org/" + dr, PatchPath + dr);
                }
                counter++;
                //worker.ReportProgress(counter / taskCount);
            });
        }

        public void AddAsset(string dr)
        {
            Asset.Add(dr);
        }

        public void AddBase(string dr)
        {
            Base.Add(dr);
        }

        public void StartWork()
        {
            worker.RunWorkerAsync();
        }
    }

    public class PatchInstallTask : LauncherTask
    {
        private List<LauncherTask> WaitOn;

        public PatchInstallTask()
        {
            WaitOn = new List<LauncherTask>();
        }

        public void WaitOnTask(LauncherTask patchDownloadTask)
        {
            WaitOn.Add(patchDownloadTask);
        }

        public void StartWork()
        {
            
        }
    }
}
