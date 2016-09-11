﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using YAMLHelper;
using System.IO;

namespace Script_Tracker
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting server.");

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

            var listener = new HttpListener();

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
                    if (context.Request.Url.LocalPath.StartsWith("/scripts"))
                    {
                        foreach (Script script in ScriptTable)
                        {
                            byte[] data = Encoding.UTF8.GetBytes("script: " + script.ID + "\nname: " + script.Name + "\nauthor: " +
                                script.Author + "\n\n");
                            context.Response.OutputStream.Write(data, 0, data.Length);
                        }
                    }
                    else if (context.Request.Url.LocalPath.StartsWith("/tracker"))
                    {
                        HandleTrackerInput(context);
                    }
                    else if (context.Request.Url.LocalPath.StartsWith("/script"))
                    {
                        Script script = GetScript(context.Request.QueryString["script"]);
                        if (script == null)
                        {
                            continue; //add 404 page
                        }
                        string searchbar = getsearchbar();
                        string publicdata = PublicDataAsString(script);
                        int days;
                        if (!int.TryParse(context.Request.QueryString["days"], out days))
                        {
                            days = 10;
                        }
                        string datasearch = context.Request.QueryString["data"];
                        if (string.IsNullOrWhiteSpace(datasearch))
                        {
                            datasearch = "servers";
                        }
                        string datavalue = context.Request.QueryString["datavalue"];
                        if (string.IsNullOrWhiteSpace(datavalue))
                        {
                            datavalue = null;
                        }
                        string modeinput = context.Request.QueryString["mode"];
                        ModeEnum mode;
                        if (!Enum.TryParse(modeinput??"", out mode))
                        {
                            mode = ModeEnum.ADD;
                        }
                        string graphurl = GetGraphUrl(script, days, datasearch, datavalue, mode);
                        byte[] data = Encoding.UTF8.GetBytes("<!doctype html><html><head><title>Script Tracker</title></head><body>" + searchbar + "<br><p> Script: " + script.Name + "<br>Author: " + script.Author + "<br>Public data: " + publicdata + "<br><img src='" + graphurl + "'></body></html>");
                        context.Response.OutputStream.Write(data, 0, data.Length);
                    }
                    else if (context.Request.Url.LocalPath.StartsWith("/popular"))
                    {
                        byte[] data = Encoding.UTF8.GetBytes("<!doctype html><html><head><title>Script Tracker</title><link rel=\"stylesheet\" href=\"https://maxcdn.bootstrapcdn.com/bootstrap/3.3.6/css/bootstrap.min.css\" integrity=\"sha384-1q8mTJOASx8j1Au+a5WDVnPi2lkFfwwEAa8hDDdjZlpLegxhjVME1fgjWPGmkzs7\" crossorigin=\"anonymous\"></head><body><div class=\"container\" style=\"margin-top: 30px\"><div class=\"row\"><div class=\"col-md-8 col-md-offset-2\"><table class=\"table table-bordered table-condensed data-table table-striped\" id=\"script-list\"><thead><tr><th class=\"text-center\">Rank</th><th class=\"text-center\">Script</th><th class=\"text-center\">Servers</th></tr></thead><tbody>");
                        context.Response.OutputStream.Write(data, 0, data.Length);
                        List<KeyValuePair<Script, int>> popular = getpopular(999);
                        int i = 0;
                        foreach (KeyValuePair<Script, int> current in popular)
                        {
                            i++;
                            Script script = current.Key;
                            int servers = current.Value;
                            byte[] data2 = Encoding.UTF8.GetBytes("<tr id=\"script-list-item\"> <td style=\"text-align: center;\"><b>" + i + "</b> </td> <td style=\"text-align: center;\"> <a href=\"/script?script=" + script.ID + "\" target=\"_blank\"><b>" + script.Name + "</b></a> </td> <td style=\"text-align: center;\"> <b>" + servers + "</b> </td> </tr>");
                            context.Response.OutputStream.Write(data2, 0, data2.Length);
                        }
                        byte[] data3 = Encoding.UTF8.GetBytes("</tbody></table></div></div></div></body></html>");
                        context.Response.OutputStream.Write(data3, 0, data3.Length);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    if (context != null)
                    {
                        byte[] data = Encoding.UTF8.GetBytes("FAILURE! Unable to process your request.");
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
        public static KeyValuePair<int, int> LoadDatabase()
        {
            HttpClient client = new HttpClient();
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
                    if (result.Contains("&lt;--script-tracker--&gt;") && result.Contains("&lt;--!script-tracker--&gt;"))
                    {
                        string arguments = result.After("&lt;--script-tracker--&gt;").Before("&lt;--!script-tracker--&gt;");
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
            return output;
        }
        static void HandleTrackerInput(HttpListenerContext request)
        {
            if (string.IsNullOrWhiteSpace(request.Request.QueryString["script"]))
            {
                byte[] data = Encoding.UTF8.GetBytes("FAILURE! No script specified!");
                request.Response.OutputStream.Write(data, 0, data.Length);
                return;
            }
            int ID = int.Parse(request.Request.QueryString["script"]);
            string address = request.Request.RemoteEndPoint.ToString().BeforeLast(":").Replace(".", "-"); // this returns the local IP, pls2fix
            Script script = GetScript(ID);
            if (script == null)
            {
                byte[] data = Encoding.UTF8.GetBytes("FAILURE! This script ID does not match our database.");
                request.Response.OutputStream.Write(data, 0, data.Length);
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
                return;
            }
            script.FloodControl[address] = new KeyValuePair<int, DateTime>(script.FloodControl[address].Key + 1, DateTime.Now);
            DateTime timestamp = DateTime.Now;
            string fileID = GetFileIDForTimestamp(timestamp).ToString();
            YAMLConfiguration log = getlog(fileID);
            foreach (string queryKey in request.Request.QueryString.Keys)
            {
                log.Set(timestamp.Hour + "." + script.ID + "." + address + "." + queryKey, request.Request.QueryString[queryKey]);
            }
            Directory.CreateDirectory("logs/");
            File.WriteAllText("logs/" + fileID + ".yml", log.SaveToString());
            byte[] data2 = Encoding.UTF8.GetBytes("SUCCESS! We successfully recieved your data. Thank your for your contribution.");
            request.Response.OutputStream.Write(data2, 0, data2.Length);
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


        static string PublicDataAsString(Script script)
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



        public static List<KeyValuePair<Script, int>> getpopular(int amount)
        {
            List<KeyValuePair<Script, int>> popular  = new List<KeyValuePair<Script, int>>();
            DateTime timestamp = DateTime.Now.AddHours(-1);
            string fileID = GetFileIDForTimestamp(timestamp).ToString();
            YAMLConfiguration log = getlog(fileID);
            foreach (string value in log.GetKeys(timestamp.Hour.ToString()))
            {
                Script script = GetScript(int.Parse(value));
                int servers = log.GetKeys(timestamp.Hour + "." + script.ID).Count;
                popular.Add(new KeyValuePair<Script, int>(script, servers));
            }
            popular.Sort((one, two) => two.Value.CompareTo(one.Value));
            return popular.GetRange(0, amount > popular.Count ? popular.Count : amount);
        }


        static string getsearchbar()
        {
            string searchbar = "<link rel=\"stylesheet\" href=\"https://maxcdn.bootstrapcdn.com/bootstrap/3.3.6/css/bootstrap.min.css\" integrity=\"sha384-1q8mTJOASx8j1Au+a5WDVnPi2lkFfwwEAa8hDDdjZlpLegxhjVME1fgjWPGmkzs7\" crossorigin=\"anonymous\"><style>body {min-height: 2000px;padding-top: 70px;}</style><script>function dosearch() { script = document.getElementById(\"scriptsearch\").value;if (script == \"\") { alert(\"No script specified!\");return;}query = \"?script=\" + script; window.location.replace(\"/script\" + query);}</script><nav class=\"navbar navbar-default navbar-fixed-top\"><div class=\"container\"><div id=\"navbar\" class=\"navbar-collapse collapse\"><div class=\"row\"><div class=\"col-md-2\"></div><div class=\"col-md-6\"><input class=\"form-control\" type=\"text\" id=\"scriptsearch\" placeholder=\"Enter Script\"></div><div class=\"col-md-2\"><input class=\"btn btn-primary\" id=\"dosearch\" type=\"button\" value=\"Search\" onclick=\"dosearch()\"></div><div class=\"col-md-2\"></div></div></div></div></nav>";
            return searchbar;
        }



        public static long GetFileIDForTimestamp(DateTime timestamp)
        {
            timestamp = timestamp.ToUniversalTime();
            return ((timestamp.Ticks / TimeSpan.TicksPerMillisecond / 1000 / 60 / 60 / 24) * 24 * 60 * 60 * 1000);
        }



        static string GetGraphUrl(Script script, int days, string data, string datavalue, ModeEnum mode)
        {
            data = data.ToLowerFast();
            datavalue = datavalue?.ToLowerFast();
            DateTime timestamp = DateTime.Now;
            string fileID = GetFileIDForTimestamp(timestamp).ToString();
            string graphvalues = "";
            string labels = "";
            int highest = 10;
            for (int i = days-1; i >= 0; i--)
            {
                YAMLConfiguration file = getlog(GetFileIDForTimestamp(timestamp.AddDays(i * -1)).ToString());
                labels += "," + timestamp.AddDays(i * -1).Day + "/" + timestamp.AddDays(i * -1).Month;
                for (int y = 0; y < 24; y++)
                {
                    if (data == "servers")
                    {
                        int amount = file.GetKeys(y + "." + script.ID).Count;
                        graphvalues += "," + amount;
                        if (highest < amount)
                        {
                            highest = amount;
                        }
                    }
                    else
                    {
                        switch (mode)
                        {
                            case ModeEnum.ADD:
                                int count = 0;
                                foreach (string server in file.GetKeys(y + "." + script.ID))
                                {
                                    count += file.ReadInt(y + "." + script.ID + "." + server + "." + data, 0);
                                }
                                if (highest < count)
                                {
                                    highest = count;
                                }
                                graphvalues += "," + count;
                                break;
                            case ModeEnum.COUNT:
                                int count2 = 0;
                                foreach (string server in file.GetKeys(y + "." + script.ID))
                                {
                                    if (file.ReadString(y + "." + script.ID + "." + server + "." + data, null).ToLowerFast() == datavalue)
                                    {
                                        count2++;
                                    }
                                }
                                if (highest < count2)
                                {
                                    highest = count2;
                                }
                                graphvalues += "," + count2;
                                break;
                        }
                    }
                }
            }
            return "http://neo.mcmonkey.org/graph_api/graph_line.png?title=" + script.Name + "&show_points=false&width=1000&Height=500&xtitle=Days&ytitle=Amount&ynotches=" + Math.Ceiling(highest/10.0) + "&xnotches=1&xsteps=0.04166&xend=" + days + "&max=" + highest + "&values=" + graphvalues.Substring(1) + "&xstart=0&match_xsteps=true&xlabels=" + labels.Substring(1);
        }

    }
}
