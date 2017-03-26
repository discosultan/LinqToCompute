using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LinqToCompute.Utilities
{
    internal static class Extensions
    {
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
        {
            return value.Select((c, i) => new { c, i }).FirstOrDefault(x => charPredicate(x.c))?.i ?? -1;
        }
    }
}
