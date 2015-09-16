using Awesomium.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BzoneLauncher
{
    public class PatchItem
    {
        public string Version;
        public List<string> Base;
        public List<string> Assets;

        public JSValue ToAwesomiumJavaObject()
        {
            JSObject Entry = new JSObject();
            Entry["Version"] = Version;
            //Entry["Base"] = new JSValue(Base.Select(dr => { return new JSValue(dr); }).ToArray());
            //Entry["Assets"] = new JSValue(Assets.Select(dr => { return new JSValue(dr); }).ToArray());
            return new JSValue(Entry);
        }
    }

    class PatchList
    {
        private string file;
        private List<PatchItem> patches = new List<PatchItem>();

        public PatchList(string file)
        {
            this.file = file;
            if (!File.Exists(file))
            {
                File.WriteAllText(file, "[]");
            }
            JToken PatchItemData = JToken.Parse(File.ReadAllText(file));
            if (PatchItemData.Type == JTokenType.Array)
            {
                //Items = new List<LauncherItem>();
                //Items = new Dictionary<string, LauncherItem>();
                //KeySort = new List<string>();

                JArray fileArray = (JArray)PatchItemData;
                foreach (JToken fileItem in fileArray)
                {
                    // TODO actually processing here
                    //Console.WriteLine(fileItem.ToString());

                    JObject obj = (JObject)fileItem;
                    JProperty propV = obj.Property("Version");
                    JProperty propB = obj.Property("Base");
                    JProperty propA = obj.Property("Assets");

                    if (propV.Value.Type == JTokenType.String &&
                        propB.Value.Type == JTokenType.Array &&
                        propA.Value.Type == JTokenType.Array)
                    {
                        string version = propV.Value.ToString();
                        JArray arrB = (JArray)propB.Value;
                        JArray arrA = (JArray)propA.Value;

                        patches.Add(new PatchItem() {
                            Version = version,
                            Base = arrB.Select(dr => dr.Value<string>()).ToList(),
                            Assets = arrA.Select(dr => dr.Value<string>()).ToList(),
                        });
                    }
                }
            }
            else
            {
                throw new ParseException(file, "Expected Array, found " + PatchItemData.Type + ".");
            }
        }

        public JSValue ToAwesomiumJavaObject()
        {
            JSValue[] vals = patches
                .Select(dr =>
                {
                    JSObject Item = dr.ToAwesomiumJavaObject();
                    return (JSValue)Item;
                }).ToArray();
            var tmp = (JSValue)vals;
            return tmp;
        }

        public PatchItem GetData(string version)
        {
            return patches.Where(dr => dr.Version == version).First();
        }
    }
}
