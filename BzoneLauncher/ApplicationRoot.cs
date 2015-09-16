using Awesomium.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace BzoneLauncher
{
    public interface IApplicationCore
    {
        bool Busy { get; set; }

        void Add3rdPartyLaunchable(string Name, string Path, string Flags);
        void ShowBaloonTip(int timeout, string title, string message, ToolTipIcon icon = ToolTipIcon.None);
        JSValue GetAwesomiumGameList();
        JSValue GetAwesomiumPatchList();
        JSValue ActivateLaunchable(string ID);
        void SortLauncherItems(string[] sortArray);
        bool AddGameInstance(string version, string name);
    }

    class ApplicationRoot : IApplicationCore
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, UInt32 wParam, IntPtr lParam);
        const UInt32 WM_SYSCOMMAND = 0x0112;
        const UInt32 SC_RESTORE = 0xF120;
        
        private NotifyIcon TrayIcon;
        private ContextMenuStrip TrayMenu;
        private ToolStripMenuItem TrayLauncher;
        private ToolStripMenuItem TrayTasks;
        private ToolStripMenuItem TrayAbout;
        private ToolStripMenuItem TrayExit;

        private LauncherForm MainForm;
        private TaskForm TaskForm;
        private AboutBox AboutBox;

        private LauncherItemCollection LauncherItems;
        private PatchList Patches;

        private List<LauncherTask> TaskList;

        public bool _busy = false;
        public bool Busy
        {
            get
            {
                return _busy;
            }
            set
            {
                _busy = value;
                if (_busy)
                {
                    TrayIcon.Icon = Properties.Resources.launcher2;
                }
                else
                {
                    TrayIcon.Icon = Properties.Resources.launcher1;
                }
            }
        }

        public ApplicationRoot()
        {
            TrayIcon = new NotifyIcon();

            TrayMenu = new ContextMenuStrip();

            TrayIcon.ContextMenuStrip = TrayMenu;
            TrayIcon.Text = "Battlezone Launcher";

            TrayLauncher = new ToolStripMenuItem("Lancher", null, new EventHandler(TrayLauncher_Click));
            TrayTasks = new ToolStripMenuItem("Tasks", null, new EventHandler(TrayTasks_Click));
            TrayAbout = new ToolStripMenuItem("About", null, new EventHandler(TrayAbout_Click));
            TrayExit = new ToolStripMenuItem("Exit", null, new EventHandler(TrayExit_Click));

            TrayMenu.Items.AddRange(new ToolStripItem[] { TrayLauncher, TrayTasks, TrayAbout, new ToolStripSeparator(), TrayExit });

            LauncherItems = new LauncherItemCollection(@".\data\launcher.json");
            Patches = new PatchList(@".\data\patches.json");

            MainForm = new LauncherForm(this);
            TaskForm = new TaskForm();
            AboutBox = new AboutBox();

            TaskList = new List<LauncherTask>();

            TrayIcon.Icon = Properties.Resources.launcher1;
            TrayIcon.Visible = true;

            TrayIcon.MouseUp += TrayIcon_MouseUp;

            MainForm.Show();
        }

        private void TrayIcon_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(TrayIcon, null);
            }
        }
        private void TrayLauncher_Click(object sender, EventArgs e)
        {
            if (MainForm == null || MainForm.IsDisposed || MainForm.Disposing) MainForm = new LauncherForm(this);
            MainForm.Show();
            if (MainForm.WindowState == FormWindowState.Minimized)
            {
                if (MainForm.Handle != IntPtr.Zero)
                {
                    SendMessage(MainForm.Handle, WM_SYSCOMMAND, SC_RESTORE, IntPtr.Zero);
                }
            }
            MainForm.Focus();
        }
        private void TrayTasks_Click(object sender, EventArgs e)
        {
            if (TaskForm == null || TaskForm.IsDisposed || TaskForm.Disposing) TaskForm = new TaskForm();
            TaskForm.Show();
            if (TaskForm.WindowState == FormWindowState.Minimized)
            {
                if (TaskForm.Handle != IntPtr.Zero)
                {
                    SendMessage(TaskForm.Handle, WM_SYSCOMMAND, SC_RESTORE, IntPtr.Zero);
                }
            }
            TaskForm.Focus();
        }
        private void TrayAbout_Click(object sender, EventArgs e)
        {
            AboutBox.ShowDialog();
        }
        private void TrayExit_Click(object sender, EventArgs e)
        {
            ApplicationExit();
        }

        private void ApplicationExit()
        {
            TrayIcon.Visible = false;
            WebCore.Shutdown();
            while (WebCore.IsShuttingDown) { Thread.Sleep(100); }
            Application.Exit();
        }

        public void Add3rdPartyLaunchable(string Name, string Path, string Flags)
        {
            LauncherItems.Add3rdParty(Name, Path, Flags);
        }

        public void ShowBaloonTip(int timeout, string title, string message, ToolTipIcon icon = ToolTipIcon.None)
        {
            TrayIcon.ShowBalloonTip(timeout, title, message, icon);
        }

        public JSValue GetAwesomiumGameList()
        {
            return LauncherItems.ToAwesomiumJavaObject();
        }

        public JSValue GetAwesomiumPatchList()
        {
            return Patches.ToAwesomiumJavaObject();
        }

        public JSValue ActivateLaunchable(string ID)
        {
            JSValue val = LauncherItems.Activate(ID);

            return val;
        }

        public void SortLauncherItems(string[] sortArray)
        {
            LauncherItems.Sort(sortArray);
        }

        public bool AddGameInstance(string version, string name)
        {
            bool retVal = LauncherItems.AddInstance(version, name);

            PatchItem item = Patches.GetData(version);

            PatchDownloadTask patchDownloadTask = new PatchDownloadTask();

            string PatchPath = @".\assets\patches\";
            item.Assets.ForEach(dr => {
                if (!File.Exists(PatchPath + dr))
                {
                    patchDownloadTask.AddAsset(dr);
                }
            });
            item.Base.ForEach(dr => {
                if (!File.Exists(PatchPath + dr))
                {
                    patchDownloadTask.AddBase(dr);
                }
            });

            PatchInstallTask patchInstallTask = new PatchInstallTask();
            patchInstallTask.WaitOnTask(patchDownloadTask);

            TaskList.Add(patchDownloadTask);
            TaskList.Add(patchInstallTask);

            {
                patchDownloadTask.StartWork();
            }

            return retVal;
        }
    }
}
