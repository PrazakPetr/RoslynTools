using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {

            var v1 = GetObject();

            var v2 = new string[] { "one", "two" }.Select(q => new { s = q, l = q.Length});

            var v3 = GetInt();
	    
	        var v4 = new object();

            v4.Extension2().Extension1().Extension3(v3).Extension4(v1, v3); 
        }

	private static ImmutableArray<int> GetImmutableArray()
        {
            return new ImmutableArray<int>();
        }

        public static int GetObject()
        {
            return 1;
        }

        public static object GetInt()
        {
            return new object();
        }
	
    }
}
