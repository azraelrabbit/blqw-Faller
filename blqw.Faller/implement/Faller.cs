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
        private static readonly MemberInfo _TimeNow = typeof(DateTime).GetProperty("Now");
        private LambdaExpression _lambda;
        private ISaw _provider;
        private bool _unaryNot;
        private string _sql;
        private object _object;
        private int _layer;
        #endregion

        public Faller(LambdaExpression expr)
        {
            _lambda = expr;
            Parameters = new List<DbParameter>();
        }

        public string ToWhere(ISaw provider)
        {
            _provider = provider;
            Initialization();
            return ParseToSql(_lambda.Body);
        }

        public string ToOrderBy(ISaw provider, bool asc)
        {
            _provider = provider;
            _provider = provider;
            Initialization();
            var expr = _lambda.Body;
            var newExpr = expr as NewExpression;
            if (newExpr != null)
            {
                return ToOrderBy(newExpr.Arguments, asc);
            }
            var arrExpr = expr as NewArrayExpression;
            if (arrExpr != null)
            {
                return ToOrderBy(arrExpr.Expressions, asc);
            }
            return ToOrderBy(expr, asc);
        }

        public string ToSet(ISaw provider)
        {
            throw new NotImplementedException();
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
                    _sql = _provider.BinaryOperator(_sql, opt, right.ToSql());
                    return DustType.Sql;
                case DustType.Number:
                    //如果左右都是 Number常量
                    if (right.Type == DustType.Number)
                    {
                        //直接计算结果
                        return Math(expr, ((IConvertible)right.Value), ((IConvertible)right.Value));
                    }
                    _sql = _provider.BinaryOperator(AddNumber(_object), opt, right.ToSql());
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
                    _sql = _provider.BinaryOperator(AddObject(_object), opt, right.ToSql());
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
                _sql = _provider.GetColumnName(GetAlias(index), expr.Member);
                return DustType.Sql;
            }
            else if (object.ReferenceEquals(expr.Member, _TimeNow))
            {
                //如果是DateTime.Now 返回数据库的当前时间表达式
                _sql = _provider.AddTimeNow(Parameters);
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
                    Parse(expr.Expression); //实例成员,必然可以得到一个对象
                    target = _object;
                    if (target == null) Throw(expr);
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
                            _sql = _provider.BinaryOperator(GetValueSql(_object), BinaryOperatorType.NotEqual, AddBoolean(_unaryNot));
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
                        _sql = _provider.BinaryOperator(GetValueSql(_object), BinaryOperatorType.Equal, AddBoolean(_unaryNot));
                    }
                    return DustType.Sql;
                case ExpressionType.Convert:
                    type = Parse(expr.Operand);
                    if (type != DustType.Sql)
                    {
                        if (!object.ReferenceEquals(_object.GetType(), expr.Type))
                        {
                            _object = Convert.ChangeType(_object, expr.Type);
                            return Result(_object);
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

            if (object.ReferenceEquals(expr.Method.ReflectedType, typeof(string)))
            {
                _sql = ParseStringMethod(expr.Method, target, args);
                return DustType.Sql;
            }
            else if (object.ReferenceEquals(expr.Method.ReflectedType, typeof(System.Linq.Enumerable)))
            {
                if (expr.Method.Name == "Contains"
                    && args.Length == 2
                    && args[0].Type == DustType.Array
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
                    _sql = _provider.Contains(_unaryNot, element, array);
                    return DustType.Sql;
                }
            }

            _sql = _provider.CallMethod(expr.Method, target, args);
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
                            return _provider.BinaryOperator(target.ToSql(), BinaryOperatorType.NotStartWith, args[0].ToSql());
                        return _provider.BinaryOperator(target.ToSql(), BinaryOperatorType.StartWith, args[0].ToSql());
                    case "EndWith":
                        if (_unaryNot)
                            return _provider.BinaryOperator(target.ToSql(), BinaryOperatorType.NotEndWith, args[0].ToSql());
                        return _provider.BinaryOperator(target.ToSql(), BinaryOperatorType.EndWith, args[0].ToSql());
                    case "Contains":
                        if (_unaryNot)
                            return _provider.BinaryOperator(target.ToSql(), BinaryOperatorType.NotContains, args[0].ToSql());
                        return _provider.BinaryOperator(target.ToSql(), BinaryOperatorType.Contains, args[0].ToSql());
                    default:
                        break;
                }
            }
            return _provider.CallMethod(method, target, args);
        }






        #endregion

        #region Base

        internal string AddObject(object value)
        {
            return _provider.AddObject(value, Parameters);
        }

        internal string AddNumber(object value)
        {
            return _provider.AddNumber((IConvertible)value, Parameters);
        }

        internal string AddBoolean(object value)
        {
            return _provider.AddBoolean((bool)value, Parameters);
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
                throw new NotSupportedException();
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
                    return _provider.AddNumber((IConvertible)_object, Parameters);
                case DustType.Array:
                    break;
                case DustType.Boolean:
                    return _provider.AddBoolean((bool)_object, Parameters);
                case DustType.Object:
                    return _provider.AddObject(_object, Parameters);
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
                return _provider.AddObject(null, Parameters);
            }
            else if (obj is bool)
            {
                return _provider.AddBoolean((bool)obj, Parameters);
            }
            var conv = obj as IConvertible;
            if (conv != null)
            {
                var code = conv.GetTypeCode();
                if (code >= TypeCode.SByte && code <= TypeCode.Decimal)
                {
                    return _provider.AddNumber(conv, Parameters);
                }
            }
            return _provider.AddObject(obj, Parameters);
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
            return DustType.Object;
        }

        #endregion


        private string ToOrderBy(ICollection<Expression> exps, bool asc)
        {
            var length = exps.Count;
            string[] sqls = new string[length];
            var i = 0;
            foreach (var expr in exps)
            {
                if (Parse(expr) == DustType.Sql)
                {
                    sqls[i++] = _sql;
                }
                else
                {
                    Throw(expr);
                }
            }
            return _provider.OrderBy(sqls, asc);
        }

        private string ToOrderBy(Expression expr, bool asc)
        {
            if (Parse(expr) != DustType.Sql)
            {
                Throw(expr);
            }
            return _provider.OrderBy(_sql, asc);
        }

    }
}
