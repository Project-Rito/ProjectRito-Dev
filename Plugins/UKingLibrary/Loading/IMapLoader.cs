using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using Toolbox.Core.ViewModels;

namespace UKingLibrary
{
    public interface IMapLoader : IDisposable
    {
        public UKingEditor ParentEditor { get; set; }
        public GLScene Scene { get; set; }
        public List<MapData> MapData { get; set; }
        public NodeFolder RootNode { get; set; }
    }
}
