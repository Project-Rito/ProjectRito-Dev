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
            GLL.Enable(EnableCap.CullFace);

            if (CullingMode == CullMode.None)
                GLL.Disable(EnableCap.CullFace);
            else if (CullingMode == CullMode.FrontAndBack)
                GLL.CullFace(CullFaceMode.FrontAndBack);
            else if (CullingMode == CullMode.Front)
                GLL.CullFace(CullFaceMode.Front);
            else if (CullingMode == CullMode.Back)
                GLL.CullFace(CullFaceMode.Back);
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
