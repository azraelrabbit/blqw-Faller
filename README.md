# blqw.Faller 轻量级的表达式树解析框架,简单!灵活!强大!
## 简介  
Faller(砍树人)是一个轻量级的表达式树解析框架  
设计初衷是尽量的简单和灵活  
它不需要任何其他组件的支持  
所以可以随意嵌入到任何新项目或已有项目中  
不同数据库仅需要重新实现[ISaw](https://code.csdn.net/jy02305022/blqw-faller/tree/master/blqw.Faller/interface/ISaw.cs)接口,即可构造不同的SQL语句  
当前版本提供MsSql和Oracle的解释方式  
[MsSqlSaw](https://code.csdn.net/jy02305022/blqw-faller/tree/master/blqw.Faller/implement/MsSqlSaw.cs)  
[OracleSaw](https://code.csdn.net/jy02305022/blqw-faller/tree/master/blqw.Faller/implement/OracleSaw.cs)  
  
## 更新说明  
#### 2014.11.07
* 修正一个lambda中出现2次Not()会解析错误的问题 感谢网友@不跟随

#### 2014.11.05
* 直接引入Literacy源码,不用在额外引用项目
* 感谢网友 @不跟随 提供bug反馈
* 解决实体类中可空值类型.ToString() 报错的问题
* 解决实体类中可空值类型.Value 属性报错的问题
* 新增支持实体类可空值类型.HasValue 属性的解析,现在可以得到 xxx IS NULL 的sql语句

#### 2014.10.21
* 解决部分小bug
* 优化代码
* 引用[Literacy](https://code.csdn.net/jy02305022/blqw.Literacy)优化性能

#### 2014.10.10
* 优化ToColumnsAndValues,也支持匿名类型

#### 2014.09.17
* 修正表达式中 "".Split(char) 方法解释报错的问题  
* 小幅度优化
* 增加一个实际应用中的 Demo

#### 2014.07.23  
* 增加了对异常的处理
* 修正初始化Array可能造成的错误
* 优化初始化Array的操作
* 确定不支持初始化List表达式, 如`Where<User>(u => new List<int>() {1,2,3,4,5 }.Contains(u.ID));`将不被支持,可以使用`Where<User>(u => new []{1,2,3,4,5 }.Contains(u.ID));`代替  
* 优化表达式中如果只有一个泛型实体参数,则sql中不使用别名
* 修正SawDust类型的值嵌套可能带来的问题

#### 2014.07.22  
* 整理代码  
* 完善注释  
* 增加了解释mssql的方式  
* 已知BUG : 在表达始式中初始化List或者Array有可能无法解析,正在解决

#### 2014.07.18  
* 增加部分方法  

#### 2014.07.17  
* 完成部分代码的整理和注释工作  

#### 2014.07.16  
* 单元测试已经完成了  
* 接下来要整理代码,再添加注释  
* 继续加油吧  


## 特色  
#### 可以把DateTime.Now转换成数据库当前时间对象,如Oracle的SYSDATE  

    Where(u => u.Birthday < DateTime.Now);    // a.BIRTHDAY < SYSDATE  

#### 可以灵活处理更多的C#方法 -> 数据库函数,如  

    Where(u => u.Birthday.Day == 1);          //EXTRACT(DAY FROM a.BIRTHDAY) = 1  
    Where(u => u.Name.Trim() == "");          //ltrim(rtrim(a.NAME)) = :auto_p0  
    Where(u => string.IsNullOrEmpty(u.Name)); //a.NAME IS NULL OR a.NAME = ''  

#### 可以在表达式树中灵活插入sql表达式  

    Where(u => (SqlExpr)"rownum < 10");               //rownum < 10   
    Where(u => (SqlExpr)"rownum < 10" && u.ID > 10);  //rownum < 10 AND a.ID > 10  


##Demo代码  
```csharp
public static class UserBLL
{
    public static List<User> GetUsers(Expression<Func<User, bool>> where)
    {
        var parse = Faller.Create(where);
        var sql = parse.ToWhere(new MySaw());
        using (var conn = new SqlConnection("Data Source=.;Initial Catalog=Test;Integrated Security=True"))
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "select * from test where " + sql;
            cmd.Parameters.AddRange(parse.Parameters.ToArray());
            conn.Open();
            using (var reader = cmd.ExecuteReader())
            {
                return Convert2.ToList<User>(reader);
            }
        }
    }
}
```