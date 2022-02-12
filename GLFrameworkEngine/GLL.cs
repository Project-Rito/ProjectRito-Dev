using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class GLL : GL
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

        public new static void Disable(EnableCap cap)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.Disable(cap);
                };
            }
            else
                GL.Disable(cap);
        }
        public new static void Disable(IndexedEnableCap target, int index)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.Disable(target, index);
                };
            }
            else
                GL.Disable(target, index);
        }
        public new static void Disable(IndexedEnableCap target, uint index)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.Disable(target, index);
                };
            }
            else
                GL.Disable(target, index);
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

        public new static void PolygonOffset(float factor, float units)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.PolygonOffset(factor, units);
                };
            }
            else
                GL.PolygonOffset(factor, units);
        }

        public new static void BindBuffer(BufferTarget target, int buffer)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.BindBuffer(target, buffer);
                };
            }
            else
                GL.BindBuffer(target, buffer);
        }
        public new static void BindBuffer(BufferTarget target, uint buffer)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.BindBuffer(target, buffer);
                };
            }
            else
                GL.BindBuffer(target, buffer);
        }

        public new static void DeleteBuffer(int buffers)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.DeleteBuffer(buffers);
                };
            }
            else
                GL.DeleteBuffer(buffers);
        }
        public new static void DeleteBuffer(uint buffers)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.DeleteBuffer(buffers);
                };
            }
            else
                GL.DeleteBuffer(buffers);
        }

        public new static void DeleteVertexArray(int arrays)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.DeleteVertexArray(arrays);
                };
            }
            else
                GL.DeleteVertexArray(arrays);
        }
        public new static void DeleteVertexArray(uint arrays)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.DeleteVertexArray(arrays);
                };
            }
            else
                GL.DeleteVertexArray(arrays);
        }

        public new static void EnableVertexAttribArray(int index)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.EnableVertexAttribArray(index);
                };
            }
            else
                GL.EnableVertexAttribArray(index);
        }
        public new static void EnableVertexAttribArray(uint index)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.EnableVertexAttribArray(index);
                };
            }
            else
                GL.EnableVertexAttribArray(index);
        }

        public new static void DisableVertexAttribArray(int index)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.DisableVertexAttribArray(index);
                };
            }
            else
                GL.DisableVertexAttribArray(index);
        }
        public new static void DisableVertexAttribArray(uint index)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.DisableVertexAttribArray(index);
                };
            }
            else
                GL.DisableVertexAttribArray(index);
        }

        public new static void VertexAttribIPointer(int index, int size, VertexAttribIntegerType type, int stride, IntPtr pointer)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.VertexAttribIPointer(index, size, type, stride, pointer);
                };
            }
            else
                GL.VertexAttribIPointer(index, size, type, stride, pointer);
        }
        public new static void VertexAttribIPointer<T>(int index, int size, VertexAttribIntegerType type, int stride, [In] [Out] T[] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.VertexAttribIPointer(index, size, type, stride, pointer);
                };
            }
            else
                GL.VertexAttribIPointer(index, size, type, stride, pointer);
        }
        public new static void VertexAttribIPointer<T>(int index, int size, VertexAttribIntegerType type, int stride, [In][Out] T[,] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.VertexAttribIPointer(index, size, type, stride, pointer);
                };
            }
            else
                GL.VertexAttribIPointer(index, size, type, stride, pointer);
        }
        public new static void VertexAttribIPointer<T>(int index, int size, VertexAttribIntegerType type, int stride, [In][Out] ref T pointer) where T : struct
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.VertexAttribIPointer(index, size, type, stride, ref pointer);
        }
        public new static void VertexAttribIPointer(uint index, int size, VertexAttribIntegerType type, int stride, IntPtr pointer)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.VertexAttribIPointer(index, size, type, stride, pointer);
                };
            }
            else
                GL.VertexAttribIPointer(index, size, type, stride, pointer);
        }
        public new static void VertexAttribIPointer<T>(uint index, int size, VertexAttribIntegerType type, int stride, [In][Out] T[] pointer) where T: struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.VertexAttribIPointer(index, size, type, stride, pointer);
                };
            }
            else
                GL.VertexAttribIPointer(index, size, type, stride, pointer);
        }
        public new static void VertexAttribIPointer<T>(uint index, int size, VertexAttribIntegerType type, int stride, [In][Out] T[,] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.VertexAttribIPointer(index, size, type, stride, pointer);
                };
            }
            else
                GL.VertexAttribIPointer(index, size, type, stride, pointer);
        }
        public new static void VertexAttribIPointer<T>(uint index, int size, VertexAttribIntegerType type, int stride, [In][Out] T[,,] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.VertexAttribIPointer(index, size, type, stride, pointer);
                };
            }
            else
                GL.VertexAttribIPointer(index, size, type, stride, pointer);
        }
        public new static void VertexAttribIPointer<T>(uint index, int size, VertexAttribIntegerType type, int stride, [In][Out] ref T pointer) where T : struct
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.VertexAttribIPointer(index, size, type, stride, ref pointer);
        }

        public new static void VertexAttribPointer(int index, int size, VertexAttribPointerType type, bool normalized, int stride, int offset)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.VertexAttribPointer(index, size, type, normalized, stride, offset);
                };
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, offset);
        }
        public new static void VertexAttribPointer(int index, int size, VertexAttribPointerType type, bool normalized, int stride, IntPtr pointer)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
                };
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
        }
        public new static void VertexAttribPointer<T>(int index, int size, VertexAttribPointerType type, bool normalized, int stride, [In] [Out] T[] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
                };
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
        }
        public new static void VertexAttribPointer<T>(int index, int size, VertexAttribPointerType type, bool normalized, int stride, [In][Out] T[,] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
                };
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
        }
        public new static void VertexAttribPointer<T>(int index, int size, VertexAttribPointerType type, bool normalized, int stride, [In][Out] T[,,] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
                };
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
        }
        public new static void VertexAttribPointer<T>(int index, int size, VertexAttribPointerType type, bool normalized, int stride, [In][Out] ref T pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
                };
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, ref pointer);
        }
        public new static void VertexAttribPointer(uint index, int size, VertexAttribPointerType type, bool normalized, int stride, IntPtr pointer)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
                };
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
        }
        public new static void VertexAttribPointer<T>(uint index, int size, VertexAttribPointerType type, bool normalized, int stride, [In][Out] T[] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
                };
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
        }
        public new static void VertexAttribPointer<T>(uint index, int size, VertexAttribPointerType type, bool normalized, int stride, [In][Out] T[,] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
                };
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
        }
        public new static void VertexAttribPointer<T>(uint index, int size, VertexAttribPointerType type, bool normalized, int stride, [In][Out] T[,,] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
                };
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
        }
        public new static void VertexAttribPointer<T>(uint index, int size, VertexAttribPointerType type, bool normalized, int stride, [In][Out] ref T pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
                };
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, ref pointer);
        }

        public new static void VertexAttribDivisor(int index, int divisor)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.VertexAttribDivisor(index, divisor);
                };
            }
            else
                GL.VertexAttribDivisor(index, divisor);
        }
        public new static void VertexAttribDivisor(uint index, uint divisor)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.VertexAttribDivisor(index, divisor);
                };
            }
            else
                GL.VertexAttribDivisor(index, divisor);
        }

        public new static void GenVertexArrays(int n, [Out] int[] arrays)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.GenVertexArrays(n, arrays);
                };
            }
            else
                GL.GenVertexArrays(n, arrays);
        }
        public new static void GenVertexArrays(int n, out int arrays)
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.GenVertexArrays(n, out arrays);
        }
        public new static void GenVertexArrays(int n, [Out] uint[] arrays)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup += delegate
                {
                    GL.GenVertexArrays(n, arrays);
                };
            }
            else
                GL.GenVertexArrays(n, arrays);
        }
        public new static void GenVertexArrays(int n, out uint arrays)
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.GenVertexArrays(n, out arrays);
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
