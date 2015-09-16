using Awesomium.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace BzoneLauncher
{
    public class ParseException : Exception {
        public ParseException(string file, string message)
            : base(message)
        {
            this.Source = file;
        }
    }

    public class LauncherItemCollection
    {
        private string file;
        //private List<LauncherItem> Items;
        private Dictionary<string, LauncherItem> Items;
        private List<string> KeySort;

        public LauncherItemCollection(string file)
        {
            this.file = file;
            if (!File.Exists(file))
            {
                File.WriteAllText(file, "[]");
            }
            JToken LauncherItemData = JToken.Parse(File.ReadAllText(file));
            if (LauncherItemData.Type == JTokenType.Array)
            {
                //Items = new List<LauncherItem>();
                Items = new Dictionary<string, LauncherItem>();
                KeySort = new List<string>();

                JArray fileArray = (JArray)LauncherItemData;
                foreach (JToken fileItem in fileArray)
                {
                    // TODO actually processing here
                    //Console.WriteLine(fileItem.ToString());

                    JObject obj = (JObject)fileItem;
                    JProperty prop = obj.Property("Type");
                    switch (prop.Value.ToString())
                    {
                        case "3rdParty":
                            //Items.Add(obj.ToObject<ThirdPartyLauncherItem>());
                            {
                                ThirdPartyLauncherItem launcherItem = obj.ToObject<ThirdPartyLauncherItem>();
                                Items.Add(launcherItem.GetID(), launcherItem);
                                KeySort.Add(launcherItem.GetID());
                            }
                            break;
                        default:
                            Console.WriteLine("Unknown Launchable Type {0}", prop.Value.ToString());
                            break;
                    }
                }
            }
            else
            {
                throw new ParseException(file, "Expected Array, found " + LauncherItemData.Type + ".");
            }
        }

        private void Save()
        {
            //JObject obj = new JObject();
            //obj["sort"] = JArray.FromObject(KeySort);
            //obj["items"] = JArray.FromObject(Items.Values);
            //File.WriteAllText(file, obj.ToString());

            File.WriteAllText(file, JsonConvert.SerializeObject(Items
                .OrderBy(dr => { int idx = KeySort.IndexOf(dr.Key); return idx >= 0 ? idx : 9999; })
                .ThenBy(dr => dr.Value.GetName())
                .Select(dr => dr.Value)
                .ToArray()));
        }

        public void Add3rdParty(string Name, string Path, string Flags)
        {
            ThirdPartyLauncherItem newItem = new ThirdPartyLauncherItem(Name, Path, Flags);
            Items.Add(newItem.GetID(), newItem);

            Save();
        }

        public bool AddInstance(string version, string name)
        {
            Console.WriteLine("{0}\t{1}", version, name);
            return true;
        }

        public JSValue ToAwesomiumJavaObject()
        {
            JSValue val = (JSValue)Items
                .OrderBy(dr => { int idx = KeySort.IndexOf(dr.Key); return idx >= 0 ? idx : 9999; })
                .ThenBy(dr => dr.Value.GetName())
                .Select(dr =>
            {
                JSObject Item = dr.Value.ToAwesomiumJavaObject();
                return new JSValue(Item);
            }).ToArray();
            return val;
        }

        public JSValue Activate(string ID)
        {
            LauncherItem Item = Items[ID];
            return Item.Activate();
        }

        public void Sort(string[] sortArray)
        {
            KeySort = sortArray.ToList();

            Save();
        }
    }

    interface LauncherItem
    {
        string GetID();
        string GetName();
        JSValue ToAwesomiumJavaObject();
        //string GetLauncherType();

        JSValue Activate();
    }

    [DataContract]
    class ThirdPartyLauncherItem : LauncherItem
    {
        [DataMember]
        string ID;
        [DataMember]
        string Type;
        [DataMember]
        string Name;
        [DataMember]
        string Path;
        [DataMember]
        string Flags;
        [DataMember]
        bool Minimize;

        public ThirdPartyLauncherItem(string Name, string Path, string Flags)
        {
            this.Name = Name;
            this.Path = Path;
            this.Flags = Flags;
            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] data = Encoding.UTF8.GetBytes("shortcut " + Path + "\0" + Flags);
            this.ID = string.Concat(sha.ComputeHash(data).Select(x => x.ToString("x2")));
            this.Type = "3rdParty";
            this.Minimize = true;
        }

        public JSValue ToAwesomiumJavaObject()
        {
            JSObject Entry = new JSObject();
            Entry["ID"] = ID;
            Entry["Type"] = Type;
            Entry["Name"] = Name;
            Entry["Path"] = Path;
            Entry["Flags"] = Flags;
            Entry["Minimize"] = Minimize;
            return new JSValue(Entry);
        }

        public string GetID()
        {
            return ID;
        }

        public string GetName()
        {
            return Name;
        }

        //public string GetLauncherType()
        //{
        //    return Type;
        //}

        public JSValue Activate()
        {
            ProcessStartInfo info = new ProcessStartInfo(Path, Flags);
            Process.Start(info);
            JSObject obj = new JSObject();
            obj["Minimize"] = Minimize;
            return new JSValue(obj);
        }
    }

    /*class BattlezoneInstall : LauncherItem
    {
        public int Status;
        public string Version;
        public string[] Base;
        public string[] Assets;
        public BattlezoneInstallOptions[] Optional;
    }

    class BattlezoneInstallOptions
    {
        public string Name;
        public BattlezoneInstallOptionItem[] Data;
    }

    class BattlezoneInstallOptionItem
    {
        public string Name;
        public string[] Files;
    }*/
}
