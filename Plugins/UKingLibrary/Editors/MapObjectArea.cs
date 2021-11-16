using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using UKingLibrary.Rendering;
using OpenTK;
using Toolbox.Core.ViewModels;

namespace UKingLibrary
{
    public class MapObjectArea : MapObject
    {
        public IDictionary<string, dynamic> Parameters
        {
            get {
                if (!Properties.ContainsKey("!Parameters"))
                    return new Dictionary<string, dynamic>();
                
                return Properties["!Parameters"]; }
            set { Properties["!Parameters"] = value; }
        }

        public AreaShapes Shape
        {
            get {
                if (!Properties.ContainsKey("Shape"))
                    return AreaShapes.Box;

                return Enum.Parse(typeof(AreaShapes), Parameters["Shape"]); }
            set { BymlHelper.SetValue(Parameters, "Shape", value.ToString()); }
        }

        public override EditableObject LoadRenderObject(IDictionary<string, dynamic> actor, dynamic obj, NodeBase parent)
        {
            return new AreaRender(parent, Shape, new Vector4(0, 0, 0, 1));
        }
    }
}
