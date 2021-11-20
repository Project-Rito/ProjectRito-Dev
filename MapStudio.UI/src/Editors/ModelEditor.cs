using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.ViewModels;
using MapStudio.UI;
using GLFrameworkEngine;
using Toolbox.Core;
using OpenTK;

namespace MapStudio.UI
{
    /// <summary>
    /// Represents an editor space for a model file type.
    /// </summary>
    public class ModelEditor : IEditor
    {
        public string Name => "MODEL_EDITOR";

        public string SubEditor { get; set; } = "Default";

        public List<string> SubEditors => new List<string>() { "Default", };

        public IToolWindowDrawer ToolWindowDrawer { get; set; }

        public List<NodeBase> Nodes { get; set; } = new List<NodeBase>();

        private NodeBase ModelFolder = new NodeBase(TranslationSource.GetText("MODELS"));
        private NodeBase TextureFolder = new NodeBase(TranslationSource.GetText("TEXTURES"));

        List<string> LoadedTextures = new List<string>();

        public ModelEditor()
        {
            Nodes.Add(ModelFolder);
            Nodes.Add(TextureFolder);
        }

        public bool ImportAsset(string filePath)
        {
            IFileFormat fileFormat = Toolbox.Core.IO.STFileLoader.OpenFileFormat(filePath);
            if (fileFormat == null)
                return false;

            var workspace = Workspace.ActiveWorkspace;

            if (fileFormat is IRenderableFile)
            {
                var render = ((IRenderableFile)fileFormat).Renderer;
                GLContext.ActiveContext.Scene.AddRenderObject(render);
            }
            if (fileFormat is NodeBase)
            {
                var node = fileFormat as NodeBase;
                Nodes.Add(node);
            }
            if (fileFormat is IModelFormat)
            {
                var modelFormat = new ModelFileFormat();
                modelFormat.FilePath = fileFormat.FileInfo.FilePath;
                modelFormat.Load(workspace, fileFormat);

                return true;
            }
            if (fileFormat is STGenericTexture)
            {
                if (LoadedTextures.Contains(fileFormat.FileInfo.FilePath))
                    return false;

                LoadedTextures.Add(fileFormat.FileInfo.FilePath);

                var textureFormat = new TextureFileFormat();
                textureFormat.FilePath = fileFormat.FileInfo.FilePath;
                textureFormat.Load(workspace, fileFormat);

                return true;
            }

            return false;
        }

        public List<MenuItemModel> GetViewportMenuIcons()
        {
            return new List<MenuItemModel>();
        }

        public List<MenuItemModel> GetFilterMenuItems()
        {
            return new List<MenuItemModel>();
        }

        public List<MenuItemModel> GetEditMenuItems()
        {
            return new List<MenuItemModel>();
        }

        public void AssetViewportDrop(AssetItem item, Vector2 screenCoords)
        {

        }

        public void AddMesh(GenericMeshRender render)
        {
            //Prepare remove handling
            render.RemoveCallback += delegate {
                //Remove the node from the model list
                ModelFolder.Children.Remove(render.UINode);
                //Dispose the mesh.
                render.Dispose();
            };
            //Prepare add handling
            render.AddCallback += delegate {
                //Add the node from the model list
                ModelFolder.Children.Add(render.UINode);
            };
            //Add to the scene for viewing
            GLContext.ActiveContext.Scene.AddRenderObject(render);
        }

        public void AddTexture(NodeBase textureNode) {
            TextureFolder.AddChild(textureNode);
        }

        public void Reload()
        {

        }

        public void OnMouseMove() { }
        public void OnMouseDown() { }
        public void OnMouseUp() { }
        public void OnKeyDown() { }

        public void OnSave(ProjectResources resources)
        {

        }
    }
}
