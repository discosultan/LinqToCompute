using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LinqToCompute.Utilities;

namespace LinqToCompute.Glsl
{
    internal class GlslComputeShader
    {
        private bool _mainOpen;
        private readonly StringBuilder _header = new StringBuilder();
        private readonly StringBuilder _structs = new StringBuilder();
        private readonly StringBuilder _buffers = new StringBuilder();
        private readonly IndentingStringBuilder _functions = new IndentingStringBuilder();

        private int _bindingCounter;

        public GlslComputeShader()
        {
            _header.AppendLine("#version 450");
            // Set local workgorup size.
            _header.AppendLine("layout(local_size_x = 256) in;");
            // Sets the default layout for Shader Storage Buffer Objects (SSBO).
            _header.AppendLine("layout(std430) buffer;");
            _header.AppendLine();

            BeginMain();
        }

        public IndentingStringBuilder Main { get; } = new IndentingStringBuilder();

        public GlslVariable AddBuffer(Type elementType, bool isReadOnly, bool isStartOfChain)
        {
            int number = _bindingCounter++;

            string readOnlyMarker = isReadOnly ? " readonly" : "";
            string layoutName = "layout" + number;
            string name = "buffer" + number;

            _buffers.AppendLine($"layout (binding = {number}){readOnlyMarker} buffer {layoutName}");
            _buffers.AppendLine("{");
            _buffers.AppendLine($"    {elementType.GlslName()} {name}[];");
            _buffers.AppendLine("};");
            _buffers.AppendLine();

            return new GlslVariable(elementType, isStartOfChain ? name + "[gl_GlobalInvocationID.x]" : name, true);
        }

        public void AddStruct(Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);            

            _structs.AppendLine($"struct {type.GlslName()}");
            _structs.AppendLine("{");
            foreach (FieldInfo field in fields)
                _structs.AppendLine($"    {field.FieldType.GlslName()} {field.GlslName()};");
            _structs.AppendLine("};");
            _structs.AppendLine();
        }

        private void BeginMain()
        {
            _mainOpen = true;
            Main.AppendLine("void main()");
            Main.AppendLine("{", postIndent: true);
        }

        private void EndMain()
        {
            if (!_mainOpen) return;

            Main.AppendLine("}", preOutdent: true);
            Main.AppendLine();
            _mainOpen = false;
        }

        public byte[] CompileToSpirV()
        {
            string glslPath = $"{Guid.NewGuid()}.comp";
            string spirvPath = glslPath + ".spv";

            File.WriteAllText(glslPath, ToString());

            var process = Process.Start(new ProcessStartInfo("glslangValidator.exe", $"-V -o {spirvPath} {glslPath}")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process.WaitForExit();

            ThrowForErrorOutput(process.StandardOutput.ReadToEnd());
            
            return File.ReadAllBytes(spirvPath);
        }

        public override string ToString()
        {
            EndMain();
            return _header.ToString() + _structs.ToString() + _buffers.ToString() + _functions.ToString() + Main.ToString();
        }

        private static void ThrowForErrorOutput(string glslangOutput)
        {
            var error = glslangOutput
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(x => x.StartsWith("ERROR"));
            if (error != null)
            {
                throw new InvalidOperationException(error);
            }
        }
    }
}
