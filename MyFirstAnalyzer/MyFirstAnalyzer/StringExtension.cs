using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFirstAnalyzer
{
    static class StringExtension
    {
        public static string ToUpperLead(this string value)
        {
            string result = null;

            if (value != null)
            {
                char[] chars = value.ToCharArray();
                chars[0] = char.ToUpper(chars[0]);
                result = new string(chars);
            }

            return result;
        }
    }
}
