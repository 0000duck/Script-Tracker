using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Script_Tracker
{
    class Script
    {
        public Script(int id, string name, string author, List<string> publicdata, List<DateTime> realtimepings)
        {
            ID = id;
            Name = name;
            Author = author;
            PublicData = publicdata;
            RealTimePings = realtimepings;
        }
        public int ID = -1;
        public string Name = null;
        public string Author = null;
        public List<string> PublicData = new List<string>();
        public Dictionary<string, KeyValuePair<int, DateTime>> FloodControl = new Dictionary<string, KeyValuePair<int, DateTime>>();
        public List<DateTime> RealTimePings = new List<DateTime>();
    }
}
