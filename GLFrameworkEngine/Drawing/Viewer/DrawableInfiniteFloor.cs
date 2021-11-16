using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core;

namespace GLFrameworkEngine
{
    public class DrawableInfiniteFloor
    {
        PlaneRenderer PlaneRender;

        public static bool IsSolid = false;
        public static bool IsSpotlight = false;

        public void Draw(GLContext control)
        {
            if (!DrawableFloor.Display)
                return;

            if (PlaneRender == null) {
                PlaneRender = new PlaneRenderer();
            }

            var gridShaderProgram = GlobalShaders.GetShader("GRID_INFINITE");
            control.CurrentShader = gridShaderProgram;

            var mtxView = control.Camera.ViewMatrix;
            var mtxProj = control.Camera.ProjectionMatrix;
            gridShaderProgram.SetMatrix4x4("mtxView", ref mtxView);
            gridShaderProgram.SetMatrix4x4("mtxProj", ref mtxProj);
            gridShaderProgram.SetFloat("near", 0.01f);
            gridShaderProgram.SetFloat("far", 10);
            gridShaderProgram.SetBoolToInt("solidFloor", IsSolid);
            gridShaderProgram.SetBoolToInt("spotLight", IsSpotlight);
            gridShaderProgram.SetVector3("gridColor", new Vector3(
                DrawableFloor.GridColor.X,
                DrawableFloor.GridColor.Y,
                DrawableFloor.GridColor.Z));

            GLMaterialBlendState.Translucent.RenderBlendState();

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);

            PlaneRender.Draw(control);

            GLMaterialBlendState.Opaque.RenderBlendState();
        }
    }
}