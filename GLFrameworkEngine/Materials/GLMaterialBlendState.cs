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
                GLL.Enable(EnableCap.DepthTest);
                GLL.DepthFunc(DepthFunction);
                GLL.DepthMask(DepthWrite);
            }
            else
                GLL.Disable(EnableCap.DepthTest);
        }

        public void RenderAlphaTest()
        {
            if (AlphaTest)
            {
                GLL.Enable(EnableCap.AlphaTest);
                GLL.AlphaFunc(AlphaFunction, AlphaValue);
            }
            else
                GLL.Disable(EnableCap.AlphaTest);
        }

        public void RenderBlendState()
        {
            if (BlendColor || BlendMask)
            {
                GLL.Enable(EnableCap.Blend);
                GLL.BlendFuncSeparate(ColorSrc, ColorDst, AlphaSrc, AlphaDst);
                GLL.BlendEquationSeparate(ColorOp, AlphaOp);
                GLL.BlendColor(Color.X, Color.Y, Color.Z, Color.W);
            }
            else
                GLL.Disable(EnableCap.Blend);
        }
    }
}
