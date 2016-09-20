using System;
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
    class webPages
    {
        public static void getHowToPage(HttpListenerContext request)
        {
            string HTML = ParseHTML("html/howto page.html", null);
            byte[] data = Encoding.UTF8.GetBytes(HTML);
            request.Response.OutputStream.Write(data, 0, data.Length);
        }


        public static void getScriptPage(HttpListenerContext request)
        {
            Script script = Program.GetScript(request.Request.QueryString["script"]);
            if (script == null)
            {
                return; //add 404 page
            }
            string publicdata = Program.PublicDataAsString(script);
            int days;
            if (!int.TryParse(request.Request.QueryString["days"], out days))
            {
                days = 10;
            }
            string datasearch = request.Request.QueryString["data"];
            if (string.IsNullOrWhiteSpace(datasearch))
            {
                datasearch = "servers";
            }
            string datavalue = request.Request.QueryString["datavalue"];
            if (string.IsNullOrWhiteSpace(datavalue))
            {
                datavalue = null;
            }
            string modeinput = request.Request.QueryString["mode"];
            ModeEnum mode;
            if (!Enum.TryParse(modeinput ?? "", true, out mode))
            {
                mode = ModeEnum.ADD;
            }
            string searchbar = ParseHTML("html/searchbar.html", null);
            string graphurl = GetGraphUrl(script, days, datasearch, datavalue, mode);
            Dictionary<string, string> parseArgs = new Dictionary<string, string>() {
                {"searchbar", searchbar}, {"title", script.Name}, {"author", script.Author}, {"public_data", publicdata}, {"url1", graphurl}, {"ID", script.ID.ToString()}
            };
            string HTML = ParseHTML("html/script page.html", parseArgs);
            byte[] data = Encoding.UTF8.GetBytes(HTML);
            request.Response.OutputStream.Write(data, 0, data.Length);
        }


        
        public static void getPopularPage(HttpListenerContext request)
        {
            StringBuilder tablecontents = new StringBuilder();
            List<KeyValuePair<Script, int>> popular = Program.getpopular(999);
            int i = 0;
            foreach (KeyValuePair<Script, int> current in popular)
            {
                i++;
                Script script = current.Key;
                int servers = current.Value;
                Dictionary<string, string> parseArgs = new Dictionary<string, string>() {
                    {"rank", i.ToString()}, {"title", script.Name}, {"servers", servers.ToString()}, {"ID", script.ID.ToString()}
                };
                string table = ParseHTML("html/popular table row.html", parseArgs);
                tablecontents.Append(table);
            }
            Dictionary<string, string> parseArgs2 = new Dictionary<string, string>() {
                    {"table_contents", tablecontents.ToString()}
                };
            string HTML = ParseHTML("html/popular page.html", parseArgs2);
            byte[] data = Encoding.UTF8.GetBytes(HTML);
            request.Response.OutputStream.Write(data, 0, data.Length);
        }



        public static string ParseHTML(string htmlpath, Dictionary<string, string> args)
        {
            string HTML = File.ReadAllText(htmlpath);

            if (string.IsNullOrWhiteSpace(HTML))
            {
                return null;
            }

            if (args == null)
            {
                return HTML;
            }

            string variable;
            string outcome;
            foreach (KeyValuePair<string, string> variableArgs in args)
            {
                variable = "<{" + variableArgs.Key + "}>";
                outcome = variableArgs.Value;

                HTML = HTML.Replace(variable, outcome);
            }

            return HTML;
        }




        public static void getGraphImage(HttpListenerContext request)
        {
            Script script = Program.GetScript(request.Request.QueryString["script"]);
            if (script == null)
            {
                return; //add 404 page
            }
            int days;
            if (!int.TryParse(request.Request.QueryString["days"], out days))
            {
                days = 10;
            }
            string data = request.Request.QueryString["data"];
            if (string.IsNullOrWhiteSpace(data))
            {
                data = "servers";
            }
            string datavalue = request.Request.QueryString["datavalue"];
            if (string.IsNullOrWhiteSpace(datavalue))
            {
                datavalue = null;
            }
            string modeinput = request.Request.QueryString["mode"];
            ModeEnum mode;
            if (!Enum.TryParse(modeinput ?? "", true, out mode))
            {
                mode = ModeEnum.ADD;
            }
            byte[] output = GetGraph(script, days, data, datavalue, mode);
            request.Response.OutputStream.Write(output, 0, output.Length);
        }

        public static string GetGraphUrl(Script script, int days, string data, string datavalue, ModeEnum mode)
        {
            return "https://stats.denizenscript.com/graph?script=" + script.ID + "&data=" + data + "&mode=" + mode.ToString() + "&datavalue=" + datavalue + "&days=" + days;
        }
        public static byte[] GetGraph(Script script, int days, string data, string datavalue, ModeEnum mode)
        {
            data = data.ToLowerFast();
            datavalue = datavalue?.ToLowerFast();
            DateTime timestamp = DateTime.Now.ToUniversalTime();
            string fileID = Program.GetFileIDForTimestamp(timestamp);
            List<double> graphvalues = new List<double>();
            List<string> labels = new List<string>();
            int highest = 10;
            for (int i = days - 1; i >= 0; i--)
            {
                YAMLConfiguration file = Program.getlog(Program.GetFileIDForTimestamp(timestamp.AddDays(i * -1)));
                labels.Add(timestamp.AddDays(i * -1).Day + "/" + timestamp.AddDays(i * -1).Month);
                for (int y = 0; y < 24; y++)
                {
                    if (data == "servers")
                    {
                        int amount = file.GetKeys(y + "." + script.ID).Count;
                        graphvalues.Add(amount);
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
                                double count = 0;
                                foreach (string server in file.GetKeys(y + "." + script.ID))
                                {
                                    count += file.ReadDouble(y + "." + script.ID + "." + server + "." + data, 0);
                                }
                                if (highest < count)
                                {
                                    highest = Convert.ToInt32(Math.Ceiling(count));
                                }
                                graphvalues.Add(count);
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
                                graphvalues.Add(count2);
                                break;
                            case ModeEnum.AVERAGE:
                                double count3 = 0;
                                List<string> keys = file.GetKeys(y + "." + script.ID);
                                foreach (string server in keys)
                                {
                                    count3 += file.ReadDouble(y + "." + script.ID + "." + server + "." + data, 0);
                                }
                                int keycount = keys.Count;
                                if (keycount < 1)
                                {
                                    keycount = 1;
                                }
                                count3 /= keycount;
                                if (highest < count3)
                                {
                                    highest = Convert.ToInt32(Math.Ceiling(count3));
                                }
                                graphvalues.Add(count3);
                                break;
                        }
                    }
                }
            }
            return GraphRenderer.BasicLineGraph(script.Name, graphvalues, highest, Math.Ceiling(highest / 10.0), days, 24, labels);
           // return "http://neo.mcmonkey.org/graph_api/graph_line.png?title=" + script.Name + "&show_points=false&width=1000&Height=500&xtitle=Days&ytitle=Amount&ynotches=" + Math.Ceiling(highest / 10.0) + "&xnotches=1&xsteps=0.04166&xend=" + days + "&max=" + highest + "&values=" + graphvalues.ToString().Substring(1) + "&xstart=0&match_xsteps=true&xlabels=" + labels.ToString().Substring(1);
        }

    }
}
