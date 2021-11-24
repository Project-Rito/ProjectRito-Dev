using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using GLFrameworkEngine;
using BfresLibrary;
using Toolbox.Core;
using BfresLibrary.Helpers;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core.IO;

namespace CafeLibrary.Rendering
{
    public class BfresLoader
    {
        public static BfresRender Load(System.IO.Stream stream, string name) {
            return new BfresRender(stream, name);
        }

        public static bool LoadBfresCached(BfresRender renderer, BfresRender cachedRender)
        {
            //Render already has models so return
            if (renderer.Models.Count > 0) return true;

            renderer.BoundingSphere = cachedRender.BoundingSphere;

            foreach (var tex in cachedRender.Textures)
                renderer.Textures.Add(tex.Key, tex.Value);
            foreach (BfresModelRender model in cachedRender.Models)
            {
                //Create a new model instance so mesh frustum data and individual skeletons can be stored there
                renderer.Models.Add(BfresModelRender.CreateCache(model));
            }

            foreach (var anim in cachedRender.SkeletalAnimations)
                renderer.SkeletalAnimations.Add(anim.Clone());
            foreach (var anim in cachedRender.MaterialAnimations)
                renderer.MaterialAnimations.Add(anim.Clone());

            return true;
        }

        public static Dictionary<string, GenericRenderer.TextureView> GetTextures(string filePath)
        {
            if (File.Exists(filePath)) {
                if (YAZ0.IsCompressed(filePath))
                    return GetTextures(new System.IO.MemoryStream(YAZ0.Decompress(filePath)));
                else
                    return GetTextures(System.IO.File.OpenRead(filePath));
            }
            return null;
        }

        public static void LoadAnimations(BfresRender render, string filePath)
        {
            if (render.SkeletalAnimations.Count > 0)
                return;

            if (YAZ0.IsCompressed(filePath))
                LoadAnimations(render, new System.IO.MemoryStream(YAZ0.Decompress(filePath)));
            else
                LoadAnimations(render, System.IO.File.OpenRead(filePath));
        }

        static void LoadAnimations(BfresRender renderer, System.IO.Stream stream)
        {
            ResFile resFile = new ResFile(stream);

            foreach (var anim in resFile.SkeletalAnims.Values)
                renderer.SkeletalAnimations.Add(new BfresSkeletalAnim(anim, renderer.Name));
            foreach (var anim in resFile.ShaderParamAnims.Values)
                renderer.MaterialAnimations.Add(new BfresMaterialAnim(anim, renderer.Name));
            foreach (var anim in resFile.ColorAnims.Values)
                renderer.MaterialAnimations.Add(new BfresMaterialAnim(anim, renderer.Name));
            foreach (var anim in resFile.TexSrtAnims.Values)
                renderer.MaterialAnimations.Add(new BfresMaterialAnim(anim, renderer.Name));
            foreach (var anim in resFile.TexPatternAnims.Values)
                renderer.MaterialAnimations.Add(new BfresMaterialAnim(anim, renderer.Name));
        }

        public static Dictionary<string, GenericRenderer.TextureView> GetTextures(System.IO.Stream stream)
        {
            Dictionary<string, GenericRenderer.TextureView> textures = new Dictionary<string, GenericRenderer.TextureView>();

            ResFile resFile = new ResFile(stream);

            foreach (var tex in resFile.Textures.Values)
                textures.Add(tex.Name, PrepareTexture(resFile, tex));

            return textures;
        }

        public static bool OpenBfres(System.IO.Stream stream, BfresRender renderer)
        {
            //Render already has models so return
            if (renderer.Models.Count > 0) return true;

            ResFile resFile = new ResFile(stream);

            //Find and load any external shader binaries
            if (renderer.UseGameShaders) {
                for (int i = 0; i < resFile.ExternalFiles.Count; i++) {
                    string fileName = resFile.ExternalFiles.Keys.ToList()[i];
                    if (fileName.EndsWith(".bfsha")) {
                        renderer.ShaderFiles.Add(new BfshaLibrary.BfshaFile(new System.IO.MemoryStream(resFile.ExternalFiles[i].Data)));
                    }
                }
            }

            foreach (var model in resFile.Models.Values)
                renderer.Models.Add(PrepareModel(renderer, resFile, model));
            foreach (var tex in resFile.Textures.Values)
                renderer.Textures.Add(tex.Name, PrepareTexture(resFile, tex));

            foreach (var anim in resFile.SkeletalAnims.Values)
                renderer.SkeletalAnimations.Add(new BfresSkeletalAnim(anim, renderer.Name));
            foreach (var anim in resFile.ShaderParamAnims.Values)
                renderer.MaterialAnimations.Add(new BfresMaterialAnim(anim, renderer.Name));
            foreach (var anim in resFile.ColorAnims.Values)
                renderer.MaterialAnimations.Add(new BfresMaterialAnim(anim, renderer.Name));
            foreach (var anim in resFile.TexSrtAnims.Values)
                renderer.MaterialAnimations.Add(new BfresMaterialAnim(anim, renderer.Name));
            foreach (var anim in resFile.TexPatternAnims.Values)
                renderer.MaterialAnimations.Add(new BfresMaterialAnim(anim, renderer.Name));

            if (renderer.Models.Count > 0)
            {
                var render = (BfresModelRender)renderer.Models.FirstOrDefault();
                var positions = GetVertices(resFile, render, resFile.Models.Values.FirstOrDefault());
                renderer.BoundingSphere = GLFrameworkEngine.Utils.BoundingSphereGenerator.GenerateBoundingSphere(positions);
            }

            return true;
        }

        static BfresModelRender PrepareModel(BfresRender renderer, ResFile resFile, Model model)
        {
            BfresModelRender modelRender = new BfresModelRender();
            modelRender.Name = model.Name;

            //Caustics are drawn with projection in light pre pass
            if (modelRender.Name == "CausticsArea")
                modelRender.IsVisible = false;

            var genericModel = new STGenericModel();
            genericModel.Skeleton = new FSKL(model.Skeleton); 
            modelRender.ModelData = genericModel;
            modelRender.MeshInFrustum = new bool[model.Shapes.Count];
            for (int i = 0; i < model.Shapes.Count; i++)
            {
                modelRender.MeshInFrustum[i] = true;
                var shape = model.Shapes[i];

                var mesh = new BfresMeshRender(i);
                mesh.Name = shape.Name;
                mesh.BoundingNode = CalculateBounding(resFile, modelRender, model, shape);
                mesh.VertexSkinCount = shape.VertexSkinCount;
                mesh.BoneIndex = shape.BoneIndex;

                var mat = model.Materials[shape.MaterialIndex];
                var attributes = BfresGLLoader.LoadAttributes(model, shape, mat);
                var buffer = BfresGLLoader.LoadBufferData(resFile, model, shape, attributes);
                var groups = LoadMeshGroups(mesh, shape);
                var indices = BfresGLLoader.LoadIndexBufferData(shape);

                mesh.LODMeshes = groups;
                mesh.InitVertexBuffer(attributes, buffer, indices);

                var matRender = LoadMaterial(renderer, modelRender, mesh, mat, shape);
                if (matRender.IsTransparent)
                    mesh.Pass = Pass.TRANSPARENT;

                mesh.MaterialAsset = matRender;
                modelRender.Meshes.Add(mesh);

                modelRender.BoundingNode.Include(mesh.BoundingNode);
            }
            return modelRender;
        }

        static BfresMaterialRender LoadMaterial(BfresRender render, BfresModelRender model, BfresMeshRender meshRender, Material mat, Shape shape)
        {
            BfresMaterialRender matRender = new BfresMaterialRender(render, model);
            if (render.UseGameShaders && render.ShaderFiles.Count > 0) {
                matRender = new TurboNXRender(render, model);
            }

            matRender.Name = mat.Name;
            BfresMatGLConverter.ConvertRenderState(matRender, mat, mat.RenderState);

            matRender.SamplerAssign = mat.ShaderAssign.SamplerAssigns;
            matRender.ShaderArchiveName = mat.ShaderAssign.ShaderArchiveName;
            matRender.ShaderModelName = mat.ShaderAssign.ShadingModelName;

            foreach (var param in mat.ShaderParams)
                matRender.ShaderParams.Add(param.Key, param.Value);
            foreach (var option in mat.ShaderAssign.ShaderOptions)
                matRender.ShaderOptions.Add(option.Key, option.Value);

            if (matRender.ShaderOptions.ContainsKey("enable_color_buffer") && matRender.ShaderOptions["enable_color_buffer"] == "1")
                meshRender.UseColorBufferPass = true;

            matRender.LightMap = BfresMatGLConverter.GetRenderInfo(mat, "gsys_light_diffuse");

            if (BfresMatGLConverter.GetRenderInfo(mat, "gsys_dynamic_depth_shadow") == "1")
                meshRender.ProjectDynamicShadowMap = true;  //Project shadows to cast onto objects
            if (BfresMatGLConverter.GetRenderInfo(mat, "gsys_static_depth_shadow") == "1")
                meshRender.ProjectStaticShadowMap = true; //Project shadows to cast onto objects (for map models)

            if (BfresMatGLConverter.GetRenderInfo(mat, "gsys_static_depth_shadow_only") == "1")
                meshRender.IsDepthShadow = true; //Only draw in the shadow pass.
            if (BfresMatGLConverter.GetRenderInfo(mat, "gsys_pass") == "seal")
                meshRender.IsSealPass = true; //Draw over objects
            if (BfresMatGLConverter.GetRenderInfo(mat, "gsys_cube_map_only") == "1")
                meshRender.IsCubeMap = true; //Draw only in cubemaps
            if (BfresMatGLConverter.GetRenderInfo(mat, "gsys_cube_map") == "1")
                meshRender.RenderInCubeMap = true; //Draw in cubemaps

            if (meshRender.IsDepthShadow || meshRender.IsCubeMap)
                meshRender.IsVisible = false;

            for (int i = 0; i < mat.TextureRefs.Count; i++)
            {
                string name = mat.TextureRefs[i].Name;
                Sampler sampler = mat.Samplers[i];
                var texSampler = mat.Samplers[i].TexSampler;
                string samplerName = sampler.Name;
                string fragSampler = "";

                //Force frag shader sampler to be used 
                if (mat.ShaderAssign.SamplerAssigns.ContainsValue(samplerName))
                    mat.ShaderAssign.SamplerAssigns.TryGetKey(samplerName, out fragSampler);

                matRender.Samplers.Add(fragSampler);

                matRender.TextureMaps.Add(new STGenericTextureMap()
                {
                    Name = name,
                    Sampler = samplerName,
                    MagFilter = BfresMatGLConverter.ConvertMagFilter(texSampler.MagFilter),
                    MinFilter = BfresMatGLConverter.ConvertMinFilter(
                        texSampler.MipFilter,
                        texSampler.MinFilter),
                    WrapU = BfresMatGLConverter.ConvertWrapMode(texSampler.ClampX),
                    WrapV = BfresMatGLConverter.ConvertWrapMode(texSampler.ClampY),
                    LODBias = texSampler.LodBias,
                    MaxLOD = texSampler.MaxLod,
                    MinLOD = texSampler.MinLod,
                });
            }

            if (matRender is BfshaRenderer)
                ((BfshaRenderer)matRender).TryLoadShader(render, meshRender);

            return matRender;
        }

        static List<BfresPolygonGroupRender> LoadMeshGroups(BfresMeshRender meshRender, Shape shape)
        {
            List<BfresPolygonGroupRender> groups = new List<BfresPolygonGroupRender>();
            int offset = 0;
            foreach (var mesh in shape.Meshes) {
                groups.Add(new BfresPolygonGroupRender(meshRender, shape, mesh, 0, offset));
                int stride = 4;
                if (mesh.IndexFormat == BfresLibrary.GX2.GX2IndexFormat.UInt16 ||
                    mesh.IndexFormat == BfresLibrary.GX2.GX2IndexFormat.UInt16LittleEndian)
                {
                    stride = 2;
                }
                offset += (int)mesh.IndexCount * stride;
            }
            return groups;
        }

        static Vector3[] GetVertices(ResFile resFile, BfresModelRender modelRender, Model model)
        {
            List<Vector3> vertices = new List<Vector3>();
            foreach (var shape in model.Shapes.Values)
            {
                VertexBufferHelper helper = new VertexBufferHelper(
                     model.VertexBuffers[shape.VertexBufferIndex], resFile.ByteOrder);

                var positions = helper.Attributes.FirstOrDefault(x => x.Name == "_p0");
                var indices = helper.Attributes.FirstOrDefault(x => x.Name == "_i0");

                for (int i = 0; i < positions.Data.Length; i++)
                {
                    var position = new Vector3(positions.Data[i].X, positions.Data[i].Y, positions.Data[i].Z) * GLContext.PreviewScale;
                    //Calculate in worldspace
                    if (shape.VertexSkinCount == 1)
                    {
                        var index = (int)model.Skeleton.MatrixToBoneList[(int)indices.Data[i].X];
                        var transform = modelRender.ModelData.Skeleton.Bones[index].Transform;
                        position = Vector3.TransformPosition(position, transform);
                    }
                    else if (shape.VertexSkinCount == 0)
                    {
                        var transform = modelRender.ModelData.Skeleton.Bones[shape.BoneIndex].Transform;
                        position = Vector3.TransformPosition(position, transform);
                    }
                    vertices.Add(position);
                }
            }
            return vertices.ToArray();
        }

        static GLFrameworkEngine.BoundingNode CalculateBounding(ResFile resFile, BfresModelRender modelRender, Model model, Shape shape)
        {
            VertexBufferHelper helper = new VertexBufferHelper(
                model.VertexBuffers[shape.VertexBufferIndex], resFile.ByteOrder);

            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);

            var positions = helper.Attributes.FirstOrDefault(x => x.Name == "_p0");
            var indices = helper.Attributes.FirstOrDefault(x => x.Name == "_i0");

            for (int i = 0; i < positions.Data.Length; i++)
            {
                var position = new Vector3(positions.Data[i].X, positions.Data[i].Y, positions.Data[i].Z) * GLContext.PreviewScale;
                //Calculate in worldspace
                if (shape.VertexSkinCount == 1)
                {
                    var index = (int)model.Skeleton.MatrixToBoneList[(int)indices.Data[i].X];
                    var transform = modelRender.ModelData.Skeleton.Bones[index].Transform;
                    position = Vector3.TransformPosition(position, transform);
                }
                if (shape.VertexSkinCount == 0)
                {
                    var transform = modelRender.ModelData.Skeleton.Bones[shape.BoneIndex].Transform;
                    position = Vector3.TransformPosition(position, transform);
                }

                min.X = MathF.Min(min.X, position.X);
                min.Y = MathF.Min(min.Y, position.Y);
                min.Z = MathF.Min(min.Z, position.Z);
                max.X = MathF.Max(max.X, position.X);
                max.Y = MathF.Max(max.Y, position.Y);
                max.Z = MathF.Max(max.Z, position.Z);
            }
 
            return new GLFrameworkEngine.BoundingNode()
            {
                Radius = shape.RadiusArray.FirstOrDefault(),
                Center = (max - min) / 2f,
                Box = BoundingBox.FromMinMax(min, max),
        };
        }

        static GenericRenderer.TextureView PrepareTexture(ResFile resFile, TextureShared tex)
        {
            if (tex is BfresLibrary.WiiU.Texture)
            {
                FtexTexture ftex = new FtexTexture(resFile, (BfresLibrary.WiiU.Texture)tex);
                return new GenericRenderer.TextureView(ftex);
            }
            else
            {
                var texture = (BfresLibrary.Switch.SwitchTexture)tex;
                BntxTexture bntxTexture = new BntxTexture(texture.BntxFile, texture.Texture);
                return new GenericRenderer.TextureView(bntxTexture);
            }
        }
    }
}
