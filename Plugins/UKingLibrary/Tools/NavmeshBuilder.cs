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
            foreach (var navmeshLoader in ActiveLoader.Navmesh)
            {
                Vector3 min = navmeshLoader.Origin - new Vector3(250/2);
                min.Y = float.MinValue;
                Vector3 max = navmeshLoader.Origin + new Vector3(250/2);
                max.Y = float.MaxValue;
                DrawingHelper.VerticesIndices<Vector3> mergedMesh = GetMergedMesh(navmeshLoader.Origin, min, max);

                var navmesh = hkaiNavMeshBuilder.Build(UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig, mergedMesh.Vertices.Select(v => new System.Numerics.Vector3(v.X, v.Y, v.Z)).ToList(), mergedMesh.Indices);

                navmeshLoader.Replace(navmesh);
            }
        }

        /// <summary>
        /// Gets a simple merged mesh from all the collision data in the current loader/scene.
        /// </summary>
        private static DrawingHelper.VerticesIndices<Vector3> GetMergedMesh(Vector3 navmeshOrigin, Vector3 sampleMin, Vector3 sampleMax)
        {
            DrawingHelper.VerticesIndices<Vector3> res = new DrawingHelper.VerticesIndices<Vector3>();
            int vtxBase = 0;
            foreach (var collisionLoader in ActiveLoader.BakedCollision)
            {
                foreach (var render in collisionLoader.ShapeRenders)
                {
                    if (!(render.Transform.Position.X / GLContext.PreviewScale > sampleMin.X &&
                        render.Transform.Position.Y / GLContext.PreviewScale > sampleMin.Y &&
                        render.Transform.Position.Z / GLContext.PreviewScale > sampleMin.Z &&
                        render.Transform.Position.X / GLContext.PreviewScale <= sampleMax.X &&
                        render.Transform.Position.Y / GLContext.PreviewScale <= sampleMax.Y &&
                        render.Transform.Position.Z / GLContext.PreviewScale <= sampleMax.Z))
                        continue;

                    Matrix4 renderTransform = render.Transform.TransformMatrix;
                    renderTransform.Transpose();

                    Matrix4 navmeshTransform = Matrix4.CreateTranslation(navmeshOrigin * GLContext.PreviewScale);
                    navmeshTransform.Transpose();

                    res.Vertices.AddRange(render.Vertices.Select(x => (navmeshTransform.Inverted() * renderTransform * new Vector4(x.Position, 1f)).Xyz / GLContext.PreviewScale));

                    foreach (int index in render.Indices)
                    {
                        res.Indices.Add(index + vtxBase);
                    }

                    vtxBase += render.Vertices.Length;
                }
            }

            return res;
        }
    }
}
