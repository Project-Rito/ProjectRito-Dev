using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MapStudio.UI
{
    public class FileUtility
    {
        /// <summary>
        /// Loads a file with the default application in windows explorer.
        /// </summary>
        /// <param name="path"></param>
        public static void OpenWithDefaultProgram(string path)
        {
            using Process fileopener = new Process();

            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + path + "\"";
            fileopener.Start();
        }
    }
}
