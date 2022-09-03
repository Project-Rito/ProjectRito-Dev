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
        private hkRootLevelContainer Root;

        private STFileLoader.Settings FileSettings;

        public void Load(Stream stream, string fileName, Vector3 origin, GLScene scene = null)
        {
            FileSettings = STFileLoader.TryDecompressFile(stream, fileName);

            List<IHavokObject> roots = Util.ReadBotwHKX(FileSettings.Stream.ReadAllBytes(), ".hknm2");

            Root = (hkRootLevelContainer)roots[0];

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

        public void Replace(hkRootLevelContainer root)
        {
            // We'll just do this...
            root.m_namedVariants[0].m_name = Root.m_namedVariants[0].m_name;
            root.m_namedVariants[1].m_name = Root.m_namedVariants[1].m_name;
            root.m_namedVariants[2].m_name = Root.m_namedVariants[2].m_name;

            Root = root;
            UpdateRenders();
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
            nmrender.LoadNavmesh((hkaiNavMesh)Root.m_namedVariants[0].m_variant);

            nmrender.Transform.Position = Origin * GLContext.PreviewScale;
            nmrender.Transform.Rotation = OpenTK.Quaternion.Identity;
            nmrender.Transform.Scale = OpenTK.Vector3.One;
            nmrender.Transform.UpdateMatrix(true);

            nmrender.IsVisibleCallback += delegate
            {
                return MapData.ShowNavmeshShapes;
            };

            ((EditableObjectNode)nmrender.UINode).UIProperyDrawer += delegate
            {
                ImGui.Separator();
                ImGui.Text("Debug Navmesh Info:");
                ImGui.Separator();
            };

            Renders.Add(nmrender);

            Scene?.AddRenderObject(nmrender);

            foreach (System.Numerics.Vector4 pos in ((hkaiDirectedGraphExplicitCost)Root.m_namedVariants[1].m_variant).m_positions)
            {
                continue;
                Vector4 position = new Vector4(pos.X, pos.Y, pos.Z, pos.W);
                var render = new TransformableObject(RootNode);
                render.Transform.Position = (position.Xyz + Origin) * GLContext.PreviewScale;
                render.Transform.UpdateMatrix(true);
                Scene?.AddRenderObject(render);
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
