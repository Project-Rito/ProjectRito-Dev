using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    /// <summary>
    /// Represents a generic material renderer used by a generic mesh.
    /// </summary>
    public class GenericMaterialRender
    {
        StandardMaterial MaterialData = new StandardMaterial();

        public Pass Pass = Pass.OPAQUE;

        public virtual void Init(GenericMeshRender meshRender, STGenericMesh mesh)
        {
            MaterialData.HalfLambertShading = true;
            MaterialData.Color = new OpenTK.Vector4(0.5f, 0.5f, 0.5f, 1.0f);

            meshRender.Drawer.ClearAttributes();
            meshRender.Drawer.AddAttributes(RenderAttribute.GetAttributes<VertexPositionNormalTexCoord>());
        }

        public virtual void Render(GLContext context, EditableObject obj)
        {
            MaterialData.DisplaySelection = obj.IsSelected || obj.IsHovered;
            MaterialData.ModelMatrix = obj.Transform.TransformMatrix;
            MaterialData.Render(context);
        }

        /// <summary>
        /// Updates the current vertex data with the set of vertices provided by the generic mesh.
        /// </summary>
        public virtual void UpdateVertexData(GenericMeshRender meshRender)
        {
            var vertices = new VertexPositionNormalTexCoord[meshRender.MeshData.Vertices.Count];
            for (int i = 0; i < meshRender.MeshData.Vertices.Count; i++)
            {
                vertices[i] = new VertexPositionNormalTexCoord()
                {
                    Position = meshRender.MeshData.Vertices[i].Position,
                    TexCoord = meshRender.MeshData.Vertices[i].TexCoords.Length > 0 ? meshRender.MeshData.Vertices[i].TexCoords[0] : new OpenTK.Vector2(),
                    Normal = meshRender.MeshData.Vertices[i].Normal,
                };
            }

            meshRender.Drawer.SetData(vertices, 0);
            meshRender.Drawer.InitVertexBufferObject();
        }
    }
}
