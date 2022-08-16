using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class LinkingTool
    {
        public bool DisplayAllLinks = false;

        public bool IsActive => SourceObject != null;

        bool CanEdit = true;

        RenderMesh<LinkVertex> LinkRender;

        IObjectLink SourceObject;
        IObjectLink DestObject;
        IObjectLink CandidateObject;

        enum ConnectionType
        {
            None,
            ToSource,
            ToDest,
        }

        ConnectionType ConnectionAction;

        public void OnMouseDown(GLContext context)
        {
            if (!CanEdit)
                return;

            //Create new connection
            if (KeyInfo.EventInfo.KeyAlt)
            {
                //Check for a picked model during a linkable operation
                Vector2 position = new Vector2(MouseEventInfo.Position.X, context.Height - MouseEventInfo.Position.Y);
                var pickable = context.Scene.FindPickableAtPosition(context, position);
                //No linking operation if no pickable is found
                if (pickable == null)
                    return;

                //Start a link operation
                if (pickable is IObjectLink && ((IObjectLink)pickable).EnableLinking)
                    SetLinkSource(context, pickable);
            }
        }

        public void OnMouseMove(GLContext context)
        {
            if (!IsActive)
                return;

            //Check for a picked model during a linkable operation
            Vector2 position = new Vector2(MouseEventInfo.Position.X, context.Height - MouseEventInfo.Position.Y);
            IObjectLink pickable = context.Scene.FindPickableAtPosition(context, position) as IObjectLink;
            CandidateObject = pickable?.EnableLinking == true && pickable != SourceObject ? pickable : null;
        }

        public void OnMouseUp(GLContext context)
        {
            if (!IsActive)
                return;

            //Apply the link when the mouse has been released
            if (CandidateObject != null)
                LinkObject(SourceObject, CandidateObject);
            else
                UnlinkObject(SourceObject, DestObject);

            SourceObject = null;
            DestObject = null;
            CandidateObject = null;
        }

        private void LinkObject(IObjectLink source, IObjectLink dest)
        {
            if (!source.DestObjectLinks.Contains((ITransformableObject)dest))
            {
                source.DestObjectLinks.Add((ITransformableObject)dest);
                source.OnObjectLink((ITransformableObject)dest);
            }

            if (!dest.SourceObjectLinks.Contains((ITransformableObject)source))
                dest.SourceObjectLinks.Add((ITransformableObject)source);
        }

        private void UnlinkObject(IObjectLink source, IObjectLink dest)
        {
            if (dest == null)
                return;

            if (source.DestObjectLinks.Contains((ITransformableObject)dest))
            {
                source.DestObjectLinks.Remove((ITransformableObject)dest);
                source.OnObjectUnlink((ITransformableObject)dest);
            }

            if (dest.SourceObjectLinks.Contains((ITransformableObject)source))
                dest.SourceObjectLinks.Remove((ITransformableObject)source);
        }

        public void SetLinkSource(GLContext context, ITransformableObject linkableObject)
        {
            if (!(linkableObject is IObjectLink && ((IObjectLink)linkableObject).EnableLinking)) //Object not a linkable type
                return;

            //Set the source link from a linkable object.
            ConnectionAction = ConnectionType.ToDest;
            SourceObject = (IObjectLink)linkableObject;
        }

        public void Render(GLContext context, float x, float y)
        {
            if (!IsActive)
                return;

            //Don't draw a link connector until a source is set
            if (SourceObject == null)
            {
                foreach (var ob in context.Scene.Objects) {
                    if (ob is IObjectLink) {
                        var link = (IObjectLink)ob;
                        foreach (var dest in link.DestObjectLinks)
                        {
                            Vector3 positon1 = ((ITransformableObject)link).Transform.Position;
                            Vector3 positon2 = ((ITransformableObject)dest).Transform.Position;
                            Vector2 srcPos1 = context.WorldToScreen(positon1);
                            Vector2 destPos2 = context.WorldToScreen(positon2);

                            DrawConnection(context, srcPos1, destPos2, true);
                        }
                    }
                }
                return;
            }

            //Object links are always transformable with a position. Get the world pos
            Vector3 positon = ((ITransformableObject)SourceObject).Transform.Position;
            Vector2 srcPos = context.WorldToScreen(positon);

            //Draw the current mouse placement
            DrawConnection(context, srcPos, new Vector2(x, y), CandidateObject != null);
        }

        private void DrawConnection(GLContext context, Vector2 srcPos, Vector2 destPos, bool hasDest)
        {
            var normalized1 = context.NormalizeMouseCoords(srcPos);
            var normalized2 = context.NormalizeMouseCoords(destPos);

            if (LinkRender == null)
                LinkRender = new RenderMesh<LinkVertex>(new LinkVertex[0], PrimitiveType.LineLoop);

            LinkRender.UpdateVertexData(new LinkVertex[] {
                 new LinkVertex(normalized1, ObjectLinkDrawer.SrcColor_Edit),
                 new LinkVertex(normalized2, ObjectLinkDrawer.DestColor_Edit),
            });

            //Connection render
            GL.LineWidth(5);

            var mat = new LinkRenderMaterial();
            if (hasDest)
                mat.UseVertexColors = true;
            else
            {
                mat.UseVertexColors = false;
                mat.Color = new Vector4(0.4f, 0.4f, 0.4f, 1.0f);
            }
            mat.Render(context);
            LinkRender.UpdatePrimitiveType(PrimitiveType.LineLoop);
            LinkRender.Draw(context);

            GL.LineWidth(1);

            GL.PointSize(10);

            LinkRender.UpdatePrimitiveType(PrimitiveType.Points);
            LinkRender.Draw(context);

            GL.PointSize(1);
        }
    }

    struct LinkVertex 
    {
        [RenderAttribute("vPosition", VertexAttribPointerType.Float, 0)]
        public Vector2 Position;

        [RenderAttribute("vColor", VertexAttribPointerType.Float, 8)]
        public Vector4 vColor;

        public LinkVertex(Vector2 position, Vector4 color)
        {
            Position = position;
            vColor = color;
        }
    }

    class LinkRenderMaterial
    {
        public Vector2 Position = Vector2.Zero;
        public float Scale = 1.0f;
        public bool UseVertexColors = false;
        public Vector4 Color = Vector4.One;

        public void Render(GLContext context)
        {
            var shader = GlobalShaders.GetShader("LINKING");
            context.CurrentShader = shader;

            var vp = Matrix4.Identity;
            shader.SetMatrix4x4(GLConstants.ViewProjMatrix, ref vp);

            shader.SetFloat("Time", 0);
            shader.SetFloat("scale", Scale);
            shader.SetVector2("pos", Position);
            shader.SetVector4("color", Color);

            shader.SetBoolToInt("useVertexColors", UseVertexColors);
        }
    }
}
