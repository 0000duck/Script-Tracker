using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Script_Tracker
{
    class IRCBot
    {
        public Socket Connection;
        public static char actionchr = (char)0x01;
        public DateTime CollectCooldown = DateTime.Now;

        public const char C_S_BOLD = (char)0x02;
        public const char C_S_COLOR = (char)0x03;
        public const char C_S_NORMAL = (char)0x0F;
        public const char C_S_UNDERLINE = (char)0x1F;
        public static string S_UNDERLINE = C_S_UNDERLINE.ToString();
        public static string S_NORMAL = C_S_NORMAL.ToString();
        public static string S_BOLD = C_S_BOLD.ToString();
        public static string S_WHITE = C_S_COLOR.ToString() + "00";
        static string S_BLACK = C_S_COLOR.ToString() + "01";
        static string S_DARKBLUE = C_S_COLOR.ToString() + "02";
        static string S_GREEN = C_S_COLOR.ToString() + "03";
        static string S_RED = C_S_COLOR.ToString() + "04";
        static string S_BROWN = C_S_COLOR.ToString() + "05";
        static string S_PURPLE = C_S_COLOR.ToString() + "06";
        static string S_ORANGE = C_S_COLOR.ToString() + "07";
        static string S_YELLOW = C_S_COLOR.ToString() + "08";
        static string S_LIME = C_S_COLOR.ToString() + "09";
        static string S_CYAN = C_S_COLOR.ToString() + "10";
        static string S_BLUE = C_S_COLOR.ToString() + "11";
        static string S_DARKCYAN = C_S_COLOR.ToString() + "12";
        static string S_MAGENTA = C_S_COLOR.ToString() + "13";
        static string S_DARKGRAY = C_S_COLOR.ToString() + "14";
        static string S_GRAY = C_S_COLOR.ToString() + "15";

        public void Start(string ip, int port, string chan)
        {
            Connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Connection.Connect(ip, port);
            SendCommand("USER", "script-tracker blackcoyote.org blackcoyote.org :script-tracker");
            SendCommand("NICK", "script-tracker");
            string receivedAlready = string.Empty;
            while (true)
            {
                long timePassed = 0;
                bool pinged = false;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (Connection.Available <= 0)
                {
                    sw.Stop();
                    timePassed += sw.ElapsedMilliseconds;
                    if (timePassed > 60 * 1000 && !pinged)
                    {
                        SendCommand("PING", new Random().Next(10000).ToString());
                        pinged = true;
                    }
                    if (timePassed > 120 * 1000)
                    {
                        throw new Exception("Ping timed out!");
                    }
                    sw.Reset();
                    sw.Start();
                    Thread.Sleep(1);
                }
                int avail = Connection.Available;
                byte[] receivedNow = new byte[avail > 1024 ? 1024 : avail];
                Connection.Receive(receivedNow, receivedNow.Length, SocketFlags.None);
                string got = UTF8.GetString(receivedNow).Replace("\r", "");
                receivedAlready += got;
                while (receivedAlready.Contains('\n'))
                {
                    int index = receivedAlready.IndexOf('\n');
                    string message = receivedAlready.Substring(0, index);
                    receivedAlready = receivedAlready.Substring(index + 1);
                    List<string> data = message.Split(' ').ToList();
                    string user = "";
                    string command = data[0];
                    if (command.StartsWith(":"))
                    {
                        user = command.Substring(1);
                        data.RemoveAt(0);
                        if (data.Count > 0)
                        {
                            command = data[0];
                            data.RemoveAt(0);
                        }
                        else
                        {
                            command = "null";
                        }
                    }
                    switch (command.ToLowerFast())
                    {
                        case "376":
                            SendCommand("JOIN", chan);
                            break;
                        case "ping":
                            SendCommand("PONG", data.Count > 0 ? data[1] : null);
                            break;
                        case "privmsg":
                            string channel = data[0].ToLower();
                            data[1] = data[1].Substring(1);
                            string privmsg = StringHelpers.Concat(data, 1);
                            bool isPM = !channel.StartsWith("#");
                            if (isPM && privmsg == actionchr + "VERSION" + actionchr)
                            {
                                Notice(user.Substring(0, user.IndexOf('!')), actionchr.ToString() + "VERSION " + "eleven" + actionchr.ToString());
                            }
                            else if (isPM && privmsg.StartsWith(actionchr + "PING "))
                            {
                                Notice(user.Substring(0, user.IndexOf('!')), privmsg);
                            }
                            if (privmsg.StartsWith("+"))
                            {
                                switch (privmsg.Substring(1).Before(" ").ToLowerFast())
                                {
                                    case "how":
                                    case "howto":
                                        Sendchat(S_DARKBLUE + "To start listing your own script, simply follow the explanation on http://stats.denizenscript.com/howto", channel);
                                        break;
                                    case "halp":
                                    case "help":
                                        Sendchat(S_DARKBLUE + "I know of these commands: +popular, +script <script>, +author <author>, +howto, and +help", channel);
                                        break;
                                    case "pop":
                                    case "popular":
                                        {
                                            List<KeyValuePair<Script, int>> popular = Program.getpopular(5);
                                            StringBuilder result = new StringBuilder();
                                            result.Append(S_DARKBLUE + "Popular scripts: ");
                                            int i = 0;
                                            foreach (KeyValuePair<Script, int> pair in popular)
                                            {
                                                i++;
                                                result.Append(S_DARKBLUE + i + ". " + S_CYAN + pair.Key.Name + " " + S_GRAY + "(" + pair.Value + ")");
                                                if (i < popular.Count - 1)
                                                {
                                                    result.Append(S_DARKBLUE + ", ");
                                                }
                                                else if (i < popular.Count)
                                                {
                                                    result.Append(S_DARKBLUE + ", and ");
                                                }
                                                else
                                                {
                                                    result.Append(S_DARKBLUE + " ");
                                                }
                                            }
                                            result.Append(S_DARKBLUE + "- http://stats.denizenscript.com/popular");
                                            Sendchat(S_DARKBLUE + result.ToString(), channel);
                                            break;
                                        }
                                    case "script":
                                    case "s":
                                        {
                                            string search = privmsg.After(" ");
                                            if (string.IsNullOrWhiteSpace(search))
                                            {
                                                Sendchat(S_DARKBLUE + "No script specified!", channel);
                                                break;
                                            }
                                            Script script = Program.GetScript(search);
                                            if (script == null)
                                            {
                                                Sendchat(S_DARKBLUE + "No info found on this script.", channel);
                                                break;
                                            }
                                            DateTime timestamp = DateTime.Now.ToUniversalTime().AddHours(-1);
                                            string fileID = Program.GetFileIDForTimestamp(timestamp);
                                            int servers = Program.getlog(fileID).GetKeys(timestamp.Hour + "." + script.ID).Count;
                                            KeyValuePair<Script, int> indexsearch = new KeyValuePair<Script, int>(script, servers);
                                            int rank = Program.sortedscripts.IndexOf(indexsearch) + 1;
                                            Sendchat(S_DARKBLUE + "Script: " + S_CYAN + script.Name + S_DARKBLUE + " - Author: " + S_CYAN + script.Author + S_DARKBLUE + " - Rank: " + S_CYAN + rank +
                                                S_DARKBLUE + " - Servers: " + S_CYAN + servers + S_DARKBLUE +
                                                " - Link: http://one.denizenscript.com/denizen/repo/entry/" + script.ID, channel);
                                            break;
                                        }
                                    case "r":
                                    case "rank":
                                        {
                                            string search = privmsg.After(" ").Trim();
                                            int result;
                                            if (int.TryParse(search, out result))
                                            {
                                                if (Program.sortedscripts.Count < result || result < 1)
                                                {
                                                    Sendchat(S_DARKBLUE + "There aren't this many scripts!", channel);
                                                }
                                                else
                                                {
                                                    Script script = Program.getpopular(999)[result - 1].Key;
                                                    DateTime timestamp = DateTime.Now.ToUniversalTime().AddHours(-1);
                                                    string fileID = Program.GetFileIDForTimestamp(timestamp);
                                                    int servers = Program.getlog(fileID).GetKeys(timestamp.Hour + "." + script.ID).Count;
                                                    Sendchat(S_DARKBLUE + "Script: " + S_CYAN + script.Name + S_DARKBLUE + " - Author: " + S_CYAN + script.Author + S_DARKBLUE + " - Rank: " + S_CYAN + result +
                                                        S_DARKBLUE + " - Servers: " + S_CYAN + servers + S_DARKBLUE +
                                                        " - Link: http://one.denizenscript.com/denizen/repo/entry/" + script.ID, channel);
                                                }
                                            }
                                            else
                                            {
                                                Sendchat(S_DARKBLUE + "This is not a valid rank!", channel);
                                            }
                                            break;
                                        }
                                    case "author":
                                    case "auth":
                                    case "au":
                                    case "a":
                                        {
                                            string search = privmsg.After(" ").Trim();
                                            if (string.IsNullOrWhiteSpace(search))
                                            {
                                                Sendchat(S_DARKBLUE + "No author specified!", channel);
                                                break;
                                            }
                                            List<Script> scripts = Program.getScriptsByAuthor(search);
                                            if (scripts.Count == 0)
                                            {
                                                Sendchat(S_DARKBLUE + "No info found on this author.", channel);
                                                break;
                                            }
                                            StringBuilder result = new StringBuilder();
                                            int i = 0;
                                            int max = 10;
                                            if (scripts.Count < max)
                                            {
                                                max = scripts.Count;
                                            }
                                            foreach (Script script in scripts)
                                            {
                                                i++;
                                                result.Append(S_CYAN + script.Name);
                                                if (i < max - 1)
                                                {
                                                    result.Append(S_DARKBLUE + ", ");
                                                }
                                                else if (i < max)
                                                {
                                                    result.Append(S_DARKBLUE + ", and ");
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }
                                            string end;
                                            if (scripts.Count > max)
                                            {
                                                int remainder = scripts.Count - max;
                                                end = S_GRAY + " (and " + remainder + " more!)";
                                            }
                                            else
                                            {
                                                end = ".";
                                            }
                                            Sendchat(S_DARKBLUE + search + " has written " + scripts.Count + " script(s): " + result + end, channel);
                                            break;
                                        }
                                    case "collect":
                                        {
                                            if (DateTime.Now.Subtract(CollectCooldown).TotalMinutes < 10)
                                            {
                                                Sendchat(S_DARKBLUE + "I already did this too recently!" + S_GRAY + " (10m cooldown)", channel);
                                                break;
                                            }
                                            CollectCooldown = DateTime.Now;
                                            Task.Factory.StartNew(() =>
                                            {
                                                Sendchat(S_DARKBLUE + "Reloading database from the script repository...", channel);
                                                KeyValuePair<int, int> result = Program.LoadDatabase();
                                                int amount = result.Key;
                                                int authors = result.Value;
                                                Sendchat(S_DARKBLUE + "Reloaded database. I now know of " + S_CYAN + amount + S_DARKBLUE + " scripts from " 
                                                    + S_CYAN + authors + S_DARKBLUE + " authors.", channel);
                                            });
                                            break;
                                        }
                                    case "status":
                                        {
                                            int uptime = (int)Math.Floor(DateTime.UtcNow.Subtract(Program.StartTime).TotalMinutes);
                                            int ActiveScripts = 0;
                                            int pings = 0;
                                            foreach (Script script in Program.ScriptTable)
                                            {
                                                int scriptpings = Program.GetRTservers(script);
                                                if (scriptpings > 0)
                                                {
                                                    ActiveScripts++;
                                                    pings += scriptpings;
                                                }
                                            }
                                            Sendchat(S_DARKBLUE + "Uptime: " + S_CYAN + uptime + " min " + S_DARKBLUE + "- Scripts: " + S_CYAN + Program.ScriptTable.Count
                                                + S_DARKBLUE + " - Active Scripts: " + S_CYAN + ActiveScripts + S_DARKBLUE + " - Pings Per Hour: " + S_CYAN + pings, channel);
                                            break;
                                        }
                                    case "search":
                                        {
                                            string tagsearch = privmsg.ToLowerFast().After("+search ");
                                            if (String.IsNullOrWhiteSpace(tagsearch))
                                            {
                                                Sendchat(S_DARKBLUE + "No tags were specified!", channel);
                                            }
                                            string[] tags = tagsearch.SplitFast(' ');
                                            Dictionary<Script, int> matches = new Dictionary<Script, int>();
                                            foreach (Script script in Program.ScriptTable)
                                            {
                                                foreach (String tag in tags)
                                                {
                                                    if (script.Tags.Contains(tag))
                                                    {
                                                        if (matches.ContainsKey(script))
                                                        {
                                                            matches[script]++;
                                                        }
                                                        else
                                                        {
                                                            matches.Add(script, 1);
                                                        }
                                                    }
                                                }
                                            }
                                            List<KeyValuePair<Script, int>> matchlist = matches.ToList();
                                            if (matchlist.Count == 0)
                                            {
                                                Sendchat(S_DARKBLUE + "No scripts were found with the tags '" + S_CYAN + tagsearch + "'.", channel);
                                                break;
                                            }
                                            matchlist.Sort((one, two) => two.Value.CompareTo(one.Value));
                                            StringBuilder result = new StringBuilder();
                                            int i = 0;
                                            int max = 5;
                                            if (matchlist.Count < max)
                                            {
                                                max = matchlist.Count;
                                            }
                                            foreach (KeyValuePair<Script, int> match in matchlist)
                                            {
                                                Script script = match.Key;
                                                i++;
                                                result.Append(S_CYAN + script.Name);
                                                if (i < max - 1)
                                                {
                                                    result.Append(S_DARKBLUE + ", ");
                                                }
                                                else if (i < max)
                                                {
                                                    result.Append(S_DARKBLUE + ", and ");
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }
                                            Sendchat(S_DARKBLUE + "Possible results: " + result + ".", channel);
                                            break;
                                        }
                                }
                              //  Sendchat(S_DARKBLUE + "yo", channel);
                            }
                            break;
                    }
                }
            }
        }
        Object SocketLocker = new Object();
        public static Encoding UTF8 = new UTF8Encoding(false);
        public void SendCommand(string command, string data)
        {
            lock (SocketLocker)
            {
                if (string.IsNullOrWhiteSpace(data))
                {
                    Connection.Send(UTF8.GetBytes(command.ToUpper() + "\n"));
                } 
                else
                {
                    Connection.Send(UTF8.GetBytes(command.ToUpper() + " " + data + "\n"));
                }
            }
        }
        public void Notice(string user, string data)
        {
            SendCommand("NOTICE", user + " :" + data);
        }
        public void Sendchat(string chat, string channel)
        {
            SendCommand("PRIVMSG", channel + " :" + chat);
        }
    }
}
