﻿using GLFrameworkEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
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
        private const float TILE_WIDTH = 250f;

        public static void Prepare(string savePath)
        {
            for (int xIndex = 0; xIndex < 40; xIndex++)
            {
                for (int zIndex = 0; zIndex < 32; zIndex++)
                {
                    string fileName = $"{xIndex}-{zIndex}.shknm2";

                    foreach (string fieldName in GlobalData.FieldNames)
                    {
                        MapNavmeshLoader loader = new MapNavmeshLoader();
                        loader.Load(File.OpenRead(PluginConfig.GetContentPath($"NavMesh/{fieldName}/{fileName}")), fileName, Vector3.Zero);
                        loader.ClearStreamingSets();

                        string filePath = Path.Join(savePath, $"content/NavMesh/{fieldName}/{fileName}");

                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        loader.Save(File.Create(filePath));
                    }
                }
            }
        }

        public static void Build()
        {
            hkaiNavMeshBuilder.ClearGeometry();
            hkaiNavMeshBuilder.Configure(UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig);

            foreach (var navmeshLoader in ActiveLoader.Navmesh)
            {
                Vector3 min;
                Vector3 max;

                if (ActiveLoader is FieldMapLoader)
                {
                    min = navmeshLoader.Origin - new Vector3(25);
                    min.Y = float.MinValue;
                    max = navmeshLoader.Origin + new Vector3(250) + new Vector3(25);
                    max.Y = float.MaxValue;
                }
                else
                {
                    min = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                    max = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                }

                DrawingHelper.VerticesIndices<Vector3> mergedMesh = GetMergedMesh(navmeshLoader.Origin, min, max, 25f);

                // Visualize merged mesh
                //HavokMeshShapeRender mergedMeshDebugRender = new HavokMeshShapeRender(navmeshLoader.RootNode);
                //mergedMeshDebugRender.LoadVerticesIndices(mergedMesh, navmeshLoader.Origin);
                //GLContext.ActiveContext.Scene?.AddRenderObject(mergedMeshDebugRender);
                

                if (!hkaiNavMeshBuilder.BuildTileForMesh(mergedMesh.Vertices.Select(v => new System.Numerics.Vector3(v.X, v.Y, v.Z)).ToList(), mergedMesh.Indices, (int)(navmeshLoader.Origin.X / TILE_WIDTH), (int)(navmeshLoader.Origin.Z / TILE_WIDTH), new System.Numerics.Vector3(navmeshLoader.Origin.X, navmeshLoader.Origin.Y, navmeshLoader.Origin.Z), ActiveLoader is FieldMapLoader))
                    Console.WriteLine("Navmesh build failure!");
            }

            foreach (var navmeshLoader in ActiveLoader.Navmesh)
            {
                hkaiNavMesh navmesh = hkaiNavMeshBuilder.Build((int)(navmeshLoader.Origin.X / TILE_WIDTH), (int)(navmeshLoader.Origin.Z / TILE_WIDTH));
                hkaiStaticTreeNavMeshQueryMediator queryMediator = hkaiStaticTreeNavMeshQueryMediatorBuilder.Build(navmesh);
                hkaiDirectedGraphExplicitCost graph = hkaiDirectedGraphExplicitCostBuilder.Build(navmesh, UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig);
                hkRootLevelContainer root = new hkRootLevelContainer()
                {
                    m_namedVariants = new List<hkRootLevelContainerNamedVariant>()
                    {
                        new()
                        {
                            m_className = "hkaiNavMesh",
                            m_name = navmeshLoader.Root.m_namedVariants[0].m_name,
                            m_variant = navmesh
                        },
                        new()
                        {
                            m_className = "hkaiDirectedGraphExplicitCost",
                            m_name = navmeshLoader.Root.m_namedVariants[1].m_name,
                            m_variant = graph
                        },
                        new()
                        {
                            m_className = "hkaiStaticTreeNavMeshQueryMediator",
                            m_name = navmeshLoader.Root.m_namedVariants[2].m_name,
                            m_variant = queryMediator
                        }
                    }
                };

                navmeshLoader.Root = root;
            }

            // Streamable adjacent indices:
            // 0: -x -z
            // 1: x -z
            // 2: +x -z
            // 3 -x z
            // 4: +x z
            // 5: -x +z
            // 6: x +z
            // 7: +x +z

            if (ActiveLoader is FieldMapLoader) {
                foreach (var navmeshLoader in ((FieldMapLoader)ActiveLoader).StreamableNavmesh)
                {
                    hkaiNavMeshBuilder.AddPrebuiltTile((hkaiNavMesh)navmeshLoader.Root.m_namedVariants[0].m_variant, (int)(navmeshLoader.Origin.X / TILE_WIDTH), (int)(navmeshLoader.Origin.Z / TILE_WIDTH), new System.Numerics.Vector3(navmeshLoader.Origin.X, navmeshLoader.Origin.Y, navmeshLoader.Origin.Z));
                }

                // Update streaming sets.
                foreach (var navmeshLoader in ActiveLoader.Navmesh)
                {
                    MapNavmeshLoader[] streamables =
                    {
                        ((FieldMapLoader)ActiveLoader).AllNavmesh.Find(x => x.Origin.X == navmeshLoader.Origin.X - 250 && x.Origin.Z == navmeshLoader.Origin.Z - 250),
                        ((FieldMapLoader)ActiveLoader).AllNavmesh.Find(x => x.Origin.X == navmeshLoader.Origin.X + 000 && x.Origin.Z == navmeshLoader.Origin.Z - 250),
                        ((FieldMapLoader)ActiveLoader).AllNavmesh.Find(x => x.Origin.X == navmeshLoader.Origin.X + 250 && x.Origin.Z == navmeshLoader.Origin.Z - 250),
                        ((FieldMapLoader)ActiveLoader).AllNavmesh.Find(x => x.Origin.X == navmeshLoader.Origin.X - 250 && x.Origin.Z == navmeshLoader.Origin.Z + 000),
                        ((FieldMapLoader)ActiveLoader).AllNavmesh.Find(x => x.Origin.X == navmeshLoader.Origin.X + 250 && x.Origin.Z == navmeshLoader.Origin.Z + 000),
                        ((FieldMapLoader)ActiveLoader).AllNavmesh.Find(x => x.Origin.X == navmeshLoader.Origin.X - 250 && x.Origin.Z == navmeshLoader.Origin.Z + 250),
                        ((FieldMapLoader)ActiveLoader).AllNavmesh.Find(x => x.Origin.X == navmeshLoader.Origin.X + 000 && x.Origin.Z == navmeshLoader.Origin.Z + 250),
                        ((FieldMapLoader)ActiveLoader).AllNavmesh.Find(x => x.Origin.X == navmeshLoader.Origin.X + 250 && x.Origin.Z == navmeshLoader.Origin.Z + 250),
                    };
                    hkRootLevelContainer root = hkaiNavMeshBuilder.UpdateStreamingSets(navmeshLoader.Root, navmeshLoader.Origin.GetNumeric(), streamables.Select(x => x.Root).ToArray(), streamables.Select(x => x.Origin.GetNumeric()).ToArray(), UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig);
                    
                    // Update aaob. Not perfect - best method would be another function to do just this based on geometry. Whatever.
                    ((hkaiNavMesh)root.m_namedVariants[0].m_variant).m_aabb.m_min.X -= UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.StreamingSetSearchRadius;
                    ((hkaiNavMesh)root.m_namedVariants[0].m_variant).m_aabb.m_min.Y -= UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.StreamingSetSearchRadius;
                    ((hkaiNavMesh)root.m_namedVariants[0].m_variant).m_aabb.m_min.Z -= UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.StreamingSetSearchRadius;
                    ((hkaiNavMesh)root.m_namedVariants[0].m_variant).m_aabb.m_max.X += UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.StreamingSetSearchRadius;
                    ((hkaiNavMesh)root.m_namedVariants[0].m_variant).m_aabb.m_max.Y += UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.StreamingSetSearchRadius;
                    ((hkaiNavMesh)root.m_namedVariants[0].m_variant).m_aabb.m_max.Z += UKingEditor.ActiveUkingEditor.EditorConfig.NavmeshConfig.StreamingSetSearchRadius;


                    // Update going the other way, too.
                    for (int i = 0; i < streamables.Length; i++)
                    {
                        streamables[i].Root = hkaiNavMeshBuilder.LinkStreamingSet(streamables[i].Root, streamables[i].Origin.GetNumeric(), root, navmeshLoader.Origin.GetNumeric(), 7 - i);
                    }

                    navmeshLoader.Root = root;
                }
            }

            hkaiNavMeshBuilder.ClearGeometry();
        }

        /// <summary>
        /// Gets a simple merged mesh from all the collision data in the current loader/scene.
        /// </summary>
        private static DrawingHelper.VerticesIndices<Vector3> GetMergedMesh(Vector3 navmeshOrigin, Vector3 sampleMin, Vector3 sampleMax, float terrSamplePadding)
        {
            DrawingHelper.VerticesIndices<Vector3> res = new DrawingHelper.VerticesIndices<Vector3>();
            int vtxBase = 0;

            foreach (var collisionLoader in ActiveLoader.BakedCollision)
            {
                var boundingBox = new BoundingBox(sampleMin * GLContext.PreviewScale, sampleMax * GLContext.PreviewScale);
                foreach (var render in collisionLoader.ShapeRenders)
                {
                    if (!(render.BoundingNode.Box.IsOverlapping(boundingBox) || boundingBox.IsInside(render.BoundingNode.Box)))
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

                    vtxBase = res.Vertices.Count;
                }
            }

            if (ActiveLoader is FieldMapLoader)
            {
                foreach (var render in ((FieldMapLoader)ActiveLoader).Terrain.TerrainRenders)
                {
                    // Must be greatest to smallest index
                    SortedSet<int> removedVertices = new SortedSet<int>();
                    int[] indexMapping = Enumerable.Repeat(-1, render.Vertices.Length).ToArray();

                    for (int i = 0; i < render.Vertices.Length; i++)
                    {
                        Matrix4 renderTransform = render.Transform.TransformMatrix;
                        renderTransform.Transpose();

                        Vector4 pos = (renderTransform * new Vector4(render.Vertices[i].Position, 1f));

                        if (!(pos.X / GLContext.PreviewScale >= sampleMin.X - terrSamplePadding &&
                        pos.Y / GLContext.PreviewScale >= sampleMin.Y - terrSamplePadding &&
                        pos.Z / GLContext.PreviewScale >= sampleMin.Z - terrSamplePadding &&
                        pos.X / GLContext.PreviewScale <= sampleMax.X + terrSamplePadding &&
                        pos.Y / GLContext.PreviewScale <= sampleMax.Y + terrSamplePadding &&
                        pos.Z / GLContext.PreviewScale <= sampleMax.Z + terrSamplePadding))
                        {
                            removedVertices.Add(i);
                            continue;
                        }

                        Matrix4 navmeshTransform = Matrix4.CreateTranslation(navmeshOrigin * GLContext.PreviewScale);
                        navmeshTransform.Transpose();

                        indexMapping[i] = res.Vertices.Count;
                        res.Vertices.Add((navmeshTransform.Inverted() * pos).Xyz / GLContext.PreviewScale);
                    }

                    List<int> indices = Rendering.TerrainRender.Indices.ToList();

                    // Optimize a little bit
                    if (removedVertices.Count == render.Vertices.Length)
                        continue;

                    for (int i = 0; i < indices.Count; i += 3)
                    {
                        if (removedVertices.Contains(indices[i + 0]))
                            continue;
                        if (removedVertices.Contains(indices[i + 1]))
                            continue;
                        if (removedVertices.Contains(indices[i + 2]))
                            continue;

                        res.Indices.Add(indexMapping[indices[i + 0]]);
                        res.Indices.Add(indexMapping[indices[i + 1]]);
                        res.Indices.Add(indexMapping[indices[i + 2]]);
                    }

                    vtxBase = res.Vertices.Count;
                }
                foreach (var render in ((FieldMapLoader)ActiveLoader).Terrain.WaterRenders)
                {
                    // Must be greatest to smallest index
                    SortedSet<int> removedVertices = new SortedSet<int>();
                    int[] indexMapping = Enumerable.Repeat(-1, render.Vertices.Length).ToArray();

                    for (int i = 0; i < render.Vertices.Length; i++)
                    {
                        Matrix4 renderTransform = render.Transform.TransformMatrix;
                        renderTransform.Transpose();

                        Vector4 pos = (renderTransform * new Vector4(render.Vertices[i].Position, 1f));

                        if (!(pos.X / GLContext.PreviewScale >= sampleMin.X - terrSamplePadding &&
                        pos.Y / GLContext.PreviewScale >= sampleMin.Y - terrSamplePadding &&
                        pos.Z / GLContext.PreviewScale >= sampleMin.Z - terrSamplePadding &&
                        pos.X / GLContext.PreviewScale <= sampleMax.X + terrSamplePadding &&
                        pos.Y / GLContext.PreviewScale <= sampleMax.Y + terrSamplePadding &&
                        pos.Z / GLContext.PreviewScale <= sampleMax.Z + terrSamplePadding))
                        {
                            removedVertices.Add(i);
                            continue;
                        }

                        Matrix4 navmeshTransform = Matrix4.CreateTranslation(navmeshOrigin * GLContext.PreviewScale);
                        navmeshTransform.Transpose();

                        indexMapping[i] = res.Vertices.Count;
                        res.Vertices.Add((navmeshTransform.Inverted() * pos).Xyz / GLContext.PreviewScale);
                    }

                    List<int> indices = Rendering.WaterRender.Indices.ToList();

                    // Optimize a little bit
                    if (removedVertices.Count == render.Vertices.Length)
                        continue;

                    for (int i = 0; i < indices.Count; i += 3)
                    {
                        if (removedVertices.Contains(indices[i + 0]))
                            continue;
                        if (removedVertices.Contains(indices[i + 1]))
                            continue;
                        if (removedVertices.Contains(indices[i + 2]))
                            continue;

                        res.Indices.Add(indexMapping[indices[i + 0]]);
                        res.Indices.Add(indexMapping[indices[i + 1]]);
                        res.Indices.Add(indexMapping[indices[i + 2]]);
                    }

                    vtxBase = res.Vertices.Count;
                }
            }

            return res;
        }

        hkRootLevelContainer BuildNavmeshRoot(hkaiNavMesh navmesh, hkaiStaticTreeNavMeshQueryMediator queryMediator, hkaiDirectedGraphExplicitCost graph)
        {
            hkRootLevelContainer root = new hkRootLevelContainer()
            {
                m_namedVariants = new List<hkRootLevelContainerNamedVariant>()
            };

            var navmeshVariant = new hkRootLevelContainerNamedVariant();
            navmeshVariant.m_className = "hkaiNavMesh";
            navmeshVariant.m_name = "";
            navmeshVariant.m_variant = navmesh;

            var queryMediatorVariant = new hkRootLevelContainerNamedVariant();
            queryMediatorVariant.m_className = "hkaiStaticTreeNavMeshQueryMediator";
            queryMediatorVariant.m_name = "";
            queryMediatorVariant.m_variant = queryMediator;

            var graphVariant = new hkRootLevelContainerNamedVariant();
            graphVariant.m_className = "hkaiDirectedGraphExplicitCost";
            graphVariant.m_name = "";
            graphVariant.m_variant = graph;

            root.m_namedVariants.Add(navmeshVariant);
            root.m_namedVariants.Add(graphVariant);
            root.m_namedVariants.Add(queryMediatorVariant);

            return root;
        }
    }
}
