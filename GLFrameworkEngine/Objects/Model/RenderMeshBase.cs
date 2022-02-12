using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class RenderMeshBase
    {
        public bool IsDisposed { get; private set; }

        private PrimitiveType primitiveType;

        public RenderAttribute[] attributes;
        public BufferObject[] buffers;
        public BufferObject[] instancedBuffers;
        public BufferObject indexBufferData;

        public int DrawCount = 0;

        public RenderMeshBase() { }

        public RenderMeshBase(PrimitiveType type) {
            primitiveType = type;
        }

        public void UpdatePrimitiveType(PrimitiveType type) {
            primitiveType = type;
        }

        protected virtual void BindVAO()
        {
        }

        protected virtual void PrepareAttributes(ShaderProgram shader)
        {
        }

        public void DrawPicking(GLContext context, ITransformableObject pickable, OpenTK.Matrix4 modelMatrix)
        {
            context.CurrentShader = GlobalShaders.GetShader("PICKING");
            context.ColorPicker.SetPickingColor(pickable, context.CurrentShader);
            context.CurrentShader.SetMatrix4x4(GLConstants.ModelMatrix, ref modelMatrix);

            Draw(context);
        }

        public void DrawWithSelection(GLContext context, bool isSelected)
        {
            context.CurrentShader.SetVector4(GLConstants.SelectionColorUniform, OpenTK.Vector4.Zero);

            if (isSelected)
            {
                GLH.Enable(EnableCap.StencilTest);
                GLH.Clear(ClearBufferMask.StencilBufferBit);
                GLH.ClearStencil(0);
                GLH.StencilFunc(StencilFunction.Always, 0x1, 0x1);
                GLH.StencilOp(StencilOp.Keep, StencilOp.Replace, StencilOp.Replace);

                context.CurrentShader.SetVector4(GLConstants.SelectionColorUniform, new OpenTK.Vector4(GLConstants.SelectColor.Xyz, 0.5f));
            }

            Draw(context);
            context.CurrentShader.SetVector4(GLConstants.SelectionColorUniform, OpenTK.Vector4.Zero);

            context.CurrentShader.SetInt("hasTextures", 0);
            context.CurrentShader.SetInt("halfLambert", 0);

            if (isSelected)
            {
                GLH.Disable(EnableCap.Blend);

                context.CurrentShader.SetVector4(GLConstants.SelectionColorUniform, GLConstants.SelectOutlineColor);

                GLH.LineWidth(GLConstants.SelectionWidth);
                GLH.StencilFunc(StencilFunction.Equal, 0x0, 0x1);
                GLH.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);

                GLH.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                Draw(context);
                GLH.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

                GLH.Disable(EnableCap.StencilTest);
                GLH.LineWidth(2);
            }
        }

        public void DrawSolidWithSelection(GLContext context, OpenTK.Matrix4 modelMatrix, OpenTK.Vector4 color, bool isSelected)
        {
            var standard = new StandardMaterial();
            standard.Color = color;
            standard.ModelMatrix = modelMatrix;
            standard.DisplaySelection = isSelected;
            standard.Render(context);

            GLH.Enable(EnableCap.CullFace);
            GLH.CullFace(CullFaceMode.Back);

            DrawWithSelection(context, isSelected);
        }

        public void DrawSolid(GLContext context, OpenTK.Matrix4 modelMatrix, OpenTK.Vector4 color)
        {
            var standard = new StandardMaterial();
            standard.Color = color;
            standard.ModelMatrix = modelMatrix;
            standard.Render(context);

            Draw(context);
        }

        public void DrawHalfLambert(GLContext context, OpenTK.Matrix4 modelMatrix, OpenTK.Vector4 color)
        {
            var standard = new StandardMaterial();
            standard.HalfLambertShading = true;
            standard.Color = color;
            standard.ModelMatrix = modelMatrix;
            standard.Render(context);

            Draw(context);
        }

        /// <summary>
        /// Draws a model with the current context.
        /// </summary>
        public void Draw(GLContext context) {
            Draw(context.CurrentShader, DrawCount, 0);
        }

        public void Draw(ShaderProgram shader) {
            Draw(shader, DrawCount, 0);
        }

        public void DrawInstance(GLContext context, int instanceCount) {
            DrawInstances(context.CurrentShader, DrawCount, instanceCount, 0);
        }

        public void DrawInstance(ShaderProgram shader, int instanceCount) {
            DrawInstances(shader, DrawCount, instanceCount, 0);
        }

        public void Draw(ShaderProgram shader, int count, int offset = 0)
        {
            //Skip if count is empty
            if (count == 0 || IsDisposed)
                return;

            PrepareAttributes(shader);
            BindVAO();

            if (indexBufferData != null)
                DrawElements(count, offset);
            else
                DrawArrays(count, offset);
        }

        public void DrawInstances(ShaderProgram shader, int count, int instanceCount, int offset = 0)
        {
            //Skip if count is empty
            if (count == 0 || IsDisposed)
                return;

            PrepareAttributes(shader);
            BindVAO();
            if (indexBufferData != null)
                DrawElementsInstanced(count, instanceCount, offset);
            else
                DrawArraysInstanced(count, instanceCount, offset);
        }

        public virtual void Dispose()
        {
            for (int i = 0; i < buffers.Length; i++)
                buffers[i].Dispose();

            IsDisposed = true;
        }

        private void DrawArrays(int count, int offset = 0) {
            GLH.DrawArrays(primitiveType, offset, count);

            ResourceTracker.NumDrawCalls += 1;
            ResourceTracker.NumDrawTriangles += count;
        }

        private void DrawElements(int count, int offset = 0) {
            GLH.DrawElements(primitiveType, count, DrawElementsType.UnsignedInt, offset);

            ResourceTracker.NumDrawCalls += 1;
            ResourceTracker.NumDrawTriangles += count;
        }

        private void DrawArraysInstanced(int count, int instanceCount, int offset = 0) {
            GLH.DrawArraysInstanced(primitiveType, offset, count, instanceCount);

            ResourceTracker.NumDrawCalls += 1;
            ResourceTracker.NumDrawTriangles += count;
        }

        private void DrawElementsInstanced(int count, int instanceCount, int offset = 0) {
            GLH.DrawElements(primitiveType, count, DrawElementsType.UnsignedInt, offset);

            ResourceTracker.NumDrawCalls += 1;
            ResourceTracker.NumDrawTriangles += count;
        }
    }
}
