using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

namespace GLFrameworkEngine
{
    public class TransformSettings
    {
        /// <summary>
        /// The scale of the gizmo adjusted on the distance of the camera and gizmo itself.
        /// Updates during the render loop for the gizmo rendering.
        /// </summary>
        public float GizmoScale = 1.0f;

        /// <summary>
        /// The angle of the current rotation gizmo.
        /// Used for the UI to draw an angle difference in the arc angle.
        /// </summary>
        public float RotationAngle { get; set; }

        public Vector3 Origin { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Vector3 RotationStartVector { get; set; }

        /// <summary>
        /// Gets or sets the active axis.
        /// </summary>
        public TransformEngine.Axis ActiveAxis { get; set; }

        public float TextInput = 0.0f;
        public bool HasTextInput = false;

        public bool DisplayTranslationOriginLines = true;
        public bool DisplayGizmo = true;

        public bool RotateFromOrigin = true;

        public bool CollisionDetect = true;

        private Quaternion _rotation = Quaternion.Identity;
        public Quaternion Rotation
        {
            get { return _rotation; }
            set
            {
                _rotation = value;
                RotationMatrix = Matrix4.CreateFromQuaternion(_rotation);
            }
        }

        public Matrix4 RotationMatrix;

        public Vector3 TranslateSnapFactor { get; set; } = new Vector3(0.25f);
        public Vector3 ScaleSnapFactor { get; set; } = new Vector3(0.25f);
        public Vector3 RotateSnapFactor { get; set; } = new Vector3(45);

        public bool SnapTransform;

        public TransformSpace TransformMode { get; set; } = TransformSpace.World;
        public PivotSpace PivotMode { get; set; } = PivotSpace.Selected;

        public bool IsLocal => TransformMode == TransformSpace.Local;
        public bool IsWorld => TransformMode == TransformSpace.World;

        public enum PivotSpace
        {
            Selected,
            Individual,
        }

        public enum TransformSpace
        {
            World,
            Local,
        }
    }
}
