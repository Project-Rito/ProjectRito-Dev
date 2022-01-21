using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.ViewModels;
using MapStudio.UI;
using OpenTK;
using GLFrameworkEngine;

namespace MapStudio.UI
{
    public interface IEditor
    {
        /// <summary>
        /// The name of the editor instance.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The optional tool window for drawing the tool UI.
        /// </summary>
        IToolWindowDrawer ToolWindowDrawer { get; set; }

        /// <summary>
        /// The active sub editor to filter other editors.
        /// </summary>
        string SubEditor { get; set; }

        /// <summary>
        /// Gets a list of possible sub editors.
        /// </summary>
        List<string> SubEditors { get; }

        /// <summary>
        /// Gets or sets the list of tree nodes used for the outlier.
        /// </summary>
        List<NodeBase> Nodes { get; set; }

        /// <summary>
        /// Gets a list of menu icons for the viewport window.
        /// </summary>
        List<MenuItemModel> GetViewportMenuIcons();

        /// <summary>
        /// Gets a list of menu icons for the outlier filter menu.
        /// </summary>
        List<MenuItemModel> GetFilterMenuItems();

        /// <summary>
        /// Gets a list of menu icons for the edit menu in the main window.
        /// </summary>
        List<MenuItemModel> GetEditMenuItems();

        void CreateAndSelect(GLContext context);

        void AssetViewportDrop(AssetItem item, Vector2 screenCoords);

        void OnMouseMove();
        void OnMouseDown();
        void OnMouseUp();
        void OnKeyDown(KeyEventInfo e, GLContext context);
        void OnSave(ProjectResources resources);
    }
}
