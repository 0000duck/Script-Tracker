using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using YAMLHelper;

namespace Script_Tracker
{
    class webPages
    {
        public static void getScriptPage(HttpListenerContext request)
        {
            Script script = Program.GetScript(request.Request.QueryString["script"]);
            if (script == null)
            {
                return; //add 404 page
            }
            string searchbar = Program.getsearchbar();
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
            if (!Enum.TryParse(modeinput ?? "", out mode))
            {
                mode = ModeEnum.ADD;
            }
            string graphurl = GetGraphUrl(script, days, datasearch, datavalue, mode);
            byte[] data = Encoding.UTF8.GetBytes("<!doctype html><html><head><title>Script Tracker</title></head><body>" + searchbar + "<br><p> Script: " + script.Name + "<br>Author: " + script.Author + "<br>Public data: " + publicdata + "<br><img src='" + graphurl + "'></body></html>");
            request.Response.OutputStream.Write(data, 0, data.Length);
        }




        public static void getPopularPage(HttpListenerContext request)
        {
            byte[] data = Encoding.UTF8.GetBytes("<!doctype html><html><head><title>Script Tracker</title><link rel=\"stylesheet\" href=\"https://maxcdn.bootstrapcdn.com/bootstrap/3.3.6/css/bootstrap.min.css\" integrity=\"sha384-1q8mTJOASx8j1Au+a5WDVnPi2lkFfwwEAa8hDDdjZlpLegxhjVME1fgjWPGmkzs7\" crossorigin=\"anonymous\"></head><body><div class=\"container\" style=\"margin-top: 30px\"><div class=\"row\"><div class=\"col-md-8 col-md-offset-2\"><table class=\"table table-bordered table-condensed data-table table-striped\" id=\"script-list\"><thead><tr><th class=\"text-center\">Rank</th><th class=\"text-center\">Script</th><th class=\"text-center\">Servers</th></tr></thead><tbody>");
            request.Response.OutputStream.Write(data, 0, data.Length);
            List<KeyValuePair<Script, int>> popular = Program.getpopular(999);
            int i = 0;
            foreach (KeyValuePair<Script, int> current in popular)
            {
                i++;
                Script script = current.Key;
                int servers = current.Value;
                byte[] data2 = Encoding.UTF8.GetBytes("<tr id=\"script-list-item\"> <td style=\"text-align: center;\"><b>" + i + "</b> </td> <td style=\"text-align: center;\"> <a href=\"/scripts?script=" + script.ID + "\" target=\"_blank\"><b>" + script.Name + "</b></a> </td> <td style=\"text-align: center;\"> <b>" + servers + "</b> </td> </tr>");
                request.Response.OutputStream.Write(data2, 0, data2.Length);
            }
            byte[] data3 = Encoding.UTF8.GetBytes("</tbody></table></div></div></div></body></html>");
            request.Response.OutputStream.Write(data3, 0, data3.Length);
        }








        public static string GetGraphUrl(Script script, int days, string data, string datavalue, ModeEnum mode)
        {
            data = data.ToLowerFast();
            datavalue = datavalue?.ToLowerFast();
            DateTime timestamp = DateTime.Now;
            string fileID = Program.GetFileIDForTimestamp(timestamp).ToString();
            string graphvalues = "";
            string labels = "";
            int highest = 10;
            for (int i = days - 1; i >= 0; i--)
            {
                YAMLConfiguration file = Program.getlog(Program.GetFileIDForTimestamp(timestamp.AddDays(i * -1)).ToString());
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
            return "http://neo.mcmonkey.org/graph_api/graph_line.png?title=" + script.Name + "&show_points=false&width=1000&Height=500&xtitle=Days&ytitle=Amount&ynotches=" + Math.Ceiling(highest / 10.0) + "&xnotches=1&xsteps=0.04166&xend=" + days + "&max=" + highest + "&values=" + graphvalues.Substring(1) + "&xstart=0&match_xsteps=true&xlabels=" + labels.Substring(1);
        }

    }
}
