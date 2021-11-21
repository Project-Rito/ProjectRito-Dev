using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using UKingLibrary.UI;
using Toolbox.Core.ViewModels;

namespace UKingLibrary
{
    public class RailPathData
    {
        public RenderablePath PathRender = new RenderablePath();

        public IDictionary<string, dynamic> Properties;

        public uint HashId
        {
            get { return Properties["HashId"]; }
            set { Properties["HashId"] = value; }
        }

        public bool IsClosed
        {
            get { return Properties["IsClosed"]; }
            set { Properties["IsClosed"] = value; }
        }

        public string RailType
        {
            get { return Properties["RailType"]; }
            set { Properties["RailType"] = value; }
        }

        public void LoadRail(MapData mapData, IDictionary<string, dynamic> properties, NodeBase parent)
        {
            RenderablePath.BezierArrowLength = 2;
            RenderablePath.BezierLineWidth = 2;
            RenderablePath.BezierPointScale = 2;
            RenderablePath.PointSize = 2;

            Properties = properties;
            var translate = (IList<dynamic>)properties["Translate"]; // NOT the origin point - railpoints aren't relative.

            PathRender.UINode.ContextMenus.Add(new MenuItemModel("Remove", () =>
            {
                GLContext.ActiveContext.Scene.RemoveRenderObject(PathRender, true);
            }));
            PathRender.AddCallback += delegate
            {
                parent.AddChild(PathRender.UINode);
                mapData.Rails.Add(HashId, this);
            };
            PathRender.RemoveCallback += delegate
            {
                parent.Children.Remove(PathRender.UINode);
                mapData.Rails.Remove(HashId);
            };

            PathRender.Loop = this.IsClosed;
            if (RailType == "Linear")
                PathRender.InterpolationMode = RenderablePath.Interpolation.Linear;
            if (RailType == "Bezier")
                PathRender.InterpolationMode = RenderablePath.Interpolation.Bezier;

            PathRender.UINode.Tag = properties;
            PathRender.UINode.TagUI.UIDrawer += delegate
            {
                PropertyDrawer.LoadPropertyUI(properties);
            };

            foreach (IDictionary<string, dynamic> point in properties["RailPoints"])
            {
                var pos = (IList<dynamic>)point["Translate"];
                RenderablePathPoint pathPoint = new RenderablePathPoint(PathRender,
                    new OpenTK.Vector3(
                    pos[0],
                    pos[1],
                    pos[2]) * GLContext.PreviewScale);

                pathPoint.UINode.Tag = point;
                pathPoint.UINode.TagUI.UIDrawer += delegate
                {
                    PropertyDrawer.LoadPropertyUI(point);

                    if (point.ContainsKey("!Parameters"))
                        PropertyDrawer.LoadPropertyUI(point["!Parameters"]);
                };

                if (point.ContainsKey("ControlPoints"))
                {
                    var controlPoint1 = point["ControlPoints"][0];
                    var controlPoint2 = point["ControlPoints"][1];

                    pathPoint.ControlPoint1.LocalPosition = new OpenTK.Vector3(
                        controlPoint1[0], controlPoint1[1], controlPoint1[2]) * GLContext.PreviewScale;

                    pathPoint.ControlPoint2.LocalPosition = new OpenTK.Vector3(
                        controlPoint2[0], controlPoint2[1], controlPoint2[2]) * GLContext.PreviewScale;

                    pathPoint.UpdateMatrices();
                }

                PathRender.AddPoint(pathPoint, PathRender.PathPoints.Count);
            }
            //Add children
            for (int i = 0; i < PathRender.PathPoints.Count; i++)
            {
                if (PathRender.PathPoints.Count - 1 > i)
                    PathRender.PathPoints[i].AddChild(PathRender.PathPoints[i + 1]);
            }
        }

        public void SaveRail()
        {

        }

        public void AddToScene() {
            GLContext.ActiveContext.Scene.AddRenderObject(PathRender);
        }

        public void RemoveFromScene() {
            GLContext.ActiveContext.Scene.RemoveRenderObject(PathRender);
        }

        public void RemoveSelected()
        {
            GLContext.ActiveContext.Scene.AddToUndo(
                new EditableObjectDeletedUndo(GLContext.ActiveContext.Scene, new List<RenderablePath>() { PathRender }));

            RemoveFromScene();
        }

        public void OnKeyDown()
        {
            if (KeyInfo.EventInfo.IsKeyDown(InputSettings.INPUT.Scene.Delete) && !PathRender.EditMode && PathRender.IsSelected)
                RemoveSelected();
        }
    }
}
