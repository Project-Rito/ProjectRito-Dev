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
        public List<MapNavmeshLoader> Navmesh { get; set; }
        public NodeFolder RootNode { get; set; }

        public void AddBakedCollisionShape(uint hashId, string muuntFileName, BakedCollisionShapeCacheable info, System.Numerics.Vector3 translation, System.Numerics.Quaternion rotation, System.Numerics.Vector3 scale);
        public void RemoveBakedCollisionShape(uint hashId);
        public bool BakedCollisionShapeExists(uint hashId);
        public bool UpdateBakedCollisionShapeTransform(uint hashId, System.Numerics.Vector3 translation, System.Numerics.Quaternion rotation, System.Numerics.Vector3 scale);

        public MapObject MapObjectByHashId(uint hashId);
    }
}
