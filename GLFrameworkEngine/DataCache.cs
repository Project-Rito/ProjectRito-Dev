using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLFrameworkEngine
{
    public class DataCache
    {
        public static ConcurrentDictionary<string, GenericRenderer> ModelCache = new ConcurrentDictionary<string, GenericRenderer>();
        public static ConcurrentDictionary<string, Dictionary<string, GenericRenderer.TextureView>> TextureCache = new ConcurrentDictionary<string, Dictionary<string, GenericRenderer.TextureView>>();
    }
}
