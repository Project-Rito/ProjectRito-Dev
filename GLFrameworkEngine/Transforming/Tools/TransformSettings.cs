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

        public Vector3 Origin { get; set; }

        public Vector3 PlaneNormal = new Vector3(0, 1, 0);

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

        public Quaternion Rotation = Quaternion.Identity;

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
