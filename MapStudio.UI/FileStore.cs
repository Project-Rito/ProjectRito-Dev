using System;
using System.Collections.Generic;
using System.Text;

namespace MapStudio.UI
{
    public class FileStore
    {
        /// <summary>
        /// Mapping of (absolute) paths to objects that represent the files themselves.
        /// </summary>
        public Dictionary<string, object> OpenFiles = new Dictionary<string, object>();

        public T GetOrOpen<T>(string path, Func<string, T> open)
        {
            if (OpenFiles.ContainsKey(path))
                return (T)OpenFiles[path];

            OpenFiles.Add(path, open(path));
            return (T)OpenFiles[path];
        }
    }
}
