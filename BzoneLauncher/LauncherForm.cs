using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Awesomium.Core;
using Awesomium.Windows.Controls;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using Awesomium.Core.Data;
using System.Runtime.InteropServices;

namespace BzoneLauncher
{
    public partial class LauncherForm : Form
    {
        private LauncherResourceInterceptor ResourceInterceptor;
        private LauncherDataSource DataSource;
        private WebSession session;
        private Stack<Uri> PageStack;

        private bool fullscreen = false;
        private FormWindowState preFullscreenState = FormWindowState.Normal;

        private IApplicationCore AppCore;

        private enum ZoomMode { None, Full, Width, Height }

        private ZoomMode zoomMode = ZoomMode.None;
        private float zoomTargetW = 640;
        private float zoomTargetH = 480;

        public LauncherForm(IApplicationCore AppCore)
        {
            this.AppCore = AppCore;

            PageStack = new Stack<Uri>();
            
            InitializeComponent();

            if (!WebCore.IsInitialized)
            {
                WebCore.Initialize(new WebConfig() {
                    HomeURL = new Uri(@"asset://bzlauncher/home"),
                    LogLevel = LogLevel.None,
                    RemoteDebuggingHost = @"127.0.0.1",
                    RemoteDebuggingPort = 8001
                }, true);
            }
            session = WebCore.CreateWebSession(@".\SessionDataPath", WebPreferences.Default);

            DataSource = new LauncherDataSource();
            ResourceInterceptor = new LauncherResourceInterceptor();

            session.AddDataSource("bzlauncher", DataSource);
            WebCore.ResourceInterceptor = ResourceInterceptor;
        }

        #region LauncherForm Event Bindings
        private void LauncherForm_Load(object sender, EventArgs e)
        {
            this.ClientSize = new Size(640, 480);
            this.MinimumSize = this.Size;
            webControl1.Source = new Uri(@"asset://bzlauncher/home");
        }
        private void LauncherForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Don't shut the WebCore down here, keep it around and shut it down in the root application on exit.
            // Do investigate restoring the WebCore from a crash however.
            AppCore.ShowBaloonTip(10, "Battlezone Launcher is still running", "Battlezone Launcher is still running, you may terminate the application from this icon.", ToolTipIcon.Info);
        }
        #endregion

        #region Awesomium Event Bindings
        private void Awesomium_Windows_Forms_WebControl_InitializeView(object sender, WebViewEventArgs e)
        {
            webControl1.WebSession = session;
        }
        private void Awesomium_Windows_Forms_WebControl_AddressChanged(object sender, UrlEventArgs e)
        {
            Console.WriteLine("HistoryStack: " + string.Join(",", PageStack.ToList()));
        }
        private void Awesomium_Windows_Forms_WebControl_NativeViewInitialized(object sender, WebViewEventArgs e)
        {
            JSValue result = webControl1.CreateGlobalJavascriptObject("Launcher");
            if (result.IsObject)
            {
                JSObject appObject = result;

                appObject.BindAsync("GetInstances", JS_GetInstances);
                appObject.BindAsync("Add3rdPartyLaunchable", JS_Add3rdPartyLaunchable);
                appObject.Bind("Quit", JS_Quit);
                appObject.Bind("Navigate", JS_Navigate);
                appObject.Bind("NavigateBack", JS_NavigateBack);
                appObject.BindAsync("LogIn", JS_LogIn);
                appObject.Bind("ActivateItem", JS_ActivateItem);
                appObject.Bind("SelectFile", JS_SelectFile);
                appObject.Bind("SaveModSort", JS_SaveModSort);
                appObject.Bind("ToggleFullscreen", JS_ToggleFullscreen);

                appObject.BindAsync("GetPatches", JS_GetPatches);

                appObject.Bind("CreateInstance", JS_CreateInstance); 

                //JSObject zoomObj = new JSObject();
                //appObject["Zoom"] = zoomObj;
                appObject.Bind("Zoom_SetModeNone", JS_Zoom_SetModeNone);
                appObject.Bind("Zoom_SetModeFull", JS_Zoom_SetModeFull);
                appObject.Bind("Zoom_SetModeHeight", JS_Zoom_SetModeHeight);
                appObject.Bind("Zoom_SetModeWidth", JS_Zoom_SetModeWidth);
                appObject.Bind("Zoom_SetTargetWidth", JS_Zoom_SetTargetWidth);
                appObject.Bind("Zoom_SetTargetHeight", JS_Zoom_SetTargetHeight);
            }
        }
        #endregion

        #region Zoom JS Bindings
        private JSValue JS_Zoom_SetModeNone(object sender, JavascriptMethodEventArgs args) { zoomMode = ZoomMode.None; return null; }
        private JSValue JS_Zoom_SetModeFull(object sender, JavascriptMethodEventArgs args) { zoomMode = ZoomMode.Full; return null; }
        private JSValue JS_Zoom_SetModeHeight(object sender, JavascriptMethodEventArgs args) { zoomMode = ZoomMode.Height; return null; }
        private JSValue JS_Zoom_SetModeWidth(object sender, JavascriptMethodEventArgs args) { zoomMode = ZoomMode.Width; return null; }
        private JSValue JS_Zoom_SetTargetWidth(object sender, JavascriptMethodEventArgs args) { if (args.Arguments.Count() > 0) { zoomTargetW = (float)(args.Arguments[0]); } return null; }
        private JSValue JS_Zoom_SetTargetHeight(object sender, JavascriptMethodEventArgs args) { if (args.Arguments.Count() > 0) { zoomTargetH = (float)(args.Arguments[0]); } return null; }
        #endregion

        private void JS_GetPatches(object sender, JavascriptMethodEventArgs args)
        {
            IWebView webView = (IWebView)sender;

            if (!webView.IsLive)
                return;

            // Access essential objects of the web-page's current JavaScript environment.
            //var global = e.Environment;

            if (args.Arguments.Count() > 0)
            {
                JSObject callback = args.Arguments[0];
                JSObject callbackCopy = callback.Clone();
                Task.Factory.StartNew(() =>
                { }).ContinueWith((t) =>
                {
                    JSValue val = (JSValue)AppCore.GetAwesomiumPatchList();
                    callbackCopy.InvokeAsync("call", callbackCopy, val);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private JSValue JS_CreateInstance(object sender, JavascriptMethodEventArgs args)
        {
            string version = null;
            string name = null;
            if (args.Arguments.Count() > 0)
            {
                version = (string)args.Arguments[0];
            }
            else
            {
                return false;
            }
            if (args.Arguments.Count() > 1)
            {
                name = (string)args.Arguments[1];
                if(name.Length == 0)
                {
                    name = version;
                }
            }
            else
            {
                name = version;
            }

            if (version == null || name == null) return false;

            bool InstanceAdded = AppCore.AddGameInstance(version, name);

            return InstanceAdded;
        }

        private void JS_Add3rdPartyLaunchable(object sender, JavascriptMethodEventArgs args)
        {
            IWebView webView = (IWebView)sender;

            if (!webView.IsLive)
                return;

            // Access essential objects of the web-page's current JavaScript environment.
            //var global = e.Environment;

            if (args.Arguments.Count() > 2)
            {
                JSObject callbackCopy = null;
                if (args.Arguments.Count() > 3)
                {
                    JSObject callback = args.Arguments[3];
                    callbackCopy = callback.Clone();
                }

                string strName = args.Arguments[0];
                string strPath = args.Arguments[1];
                string strFlags = args.Arguments[2];

                Task.Factory.StartNew(() =>
                { }).ContinueWith((t) =>
                {
                    AppCore.Add3rdPartyLaunchable(strName, strPath, strFlags);

                    if (callbackCopy != null)
                    {
                        callbackCopy.InvokeAsync("call", callbackCopy/*, val*/);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private void JS_GetInstances(object sender, JavascriptMethodEventArgs args)
        {
            IWebView webView = (IWebView)sender;

            if (!webView.IsLive)
                return;

            // Access essential objects of the web-page's current JavaScript environment.
            //var global = e.Environment;

            if (args.Arguments.Count() > 0)
            {
                JSObject callback = args.Arguments[0];
                JSObject callbackCopy = callback.Clone();
                Task.Factory.StartNew(() =>
                {}).ContinueWith((t) =>
                {
                    //JSValue val = webControl1.ExecuteJavascriptWithResult(JsonConvert.SerializeObject(BaseGameInstalls));
                    //JSValue val = (JSValue)LauncherItems.ToAwesomiumJavaObject();
                    JSValue val = (JSValue)AppCore.GetAwesomiumGameList(); 
                    callbackCopy.InvokeAsync("call", callbackCopy, val);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

        }
        private JSValue JS_Quit(object sender, JavascriptMethodEventArgs args)
        {
            // Attempt to close the program
            //AttemptToExit();
            this.Close();
            return null;
        }
        private JSValue JS_Navigate(object sender, JavascriptMethodEventArgs args)
        {
            if (args.Arguments.Length > 0)
            {
                if (PageStack.Count == 0 || PageStack.Peek() != webControl1.Source)
                    PageStack.Push(webControl1.Source);

                string page = args.Arguments[0];
                webControl1.Source = new Uri(@"asset://bzlauncher/" + page);
            }
            return null;
        }
        private JSValue JS_ActivateItem(object sender, JavascriptMethodEventArgs args)
        {
            if (args.Arguments.Length > 0)
            {
                string ID = args.Arguments[0];
                JSValue val = AppCore.ActivateLaunchable(ID);
                //JSValue val = LauncherItems.Activate(ID);
                {
                    //JSObject obj = ((JSObject)val);
                    //if(obj.HasProperty("Minimize") && obj["Minimize"] == true)
                    //{
                    //    notifyIcon1.Visible = true;
                    //    this.Hide();
                    //}
                }
                return val;
                //string page = args.Arguments[0];
                //webControl1.Source = new Uri(@"asset://bzlauncher/" + page);
            }
            return null;
        }
        private JSValue JS_NavigateBack(object sender, JavascriptMethodEventArgs args)
        {
            NavigateBack();
            return null;
        }
        private JSValue JS_SelectFile(object sender, JavascriptMethodEventArgs args)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                JSObject obj = new JSObject();
                obj["fullpath"] = openFileDialog1.FileName;
                obj["filename"] = Path.GetFileName(openFileDialog1.FileName);
                return new JSValue(obj);
            }
            return null;
        }
        private JSValue JS_SaveModSort(object sender, JavascriptMethodEventArgs args)
        {
            if (args.Arguments.Length == 0) return false;
            JSValue param1 = args.Arguments[0];
            if (!param1.IsArray) return false;
            JSValue[] arr = (JSValue[])param1;
            string[] sortArray = arr.Where(dr => dr.IsString).Select(dr => (string)dr).ToArray();
            if (sortArray.Length == 0) return false;
            SaveModSort(sortArray);
            return true;
        }

        #region Temporary Implementation
        private void JS_LogIn(object sender, JavascriptMethodEventArgs args)
        {
            string email = args.Arguments[0];
            string pass = args.Arguments[1];
            JSObject callback = args.Arguments[2];
            JSObject callbackCopy = callback.Clone();

            string username = string.Empty;

            //TaskScheduler.FromCurrentSynchronizationContext();
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(5000);
                //callbackCopy.InvokeAsync("call", callbackCopy, true);
                username = "TestUser";
            }).ContinueWith(
                (t) => callbackCopy.InvokeAsync("call", callbackCopy, true, username),
                TaskScheduler.FromCurrentSynchronizationContext());
        }
        #endregion

        /*private void LoadMods()
        {
            string[] instanceFiles = Directory.GetFiles(".\\Core\\Manifest\\Instances\\");
            foreach(string instance in instanceFiles)
            {
                string fullText = File.ReadAllText(instance);
                string key = Path.GetFileNameWithoutExtension(instance);
                if (!GameInstances.ContainsKey(key))
                {
                    JObject obj = JObject.Parse(fullText);
                    GameInstance newInstance = new GameInstance() { Name = obj["name"].Value<string>(), Shell = obj["shell"].Value<string>() };
                    GameInstances.Add(key, newInstance);
                }
            }
        }*/
        /*private void WebCore_Started(object sender, CoreStartEventArgs coreStartEventArgs)
        {
            WebCore.ResourceInterceptor = ResourceInterceptor;
        }*/
        /*struct GameInstance
        {
	        public string Name;
            public string Shell;
        }*/
        private void Awesomium_Windows_Forms_WebControl_ConsoleMessage(object sender, ConsoleMessageEventArgs e)
        {
            Console.WriteLine("[{0}] {1}\tLINE:{2}", e.EventName, e.Message, e.LineNumber);
        }

        private JSValue JS_ToggleFullscreen(object sender, JavascriptMethodEventArgs args)
        {
            ToggleFullscreen();
            return null;
        }
        private void tmrFixZoom_Tick(object sender, EventArgs e)
        {
            WindowZoomFix();
        }

        private void NavigateBack()
        {
            if (PageStack.Count > 0)
            {
                //PageStack.Pop();
                webControl1.Source = PageStack.Pop();
            }
        }
        private void SaveModSort(string[] sortArray)
        {
            AppCore.SortLauncherItems(sortArray);
        }
        private void ToggleFullscreen()
        {
            if (fullscreen)
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
                this.WindowState = preFullscreenState;
                fullscreen = false;
            }
            else
            {
                preFullscreenState = this.WindowState;
                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                fullscreen = true;
            }
        }
        private void WindowZoomFix()
        {
            if (this.WindowState != FormWindowState.Minimized)
            {
                float zoomLev = 100;

                switch (zoomMode)
                {
                    case ZoomMode.Full:
                        {
                            if ((webControl1.Width * (zoomTargetH / zoomTargetW)) > webControl1.Height)
                            {
                                zoomLev = webControl1.Height * 100.0f / zoomTargetH;
                            }
                            else
                            {
                                zoomLev = webControl1.Width * 100.0f / zoomTargetW;
                            }
                        }
                        break;
                    case ZoomMode.Height:
                        {
                            zoomLev = webControl1.Height * 100.0f / zoomTargetH;
                        }
                        break;
                    case ZoomMode.Width:
                        {
                            zoomLev = webControl1.Width * 100.0f / zoomTargetW;
                        }
                        break;
                }

                webControl1.Zoom = (int)zoomLev;
            }
        }
    }
}
