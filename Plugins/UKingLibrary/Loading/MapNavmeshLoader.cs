using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Toolbox.Core.IO;
using Toolbox.Core.ViewModels;
using OpenTK;
using HKX2;
using HKX2Builders;
using HKX2Builders.Extensions;
using GLFrameworkEngine;
using ImGuiNET;

/*
 THE STORY OF THE BORED PROGRAMMER Pt 2

    Once upon a time navmesh needed to be added.
 */

namespace UKingLibrary
{
    public class MapNavmeshLoader
    {
        public NodeBase RootNode;

        public Vector3 Origin;

        private string Prefix;

        private HKXHeader Header = HKXHeader.BotwWiiu(); // TODO - actually get the right platform
        private hkRootLevelContainer _root;
        public hkRootLevelContainer Root
        {
            get
            {
                return _root;
            }
            set
            {
                // We'll just do this...
                //ClearStreamingSets();
                //((hkaiDirectedGraphExplicitCost)root.m_namedVariants[1].m_variant).m_streamingSets = ((hkaiDirectedGraphExplicitCost)Root.m_namedVariants[1].m_variant).m_streamingSets;
                value.m_namedVariants[0].m_name = _root.m_namedVariants[0].m_name;
                value.m_namedVariants[1].m_name = _root.m_namedVariants[1].m_name;
                value.m_namedVariants[2].m_name = _root.m_namedVariants[2].m_name;

                _root = value;
                //Root.m_namedVariants[0] = root.m_namedVariants[0];
                UpdateRenders();
            }
        }

        public override string ToString()
        {
            if (_root != null)
                return _root.m_namedVariants[0].m_name.Split("//")[0];
            return null;
        }

        private STFileLoader.Settings FileSettings;

        public void Load(Stream stream, string fileName, Vector3 origin, GLScene scene = null)
        {
            FileSettings = STFileLoader.TryDecompressFile(stream, fileName);

            List<IHavokObject> roots = Util.ReadBotwHKX(FileSettings.Stream.ReadAllBytes(), ".hknm2");

            _root = (hkRootLevelContainer)roots[0];

            RootNode = new NodeFolder(fileName)
            {
                Tag = this,
                HasCheckBox = true,
                IsChecked = true,
                OnChecked = (object value, EventArgs args) =>
                {
                    IsVisible = (bool)value;
                }
            };
            Scene = scene;

            Prefix = Path.GetFileNameWithoutExtension(fileName);

            Origin = origin;

            UpdateRenders();
        }

        public void Unload()
        {
            RemoveRenders();
        }

        public void ClearStreamingSets()
        {
            if (Root.m_namedVariants.Count != 3)
                return;
            foreach (var streamingSet in ((hkaiNavMesh)Root.m_namedVariants[0].m_variant).m_streamingSets)
            {
                streamingSet.m_meshConnections.Clear();
                streamingSet.m_graphConnections.Clear();
                streamingSet.m_volumeConnections.Clear();
            }
            foreach (var streamingSet in ((hkaiDirectedGraphExplicitCost)Root.m_namedVariants[1].m_variant).m_streamingSets)
            {
                streamingSet.m_meshConnections.Clear();
                streamingSet.m_graphConnections.Clear();
                streamingSet.m_volumeConnections.Clear();
            }
            foreach (var streamingSet in ((hkaiStaticTreeNavMeshQueryMediator)Root.m_namedVariants[2].m_variant).m_navMesh.m_streamingSets)
            {
                streamingSet.m_meshConnections.Clear();
                streamingSet.m_graphConnections.Clear();
                streamingSet.m_volumeConnections.Clear();
            }
        }

        public void Save(Stream stream)
        {
            UpdateRenders(); // Might as well apply render updates

            var uncompressed = new MemoryStream();
            Util.WriteBotwHKX(new IHavokObject[] { Root }, Header, ".hknm2", uncompressed);

            uncompressed.Position = 0;
            stream.Position = 0;
            FileSettings.CompressionFormat.Compress(uncompressed).CopyTo(stream);
            stream.SetLength(stream.Position);
        }

        #region Rendering
        private bool _isVisible = true;
        public bool IsVisible
        {
            get
            {
                // Kinda meh location for this logic
                // but it cleans up the user experience
                // in such a small way that I'm not too worried about it.
                if (!MapData.ShowCollisionShapes)
                    RootNode.HasCheckBox = false;
                else
                    RootNode.HasCheckBox = true;

                return MapData.ShowCollisionShapes && _isVisible;
            }
            set
            {
                _isVisible = value;
            }
        }

        private List<HavokMeshShapeRender> Renders = new List<HavokMeshShapeRender>();
        private GLScene Scene;

        private void UpdateRenders()
        {
            if (Root.m_namedVariants.Count == 0)
                return;

            RemoveRenders();

            HavokMeshShapeRender nmrender = new HavokMeshShapeRender(RootNode);
            nmrender.LoadNavmesh((hkaiNavMesh)Root.m_namedVariants[0].m_variant, Origin * GLContext.PreviewScale);

            nmrender.Transform.Position = Origin * GLContext.PreviewScale;
            nmrender.Transform.Rotation = OpenTK.Quaternion.Identity;
            nmrender.Transform.Scale = OpenTK.Vector3.One;
            nmrender.Transform.UpdateMatrix(true);

            nmrender.IsVisibleCallback += delegate
            {
                return MapData.ShowNavmeshShapes;
            };

            int debugUIFaceIndex = 0;
            int debugUIEdgeIndex = 0;
            int debugUIVertexIndex = 0;
            ((EditableObjectNode)nmrender.UINode).UIProperyDrawer += delegate
            {
                hkaiNavMesh navmesh = (hkaiNavMesh)Root.m_namedVariants[0].m_variant;

                ImGui.Separator();
                ImGui.Text("Navmesh Debug:");
                ImGui.Separator();

                ImGui.Text("Name:");
                ImGui.Text(Root.m_namedVariants[0].m_name);

                ImGui.Text("Find Face By Index:");
                ImGui.InputInt("Index##debugNavmeshFindFaceByIndex", ref debugUIFaceIndex);
                if (ImGui.Button("Go!##debugNavmeshFindFaceByIndexGo"))
                {
                    TransformableObject r = new TransformableObject(RootNode);
                    Vector4 midpoint = new Vector4();
                    for (int i = navmesh.m_faces[debugUIFaceIndex].m_startEdgeIndex; i < navmesh.m_faces[debugUIFaceIndex].m_startEdgeIndex + navmesh.m_faces[debugUIFaceIndex].m_numEdges; i++)
                    {
                        var edgemidpoint = (navmesh.m_vertices[navmesh.m_edges[i].m_a] + navmesh.m_vertices[navmesh.m_edges[i].m_b]) / 2;
                        midpoint.X += edgemidpoint.X;
                        midpoint.Y += edgemidpoint.Y;
                        midpoint.Z += edgemidpoint.Z;
                        midpoint.W += edgemidpoint.W;
                    }
                    midpoint.X /= navmesh.m_faces[debugUIFaceIndex].m_numEdges;
                    midpoint.Y /= navmesh.m_faces[debugUIFaceIndex].m_numEdges;
                    midpoint.Z /= navmesh.m_faces[debugUIFaceIndex].m_numEdges;
                    midpoint.W /= navmesh.m_faces[debugUIFaceIndex].m_numEdges;

                    r.Transform.Position = new OpenTK.Vector3(midpoint.X + Origin.X, midpoint.Y + Origin.Y, midpoint.Z + Origin.Z) * GLContext.PreviewScale;
                    r.Transform.UpdateMatrix(true);

                    GLContext.ActiveContext.Scene.AddRenderObject(r);

                    GLContext.ActiveContext.Camera.TargetPosition = r.Transform.Position;
                }
                ImGui.Separator();
                ImGui.Text("Find Edge By Index:");
                ImGui.InputInt("Index##debugNavmeshFindEdgeByIndex", ref debugUIEdgeIndex);
                if (ImGui.Button("Go!##debugNavmeshFindEdgeByIndexGo"))
                {
                    TransformableObject r = new TransformableObject(RootNode);
                    var midpoint = (navmesh.m_vertices[navmesh.m_edges[debugUIEdgeIndex].m_a] + navmesh.m_vertices[navmesh.m_edges[debugUIEdgeIndex].m_b]) / 2;
                    r.Transform.Position = new OpenTK.Vector3(midpoint.X + Origin.X, midpoint.Y + Origin.Y, midpoint.Z + Origin.Z) * GLContext.PreviewScale;
                    r.Transform.UpdateMatrix(true);

                    GLContext.ActiveContext.Scene.AddRenderObject(r);

                    GLContext.ActiveContext.Camera.TargetPosition = r.Transform.Position;
                }
                ImGui.Separator();
                ImGui.Text("Find Vertex By Index:");
                ImGui.InputInt("Index##debugNavmeshFindVertexByIndex", ref debugUIVertexIndex);
                if (ImGui.Button("Go!##debugNavmeshFindVertexByIndexGo"))
                {
                    TransformableObject r = new TransformableObject(RootNode);
                    var pos = navmesh.m_vertices[debugUIVertexIndex];
                    r.Transform.Position = new OpenTK.Vector3(pos.X + Origin.X, pos.Y + Origin.Y, pos.Z + Origin.Z) * GLContext.PreviewScale;
                    r.Transform.UpdateMatrix(true);

                    GLContext.ActiveContext.Scene.AddRenderObject(r);

                    GLContext.ActiveContext.Camera.TargetPosition = r.Transform.Position;
                }
                ImGui.Separator();
                int streamingSetIndex = 0;
                foreach (var streamingSet in navmesh.m_streamingSets)
                {
                    if (ImGui.TreeNode($"streamingSetDebug_{streamingSetIndex}"))
                    {
                        ImGui.Text($"Streaming Set {streamingSetIndex}");

                        int meshConnectionIndex = 0;
                        foreach (var meshConnection in streamingSet.m_meshConnections)
                        {
                            if (ImGui.TreeNode($"streamingSetDebug_{streamingSetIndex} meshConnection_{meshConnectionIndex}"))
                            {
                                ImGui.Text($"Mesh Connection {meshConnectionIndex}");

                                ImGui.Text($"Face Index: {meshConnection.m_faceIndex}");
                                ImGui.Text($"Edge Index: {meshConnection.m_edgeIndex}");
                                ImGui.Text($"Opposite Face Index: {meshConnection.m_oppositeFaceIndex}");
                                ImGui.Text($"Opposite Edge Index: {meshConnection.m_oppositeEdgeIndex}");

                                ImGui.Separator();

                                ImGui.Text("Here for convenience:");
                                ImGui.Text($"Opposite UID: {streamingSet.m_oppositeUid.ToString("X")}");

                                ImGui.TreePop();
                            }
                            meshConnectionIndex++;
                        }
                        ImGui.TreePop();
                    }

                    streamingSetIndex++;
                }
            };

            Renders.Add(nmrender);

            Scene?.AddRenderObject(nmrender);

            return;
            int startNodeIdx = Scene.Objects.Count;
            var graph = (hkaiDirectedGraphExplicitCost)Root.m_namedVariants[1].m_variant;
            for (int i = 0; i < graph.m_nodes.Count; i++)
            {
                Vector4 position = new Vector4(graph.m_positions[i].X, graph.m_positions[i].Y, graph.m_positions[i].Z, graph.m_positions[i].W);
                var render = new TransformableObject(RootNode);
                render.Transform.Position = (position.Xyz + Origin) * GLContext.PreviewScale;
                render.Transform.UpdateMatrix(true);
                render.UINode.Header = "Map Navmesh Graph Node";
                int index = i; // So the capture doesn't mess up, I guess...
                ((EditableObjectNode)render.UINode).UIProperyDrawer += delegate
                {
                    ImGui.Text("Index:");
                    ImGui.Text($"{index}");
                    ImGui.Separator();
                    ImGui.Text("Connected to:");
                    for (int edgeIdx = graph.m_nodes[index].m_startEdgeIndex; edgeIdx < graph.m_nodes[index].m_startEdgeIndex + graph.m_nodes[index].m_numEdges; edgeIdx++)
                    {
                        ImGui.Text("Idx: ");
                        ImGui.Text($"  {graph.m_edges[edgeIdx].m_target}");
                        ImGui.Text("Cost: ");
                        ImGui.Text($"  As half: {graph.m_edges[edgeIdx].m_cost}");
                        unsafe
                        {
                            fixed (System.Half* cost = &graph.m_edges[edgeIdx].m_cost)
                            {
                                ImGui.Text($"  As short: {*(short*)(cost)}");
                            }
                        }
                        ImGui.NewLine();
                    }
                };
                Scene?.AddRenderObject(render);
            }
            for (int i = 0; i < graph.m_nodes.Count; i++) // Add link renders
            {
                for (int edgeIdx = graph.m_nodes[i].m_startEdgeIndex; edgeIdx < graph.m_nodes[i].m_startEdgeIndex + graph.m_nodes[i].m_numEdges; edgeIdx++)
                {
                    ((EditableObject)Scene.Objects[startNodeIdx + i]).DestObjectLinks.Add((EditableObject)Scene.Objects[startNodeIdx + (int)graph.m_edges[edgeIdx].m_target]);
                }
            }
        }

        private void RemoveRenders()
        {
            foreach (HavokMeshShapeRender r in Renders)
            {
                Scene?.RemoveRenderObject(r);
                r.Dispose();
            }
            Renders.Clear();
        }
        #endregion

        public void Dispose()
        {
            // Haha who knows who cares
        }
    }
}
