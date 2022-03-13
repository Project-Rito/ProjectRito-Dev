using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core;

namespace CafeLibrary.Rendering
{
    public class BfresMeshRender : GenericPickableMesh, ITransformableObject, IColorPickable
    {
        public GLTransform Transform { get; set; }

        public VertexBufferObject vao;
        //For custom shaders
        public VertexBufferObject customvao;

        public List<BfresPolygonGroupRender> LODMeshes = new List<BfresPolygonGroupRender>();

        public bool IsVisible { get; set; } = true;
        public int VertexSkinCount { get; set; }
        public int BoneIndex { get; set; }
        public bool IsSealPass { get; set; }

        public bool IsDepthShadow { get; set; }
        public bool IsCubeMap { get; set; }
        public bool RenderInCubeMap { get; set; }
        public bool IsSelected { get; set; }
        public bool IsHovered { get; set; }
        public bool UseColorBufferPass { get; set; }

        public bool ProjectDynamicShadowMap { get; set; }
        public bool ProjectStaticShadowMap { get; set; }

        public int Index { get; private set; }

        public bool CanSelect { get; set; } = true;

        List<BfresGLLoader.VaoAttribute> Attributes;

        public BfresMeshRender(int index) { Index = index; }

        public void DrawColorPicking(GLContext control) { }

        public void DrawWithPolygonOffset(ShaderProgram shader, int instanceCount, int displayLOD = 0)
        {
            //Seal objects draw ontop of meshes so offset them
            if (IsSealPass)
            {
                GL.Enable(EnableCap.PolygonOffsetFill);
                GL.PolygonOffset(-1, 1f);
            }
            else
            {
                GL.Disable(EnableCap.PolygonOffsetFill);
            }

            Draw(shader, instanceCount, displayLOD);

            GL.Disable(EnableCap.PolygonOffsetFill);
        }

        public int GetDisplayLevel(GLContext context, BfresRender render)
        {
            var pos = render.BoundingNode.Box.GetClosestPosition(context.Camera.GetViewPostion());
            if (render.BoundingNode.Box.IsInside(context.Camera.GetViewPostion()))
                return 0;
            if (!context.Camera.InRange(pos, BfresRender.LOD_LEVEL_2_DISTANCE * GLContext.PreviewScale) && LODMeshes.Count > 2)
                return 2;
            if (!context.Camera.InRange(pos, BfresRender.LOD_LEVEL_1_DISTANCE * GLContext.PreviewScale) && LODMeshes.Count > 1)
                return 1;
            return 0;
        }

        public void DrawCustom(ShaderProgram shader, int instanceCount)
        {
            //Seal objects draw ontop of meshes so offset them
            if (IsSealPass)
            {
                GL.Enable(EnableCap.PolygonOffsetFill);
                GL.PolygonOffset(-1, 1f);
            }
            else
            {
                GL.Disable(EnableCap.PolygonOffsetFill);
            }

            customvao.Enable(shader);
            customvao.Use();

            if (Runtime.RenderSettings.Wireframe)
                DrawModelWireframe(shader, instanceCount);
            else
                DrawSubMesh(instanceCount);

            GL.Disable(EnableCap.PolygonOffsetFill);
        }

        public void Draw(ShaderProgram shader, int instanceCount, int displayLOD = 0)
        {
            vao.Enable(shader);
            vao.Use();

            if (Runtime.RenderSettings.Wireframe)
                DrawModelWireframe(shader, instanceCount, displayLOD);
            else
                DrawSubMesh(instanceCount, displayLOD);
        }

        private void DrawModelWireframe(ShaderProgram shader, int instanceCount, int displayLOD = 0)
        {
            // use vertex color for wireframe color
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Enable(EnableCap.LineSmooth);
            GL.LineWidth(1.5f);
            DrawSubMesh(instanceCount, displayLOD);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }

        private void DrawSubMesh(int instanceCount, int displayLOD = 0)
        {
            BfresPolygonGroupRender polygonGroup = LODMeshes[displayLOD];
            GL.DrawElementsInstanced(OpenGLHelper.PrimitiveTypes[polygonGroup.PrimitiveType],
               (int)polygonGroup.FaceCount, polygonGroup.DrawElementsType, (IntPtr)polygonGroup.Offset, instanceCount);

            ResourceTracker.NumDrawCalls += 1;
            ResourceTracker.NumDrawTriangles += (polygonGroup.FaceCount / 3);

            //Reset to default depth settings after draw
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthRange(0.0, 1.0);
            GL.DepthMask(true);
        }

        public void InitVertexBuffer(List<BfresGLLoader.VaoAttribute> attributes, byte[] bufferData, byte[] indices)
        {
            Attributes = attributes;

            if (Attributes.Count == 0)
                throw new Exception();

            //Load vaos
            int[] buffers = new int[2];
            GL.GenBuffers(2, buffers);

            int indexBuffer = buffers[0];
            int vaoBuffer = buffers[1];

            vao = new VertexBufferObject(vaoBuffer, indexBuffer);
            customvao = new VertexBufferObject(vaoBuffer, indexBuffer);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length, indices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vaoBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, bufferData.Length, bufferData, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            UpdateVaoAttributes();
        }

        public void UpdateVaoAttributes(Dictionary<string, int> attributeToLocation)
        {
            customvao.Clear();

            var strideTotal = Attributes.Sum(x => x.Stride);
            for (int i = 0; i < Attributes.Count; i++)
            {
                var att = Attributes[i];
                if (!attributeToLocation.ContainsKey(att.name))
                {
                    Console.WriteLine($"attributeToLocation does not contain {att.name}. skipping");
                    continue;
                }

                customvao.AddAttribute(
                    attributeToLocation[att.name],
                    att.ElementCount,
                    att.Type,
                    false,
                    strideTotal,
                    att.Offset);
            }
            customvao.Initialize();
        }

        public void UpdateVaoAttributes(Dictionary<string, string> attributeToUniform)
        {
            customvao.Clear();

            var strideTotal = Attributes.Sum(x => x.Stride);
            for (int i = 0; i < Attributes.Count; i++)
            {
                var att = Attributes[i];
                if (!attributeToUniform.ContainsKey(att.name))
                    continue;

                customvao.AddAttribute(
                    attributeToUniform[att.name],
                    att.ElementCount,
                    att.Type,
                    false,
                    strideTotal,
                    att.Offset);
            }

            customvao.Initialize();
        }

        public void UpdateVaoAttributes()
        {
            vao.Clear();

            var strideTotal = Attributes.Sum(x => x.Stride);
            for (int i = 0; i < Attributes.Count; i++)
            {
                var att = Attributes[i];
                vao.AddAttribute(
                    att.UniformName,
                    att.ElementCount,
                    att.Type,
                    false,
                    strideTotal,
                    att.Offset);
            }
            vao.Initialize();
        }
    }
}
