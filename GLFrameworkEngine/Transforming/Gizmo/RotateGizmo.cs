using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace GLFrameworkEngine
{
    public class RotateGizmo : IGizmoRender
    {
        LineRender[] LineRenders = new LineRender[3];

        public Vector3[] _colors = new Vector3[3] {
             new Vector3(1.0f, 0, 0), new Vector3(0, 0, 1.0f), new Vector3(0, 1.0f, 0)
        };

        public Vector4[] _rotations = new Vector4[3] {
            new Vector4(0, 1, 0, 90),  new Vector4(1, 0, 0, -90), new Vector4(0, 0, 1, -90)
        };

        //Shapes for drawing axis
        static CylinderRenderer CylinderRenderer = null;
        static Circle2DRenderer CircleRenderer = null;
        static Circle2DRenderer CircleChangeSegmentRenderer = null;
        static SphereRender SphereRender = null;

        public RotateGizmo()
        {
        }

        public TransformEngine.Axis UpdateAxisSelection(GLContext context, Vector3 position, Quaternion rotation, Vector2 point, TransformSettings settings)
        {
            Matrix4 invTrasnform = (Matrix4.CreateTranslation(position) * Matrix4.CreateFromQuaternion(rotation)).Inverted();
            var axis = TransformEngine.Axis.None;

            var ray = context.PointScreenRay((int)point.X, (int)point.Y);
            var gizmoScale = settings.GizmoScale;

            //From brawlbox
            float radius = gizmoScale * 2.2f;
            if (GLMath.RayIntersectsLineSphere(ray, position, radius, out Vector3 result))
            {
                var distance = Vector3.Distance(result, position);
                if (Math.Abs(distance - radius) < (radius * 1.5f)) //Point lies within orb radius
                {
                    Vector3 angle = Angles(Vector3.TransformPosition(result, invTrasnform)) * Toolbox.Core.STMath.Rad2Deg;
                    angle.X = (float)Math.Abs(angle.X);
                    angle.Y = (float)Math.Abs(angle.Y);
                    angle.Z = (float)Math.Abs(angle.Z);

                    float _axisSnapRange = 15.0f;
                    if (Math.Abs(angle.Y - 90.0f) <= _axisSnapRange)
                        axis = TransformEngine.Axis.X;
                    else if (angle.X >= (180.0f - _axisSnapRange) || angle.X <= _axisSnapRange)
                        axis = TransformEngine.Axis.Y;
                    else if (angle.Y >= (180.0f - _axisSnapRange) || angle.Y <= _axisSnapRange)
                        axis = TransformEngine.Axis.Z;
                }
                  else if (Math.Abs(distance - (radius * 1.5f)) < (radius * 1.8f)) //Point lies on circ line
                    axis = TransformEngine.Axis.All;

                return axis;
            }
            else if (GLMath.RayIntersectsLineSphere(ray, position, gizmoScale * 2.5f, out Vector3 result2))
            {
                axis = TransformEngine.Axis.All;
            }
            return axis;
        }

        Vector3 Angles(Vector3 i)
        {
            Vector3 ni = new Vector3();
            ni.X = (float)Math.Atan2(i.Y, -i.Z);
            ni.Y = (float)Math.Atan2(-i.Z, i.X);
            ni.Z = (float)Math.Atan2(i.Y, i.X);
            return ni;
        }

        void Init()
        {
            CylinderRenderer = new CylinderRenderer(0.04f, 2);
            CircleRenderer = new Circle2DRenderer(64);
            CircleChangeSegmentRenderer = new Circle2DRenderer(32, PrimitiveType.TriangleStrip);
            SphereRender = new SphereRender(1, 32, 32);
        }

        public void Render(GLContext context, Vector3 position, Quaternion rotation, float scale, bool isMoving, bool[] isSelected, bool[] isHovered)
        {
            if (CylinderRenderer == null)
                Init();

            var shader = GlobalShaders.GetShader("GIZMO");
            context.CurrentShader = shader;

            GL.Disable(EnableCap.DepthTest);

            //Ignore the previous depth from the current drawn buffer.
            //This is to prevent depth from clipping in models.
            GL.Clear(ClearBufferMask.DepthBufferBit);

            var translateMtx = Matrix4.CreateTranslation(position);
            var scaleMtx = Matrix4.CreateScale(scale);
            var rotationMtx = Matrix4.CreateFromQuaternion(rotation);
            var transform = scaleMtx * rotationMtx * translateMtx;

            bool displayGizmo = true;

            if (displayGizmo)
            {
                GL.LineWidth(5);
                GL.PushAttrib(AttribMask.DepthBufferBit);

                transform = scaleMtx * translateMtx;

                if (!isMoving)
                    DrawOrb(context, ref transform, isMoving, isHovered[6]);

                transform = scaleMtx * rotationMtx * translateMtx;

                for (int i = 0; i < 3; i++) {
                    if (isMoving && !isSelected[i])
                        continue;

                    DrawAxis(context, isMoving && isSelected[i], isHovered[i], ref transform,_rotations[i], _colors[i]);
                }

                GL.PopAttrib();

                if (!isMoving && !isSelected.Any(x => x))
                    GizmoCenterRender.Draw(context, position, scale, new Vector4(1));
            }

            GL.LineWidth(2);

            //Draw lines for the selected axis objects during movement
            if (isMoving && !displayGizmo)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (!isSelected[i])
                        continue;

                    context.CurrentShader.SetVector4("color", new Vector4(_colors[i], 1.0f));
                    context.CurrentShader.SetVector4("selectionColor", Vector4.Zero);
                    context.CurrentShader.SetMatrix4x4("mtxMdl", ref transform);

                    if (LineRenders[i] == null) LineRenders[i] = new LineRender();

                    LineRenders[i].Draw(
                        GetSelectedAxisVector3(i) * context.Camera.ZFar,
                        -(GetSelectedAxisVector3(i) * context.Camera.ZFar),
                        new Vector4(1), false);
                }
            }

            GL.LineWidth(1);

            context.CurrentShader = null;
            GL.Enable(EnableCap.DepthTest);
        }

        public void DrawOrb(GLContext context, ref Matrix4 transform, bool isMoving, bool isHovered)
        {
            var matrix = Matrix4.CreateScale(2) *  new Matrix4(context.Camera.InverseRotationMatrix) * transform;
            var matrix2 = Matrix4.CreateScale(2.5f) * new Matrix4(context.Camera.InverseRotationMatrix) * transform;

            context.CurrentShader.SetMatrix4x4("mtxMdl", ref matrix);
            context.CurrentShader.SetVector4("selectionColor", Vector4.Zero);

            GL.Enable(EnableCap.DepthTest);

            GLMaterialBlendState.Translucent.RenderBlendState();
            context.CurrentShader.SetVector4("color", new Vector4(0.7f, 0.7f, 0.7f, 0.15f));
            SphereRender.Draw(context);

            GLMaterialBlendState.Opaque.RenderBlendState();

            GL.Disable(EnableCap.DepthTest);

            context.CurrentShader.SetVector4("color", new Vector4(0.4f, 0.4f, 0.4f, 1));
            CircleRenderer.Draw(context);

            context.CurrentShader.SetMatrix4x4("mtxMdl", ref matrix2);

            context.CurrentShader.SetVector4("color", new Vector4(1, 1, 1, 1));
            if (isHovered)
                context.CurrentShader.SetVector4("color", new Vector4(1, 1, 0, 1));
            CircleRenderer.Draw(context);

            GL.Enable(EnableCap.DepthTest);
        }

        public void DrawAxis(GLContext context, bool isMoving, bool isSelected, ref Matrix4 transform, Vector4 rotation, Vector3 color)
        {
            var rotationMatrix = Matrix4.CreateFromAxisAngle(rotation.Xyz, MathHelper.DegreesToRadians(rotation.W));
            var scaleMatrix = Matrix4.CreateScale(2);
            var matrix = scaleMatrix * rotationMatrix * transform;

            context.CurrentShader.SetMatrix4x4("mtxMdl", ref matrix);
            context.CurrentShader.SetVector4("color", new Vector4(color, 1.0f));
            context.CurrentShader.SetVector4("selectionColor", Vector4.Zero);

            //Draw a filled circle if in a moving action
            if (isMoving)
            {
                GL.Disable(EnableCap.DepthTest);
                CircleRenderer.Draw(context);

               // context.CurrentShader.SetVector4("color", new Vector4(1, 1, 0, 1));
                //CircleChangeSegmentRenderer.SetCustomSegment(startAngleRotate, endAngleRotate);
               // CircleChangeSegmentRenderer.Draw(context);
            }
            else
            {
                if (isSelected)
                    context.CurrentShader.SetVector4("selectionColor", new Vector4(1, 1, 0, 1));

                CircleRenderer.Draw(context);
            }
        }

        private Vector3 GetSelectedAxisVector3(int axis)
        {
            switch (axis)
            {
                case 0: return Vector3.UnitX;
                case 1: return Vector3.UnitY;
                case 2: return Vector3.UnitZ;
                default:
                    return Vector3.UnitZ;
            }
        }
    }
}
