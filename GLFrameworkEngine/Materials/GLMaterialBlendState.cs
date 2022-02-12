using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace GLFrameworkEngine
{
    [Serializable]
    public class GLMaterialBlendState
    {
        public static readonly GLMaterialBlendState TranslucentAlphaOne = new GLMaterialBlendState()
        {
            AlphaSrc = BlendingFactorSrc.One,
            AlphaDst = BlendingFactorDest.One,
            BlendColor = true,
            DepthWrite = false,
        };

        public static readonly GLMaterialBlendState Translucent = new GLMaterialBlendState()
        {
            BlendColor = true,
            AlphaTest = false,
            ColorSrc = BlendingFactorSrc.SrcAlpha,
            ColorDst = BlendingFactorDest.OneMinusSrcAlpha,
            ColorOp = BlendEquationMode.FuncAdd,
            AlphaSrc = BlendingFactorSrc.One,
            AlphaDst =    BlendingFactorDest.Zero,
            AlphaOp = BlendEquationMode.FuncAdd,
            State = BlendState.Translucent,
            DepthWrite = false,
        };

        public static readonly GLMaterialBlendState Opaque = new GLMaterialBlendState()
        {
            BlendColor = false,
            AlphaTest = false,
            State = BlendState.Opaque,
        };

        public bool BlendMask = false;

        public bool DepthTest = true;
        public DepthFunction DepthFunction = DepthFunction.Lequal;
        public bool DepthWrite = true;

        public bool AlphaTest = true;
        public AlphaFunction AlphaFunction = AlphaFunction.Gequal;
        public float AlphaValue = 0.5f;

        public BlendingFactorSrc ColorSrc = BlendingFactorSrc.SrcAlpha;
        public BlendingFactorDest ColorDst = BlendingFactorDest.OneMinusSrcAlpha;
        public BlendEquationMode ColorOp = BlendEquationMode.FuncAdd;

        public BlendingFactorSrc AlphaSrc = BlendingFactorSrc.One;
        public BlendingFactorDest AlphaDst = BlendingFactorDest.Zero;
        public BlendEquationMode AlphaOp = BlendEquationMode.FuncAdd;

        public BlendState State = BlendState.Opaque;

        public Vector4 Color = Vector4.Zero;

        public bool BlendColor = false;

        public enum BlendState
        {
            Opaque,
            Mask,
            Translucent,
            Custom,
        }

        public void RenderDepthTest()
        {
            if (DepthTest)
            {
                GLH.Enable(EnableCap.DepthTest);
                GLH.DepthFunc(DepthFunction);
                GLH.DepthMask(DepthWrite);
            }
            else
                GLH.Disable(EnableCap.DepthTest);
        }

        public void RenderAlphaTest()
        {
            if (AlphaTest)
            {
                GLH.Enable(EnableCap.AlphaTest);
                GLH.AlphaFunc(AlphaFunction, AlphaValue);
            }
            else
                GLH.Disable(EnableCap.AlphaTest);
        }

        public void RenderBlendState()
        {
            if (BlendColor || BlendMask)
            {
                GLH.Enable(EnableCap.Blend);
                GLH.BlendFuncSeparate(ColorSrc, ColorDst, AlphaSrc, AlphaDst);
                GLH.BlendEquationSeparate(ColorOp, AlphaOp);
                GLH.BlendColor(Color.X, Color.Y, Color.Z, Color.W);
            }
            else
                GLH.Disable(EnableCap.Blend);
        }
    }
}
