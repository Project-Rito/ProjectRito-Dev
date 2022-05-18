using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;

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
        public void DrawPicking(GLContext context, ITransformableObject pickable, List<OpenTK.Matrix4> modelMatrices)
        {
            float[] modelMatricesFloatArr = MemoryMarshal.Cast<OpenTK.Matrix4, float>(modelMatrices.ToArray()).ToArray();

            context.CurrentShader = GlobalShaders.GetShader("PICKING");
            context.ColorPicker.SetPickingColor(pickable, context.CurrentShader);
            context.CurrentShader.SetMatrix4x4(GLConstants.ModelMatrix, modelMatricesFloatArr);

            Draw(context);
        }

        public void DrawWithSelection(GLContext context, bool isSelected, int instanceCount = 1)
        {
            context.CurrentShader.SetVector4(GLConstants.SelectionColorUniform, OpenTK.Vector4.Zero);

            if (isSelected)
            {
                GL.Enable(EnableCap.StencilTest);
                GL.Clear(ClearBufferMask.StencilBufferBit);
                GL.ClearStencil(0);
                GL.StencilFunc(StencilFunction.Always, 0x1, 0x1);
                GL.StencilOp(StencilOp.Keep, StencilOp.Replace, StencilOp.Replace);

                context.CurrentShader.SetVector4(GLConstants.SelectionColorUniform, new OpenTK.Vector4(GLConstants.SelectColor.Xyz, 0.5f));
            }

            DrawInstanced(context, instanceCount);
            context.CurrentShader.SetVector4(GLConstants.SelectionColorUniform, OpenTK.Vector4.Zero);

            context.CurrentShader.SetInt("hasTextures", 0);
            context.CurrentShader.SetInt("halfLambert", 0);

            if (isSelected)
            {
                GL.Disable(EnableCap.Blend);

                context.CurrentShader.SetVector4(GLConstants.SelectionColorUniform, GLConstants.SelectOutlineColor);

                GL.LineWidth(GLConstants.SelectionWidth);
                GL.StencilFunc(StencilFunction.Equal, 0x0, 0x1);
                GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);

                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                DrawInstanced(context, instanceCount);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

                GL.Disable(EnableCap.StencilTest);
                GL.LineWidth(2);
            }
        }

        public void DrawSolidWithSelection(GLContext context, OpenTK.Matrix4 modelMatrix, OpenTK.Vector4 color, bool isSelected)
        {
            var standard = new StandardMaterial();
            standard.Color = color;
            standard.ModelMatrix = modelMatrix;
            standard.DisplaySelection = isSelected;
            standard.Render(context);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            DrawWithSelection(context, isSelected);
        }
        public void DrawSolidWithSelection(GLContext context, List<OpenTK.Matrix4> modelMatrices, OpenTK.Vector4 color, bool isSelected)
        {
            var standard = new StandardInstancedMaterial();
            standard.Color = color;
            standard.ModelMatrices = modelMatrices;
            standard.DisplaySelection = isSelected;
            standard.Render(context);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            DrawWithSelection(context, isSelected, modelMatrices.Count);
        }

        public void DrawSolid(GLContext context, OpenTK.Matrix4 modelMatrix, OpenTK.Vector4 color)
        {
            var standard = new StandardMaterial();
            standard.Color = color;
            standard.ModelMatrix = modelMatrix;
            standard.Render(context);

            Draw(context);
        }
        public void DrawSolid(GLContext context, List<OpenTK.Matrix4> modelMatrices, OpenTK.Vector4 color)
        {
            var standard = new StandardInstancedMaterial();
            standard.Color = color;
            standard.ModelMatrices = modelMatrices;
            standard.Render(context);

            DrawInstanced(context, modelMatrices.Count);
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
        public void DrawHalfLambert(GLContext context, List<OpenTK.Matrix4> modelMatrices, OpenTK.Vector4 color)
        {
            var standard = new StandardInstancedMaterial();
            standard.HalfLambertShading = true;
            standard.Color = color;
            standard.ModelMatrices = modelMatrices;
            standard.Render(context);

            DrawInstanced(context, modelMatrices.Count);
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

        public void DrawInstanced(GLContext context, int instanceCount) {
            DrawInstances(context.CurrentShader, DrawCount, instanceCount, 0);
        }

        public void DrawInstanced(ShaderProgram shader, int instanceCount) {
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
            GL.DrawArrays(primitiveType, offset, count);

            ResourceTracker.NumDrawCalls += 1;
            ResourceTracker.NumDrawTriangles += count;
        }

        private void DrawElements(int count, int offset = 0) {
            GL.DrawElements(primitiveType, count, DrawElementsType.UnsignedInt, offset);

            ResourceTracker.NumDrawCalls += 1;
            ResourceTracker.NumDrawTriangles += count;
        }

        private void DrawArraysInstanced(int count, int instanceCount, int offset = 0) {
            GL.DrawArraysInstanced(primitiveType, offset, count, instanceCount);

            ResourceTracker.NumDrawCalls += 1;
            ResourceTracker.NumDrawTriangles += count;
        }

        private void DrawElementsInstanced(int count, int instanceCount, int offset = 0) {
            GL.DrawElementsInstanced(primitiveType, count, DrawElementsType.UnsignedInt, (IntPtr)offset, instanceCount);

            ResourceTracker.NumDrawCalls += 1;
            ResourceTracker.NumDrawTriangles += count;
        }
    }
}
