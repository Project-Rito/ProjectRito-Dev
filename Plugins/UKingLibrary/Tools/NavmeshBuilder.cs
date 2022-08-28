using GLFrameworkEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using MapStudio.UI;
using HKX2;
using HKX2Builders;
using OpenTK.Graphics;

namespace UKingLibrary
{
    public class NavmeshBuilder
    {
        private static IMapLoader ActiveLoader
        {
            get
            {
                return UKingEditor.ActiveUkingEditor.ActiveMapLoader;
            }
        }

        public static void Build()
        {
            DrawingHelper.VerticesIndices<Vector3> mergedMesh = GetMergedMesh();

            var navmesh = hkaiNavMeshBuilder.Build(UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig, mergedMesh.Vertices.Select(v => new System.Numerics.Vector3(v.X, v.Y, v.Z)).ToList(), mergedMesh.Indices);


            ActiveLoader.Navmesh[0].Replace(navmesh);
        }

        /// <summary>
        /// Gets a simple merged mesh from all the collision data in the current loader/scene.
        /// </summary>
        private static DrawingHelper.VerticesIndices<Vector3> GetMergedMesh()
        {
            DrawingHelper.VerticesIndices<Vector3> res = new DrawingHelper.VerticesIndices<Vector3>();
            int vtxBase = 0;
            foreach (var render in ActiveLoader.BakedCollision[0].ShapeRenders)
            {
                Matrix4 renderTransform = render.Transform.TransformMatrix;
                renderTransform.Transpose();

                Matrix4 navmeshTransform = Matrix4.CreateTranslation(ActiveLoader.Navmesh[0].Origin * GLContext.PreviewScale);
                navmeshTransform.Transpose();

                res.Vertices.AddRange(render.Vertices.Select(x => (navmeshTransform.Inverted() * renderTransform * new Vector4(x.Position, 1f)).Xyz / GLContext.PreviewScale));

                foreach (int index in render.Indices)
                {
                    res.Indices.Add(index + vtxBase);
                }

                vtxBase += render.Vertices.Length;
            }

            return res;
        }
    }
}
