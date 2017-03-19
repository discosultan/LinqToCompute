using System;
using System.Linq;
using System.Text;

namespace LinqToCompute.Utilities
{
    internal class IndentingStringBuilder
    {
        private readonly StringBuilder _builder = new StringBuilder();

        public bool IsNewline { get; private set; } = true;
        public string IndentString { get; set; } = "    ";
        public int IndentLevel { get; set; }
        
        public void Append(object value)
        {
            if (IsNewline)
            {
                _builder.Append(Repeat(IndentString, IndentLevel));
                IsNewline = false;
            }
            _builder.Append(value);
        }

        public void AppendLine(object value = null, bool postIndent = false, bool preOutdent = false)
        {
            if (preOutdent) IndentLevel = Math.Max(IndentLevel - 1, 0);

            if (IsNewline) _builder.Append(Repeat(IndentString, IndentLevel));

            if (value == null)
            {
                _builder.AppendLine();
            }
            else
            {
                _builder.AppendLine(value.ToString());
            }
            IsNewline = true;

            if (postIndent) IndentLevel++;
        }

        public override string ToString() => _builder.ToString();

        private string Repeat(string value, int count) => string.Join("", Enumerable.Range(0, count).Select(_ => value));
    }
}
