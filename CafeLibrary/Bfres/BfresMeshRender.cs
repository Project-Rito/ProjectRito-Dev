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

        public void DrawWithPolygonOffset(ShaderProgram shader, int displayLOD = 0)
        {
            //Seal objects draw ontop of meshes so offset them
            if (IsSealPass)
            {
                GLH.Enable(EnableCap.PolygonOffsetFill);
                GLH.PolygonOffset(-1, 1f);
            }
            else
            {
                GLH.Disable(EnableCap.PolygonOffsetFill);
            }

            Draw(shader, displayLOD);

            GLH.Disable(EnableCap.PolygonOffsetFill);
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

        public void DrawCustom(ShaderProgram shader)
        {
            //Seal objects draw ontop of meshes so offset them
            if (IsSealPass)
            {
                GLH.Enable(EnableCap.PolygonOffsetFill);
                GLH.PolygonOffset(-1, 1f);
            }
            else
            {
                GLH.Disable(EnableCap.PolygonOffsetFill);
            }

            customvao.Enable(shader);
            customvao.Use();

            if (Runtime.RenderSettings.Wireframe)
                DrawModelWireframe(shader);
            else
                DrawSubMesh();

            GLH.Disable(EnableCap.PolygonOffsetFill);
        }

        public void Draw(ShaderProgram shader, int displayLOD = 0)
        {
            vao.Enable(shader);
            vao.Use();

            if (Runtime.RenderSettings.Wireframe)
                DrawModelWireframe(shader, displayLOD);
            else
                DrawSubMesh(displayLOD);
        }

        private void DrawModelWireframe(ShaderProgram shader, int displayLOD = 0)
        {
            // use vertex color for wireframe color
            GLH.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GLH.Enable(EnableCap.LineSmooth);
            GLH.LineWidth(1.5f);
            DrawSubMesh(displayLOD);
            GLH.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }

        private void DrawSubMesh(int displayLOD = 0)
        {
            BfresPolygonGroupRender polygonGroup = LODMeshes[displayLOD];
            GLH.DrawElements(OpenGLHelper.PrimitiveTypes[polygonGroup.PrimitiveType],
               (int)polygonGroup.FaceCount, polygonGroup.DrawElementsType, polygonGroup.Offset);

            ResourceTracker.NumDrawCalls += 1;
            ResourceTracker.NumDrawTriangles += (polygonGroup.FaceCount / 3);

            //Reset to default depth settings after draw
            GLH.Enable(EnableCap.DepthTest);
            GLH.DepthFunc(DepthFunction.Lequal);
            GLH.DepthRange(0.0, 1.0);
            GLH.DepthMask(true);
        }

        public void InitVertexBuffer(List<BfresGLLoader.VaoAttribute> attributes, byte[] bufferData, byte[] indices)
        {
            Attributes = attributes;

            if (Attributes.Count == 0)
                throw new Exception();

            //Load vaos
            int[] buffers = new int[2];
            GLH.GenBuffers(2, buffers);

            int indexBuffer = buffers[0];
            int vaoBuffer = buffers[1];

            vao = new VertexBufferObject(vaoBuffer, indexBuffer);
            customvao = new VertexBufferObject(vaoBuffer, indexBuffer);

            GLH.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
            GLH.BufferData(BufferTarget.ElementArrayBuffer, indices.Length, indices, BufferUsageHint.StaticDraw);
            GLH.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            GLH.BindBuffer(BufferTarget.ArrayBuffer, vaoBuffer);
            GLH.BufferData(BufferTarget.ArrayBuffer, bufferData.Length, bufferData, BufferUsageHint.StaticDraw);
            GLH.BindBuffer(BufferTarget.ArrayBuffer, 0);

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
