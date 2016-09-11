using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Script_Tracker
{
    static class StringHelpers
    {
        public static string Before(this string input, string match)
        {
            int ind = input.IndexOf(match);
            if (ind < 0)
            {
                return input;
            }

            return input.Substring(0, ind);
        }

        public static string BeforeAndAfter(this string input, string match, out string after)
        {
            int ind = input.IndexOf(match);
            if (ind < 0)
            {
                after = "";
                return input;
            }
            after = input.Substring(ind + match.Length);
            return input.Substring(0, ind);
        }

        public static string After(this string input, string match)
        {
            int ind = input.IndexOf(match);
            if (ind < 0)
            {
                return input;
            }
            return input.Substring(ind + match.Length);
        }
        /// <summary>
        /// Rapidly converts an ASCII string to a lowercase representation.
        /// </summary>
        /// <param name="input">The original string.</param>
        /// <returns>A lowercase version.</returns>
        public static string ToLowerFast(this string input)
        {
            char[] dt = input.ToCharArray();
            for (int i = 0; i < dt.Length; i++)
            {
                if (dt[i] >= 'A' && dt[i] <= 'Z')
                {
                    dt[i] = (char)(dt[i] - ('A' - 'a'));
                }
            }
            return new string(dt);
        }
        public static string Concat(List<string> input, int start = 0)
        {
            StringBuilder outp = new StringBuilder();
            for (int i = start; i < input.Count; i++)
            {
                outp.Append(input[i]);
                if (i + 1 < input.Count)
                {
                    outp.Append(" ");
                }
            }
            return outp.ToString();
        }
        /// <summary>
        /// Quickly split a string.
        /// </summary>
        /// <param name="input">The original string.</param>
        /// <param name="splitter">What to split it by.</param>
        /// <param name="count">The maximum number of times to split it.</param>
        /// <returns>The split string pieces.</returns>
        public static string[] SplitFast(this string input, char splitter, int count = int.MaxValue)
        {
            int len = input.Length;
            int c = 0;
            for (int i = 0; i < len; i++)
            {
                if (input[i] == splitter)
                {
                    c++;
                }
            }
            c = ((c > count) ? count : c);
            string[] res = new string[c + 1];
            int start = 0;
            int x = 0;
            for (int i = 0; i < len && x < c; i++)
            {
                if (input[i] == splitter)
                {
                    res[x++] = input.Substring(start, i - start);
                    start = i + 1;
                }
            }
            res[x] = input.Substring(start);
            return res;
        }
        public static string BeforeLast(this string input, string match)
        {
            int ind = input.LastIndexOf(match);
            if (ind < 0)
            {
                return input;
            }

            return input.Substring(0, ind);
        }
    }
}




