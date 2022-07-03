using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nintendo.Byml;

namespace UKingLibrary
{
    public class BymlHelper
    {
        public static void SetValue(BymlNode properties, string key, BymlNode value)
        {
            if (properties.Hash.ContainsKey(key))
                properties.Hash[key] = value;
            else
                properties.Hash.Add(key, value);
        }
    }
}
