using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Linq;

namespace GLFrameworkEngine
{
    /// <summary>
    /// Represents a shader program that stores GL shader data.
    /// </summary>
    public class ShaderProgram : IDisposable
    {
        /// <summary>
        /// The ID of the shader program.
        /// </summary>
        public int program;

        //The input attributes in the shader
        private Dictionary<string, int> attributes = new Dictionary<string, int>();
        //The uniforms in the shader
        private Dictionary<string, int> uniforms = new Dictionary<string, int>();
        private int activeAttributeCount;
        //The shader stages inn the program
        private HashSet<Shader> shaders = new HashSet<Shader>();
        //Feedback attribute varyings to store attribute data executed from shader code
        public string[] FeedbackVaryings = new string[0];
        //Check if the shader was linked
        public bool LinkSucessful = false;

        // This isn't in OpenTK's enums for some reason.
        // https://www.khronos.org/registry/OpenGL/api/GL/glcorearb.h
        private static readonly int GL_PROGRAM_BINARY_MAX_LENGTH = 0x8741;

        public ShaderProgram(Shader[] shaders)
        {
            program = GLL.CreateProgram();
            LoadSource(shaders);
        }

        public ShaderProgram()
        {
            program = GLL.CreateProgram();
        }

        public ShaderProgram(Shader vertexShader, Shader fragmentShader)
        {
            program = GLL.CreateProgram();
            LoadSource(vertexShader, fragmentShader);
        }

        public ShaderProgram(byte[] binaryData, BinaryFormat format)
        {
            program = GLL.CreateProgram();
            LoadBinary(binaryData, format);
        }


        /// <summary>
        /// Loads the shader from shader stages.
        /// </summary>
        public void LoadSource(Shader[] shaders)
        {
            foreach (Shader shader in shaders)
            {
                if (!this.shaders.Contains(shader))
                    this.shaders.Add(shader);
            }
            CompileShaders();
        }

        /// <summary>
        /// Loads the shader from vertex and fragment shader stages.
        /// </summary>
        public void LoadSource(Shader vertexShader, Shader fragmentShader)
        {
            if (!this.shaders.Contains(vertexShader))
                this.shaders.Add(vertexShader);
            if (!this.shaders.Contains(fragmentShader))
                this.shaders.Add(fragmentShader);

            CompileShaders();
        }

        /// <summary>
        /// Loads the shader from a dumped glsl binary.
        /// </summary>
        public void LoadBinary(byte[] binaryData, BinaryFormat format)
        {
            // Number of supported binary formats.
            int binaryFormatCount;
            GLL.GetInteger(GetPName.NumProgramBinaryFormats, out binaryFormatCount);

            // Get all supported formats.
            int[] binaryFormats = new int[binaryFormatCount];
            GLL.GetInteger(GetPName.ProgramBinaryFormats, binaryFormats);

            if (binaryFormats.Contains((int)format))
            {
                try
                {
                    GLL.ProgramBinary(program, format, binaryData, binaryData.Length);
                }
                catch (AccessViolationException)
                {
                    // The binary is corrupt or the wrong format. 
                    LinkSucessful = false;
                    return;
                }

                LoadAttributes(program);
                LoadUniorms(program);

                LinkSucessful = true;
            }
        }

        /// <summary>
        /// Links the shader program.
        /// </summary>
        public void Link()
        {
            GLL.LinkProgram(program);
        }

        /// <summary>
        /// Enables the shader program.
        /// </summary>
        public void Enable()
        {
            GLL.UseProgram(program);
        }

        /// <summary>
        /// Disables the shader program.
        /// </summary>
        public void Disable()
        {
            GLL.UseProgram(0);
        }

        /// <summary>
        /// Disposes the shader program.
        /// </summary>
        public void Dispose()
        {
            foreach (var shader in shaders)
                shader.Dispose();

            GLL.DeleteProgram(program);
        }

        /// <summary>
        /// Loads a 2D texture into the shader given a uniform name, texture ID, and slot number.
        /// </summary>
        public void SetTexture2D(string uniform, int id, int slot)
        {
            GLL.ActiveTexture(TextureUnit.Texture0 + slot);
            GLL.BindTexture(TextureTarget.Texture2D, id);
            this.SetInt(uniform, slot);
        }

        /// <summary>
        /// Loads a 2D texture into the shader given a texture instance, uniform name, and slot number.
        /// </summary>
        public void SetTexture(GLTexture tex, string uniform, int slot)
        {
            GLL.ActiveTexture(TextureUnit.Texture0 + slot);
            tex.Bind();
            this.SetInt(uniform, slot);
        }

        /// <summary>
        /// Loads a transform into the shader given a uniform name and gl transform.
        /// </summary>
        public void SetTransform(string name, GLTransform transform) {
            var matrix = transform.TransformMatrix;
            SetMatrix4x4(name, ref matrix);
        }

        public void SetVector4(string name, Vector4 value)
        {
            if (uniforms.ContainsKey(name))
                GLL.Uniform4(uniforms[name], value);
        }

        public void SetVector3(string name, Vector3 value)
        {
            if (uniforms.ContainsKey(name))
                GLL.Uniform3(uniforms[name], value);
        }

        public void SetVector2(string name, Vector2 value)
        {
            if (uniforms.ContainsKey(name))
                GLL.Uniform2(uniforms[name], value);
        }

        public void SetFloat(string name, float value)
        {
            if (uniforms.ContainsKey(name))
                GLL.Uniform1(uniforms[name], value);
        }

        public void SetInt(string name, int value)
        {
            if (uniforms.ContainsKey(name))
                GLL.Uniform1(uniforms[name], value);
        }

        public void SetBool(string name, bool value)
        {
            int intValue = value == true ? 1 : 0;

            if (uniforms.ContainsKey(name))
                GLL.Uniform1(uniforms[name], intValue);
        }

        public void SetBoolToInt(string name, bool value)
        {
            if (!uniforms.ContainsKey(name))
                return;

            if (value)
                GLL.Uniform1(uniforms[name], 1);
            else
                GLL.Uniform1(this[name], 0);
        }

        public void SetColor(string name, System.Drawing.Color color)
        {
            if (uniforms.ContainsKey(name))
                GLL.Uniform4(uniforms[name], color.R, color.G, color.B, color.A);
        }

        public void SetMatrix4x4(string name, ref Matrix4 value, bool transpose = false)
        {
            if (uniforms.ContainsKey(name))
                GLL.UniformMatrix4(uniforms[name], transpose, ref value);
        }

        public int this[string name]
        {
            get { return uniforms[name]; }
        }

        private void LoadUniorms(int program)
        {
            uniforms.Clear();

            GLL.GetProgram(program, GetProgramParameterName.ActiveUniforms, out activeAttributeCount);
            for (int i = 0; i < activeAttributeCount; i++)
            {
                string name = GLL.GetActiveUniform(program, i, out int size, out ActiveUniformType type);
                int location = GLL.GetUniformLocation(program, name);

                // Overwrite existing vertex attributes.
                uniforms[name] = location;
            }
        }

        private void LoadAttributes(int program)
        {
            attributes.Clear();

            GLL.GetProgram(program, GetProgramParameterName.ActiveAttributes, out activeAttributeCount);
            for (int i = 0; i < activeAttributeCount; i++)
            {
                string name = GLL.GetActiveAttrib(program, i, out int size, out ActiveAttribType type);
                int location = GLL.GetAttribLocation(program, name);

                // Overwrite existing vertex attributes.
                attributes[name] = location;
            }
        }

        public int GetAttribute(string name)
        {
            if (string.IsNullOrEmpty(name) || !attributes.ContainsKey(name))
                return -1;
            else
                return attributes[name];
        }


        public void EnableVertexAttributes()
        {
            foreach (KeyValuePair<string, int> attrib in attributes)
                GLL.EnableVertexAttribArray(attrib.Value);
        }

        public void DisableVertexAttributes()
        {
            foreach (KeyValuePair<string, int> attrib in attributes)
                GLL.DisableVertexAttribArray(attrib.Value);
        }

        public void SaveBinary(string fileName)
        {
            CreateBinary(out byte[] binaryData, out BinaryFormat format);
            System.IO.File.WriteAllBytes(fileName + ".bin", binaryData);
            System.IO.File.WriteAllBytes(fileName + ".format", BitConverter.GetBytes((int)format));
        }

        private void CreateBinary(out byte[] binaryData, out BinaryFormat format)
        {
            GLL.GetProgram(program, (GetProgramParameterName)GL_PROGRAM_BINARY_MAX_LENGTH, out int size);
            binaryData = new byte[size];
            GLL.GetProgramBinary(program, size, out _, out format, binaryData);
        }

        public virtual void OnCompiled() { }

        private void CompileShaders()
        {
            foreach (Shader shader in shaders)
            {
                GLL.AttachShader(program, shader.id);
            }

            //Before linking, attach feedback varyings
            TransformFeedbackVaryings(program);

            GLL.LinkProgram(program);
            foreach (var shader in shaders)
            {
                Console.WriteLine($"{shader.type.ToString("g")}:");

                string log = GLL.GetShaderInfoLog(shader.id);
                Console.WriteLine(log);
            }
            LoadAttributes(program);
            LoadUniorms(program);

            LinkSucessful = true;
        }

        private void TransformFeedbackVaryings(int program)
        {
            if (FeedbackVaryings.Length == 0)
                return;

            string[] varyings = this.FeedbackVaryings;
            GLL.TransformFeedbackVaryings(program, varyings.Length, varyings, TransformFeedbackMode.SeparateAttribs);
        }
    }

    public class Shader : IDisposable
    {
        public Shader(string src, ShaderType type)
        {
            id = GLL.CreateShader(type);
            GLL.ShaderSource(id, src);
            GLL.CompileShader(id);
            this.type = type;
        }

        public string GetShaderSource()
        {
            string source = "";

            GLL.GetShader(id, ShaderParameter.ShaderSourceLength, out int length);
            if (length != 0)
                GLL.GetShaderSource(id, length, out _, out source);
            return source;
        }

        public string GetInfoLog()
        {
            return GLL.GetShaderInfoLog(id);
        }

        public void Dispose()
        {
            GLL.DeleteShader(id);
        }

        public ShaderType type;

        public int id;
    }

    public class FragmentShader : Shader
    {
        public FragmentShader(string src)
            : base(src, ShaderType.FragmentShader)
        {

        }
    }

    public class VertexShader : Shader
    {
        public VertexShader(string src)
            : base(src, ShaderType.VertexShader)
        {

        }
    }

    public class GeomertyShader : Shader
    {
        public GeomertyShader(string src)
            : base(src, ShaderType.GeometryShader)
        {

        }
    }
}
