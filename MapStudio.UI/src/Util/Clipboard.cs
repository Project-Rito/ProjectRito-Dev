using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MapStudio.UI
{
    public static class Clipboard
    {
        public static void Copy(string val)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                $"echo {val} | clip".Bat();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                $"echo {val} | clip".Bat();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                $"echo \"{val}\" | pbcopy".Bash();
            }
        }
    }
}
