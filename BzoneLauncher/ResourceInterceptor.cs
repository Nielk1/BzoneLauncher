using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awesomium.Core;
using System.Reflection;
using System.IO;

namespace BzoneLauncher
{
    /// <summary>
    ///     A custom implementation of the `Awesomium.Windows.Forms.ResourceDataSourceProvider` functionality.
    ///     This interceptor supports using the standard `http://` protocol scheme instead of Awesomium's use of the
    ///     custom `asset://` scheme, which can cause problems with cross origin requests in Chromium, as well as
    ///     third party services like Google APIs which authorize JavaScript clients based on the value of `window.location`
    ///     in the browser.
    /// </summary>
    public class ResourceInterceptor : IResourceInterceptor
    {
        /// <summary>The base Uri to use as the protocol/scheme and server/domain for requesting embedded resources in the assembly manifest</summary>
        /// <remarks>Uses the template `http://[AssemblyName].local`</remarks>
        public static readonly Uri EmbeddedResourceDomain;

        /// <summary>A cached reference to the assembly containing the embedded resources</summary>
        private static readonly Assembly AppAssembly;

        /// <summary>The temporary folder on disk </summary>
        private static readonly string TempFolder;

        /// <summary>
        ///     Static constructor to initialize the EmbeddedResourceDomain and TempFolder variables
        /// </summary>
        static ResourceInterceptor()
        {
            // cache a reference to the app's assembly to avoid looking up for every static file
            AppAssembly = typeof(ResourceInterceptor).Assembly;

            // the base Uri to use for all embedded resource requests
            EmbeddedResourceDomain = new Uri(String.Concat("http://", AppAssembly.GetName().Name, ".local"));

            // let the framework create a unique directory path for us by using the method for creating a unique temp file
            TempFolder = Path.GetTempFileName();
            File.Delete(TempFolder);
            Directory.CreateDirectory(TempFolder);
        }

        /// <summary>
        ///     Static finalizer to delete any temp folders/files created by this type
        /// </summary>
        ~ResourceInterceptor()
        {
            try
            {
                if (null != TempFolder && Directory.Exists(TempFolder))
                {
                    Directory.Delete(TempFolder, true);
                }
            }
            catch { }
        }

        /// <summary>
        ///     Optionally blocks any web browser requests by returning true.  Not used.
        /// </summary>
        /// <remarks>
        ///     This method can implement a whitelist of allowed URLs here by
        ///     returning true to block any whitelist misses
        /// </remarks>
        public virtual bool OnFilterNavigation(NavigationRequest request)
        {
            return false;
        }

        /// <summary>
        ///     Intercepts any requests for the EmbeddedResourceDomain base Uri,
        ///     and returns a response using the embedded resource in this app's assembly/DLL file
        /// </summary>
        public virtual ResourceResponse OnRequest(ResourceRequest request)
        {
            ResourceResponse response = null;

            // log the request to the debugger output
            System.Diagnostics.Debug.Print(String.Concat(request.Method, ' ', request.Url.ToString()));

            if (IsEmbeddedResource(request))
            {
                response = CreateResponseFromResource(request);
            }

            return response;
        }

        /// <summary>
        ///     Determines if the request is for an embedded resource
        /// </summary>
        private bool IsEmbeddedResource(ResourceRequest request)
        {
            return EmbeddedResourceDomain.IsBaseOf(request.Url);
        }

        /// <summary>
        ///     Creates a response using the contents of an embedded assembly resource.
        /// </summary>
        private ResourceResponse CreateResponseFromResource(ResourceRequest request)
        {
            string resourceName;
            string filePath;

            // this project embeds static HTML/JS/CSS/PNG files as resources
            // by translating the resource's relative file path like Resources\foo/bar.html
            // to a logical name like /www/foo/bar.html
            resourceName = String.Concat("www", request.Url.AbsolutePath);
            filePath = Path.GetFullPath(Path.Combine(TempFolder, resourceName.Replace('/', Path.DirectorySeparatorChar)));

            // cache the resource to a temp file if 
            if (!File.Exists(filePath))
            {
                ExtractResourceToFile(resourceName, filePath);
            }

            return ResourceResponse.Create(filePath);
        }

        /// <summary>
        ///   Extracts an assembly resource to a temporary file on disk.
        ///   This avoids with pinning a managed byte array from GC reallocation in multi-threaded code.
        /// </summary>
        /// <remarks>
        ///     While we could just use the `ResourceResponse.Create(uint NumBytes, IntPtr buffer, string mimeType)`
        ///     overload to read a resource directly into memory, this could create headaches with needing to pin
        ///     the byte array buffer in memory so that the GC can't move it.
        ///     This can be challenging with value types like bytes in multi-threaded apps.
        ///     
        ///     It also eliminates the need to deal with determining the file type to mime type mapping
        ///     in .Net 4.0 Client Profile installations.
        ///     
        ///     See http://www.hanselman.com/blog/BackToBasicsEveryoneRememberWhereWeParkedThatMemory.aspx
        /// </remarks>
        private void ExtractResourceToFile(string resourceName, string filePath)
        {
            string parentPath;

            // the embedded resources start with a '/' char in this project
            resourceName = String.Concat('/', resourceName);

            parentPath = Directory.GetParent(filePath).FullName;
            if (!Directory.Exists(parentPath))
            {
                Directory.CreateDirectory(parentPath);
            }

            using (Stream inputStream = AppAssembly.GetManifestResourceStream(resourceName))
            using (FileStream outputStream = new FileStream(filePath, FileMode.Create))
            {
                inputStream.CopyTo(outputStream);
                outputStream.Close();
            }
        }
    }
}
