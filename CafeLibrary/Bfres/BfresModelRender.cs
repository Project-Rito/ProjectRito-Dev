using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using GLFrameworkEngine;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Toolbox.Core;

namespace CafeLibrary.Rendering
{
    public class BfresModelRender : ModelAsset
    {
        public Matrix4 ModelTransform = Matrix4.Identity;

        static GLMaterialBlendState DefaultBlendState = new GLMaterialBlendState();

        public override IEnumerable<GenericPickableMesh> MeshList => Meshes;

        public List<BfresMeshRender> Meshes = new List<BfresMeshRender>();
        public bool[] MeshInFrustum = new bool[0];

        public BoundingNode BoundingNode = new BoundingNode();

        public void ResetAnimations() {
            foreach (var bone in ModelData.Skeleton.Bones)
                bone.Visible = true;

            ModelData.Skeleton.Reset();
        }

        public static BfresModelRender CreateCache(BfresModelRender model)
        {
            BfresModelRender modelCache = new BfresModelRender();

            modelCache.Name = model.Name;
            modelCache.ModelData = new STGenericModel();
            modelCache.BoundingNode = model.BoundingNode;
            var skeletonCache = model.ModelData.Skeleton;
/*
            var skeleton = new STSkeleton();
            modelCache.ModelData.Skeleton = skeleton;

            for (int i = 0; i < skeletonCache.Bones.Count; i++)
            {
                skeleton.Bones.Add(new STBone(skeleton)
                {
                    Name = skeletonCache.Bones[i].Name,
                    Position = skeletonCache.Bones[i].Position,
                    Rotation = skeletonCache.Bones[i].Rotation,
                    Scale = skeletonCache.Bones[i].Scale,
                    ParentIndex = skeletonCache.Bones[i].ParentIndex,
                });
            }*/

             modelCache.ModelData = model.ModelData;
            modelCache.MeshInFrustum = new bool[model.Meshes.Count];
            for (int i = 0; i < model.Meshes.Count; i++)
                modelCache.MeshInFrustum[i] = true;
            modelCache.Meshes.AddRange(model.Meshes);

            return modelCache;
        }

        public void UpdateSkeletonUniforms()
        {

        }

        public void UpdateFrustum(GLContext context, BfresRender render)
        {
            for (int i = 0; i < Meshes.Count; i++)
                MeshInFrustum[i] = IsMeshInFustrum(context, render, Meshes[i]);
        }

        public void Draw(GLContext context, Pass pass, BfresRender parentRender, List<GLTransform> transforms)
        {
            if (!IsVisible)
                return;

            //Full model selection
            if (pass == Pass.OPAQUE && (parentRender.IsSelected || parentRender.IsHovered))
                DrawFrontFaceSelection(context, parentRender.IsSelected);

            if (DebugShaderRender.DebugRendering != DebugShaderRender.DebugRender.Default) {
                DrawMeshes(context, parentRender, pass, RenderPass.DEBUG, transforms);
            }
            else
            {
                foreach (var mesh in Meshes) {
                    if (pass != mesh.Pass || !mesh.IsVisible || mesh.UseColorBufferPass)
                        continue;

                    RenderMesh(context, parentRender, mesh, transforms);
                }

                /* if (Meshes.Where(x => x.UseColorBufferPass).ToList().Count > 0)
                 {
                     GLFrameworkEngine.ScreenBufferTexture.FilterScreen(context);
                     foreach (var mesh in Meshes.Where(x => x.UseColorBufferPass))
                     {
                         if (pass != mesh.Pass || !mesh.IsVisible)
                             continue;

                         RenderMesh(context, parentRender, mesh);
                     }
                 }*/
            }

            //Reset blend state
            DefaultBlendState.RenderAlphaTest();
            DefaultBlendState.RenderBlendState();
            DefaultBlendState.RenderDepthTest();

            if (pass == Pass.OPAQUE && (parentRender.IsSelected || parentRender.IsHovered))
                DrawLineSelection(context, parentRender, parentRender.IsSelected, parentRender.IsHovered, transforms);

            GL.DepthMask(true);
        }

        public void DrawShadowModel(GLContext context, BfresRender parentRender, List<GLTransform> transforms = null)
        {
            if (!IsVisible)
                return;

            DrawMeshes(context, parentRender, Pass.OPAQUE, RenderPass.SHADOW_DYNAMIC, transforms);
            DrawMeshes(context, parentRender, Pass.TRANSPARENT, RenderPass.SHADOW_DYNAMIC, transforms);
        }

        public void DrawCubemapModel(GLContext context, BfresRender parentRender, List<GLTransform> transforms = null)
        {
            if (!IsVisible)
                return;

            foreach (var mesh in Meshes) {
                if (!mesh.IsCubeMap && !mesh.RenderInCubeMap)
                    continue;

                RenderMesh(context, parentRender, mesh, transforms);
            }
        }

        public void DrawGBuffer(GLContext context, BfresRender parentRender, List<GLTransform> transforms = null)
        {
            if (!IsVisible)
                return;

            DrawMeshes(context, parentRender, Pass.OPAQUE, RenderPass.GBUFFER, transforms);
            DrawMeshes(context, parentRender, Pass.TRANSPARENT, RenderPass.GBUFFER, transforms);
        }

        public void DrawColorBufferPass(GLContext context, BfresRender parentRender, List<GLTransform> transforms = null)
        {
            if (!Meshes.Any(x => x.UseColorBufferPass))
                return;

            //Draw objects that use the color buffer texture
            DrawMeshes(context, parentRender, Pass.OPAQUE, RenderPass.COLOR_COPY, transforms);
            DrawMeshes(context, parentRender, Pass.TRANSPARENT, RenderPass.COLOR_COPY, transforms);
        }

        enum RenderPass
        {
            DEFAULT,
            DEBUG,
            COLOR_COPY,
            SHADOW_DYNAMIC,
            GBUFFER,
        }

        private void DrawMeshes(GLContext context, BfresRender parentRender, Pass pass, RenderPass renderMode, List<GLTransform> transforms)
        {
            foreach (var mesh in Meshes)
            {
                if (mesh.Pass != pass || !mesh.IsVisible ||
                     renderMode == RenderPass.COLOR_COPY && !mesh.UseColorBufferPass ||
                     renderMode == RenderPass.SHADOW_DYNAMIC && !mesh.ProjectDynamicShadowMap)
                    continue;

                if (renderMode == RenderPass.DEFAULT && mesh.UseColorBufferPass)
                    return;

                if (renderMode == RenderPass.SHADOW_DYNAMIC)
                {
                    var frustum = context.Scene.ShadowRenderer.GetShadowFrustum();
                    if (FrustumHelper.CubeInFrustum(frustum, BoundingNode.GetCenter(), BoundingNode.GetRadius()))
                    {
                        ((BfresMaterialRender)mesh.MaterialAsset).RenderShadowMaterial(context);

                        DrawMesh(context.CurrentShader, parentRender, mesh, transforms, false);
                        ResourceTracker.NumShadowDrawCalls += mesh.LODMeshes[0].DrawCalls.Count;
                    }
                }
                else if (renderMode == RenderPass.DEBUG)
                {
                    if (DebugShaderRender.DebugRendering != DebugShaderRender.DebugRender.Diffuse)
                        context.UseSRBFrameBuffer = false;

                    ((BfresMaterialRender)mesh.MaterialAsset).SetRenderState();
                    DebugShaderRender.RenderMaterial(context);
                    context.CurrentShader.SetBoolToInt("DrawAreaID", BfresRender.DrawDebugAreaID);
                    context.CurrentShader.SetInt("AreaIndex", ((BfresMaterialRender)mesh.MaterialAsset).AreaIndex);

                    DrawMesh(context.CurrentShader, parentRender, mesh, transforms);
                }
                else
                    RenderMesh(context, parentRender, mesh, transforms);
            }
        }

        private void DrawFrontFaceSelection(GLContext control, bool parentSelected)
        {
            GL.Enable(EnableCap.StencilTest);
            GL.Clear(ClearBufferMask.StencilBufferBit);
            GL.ClearStencil(0);
            GL.StencilFunc(StencilFunction.Always, 0x1, 0x1);
            GL.StencilOp(StencilOp.Keep, StencilOp.Replace, StencilOp.Replace);
        }

        private void DrawLineSelection(GLContext control, BfresRender parentRender, bool parentSelected, bool isHovered, List<GLTransform> transforms)
        {
            GL.Disable(EnableCap.AlphaTest);
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            var selectionShader = GlobalShaders.GetShader("PICKING");
            control.CurrentShader = selectionShader;
            if (isHovered)
                selectionShader.SetVector4("color", GLConstants.HoveredColor);
            else
                selectionShader.SetVector4("color", GLConstants.SelectColor);

            //Draw lines
            {
                GL.LineWidth(GLConstants.SelectionWidth);
                GL.StencilFunc(StencilFunction.Equal, 0x0, 0x1);
                GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);

                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

                foreach (var mesh in Meshes)
                {
                    if ((mesh.IsSelected || isHovered || parentSelected) && mesh.IsVisible) {
                        DrawMesh(selectionShader, parentRender, mesh, transforms);
                    }
                }

                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            }

            GL.Disable(EnableCap.StencilTest);
            GL.LineWidth(1);
        }

        public void DrawColorPicking(GLContext control, BfresRender parentRender, List<GLTransform> transforms = null)
        {
            if (!IsVisible)
                return;

            GL.Enable(EnableCap.DepthTest);
            foreach (BfresMeshRender mesh in this.Meshes)
            {
                if (!mesh.IsVisible)
                    continue;

                ((BfresMaterialRender)mesh.MaterialAsset).SetRenderState();

                //Draw the mesh
                if (control.ColorPicker.PickingMode == ColorPicker.SelectionMode.Mesh)
                    control.ColorPicker.SetPickingColor(mesh, control.CurrentShader);

                DrawMesh(control.CurrentShader, parentRender, mesh, transforms);

                control.CurrentShader.SetInt("UseSkinning", 0);
            }
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
        }

        public void RenderMesh(GLContext context, BfresRender parentRender, BfresMeshRender mesh, List<GLTransform> transforms)
        {
            if (mesh.MaterialAsset is BfshaRenderer && !BfresRender.DrawDebugAreaID) {
                DrawCustomShaderRender(context, parentRender, mesh, 0);
            }
            else //Draw default if not using game shader rendering.
            {
                context.CurrentShader = BfresRender.DefaultShader;

                ((BfresMaterialRender)mesh.MaterialAsset).RenderDefaultMaterials(context, parentRender.Transform, context.CurrentShader, mesh);
                DrawMesh(context.CurrentShader, parentRender, mesh, transforms, true);
            }
        }

        public void DrawSolid(GLContext context, BfresRender parentRender, BfresMeshRender mesh, int instanceCount)
        {
            var mat = new StandardMaterial();
            mat.Color = new Vector4(1, 0, 0, 1);
            mat.ModelMatrix = parentRender.Transform.TransformMatrix;
            mat.Render(context);
            mesh.Draw(context.CurrentShader, instanceCount);
        }

        private void DrawCustomShaderRender(GLContext context, BfresRender parentRender, BfresMeshRender mesh, int instanceCount, int stage = 0)
        {
            var materialAsset = ((TurboNXRender)mesh.MaterialAsset);
            if (!materialAsset.HasValidProgram) {
                DrawSolid(context, parentRender, mesh, instanceCount);
                return;
            }

            materialAsset.ShaderIndex = stage;
            materialAsset.CheckProgram(context, mesh, stage);

            if (materialAsset.GLShaderInfo == null)
                return;

            //  if (context.CurrentShader != materialAsset.Shader)
            context.CurrentShader = materialAsset.Shader;

            materialAsset.Shader.Enable();

            ((BfshaRenderer)mesh.MaterialAsset).Render(context, parentRender.Transform, materialAsset.Shader, mesh);

            //Draw the mesh
            mesh.DrawCustom(context.CurrentShader, instanceCount);
        }

        private void DrawMesh(ShaderProgram shader, BfresRender parentRender, BfresMeshRender mesh, List<GLTransform> transforms, bool usePolygonOffset = false)
        {
            if (!MeshInFrustum[mesh.Index])
                return;

            bool enableSkinning = true;

            if (mesh.VertexSkinCount > 0 && enableSkinning)
                SetModelMatrix(shader.program, ModelData.Skeleton, mesh.VertexSkinCount > 1);

            List<Matrix4> worldTransforms = new List<Matrix4>(transforms.Count);
            for (int i = 0; i < transforms.Count; i++)
            {
                worldTransforms.Add(transforms[i].TransformMatrix * ModelTransform);
            }
            var transform = this.ModelData.Skeleton.Bones[mesh.BoneIndex].Transform;
            float[] worldTransformFloatArr = MemoryMarshal.Cast<Matrix4, float>(worldTransforms.ToArray()).ToArray();

            shader.SetMatrix4x4("RigidBindTransform", ref transform);
            shader.SetMatrix4x4("mtxMdl[0]", worldTransformFloatArr);
            shader.SetInt("SkinCount", mesh.VertexSkinCount);
            shader.SetInt("UseSkinning", enableSkinning ? 1 : 0);
            if (parentRender.CanSelect)
            {
                if (parentRender.IsSelected || mesh.IsSelected)
                    shader.SetVector4(GLConstants.SelectionColorUniform, GLConstants.SelectColor);
                else if (parentRender.IsHovered || mesh.IsHovered)
                    shader.SetVector4(GLConstants.SelectionColorUniform, GLConstants.HoveredColor);
            }
 
            int lod = mesh.GetDisplayLevel(GLContext.ActiveContext, parentRender);            

            //Draw the mesh
            if (usePolygonOffset)
                mesh.DrawWithPolygonOffset(shader, transforms.Count, lod);
            else
                mesh.Draw(shader, transforms.Count, lod);

            shader.SetVector4(GLConstants.SelectionColorUniform, Vector4.Zero);
        }

        /// <summary>
        /// Checks for when the given mesh render is in the fustrum of the camera
        /// Returns true if in view.
        /// </summary>
        private bool IsMeshInFustrum(GLContext control, BfresRender parentRender, GenericPickableMesh mesh)
        {
            if (parentRender.StayInFrustum)
                return true;

            var msh = (BfresMeshRender)mesh;
            var bone = ModelData.Skeleton.Bones[msh.BoneIndex].Transform;
            mesh.BoundingNode.UpdateTransform(bone * parentRender.Transform.TransformMatrix);
            return control.Camera.InFustrum(mesh.BoundingNode);
        }

        private void SetModelMatrix(int programID, STSkeleton skeleton, bool useInverse = true)
        {
            GL.Uniform1(GL.GetUniformLocation(programID, "UseSkinning"), 1);

            for (int i = 0; i < skeleton.Bones.Count; i++)
            {
                Matrix4 transform = skeleton.Bones[i].Transform;
                //Check if the bone is smooth skinning aswell for accuracy purposes.
                if (useInverse)
                    transform = skeleton.Bones[i].Inverse * skeleton.Bones[i].Transform;
                GL.UniformMatrix4(GL.GetUniformLocation(programID, String.Format("bones[{0}]", i)), false, ref transform);
            }
        }
    }
}
