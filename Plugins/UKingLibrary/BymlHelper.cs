using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UKingLibrary
{
    public class BymlHelper
    {
        public static void SetValue(IDictionary<string, dynamic> properties, string key, dynamic value)
        {
            if (properties.ContainsKey(key))
                properties[key] = value;
            else
                properties.Add(key, value);
        }
    }
}
