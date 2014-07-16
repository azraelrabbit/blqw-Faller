这是一个轻量级的表达式树解析框架  

暂时只完成了Where和Oracle部分  
而且没有测试  
而且没有注释  
慢慢完善吧


####部分特色  
* 可以把DateTime.Now转换成数据库当前时间对象,如Oracle的SYSDATE
```csharp
  Where(u => u.Birthday < DateTime.Now);    // a.BIRTHDAY < SYSDATE
```
* 可以灵活处理更多的C#方法 -> 数据库函数,如
```csharp
  Where(u => u.Birthday.Day == 1);          //EXTRACT(DAY FROM a.BIRTHDAY) = 1
  Where(u => u.Name.Trim() == "");          //ltrim(rtrim(a.NAME)) = :auto_p0
  Where(u => string.IsNullOrEmpty(u.Name)); //a.NAME IS NULL OR a.NAME == ''
```
* 可以在表达式树中灵活插入sql表达式
```csharp
  Where(u => (SqlExpr)"rownum < 10");               //rownum < 10
  Where(u => (SqlExpr)"rownum < 10" && u.ID > 10);  //rownum < 10 AND a.ID > 10
```