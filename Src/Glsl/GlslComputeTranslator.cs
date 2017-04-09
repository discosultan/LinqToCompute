using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using LinqToCompute.Utilities;

namespace LinqToCompute.Glsl
{
    internal class GlslComputeTranslator : ExpressionVisitor
    {
        private VulkanDevice _device;
        private GlslComputeShader _shader;
        private VulkanComputeContext _ctx;

        // A pair of variables that are used to keep track of previous and current target assignments.
        private GlslVariable SrcVariable;
        private GlslVariable DstVariable;
        // Initial buffer accesses are using built-in `gl_GlobalInvocationID` as index.
        // Keeps track if such a special index should be used.
        private bool _isBufferStartOfChain = true;

        // Keeps track of member access (i.e `int w = x.y.z`).
        private readonly Stack<MemberInfo> _parameterMemberChain = new Stack<MemberInfo>();
        // Keeps track of active referenced buffer variables.
        private readonly Stack<GlslVariable> _bufferVariables = new Stack<GlslVariable>();
        // Maps lambda parameter names to buffer variables.
        private readonly Dictionary<string, GlslVariable> _lambdaParamToBufferMap = new Dictionary<string, GlslVariable>();
        // Keeps track of buffer variables written to GLSL shader.
        private readonly Dictionary<object, GlslVariable> _existingBuffersMap = new Dictionary<object, GlslVariable>();        
        // Keeps track of structs written to GLSL shader.
        private readonly HashSet<Type> _existingStructsSet = new HashSet<Type>
        {
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Matrix3x2),
            typeof(Matrix4x4)
        };
        // Since the method signatures of C# methods do not map one-to-one to GLSL functions,
        // this map is used to track required type conversions for convert type expressions.
        private readonly Dictionary<Expression, Type> _convertNodeToTypeMap = new Dictionary<Expression, Type>();

        public VulkanComputeContext Translate(Expression expression, VulkanDevice device)
        {
            _device = device;
            _shader = new GlslComputeShader();

            _ctx = new VulkanComputeContext(device);

            Visit(expression);

            _ctx.Output = ResolveOutput(_ctx.Inputs[0].Count, expression);
            GlslVariable outputVar = _shader.AddBuffer(_ctx.Output.ElementType, false, true);
            _shader.Main.AppendLine($"{outputVar.Name} = {SrcVariable.Name};");

            _ctx.SpirV = _shader.CompileToSpirV();

            return _ctx;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            bool handled = false;

            handled = handled || HandleQueryMethodCall(node);    // Translate LINQ query.
            handled = handled || HandleStandardMethodCall(node); // Translate built-in GLSL function.

            if (!handled)
                throw new NotImplementedException();

            return node;
        }

        private bool HandleQueryMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType != typeof(Queryable))
                return false;

            switch (node.Method.Name)
            {
                case nameof(Queryable.Select): // Implements LINQ Select query operator.
                {
                    Visit(node.Arguments[0]);

                    DstVariable = GlslVariable.Next(node.Type.GenericTypeArguments[0]);

                    _shader.Main.Append($"{DstVariable.Type.GlslName()} {DstVariable.Name} = ");

                    _parameterMemberChain.Clear();
                   _lambdaParamToBufferMap.Clear();
                    var lambda = (LambdaExpression)node.Arguments[1].StripQuotes();
                    _lambdaParamToBufferMap.Add(lambda.Parameters[0].Name, _bufferVariables.Pop());
                    _isBufferStartOfChain = false;

                    Visit(lambda.Body);

                    if (!_shader.Main.IsNewline)
                        _shader.Main.AppendLine(";");

                    _bufferVariables.Push(DstVariable);
                    SrcVariable = DstVariable;
                    return true;
                }
                case nameof(Queryable.Zip): // Implements LINQ Zip query operator.
                {
                    Visit(node.Arguments[0]);
                    Visit(node.Arguments[1]);

                    DstVariable = GlslVariable.Next(node.Type.GenericTypeArguments[0]);

                    _shader.Main.Append($"{DstVariable.Type.GlslName()} {DstVariable.Name} = ");

                    _parameterMemberChain.Clear();
                    _lambdaParamToBufferMap.Clear();
                    var lambda = (LambdaExpression)node.Arguments[2].StripQuotes();
                    _lambdaParamToBufferMap.Add(lambda.Parameters[1].Name, _bufferVariables.Pop());
                    _lambdaParamToBufferMap.Add(lambda.Parameters[0].Name, _bufferVariables.Pop());
                    _isBufferStartOfChain = false;

                    Visit(lambda.Body);

                    if (!_shader.Main.IsNewline)
                        _shader.Main.AppendLine(";");

                    _bufferVariables.Push(DstVariable);
                    SrcVariable = DstVariable;
                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }

        private bool HandleStandardMethodCall(MethodCallExpression node)
        {
            if (_glslFunctionMap.TryGetValue(node.Method.Name, out var translate))
            {
                translate(this, node);
                return true;
            }
            return false;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.ArrayIndex:
                    Visit(node.Left);
                    GlslVariable variable = _bufferVariables.Pop();
                    _shader.Main.Append(variable.Name);
                    _shader.Main.Append("[");
                    Visit(node.Right);
                    _shader.Main.Append("]");
                    break;
                case ExpressionType.Multiply:
                    Visit(node.Right);
                    _shader.Main.Append($" {node.NodeType.GlslSymbol()} ");
                    Visit(node.Left);
                    break;
                default:
                    Visit(node.Left);
                    _shader.Main.Append($" {node.NodeType.GlslSymbol()} ");
                    Visit(node.Right);
                    break;
            }
            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Negate:
                    _shader.Main.Append("-");
                    return base.VisitUnary(node);
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    switch (node.Operand)
                    {
                        case MethodCallExpression mOperand:
                            if (_glslMethodInfoMap.TryGetValue(mOperand.Method.Name, out GlslMethodSignature mappedType))
                            {
                                if (mappedType.Returns != node.Type)
                                    _convertNodeToTypeMap.Add(node, mappedType.Returns);

                                for (int i = 0; i < mOperand.Arguments.Count; i++)
                                {
                                    Expression arg = mOperand.Arguments[i];
                                    if ((arg.NodeType == ExpressionType.Convert ||
                                         arg.NodeType == ExpressionType.ConvertChecked) &&
                                        arg.Type != mappedType.Parameters[i])
                                    {
                                        _convertNodeToTypeMap.Add(arg, mappedType.Parameters[i]);
                                    }
                                }
                            }
                            goto default;
                        case ParameterExpression _:
                        case BinaryExpression _:
                        case MemberExpression _:
                            _shader.Main.Append(_convertNodeToTypeMap.TryGetValue(node, out Type convertTo)
                                ? convertTo.GlslName()
                                : node.Type.GlslName());
                            break;
                        default:
                            _shader.Main.Append(node.Type.GlslName());
                            break;
                    }
                    _shader.Main.Append("(");
                    base.VisitUnary(node);
                    _shader.Main.Append(")");
                    return node;
                default:
                    return base.VisitUnary(node);
            }
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            EnsureTypeAdded(node.Type);
            _parameterMemberChain.Push(node.Member);
            return base.VisitMember(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            EnsureTypeAdded(node.Type);

            GlslVariable variable = _lambdaParamToBufferMap[node.Name];
            string srcName = variable.Name;
            while (_parameterMemberChain.Count > 0)
            {
                MemberInfo member = _parameterMemberChain.Pop();
                srcName += "." + member.GlslName();
            }

            _shader.Main.Append(srcName);

            return node;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            EnsureTypeAdded(node.Type);

            _shader.Main.Append($"{node.Type.GlslName()}(");
            for (var i = 0; i < node.Arguments.Count; i++)
            {
                Visit(node.Arguments[i]);
                // If not last, add comma.
                if (i != node.Arguments.Count - 1)
                    _shader.Main.Append(", ");
            }
            _shader.Main.Append(")");
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            object value = node.Value;
            switch (value)
            {
                case IQueryable query:
                {
                    if (!_existingBuffersMap.TryGetValue(query, out GlslVariable variable))
                    {
                        VulkanBuffer computeBuffer = QueryToComputeBuffer(value);
                        _ctx.Inputs.Add(computeBuffer);
                        variable = _shader.AddBuffer(computeBuffer.ElementType, true, _isBufferStartOfChain);
                        _existingBuffersMap.Add(query, variable);
                    }
                    _bufferVariables.Push(variable);
                    break;
                }
                case Array array:
                {
                    if (!_existingBuffersMap.TryGetValue(array, out GlslVariable variable))
                    {
                        var computeBuffer = new VulkanBuffer(_device, array, TypeSystem.GetElementType(node.Type), ResourceDirection.CpuToGpu);
                        _ctx.Inputs.Add(computeBuffer);
                        variable = _shader.AddBuffer(computeBuffer.ElementType, true, _isBufferStartOfChain);
                        _existingBuffersMap.Add(array, variable);
                    }
                    _bufferVariables.Push(variable);
                    break;
                }
                default:
                {
                    if (value == null)
                        throw new NotImplementedException();
                    _shader.Main.Append(value.GlslLiteral());
                    break;
                }
            }
            return node;
        }

        private VulkanBuffer QueryToComputeBuffer(object value)
        {
            Type type = value.GetType();
            Type elementType = TypeSystem.GetElementType(type);
            PropertyInfo pi = type.GetProperty("Enumerable", BindingFlags.NonPublic | BindingFlags.Instance);
            var array = (Array)pi.GetValue(value);

            return new VulkanBuffer(_device, array, elementType, ResourceDirection.CpuToGpu);
        }

        private VulkanBuffer ResolveOutput(int count, Expression expression)
        {
            Type type = TypeSystem.GetElementType(expression.Type);
            Array array = Array.CreateInstance(type, count);

            return new VulkanBuffer(_device, array, type, ResourceDirection.GpuToCpu);
        }

        private void EnsureTypeAdded(Type type)
        {
            TypeCode typeCode = Type.GetTypeCode(type);
            if (typeCode == TypeCode.Object &&
                !_existingStructsSet.Contains(type))
            {
                _shader.AddStruct(type);
                _existingStructsSet.Add(type);
            }
        }

        private static readonly Dictionary<string, Action<GlslComputeTranslator, MethodCallExpression>> _glslFunctionMap
            = new Dictionary<string, Action<GlslComputeTranslator, MethodCallExpression>>(StringComparer.OrdinalIgnoreCase)
            {
                ["negate"] = (self, node) =>
                {
                    self._shader.Main.Append("-");
                    self.Visit(node.Arguments[0]);
                },
                ["add"] = (self, node) =>
                {
                    self.Visit(node.Arguments[0]);
                    self._shader.Main.Append(" + ");
                    self.Visit(node.Arguments[1]);
                },
                ["subtract"] = (self, node) =>
                {
                    self.Visit(node.Arguments[0]);
                    self._shader.Main.Append(" - ");
                    self.Visit(node.Arguments[1]);
                },
                // Note that we multiply matrices in GLSL in reverse order because while in C#
                // matrices are represented in row-major order, in GLSL they are in column-major order.
                ["multiply"] = (self, node) =>
                {
                    self.Visit(node.Arguments[1]);
                    self._shader.Main.Append(" * ");
                    self.Visit(node.Arguments[0]);
                },
                ["divide"] = (self, node) =>
                {
                    self.Visit(node.Arguments[0]);
                    self._shader.Main.Append(" / ");
                    self.Visit(node.Arguments[1]);
                },
                ["dot"] = (self, node) =>
                {
                    self._shader.Main.Append("dot(");
                    self.Visit(node.Arguments[0]);
                    self._shader.Main.Append(", ");
                    self.Visit(node.Arguments[1]);
                    self._shader.Main.Append(")");
                },
                ["cross"] = (self, node) =>
                {
                    self._shader.Main.Append("cross(");
                    self.Visit(node.Arguments[0]);
                    self._shader.Main.Append(", ");
                    self.Visit(node.Arguments[1]);
                    self._shader.Main.Append(")");
                },
                ["sin"] = (self, node) =>
                {
                    self._shader.Main.Append("sin(");
                    self.Visit(node.Arguments[0]);
                    self._shader.Main.Append(")");
                },
                ["cos"] = (self, node) =>
                {
                    self._shader.Main.Append("cos(");
                    self.Visit(node.Arguments[0]);
                    self._shader.Main.Append(")");
                },
                ["tan"] = (self, node) =>
                {
                    self._shader.Main.Append("tan(");
                    self.Visit(node.Arguments[0]);
                    self._shader.Main.Append(")");
                },
                ["length"] = (self, node) =>
                {
                    self._shader.Main.Append("length(");
                    self.Visit(node.Object);
                    self._shader.Main.Append(")");
                },
                ["distance"] = (self, node) =>
                {
                    self._shader.Main.Append("distance(");
                    self.Visit(node.Arguments[0]);
                    self._shader.Main.Append(", ");
                    self.Visit(node.Arguments[1]);
                    self._shader.Main.Append(")");
                },
                ["abs"] = (self, node) =>
                {
                    self._shader.Main.Append("abs(");
                    self.Visit(node.Arguments[0]);
                    self._shader.Main.Append(")");
                },
                ["lerp"] = (self, node) =>
                {
                    self._shader.Main.Append("mix(");
                    self.Visit(node.Arguments[0]);
                    self._shader.Main.Append(", ");
                    self.Visit(node.Arguments[1]);
                    self._shader.Main.Append(", ");
                    self.Visit(node.Arguments[2]);
                    self._shader.Main.Append(")");
                },
                ["clamp"] = (self, node) =>
                {
                    self._shader.Main.Append("clamp(");
                    self.Visit(node.Arguments[0]);
                    self._shader.Main.Append(", ");
                    self.Visit(node.Arguments[1]);
                    self._shader.Main.Append(", ");
                    self.Visit(node.Arguments[2]);
                    self._shader.Main.Append(")");
                },
                ["log"] = (self, node) =>
                {
                    self._shader.Main.Append("log(");
                    self.Visit(node.Arguments[0]);
                    self._shader.Main.Append(")");
                },
                ["floor"] = (self, node) =>
                {
                    self._shader.Main.Append("floor(");
                    self.Visit(node.Arguments[0]);
                    self._shader.Main.Append(")");
                },
                ["ceiling"] = (self, node) =>
                {
                    self._shader.Main.Append("ceil(");
                    self.Visit(node.Arguments[0]);
                    self._shader.Main.Append(")");
                },
                ["round"] = (self, node) =>
                {
                    self._shader.Main.Append("round(");
                    self.Visit(node.Arguments[0]);
                    self._shader.Main.Append(")");
                },
                ["exp"] = (self, node) =>
                {
                    self._shader.Main.Append("exp(");
                    self.Visit(node.Arguments[0]);
                    self._shader.Main.Append(")");
                },
                ["pow"] = (self, node) =>
                {
                    self._shader.Main.Append("pow(");
                    self.Visit(node.Arguments[0]);
                    self._shader.Main.Append(", ");
                    self.Visit(node.Arguments[1]);
                    self._shader.Main.Append(")");
                }
            };

        private static readonly Dictionary<string, GlslMethodSignature> _glslMethodInfoMap =
            new Dictionary<string, GlslMethodSignature>(StringComparer.OrdinalIgnoreCase)
            {
                ["sin"] = new GlslMethodSignature(typeof(float), typeof(float)),
                ["cos"] = new GlslMethodSignature(typeof(float), typeof(float)),
                ["tan"] = new GlslMethodSignature(typeof(float), typeof(float)),
                ["log"] = new GlslMethodSignature(typeof(float), typeof(float)),
                ["floor"] = new GlslMethodSignature(typeof(float), typeof(float)),
                ["ceiling"] = new GlslMethodSignature(typeof(float), typeof(float)),
                ["round"] = new GlslMethodSignature(typeof(float), typeof(float)),
                ["exp"] = new GlslMethodSignature(typeof(float), typeof(float)),
                ["pow"] = new GlslMethodSignature(typeof(float), typeof(float), typeof(float))
            };
    }
}
