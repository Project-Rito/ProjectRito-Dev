using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using Toolbox.Core.ViewModels;
using OpenTK;

namespace UKingLibrary
{
    public class TagRender : EditableObject, IInstanceColorPickable, IFrustumCulling, IInstanceDrawable
    {
        public string Name;

        private Vector4 _color = Vector4.One;
        public Vector4 Color
        {
            get
            {
                return _color;
            }
            set
            {
                if (_color != value)
                    UpdateInstanceGroup = true;
                _color = value;
            }
        }

        static GLTexture LinkTagAndTexture = null;
        static GLTexture LinkTagNAndTexture = null;
        static GLTexture LinkTagOrTexture = null;
        static GLTexture LinkTagXOrTexture = null;
        static GLTexture LinkTagNOrTexture = null;
        static GLTexture LinkTagNoneTexture = null;
        static GLTexture LinkTagCountTexture = null;
        static GLTexture LinkTagPulseTexture = null;
        static GLTexture EventTagTexture = null;

        UVCubeRenderer CubeRenderer = null;
        StandardInstancedMaterial Material = new StandardInstancedMaterial();

        public bool EnableFrustumCulling => true;
        private bool _inFrustum;
        public bool InFrustum
        {
            get
            {
                return _inFrustum;
            }
            set
            {
                if (value != _inFrustum)
                    UpdateInstanceGroup = true;
                _inFrustum = value;
            }
        }

        private bool _isSelected;
        public override bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (_isSelected != value)
                    UpdateInstanceGroup = true;
                _isSelected = value;
            }
        }

        private bool _updateInstanceGroup = true;
        public virtual bool UpdateInstanceGroup { get { return _updateInstanceGroup; } set { _updateInstanceGroup = value; } }

        public BoundingNode Boundings = new BoundingNode()
        {
            Center = new Vector3(0, 0, 0),
            Box = new BoundingBox(new Vector3(-10), new Vector3(10)),
        };

        public bool IsInsideFrustum(GLContext context) {
            return context.Camera.InFustrum(Boundings);
        }

        public TagRender(string name, NodeBase parent) : base(parent)
        {
            //UpdateInstanceGroup = true;
            VisibilityChanged += (object sender, EventArgs e) =>
            {
                UpdateInstanceGroup = true;
            };
            RemoveCallback += (object sender, EventArgs e) =>
            {
                UpdateInstanceGroup = true;
            };

            Name = name;
            //Update boundings on transform changed
            this.Transform.TransformUpdated += delegate {
                Boundings.UpdateTransform(this.Transform.TransformMatrix);
            };
            UINode.Tag = this;
            ReloadTexture();
        }

        public bool GroupsWith(IInstanceDrawable drawable)
        {
            if (!(drawable is TagRender))
                return false;

            if ((((TagRender)drawable).InFrustum != InFrustum))
                return false;
            if ((((TagRender)drawable).IsVisible != IsVisible))
                return false;
            if ((((TagRender)drawable).IsSelected != IsSelected))
                return false;
            if (((TagRender)drawable).Color != Color)
                return false;
            if (((TagRender)drawable).Name != Name)
                return false;

            return true;
        }

        [Obsolete("Deprecated. Prefer the instanced version.")]
        public void DrawColorPicking(GLContext context)
        {
            DrawColorPicking(context, new List<GLTransform> { Transform });
        }
        public void DrawColorPicking(GLContext context, List<GLTransform> transforms)
        {
            List<Matrix4> modelMatrices = new List<Matrix4>(transforms.Count);
            foreach (var transform in transforms)
                modelMatrices.Add(transform.TransformMatrix);

            Prepare();

            CubeRenderer.DrawPicking(context, this, modelMatrices);
        }

        public override void DrawModel(GLContext context, Pass pass) { }

        public void DrawModel(GLContext context, Pass pass, List<GLTransform> transforms)
        {
            if (pass != Pass.OPAQUE || !this.InFrustum)
                return;

            List<Matrix4> modelMatrices = new List<Matrix4>(transforms.Count);
            foreach (var transform in transforms)
                modelMatrices.Add(transform.TransformMatrix);

            Prepare();

            Material.DisplaySelection = IsSelected | IsHovered;
            Material.HalfLambertShading = false;
            Material.ModelMatrices = modelMatrices;
            Material.Render(context);

            CubeRenderer.DrawWithSelection(context, IsSelected || IsHovered, transforms.Count);
        }

        private void ReloadTexture()
        {
            Prepare();

            Material.DiffuseTextureID = -1;
            Material.Color = this.Color;

            switch (Name)
            {
                case "LinkTagAnd":
                    Material.DiffuseTextureID = LinkTagAndTexture.ID;
                    break;
                case "LinkTagNAnd":
                    Material.DiffuseTextureID = LinkTagNAndTexture.ID;
                    break;
                case "LinkTagOr":
                    Material.DiffuseTextureID = LinkTagOrTexture.ID;
                    break;
                case "LinkTagXOr":
                    Material.DiffuseTextureID = LinkTagXOrTexture.ID;
                    break;
                case "LinkTagNOr":
                    Material.DiffuseTextureID = LinkTagNOrTexture.ID;
                    break;
                case "LinkTagNone":
                    Material.DiffuseTextureID = LinkTagNoneTexture.ID;
                    break;
                case "LinkTagCount":
                    Material.DiffuseTextureID = LinkTagCountTexture.ID;
                    break;
                case "LinkTagPulse":
                    Material.DiffuseTextureID = LinkTagPulseTexture.ID;
                    break;
                case "EventTag":
                    Material.DiffuseTextureID = EventTagTexture.ID;
                    break;
            }
        }

        private void Prepare()
        {
            if (CubeRenderer == null || CubeRenderer.IsDisposed)
                CubeRenderer = new UVCubeRenderer(10);

            if (LinkTagAndTexture == null)
                LinkTagAndTexture = GLTexture2D.FromBitmap(Properties.Resources.LinkTagAnd);
            if (LinkTagNAndTexture == null)
                LinkTagNAndTexture = GLTexture2D.FromBitmap(Properties.Resources.LinkTagNAnd);
            if (LinkTagOrTexture == null)
                LinkTagOrTexture = GLTexture2D.FromBitmap(Properties.Resources.LinkTagOr);
            if (LinkTagXOrTexture == null)
                LinkTagXOrTexture = GLTexture2D.FromBitmap(Properties.Resources.LinkTagXOr);
            if (LinkTagNOrTexture == null)
                LinkTagNOrTexture = GLTexture2D.FromBitmap(Properties.Resources.LinkTagNOr);
            if (LinkTagNoneTexture == null)
                LinkTagNoneTexture = GLTexture2D.FromBitmap(Properties.Resources.LinkTagNone);
            if (LinkTagCountTexture == null)
                LinkTagCountTexture = GLTexture2D.FromBitmap(Properties.Resources.LinkTagCount);
            if (LinkTagPulseTexture == null)
                LinkTagPulseTexture = GLTexture2D.FromBitmap(Properties.Resources.LinkTagPulse);
            if (EventTagTexture == null)
                EventTagTexture = GLTexture2D.FromBitmap(Properties.Resources.EventTag);
        }

        public override void Dispose()
        {
            CubeRenderer?.Dispose();
        }


        public static bool IsTag(string name)
        {
            if (name.StartsWith("LinkTag"))
                return true;
            
            switch (name)
            {
                case "EventTag":
                    return true;
            }

            return false;
        }
    }
}
