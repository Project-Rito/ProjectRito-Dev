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
        public List<MapCollisionLoader> BakedCollision { get; set; }
        public NodeFolder RootNode { get; set; }

        public void AddBakedCollisionShape(HKX2.hkpShape shape, System.Numerics.Matrix4x4 transform, uint hashId);
    }
}
