using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class GLMaterialPolygonState
    {
        public CullMode CullingMode = CullMode.Back;

        public void DisplayBoth()  { CullingMode = CullMode.None; }
        public void DisplayFront() { CullingMode = CullMode.Front; }
        public void DisplayBack()  { CullingMode = CullMode.Back; }

        public void Render()
        {
            GLH.Enable(EnableCap.CullFace);

            if (CullingMode == CullMode.None)
                GLH.Disable(EnableCap.CullFace);
            else if (CullingMode == CullMode.FrontAndBack)
                GLH.CullFace(CullFaceMode.FrontAndBack);
            else if (CullingMode == CullMode.Front)
                GLH.CullFace(CullFaceMode.Front);
            else if (CullingMode == CullMode.Back)
                GLH.CullFace(CullFaceMode.Back);
        }

        public enum CullMode
        {
            Front,
            Back,
            FrontAndBack,
            None,
        }
    }
}
