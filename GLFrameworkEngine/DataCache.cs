using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLFrameworkEngine
{
    public class DataCache
    {
        public static Dictionary<string, GenericRenderer> ModelCache = new Dictionary<string, GenericRenderer>();
        public static Dictionary<string, Dictionary<string, GenericRenderer.TextureView>> TextureCache = new Dictionary<string, Dictionary<string, GenericRenderer.TextureView>>();
    }
}
