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
        private static void NotNull(object value, string argName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(argName);
            }
        }

        private bool IsNullable(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private Faller() { }
        /// <summary> 创建一个解析器
        /// </summary>
        /// <param name="expr">lambda表达式</param>
        public static IFaller Create(LambdaExpression expr, LambdaExpression parentExpr = null)
        {
            NotNull(expr, "expr");
            NotNull(expr.Body, "expr.Body");
            return new Faller() {
                _lambda = expr,
                Parameters = new List<DbParameter>(),
                EnabledAlias = expr.Parameters.Count > 1 || parentExpr != null,  //当对象只有1个的时候不使用别名
                _parentExpr = parentExpr
            };
        }

        #region interface IFaller

        public string ToWhere(ISaw saw)
        {
            NotNull(saw, "saw");
            _entry = WHERE;
            _saw = saw;
            _state = new State(this);
            try
            {
                Parse(_lambda.Body);
                if (_state.IsParameter)
                {
                    return _saw.BinaryOperation(_state.Sql, BinaryOperator.Equal, AddBoolean(true));
                }
                else if (_state.DustType == DustType.Boolean)
                {
                    return (_state.Boolean) ? " 1 = 1" : " 1 = 0";
                }
                return GetSql();
            }
            catch (Exception ex)
            {
                if (_throw == false)
                {
                    Throw(ex);
                }
                throw;
            }
        }

        public string ToOrderBy(ISaw saw, bool asc)
        {
            NotNull(saw, "saw");
            _entry = ORDERBY;
            _saw = saw;
            _state = new State(this);
            var expr = _lambda.Body;
            if (asc)
            {
                return ToValues(saw, it => it + " ASC");
            }
            return ToValues(saw, it => it + " DESC");
        }

        public string ToSets(ISaw saw)
        {
            NotNull(saw, "saw");
            _entry = SETS;
            _saw = saw;
            _state = new State(this);
            var expr = _lambda.Body as MemberInitExpression;
            if (expr == null)
            {
                Throw("仅支持new Model{ Field1 = Value1, Field2 = Value2 }表达式");
            }
            if (expr.Bindings.Count == 0)
            {
                return "";
            }
            try
            {
                if (expr.Bindings.Count == 1)
                {
                    return ToSets(expr.Bindings[0]);
                }
                return string.Join(", ", expr.Bindings.Select(ToSets));
            }
            catch (Exception ex)
            {
                if (_throw == false)
                {
                    Throw(ex);
                }
                throw;
            }
        }

        public string ToSelectColumns(ISaw saw)
        {
            NotNull(saw, "saw");
            _entry = COLUMNS;
            _saw = saw;
            _state = new State(this);
            var expr = _lambda.Body;
            try
            {
                if (expr == null ||
                       (expr.NodeType == ExpressionType.Constant) &&
                       ((ConstantExpression)expr).Value == null)
                {
                    return ToColumnAll();
                }
                Parse(expr);

                if (_state.DustType == DustType.Array)
                {
                    var arr = _state.Array as ISawDust[];
                    if (arr != null)
                    {
                        return string.Join(", ", arr.Select(it => it.ToSql()));
                    }
                    else
                    {
                        return string.Join(", ", _state.Array.Cast<object>().Select(GetSql));
                    }
                }
                else
                {
                    return GetSql();
                }
            }
            catch (Exception ex)
            {
                if (_throw == false)
                {
                    Throw(ex);
                }
                throw;
            }
        }

        public string ToValues(ISaw saw)
        {
            NotNull(saw, "saw");
            return ToValues(saw, null);
        }

        public string ToValues(ISaw saw, Func<string, string> replace)
        {
            NotNull(saw, "saw");
            if (_entry == 0)
            {
                _entry = VALUES;
            }
            _saw = saw;
            _state = new State(this);
            var expr = _lambda.Body;
            try
            {
                Parse(expr);
                if (_state.DustType == DustType.Array)
                {
                    var arr = _state.Array as ISawDust[];
                    if (replace == null)
                    {
                        if (arr != null)
                        {
                            return string.Join(", ", arr.Select(it => it.ToSql()));
                        }
                        return string.Join(", ", _state.Array.Cast<object>().Select(GetSql));
                    }
                    else
                    {
                        if (arr != null)
                        {
                            return string.Join(", ", arr.Select(it => replace(it.ToSql())));
                        }
                        return string.Join(", ", _state.Array.Cast<object>().Select(it => replace(GetSql(it))));
                    }
                }
                else if (replace == null)
                {
                    return GetSql(expr);
                }
                else
                {
                    return replace(GetSql(expr));
                }
            }
            catch (Exception ex)
            {
                if (_throw == false)
                {
                    Throw(ex);
                }
                throw;
            }
        }

        public KeyValuePair<string, string> ToColumnsAndValues(ISaw saw)
        {
            NotNull(saw, "saw");
            _entry = COLUMNS_VALUES;
            _saw = saw;
            _state = new State(this);
            var expr = _lambda.Body as MemberInitExpression;
            if (expr != null)
            {
                return ToColumnsAndValues(expr);
            }
            var expr2 = _lambda.Body as NewExpression;
            if (expr2 != null)
            {
                return ToColumnsAndValues(expr2);
            }
            Throw("仅支持 MemberInitExpression/NewExpression \n如:new Model{ Field1 = Value1, Field2 = Value2 } 表达式");
            throw new Exception();
        }

        public ICollection<DbParameter> Parameters { get; private set; }

        #endregion

        /// <summary> 是否使用别名
        /// </summary>
        public bool EnabledAlias { get; set; }
        /// <summary> 存在子表达式
        /// </summary>
        public bool ExistsSubExpression { get; set; }

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
        /// <summary> 是否已抛出异常
        /// </summary>
        private bool _throw = false;
        /// <summary> 父表达式
        /// </summary>
        private LambdaExpression _parentExpr;
        #endregion

        #region State
        /// <summary> 解析器状态数据,算是一种状态机
        /// </summary>
        private class State
        {
            private Faller _faller;
            public State(Faller faller)
            {
                _faller = faller;
            }
            private bool _unaryNot;
            private bool _isParameter;
            /// <summary> 验证当前状态
            /// </summary>
            /// <param name="type"></param>
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
                _unaryNot = !_unaryNot;
            }
            /// <summary> 初始化 UnaryNot 状态
            /// </summary>
            public void ResetUnaryNot()
            {
                _unaryNot = false;
            }
            /// <summary> 保存当前递归层数,最大100
            /// </summary>
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

            /// <summary> 当前解析结果的类型
            /// </summary>
            public DustType DustType { get; private set; }
            /// <summary> 在解析一元表达式时用于控制当前状态
            /// </summary>
            public bool UnaryNot
            {
                get
                {
                    try
                    {
                        return _unaryNot;
                    }
                    finally
                    {
                        _unaryNot = false;
                    }
                }
            }


            public bool IsParameter
            {
                get { return _isParameter; }
                set
                {
                    Check(blqw.DustType.Sql);
                    _isParameter = value;
                }
            }


            #region Value

            private string _sql;
            private object _object;
            private IConvertible _number;
            private ISubExpression _subExpression;
            private IEnumerable _array;
            private bool _boolean;
            private DateTime _datetime;
            private byte[] _binary;
            private string _string;

            public string Sql
            {
                get
                {
                    Check(blqw.DustType.Sql);
                    return _sql;
                }
                set
                {
                    if (_isParameter) _isParameter = false;
                    DustType = blqw.DustType.Sql;
                    _sql = value;
                }
            }

            public object Object
            {
                get
                {
                    switch (DustType)
                    {
                        case DustType.Object:
                            return _object;
                        case DustType.SubExpression:
                            return _subExpression;
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
                            if (_object is char)
                            {
                                return _object;
                            }
                            return _string;
                        case DustType.Sql:
                        case DustType.Undefined:
                        default:
                            throw new NotSupportedException("结果类型错误");
                    }
                }
                set
                {
                    if (_isParameter) _isParameter = false;
                    _object = null;
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
                                    DustType = DustType.String;
                                    _string = conv.ToString(null);
                                    break;
                                case TypeCode.Char:
                                    DustType = DustType.String;
                                    _object = value;
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
                    else if (value is ISubExpression)
                    {
                        DustType = DustType.SubExpression;
                        _subExpression = ((ISubExpression)value);
                        _subExpression.ParentExpression = _faller._lambda;
                    }
                    else
                    {
                        DustType = DustType.Object;
                        _object = value;
                    }
                }
            }

            public IConvertible Number
            {
                get
                {
                    Check(blqw.DustType.Number);
                    return _number;
                }
                set
                {
                    if (_isParameter) _isParameter = false;
                    DustType = blqw.DustType.Number;
                    _number = value;
                }
            }

            public IEnumerable Array
            {
                get
                {
                    Check(blqw.DustType.Array);
                    return _array;
                }
                set
                {
                    if (_isParameter) _isParameter = false;
                    DustType = blqw.DustType.Array;
                    _array = value;
                }
            }

            public bool Boolean
            {
                get
                {
                    Check(blqw.DustType.Boolean);
                    return _boolean;
                }
                set
                {
                    if (_isParameter) _isParameter = false;
                    DustType = blqw.DustType.Boolean;
                    _boolean = value;
                }
            }

            public DateTime DateTime
            {
                get
                {
                    Check(blqw.DustType.DateTime);
                    return _datetime;
                }
                set
                {
                    if (_isParameter) _isParameter = false;
                    DustType = blqw.DustType.DateTime;
                    _datetime = value;
                }
            }

            public byte[] Binary
            {
                get
                {
                    Check(blqw.DustType.Binary);
                    return _binary;
                }
                set
                {
                    if (_isParameter) _isParameter = false;
                    DustType = blqw.DustType.Binary;
                    _binary = value;
                }
            }

            public string String
            {
                get
                {
                    Check(blqw.DustType.String);
                    return _string;
                }
                set
                {
                    if (_isParameter) _isParameter = false;
                    DustType = blqw.DustType.String;
                    _string = value;
                }
            }

            public ISubExpression SubExpression
            {
                get
                {
                    Check(blqw.DustType.SubExpression);
                    return _subExpression;
                }
                set
                {
                    if (_isParameter) _isParameter = false;
                    DustType = blqw.DustType.SubExpression;
                    _subExpression = value;
                }
            }
            #endregion

            /// <summary> 判断值是否为空
            /// </summary>
            /// <returns></returns>
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
                    case DustType.Binary:
                        return _binary == null;
                    case DustType.SubExpression:
                        return _subExpression == null;
                    default:
                        return false;
                }
            }
        }
        /// <summary> 表示解析器的各种状态
        /// </summary>
        private State _state;

        #endregion

        #region Parse

        private Dictionary<MemberInfo, LiteracyGetter> Getters = new Dictionary<MemberInfo, LiteracyGetter>();

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
                    _state.Sql = _saw.BinaryOperation(_state.Sql, ConvertBinaryOperator(expr.NodeType), right.ToSql());
                    return;
                case DustType.Number:
                    //如果左右都是 Number常量
                    if (right.Type == DustType.Number)
                    {
                        //直接计算结果
                        Math(expr.NodeType, ((IConvertible)right.Value), ((IConvertible)right.Value));
                    }
                    else
                    {
                        _state.Sql = _saw.BinaryOperation(AddNumber(_state.Number), ConvertBinaryOperator(expr.NodeType), right.ToSql());
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
                    _state.Sql = _saw.BinaryOperation(AddObject(_state.DateTime), ConvertBinaryOperator(expr.NodeType), right.ToSql());
                    return;
                case DustType.Binary:
                    _state.Sql = _saw.BinaryOperation(AddObject(_state.Binary), ConvertBinaryOperator(expr.NodeType), right.ToSql());
                    return;
                case DustType.String:
                    _state.Sql = _saw.BinaryOperation(AddObject(_state.String), ConvertBinaryOperator(expr.NodeType), right.ToSql());
                    return;
                case DustType.Object:
                    _state.Sql = _saw.BinaryOperation(AddObject(_state.Object), ConvertBinaryOperator(expr.NodeType), right.ToSql());
                    return;
                case DustType.Undefined:
                case DustType.Array:
                default:
                    Throw(expr);
                    throw new NotImplementedException();
            }
        }
        private void Parse(ConditionalExpression expr) { Throw("不支持ConditionalExpression"); }
        private void Parse(ConstantExpression expr)
        {
            _state.Object = expr.Value;
        }
        private void Parse(ListInitExpression expr) { Throw("不支持ListInitExpression"); }
        private void Parse(MemberExpression expr)
        {
            var para = expr.Expression as ParameterExpression;
            if (para != null)
            {
                Parse(para, expr.Member);
                return;
            }

            if (object.ReferenceEquals(expr.Member, _TimeNow)) //判断 DateTime.Now
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
                return;
            }

            object target = null; //获取 非静态属性/字段的所属对象
            if (expr.Expression != null)
            {
                Parse(expr.Expression); //解释对象 实例成员,必然可以得到一个对象
                if (_state.DustType == DustType.Sql) //如果是Sql, 应该被解释为sql而不是对象
                {
                    if (expr.Member.MemberType == MemberTypes.Property)
                    {
                        var prop = (PropertyInfo)expr.Member;
                        if (prop.Name == "Value")
                        {
                            if (IsNullable(prop.ReflectedType))
                            {
                                return;
                            }
                        }
                        else if (prop.Name == "HasValue")
                        {
                            if (IsNullable(prop.ReflectedType))
                            {
                                _state.Sql = _saw.BinaryOperation(
                                    _state.Sql,
                                    _state.UnaryNot ? BinaryOperator.NotEqual : BinaryOperator.Equal,
                                    AddObject(null));
                                return;
                            }
                        }
                        _state.Sql = _saw.ParseProperty(prop, GetSawDust());
                    }
                    else
                    {
                        _state.Sql = _saw.ParseField((FieldInfo)expr.Member, GetSawDust());
                    }
                    return;
                }
                target = _state.Object;
                if (target == null) //这里不可能是null的...
                {
                    Throw(expr);
                }
            }

            LiteracyGetter getter;
            if (Getters.TryGetValue(expr.Member, out getter) == false)
            {
                lock (Getters)
                {
                    if (Getters.TryGetValue(expr.Member, out getter) == false)
                    {
                        if (expr.Member.MemberType == MemberTypes.Property)
                        {
                            getter = Literacy.CreateGetter((PropertyInfo)expr.Member);
                        }
                        else
                        {
                            getter = Literacy.CreateGetter((FieldInfo)expr.Member);
                        }
                    }
                }
            }

            _state.Object = getter(target);
        }
        private void Parse(MemberInitExpression expr) { Throw("不支持MemberInitExpression"); }
        private void Parse(NewArrayExpression expr)
        {
            var exps = expr.Expressions;
            var length = expr.Expressions.Count;
            var arr = Array.CreateInstance(expr.Type.GetElementType(), length);
            for (int i = 0; i < length; i++)
            {
                Parse(exps[i]);
                if (_state.DustType != DustType.Sql)
                {
                    arr.SetValue(_state.Object, i);
                }
                else
                {
                    var arr1 = new ISawDust[length];
                    var dust = GetSawDust();
                    if (i > 0)
                    {
                        _state.Object = arr.GetValue(0);
                        arr1[0] = new SawDust(this, _state.DustType, arr.GetValue(0));
                        for (int j = 1; j < i; j++)
                        {
                            arr1[j] = new SawDust(this, _state.DustType, arr.GetValue(j));
                        }
                    }
                    for (; i < length; i++)
                    {
                        Parse(exps[i]);
                        arr1[i] = GetSawDust();
                    }
                    _state.Array = arr1;
                    return;
                }
            }
            _state.Array = arr;
        }
        private void Parse(NewExpression expr)
        {
            var length = expr.Arguments.Count;
            var arr = new ISawDust[length];
            for (int i = 0; i < length; i++)
            {
                var column = expr.Arguments[i];
                var member = column as MemberExpression;
                var alias = expr.Members[i];
                Parse(column);
                if (member == null || member.Member.Name != alias.Name)
                {
                    _state.Sql = _saw.GetColumn(GetSql(), alias.Name);
                }
                arr[i] = GetSawDust();
            }
            _state.Array = arr;
        }
        private void Parse(ParameterExpression expr, MemberInfo member)
        {
            //命名参数,返回 表别名.列名
            var index = _lambda.Parameters.IndexOf(expr);
            if (_parentExpr != null)
            {
                if (index == -1)
                {
                    index = _parentExpr.Parameters.IndexOf(expr);
                }
                else
                {
                    index += _parentExpr.Parameters.Count;
                }
            }
            _state.Sql = _saw.GetColumn(GetAlias(index), member);
            _state.IsParameter = true;
            //if (_entry == WHERE)
            //{
            //    Type type = (member.MemberType == MemberTypes.Property) ? ((PropertyInfo)member).PropertyType : ((FieldInfo)member).FieldType;

            //    if (type == typeof(bool) || type == typeof(bool?))
            //    {
            //        _state.Sql = _saw.BinaryOperation(_state.Sql, BinaryOperator.Equal, AddBoolean(!_state.UnaryNot));
            //    }
            //}
        }
        private void Parse(TypeBinaryExpression expr) { Throw("不支持TypeBinaryExpression"); }
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
                    else if (_state.DustType != DustType.Sql || _state.IsParameter)
                    {
                        _state.Sql = _saw.BinaryOperation(_state.Sql, BinaryOperator.Equal, AddBoolean(!_state.UnaryNot));
                    }
                    _state.Not();
                    return;
                case ExpressionType.IsTrue:
                    Parse(expr.Operand);
                    if (_state.DustType == DustType.Boolean)
                    {
                        _state.Boolean = _state.Boolean == _state.UnaryNot;
                    }
                    else if (_state.DustType != DustType.Sql || _state.IsParameter)
                    {
                        _state.Sql = _saw.BinaryOperation(_state.Sql, BinaryOperator.Equal, AddBoolean(!_state.UnaryNot));
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
                        var type = obj.GetType();
                        if (type != expr.Type &&
                            type != Nullable.GetUnderlyingType(expr.Type))
                        {
                            _state.Object = Convert.ChangeType(obj, expr.Type);
                        }
                    }
                    return;
                case ExpressionType.Quote:
                    _state.Object = expr.Operand;
                    ExistsSubExpression = true;
                    return;
                default:
                    Throw(expr);
                    throw new NotImplementedException();
            }
        }
        private void Parse(MethodCallExpression expr)
        {
            ISawDust target;
            ISawDust[] args;
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

            if (method.ReflectedType == typeof(string))
            {
                if (ParseStringMethod(method, target, args))
                {
                    return;
                }
            }
            else if (method.ReflectedType == typeof(System.Linq.Enumerable))
            {
                if (method.Name == "Contains" && args.Length == 2)
                {
                    if (args[0].Type == DustType.Array && args[1].Type == DustType.Sql)
                    {
                        var element = (string)args[1].Value;
                        string[] array;
                        var enumerable = args[0].Value as ISawDust[];
                        if (enumerable != null)
                        {
                            array = enumerable.Select(it => it.ToSql()).ToArray();
                        }
                        else
                        {
                            array = ((IEnumerable)args[0].Value).Cast<object>().Select(GetSql).ToArray();
                        }
                        _state.Sql = _saw.ContainsOperation(_state.UnaryNot, element, array);
                        return;
                    }
                    else if (args[0].Type == DustType.Sql && args[1].Type == DustType.String)
                    {
                        if (ParseStringMethod(method, args[0], new ISawDust[] { args[1] }))
                        {
                            return;
                        }
                    }
                }
            }
            _state.Sql = _saw.ParseMethod(method, target, args);
        }

        #endregion

        #region ParseMethods


        private bool ParseStringMethod(MethodInfo method, ISawDust target, ISawDust[] args)
        {
            if (args.Length >= 1)
            {
                BinaryOperator opt;
                switch (method.Name)
                {
                    case "StartsWith":
                        opt = _state.UnaryNot ? BinaryOperator.NotStartWith : BinaryOperator.StartWith;
                        break;
                    case "EndsWith":
                        opt = _state.UnaryNot ? BinaryOperator.NotEndWith : BinaryOperator.EndWith;
                        break;
                    case "Contains":
                        opt = _state.UnaryNot ? BinaryOperator.NotContains : BinaryOperator.Contains;
                        break;
                    default:
                        return false;
                }
                _state.Sql = _saw.BinaryOperation(target.ToSql(), opt, args[0].ToSql());
                return true;
            }
            return false;
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

        /// <summary> 根据索引获取表别名 a,b,c,d,e,f...类推
        /// </summary>
        private string GetAlias(int index)
        {
            if (EnabledAlias == false) return null;
            if (index > 26)
            {
                throw new NotSupportedException("对象过多");
            }
            return (char)('a' + index) + "";
        }

        /// <summary> 将ExpressionType转为BinaryOperatorType
        /// </summary>
        private BinaryOperator ConvertBinaryOperator(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return BinaryOperator.Add;
                case ExpressionType.And:
                    return BinaryOperator.BitAnd;
                case ExpressionType.AndAlso:
                    return BinaryOperator.And;
                case ExpressionType.Divide:
                    return BinaryOperator.Divide;
                case ExpressionType.Equal:
                    return _state.UnaryNot ? BinaryOperator.NotEqual : BinaryOperator.Equal;
                case ExpressionType.NotEqual:
                    return _state.UnaryNot ? BinaryOperator.Equal : BinaryOperator.NotEqual;
                case ExpressionType.ExclusiveOr:
                    return BinaryOperator.BitXor;
                case ExpressionType.GreaterThan:
                    return BinaryOperator.GreaterThan;
                case ExpressionType.GreaterThanOrEqual:
                    return BinaryOperator.GreaterThanOrEqual;
                case ExpressionType.LeftShift:
                    return BinaryOperator.LeftShift;
                case ExpressionType.LessThan:
                    return BinaryOperator.LessThan;
                case ExpressionType.LessThanOrEqual:
                    return BinaryOperator.LessThanOrEqual;
                case ExpressionType.Modulo:
                    return BinaryOperator.Modulo;
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return BinaryOperator.Multiply;
                case ExpressionType.Or:
                    return BinaryOperator.BitOr;
                case ExpressionType.OrElse:
                    return BinaryOperator.Or;
                case ExpressionType.Power:
                    return BinaryOperator.Power;
                case ExpressionType.RightShift:
                    return BinaryOperator.RightShift;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return BinaryOperator.Subtract;
                default:
                    throw new NotSupportedException("无法解释 ExpressionType." + type.ToString());
            }
        }

        /// <summary> 返回最后一次解析的结果
        /// </summary>
        /// <returns></returns>
        private ISawDust GetSawDust()
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

        /// <summary> 判断最后一次解析结果的类型
        /// </summary>
        /// <param name="type"></param>
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
        /// <summary> 解析方法,如果全部是常量,则直接执行
        /// </summary>
        /// <param name="expr">方法表达式</param>
        /// <param name="target">方法的调用实例</param>
        /// <param name="args">方法参数</param>
        /// <returns></returns>
        private bool TryInvoke(MethodCallExpression expr, out ISawDust target, out ISawDust[] args)
        {
            ISubExpression subexpr = null;
            //判断方法调用实例,如果是null为静态方法,反之为实例方法
            if (expr.Object == null)
            {
                target = new SawDust(this, DustType.Object, null);
            }
            else
            {
                Parse(expr.Object);
                if (_state.DustType == DustType.SubExpression) subexpr = _state.SubExpression;
                target = GetSawDust();
            }

            var exprArgs = expr.Arguments;
            var length = exprArgs.Count;
            args = new ISawDust[length];
            var call = target.Type != DustType.Sql;
            for (int i = 0; i < length; i++)
            {
                Parse(exprArgs[i]);
                if (_state.DustType == DustType.Sql)
                {
                    if (call) call = false;
                    args[i] = new SawDust(this, DustType.Sql, _state.Sql);
                }
                else if (_state.DustType == DustType.Array && _state.Array is ISawDust[])
                {
                    if (call) call = false;
                    args[i] = new SawDust(this, DustType.Array, _state.Array);
                }
                else
                {
                    args[i] = new SawDust(this, _state.DustType, _state.Object);
                }
            }

            if (call)
            {
                var method = expr.Method;
                if (subexpr != null && !TypesHelper.IsChild(typeof(ISubExpression), method.ReturnType))
                {
                    _state.Sql = subexpr.GetSqlString(args);
                }
                else
                {
                    _state.Object = method.Invoke(target.Value, args.Select(it => it.Value).ToArray());
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary> 根据表达式2元操作类型,计算2个常量的值
        /// </summary>
        /// <param name="nodeType"></param>
        /// <param name="a">常量1</param>
        /// <param name="b">常量2</param>
        private void Math(ExpressionType nodeType, IConvertible a, IConvertible b)
        {
            switch (nodeType)
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
                        Throw();
                    }
                    _state.Number = (int)a >> (int)b;
                    return;
                case ExpressionType.LeftShift:
                    if (a is int == false)
                    {
                        Throw();
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
                        Throw();
                    }
                    _state.Number = (int)a ^ (int)b;
                    return;
                default:
                    Throw();
                    throw new NotImplementedException();
            }
        }

        #endregion

        #region Throw

        /// <summary> 强制抛出当前表达式的解析异常
        /// </summary
        private void Throw(Exception ex = null)
        {
            Throw(_currExpr, ex);
        }

        /// <summary> 强制抛出表达式解析异常
        /// </summary>
        private void Throw(Expression expr, Exception ex = null)
        {
            if (expr == null)
            {
                Throw("缺失表达式", ex);
            }
            Throw("无法解析表达式 => " + expr.ToString(), ex);
        }

        /// <summary> 
        /// </summary>
        /// <param name="message"></param>
        private void Throw(string message, Exception ex = null)
        {
            var info = ex == null ? "失败" : "错误(详见内部异常)";
            switch (_entry)
            {
                case WHERE:
                    message = "ToWhere " + info + ":\n" + message;
                    break;
                case ORDERBY:
                    message = "ToOrderBy " + info + ":\n" + message;
                    break;
                case SETS:
                    message = "ToSets " + info + ":\n" + message;
                    break;
                case COLUMNS:
                    message = "ToColumns " + info + ":\n" + message;
                    break;
                case VALUES:
                    message = "ToValues " + info + ":\n" + message;
                    break;
                case COLUMNS_VALUES:
                    message = "ToColumnsAndValues " + info + ":\n" + message;
                    break;
                default:
                    break;
            }
            _throw = true;
            throw new NotSupportedException(message, ex);
        }

        #endregion

        #region GetSql

        /// <summary> 解析参数中的表达式,得到结果,并转换成sql形式
        /// </summary>
        private string GetSql(Expression expr)
        {
            Parse(expr);
            return GetSql();
        }

        /// <summary> 获取任何对象的sql形式
        /// </summary>
        internal string GetSql(object obj)
        {
            if (obj == null || obj is DBNull)
            {
                return _saw.AddObject(null, Parameters);
            }
            var conv = obj as IConvertible;
            if (conv != null)
            {
                var code = conv.GetTypeCode();
                if (code >= TypeCode.SByte && code <= TypeCode.Decimal)
                {
                    return _saw.AddNumber(conv, Parameters);
                }
                else if (code == TypeCode.Boolean)
                {
                    return _saw.AddBoolean(conv.ToBoolean(null), Parameters);
                }
            }
            else if (obj is ISawDust)
            {
                return ((ISawDust)obj).ToSql();
            }
            else if (obj is SqlExpr)
            {
                return ((SqlExpr)obj).Sql;
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
                case DustType.Array:
                    var arr = _state.Array as ISawDust[];
                    if (arr != null)
                    {
                        return string.Join(", ", arr.Select(it => it.ToSql()));
                    }
                    return string.Join(", ", _state.Array.Cast<object>().Select(GetSql));
                case DustType.Undefined:
                default:
                    throw new NotSupportedException("解析结果类型未知");
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
            return string.Concat(column, " = ", value);
        }

        #endregion

        #region ToColumn

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
                if (length == 0)
                {
                    return null;
                }
                if (length == 1)
                {
                    return "*";
                }
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

        private KeyValuePair<string, string> ToColumnsAndValues(MemberInitExpression expr)
        {
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

        private KeyValuePair<string, string> ToColumnsAndValues(NewExpression expr)
        {
            var members = expr.Members;
            var length = members.Count;
            if (length == 0)
            {
                return new KeyValuePair<string, string>();
            }
            if (length == 1)
            {
                return new KeyValuePair<string, string>(_saw.GetColumn(null, members[0]), GetSql(expr.Arguments[0]));
            }
            var args = expr.Arguments;
            var columns = new string[length];
            var values = new string[length];
            for (int i = 0; i < length; i++)
            {
                columns[i] = _saw.GetColumn(null, members[i]);
                values[i] = GetSql(args[i]);
            }

            return new KeyValuePair<string, string>(string.Join(", ", columns), string.Join(", ", values));
        }

    }
}
