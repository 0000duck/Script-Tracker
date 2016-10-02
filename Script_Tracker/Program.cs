using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using YAMLHelper;
using System.IO;
using System.Threading;

namespace Script_Tracker
{
    class Program
    {
        static DateTime StartTime = new DateTime();
        static void Main(string[] args)
        {
            Console.WriteLine("Starting server.");

            StartTime = DateTime.Now.ToUniversalTime();
            LoadDatabase();

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        IRCBot bottymcbotface = new IRCBot();
                        bottymcbotface.Start("irc.frenetic.xyz", 6667, "#script-tracker");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            });
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        IRCBot bottymcbotface = new IRCBot();
                        bottymcbotface.Start("irc.esper.net", 6667, "#denizen-dev");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            });
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000 * 60 * 10);
                    try
                    {
                        int lastID = ScriptTable.Last().ID + 1;
                        string result = client.GetStringAsync("http://one.denizenscript.com/denizen/repo/entry/" + lastID).Result;
                        if (result == null)
                        {
                            continue;
                        }
                        string name = result.After("<title>").Before(" by ");
                        if ((name == "") || (name.StartsWith("Invalid paste number")))
                        {
                            continue;
                        }
                        LoadDatabase();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            });

            var listener = new HttpListener();
            //listener.Prefixes.Add("http://localhost:8099/");
            listener.Prefixes.Add("http://*:8099/");
            //listener.Prefixes.Add("http://127.0.0.1:10123/");

            listener.Start();

            while (true)
            {
                HttpListenerContext context = null;
                try
                {
                    context = listener.GetContext();
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "text/html";
                    string page = context.Request.Url.LocalPath.Before("?");
                    switch (page)
                    {
                        case "/tracker":
                            HandleTrackerInput(context);
                            break;
                        case "/scripts":
                            webPages.getScriptPage(context);
                            break;
                        case "/popular":
                            webPages.getPopularPage(context);
                            break;
                        case "/howto":
                            webPages.getHowToPage(context);
                            break;
                        case "/graph":
                            context.Response.ContentType = "image/png";
                            webPages.getGraphImage(context);
                            break;
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    if (context != null)
                    {
                        byte[] data = Encoding.UTF8.GetBytes("FAILURE! Unable to process your request."); // better 404
                        context.Response.OutputStream.Write(data, 0, data.Length);
                    }
                }
                finally
                {
                    if (context != null)
                    {
                        context.Response.OutputStream.Close();
                    }
                }
            }
        }
        static List<Script> ScriptTable = new List<Script>();
        static HttpClient client = new HttpClient();
        public static KeyValuePair<int, int> LoadDatabase()
        {
            int i = 0;
            List<string> authors = new List<string>();
            List<Script> templist = new List<Script>();
            try
            {
                while (true)
                {
                    string result = client.GetStringAsync("http://one.denizenscript.com/denizen/repo/entry/" + i).Result;
                    if (result == null)
                    {
                        throw new Exception("result is null!");
                    }
                    string name = result.After("<title>").Before(" by ");
                    if ((name == "") || (name.StartsWith("Invalid paste number")))
                    {
                        throw new Exception("No title found!");
                    }
                    string author = result.After(" by ").Before(" ");
                    List<string> publicdata = new List<string>();
                    if (result.ToLowerFast().Contains("&lt;--script-tracker--&gt;") && result.ToLowerFast().Contains("&lt;--!script-tracker--&gt;"))
                    {
                        string arguments = result.ToLowerFast().After("&lt;--script-tracker--&gt;").Before("&lt;--!script-tracker--&gt;");
                        Dictionary<string, string> arglist = new Dictionary<string, string>();
                        foreach (string argumentvalue in arguments.Split(new string[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries)) {
                            string[] split = argumentvalue.SplitFast('=', 2);
                            arglist.Add(split[0], split[1]);
                        }
                        if (arglist.ContainsKey("public_data"))
                        {
                            publicdata = new List<string>(arglist["public_data"].SplitFast(','));
                        }
                    }
                    Script script = new Script(i, name, author, publicdata);
                    templist.Add(script);
                    if (!authors.Contains(script.Author))
                    {
                        authors.Add(script.Author);
                    }
                    Console.WriteLine("loaded script: " + i);
                    i++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            KeyValuePair<int, int> output = new KeyValuePair<int, int>(i, authors.Count);
            ScriptTable = templist;
            List<KeyValuePair<Script, int>> cachesortedscripts = getpopular(999);
            return output;
        }
        public static void HandleTrackerInput(HttpListenerContext request)
        {
            if (string.IsNullOrWhiteSpace(request.Request.QueryString["script"]))
            {
                byte[] data = Encoding.UTF8.GetBytes("FAILURE! No script specified!");
                request.Response.OutputStream.Write(data, 0, data.Length);
                Console.WriteLine("REFUSED data for script: unknown, reason: no script argument.");
                return;
            }
            int ID = int.Parse(request.Request.QueryString["script"]);
            string address = request.Request.Headers["X-Forwarded-For"].ToString().Replace(".", "-");
            Script script = GetScript(ID);
            if (script == null)
            {
                byte[] data = Encoding.UTF8.GetBytes("FAILURE! This script ID does not match our database.");
                request.Response.OutputStream.Write(data, 0, data.Length);
                Console.WriteLine("REFUSED data for script: unknown, reason: inexisting script ID.");
                return;
            }
            if (!script.FloodControl.ContainsKey(address))
            {
                script.FloodControl[address] = new KeyValuePair<int, DateTime>(0, DateTime.Now);
            }
            else if ((script.FloodControl[address].Key > 5) && (DateTime.Now.Subtract(script.FloodControl[address].Value).TotalMinutes < 10))
            {
                byte[] data = Encoding.UTF8.GetBytes("FAILURE! don't force feed me!");
                request.Response.OutputStream.Write(data, 0, data.Length);
                Console.WriteLine("REFUSED data for script: " + script.ID + ", reason: spam prevention.");
                return;
            }
            else if (DateTime.Now.Subtract(script.FloodControl[address].Value).TotalMinutes > 10) {
                script.FloodControl[address] = new KeyValuePair<int, DateTime>(0, DateTime.Now);
            }
            script.FloodControl[address] = new KeyValuePair<int, DateTime>(script.FloodControl[address].Key + 1, DateTime.Now);
            DateTime timestamp = DateTime.Now.ToUniversalTime().AddMinutes(30);
            string fileID = GetFileIDForTimestamp(timestamp);
            YAMLConfiguration log = getlog(fileID);
            Console.WriteLine("Recieved data for script: " + script.ID);
            foreach (string queryKey in request.Request.QueryString.Keys)
            {
                log.Set(timestamp.Hour + "." + script.ID + "." + address + "-" + timestamp.Ticks.ToString() + "." + queryKey.ToLowerFast().Replace('.', '_'), request.Request.QueryString[queryKey].ToLowerFast());
            }
            Directory.CreateDirectory("logs/");
            File.WriteAllText("logs/" + fileID + ".yml", log.SaveToString());
            byte[] data2 = Encoding.UTF8.GetBytes("SUCCESS! We successfully recieved your data. Thank your for your contribution.");
            request.Response.OutputStream.Write(data2, 0, data2.Length);
            AddRTTrackingInput(script);
        }



        public static List<KeyValuePair<Script, List<DateTime>>> RTTracking = new List<KeyValuePair<Script, List<DateTime>>>();
        public static void AddRTTrackingInput(Script script)
        {
            DateTime timestamp = DateTime.Now.ToUniversalTime();
            foreach (KeyValuePair<Script, List<DateTime>> current in RTTracking)
            {
                if (current.Key == script)
                {
                    List<DateTime> newlist = new List<DateTime>();
                    foreach (DateTime CurrentStamp in current.Value)
                    {
                        if (timestamp.Subtract(CurrentStamp).Minutes < 60)
                        {
                            newlist.Add(CurrentStamp);
                        }
                    }
                    newlist.Add(timestamp);
                    RTTracking.Remove(current);
                    RTTracking.Add(new KeyValuePair<Script, List<DateTime>>(script, newlist));
                    return;
                }
            }
            RTTracking.Add(new KeyValuePair<Script, List<DateTime>>(script, new List<DateTime>() {timestamp}));
            return;
        }


        public static int GetRTservers(Script script)
        {
            DateTime timestamp = DateTime.Now.ToUniversalTime();
            foreach (KeyValuePair<Script, List<DateTime>> current in RTTracking)
            {
                if (current.Key == script)
                {
                    List<DateTime> newlist = new List<DateTime>();
                    foreach (DateTime CurrentStamp in current.Value)
                    {
                        if (timestamp.Subtract(CurrentStamp).Minutes < 60)
                        {
                            newlist.Add(CurrentStamp);
                        }
                    }
                    RTTracking.Remove(current);
                    RTTracking.Add(new KeyValuePair<Script, List<DateTime>>(script, newlist));
                    return newlist.Count;
                }
            }
            return 0;
        }


        static Dictionary<string, YAMLConfiguration> LoadedLogs = new Dictionary<string, YAMLConfiguration>();
        public static Script GetScript(int ID)
        {
            foreach (Script script in ScriptTable)
            {
                if (script.ID == ID)
                {
                    return script;
                }
            }
            return null;
        }
        public static Script GetScript(string search)
        {
            int searchbyint;
            if (int.TryParse(search, out searchbyint))
            {
                return GetScript(searchbyint);
            }
            else if (string.IsNullOrWhiteSpace(search))
            {
                return null;
            }
            search = search.ToLowerFast();
            Script bestmatch = null;
            foreach (Script script in ScriptTable)
            {
                string name = script.Name.ToLowerFast();
                if (name == search)
                {
                    return script;
                }
                else if (name.StartsWith(search))
                {
                    bestmatch = script;
                }
                else if (bestmatch == null && name.Contains(search))
                {
                    bestmatch = script;
                }
            }
            return bestmatch;
        }

        public static List<Script> getScriptsByAuthor(string author)
        {
            author = author.ToLowerFast();
            List<Script> scripts = new List<Script>();
            foreach (Script script in ScriptTable)
            {
                if (script.Author.ToLowerFast() == author)
                {
                    scripts.Add(script);
                }
            }
            return scripts;
        }


        public static string PublicDataAsString(Script script)
        {
            string publicdata = "";
            foreach (string datavalue in script.PublicData)
            {
                publicdata += ", " + datavalue;
            }
            if (publicdata != "")
            {
                publicdata = publicdata.Substring(2);
                publicdata = publicdata.Replace("<br>", "");
            }
            else
            {
                publicdata = "None";
            }
            return publicdata;
        }

        public static YAMLConfiguration getlog(string fileID)
        {
            YAMLConfiguration log;
            if (!LoadedLogs.ContainsKey(fileID))
            {
                if (File.Exists("logs/" + fileID + ".yml"))
                {
                    log = new YAMLConfiguration(File.ReadAllText("logs/" + fileID + ".yml"));
                }
                else
                {
                    LoadedLogs[fileID] = log = new YAMLConfiguration("");
                }
            }
            else
            {
                log = LoadedLogs[fileID];
            }
            return log;
        }


        public static List<KeyValuePair<Script, int>> sortedscripts = new List<KeyValuePair<Script, int>>();

        public static List<KeyValuePair<Script, int>> getpopular(int amount)
        {
            List<KeyValuePair<Script, int>> popular  = new List<KeyValuePair<Script, int>>();
            DateTime timestamp = DateTime.Now.ToUniversalTime();
            string fileID = GetFileIDForTimestamp(timestamp);


            if (timestamp.Subtract(StartTime).Minutes < 60)
            {
                YAMLConfiguration log = getlog(fileID);
                foreach (Script script in ScriptTable)
                {
                    int servers = log.GetKeys(timestamp.AddHours(-1).Hour + "." + script.ID).Count;
                    popular.Add(new KeyValuePair<Script, int>(script, servers));
                }
            }
            else
            {
                foreach (Script script in ScriptTable)
                {
                    int servers = GetRTservers(script);
                    popular.Add(new KeyValuePair<Script, int>(script, servers));
                }
            }

            popular.Sort((one, two) => two.Value.CompareTo(one.Value));
            sortedscripts = popular;
            return popular.GetRange(0, amount > popular.Count ? popular.Count : amount);
        }



        public static string GetFileIDForTimestamp(DateTime timestamp)
        {
            timestamp = timestamp.ToUniversalTime();
            return timestamp.Year + "-" + timestamp.Month + "-" + timestamp.Day;
        }





    }
}
