using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class GLH : GL
    {
        // Variables to enable/disable layer features
        public static bool EnableInstancing = false;

        // Variables to keep track of calls to see which ones can be instanced/batched together
        private static List<InstancedDrawCall> InstancedDrawCalls = new List<InstancedDrawCall>();

        private class InstancedDrawCall
        {
            public List<GLTransform> Transforms;
            public Action Setup;
            public DrawFunc DrawFunc;
        }

        private class DrawCall
        {
            public GLTransform Transform;
            public Action Setup;
            public DrawFunc DrawFunc;
        }

        private class DrawFunc
        {
            public string Name;
            public List<dynamic> Params;
        }

        private static DrawCall ThisCall;

        // Setup Functions
        public new static void BindVertexArray(int array)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.BindVertexArray(array);
                };
            }
            else 
                GL.BindVertexArray(array);
        }

        public new static void Enable(EnableCap cap)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.Enable(cap);
                };
            }
            else
                GL.Enable(cap);
        }
        public new static void Enable(IndexedEnableCap target, int index)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.Enable(target, index);
                };
            }
            else
                GL.Enable(target, index);
        }
        public new static void Enable(IndexedEnableCap target, uint index)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.Enable(target, index);
                };
            }
            else
                GL.Enable(target, index);
        }

        public new static void DepthFunc(DepthFunction func)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.DepthFunc(func);
                };
            }
            else
                GL.DepthFunc(func);
        }

        public new static void DepthRange(double near, double far)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.DepthRange(near, far);
                };
            }
            else
                GL.DepthRange(near, far);
        }
        public new static void DepthRange(float near, float far)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.DepthRange(near, far);
                };
            }
            else
                GL.DepthRange(near, far);
        }

        public new static void DepthMask(bool flag)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.DepthMask(flag);
                };
            }
            else
                GL.DepthMask(flag);
        }

        public new static void PolygonMode(MaterialFace materialFace, PolygonMode polygonMode)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.PolygonMode(materialFace, polygonMode);
                };
            }
            else
                GL.PolygonMode(materialFace, polygonMode);
        }

        // Draw Functions
        public new static void DrawElements(BeginMode mode, int count, DrawElementsType type, int indices)
        {
            if (EnableInstancing)
            {
                ThisCall.DrawFunc = new DrawFunc()
                {
                    Name = "DrawElements(BeginMode, int, DrawElementsType int)",
                    Params = {
                        mode,
                        count,
                        type,
                        indices
                    }
                };
                GroupCall();
            }
            else
                GL.DrawElements(mode, count, type, indices);
        }
        public new static void DrawElements(BeginMode mode, int count, DrawElementsType type, IntPtr indices)
        {
            if (EnableInstancing)
            {
                ThisCall.DrawFunc = new DrawFunc()
                {
                    Name = "DrawElements(BeginMode, int, DrawElementsType IntPtr)",
                    Params = {
                        mode,
                        count,
                        type,
                        indices
                    }
                };
                GroupCall();
            }
            else
                GL.DrawElements(mode, count, type, indices);
        }
        public new static void DrawElements<T>(BeginMode mode, int count, DrawElementsType type, [In] [Out] T[] indices) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.DrawFunc = new DrawFunc()
                {
                    Name = "DrawElements<T>(BeginMode, int, DrawElementsType [In] [Out] T[])",
                    Params = {
                        mode,
                        count,
                        type,
                        indices
                    }
                };
                GroupCall();
            }
            else
                GL.DrawElements(mode, count, type, indices);
        }
        public new static void DrawElements<T>(BeginMode mode, int count, DrawElementsType type, [In][Out] T[,] indices) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.DrawFunc = new DrawFunc()
                {
                    Name = "DrawElements<T>(BeginMode, int, DrawElementsType [In] [Out] T[,])",
                    Params = {
                        mode,
                        count,
                        type,
                        indices
                    }
                };
                GroupCall();
            }
            else
                GL.DrawElements(mode, count, type, indices);
        }
        public new static void DrawElements<T>(BeginMode mode, int count, DrawElementsType type, [In][Out] T[,,] indices) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.DrawFunc = new DrawFunc()
                {
                    Name = "DrawElements<T>(BeginMode, int, DrawElementsType [In] [Out] T[,,])",
                    Params = {
                        mode,
                        count,
                        type,
                        indices
                    }
                };
                GroupCall();
            }
            else
                GL.DrawElements(mode, count, type, indices);
        }
        public new static void DrawElements<T>(BeginMode mode, int count, DrawElementsType type, [In][Out] ref T indices) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.DrawFunc = new DrawFunc()
                {
                    Name = "DrawElements<T>(BeginMode, int, DrawElementsType [In] [Out] ref T)",
                    Params = {
                        mode,
                        count,
                        type,
                        indices
                    }
                };
                GroupCall();
            }
            else
                GL.DrawElements(mode, count, type, ref indices);
        }
        public new static void DrawElements(PrimitiveType mode, int count, DrawElementsType type, int indices)
        {
            if (EnableInstancing)
            {
                ThisCall.DrawFunc = new DrawFunc()
                {
                    Name = "DrawElements(PrimitiveType, int, DrawElementsType, int)",
                    Params = {
                        mode,
                        count,
                        type,
                        indices
                    }
                };
                GroupCall();
            }
            else
                GL.DrawElements(mode, count, type, indices);
        }
        public new static void DrawElements(PrimitiveType mode, int count, DrawElementsType type, IntPtr indices)
        {
            if (EnableInstancing)
            {
                ThisCall.DrawFunc = new DrawFunc()
                {
                    Name = "DrawElements(PrimitiveType, int, DrawElementsType, IntPtr)",
                    Params = {
                        mode,
                        count,
                        type,
                        indices
                    }
                };
                GroupCall();
            }
            else
                GL.DrawElements(mode, count, type, indices);
        }
        public new static void DrawElements<T>(PrimitiveType mode, int count, DrawElementsType type, [In] [Out] T[] indices) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.DrawFunc = new DrawFunc()
                {
                    Name = "DrawElements(PrimitiveType, int, DrawElementsType, [In] [Out] T[])",
                    Params = {
                        mode,
                        count,
                        type,
                        indices
                    }
                };
                GroupCall();
            }
            else
                GL.DrawElements(mode, count, type, indices);
        }
        public new static void DrawElements<T>(PrimitiveType mode, int count, DrawElementsType type, [In][Out] T[,] indices) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.DrawFunc = new DrawFunc()
                {
                    Name = "DrawElements(PrimitiveType, int, DrawElementsType, [In] [Out] T[,])",
                    Params = {
                        mode,
                        count,
                        type,
                        indices
                    }
                };
                GroupCall();
            }
            else
                GL.DrawElements(mode, count, type, indices);
        }
        public new static void DrawElements<T>(PrimitiveType mode, int count, DrawElementsType type, [In][Out] T[,,] indices) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.DrawFunc = new DrawFunc()
                {
                    Name = "DrawElements(PrimitiveType, int, DrawElementsType, [In] [Out] T[,,])",
                    Params = {
                        mode,
                        count,
                        type,
                        indices
                    }
                };
                GroupCall();
            }
            else
                GL.DrawElements(mode, count, type, indices);
        }
        public new static void DrawElements<T>(PrimitiveType mode, int count, DrawElementsType type, [In][Out] ref T indices) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.DrawFunc = new DrawFunc()
                {
                    Name = "DrawElements(PrimitiveType, int, DrawElementsType, [In] [Out] ref T)",
                    Params = {
                        mode,
                        count,
                        type,
                        indices
                    }
                };
                GroupCall();
            }
            else
                GL.DrawElements(mode, count, type, ref indices);
        }



        private static void GroupCall()
        {
            bool foundCall = false;
            foreach (var call in InstancedDrawCalls)
            {
                if (call.Setup == ThisCall.Setup)
                {
                    call.Transforms.Add(ThisCall.Transform);
                    call.DrawFunc = ThisCall.DrawFunc;
                    foundCall = true;
                    break;
                }
            }
            if (!foundCall)
            {
                InstancedDrawCalls.Add(new InstancedDrawCall()
                {
                    Transforms = { ThisCall.Transform },
                    Setup = ThisCall.Setup,
                    DrawFunc = ThisCall.DrawFunc
                });
            }
            ThisCall = new DrawCall(); // We don't actually need to clear this.. but this will be simpler for debugging purposes. (It can produce null errors)
        }

        /// <summary>
        /// Should be called when all drawing is done
        /// </summary>
        public static void EndCall()
        {
            foreach (var call in InstancedDrawCalls)
            {
                call.Setup?.Invoke();
                if (call.DrawFunc.Name == "DrawElements(BeginMode, int, DrawElementsType, int)")
                    GL.DrawElements(call.DrawFunc.Params[0], call.DrawFunc.Params[1], call.DrawFunc.Params[2], call.DrawFunc.Params[3]);
                if (call.DrawFunc.Name == "DrawElements(BeginMode, int, DrawElementsType, IntPtr)")
                    GL.DrawElements(call.DrawFunc.Params[0], call.DrawFunc.Params[1], call.DrawFunc.Params[2], call.DrawFunc.Params[3]);
                if (call.DrawFunc.Name == "DrawElements(BeginMode, int, DrawElementsType, [In] [Out] T[])")
                    GL.DrawElements(call.DrawFunc.Params[0], call.DrawFunc.Params[1], call.DrawFunc.Params[2], call.DrawFunc.Params[3]);
                if (call.DrawFunc.Name == "DrawElements(BeginMode, int, DrawElementsType, [In] [Out] T[,])")
                    GL.DrawElements(call.DrawFunc.Params[0], call.DrawFunc.Params[1], call.DrawFunc.Params[2], call.DrawFunc.Params[3]);
                if (call.DrawFunc.Name == "DrawElements(BeginMode, int, DrawElementsType, [In] [Out] T[,,])")
                    GL.DrawElements(call.DrawFunc.Params[0], call.DrawFunc.Params[1], call.DrawFunc.Params[2], call.DrawFunc.Params[3]);
                if (call.DrawFunc.Name == "DrawElements(BeginMode, int, DrawElementsType, [In] [Out] ref T)")
                    GL.DrawElements(call.DrawFunc.Params[0], call.DrawFunc.Params[1], call.DrawFunc.Params[2], call.DrawFunc.Params[3]);
                if (call.DrawFunc.Name == "DrawElements(PrimitiveType, int, DrawElementsType, int)")
                    GL.DrawElements(call.DrawFunc.Params[0], call.DrawFunc.Params[1], call.DrawFunc.Params[2], call.DrawFunc.Params[3]);
                if (call.DrawFunc.Name == "DrawElements(PrimitiveType, int, DrawElementsType, IntPtr)")
                    GL.DrawElements(call.DrawFunc.Params[0], call.DrawFunc.Params[1], call.DrawFunc.Params[2], call.DrawFunc.Params[3]);
                if (call.DrawFunc.Name == "DrawElements(PrimitiveType, int, DrawElementsType, [In] [Out] T[])")
                    GL.DrawElements(call.DrawFunc.Params[0], call.DrawFunc.Params[1], call.DrawFunc.Params[2], call.DrawFunc.Params[3]);
                if (call.DrawFunc.Name == "DrawElements(PrimitiveType, int, DrawElementsType, [In] [Out] T[,])")
                    GL.DrawElements(call.DrawFunc.Params[0], call.DrawFunc.Params[1], call.DrawFunc.Params[2], call.DrawFunc.Params[3]);
                if (call.DrawFunc.Name == "DrawElements(PrimitiveType, int, DrawElementsType, [In] [Out] T[,,])")
                    GL.DrawElements(call.DrawFunc.Params[0], call.DrawFunc.Params[1], call.DrawFunc.Params[2], call.DrawFunc.Params[3]);
                if (call.DrawFunc.Name == "DrawElements(PrimitiveType, int, DrawElementsType, [In] [Out] ref T)")
                    GL.DrawElements(call.DrawFunc.Params[0], call.DrawFunc.Params[1], call.DrawFunc.Params[2], call.DrawFunc.Params[3]);
            }
        }
    }
}
