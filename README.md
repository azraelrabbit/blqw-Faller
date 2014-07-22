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
2014.07.22
整理代码
增加注释
增加了解释mssql的方式
2014.07.18
增加2个方法
string ToValues(ISaw saw);
KeyValuePair<string, string> ToColumnsAndValues(ISaw saw);
2014.07.17
完成部分代码的整理和注释工作
2014.07.16  
单元测试已经完成了  
接下来要整理代码,再添加注释  
继续加油吧  


## 特色  
#### 可以把DateTime.Now转换成数据库当前时间对象,如Oracle的SYSDATE  

    Where(u => u.Birthday < DateTime.Now);    // a.BIRTHDAY < SYSDATE  

#### 可以灵活处理更多的C#方法 -> 数据库函数,如  

    Where(u => u.Birthday.Day == 1);          //EXTRACT(DAY FROM a.BIRTHDAY) = 1  
    Where(u => u.Name.Trim() == "");          //ltrim(rtrim(a.NAME)) = :auto_p0  
    Where(u => string.IsNullOrEmpty(u.Name)); //a.NAME IS NULL OR a.NAME == ''  

#### 可以在表达式树中灵活插入sql表达式  

    Where(u => (SqlExpr)"rownum < 10");               //rownum < 10   
    Where(u => (SqlExpr)"rownum < 10" && u.ID > 10);  //rownum < 10 AND a.ID > 10  
