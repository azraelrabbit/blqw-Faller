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
    public class Faller : IFaller
    {
        #region 字段
        private const int WHERE = 1;
        private const int ORDERBY = 2;
        private const int SET = 3;
        private const int COLUMNS = 4;
        private static readonly MemberInfo _TimeNow = typeof(DateTime).GetProperty("Now");
        private LambdaExpression _lambda;
        private ISaw _saw;
        private bool _unaryNot;
        private string _sql;
        private object _object;
        private int _layer;
        private int _entry;
        #endregion

        public Faller(LambdaExpression expr)
        {
            _lambda = expr;
            Parameters = new List<DbParameter>();
        }

        public string ToWhere(ISaw saw)
        {
            _entry = WHERE;
            _saw = saw;
            Initialization();
            return ParseToSql(_lambda.Body);
        }

        public string ToOrderBy(ISaw saw, bool asc)
        {
            _entry = ORDERBY;
            _saw = saw;
            Initialization();
            var expr = _lambda.Body;
            var newExpr = expr as NewExpression;
            if (newExpr != null)
            {
                return string.Join(", ", newExpr.Arguments.Select(it => _saw.OrderBy(ParseToSql(it), asc)));
            }
            var arrExpr = expr as NewArrayExpression;
            if (arrExpr != null)
            {
                return string.Join(", ", arrExpr.Expressions.Select(it => _saw.OrderBy(ParseToSql(it), asc)));
            }
            return _saw.OrderBy(ParseToSql(expr), asc);
        }

        public string ToSet(ISaw saw)
        {
            _entry = SET;
            _saw = saw;
            Initialization();
            var expr = _lambda.Body as MemberInitExpression;
            if (expr == null)
            {
                Throw(_lambda.Body);
            }
            if (expr.Bindings.Count == 0)
            {
                return "";
            }
            if (expr.Bindings.Count == 1)
            {
                return ToSet(expr.Bindings[0]);
            }
            return string.Join(", ", expr.Bindings.Select(ToSet));
        }

        public string ToColumns(ISaw saw)
        {
            _entry = COLUMNS;
            _saw = saw;
            Initialization();
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
                return ParseToSql(member);
            }
            else
            {
                return _saw.GetColumn(ParseToSql(column), alias.Name);
            }
        }

        private string ToColumnAll()
        {
            var expr = _lambda.Body as ConstantExpression;
            if (expr == null)
            {
                Throw(_lambda.Body);
            }
            if (Parse(expr) == DustType.Sql)
            {
                return _sql;
            }
            if (_object == null)
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
                return GetValueSql(_object);
            }
        }


        public ICollection<DbParameter> Parameters { get; private set; }

        #region Parse

        private DustType ParseArrayIndex(BinaryExpression expr)
        {
            if (Parse(expr.Left) != DustType.Array)
            {
                Throw(expr);
            }
            var arr = (dynamic)_object;
            if (Parse(expr.Right) != DustType.Number)
            {
                Throw(expr);
            }
            _object = arr[Convert.ToInt32(_object)];
            return Result(_object);
        }

        private DustType Parse(BinaryExpression expr)
        {
            //得到 expr.Right 部分的返回值
            var right = new SawDust(this, Parse(expr.Right), _sql ?? _object);
            //如果右边是布尔值常量
            if (right.Type == DustType.Boolean)
            {
                if ((expr.NodeType == ExpressionType.Equal) != (bool)_object)
                {
                    _unaryNot = !_unaryNot;
                }
                var unaryExpr = UnaryExpression.IsTrue(expr.Left);
                return Parse(unaryExpr);
            }
            else if (right.Type == DustType.Undefined)
            {
                Throw(expr);
            }
            // 解析 expr.Left 部分
            var left = Parse(expr.Left);
            var opt = ConvertBinaryOperator(expr.NodeType);
            switch (left)
            {
                case DustType.Sql:
                    _sql = _saw.BinaryOperator(_sql, opt, right.ToSql());
                    return DustType.Sql;
                case DustType.Number:
                    //如果左右都是 Number常量
                    if (right.Type == DustType.Number)
                    {
                        //直接计算结果
                        return Math(expr, ((IConvertible)right.Value), ((IConvertible)right.Value));
                    }
                    _sql = _saw.BinaryOperator(AddNumber(_object), opt, right.ToSql());
                    return DustType.Sql;
                case DustType.Boolean:
                    //如果左边是布尔值常量,虽然这种写法很操蛋
                    if ((expr.NodeType == ExpressionType.Equal) != (bool)_object)
                    {
                        _unaryNot = !_unaryNot;
                    }
                    var unaryExpr = UnaryExpression.IsTrue(expr.Right);
                    return Parse(unaryExpr);
                case DustType.Object:
                    _sql = _saw.BinaryOperator(AddObject(_object), opt, right.ToSql());
                    return DustType.Sql;
                case DustType.Undefined:
                case DustType.Array:
                default:
                    Throw(expr);
                    break;
            }
            throw new NotImplementedException();
        }
        private DustType Parse(ConditionalExpression expr) { throw new NotImplementedException(); }
        private DustType Parse(ConstantExpression expr)
        {
            return Result(expr.Value);
        }
        private DustType Parse(ListInitExpression expr) { throw new NotImplementedException(); }
        private DustType Parse(MemberExpression expr)
        {
            var para = expr.Expression as ParameterExpression;
            if (para != null)
            {
                //命名参数,返回 表别名.列名
                var index = _lambda.Parameters.IndexOf(para);
                _sql = _saw.GetColumn(GetAlias(index), expr.Member);
                if (expr.Type == typeof(bool) && _entry == WHERE)
                {
                    _sql = _saw.BinaryOperator(_sql, BinaryOperatorType.Equal, AddBoolean(true));
                }
                return DustType.Sql;
            }
            else if (object.ReferenceEquals(expr.Member, _TimeNow))
            {
                //如果是DateTime.Now 返回数据库的当前时间表达式
                _sql = _saw.AddTimeNow(Parameters);
                //如果数据库没有相应的表达式,则使用C#中的当前时间
                if (_sql == null)
                {
                    _object = DateTime.Now;
                    return DustType.Object;
                }
                return DustType.Sql;
            }
            else
            {
                object target = null;
                // expr.Expression 不等于 null 说明是实例成员,否则是静态成员
                if (expr.Expression != null)
                {
                    var dusttype = Parse(expr.Expression);
                    if (dusttype == DustType.Sql) //实例成员,必然可以得到一个对象
                    {
                        if (expr.Member is PropertyInfo)
                        {
                            var method = ((PropertyInfo)expr.Member).GetGetMethod();
                            if (method == null)
                            {
                                Throw(expr);
                            }
                            _sql = _saw.CallMethod(method, new SawDust(this, dusttype, _sql), new SawDust[0]);
                            return DustType.Sql;
                        }
                    }
                    else
                    {
                        target = _object;
                        if (target == null)
                            Throw(expr);
                    }
                }
                //判断 Member 是属性还是字段,使用反射,得到值
                var p = expr.Member as PropertyInfo;
                if (p != null)
                {
                    return Result(p.GetValue(target, null));
                }
                else //不是属性,只能是字段
                {
                    return Result(((FieldInfo)expr.Member).GetValue(target));
                }
            }
        }
        private DustType Parse(MemberInitExpression expr) { throw new NotImplementedException(); }
        private DustType Parse(NewArrayExpression expr)
        {
            var exps = expr.Expressions;
            var length = expr.Expressions.Count;
            var arr = Array.CreateInstance(expr.Type.GetElementType(), length);
            for (int i = 0; i < length; i++)
            {
                arr.SetValue(new SawDust(this, Parse(exps[i]), _sql ?? _object), i);
            }
            _object = arr;
            return DustType.Array;
        }
        private DustType Parse(NewExpression expr) { throw new NotImplementedException(); }
        private DustType Parse(ParameterExpression expr) { throw new NotImplementedException(); }
        private DustType Parse(TypeBinaryExpression expr) { throw new NotImplementedException(); }
        private DustType Parse(UnaryExpression expr)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.Not:
                case ExpressionType.IsFalse:
                    _unaryNot = !_unaryNot;
                    var type = Parse(expr.Operand);
                    try
                    {
                        if (type == DustType.Boolean)
                        {
                            _object = (bool)_object != _unaryNot;
                            return DustType.Boolean;
                        }
                        else if (type != DustType.Sql)
                        {
                            _sql = _saw.BinaryOperator(GetValueSql(_object), BinaryOperatorType.NotEqual, AddBoolean(_unaryNot));
                        }
                        return DustType.Sql;
                    }
                    finally
                    {
                        _unaryNot = !_unaryNot;
                    }
                case ExpressionType.IsTrue:
                    type = Parse(expr.Operand);
                    if (type == DustType.Boolean)
                    {
                        _object = (bool)_object == _unaryNot;
                        return DustType.Boolean;
                    }
                    else if (type != DustType.Sql)
                    {
                        _sql = _saw.BinaryOperator(GetValueSql(_object), BinaryOperatorType.Equal, AddBoolean(_unaryNot));
                    }
                    return DustType.Sql;
                case ExpressionType.Convert:
                    type = Parse(expr.Operand);
                    if (type != DustType.Sql)
                    {
                        if (!object.ReferenceEquals(_object.GetType(), expr.Type))
                        {
                            if (_object is string && expr.Type == typeof(SqlExpr))
                            {
                                _sql = (string)_object;
                                _object = null;
                                return DustType.Sql;
                            }
                            else
                            {
                                _object = Convert.ChangeType(_object, expr.Type);
                                return Result(_object);
                            }
                        }
                    }
                    return type;
                default:
                    break;
            }

            throw new NotImplementedException();
        }
        private DustType Parse(MethodCallExpression expr)
        {
            SawDust target;
            SawDust[] args;
            //尝试直接调用,如果成功 返回true 如果失败,返回已解析的对象
            if (TryInvoke(expr, out target, out args))
            {
                return Result(_object);
            }

            var method = expr.Method;
            if (method.ReflectedType == typeof(object) && expr.Object != null)
            {
                method = expr.Object.Type.GetMethod(expr.Method.Name, expr.Method.GetParameters().Select(it => it.ParameterType).ToArray());
            }

            if (object.ReferenceEquals(method.ReflectedType, typeof(string)))
            {
                _sql = ParseStringMethod(method, target, args);
                return DustType.Sql;
            }
            else if (object.ReferenceEquals(method.ReflectedType, typeof(System.Linq.Enumerable)))
            {
                if (method.Name == "Contains"
                    && args.Length == 2)
                {
                    if (args[0].Type == DustType.Array
                    && args[1].Type == DustType.Sql)
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
                            array = ((IEnumerable)args[0].Value).Cast<object>().Select(GetValueSql).ToArray();
                        }
                        _sql = _saw.Contains(_unaryNot, element, array);
                        return DustType.Sql;
                    }
                    else if (args[0].Type == DustType.Sql
                    && args[1].Type == DustType.String)
                    {
                        _sql = ParseStringMethod(method, args[0], new SawDust[] { args[1] });
                        return DustType.Sql;
                    }
                }
            }

            _sql = _saw.CallMethod(method, target, args);
            return DustType.Sql;
        }

        #endregion

        #region MyRegion

        private bool TryInvoke(MethodCallExpression expr, out SawDust target, out SawDust[] args)
        {
            //判断方法对象,如果是null为静态方法,反之为实例方法
            if (expr.Object == null)
            {
                target = new SawDust(this, DustType.Object, null);
            }
            else
            {
                target = new SawDust(this, Parse(expr.Object), _sql ?? _object);
            }

            var exprArgs = expr.Arguments;
            var length = exprArgs.Count;
            args = new SawDust[length];
            var call = target.Type != DustType.Sql;
            for (int i = 0; i < length; i++)
            {
                var type = Parse(exprArgs[i]);
                if (type == DustType.Sql)
                {
                    if (call) call = false;
                    args[i] = new SawDust(this, DustType.Sql, _sql ?? _object);
                }
                else
                {
                    args[i] = new SawDust(this, type, _object);
                }
            }
            //target 一直保持null 说明所有参数都有实际值,直接执行方法,获得方法返回值
            if (call)
            {
                _object = expr.Method.Invoke(target.Value, args.Select(it => it.Value).ToArray());
                return true;
            }
            else
            {
                return false;
            }
        }


        private DustType Math(Expression expr, IConvertible a, IConvertible b)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.Add:
                    unchecked { _object = a.ToDecimal(null) + b.ToDecimal(null); }
                    return DustType.Number;
                case ExpressionType.AddChecked:
                    checked { _object = a.ToDecimal(null) + b.ToDecimal(null); }
                    return DustType.Number;
                case ExpressionType.Subtract:
                    unchecked { _object = a.ToDecimal(null) - b.ToDecimal(null); }
                    return DustType.Number;
                case ExpressionType.SubtractChecked:
                    checked { _object = a.ToDecimal(null) - b.ToDecimal(null); }
                    return DustType.Number;
                case ExpressionType.Multiply:
                    unchecked { _object = a.ToDecimal(null) * b.ToDecimal(null); }
                    return DustType.Number;
                case ExpressionType.MultiplyChecked:
                    checked { _object = a.ToDecimal(null) * b.ToDecimal(null); }
                    return DustType.Number;
                case ExpressionType.Divide:
                    _object = a.ToDecimal(null) / b.ToDecimal(null);
                    return DustType.Number;
                case ExpressionType.Modulo:
                    _object = a.ToDecimal(null) % b.ToDecimal(null);
                    return DustType.Number;
                case ExpressionType.And:
                    _object = (long)a & (long)b;
                    return DustType.Number;
                case ExpressionType.Or:
                    _object = (long)a | (long)b;
                    return DustType.Number;
                case ExpressionType.LessThan:
                    _object = ((IComparable)a).CompareTo((IComparable)b) < 0;
                    return DustType.Boolean;
                case ExpressionType.LessThanOrEqual:
                    _object = ((IComparable)a).CompareTo((IComparable)b) <= 0;
                    return DustType.Boolean;
                case ExpressionType.GreaterThan:
                    _object = ((IComparable)a).CompareTo((IComparable)b) > 0;
                    return DustType.Boolean;
                case ExpressionType.GreaterThanOrEqual:
                    _object = ((IComparable)a).CompareTo((IComparable)b) >= 0;
                    return DustType.Boolean;
                case ExpressionType.Equal:
                    _object = ((IComparable)a).CompareTo((IComparable)b) == 0;
                    return DustType.Boolean;
                case ExpressionType.NotEqual:
                    _object = ((IComparable)a).CompareTo((IComparable)b) != 0;
                    return DustType.Boolean;
                case ExpressionType.RightShift:
                    if (a is int == false)
                    {
                        Throw(expr);
                    }
                    _object = (int)a >> (int)b;
                    return DustType.Number;
                case ExpressionType.LeftShift:
                    if (a is int == false)
                    {
                        Throw(expr);
                    }
                    _object = (int)a << (int)b;
                    return DustType.Number;
                case ExpressionType.ExclusiveOr:
                    if (a is int == false)
                    {
                        Throw(expr);
                    }
                    _object = (int)a ^ (int)b;
                    return DustType.Boolean;
                default:
                    break;
            }
            Throw(expr);
            throw new NotImplementedException();
        }

        #endregion

        #region ParseMethods


        private string ParseStringMethod(MethodInfo method, SawDust target, SawDust[] args)
        {
            if (args.Length >= 1)
            {
                switch (method.Name)
                {
                    case "StartsWith":
                        if (_unaryNot)
                            return _saw.BinaryOperator(target.ToSql(), BinaryOperatorType.NotStartWith, args[0].ToSql());
                        return _saw.BinaryOperator(target.ToSql(), BinaryOperatorType.StartWith, args[0].ToSql());
                    case "EndsWith":
                        if (_unaryNot)
                            return _saw.BinaryOperator(target.ToSql(), BinaryOperatorType.NotEndWith, args[0].ToSql());
                        return _saw.BinaryOperator(target.ToSql(), BinaryOperatorType.EndWith, args[0].ToSql());
                    case "Contains":
                        if (_unaryNot)
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

        internal string AddNumber(object value)
        {
            return _saw.AddNumber((IConvertible)value, Parameters);
        }

        internal string AddBoolean(object value)
        {
            return _saw.AddBoolean((bool)value, Parameters);
        }

        private void Initialization()
        {
            _unaryNot = false;
            _layer = 0;
        }

        private string GetAlias(int index)
        {
            if (index > 26)
            {
                throw new NotSupportedException("对象过多");
            }
            return (char)('a' + index) + "";
        }

        private string ParseToSql(Expression expr)
        {
            switch (Parse(expr))
            {
                case DustType.Undefined:
                    Throw(expr);
                    break;
                case DustType.Sql:
                    return _sql;
                case DustType.Number:
                    return _saw.AddNumber((IConvertible)_object, Parameters);
                case DustType.Array:
                    break;
                case DustType.Boolean:
                    return _saw.AddBoolean((bool)_object, Parameters);
                case DustType.Object:
                case DustType.DateTime:
                case DustType.String:
                case DustType.Binary:
                    return _saw.AddObject(_object, Parameters);
                default:
                    break;
            }
            Throw(expr);
            throw new NotImplementedException();
        }

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
                    return _unaryNot ? BinaryOperatorType.NotEqual : BinaryOperatorType.Equal;
                case ExpressionType.NotEqual:
                    return _unaryNot ? BinaryOperatorType.Equal : BinaryOperatorType.NotEqual;
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

        private DustType Parse(Expression expr)
        {
            if (_layer++ > 100)
            {
                throw new OutOfMemoryException("表达式过于复杂");
            }
            _sql = null;
            _object = null;
            if (expr == null)
                return DustType.Undefined;
            if (expr.CanReduce)
            {
                expr = expr.Reduce();
            }
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
                    return this.Parse((UnaryExpression)expr);
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
                    return this.Parse((BinaryExpression)expr);
                case ExpressionType.TypeIs:
                    return this.Parse((TypeBinaryExpression)expr);
                case ExpressionType.Conditional:
                    return this.Parse((ConditionalExpression)expr);
                case ExpressionType.Constant:
                    return this.Parse((ConstantExpression)expr);
                case ExpressionType.Parameter:
                    return this.Parse((ParameterExpression)expr);
                case ExpressionType.MemberAccess:
                    return this.Parse((MemberExpression)expr);
                case ExpressionType.Call:
                    return this.Parse((MethodCallExpression)expr);
                case ExpressionType.Lambda:
                    return this.Parse((LambdaExpression)expr);
                case ExpressionType.New:
                    return this.Parse((NewExpression)expr);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return this.Parse((NewArrayExpression)expr);
                case ExpressionType.Invoke:
                    return this.Parse((InvocationExpression)expr);
                case ExpressionType.MemberInit:
                    return this.Parse((MemberInitExpression)expr);
                case ExpressionType.ListInit:
                    return this.Parse((ListInitExpression)expr);
                case ExpressionType.ArrayIndex:
                    return this.ParseArrayIndex((BinaryExpression)expr);
                default:
                    Throw(expr);
                    return DustType.Undefined;
            }
        }

        private void Throw(Expression expr)
        {
            if (expr == null)
            {
                throw new NotSupportedException("缺失表达式");
            }
            throw new NotSupportedException("无法解释表达式 => " + expr.ToString());
        }

        private string GetValueSql(object obj)
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

        private DustType Result(object obj)
        {
            _object = obj;
            if (_object == null || _object is DBNull)
            {
                return DustType.Object;
            }
            var conv = _object as IConvertible;
            if (conv != null)
            {
                var code = conv.GetTypeCode();
                switch (conv.GetTypeCode())
                {
                    case TypeCode.Boolean:
                        return DustType.Boolean;
                    case TypeCode.String:
                    case TypeCode.Char:
                        return DustType.String;
                    case TypeCode.DateTime:
                        return DustType.DateTime;
                    default:
                        break;
                }
                if (code >= TypeCode.SByte && code <= TypeCode.Decimal)
                {
                    return DustType.Number;
                }
            }
            if (_object is byte[] || _object is System.IO.Stream)
            {
                return DustType.Binary;
            }
            if (_object is IEnumerable)
            {
                return DustType.Array;
            }
            if (_object is SqlExpr)
            {
                _sql = ((SqlExpr)_object).Sql;
                _object = null;
                return DustType.Sql;
            }
            return DustType.Object;
        }

        #endregion

        private string ToSet(MemberBinding binding)
        {
            MemberAssignment m = binding as MemberAssignment;
            if (m == null)
            {
                throw new NotSupportedException("无法解释表达式 => " + m.ToString());
            }
            var column = _saw.GetColumn(null, m.Member);
            var value = ParseToSql(m.Expression);
            return _saw.UpdateSet(column, value);
        }

        private IEnumerable<KeyValuePair<string, string>> ToSets(MemberInitExpression expr)
        {
            foreach (MemberAssignment it in expr.Bindings)
            {
                MemberAssignment m = it as MemberAssignment;
                if (m == null)
                {
                    throw new NotSupportedException("无法解释表达式 => " + expr.Bindings[0].ToString());
                }
                var column = _saw.GetColumn(null, m.Member);
                var value = ParseToSql(m.Expression);
                yield return new KeyValuePair<string, string>(column, value);
            }
        }

    }
}
