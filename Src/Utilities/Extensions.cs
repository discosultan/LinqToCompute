using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LinqToCompute.Utilities
{
    internal static class Extensions
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
                ["matrix3x2"] = "mat3x2", // column-major
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

        // Ref: https://blogs.msdn.microsoft.com/mattwar/2007/07/31/linq-building-an-iqueryable-provider-part-ii/
        public static Expression StripQuotes(this Expression expression)
        {
            while (expression.NodeType == ExpressionType.Quote)
                expression = ((UnaryExpression)expression).Operand;
            return expression;
        }

        // Ref: http://stackoverflow.com/a/5525191/1466456
        public static int PowerOfTwo(this int x)
        {
            x--; // Comment out to always take the next bigger power of two, even if x is already a power of two.
            x |= (x >> 1);
            x |= (x >> 2);
            x |= (x >> 4);
            x |= (x >> 8);
            x |= (x >> 16);
            return (x + 1);
        }

        public static int Align(this int value, int alignment)
        {
            int modulo = value % alignment;
            return modulo == 0
                ? value
                : value + (alignment - modulo);
        }

        public static long Align(this long value, long alignment)
        {
            long modulo = value % alignment;
            return modulo == 0
                ? value
                : value + (alignment - modulo);
        }

        public static int ManagedSize(this Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean: return sizeof(bool);
                case TypeCode.Byte: return sizeof(byte);
                case TypeCode.Char: return sizeof(char);
                case TypeCode.Int16: return sizeof(short);
                case TypeCode.Int32: return sizeof(int);
                case TypeCode.Int64: return sizeof(long);
                case TypeCode.UInt16: return sizeof(ushort);
                case TypeCode.UInt32: return sizeof(uint);
                case TypeCode.UInt64: return sizeof(ulong);
                case TypeCode.Single: return sizeof(float);
                case TypeCode.Double: return sizeof(double);
                default: return Marshal.SizeOf(type);
            }
        }

        // Ref: http://stackoverflow.com/a/16043551/1466456
        public static Type GetUnderlyingType(this MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Event:
                    return ((EventInfo)member).EventHandlerType;
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Method:
                    return ((MethodInfo)member).ReturnType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                default:
                    throw new NotImplementedException();
            }
        }

        // Ref: http://stackoverflow.com/a/2483054/1466456
        public static bool IsAnonymous(this Type type)
        {
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false) &&
                   (type.Name.Contains("AnonymousType") || type.Name.Contains("AnonType"));
        }

        public static bool IsTransparent(this string name)
        {
            return name.Contains("h__TransparentIdentifier");
        }

        public static int IndexOf(this string value, Func<char, bool> charPredicate)
            => value.Select((c, i) => new { c, i }).FirstOrDefault(x => charPredicate(x.c))?.i ?? -1;
    }
}
