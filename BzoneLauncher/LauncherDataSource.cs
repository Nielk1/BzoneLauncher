using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awesomium.Core.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using Awesomium.Windows.Data;
using System.Web;
using System.Collections.Specialized;

namespace BzoneLauncher
{
    class LauncherDataSource : DataSource 
    {
        protected override void OnRequest(DataSourceRequest request)
        {
            //Console.WriteLine("DataSource: {0}\tHost: {1}\tPathAndQuery: {2}\tLocalPath: {3}", request.Url, request.Url.Host, request.Url.PathAndQuery, request.Url.LocalPath);

            string path = request.Url.LocalPath.Trim('/','\\');
            string[] pathParts = path.Split('/', '\\');
            NameValueCollection QS = HttpUtility.ParseQueryString(request.Url.Query);

            if (pathParts.Length > 0)
            {
                if (pathParts[0] == "home")
                {
                    RespondeWithFile(request, ".\\core\\home.html");
                }
                else if (pathParts[0] == "about")
                {
                    RespondeWithFile(request, ".\\core\\about.html");
                }
                else if (pathParts[0] == "options")
                {
                    RespondeWithFile(request, ".\\core\\options.html");
                }
                else if (pathParts[0] == "images")
                {
                    string palletName = QS["pallet"];
                    if(palletName != null)
                    {
                        PalletCache tmpCache = PalletCache.GetInstance();
                        Image returnImage = tmpCache.ColorizeImage(palletName, Image.FromFile(".\\core\\images\\" + string.Join("\\", pathParts.Skip(1))));
                        RespondeWithPng(request, returnImage);
                    }
                    else
                    {
                        RespondeWithFile(request, ".\\core\\images\\" + string.Join("\\", pathParts.Skip(1)));
                    }
                }
                else if (pathParts[0] == "scripts")
                {
                    RespondeWithFile(request, ".\\core\\scripts\\" + string.Join("\\", pathParts.Skip(1)));
                }
                else if (pathParts[0] == "styles")
                {
                    RespondeWithFile(request, ".\\core\\styles\\" + string.Join("\\", pathParts.Skip(1)));
                }
            }
            else
            {
                SendRequestFailed(request);
            }

            /*if (request.Path == "index.html")
            {
                //SendResponse(request, DataSourceResponse
            }*/
        }

        private void RespondeWithFile(DataSourceRequest request, string filename)
        {
            string mimeType = string.Empty;
            
            string ext = Path.GetExtension(filename);
            if (ext == ".html") mimeType = "text/html";
            if (ext == ".png") mimeType = "image/png";
            if (ext == ".gif") mimeType = "image/gif";
            if (ext == ".js") mimeType = "application/javascript";
            if (ext == ".css") mimeType = "text/css";
            //if (ext == ".webm") mimeType = "video/webm";

            if (mimeType.Length > 0)
            {
                byte[] buffer = File.ReadAllBytes(filename);
                IntPtr tmpPtr = Marshal.AllocHGlobal(buffer.Length);
                Marshal.Copy(buffer, 0, tmpPtr, buffer.Length);
                DataSourceResponse response = new DataSourceResponse() { Buffer = tmpPtr, Size = (uint)buffer.Length, MimeType = mimeType };
                SendResponse(request, response);
                Marshal.FreeHGlobal(tmpPtr);
            }
            else
            {
                SendRequestFailed(request);
            }
        }

        private void RespondeWithPng(DataSourceRequest request, Image image)
        {
            byte[] buffer = new byte[0];
            using (MemoryStream stream = new MemoryStream())
            {
                image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Close();

                buffer = stream.ToArray();
            }
            IntPtr tmpPtr = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, tmpPtr, buffer.Length);
            DataSourceResponse response = new DataSourceResponse() { Buffer = tmpPtr, Size = (uint)buffer.Length, MimeType = "image/png" };
            SendResponse(request, response);
            Marshal.FreeHGlobal(tmpPtr);
        }
    }
}
