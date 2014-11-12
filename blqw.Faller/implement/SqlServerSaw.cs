using System;
using System.Collections.Generic;
using System.Data.Common;

namespace blqw
{
    public class SqlServerSaw : BaseSaw
    {
        public readonly static SqlServerSaw Instance = new SqlServerSaw();

        protected SqlServerSaw() : base(System.Data.SqlClient.SqlClientFactory.Instance) { }

        private static HashSet<string> KeyWords = InitKeyWords();

        private static HashSet<string> InitKeyWords()
        {
            return new HashSet<string>("ABSOLUTE,ACTION,ADA,ADD,ADMIN,AFTER,AGGREGATE,ALIAS,ALL,ALLOCATE,ALTER,AND,ANY,ARE,ARRAY,AS,ASC,ASSERTION,AT,AUTHORIZATION,AVG,BACKUP,BEFORE,BEGIN,BETWEEN,BINARY,BIT,BIT_LENGTH,BLOB,BOOLEAN,BOTH,BREADTH,BREAK,BROWSE,BULK,BY,CALL,CASCADE,CASCADED,CASE,CAST,CATALOG,CHAR,CHARACTER,CHARACTER_LENGTH,CHAR_LENGTH,CHECK,CHECKPOINT,CLASS,CLOB,CLOSE,CLUSTERED,COALESCE,COLLATE,COLLATION,COLUMN,COMMIT,COMPLETION,COMPUTE,CONNECT,CONNECTION,CONSTRAINT,CONSTRAINTS,CONSTRUCTOR,CONTAINS,CONTAINSTABLE,CONTINUE,CONVERT,CORRESPONDING,COUNT,CREATE,CROSS,CUBE,CURRENT,CURRENT_DATE,CURRENT_PATH,CURRENT_ROLE,CURRENT_TIME,CURRENT_TIMESTAMP,CURRENT_USER,CURSOR,CYCLE,DATA,DATABASE,DATE,DAY,DBCC,DEALLOCATE,DEC,DECIMAL,DECLARE,DEFAULT,DEFERRABLE,DEFERRED,DELETE,DENY,DEPTH,DEREF,DESC,DESCRIBE,DESCRIPTOR,DESTROY,DESTRUCTOR,DETERMINISTIC,DIAGNOSTICS,DICTIONARY,DISCONNECT,DISK,DISTINCT,DISTRIBUTED,DOMAIN,DOUBLE,DROP,DUMMY,DUMP,DYNAMIC,EACH,ELSE,END,END-EXEC,EQUALS,ERRLVL,ESCAPE,EVERY,EXCEPT,EXCEPTION,EXEC,EXECUTE,EXISTS,EXIT,EXTERNAL,EXTRACT,FALSE,FETCH,FILE,FILLFACTOR,FIRST,FLASE,FLOAT,FOR,FOREIGN,FORTRAN,FOUND,FREE,FREETEXT,FREETEXTTABLE,FROM,FULL,FUNCTION,GENERAL,GET,GLOBAL,GO,GOTO,GRANT,GROUP,GROUPING,HAVING,HOLDLOCK,HOST,HOUR,IDENTITY,IDENTITYCOL,IDENTITY_INSERT,IF,IGNORE,IMMEDIATE,IN,INCLUDE,INDEX,INDICATOR,INITIALIZE,INITIALLY,INNER,INOUT,INPUT,INSENSITIVE,INSERT,INT,INTEGER,INTERSECT,INTERVAL,INTO,IS,ISOLATION,ITERATE,JOIN,KEY,KILL,LANGUAGE,LARGE,LAST,LATERAL,LEADING,LEFT,LESS,LEVEL,LIKE,LIMIT,LINENO,LOAD,LOCAL,LOCALTIME,LOCALTIMESTAMP,LOCATOR,LOWER,MAP,MATCH,MAX,MIN,MINUTE,MODIFIES,MODIFY,MODULE,MONTH,NAMES,NATIONAL,NATURAL,NCHAR,NCLOB,NEW,NEXT,NO,NOCHECK,NONCLUSTERED,NONE,NOT,NULL,NULLIF,NUMERIC,OBJECT,OCTET_LENGTH,OF,OFF,OFFSETS,OLD,ON,ONLY,OPEN,OPENDATASOURCE,OPENQUERY,OPENROWSET,OPENXML,OPERATION,OPTION,OR,ORDER,ORDINALITY,OUT,OUTER,OUTPUT,OVER,OVERLAPS,PAD,PARAMETER,PARAMETERS,PARTIAL,PASCAL,PATH,PERCENT,PLAN,POSITION,POSTFIX,PRECISION,PREFIX,PREORDER,PREPARE,PRESERVE,PRIMARY,PRINT,PRIOR,PRIVILEGES,PROC,PROCEDURE,PUBLIC,RAISERROR,READ,READS,READTEXT,REAL,RECONFIGURE,RECURSIVE,REF,REFERENCES,REFERENCING,RELATIVE,REPLICATION,RESTORE,RESTRICT,RESULT,RETURN,RETURNS,REVOKE,RIGHT,ROLE,ROLLBACK,ROLLUP,ROUTINE,ROW,ROWCOUNT,ROWGUIDCOL,ROWS,RULE,SAVE,SAVEPOINT,SCHEMA,SCOPE,SCROLL,SEARCH,SECOND,SECTION,SELECT,SEQUENCE,SESSION,SESSION_USER,SET,SETS,SETUSER,SHUTDOWN,SIZE,SMALLINT,SOME,SPACE,SPECIFIC,SPECIFICTYPE,SQL,SQLCA,SQLCODE,SQLERROR,SQLEXCEPTION,SQLSTATE,SQLWARNING,START,STATE,STATEMENT,STATIC,STATISTICS,STRUCTURE,SUBSTRING,SUM,SYSTEM_USER,TABLE,TEMPORARY,TERMINATE,TEXTSIZE,THAN,THEN,TIME,TIMESTAMP,TIMEZONE_HOUR,TIMEZONE_MINUTE,TO,TOP,TRAILING,TRAN,TRANSACTION,TRANSLATE,TRANSLATION,TREAT,TRIGGER,TRIM,TRUE,TRUNCATE,TSEQUAL,UNDER,UNION,UNIQUE,UNKNOWN,UNNEST,UPDATE,UPDATETEXT,UPPER,USAGE,USE,USER,USING,VALUE,VALUES,VARCHAR,VARIABLE,VARYING,VIEW,WAITFOR,WHEN,WHENEVER,WHERE,WHILE,WITH,WITHOUT,WORK,WRITE,WRITETEXT,YEAR,ZONE".Split(','), StringComparer.OrdinalIgnoreCase);
        }

        public override string WarpName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (name.Contains(".") || KeyWords.Contains(name))
            {
                return string.Concat("[", name, "]");
            }
            return name;
        }

        protected override string ParameterPreFix { get { return "@"; } }

        protected override string TimeNow { get { return "GETDATE()"; } }

        protected override string LikeOperation(string val1, string val2, LikeOperator opt)
        {
            switch (opt)
            {
                case LikeOperator.Contains:
                    return string.Concat(val1, " LIKE '%' + ", val2, " + '%'");
                case LikeOperator.StartWith:
                    return string.Concat(val1, " LIKE ", val2, " + '%'");
                case LikeOperator.EndWith:
                    return string.Concat(val1, " LIKE '%' + ", val2);
                case LikeOperator.NotContains:
                    return string.Concat(val1, " NOT LIKE '%' + ", val2, " + '%'");
                case LikeOperator.NotStartWith:
                    return string.Concat(val1, " NOT LIKE ", val2, " + '%'");
                case LikeOperator.NotEndWith:
                    return string.Concat(val1, " NOT LIKE '%' + ", val2);
                default:
                    throw new ArgumentOutOfRangeException("opt");
            }
        }

        protected override string BitOperation(string val1, string val2, BitOperator opt)
        {
            switch (opt)
            {
                case BitOperator.And:
                    return string.Concat(val1, " & ", val2);
                case BitOperator.Or:
                    return string.Concat(val1, " | ", val2);
                case BitOperator.Xor:
                    return string.Concat(val1, " ^ ", val2);
                default:
                    throw new ArgumentOutOfRangeException("opt");
            }
        }

        protected override string StringTrim(string target, string arg)
        {
            if (arg != null)
            {
                throw new NotSupportedException("不支持参数");
            }
            return string.Concat("LTRIM(RTRIM(", target, "))");
        }

        protected override string StringTrimEnd(string target, string arg)
        {
            if (arg != null)
            {
                throw new NotSupportedException("不支持参数");
            }
            return string.Concat("RTRIM(", target, ")");
        }

        protected override string StringTrimStart(string target, string arg)
        {
            if (arg != null)
            {
                throw new NotSupportedException("不支持参数");
            }
            return string.Concat("LTRIM(", target, ")");
        }

        protected override string StringLength(string target)
        {
            return string.Concat("LEN(", target, ")");
        }

        protected override string SrtingToNumber(string target)
        {
            return string.Concat("CAST(", target, " as decimal)");
        }

        protected override string ObjectToString(DustType type, string target, string format)
        {
            switch (type)
            {
                case DustType.Undefined:
                    break;
                case DustType.Sql:
                    break;
                case DustType.Object:
                    break;
                case DustType.Number:
                    if (format != null)
                    {
                        throw new NotSupportedException("不支持参数");
                    }
                    return string.Concat("CAST(", target, " as NVARCHAR)");
                case DustType.Array:
                    break;
                case DustType.Boolean:
                    break;
                case DustType.DateTime:
                    switch (format)
                    {
                        case "'HH:mm'":
                            return string.Concat("CONVERT(VARCHAR(5),", target, ",114)");
                        case "'HH:mm:ss'":
                            return string.Concat("CONVERT(VARCHAR(8),", target, ",114)");
                        case "'HH:mm:ss.fff'":
                            return string.Concat("CONVERT(VARCHAR(12),", target, ",114)");
                        case "'yyyy-MM-dd'":
                            return string.Concat("CONVERT(VARCHAR(10),", target, ",21)");
                        case "'yyyy-MM-dd HH:mm'":
                            return string.Concat("CONVERT(VARCHAR(16),", target, ",21)");
                        case "'yyyy-MM-dd HH:mm:ss'":
                            return string.Concat("CONVERT(VARCHAR(19),", target, ",21)");
                        case "'yyyy-MM-dd HH:mm:ss.fff'":
                            return string.Concat("CONVERT(VARCHAR(23),", target, ",21)");
                        case "'yy/MM/dd'":
                            return string.Concat("CONVERT(VARCHAR(8),", target, ",111)");
                        case "'yyMMdd'":
                            return string.Concat("CONVERT(VARCHAR(6),", target, ",112)");
                        default:
                            break;
                    }
                    throw new NotSupportedException("不支持当前参数");
                case DustType.Binary:
                    break;
                case DustType.String:
                    break;
                default:
                    break;
            }
            return base.ObjectToString(type, target, format);
        }

        protected override string DateTimeToField(string datetime, DateTimeField field)
        {
            switch (field)
            {
                case DateTimeField.Year:
                    return string.Concat("DATEPART(YYYY, ", datetime, ")");
                case DateTimeField.Month:
                    return string.Concat("DATEPART(mm, ", datetime, ")");
                case DateTimeField.Day:
                    return string.Concat("DATEPART(dd, ", datetime, ")");
                case DateTimeField.Hour:
                    return string.Concat("DATEPART(hh, ", datetime, ")");
                case DateTimeField.Minute:
                    return string.Concat("DATEPART(mi, ", datetime, ")");
                case DateTimeField.Second:
                    return string.Concat("DATEPART(ss, ", datetime, ")");
                case DateTimeField.Week:
                    return string.Concat("(DATEPART(w, ", datetime, ") - 1)");
                default:
                    throw new ArgumentOutOfRangeException("field");
            }
        }

        public override string AddBoolean(bool value, ICollection<DbParameter> parameters)
        {
            return value ? "1" : "0";
        }

        public override string AddNumber(IConvertible number, ICollection<DbParameter> parameters)
        {
            if (number == null)
            {
                throw new ArgumentNullException("number");
            }
            return number.ToString();
        }
    }
}
