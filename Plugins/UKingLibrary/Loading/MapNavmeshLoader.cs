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
            RemoveRenders();

            HavokMeshShapeRender render = new HavokMeshShapeRender(RootNode);
            render.LoadNavmesh((hkaiNavMesh)Root.m_namedVariants[0].m_variant);

            render.Transform.Position = Origin * GLContext.PreviewScale;
            render.Transform.Rotation = OpenTK.Quaternion.Identity;
            render.Transform.Scale = OpenTK.Vector3.One;
            render.Transform.UpdateMatrix(true);

            render.IsVisibleCallback += delegate
            {
                return MapData.ShowNavmeshShapes;
            };

            ((EditableObjectNode)render.UINode).UIProperyDrawer += delegate
            {
                ImGui.Separator();
                ImGui.Text("Debug Navmesh Info:");
                ImGui.Separator();
            };

            Renders.Add(render);

            Scene?.AddRenderObject(render);
        }

        private void RemoveRenders()
        {
            foreach (HavokMeshShapeRender r in Renders)
                Scene?.RemoveRenderObject(r);
            Renders.Clear();
        }
        #endregion

        public void Dispose()
        {
            // Haha who knows who cares
        }
    }
}
