using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Service
{
    public static class UtilityFunctions
    {
        public static string CsvFormat(this string value)
        {
            return value.Contains(',') ? $"\"{value}\"" : value;
        }
    }
}
