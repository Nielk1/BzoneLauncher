using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awesomium.Core;

namespace BzoneLauncher
{
    class LauncherResourceInterceptor : IResourceInterceptor
    {
        public bool OnFilterNavigation(NavigationRequest request)
        {
            //Console.WriteLine("OnFilterNavigation:\t" + request.Url);
            //Console.WriteLine("OnFilterNavigation:\t" + request.Url.Scheme);
            //if (request.Url.Scheme != "asset") return true;
            return false;
        }

        public ResourceResponse OnRequest(ResourceRequest request)
        {
            /*Console.WriteLine("OnRequest:\t" + request.Url);
            if (request.Url.Host == "home")
            {
                
                return ResourceResponse.Create("/Core/home.html");
            }*/
            return null;
        }
    }
}
