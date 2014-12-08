
namespace blqw
{
    public interface ISubExpression
    {
        System.Linq.Expressions.LambdaExpression ParentExpression { get; set; }
        string GetSqlString(ISawDust[] args);
    }
}
