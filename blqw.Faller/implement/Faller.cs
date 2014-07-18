using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.Data.Common;
using System.Collections;

namespace blqw
{
    /// <summary> 轻量级表达式树解析器
    /// </summary>
    public sealed class Faller : IFaller
    {
        private Faller() { }
        /// <summary> 创建一个解析器
        /// </summary>
        /// <param name="expr">lambda表达式</param>
        public static IFaller Create(LambdaExpression expr)
        {
            return new Faller() {
                _lambda = expr,
                Parameters = new List<DbParameter>(),
            };
        }

        #region EntryFlags
        private const int WHERE = 1;
        private const int ORDERBY = 2;
        private const int SETS = 3;
        private const int COLUMNS = 4;
        private const int VALUES = 5;
        private const int COLUMNS_VALUES = 6;
        #endregion

        #region Fields
        /// <summary> 表别名数组,方便获取表别名
        /// </summary>
        private static readonly string[] TableAlias = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };
        /// <summary> 用于判断DateTime.Now属性
        /// </summary>
        private static readonly MemberInfo _TimeNow = typeof(DateTime).GetProperty("Now");
        /// <summary> 方法入口
        /// </summary>
        private int _entry;
        /// <summary> 需要解析的lambda表达式树
        /// </summary>
        private LambdaExpression _lambda;
        /// <summary> 当前正在解析的lambda表达式树
        /// </summary>
        private Expression _currExpr;
        /// <summary> 解析提供程序
        /// </summary>
        private ISaw _saw;
        #endregion

        #region State
        /// <summary> 解析器状态数据
        /// </summary>
        private class State
        {
            private void Check(DustType type)
            {
                if (DustType != type)
                {
                    throw new NotSupportedException("结果类型错误");
                }
            }

            /// <summary> 反转当前 UnaryNot 状态
            /// </summary>
            public void Not()
            {
                UnaryNot = !UnaryNot;
            }

            private int _layer;
            /// <summary> 增加递归层数
            /// </summary>
            public void IncreaseLayer()
            {
                if (++_layer > 100)
                {
                    throw new OutOfMemoryException("表达式过于复杂");
                }
                DustType = blqw.DustType.Undefined;
            }
            /// <summary> 减少递归层数
            /// </summary>
            public void DecreaseLayer()
            {
                _layer--;
            }

            public DustType DustType { get; private set; }
            /// <summary> 在解析一元表达式时用于控制当前状态
            /// </summary>
            public bool UnaryNot { get; private set; }

            private string _sql;
            public string Sql
            {
                get
                {
                    Check(blqw.DustType.Sql);
                    return _sql;
                }
                set
                {
                    DustType = blqw.DustType.Sql;
                    _sql = value;
                }
            }

            private object _object;
            public object Object
            {
                get
                {
                    switch (DustType)
                    {
                        case DustType.Object:
                            return _object;
                        case DustType.Number:
                            return _number;
                        case DustType.Array:
                            return _array;
                        case DustType.Boolean:
                            return _boolean;
                        case DustType.DateTime:
                            return _datetime;
                        case DustType.Binary:
                            return _binary;
                        case DustType.String:
                            return _string;
                        case DustType.Sql:
                        case DustType.Undefined:
                        default:
                            throw new NotSupportedException("结果类型错误");
                    }
                }
                set
                {
                    var conv = value as IConvertible;
                    if (conv != null)
                    {
                        var code = conv.GetTypeCode();
                        if (code >= TypeCode.SByte && code <= TypeCode.Decimal)
                        {
                            DustType = DustType.Number;
                            _number = conv;
                        }
                        else
                        {
                            switch (code)
                            {
                                case TypeCode.DBNull:
                                    DustType = DustType.Object;
                                    _object = null;
                                    break;
                                case TypeCode.Boolean:
                                    DustType = DustType.Boolean;
                                    _boolean = conv.ToBoolean(null);
                                    break;
                                case TypeCode.String:
                                case TypeCode.Char:
                                    DustType = DustType.String;
                                    _string = conv.ToString(null);
                                    break;
                                case TypeCode.DateTime:
                                    DustType = DustType.DateTime;
                                    _datetime = conv.ToDateTime(null);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else if (value is byte[])
                    {
                        DustType = DustType.Binary;
                        _binary = (byte[])value;
                    }
                    else if (value is IEnumerable)
                    {
                        DustType = DustType.Array;
                        _array = (IEnumerable)value;
                    }
                    else if (value is SqlExpr)
                    {
                        DustType = DustType.Sql;
                        _sql = ((SqlExpr)value).Sql;
                    }
                    else
                    {
                        DustType = DustType.Object;
                        _object = value;
                    }
                }
            }

            private IConvertible _number;
            public IConvertible Number
            {
                get
                {
                    Check(blqw.DustType.Number);
                    return _number;
                }
                set
                {
                    DustType = blqw.DustType.Number;
                    _number = value;
                }
            }

            private IEnumerable _array;
            public IEnumerable Array
            {
                get
                {
                    Check(blqw.DustType.Array);
                    return _array;
                }
                set
                {
                    DustType = blqw.DustType.Array;
                    _array = value;
                }
            }

            private bool _boolean;
            public bool Boolean
            {
                get
                {
                    Check(blqw.DustType.Boolean);
                    return _boolean;
                }
                set
                {
                    DustType = blqw.DustType.Boolean;
                    _boolean = value;
                }
            }

            private DateTime _datetime;
            public DateTime DateTime
            {
                get
                {
                    Check(blqw.DustType.DateTime);
                    return _datetime;
                }
                set
                {
                    DustType = blqw.DustType.DateTime;
                    _datetime = value;
                }
            }

            private byte[] _binary;
            public byte[] Binary
            {
                get
                {
                    Check(blqw.DustType.Binary);
                    return _binary;
                }
                set
                {
                    DustType = blqw.DustType.Binary;
                    _binary = value;
                }
            }

            private string _string;
            public string String
            {
                get
                {
                    Check(blqw.DustType.String);
                    return _string;
                }
                set
                {
                    DustType = blqw.DustType.String;
                    _string = value;
                }
            }

            public bool IsNull()
            {
                switch (DustType)
                {
                    case DustType.Object:
                        return _object == null;
                    case DustType.Array:
                        return _array == null;
                    case DustType.String:
                        return _string == null;
                    default:
                        return false;
                }
            }
        }
        /// <summary> 表示解析器的各种状态
        /// </summary>
        private State _state;

        #endregion

        #region interface IFaller

        public string ToWhere(ISaw saw)
        {
            _entry = WHERE;
            _saw = saw;
            _state = new State();
            return GetSql(_lambda.Body);
        }

        public string ToOrderBy(ISaw saw, bool asc)
        {
            _entry = ORDERBY;
            _saw = saw;
            _state = new State();
            var expr = _lambda.Body;
            var newExpr = expr as NewExpression;
            if (newExpr != null)
            {
                return string.Join(", ", newExpr.Arguments.Select(it => _saw.OrderBy(GetSql(it), asc)));
            }
            var arrExpr = expr as NewArrayExpression;
            if (arrExpr != null)
            {
                return string.Join(", ", arrExpr.Expressions.Select(it => _saw.OrderBy(GetSql(it), asc)));
            }
            return _saw.OrderBy(GetSql(expr), asc);
        }

        public string ToSets(ISaw saw)
        {
            _entry = SETS;
            _saw = saw;
            _state = new State();
            var expr = _lambda.Body as MemberInitExpression;
            if (expr == null)
            {
                Throw("仅支持new Model{ Field1 = Value1, Field2 = Value2 }表达式");
            }
            if (expr.Bindings.Count == 0)
            {
                return "";
            }
            if (expr.Bindings.Count == 1)
            {
                return ToSets(expr.Bindings[0]);
            }
            return string.Join(", ", expr.Bindings.Select(ToSets));
        }

        public string ToColumns(ISaw saw)
        {
            _entry = COLUMNS;
            _saw = saw;
            _state = new State();
            var expr = _lambda.Body as NewExpression;
            if (expr == null || expr.Arguments.Count == 0)
            {
                return ToColumnAll();
            }

            if (expr.Arguments.Count == 1)
            {
                return ToColumn(expr.Arguments[0], expr.Members[0]);
            }
            else
            {
                return string.Join(", ", ToColumns(expr));
            }
        }
        
        public string ToValues(ISaw saw)
        {
            _entry = VALUES;
            _saw = saw;
            _state = new State();
            var expr = _lambda.Body;
            var arrExpr = expr as NewArrayExpression;
            if (arrExpr != null)
            {
                return string.Join(", ", arrExpr.Expressions.Select(GetSql));
            }
            return GetSql(expr);
        }

        public KeyValuePair<string, string> ToColumnsAndValues(ISaw saw)
        {
            _entry = COLUMNS_VALUES;
            _saw = saw;
            _state = new State();
            var expr = _lambda.Body as MemberInitExpression;
            if (expr == null)
            {
                Throw("仅支持 new Model{ Field1 = Value1, Field2 = Value2 } 表达式");
            }
            var binds = expr.Bindings;
            var length = binds.Count;
            if (length == 0)
            {
                return new KeyValuePair<string, string>();
            }
            if (length == 1)
            {
                MemberAssignment m = binds[0] as MemberAssignment;
                if (m == null)
                {
                    Throw("无法解释表达式 => " + binds[0].ToString());
                }
                return new KeyValuePair<string, string>(_saw.GetColumn(null, m.Member), GetSql(m.Expression));
            }

            var columns = new string[length];
            var values = new string[length];
            for (int i = 0; i < length; i++)
            {
                MemberAssignment m = binds[i] as MemberAssignment;
                if (m == null)
                {
                    Throw("无法解释表达式 => " + binds[i].ToString());
                }
                columns[i] = _saw.GetColumn(null, m.Member);
                values[i] = GetSql(m.Expression);
            }

            return new KeyValuePair<string, string>(string.Join(", ", columns), string.Join(", ", values));
        }

        public ICollection<DbParameter> Parameters { get; private set; }
        
        #endregion

        #region Parse

        private void Parse(Expression expr)
        {
            _state.IncreaseLayer();
            _currExpr = expr;
            if (expr != null)
            {
                switch (expr.NodeType)
                {
                    case ExpressionType.Negate:
                    case ExpressionType.NegateChecked:
                    case ExpressionType.Not:
                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                    case ExpressionType.ArrayLength:
                    case ExpressionType.Quote:
                    case ExpressionType.TypeAs:
                        this.Parse((UnaryExpression)expr);
                        break;
                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractChecked:
                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                    case ExpressionType.Divide:
                    case ExpressionType.Modulo:
                    case ExpressionType.And:
                    case ExpressionType.AndAlso:
                    case ExpressionType.Or:
                    case ExpressionType.OrElse:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                    case ExpressionType.Coalesce:
                    case ExpressionType.RightShift:
                    case ExpressionType.LeftShift:
                    case ExpressionType.ExclusiveOr:
                        this.Parse((BinaryExpression)expr);
                        break;
                    case ExpressionType.TypeIs:
                        this.Parse((TypeBinaryExpression)expr);
                        break;
                    case ExpressionType.Conditional:
                        this.Parse((ConditionalExpression)expr);
                        break;
                    case ExpressionType.Constant:
                        this.Parse((ConstantExpression)expr);
                        break;
                    case ExpressionType.Parameter:
                        this.Parse((ParameterExpression)expr);
                        break;
                    case ExpressionType.MemberAccess:
                        this.Parse((MemberExpression)expr);
                        break;
                    case ExpressionType.Call:
                        this.Parse((MethodCallExpression)expr);
                        break;
                    case ExpressionType.Lambda:
                        this.Parse((LambdaExpression)expr);
                        break;
                    case ExpressionType.New:
                        this.Parse((NewExpression)expr);
                        break;
                    case ExpressionType.NewArrayInit:
                    case ExpressionType.NewArrayBounds:
                        this.Parse((NewArrayExpression)expr);
                        break;
                    case ExpressionType.Invoke:
                        this.Parse((InvocationExpression)expr);
                        break;
                    case ExpressionType.MemberInit:
                        this.Parse((MemberInitExpression)expr);
                        break;
                    case ExpressionType.ListInit:
                        this.Parse((ListInitExpression)expr);
                        break;
                    case ExpressionType.ArrayIndex:
                        this.ParseArrayIndex((BinaryExpression)expr);
                        break;
                    default:
                        break;
                }
            }
            if (_state.DustType == DustType.Undefined)
            {
                Throw(expr);
            }
            _currExpr = expr;
            _state.DecreaseLayer();
        }
        private void ParseArrayIndex(BinaryExpression expr)
        {
            Parse(expr.Left);
            CheckDustType(DustType.Array);
            var arr = _state.Array;
            Parse(expr.Right);
            CheckDustType(DustType.Number);
            if (arr is IList)
            {
                _state.Object = ((IList)arr)[Convert.ToInt32(_state.Number)];
            }
            else
            {
                _state.Object = ((dynamic)arr)[Convert.ToInt32(_state.Number)];
            }
        }
        private void Parse(BinaryExpression expr)
        {
            //得到 expr.Right 部分的返回值
            Parse(expr.Right);
            //如果右边是布尔值常量
            if (_state.DustType == DustType.Boolean)
            {
                if ((expr.NodeType == ExpressionType.Equal) != _state.Boolean)
                {
                    _state.Not();
                }
                Parse(UnaryExpression.IsTrue(expr.Left));
                return;
            }
            var right = GetSawDust();
            // 解析 expr.Left 部分
            Parse(expr.Left);
            switch (_state.DustType)
            {
                case DustType.Sql:
                    _state.Sql = _saw.BinaryOperator(_state.Sql, ConvertBinaryOperator(expr.NodeType), right.ToSql());
                    return;
                case DustType.Number:
                    //如果左右都是 Number常量
                    if (right.Type == DustType.Number)
                    {
                        //直接计算结果
                        Math(expr, ((IConvertible)right.Value), ((IConvertible)right.Value));
                    }
                    else
                    {
                        _state.Sql = _saw.BinaryOperator(AddNumber(_state.Number), ConvertBinaryOperator(expr.NodeType), right.ToSql());
                    }
                    return;
                case DustType.Boolean:
                    //如果左边是布尔值常量,虽然这种写法很操蛋
                    if ((expr.NodeType == ExpressionType.Equal) != _state.Boolean)
                    {
                        _state.Not();
                    }
                    Parse(UnaryExpression.IsTrue(expr.Right));
                    return;
                case DustType.DateTime:
                    _state.Sql = _saw.BinaryOperator(AddObject(_state.DateTime), ConvertBinaryOperator(expr.NodeType), right.ToSql());
                    return;
                case DustType.Binary:
                    _state.Sql = _saw.BinaryOperator(AddObject(_state.Binary), ConvertBinaryOperator(expr.NodeType), right.ToSql());
                    return;
                case DustType.String:
                    _state.Sql = _saw.BinaryOperator(AddObject(_state.String), ConvertBinaryOperator(expr.NodeType), right.ToSql());
                    return;
                case DustType.Object:
                    _state.Sql = _saw.BinaryOperator(AddObject(_state.Object), ConvertBinaryOperator(expr.NodeType), right.ToSql());
                    return;
                case DustType.Undefined:
                case DustType.Array:
                default:
                    Throw(expr);
                    throw new NotImplementedException();
            }
        }
        private void Parse(ConditionalExpression expr) { Throw(expr); }
        private void Parse(ConstantExpression expr)
        {
            _state.Object = expr.Value;
        }
        private void Parse(ListInitExpression expr) { Throw(expr); }
        private void Parse(MemberExpression expr)
        {
            var para = expr.Expression as ParameterExpression;
            if (para != null)
            {
                //命名参数,返回 表别名.列名
                var index = _lambda.Parameters.IndexOf(para);
                _state.Sql = _saw.GetColumn(GetAlias(index), expr.Member);
                if (expr.Type == typeof(bool) && _entry == WHERE)
                {
                    _state.Sql = _saw.BinaryOperator(_state.Sql, BinaryOperatorType.Equal, AddBoolean(true));
                }
            }
            else if (object.ReferenceEquals(expr.Member, _TimeNow))
            {
                //如果是DateTime.Now 返回数据库的当前时间表达式
                var now = _saw.AddTimeNow(Parameters);
                //如果数据库没有相应的表达式,则使用C#中的当前时间
                if (now == null)
                {
                    _state.Object = DateTime.Now;
                }
                else
                {
                    _state.Sql = now;
                }
            }
            else
            {
                object target = null;
                // expr.Expression 不等于 null 说明是实例成员,否则是静态成员
                if (expr.Expression != null)
                {
                    Parse(expr.Expression);
                    if (_state.DustType == DustType.Sql) //实例成员,必然可以得到一个对象
                    {
                        if (expr.Member is PropertyInfo)
                        {
                            var method = ((PropertyInfo)expr.Member).GetGetMethod();
                            if (method == null)
                            {
                                Throw(expr);
                            }
                            _state.Sql = _saw.CallMethod(method, GetSawDust(), new SawDust[0]);
                            return;
                        }
                    }
                    else if (_state.IsNull())
                    {
                        Throw(expr);
                    }
                    else
                    {
                        target = _state.Object;
                    }
                }
                //判断 Member 是属性还是字段,使用反射,得到值
                var p = expr.Member as PropertyInfo;
                if (p != null)
                {
                    _state.Object = p.GetValue(target, null);
                }
                else //不是属性,只能是字段
                {
                    _state.Object = ((FieldInfo)expr.Member).GetValue(target);
                }
            }
        }
        private void Parse(MemberInitExpression expr) { Throw(expr); }
        private void Parse(NewArrayExpression expr)
        {
            var exps = expr.Expressions;
            var length = expr.Expressions.Count;
            var arr = Array.CreateInstance(typeof(SawDust), length);
            for (int i = 0; i < length; i++)
            {
                Parse(exps[i]);
                arr.SetValue(GetSawDust(), i);
            }
            _state.Array = arr;
        }
        private void Parse(NewExpression expr) { Throw(expr); }
        private void Parse(ParameterExpression expr) { Throw(expr); }
        private void Parse(TypeBinaryExpression expr) { Throw(expr); }
        private void Parse(UnaryExpression expr)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.Not:
                case ExpressionType.IsFalse:
                    _state.Not();
                    Parse(expr.Operand);
                    if (_state.DustType == DustType.Boolean)
                    {
                        _state.Boolean = _state.Boolean != _state.UnaryNot;
                    }
                    else if (_state.DustType != DustType.Sql)
                    {
                        _state.Sql = _saw.BinaryOperator(GetSql(), BinaryOperatorType.NotEqual, AddBoolean(_state.UnaryNot));
                    }
                    _state.Not();
                    return;
                case ExpressionType.IsTrue:
                    Parse(expr.Operand);
                    if (_state.DustType == DustType.Boolean)
                    {
                        _state.Boolean = _state.Boolean == _state.UnaryNot;
                    }
                    else if (_state.DustType != DustType.Sql)
                    {
                        _state.Sql = _saw.BinaryOperator(GetSql(), BinaryOperatorType.Equal, AddBoolean(_state.UnaryNot));
                    }
                    return;
                case ExpressionType.Convert:
                    Parse(expr.Operand);
                    if (_state.DustType != DustType.Sql)
                    {
                        if (_state.DustType == DustType.String && expr.Type == typeof(SqlExpr))
                        {
                            _state.Sql = _state.String;
                            return;
                        }
                        var obj = _state.Object;
                        if (!object.ReferenceEquals(obj.GetType(), expr.Type))
                        {
                            _state.Object = Convert.ChangeType(obj, expr.Type);
                        }
                    }
                    return;
                default:
                    Throw(expr);
                    throw new NotImplementedException();
            }
        }
        private void Parse(MethodCallExpression expr)
        {
            SawDust target;
            SawDust[] args;
            //尝试直接调用,如果成功 返回true 如果失败,返回已解析的对象
            if (TryInvoke(expr, out target, out args))
            {
                return;
            }

            var method = expr.Method;
            //表达式树有时会丢失方法的调用方类型,这时需要重新反射方法
            if (method.ReflectedType == typeof(object) && expr.Object != null)
            {
                method = expr.Object.Type.GetMethod(expr.Method.Name, expr.Method.GetParameters().Select(it => it.ParameterType).ToArray());
            }

            if (object.ReferenceEquals(method.ReflectedType, typeof(string)))
            {
                _state.Sql = ParseStringMethod(method, target, args);
                return;
            }
            else if (object.ReferenceEquals(method.ReflectedType, typeof(System.Linq.Enumerable)))
            {
                if (method.Name == "Contains" && args.Length == 2)
                {
                    if (args[0].Type == DustType.Array && args[1].Type == DustType.Sql)
                    {
                        var element = (string)args[1].Value;
                        string[] array;
                        var enumerable = args[0].Value as SawDust[];
                        if (enumerable != null)
                        {
                            array = enumerable.Select(it => it.ToSql()).ToArray();
                        }
                        else
                        {
                            array = ((IEnumerable)args[0].Value).Cast<object>().Select(ObjectToSql).ToArray();
                        }
                        _state.Sql = _saw.Contains(_state.UnaryNot, element, array);
                        return;
                    }
                    else if (args[0].Type == DustType.Sql && args[1].Type == DustType.String)
                    {
                        _state.Sql = ParseStringMethod(method, args[0], new SawDust[] { args[1] });
                        return;
                    }
                }
            }
            _state.Sql = _saw.CallMethod(method, target, args);
        }

        #endregion

        #region MyRegion

        #endregion

        #region ParseMethods


        private string ParseStringMethod(MethodInfo method, SawDust target, SawDust[] args)
        {
            if (args.Length >= 1)
            {
                switch (method.Name)
                {
                    case "StartsWith":
                        if (_state.UnaryNot)
                            return _saw.BinaryOperator(target.ToSql(), BinaryOperatorType.NotStartWith, args[0].ToSql());
                        return _saw.BinaryOperator(target.ToSql(), BinaryOperatorType.StartWith, args[0].ToSql());
                    case "EndsWith":
                        if (_state.UnaryNot)
                            return _saw.BinaryOperator(target.ToSql(), BinaryOperatorType.NotEndWith, args[0].ToSql());
                        return _saw.BinaryOperator(target.ToSql(), BinaryOperatorType.EndWith, args[0].ToSql());
                    case "Contains":
                        if (_state.UnaryNot)
                            return _saw.BinaryOperator(target.ToSql(), BinaryOperatorType.NotContains, args[0].ToSql());
                        return _saw.BinaryOperator(target.ToSql(), BinaryOperatorType.Contains, args[0].ToSql());
                    default:
                        break;
                }
            }
            return _saw.CallMethod(method, target, args);
        }



        #endregion

        #region Base

        internal string AddObject(object value)
        {
            return _saw.AddObject(value, Parameters);
        }

        internal string AddNumber(IConvertible value)
        {
            return _saw.AddNumber(value, Parameters);
        }

        internal string AddBoolean(bool value)
        {
            return _saw.AddBoolean(value, Parameters);
        }

        /// <summary> 强制抛出当前表达式的解析异常
        /// </summary
        private void Throw()
        {
            Throw(_currExpr);
        }

        /// <summary> 强制抛出表达式解析异常
        /// </summary>
        private void Throw(Expression expr)
        {
            if (expr == null)
            {
                Throw("缺失表达式");
            }
            Throw("无法解析表达式 => " + expr.ToString());
        }

        private void Throw(string message)
        {
            switch (_entry)
            {
                case WHERE:
                    message = "ToWhere失败:\n" + message;
                    break;
                case ORDERBY:
                    message = "ToOrderBy失败:\n" + message;
                    break;
                case SETS:
                    message = "ToSets失败:\n" + message;
                    break;
                case COLUMNS:
                    message = "ToColumns失败:\n" + message;
                    break;
                case VALUES:
                    message = "ToValues失败:\n" + message;
                    break;
                case COLUMNS_VALUES:
                    message = "ToColumnsAndValues失败:\n" + message;
                    break;    
                default:
                    break;
            }                              
         
            throw new NotSupportedException(message);
        }

        /// <summary> 根据索引获取表别名 a,b,c,d,e,f...类推
        /// </summary>
        private string GetAlias(int index)
        {
            if (index > 26)
            {
                throw new NotSupportedException("对象过多");
            }
            return (char)('a' + index) + "";
        }

        /// <summary> 解析参数中的表达式,得到结果,并转换成sql形式
        /// </summary>
        private string GetSql(Expression expr)
        {
            Parse(expr);
            return GetSql();
        }

        /// <summary> 获取任何对象的sql形式
        /// </summary>
        private string ObjectToSql(object obj)
        {
            if (obj == null || obj is DBNull)
            {
                return _saw.AddObject(null, Parameters);
            }
            else if (obj is bool)
            {
                return _saw.AddBoolean((bool)obj, Parameters);
            }
            var conv = obj as IConvertible;
            if (conv != null)
            {
                var code = conv.GetTypeCode();
                if (code >= TypeCode.SByte && code <= TypeCode.Decimal)
                {
                    return _saw.AddNumber(conv, Parameters);
                }
            }
            return _saw.AddObject(obj, Parameters);
        }

        /// <summary> 获取最后一次解析的结果,并转换成sql形式
        /// </summary>
        private string GetSql()
        {
            switch (_state.DustType)
            {
                case DustType.Sql:
                    return _state.Sql;
                case DustType.Number:
                    return _saw.AddNumber(_state.Number, Parameters);
                case DustType.Boolean:
                    return _saw.AddBoolean(_state.Boolean, Parameters);
                case DustType.Object:
                    return _saw.AddObject(_state.Object, Parameters);
                case DustType.DateTime:
                    return _saw.AddObject(_state.DateTime, Parameters);
                case DustType.String:
                    return _saw.AddObject(_state.String, Parameters);
                case DustType.Binary:
                    return _saw.AddObject(_state.Binary, Parameters);
                case DustType.Undefined:
                case DustType.Array:
                default:
                    throw new NotSupportedException("无法从当前结果中得到Sql");
            }
        }

        /// <summary> 将ExpressionType转为BinaryOperatorType
        /// </summary>
        private BinaryOperatorType ConvertBinaryOperator(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return BinaryOperatorType.Add;
                case ExpressionType.And:
                    return BinaryOperatorType.BitAnd;
                case ExpressionType.AndAlso:
                    return BinaryOperatorType.And;
                case ExpressionType.Divide:
                    return BinaryOperatorType.Divide;
                case ExpressionType.Equal:
                    return _state.UnaryNot ? BinaryOperatorType.NotEqual : BinaryOperatorType.Equal;
                case ExpressionType.NotEqual:
                    return _state.UnaryNot ? BinaryOperatorType.Equal : BinaryOperatorType.NotEqual;
                case ExpressionType.ExclusiveOr:
                    return BinaryOperatorType.BitXor;
                case ExpressionType.GreaterThan:
                    return BinaryOperatorType.GreaterThan;
                case ExpressionType.GreaterThanOrEqual:
                    return BinaryOperatorType.GreaterThanOrEqual;
                case ExpressionType.LeftShift:
                    return BinaryOperatorType.LeftShift;
                case ExpressionType.LessThan:
                    return BinaryOperatorType.LessThan;
                case ExpressionType.LessThanOrEqual:
                    return BinaryOperatorType.LessThanOrEqual;
                case ExpressionType.Modulo:
                    return BinaryOperatorType.Modulo;
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return BinaryOperatorType.Multiply;
                case ExpressionType.Or:
                    return BinaryOperatorType.BitOr;
                case ExpressionType.OrElse:
                    return BinaryOperatorType.Or;
                case ExpressionType.Power:
                    return BinaryOperatorType.Power;
                case ExpressionType.RightShift:
                    return BinaryOperatorType.RightShift;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return BinaryOperatorType.Subtract;
                default:
                    throw new NotSupportedException("无法解释 ExpressionType." + type.ToString());
            }
        }


        private SawDust GetSawDust()
        {
            if (_state.DustType == DustType.Sql)
            {
                return new SawDust(this, DustType.Sql, _state.Sql);
            }
            else
            {
                return new SawDust(this, _state.DustType, _state.Object);
            }
        }

        private void CheckDustType(DustType type)
        {
            if (_state.DustType != type)
            {
                if (type != blqw.DustType.Object)
                {
                    Throw();
                }
                var code = (int)_state.DustType;
                if (code <= 1 || code > 8)
                {
                    Throw();
                }
            }
        }


        private bool TryInvoke(MethodCallExpression expr, out SawDust target, out SawDust[] args)
        {
            //判断方法对象,如果是null为静态方法,反之为实例方法
            if (expr.Object == null)
            {
                target = new SawDust(this, DustType.Object, null);
            }
            else
            {
                Parse(expr.Object);
                target = GetSawDust();
            }

            var exprArgs = expr.Arguments;
            var length = exprArgs.Count;
            args = new SawDust[length];
            var call = target.Type != DustType.Sql;
            for (int i = 0; i < length; i++)
            {
                Parse(exprArgs[i]);
                if (_state.DustType == DustType.Sql)
                {
                    if (call) call = false;
                    args[i] = new SawDust(this, DustType.Sql, _state.Sql);
                }
                else
                {
                    args[i] = GetSawDust();
                }
            }

            if (call)
            {
                _state.Object = expr.Method.Invoke(target.Value, args.Select(it => it.Value).ToArray());
                return true;
            }
            else
            {
                return false;
            }
        }


        private void Math(Expression expr, IConvertible a, IConvertible b)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.Add:
                    unchecked { _state.Number = a.ToDecimal(null) + b.ToDecimal(null); }
                    return;
                case ExpressionType.AddChecked:
                    checked { _state.Number = a.ToDecimal(null) + b.ToDecimal(null); }
                    return;
                case ExpressionType.Subtract:
                    unchecked { _state.Number = a.ToDecimal(null) - b.ToDecimal(null); }
                    return;
                case ExpressionType.SubtractChecked:
                    checked { _state.Number = a.ToDecimal(null) - b.ToDecimal(null); }
                    return;
                case ExpressionType.Multiply:
                    unchecked { _state.Number = a.ToDecimal(null) * b.ToDecimal(null); }
                    return;
                case ExpressionType.MultiplyChecked:
                    checked { _state.Number = a.ToDecimal(null) * b.ToDecimal(null); }
                    return;
                case ExpressionType.Divide:
                    _state.Number = a.ToDecimal(null) / b.ToDecimal(null);
                    return;
                case ExpressionType.Modulo:
                    _state.Number = a.ToDecimal(null) % b.ToDecimal(null);
                    return;
                case ExpressionType.And:
                    _state.Number = (long)a & (long)b;
                    return;
                case ExpressionType.Or:
                    _state.Number = (long)a | (long)b;
                    return;
                case ExpressionType.RightShift:
                    if (a is int == false)
                    {
                        Throw(expr);
                    }
                    _state.Number = (int)a >> (int)b;
                    return;
                case ExpressionType.LeftShift:
                    if (a is int == false)
                    {
                        Throw(expr);
                    }
                    _state.Number = (int)a << (int)b;
                    return;
                case ExpressionType.LessThan:
                    _state.Boolean = ((IComparable)a).CompareTo((IComparable)b) < 0;
                    return;
                case ExpressionType.LessThanOrEqual:
                    _state.Boolean = ((IComparable)a).CompareTo((IComparable)b) <= 0;
                    return;
                case ExpressionType.GreaterThan:
                    _state.Boolean = ((IComparable)a).CompareTo((IComparable)b) > 0;
                    return;
                case ExpressionType.GreaterThanOrEqual:
                    _state.Boolean = ((IComparable)a).CompareTo((IComparable)b) >= 0;
                    return;
                case ExpressionType.Equal:
                    _state.Boolean = ((IComparable)a).CompareTo((IComparable)b) == 0;
                    return;
                case ExpressionType.NotEqual:
                    _state.Boolean = ((IComparable)a).CompareTo((IComparable)b) != 0;
                    return;
                case ExpressionType.ExclusiveOr:
                    if (a is int == false)
                    {
                        Throw(expr);
                    }
                    _state.Number = (int)a ^ (int)b;
                    return;
                default:
                    Throw(expr);
                    throw new NotImplementedException();
            }
        }



        #endregion

        #region ToSet
        
        private string ToSets(MemberBinding binding)
        {
            MemberAssignment m = binding as MemberAssignment;
            if (m == null)
            {
                throw new NotSupportedException("无法解释表达式 => " + m.ToString());
            }
            var column = _saw.GetColumn(null, m.Member);
            var value = GetSql(m.Expression);
            return _saw.UpdateSet(column, value);
        }

        #endregion

        #region ToColumn

        private IEnumerable<string> ToColumns(NewExpression expr)
        {
            var length = expr.Arguments.Count;
            for (int i = 0; i < length; i++)
            {
                var column = expr.Arguments[i];
                var alias = expr.Members[i];
                yield return ToColumn(column, alias);
            }
        }

        private string ToColumn(Expression column, MemberInfo alias)
        {
            var member = column as MemberExpression;
            if (member != null && member.Member.Name == alias.Name)
            {
                return GetSql(member);
            }
            else
            {
                return _saw.GetColumn(GetSql(column), alias.Name);
            }
        }

        private string ToColumnAll()
        {
            var expr = _lambda.Body as ConstantExpression;
            if (expr == null)
            {
                Throw(_lambda.Body);
            }
            Parse(expr);
            if (_state.DustType == DustType.Sql)
            {
                return _state.Sql;
            }
            if (_state.IsNull())
            {
                var length = _lambda.Parameters.Count;
                if (length > 26)
                {
                    throw new NotSupportedException("对象过多");
                }
                var columns = new char[length];
                for (int i = 0; i < length; i++)
                {
                    columns[i] = ((char)('a' + i));
                }
                return string.Join(".*, ", columns) + ".*";
            }
            else
            {
                return GetSql();
            }
        }

        #endregion

    }
}
