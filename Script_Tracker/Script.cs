using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Script_Tracker
{
    class Script
    {
        public Script(int id, string name, string author, List<string> publicdata, List<DateTime> realtimepings, List<string> tags)
        {
            ID = id;
            Name = name;
            Author = author;
            PublicData = publicdata;
            RealTimePings = realtimepings;
            Tags = tags;
        }
        public int ID = -1;
        public string Name = null;
        public string Author = null;
        public List<string> PublicData = new List<string>();
        public Dictionary<string, KeyValuePair<int, DateTime>> FloodControl = new Dictionary<string, KeyValuePair<int, DateTime>>();
        public List<DateTime> RealTimePings = new List<DateTime>();
        public List<string> Tags = new List<string>();


        public static DateTime LastRankSort = DateTime.UtcNow;
        public int GetRank()
        {
            if (DateTime.UtcNow.Subtract(LastRankSort).TotalHours > 1)
            {
                Program.getpopular(999);
            }

            int rank = 0;
            foreach (KeyValuePair<Script, int> sortedscript in Program.sortedscripts)
            {
                rank++;
                if (this == sortedscript.Key)
                {
                    return rank;
                }
            }

            return rank;
        }

    }
}
