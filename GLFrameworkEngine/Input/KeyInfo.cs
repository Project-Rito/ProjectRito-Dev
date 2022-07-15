using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Input;

namespace GLFrameworkEngine
{
    public class KeyInfo
    {
        public static KeyEventInfo EventInfo { get; set; } = new KeyEventInfo();
    }
    public class KeyEventInfo
    {
        public KeyEventInfo LastState { get; set; }

        public List<string> KeyChars { get; set; } = new List<string>();

        public bool HasKeyDown() => KeyChars.Count > 0;

        public bool KeyShift { get; set; }
        public bool KeyCtrl { get; set; }
        public bool KeyAlt { get; set; }

        public bool IsKeyDown(string key)
        {
            string[] keys = key.Split('+');
            if (!KeyChars.Contains(keys.Last()) && keys.Last() != "Ctrl" && keys.Last() != "Shift" && keys.Last() != "Alt")
                return false;
            if (keys.Contains("Ctrl") != KeyCtrl && (!keys.Contains("!Ctrl") != KeyCtrl))
                return false;
            if (keys.Contains("Shift") != KeyShift && (!keys.Contains("!Shift") != KeyShift))
                return false;
            if (keys.Contains("Alt") != KeyAlt && (!keys.Contains("!Alt") != KeyAlt))
                return false;
            return true;
        }
    }
}
