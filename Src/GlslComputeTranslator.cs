using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Threading;
using LinqToCompute.Utilities;

namespace LinqToCompute
{
    internal class GlslComputeTranslator : ExpressionVisitor
    {
        private VulkanDevice _device;

        private GlslComputeShader _shader;
        private VulkanComputeExecutor _ctx;

        private GlslVariable SrcVariable;
        private GlslVariable DstVariable;

        private bool _startOfChain = true;

        private readonly Stack<GlslVariable> _variables = new Stack<GlslVariable>();
        private readonly Dictionary<object, GlslVariable> _bufferMap = new Dictionary<object, GlslVariable>();

        private readonly Dictionary<string, GlslVariable> _lambdaParamToGlslVariable =
            new Dictionary<string, GlslVariable>();

        public VulkanComputeExecutor Translate(Expression expression, VulkanDevice device)
        {
            _device = device;
            _shader = new GlslComputeShader();

            _ctx = new VulkanComputeExecutor(device);

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
                case nameof(Queryable.Select):
                {
                    Visit(node.Arguments[0]);

                    DstVariable = GlslVariable.Next(node.Type.GenericTypeArguments[0]);

                    _shader.Main.Append($"{DstVariable.GlslType} {DstVariable.Name} = ");

                    _memberChain.Clear();
                   _lambdaParamToGlslVariable.Clear();
                    var lambda = (LambdaExpression)node.Arguments[1].StripQuotes();
                    _lambdaParamToGlslVariable.Add(lambda.Parameters[0].Name, _variables.Pop());
                    _startOfChain = false;

                    Visit(lambda.Body);

                    if (!_shader.Main.IsNewline)
                        _shader.Main.AppendLine(";");

                    _variables.Push(DstVariable);
                    SrcVariable = DstVariable;
                    return true;
                }
                case nameof(Queryable.Zip):
                {
                    Visit(node.Arguments[0]);
                    Visit(node.Arguments[1]);

                    DstVariable = GlslVariable.Next(node.Type.GenericTypeArguments[0]);

                    _shader.Main.Append($"{DstVariable.GlslType} {DstVariable.Name} = ");

                    _memberChain.Clear();
                    _lambdaParamToGlslVariable.Clear();
                    var lambda = (LambdaExpression)node.Arguments[2].StripQuotes();
                    _lambdaParamToGlslVariable.Add(lambda.Parameters[1].Name, _variables.Pop());
                    _lambdaParamToGlslVariable.Add(lambda.Parameters[0].Name, _variables.Pop());
                    _startOfChain = false;

                    Visit(lambda.Body);

                    if (!_shader.Main.IsNewline)
                        _shader.Main.AppendLine(";");

                    _variables.Push(DstVariable);
                    SrcVariable = DstVariable;
                    return true;
                }
                default:
                {
                    return false;
                }
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
                    GlslVariable variable = _variables.Pop();
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

        private class MethodSignature
        {
            public MethodSignature(Type returns, params Type[] parameters)
            {
                Returns = returns;
                Parameters = parameters;
            }

            public Type Returns { get; }
            public Type[] Parameters { get; }
        }

        private static readonly Dictionary<string, MethodSignature> _glslMethodInfoMap =
            new Dictionary<string, MethodSignature>(StringComparer.OrdinalIgnoreCase)
            {
                ["sin"] = new MethodSignature(typeof(float), typeof(float)),
                ["cos"] = new MethodSignature(typeof(float), typeof(float)),
                ["tan"] = new MethodSignature(typeof(float), typeof(float)),
                ["log"] = new MethodSignature(typeof(float), typeof(float)),
                ["floor"] = new MethodSignature(typeof(float), typeof(float)),
                ["ceiling"] = new MethodSignature(typeof(float), typeof(float)),
                ["round"] = new MethodSignature(typeof(float), typeof(float)),
                ["exp"] = new MethodSignature(typeof(float), typeof(float)),
                ["pow"] = new MethodSignature(typeof(float), typeof(float), typeof(float))
            };

        private readonly Dictionary<Expression, Type> _convertNodeToTypeMap = new Dictionary<Expression, Type>();

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
                            if (_glslMethodInfoMap.TryGetValue(mOperand.Method.Name, out MethodSignature mappedType))
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

        protected override ElementInit VisitElementInit(ElementInit node)
        {
            return base.VisitElementInit(node);
        }
        
        protected override Expression VisitMember(MemberExpression node)
        {
            EnsureTypeAdded(node.Type);
            _memberChain.Push(node.Member);
            return base.VisitMember(node);
        }

        protected override MemberBinding VisitMemberBinding(MemberBinding node)
        {
            return base.VisitMemberBinding(node);
        }

        private readonly Stack<MemberInfo> _memberChain = new Stack<MemberInfo>();
        protected override Expression VisitParameter(ParameterExpression node)
        {
            EnsureTypeAdded(node.Type);

            GlslVariable variable = _lambdaParamToGlslVariable[node.Name];
            string srcName = variable.Name;
            while (_memberChain.Count > 0)
            {
                MemberInfo member = _memberChain.Pop();
                srcName += "." + member.GlslName();
            }

            _shader.Main.Append(srcName);

            return node;
        }

        protected override Expression VisitIndex(IndexExpression node)
        {
            return base.VisitIndex(node);
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

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            return base.VisitLambda(node);
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            return base.VisitMemberAssignment(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            object value = node.Value;
            switch (value)
            {
                case IQueryable query:
                {
                    if (!_bufferMap.TryGetValue(query, out GlslVariable variable))
                    {
                        var computeBuffer = QueryToComputeBuffer(value);
                        _ctx.Inputs.Add(computeBuffer);
                        variable = _shader.AddBuffer(computeBuffer.ElementType, true, _startOfChain);
                        _bufferMap.Add(query, variable);
                    }
                    _variables.Push(variable);
                    break;
                }
                case Array array:
                {
                    if (!_bufferMap.TryGetValue(array, out GlslVariable variable))
                    {
                        var computeBuffer = new VulkanBuffer(_device, array, TypeSystem.GetElementType(node.Type), ResourceDirection.CpuToGpu);
                        _ctx.Inputs.Add(computeBuffer);
                        variable = _shader.AddBuffer(computeBuffer.ElementType, true, _startOfChain);
                        _bufferMap.Add(array, variable);
                    }
                    _variables.Push(variable);
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

        private readonly HashSet<Type> _availableTypes = new HashSet<Type>
        {
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Matrix3x2),
            typeof(Matrix4x4)
        };
        private void EnsureTypeAdded(Type type)
        {
            TypeCode typeCode = Type.GetTypeCode(type);
            if (typeCode == TypeCode.Object &&
                !_availableTypes.Contains(type))
            {
                _shader.AddStruct(type);
                _availableTypes.Add(type);
            }
        }
    }

    internal class GlslVariable
    {
        private static int _sequence;

        public GlslVariable(Type type, string name, bool isGlobal)
        {
            Type = type;
            Name = name;
            IsGlobal = isGlobal;
        }

        public string Name { get; }
        public bool IsGlobal { get; }
        public string GlslType => Type.GlslName();
        public Type Type { get; }

        public override string ToString() => GlslType + " " + Name;

        public static GlslVariable Next(Type type)
        {
            int id = Interlocked.Increment(ref _sequence);
            return new GlslVariable(type, "x" + id, false);
        }
    }
}
