using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;

namespace GLFrameworkEngine
{
    public class RotateAction : ITransformAction
    {
        /// <summary>
        /// Toggles using gizmo.
        /// </summary>
        public bool UseGizmo = true;

        /// <summary>
        /// Determines if the translation action is in an active state or not.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the scale from being dragged.
        /// </summary>
        public Quaternion DeltaRotation = Quaternion.Identity;

        /// <summary>
        /// The origin before transformation is applied.
        /// </summary>
        public Vector3 OriginStart { get; set; }

        /// <summary>
        /// Gets or sets the transform settings.
        /// </summary>
        public TransformSettings Settings { get; set; }

        public RotateAction(TransformSettings settings)
        {
            Settings = settings;
        }

        private Vector2 startMousePos = new Vector2();

        public override string ToString()
        {
            var euler = Toolbox.Core.STMath.ToEulerAngles(DeltaRotation) * Toolbox.Core.STMath.Rad2Deg;

            return $"(X: {euler.X} Y: {euler.Y} Z: {euler.Z})";
        }

        static readonly double eighthPI = Math.PI / 8d;

        public int ResetTransform(GLContext context, TransformSettings settings)
        {
            OriginStart = Settings.Origin;
            //Reset position
            startMousePos = new Vector2(context.CurrentMousePoint.X, context.CurrentMousePoint.Y);
            //Check if the object gizmo is selected
            return 0;
        }

        public int TransformChanged(GLContext context, float x, float y, TransformSettings settings)
        {
            Vector2 centerPoint = context.WorldToScreen(Settings.Origin);
            Vector2 mousePos = new Vector2(x, y);

            double angle = (Math.Atan2(mousePos.X - centerPoint.X, mousePos.Y - centerPoint.Y) -
                           Math.Atan2(startMousePos.X - centerPoint.X, startMousePos.Y - centerPoint.Y));

            if (settings.SnapTransform)
                angle = Math.Round(angle / eighthPI) * eighthPI;

            DeltaRotation = GetRotation((float)angle);
            return 1;
        }

        public void ApplyTransform(List<GLTransform> previousTransforms, List<GLTransform> adjustedTransforms)
        {
            var transformTools = GLContext.ActiveContext.TransformTools;

            //Update all transforms
            for (int i = 0; i < adjustedTransforms.Count; i++) {
                var previous = previousTransforms[i];
                //Local space order
                var rotation = previous.Rotation * DeltaRotation;
                //World space order
                if (transformTools.TransformSettings.TransformMode == TransformSettings.TransformSpace.World)
                    rotation = DeltaRotation * previous.Rotation;
                //Text input to manually set specific values
                if (transformTools.TransformSettings.HasTextInput) {
                    float angle = transformTools.TransformSettings.TextInput;
                    rotation = GetRotation(MathHelper.DegreesToRadians(angle));
                }
                //Custom actions
                if (adjustedTransforms[i].CustomRotationActionCallback != null)
                {
                    adjustedTransforms[i].CustomRotationActionCallback.Invoke(
                       new GLTransform.CustomRotationArgs()
                       {
                           Origin = OriginStart,
                           PreviousTransform = previous,
                           Transform = adjustedTransforms[i],
                           DeltaRotation = DeltaRotation,
                           Rotation = rotation,
                       }, EventArgs.Empty);
                }
                else
                {
                    //Apply the rotation
                    adjustedTransforms[i].Rotation = rotation;

                    //Allow positions to be rotated from origin.
                    //Direct rotation
                    if (transformTools.TransformSettings.HasTextInput)
                    {
                        if (transformTools.TransformSettings.RotateFromOrigin)
                            adjustedTransforms[i].Position = Vector3.TransformPosition(previous.Position - OriginStart, Matrix4.CreateFromQuaternion(rotation)) + OriginStart;
                    }
                    else //Rotate by rotation difference
                    {
                        if (transformTools.TransformSettings.RotateFromOrigin)
                            adjustedTransforms[i].Position = Vector3.TransformPosition(previous.Position - OriginStart, Matrix4.CreateFromQuaternion(DeltaRotation)) + OriginStart;
                    }
                    adjustedTransforms[i].UpdateMatrix(true);
                }
            }
        }

        private Quaternion GetRotation(float angle)
        {
            Vector3 vec = GLContext.ActiveContext.Camera.InverseRotationMatrix.Row2;
            switch (Settings.ActiveAxis)
            {
                case TransformEngine.Axis.X:
                    vec = Vector3.UnitX;
                    break;
                case TransformEngine.Axis.Y:
                    vec = Vector3.UnitY;
                    break;
                case TransformEngine.Axis.Z:
                    vec = Vector3.UnitZ;
                    break;
            }

           return Quaternion.FromAxisAngle(vec, (float)angle);
        }

        public int FinishTransform()
        {
            return 0;
        }
    }
}
