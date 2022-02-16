using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;
using System.Linq;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace GLFrameworkEngine
{
    public class GLL : GL
    {
        // Variables to enable/disable layer features
        public static bool EnableInstancing = false;

        // Variables to keep track of calls to see which ones can be instanced/batched together
        private static List<InstancedDrawCall> InstancedDrawCalls = new List<InstancedDrawCall>(5000);

        private class InstancedDrawCall
        {
            public List<Matrix4> Transforms = new List<Matrix4>(32);
            public int Program;
            public List<Action> Setup = new List<Action>(500);
            public DrawFunc DrawFunc;
        }

        private class DrawCall
        {
            public Matrix4 Transform;
            public int Program;
            public List<Action> Setup = new List<Action>(500);
            public DrawFunc DrawFunc;
        }

        private class DrawFunc
        {
            public string Name;
            public List<dynamic> Params;
        }

        private static DrawCall ThisCall = new DrawCall();

        // Setup Functions
        public new static void BindVertexArray(int array)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.BindVertexArray(array);
                });
            }
            else
                GL.BindVertexArray(array);
        }

        public new static void Enable(EnableCap cap)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Enable(cap);
                });
            }
            else
                GL.Enable(cap);
        }
        public new static void Enable(IndexedEnableCap target, int index)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Enable(target, index);
                });
            }
            else
                GL.Enable(target, index);
        }
        public new static void Enable(IndexedEnableCap target, uint index)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Enable(target, index);
                });
            }
            else
                GL.Enable(target, index);
        }

        public new static void ActiveTexture(TextureUnit texture)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.ActiveTexture(texture);
                });
            }
            else
                GL.ActiveTexture(texture);
        }

        public new static void BindTexture(TextureTarget target, int texture)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.BindTexture(target, texture);
                });
            }
            else
                GL.BindTexture(target, texture);
        }
        public new static void BindTexture(TextureTarget target, uint texture)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.BindTexture(target, texture);
                });
            }
            else
                GL.BindTexture(target, texture);
        }

        public new static void UniformBlockBinding(int program, int uniformBlockIndex, int uniformBlockBinding)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformBlockBinding(program, uniformBlockIndex, uniformBlockBinding);
                });
            }
            else
                GL.UniformBlockBinding(program, uniformBlockIndex, uniformBlockBinding);
        }
        public new static void UniformBlockBinding(uint program, uint uniformBlockIndex, uint uniformBlockBinding)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformBlockBinding(program, uniformBlockIndex, uniformBlockBinding);
                });
            }
            else
                GL.UniformBlockBinding(program, uniformBlockIndex, uniformBlockBinding);
        }

        public new static void BindBufferBase(BufferRangeTarget target, int index, int buffer)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.BindBufferBase(target, index, buffer);
                });
            }
            else
                GL.BindBufferBase(target, index, buffer);
        }
        public new static void BindBufferBase(BufferRangeTarget target, uint index, uint buffer)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.BindBufferBase(target, index, buffer);
                });
            }
            else
                GL.BindBufferBase(target, index, buffer);
        }
        public new static void BindBufferBase(BufferTarget target, int index, int buffer)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.BindBufferBase(target, index, buffer);
                });
            }
            else
                GL.BindBufferBase(target, index, buffer);
        }
        public new static void BindBufferBase(BufferTarget target, uint index, uint buffer)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.BindBufferBase(target, index, buffer);
                });
            }
            else
                GL.BindBufferBase(target, index, buffer);
        }

        public new static void BufferData(BufferTarget target, int size, IntPtr data, BufferUsageHint usage)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.BufferData(target, size, data, usage);
                });
            }
            else
                GL.BufferData(target, size, data, usage);
        }
        public new static void BufferData<T>(BufferTarget target, int size, [In] [Out] T[] data, BufferUsageHint usage) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.BufferData(target, size, data, usage);
                });
            }
            else
                GL.BufferData(target, size, data, usage);
        }
        public new static void BufferData<T>(BufferTarget target, int size, [In][Out] T[,] data, BufferUsageHint usage) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.BufferData(target, size, data, usage);
                });
            }
            else
                GL.BufferData(target, size, data, usage);
        }
        public new static void BufferData<T>(BufferTarget target, int size, [In][Out] T[,,] data, BufferUsageHint usage) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.BufferData(target, size, data, usage);
                });
            }
            else
                GL.BufferData(target, size, data, usage);
        }
        public new static void BufferData<T>(BufferTarget target, int size, [In][Out] ref T data, BufferUsageHint usage) where T : struct
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.BufferData(target, size, ref data, usage);
        }
        public new static void BufferData(BufferTarget target, IntPtr size, IntPtr data, BufferUsageHint usage)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.BufferData(target, size, data, usage);
                });
            }
            else
                GL.BufferData(target, size, data, usage);
        }
        public new static void BufferData<T>(BufferTarget target, IntPtr size, [In][Out] T[] data, BufferUsageHint usage) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.BufferData(target, size, data, usage);
                });
            }
            else
                GL.BufferData(target, size, data, usage);
        }
        public new static void BufferData<T>(BufferTarget target, IntPtr size, [In][Out] T[,] data, BufferUsageHint usage) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.BufferData(target, size, data, usage);
                });
            }
            else
                GL.BufferData(target, size, data, usage);
        }
        public new static void BufferData<T>(BufferTarget target, IntPtr size, [In][Out] T[,,] data, BufferUsageHint usage) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.BufferData(target, size, data, usage);
                });
            }
            else
                GL.BufferData(target, size, data, usage);
        }
        public new static void BufferData<T>(BufferTarget target, IntPtr size, [In][Out] ref T data, BufferUsageHint usage) where T : struct
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.BufferData(target, size, ref data, usage);
        }

        public new static void Disable(EnableCap cap)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Disable(cap);
                });
            }
            else
                GL.Disable(cap);
        }
        public new static void Disable(IndexedEnableCap target, int index)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Disable(target, index);
                });
            }
            else
                GL.Disable(target, index);
        }
        public new static void Disable(IndexedEnableCap target, uint index)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Disable(target, index);
                });
            }
            else
                GL.Disable(target, index);
        }

        public new static void UseProgram(int program)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.UseProgram(program);
                });
                ThisCall.Program = program;
            }
            else
                GL.UseProgram(program);
        }
        public new static void UseProgram(uint program)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.UseProgram(program);
                });
                ThisCall.Program = (int)program;
            }
            else
                GL.UseProgram(program);
        }

        public new static void DepthFunc(DepthFunction func)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.DepthFunc(func);
                });
            }
            else
                GL.DepthFunc(func);
        }

        public new static void AlphaFunc(AlphaFunction func, float @ref)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.AlphaFunc(func, @ref);
                });
            }
            else
                GL.AlphaFunc(func, @ref);
        }

        public new static void DepthRange(double near, double far)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.DepthRange(near, far);
                });
            }
            else
                GL.DepthRange(near, far);
        }
        public new static void DepthRange(float near, float far)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.DepthRange(near, far);
                });
            }
            else
                GL.DepthRange(near, far);
        }

        public new static void DepthMask(bool flag)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.DepthMask(flag);
                });
            }
            else
                GL.DepthMask(flag);
        }

        public new static void PolygonMode(MaterialFace materialFace, PolygonMode polygonMode)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.PolygonMode(materialFace, polygonMode);
                });
            }
            else
                GL.PolygonMode(materialFace, polygonMode);
        }

        public new static void PolygonOffset(float factor, float units)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.PolygonOffset(factor, units);
                });
            }
            else
                GL.PolygonOffset(factor, units);
        }

        public new static void CullFace(CullFaceMode mode)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.CullFace(mode);
                });
            }
            else
                GL.CullFace(mode);
        }

        public new static void BindBuffer(BufferTarget target, int buffer)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.BindBuffer(target, buffer);
                });
            }
            else
                GL.BindBuffer(target, buffer);
        }
        public new static void BindBuffer(BufferTarget target, uint buffer)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.BindBuffer(target, buffer);
                });
            }
            else
                GL.BindBuffer(target, buffer);
        }

        public new static void DeleteBuffer(int buffers)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.DeleteBuffer(buffers);
                });
            }
            else
                GL.DeleteBuffer(buffers);
        }
        public new static void DeleteBuffer(uint buffers)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.DeleteBuffer(buffers);
                });
            }
            else
                GL.DeleteBuffer(buffers);
        }

        public new static void DeleteVertexArray(int arrays)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.DeleteVertexArray(arrays);
                });
            }
            else
                GL.DeleteVertexArray(arrays);
        }
        public new static void DeleteVertexArray(uint arrays)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.DeleteVertexArray(arrays);
                });
            }
            else
                GL.DeleteVertexArray(arrays);
        }

        public new static void EnableVertexAttribArray(int index)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.EnableVertexAttribArray(index);
                });
            }
            else
                GL.EnableVertexAttribArray(index);
        }
        public new static void EnableVertexAttribArray(uint index)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.EnableVertexAttribArray(index);
                });
            }
            else
                GL.EnableVertexAttribArray(index);
        }

        public new static void DisableVertexAttribArray(int index)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.DisableVertexAttribArray(index);
                });
            }
            else
                GL.DisableVertexAttribArray(index);
        }
        public new static void DisableVertexAttribArray(uint index)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.DisableVertexAttribArray(index);
                });
            }
            else
                GL.DisableVertexAttribArray(index);
        }

        public new static void VertexAttribIPointer(int index, int size, VertexAttribIntegerType type, int stride, IntPtr pointer)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.VertexAttribIPointer(index, size, type, stride, pointer);
                });
            }
            else
                GL.VertexAttribIPointer(index, size, type, stride, pointer);
        }
        public new static void VertexAttribIPointer<T>(int index, int size, VertexAttribIntegerType type, int stride, [In] [Out] T[] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.VertexAttribIPointer(index, size, type, stride, pointer);
                });
            }
            else
                GL.VertexAttribIPointer(index, size, type, stride, pointer);
        }
        public new static void VertexAttribIPointer<T>(int index, int size, VertexAttribIntegerType type, int stride, [In][Out] T[,] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.VertexAttribIPointer(index, size, type, stride, pointer);
                });
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
                ThisCall.Setup.Add(delegate
                {
                    GL.VertexAttribIPointer(index, size, type, stride, pointer);
                });
            }
            else
                GL.VertexAttribIPointer(index, size, type, stride, pointer);
        }
        public new static void VertexAttribIPointer<T>(uint index, int size, VertexAttribIntegerType type, int stride, [In][Out] T[] pointer) where T: struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.VertexAttribIPointer(index, size, type, stride, pointer);
                });
            }
            else
                GL.VertexAttribIPointer(index, size, type, stride, pointer);
        }
        public new static void VertexAttribIPointer<T>(uint index, int size, VertexAttribIntegerType type, int stride, [In][Out] T[,] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.VertexAttribIPointer(index, size, type, stride, pointer);
                });
            }
            else
                GL.VertexAttribIPointer(index, size, type, stride, pointer);
        }
        public new static void VertexAttribIPointer<T>(uint index, int size, VertexAttribIntegerType type, int stride, [In][Out] T[,,] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.VertexAttribIPointer(index, size, type, stride, pointer);
                });
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
                ThisCall.Setup.Add(delegate
                {
                    GL.VertexAttribPointer(index, size, type, normalized, stride, offset);
                });
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, offset);
        }
        public new static void VertexAttribPointer(int index, int size, VertexAttribPointerType type, bool normalized, int stride, IntPtr pointer)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
                });
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
        }
        public new static void VertexAttribPointer<T>(int index, int size, VertexAttribPointerType type, bool normalized, int stride, [In] [Out] T[] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
                });
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
        }
        public new static void VertexAttribPointer<T>(int index, int size, VertexAttribPointerType type, bool normalized, int stride, [In][Out] T[,] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
                });
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
        }
        public new static void VertexAttribPointer<T>(int index, int size, VertexAttribPointerType type, bool normalized, int stride, [In][Out] T[,,] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
                });
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
        }
        public new static void VertexAttribPointer<T>(int index, int size, VertexAttribPointerType type, bool normalized, int stride, [In][Out] ref T pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
                });
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, ref pointer);
        }
        public new static void VertexAttribPointer(uint index, int size, VertexAttribPointerType type, bool normalized, int stride, IntPtr pointer)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
                });
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
        }
        public new static void VertexAttribPointer<T>(uint index, int size, VertexAttribPointerType type, bool normalized, int stride, [In][Out] T[] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
                });
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
        }
        public new static void VertexAttribPointer<T>(uint index, int size, VertexAttribPointerType type, bool normalized, int stride, [In][Out] T[,] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
                });
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
        }
        public new static void VertexAttribPointer<T>(uint index, int size, VertexAttribPointerType type, bool normalized, int stride, [In][Out] T[,,] pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
                });
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, pointer);
        }
        public new static void VertexAttribPointer<T>(uint index, int size, VertexAttribPointerType type, bool normalized, int stride, [In][Out] ref T pointer) where T : struct
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
                });
            }
            else
                GL.VertexAttribPointer(index, size, type, normalized, stride, ref pointer);
        }

        public new static void VertexAttribDivisor(int index, int divisor)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.VertexAttribDivisor(index, divisor);
                });
            }
            else
                GL.VertexAttribDivisor(index, divisor);
        }
        public new static void VertexAttribDivisor(uint index, uint divisor)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.VertexAttribDivisor(index, divisor);
                });
            }
            else
                GL.VertexAttribDivisor(index, divisor);
        }

        public new static void GenVertexArrays(int n, [Out] int[] arrays)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.GenVertexArrays(n, arrays);
                });
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
                ThisCall.Setup.Add(delegate
                {
                    GL.GenVertexArrays(n, arrays);
                });
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

        public new static void Uniform1(int program, double x)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform1(program, x);
                });
            }
            else
                GL.Uniform1(program, x);
        }
        public new static void Uniform1(int location, int count, double[] value)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform1(location, count, value);
                });
            }
            else
                GL.Uniform1(location, count, value);
        }
        public new static void Uniform1(int location, int count, ref double value)
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.Uniform1(location, count, ref value);
        }
        public new static void Uniform1(int location, float v0)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform1(location, v0);
                });
            }
            else
                GL.Uniform1(location, v0);
        }
        public new static void Uniform1(int location, int count, float[] value)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform1(location, count, value);
                });
            }
            else
                GL.Uniform1(location, count, value);
        }
        public new static void Uniform1(int location, int count, ref float value)
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.Uniform1(location, count, ref value);
        }
        public new static void Uniform1(int location, int v0)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform1(location, v0);
                });
            }
            else
                GL.Uniform1(location, v0);
        }
        public new static void Uniform1(int location, int count, int[] value)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform1(location, count, value);
                });
            }
            else
                GL.Uniform1(location, count, value);
        }
        public new static void Uniform1(int location, int count, ref int value)
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.Uniform1(location, count, ref value);
        }
        public new static void Uniform1(int location, uint v0)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform1(location, v0);
                });
            }
            else
                GL.Uniform1(location, v0);
        }
        public new static void Uniform1(int location, int count, uint[] value)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform1(location, count, value);
                });
            }
            else
                GL.Uniform1(location, count, value);
        }
        public new static void Uniform1(int location, int count, ref uint value)
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.Uniform1(location, count, ref value);
        }

        public new static void Uniform2(int program, double x, double y)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform2(program, x, y);
                });
            }
            else
                GL.Uniform2(program, x, y);
        }
        public new static void Uniform2(int location, int count, double[] value)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform2(location, count, value);
                });
            }
            else
                GL.Uniform2(location, count, value);
        }
        public new static void Uniform2(int location, int count, ref double value)
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.Uniform2(location, count, ref value);
        }
        public new static void Uniform2(int location, float v0, float v1)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform2(location, v0, v1);
                });
            }
            else
                GL.Uniform2(location, v0, v1);
        }
        public new static void Uniform2(int location, int count, float[] value)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform2(location, count, value);
                });
            }
            else
                GL.Uniform2(location, count, value);
        }
        public new static void Uniform2(int location, int count, ref float value)
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.Uniform2(location, count, ref value);
        }
        public new static void Uniform2(int location, int v0, int v1)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform2(location, v0, v1);
                });
            }
            else
                GL.Uniform2(location, v0, v1);
        }
        public new static void Uniform2(int location, int count, int[] value)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform2(location, count, value);
                });
            }
            else
                GL.Uniform2(location, count, value);
        }
        public new static void Uniform2(int location, uint v0, uint v1)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform2(location, v0, v1);
                });
            }
            else
                GL.Uniform2(location, v0, v1);
        }
        public new static void Uniform2(int location, int count, uint[] value)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform2(location, count, value);
                });
            }
            else
                GL.Uniform2(location, count, value);
        }
        public new static void Uniform2(int location, int count, ref uint value)
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.Uniform2(location, count, ref value);
        }

        public new static void Uniform3(int program, double x, double y, double z)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform3(program, x, y, z);
                });
            }
            else
                GL.Uniform3(program, x, y, z);
        }
        public new static void Uniform3(int location, int count, double[] value)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform3(location, count, value);
                });
            }
            else
                GL.Uniform3(location, count, value);
        }
        public new static void Uniform3(int location, int count, ref double value)
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.Uniform3(location, count, ref value);
        }
        public new static void Uniform3(int location, float v0, float v1, float v2)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform3(location, v0, v1, v2);
                });
            }
            else
                GL.Uniform3(location, v0, v1, v2);
        }
        public new static void Uniform3(int location, int count, float[] value)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform3(location, count, value);
                });
            }
            else
                GL.Uniform3(location, count, value);
        }
        public new static void Uniform3(int location, int count, ref float value)
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.Uniform3(location, count, ref value);
        }
        public new static void Uniform3(int location, int v0, int v1, int v2)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform3(location, v0, v1, v2);
                });
            }
            else
                GL.Uniform3(location, v0, v1, v2);
        }
        public new static void Uniform3(int location, int count, int[] value)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform3(location, count, value);
                });
            }
            else
                GL.Uniform3(location, count, value);
        }
        public new static void Uniform3(int location, uint v0, uint v1, uint v2)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform3(location, v0, v1, v2);
                });
            }
            else
                GL.Uniform3(location, v0, v1, v2);
        }
        public new static void Uniform3(int location, int count, uint[] value)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform3(location, count, value);
                });
            }
            else
                GL.Uniform3(location, count, value);
        }
        public new static void Uniform3(int location, int count, ref uint value)
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.Uniform3(location, count, ref value);
        }

        public new static void Uniform4(int program, double x, double y, double z, double w)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform4(program, x, y, z, w);
                });
            }
            else
                GL.Uniform4(program, x, y, z, w);
        }
        public new static void Uniform4(int location, int count, double[] value)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform4(location, count, value);
                });
            }
            else
                GL.Uniform4(location, count, value);
        }
        public new static void Uniform4(int location, int count, ref double value)
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.Uniform4(location, count, ref value);
        }
        public new static void Uniform4(int location, float v0, float v1, float v2, float v3)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform4(location, v0, v1, v2, v3);
                });
            }
            else
                GL.Uniform4(location, v0, v1, v2, v3);
        }
        public new static void Uniform4(int location, int count, float[] value)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform4(location, count, value);
                });
            }
            else
                GL.Uniform4(location, count, value);
        }
        public new static void Uniform4(int location, int count, ref float value)
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.Uniform4(location, count, ref value);
        }
        public new static void Uniform4(int location, int v0, int v1, int v2, int v3)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform4(location, v0, v1, v2, v3);
                });
            }
            else
                GL.Uniform4(location, v0, v1, v2, v3);
        }
        public new static void Uniform4(int location, int count, int[] value)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform4(location, count, value);
                });
            }
            else
                GL.Uniform4(location, count, value);
        }
        public new static void Uniform4(int location, uint v0, uint v1, uint v2, uint v3)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform4(location, v0, v1, v2, v3);
                });
            }
            else
                GL.Uniform4(location, v0, v1, v2, v3);
        }
        public new static void Uniform4(int location, int count, uint[] value)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.Uniform4(location, count, value);
                });
            }
            else
                GL.Uniform4(location, count, value);
        }
        public new static void Uniform4(int location, int count, ref uint value)
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.Uniform4(location, count, ref value);
        }

        public new static void UniformMatrix2(int location, bool transpose, ref Matrix2 matrix)
        {
            if (EnableInstancing)
            {
                var matrixCopy = matrix; // We have to copy this matrix over for this to work with delegates... I don't see why opengl would need to write to this, anyway.
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformMatrix2(location, transpose, ref matrixCopy);
                });
            }
            else
                GL.UniformMatrix2(location, transpose, ref matrix);
        }
        public new static void UniformMatrix2(int location, bool transpose, ref Matrix2d matrix)
        {
            if (EnableInstancing)
            {
                var matrixCopy = matrix; // We have to copy this matrix over for this to work with delegates... I don't see why opengl would need to write to this, anyway.
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformMatrix2(location, transpose, ref matrixCopy);
                });
            }
            else
                GL.UniformMatrix2(location, transpose, ref matrix);
        }
        public new static void UniformMatrix2x3(int location, bool transpose, ref Matrix2x3 matrix)
        {
            if (EnableInstancing)
            {
                var matrixCopy = matrix; // We have to copy this matrix over for this to work with delegates... I don't see why opengl would need to write to this, anyway.
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformMatrix2x3(location, transpose, ref matrixCopy);
                });
            }
            else
                GL.UniformMatrix2x3(location, transpose, ref matrix);
        }
        public new static void UniformMatrix2x3(int location, bool transpose, ref Matrix2x3d matrix)
        {
            if (EnableInstancing)
            {
                var matrixCopy = matrix; // We have to copy this matrix over for this to work with delegates... I don't see why opengl would need to write to this, anyway.
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformMatrix2x3(location, transpose, ref matrixCopy);
                });
            }
            else
                GL.UniformMatrix2x3(location, transpose, ref matrix);
        }
        public new static void UniformMatrix2x4(int location, bool transpose, ref Matrix2x4 matrix)
        {
            if (EnableInstancing)
            {
                var matrixCopy = matrix; // We have to copy this matrix over for this to work with delegates... I don't see why opengl would need to write to this, anyway.
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformMatrix2x4(location, transpose, ref matrixCopy);
                });
            }
            else
                GL.UniformMatrix2x4(location, transpose, ref matrix);
        }
        public new static void UniformMatrix2x4(int location, bool transpose, ref Matrix2x4d matrix)
        {
            if (EnableInstancing)
            {
                var matrixCopy = matrix; // We have to copy this matrix over for this to work with delegates... I don't see why opengl would need to write to this, anyway.
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformMatrix2x4(location, transpose, ref matrixCopy);
                });
            }
            else
                GL.UniformMatrix2x4(location, transpose, ref matrix);
        }
        public new static void UniformMatrix3x2(int location, bool transpose, ref Matrix3x2 matrix)
        {
            if (EnableInstancing)
            {
                var matrixCopy = matrix; // We have to copy this matrix over for this to work with delegates... I don't see why opengl would need to write to this, anyway.
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformMatrix3x2(location, transpose, ref matrixCopy);
                });
            }
            else
                GL.UniformMatrix3x2(location, transpose, ref matrix);
        }
        public new static void UniformMatrix3x2(int location, bool transpose, ref Matrix3x2d matrix)
        {
            if (EnableInstancing)
            {
                var matrixCopy = matrix; // We have to copy this matrix over for this to work with delegates... I don't see why opengl would need to write to this, anyway.
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformMatrix3x2(location, transpose, ref matrixCopy);
                });
            }
            else
                GL.UniformMatrix3x2(location, transpose, ref matrix);
        }
        public new static void UniformMatrix3(int location, bool transpose, ref Matrix3 matrix)
        {
            if (EnableInstancing)
            {
                var matrixCopy = matrix; // We have to copy this matrix over for this to work with delegates... I don't see why opengl would need to write to this, anyway.
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformMatrix3(location, transpose, ref matrixCopy);
                });
            }
            else
                GL.UniformMatrix3(location, transpose, ref matrix);
        }
        public new static void UniformMatrix3(int location, bool transpose, ref Matrix3d matrix)
        {
            if (EnableInstancing)
            {
                var matrixCopy = matrix; // We have to copy this matrix over for this to work with delegates... I don't see why opengl would need to write to this, anyway.
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformMatrix3(location, transpose, ref matrixCopy);
                });
            }
            else
                GL.UniformMatrix3(location, transpose, ref matrix);
        }
        public new static void UniformMatrix3x4(int location, bool transpose, ref Matrix3x4 matrix)
        {
            if (EnableInstancing)
            {
                var matrixCopy = matrix; // We have to copy this matrix over for this to work with delegates... I don't see why opengl would need to write to this, anyway.
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformMatrix3x4(location, transpose, ref matrixCopy);
                });
            }
            else
                GL.UniformMatrix3x4(location, transpose, ref matrix);
        }
        public new static void UniformMatrix3x4(int location, bool transpose, ref Matrix3x4d matrix)
        {
            if (EnableInstancing)
            {
                var matrixCopy = matrix; // We have to copy this matrix over for this to work with delegates... I don't see why opengl would need to write to this, anyway.
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformMatrix3x4(location, transpose, ref matrixCopy);
                });
            }
            else
                GL.UniformMatrix3x4(location, transpose, ref matrix);
        }
        public new static void UniformMatrix4x2(int location, bool transpose, ref Matrix4x2 matrix)
        {
            if (EnableInstancing)
            {
                var matrixCopy = matrix; // We have to copy this matrix over for this to work with delegates... I don't see why opengl would need to write to this, anyway.
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformMatrix4x2(location, transpose, ref matrixCopy);
                });
            }
            else
                GL.UniformMatrix4x2(location, transpose, ref matrix);
        }
        public new static void UniformMatrix4x2(int location, bool transpose, ref Matrix4x2d matrix)
        {
            if (EnableInstancing)
            {
                var matrixCopy = matrix; // We have to copy this matrix over for this to work with delegates... I don't see why opengl would need to write to this, anyway.
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformMatrix4x2(location, transpose, ref matrixCopy);
                });
            }
            else
                GL.UniformMatrix4x2(location, transpose, ref matrix);
        }
        public new static void UniformMatrix4x3(int location, bool transpose, ref Matrix4x3 matrix)
        {
            if (EnableInstancing)
            {
                var matrixCopy = matrix; // We have to copy this matrix over for this to work with delegates... I don't see why opengl would need to write to this, anyway.
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformMatrix4x3(location, transpose, ref matrixCopy);
                });
            }
            else
                GL.UniformMatrix4x3(location, transpose, ref matrix);
        }
        public new static void UniformMatrix4x3(int location, bool transpose, ref Matrix4x3d matrix)
        {
            if (EnableInstancing)
            {
                var matrixCopy = matrix; // We have to copy this matrix over for this to work with delegates... I don't see why opengl would need to write to this, anyway.
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformMatrix4x3(location, transpose, ref matrixCopy);
                });
            }
            else
                GL.UniformMatrix4x3(location, transpose, ref matrix);
        }
        public new static void UniformMatrix4(int location, bool transpose, ref Matrix4 matrix)
        {
            if (EnableInstancing)
            {
                if (GetUniformName(ThisCall.Program, location) == "mtxMdl")
                {
                    ThisCall.Transform = matrix;
                    return;
                }

                var matrixCopy = matrix; // We have to copy this matrix over for this to work with delegates... I don't see why opengl would need to write to this, anyway.
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformMatrix4(location, transpose, ref matrixCopy);
                });
            }
            else
                GL.UniformMatrix4(location, transpose, ref matrix);
        }
        public new static void UniformMatrix4(int location, bool transpose, ref Matrix4d matrix)
        {
            GL.UniformMatrix4(location, transpose, ref matrix);
            return;
            if (EnableInstancing)
            {
                if (GetUniformName(ThisCall.Program, location) == "mtxMdl")
                {
                    // We do lose some precision, but right now our instance positions are set up around floats anyway
                    ThisCall.Transform = new Matrix4((Vector4)matrix.Column0, (Vector4)matrix.Column1, (Vector4)matrix.Column2, (Vector4)matrix.Column3);
                    return;
                }

                var matrixCopy = matrix; // We have to copy this matrix over for this to work with delegates... I don't see why opengl would need to write to this, anyway.
                ThisCall.Setup.Add(delegate
                {
                    GL.UniformMatrix4(location, transpose, ref matrixCopy);
                });
            }
            else
                GL.UniformMatrix4(location, transpose, ref matrix);
        }

        public new static void TexParameter(TextureTarget target, TextureParameterName pname, float param)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.TexParameter(target, pname, param);
                });
            }
            else
                GL.TexParameter(target, pname, param);
        }
        public new static void TexParameter(TextureTarget target, TextureParameterName pname, float[] @params)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.TexParameter(target, pname, @params);
                });
            }
            else
                GL.TexParameter(target, pname, @params);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new static void TexParameter(TextureTarget target, TextureParameterName pname, int param)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.TexParameter(target, pname, param);
                });
            }
            else
                GL.TexParameter(target, pname, param);
        }
        public new static void TexParameterI(TextureTarget target, TextureParameterName pname, int[] @params)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.TexParameterI(target, pname, @params);
                });
            }
            else
                GL.TexParameterI(target, pname, @params);
        }
        public new static void TexParameterI(TextureTarget target, TextureParameterName pname, ref int @params)
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.TexParameterI(target, pname, ref @params);
        }
        public new static void TexParameterI(TextureTarget target, TextureParameterName pname, uint[] @params)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.TexParameterI(target, pname, @params);
                });
            }
            else
                GL.TexParameterI(target, pname, @params);
        }
        public new static void TexParameterI(TextureTarget target, TextureParameterName pname, ref uint @params)
        {
            if (EnableInstancing)
            {
                throw new Exception("GLL: Something was passed by reference, but that's not supported by instancing!");
            }
            else
                GL.TexParameterI(target, pname, ref @params);
        }
        public new static void TexParameter(TextureTarget target, TextureParameterName pname, int[] @params)
        {
            if (EnableInstancing)
            {
                ThisCall.Setup.Add(delegate
                {
                    GL.TexParameter(target, pname, @params);
                });
            }
            else
                GL.TexParameter(target, pname, @params);
        }

        // Draw Functions
        public new static void DrawElements(BeginMode mode, int count, DrawElementsType type, int indices)
        {
            if (EnableInstancing)
            {
                ThisCall.DrawFunc = new DrawFunc()
                {
                    Name = "DrawElements(BeginMode, int, DrawElementsType int)",
                    Params = new List<dynamic> {
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
                    Params = new List<dynamic> {
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
                    Params = new List<dynamic> {
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
                    Params = new List<dynamic> {
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
                    Params = new List<dynamic> {
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
                    Params = new List<dynamic> {
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
                    Params = new List<dynamic> {
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
                    Params = new List<dynamic> {
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
                    Params = new List<dynamic> {
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
                    Params = new List<dynamic> {
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
                    Params = new List<dynamic> {
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
                    Params = new List<dynamic> {
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

        private static Dictionary<int, Dictionary<int, string>> UniformNameCache = new Dictionary<int, Dictionary<int, string>>();
        private static string GetUniformName(int program, int location)
        {
            if (UniformNameCache.ContainsKey(program))
                if (UniformNameCache[program].ContainsKey(location))
                    return UniformNameCache[program][location];

            if (!UniformNameCache.ContainsKey(program))
                UniformNameCache.Add(program, new Dictionary<int, string>());
            int activeAttributeCount;
            GL.GetProgram(program, GetProgramParameterName.ActiveUniforms, out activeAttributeCount);
            for (int i = 0; i < activeAttributeCount; i++)
            {
                string name = GL.GetActiveUniform(program, i, out int s, out ActiveUniformType t);
                int loc = GL.GetUniformLocation(program, name);

                UniformNameCache[program][location] = name;
                if (location == loc)
                    return name;
            }
            //Console.WriteLine("GLL: Could not find uniform name!");
            UniformNameCache[program][location] = null;
            return null;
        }

        private static void GroupCall()
        {
            bool foundCall = false;
            foreach (var call in InstancedDrawCalls)
            {
                if (call.Setup.SequenceEqual(ThisCall.Setup))
                {
                    call.Transforms.Add(ThisCall.Transform);
                    foundCall = true;
                    break;
                }
            }
            if (!foundCall)
            {
                InstancedDrawCalls.Add(new InstancedDrawCall()
                {
                    Transforms = new List<Matrix4> { ThisCall.Transform },
                    Program = ThisCall.Program,
                    Setup = ThisCall.Setup,
                    DrawFunc = ThisCall.DrawFunc
                });
            }
            
            ThisCall = new DrawCall(); // We don't actually need to clear this.. but this will be simpler for debugging purposes. (It can produce null errors)
        }

        /// <summary>
        /// Should be called when all drawing is done
        /// </summary>
        public static void EndFrame()
        {
            foreach (var call in InstancedDrawCalls)
            {
                foreach (var fn in call.Setup)
                    fn?.Invoke();

                var transformsFloatArr = MemoryMarshal.Cast<Matrix4, float>(call.Transforms.ToArray()).ToArray();

                GL.UseProgram(call.Program);
                GL.Uniform1(GL.GetUniformLocation(call.Program, "GLL_IEnabled"), 1);
                GL.UniformMatrix4(GL.GetUniformLocation(call.Program, "GLL_IMtxMdls"), transformsFloatArr.Length/16, false, transformsFloatArr);

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
                if (call.DrawFunc.Name == "DrawElements(PrimitiveType, int, DrawElementsType, int)") // This one
                    //GL.DrawElements(call.DrawFunc.Params[0], call.DrawFunc.Params[1], call.DrawFunc.Params[2], call.DrawFunc.Params[3]);
                    GL.DrawElementsInstanced(call.DrawFunc.Params[0], call.DrawFunc.Params[1], call.DrawFunc.Params[2], (IntPtr)call.DrawFunc.Params[3], 32);
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
            InstancedDrawCalls.Clear();
        }
    }
}
