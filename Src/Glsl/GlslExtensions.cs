using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using LinqToCompute.Utilities;

namespace LinqToCompute.Glsl
{
    internal static class GlslExtensions
    {
        private static readonly Dictionary<string, string> _typeMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["boolean"] = "bool",
                ["int32"] = "int",
                ["uint32"] = "uint",
                ["single"] = "float",
                ["vector2"] = "vec2",
                ["vector3"] = "vec3",
                ["vector4"] = "vec4",
                ["matrix4x4"] = "mat4",
                ["matrix3x2"] = "mat3x2", // Column-major.
                ["matrix"] = "mat4"
            };

        public static string GlslName(this Type type)
        {
            string name;
            if (type.IsAnonymous())
            {
                int startIndex = type.Name.IndexOf(char.IsDigit);
                int endIndex = type.Name.IndexOf('`'); // Exclusive.
                name = "anonymous" + type.Name.Substring(startIndex, endIndex - startIndex);
            }
            else
            {
                if (!_typeMap.TryGetValue(type.Name, out name))
                    name = type.Name.ToLowerInvariant();
            }
            return name;
        }

        public static string GlslName(this FieldInfo field)
        {
            string name;
            if (field.DeclaringType.IsAnonymous())
            {
                if (field.Name.IsTransparent())
                {
                    int endIndex = field.Name.LastIndexOf(">", StringComparison.Ordinal);
                    int startIndex = endIndex - 1;
                    while (char.IsDigit(field.Name[startIndex - 1]))
                        startIndex--;
                    name = "transparent" + field.Name.Substring(startIndex, endIndex - startIndex);
                }
                else
                {
                    name = field.Name.Substring(1, field.Name.IndexOf('>') - 1);
                }
            }
            else
            {
                name = field.Name.ToLowerInvariant();
            }
            return name;
        }

        public static string GlslName(this MemberInfo member)
        {
            string name;
            if (member.Name.IsTransparent())
            {
                int startIndex = member.Name.Length - 1;
                while (char.IsDigit(member.Name[startIndex - 1]))
                    startIndex--;
                name = "transparent" + member.Name.Substring(startIndex);
            }
            else
            {
                name = member.Name.ToLowerInvariant();
            }
            return name;
        }

        private static readonly Dictionary<ExpressionType, string> _expressionTypeMap =
            new Dictionary<ExpressionType, string>
            {
                [ExpressionType.Add] = "+",
                [ExpressionType.AddAssign] = "+=",
                [ExpressionType.Subtract] = "-",
                [ExpressionType.SubtractAssign] = "-=",
                [ExpressionType.Multiply] = "*",
                [ExpressionType.MultiplyAssign] = "*=",
                [ExpressionType.Divide] = "/",
                [ExpressionType.DivideAssign] = "/=",
                [ExpressionType.Modulo] = "%",
                [ExpressionType.ModuloAssign] = "%="
            };

        public static string GlslSymbol(this ExpressionType expressionType)
        {
            if (!_expressionTypeMap.TryGetValue(expressionType, out string symbol))
                throw new NotImplementedException();
            return symbol;
        }

        public static string GlslLiteral(this object value)
        {
            Type type = value.GetType();
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.String:
                    throw new NotImplementedException();
                case TypeCode.Object:
                    switch (value)
                    {
                        case Vector2 vec:
                            return $"vec2({Format(vec.X)}, {Format(vec.Y)})";
                        case Vector3 vec:
                            return $"vec3({Format(vec.X)}, {Format(vec.Y)}, {Format(vec.Z)})";
                        case Vector4 vec:
                            return $"vec4({Format(vec.X)}, {Format(vec.Y)}, {Format(vec.Z)}, {Format(vec.W)})";
                        case Matrix3x2 mat:
                            return $"mat3x2({Format(mat.M11)}, {Format(mat.M12)}, {Format(mat.M21)}, {Format(mat.M22)}, {Format(mat.M31)}, {Format(mat.M32)})";
                        case Matrix4x4 mat:
                            return $"mat4({Format(mat.M11)}, {Format(mat.M12)}, {Format(mat.M13)}, {Format(mat.M14)}, {Format(mat.M21)}, {Format(mat.M22)}, {Format(mat.M23)}, {Format(mat.M24)}, {Format(mat.M31)}, {Format(mat.M32)}, {Format(mat.M33)}, {Format(mat.M34)}, {Format(mat.M41)}, {Format(mat.M42)}, {Format(mat.M43)}, {Format(mat.M44)})";
                    }
                    throw new NotImplementedException();
                case TypeCode.Decimal:
                    return Format((decimal)value);
                case TypeCode.Double:
                    return Format((double)value);
                case TypeCode.Single:
                    return Format((float)value);
                default:
                    return value.ToString().ToLowerInvariant();
            }
        }

        private const string DecimalFormat = "0.################################"; // Up to 32 places.
        private static string Format(float value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Format(double value) => value.ToString(DecimalFormat, CultureInfo.InvariantCulture);
        private static string Format(decimal value) => value.ToString(DecimalFormat, CultureInfo.InvariantCulture);
    }
}
