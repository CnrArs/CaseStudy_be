using ConsoleApp1.ApplicaitonSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Helper
{
    public static class Ex
    {
        public static string ToQueryString(this List<Keys> nvc)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var key in nvc)
            {
                sb.AppendFormat("&{0}={1}", Uri.EscapeDataString(key.Key), Uri.EscapeDataString(key.Value));
            }

            return sb.ToString();
        }
    }
}
