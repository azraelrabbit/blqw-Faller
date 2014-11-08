using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Oracle.ManagedDataAccess.Client;
using System.Reflection;

namespace blqw
{
    public class OracleSaw : BaseSaw
    {
        public static OracleSaw Instance = new OracleSaw();

        protected OracleSaw() : base(OracleClientFactory.Instance) { }

        private static HashSet<string> KeyWords = InitKeyWords();

        private static HashSet<string> InitKeyWords()
        {
            return new HashSet<string>("ALL,ALTER,AND,ANY,ARRAY,AS,ASC,AT,AUTHID,AVG,BEGIN,BETWEEN,BINARY_INTEGER,BODY,BOOLEAN,BULK,BY,CHAR,CHAR_BASE,CHECK,CLOSE,CLUSTER,COLLECT,COMMENT,COMMIT,COMPRESS,CONNECT,CONSTANT,CREATE,CURRENT,CURRVAL,CURSOR,DATE,DAY,DECLARE,DECIMAL,DEFAULT,DELETE,DESC,DISTINCT,DO,DROP,ELSE,ELSIF,END,EXCEPTION,EXCLUSIVE,EXECUTE,EXISTS,EXIT,EXTENDS,FALSE,FETCH,FLOAT,FOR,FORALL,FROM,FUNCTION,GOTO,GROUP,HAVING,HEAP,HOUR,IF,IMMEDIATE,IN,INDEX,INDICATOR,INSERT,INTEGER,INTERFACE,INTERSECT,INTERVAL,INTO,IS,ISOLATION,JAVA,LEVEL,LIKE,LIMITED,LOCK,LONG,LOOP,MAX,MIN,MINUS,MINUTE,MLSLABEL,MOD,MODE,MONTH,NATURAL,NATURALN,NEW,NEXTVAL,NOCOPY,NOT,NOWAIT,NULL,NUMBER,NUMBER_BASE,OCIROWID,OF,ON,OPAQUE,OPEN,OPERATOR,OPTION,OR,ORDER,ORGANIZATION,OTHERS,OUT,PACKAGE,PARTITION,PCTFREE,PLS_INTEGER,POSITIVE,POSITIVEN,PRAGMA,PRIOR,PRIVATE,PROCEDURE,PUBLIC,RAISE,RANGE,RAW,REAL,RECORD,REF,RELEASE,RETURN,REVERSE,ROLLBACK,ROW,ROWID,ROWNUM,ROWTYPE,SAVEPOINT,SECOND,SELECT,SEPARATE,SET,SHARE,SMALLINT,SPACE,SQL,SQLCODE,SQLERRM,START,STDDEV,SUBTYPE,SUCCESSFUL,SUM,SYNONYM,SYSDATE,TABLE,THEN,TIME,TIMESTAMP,TO,TRIGGER,TRUE,TYPE,UID,UNION,UNIQUE,UPDATE,USE,USER,VALIDATE,VALUES,VARCHAR,VARCHAR2,VARIANCE,VIEW,WHEN,WHENEVER,WHERE,WHILE,WITH,WORK,WRITE,YEAR,ZONE".Split(','));
        }
        
        protected override string ParameterPreFix
        {
            get { return ":"; }
        }

        protected override string TimeNow
        {
            get { return "SYSDATE"; }
        }

        protected override string LikeOperation(string val1, string val2, LikeOperator opt)
        {
            switch (opt)
            {
                case LikeOperator.Contains:
                    return string.Concat(val1, " LIKE '%' || ", val2, " || '%'");
                case LikeOperator.StartWith:
                    return string.Concat(val1, " LIKE ", val2, " || '%'");
                case LikeOperator.EndWith:
                    return string.Concat(val1, " LIKE '%' || ", val2);
                case LikeOperator.NotContains:
                    return string.Concat(val1, " NOT LIKE '%' || ", val2, " || '%'");
                case LikeOperator.NotStartWith:
                    return string.Concat(val1, " NOT LIKE ", val2, " || '%'");
                case LikeOperator.NotEndWith:
                    return string.Concat(val1, " NOT LIKE '%' || ", val2);
                default:
                    throw new ArgumentOutOfRangeException("opt");
            }
        }

        protected override string BitOperation(string val1, string val2, BitOperator opt)
        {
            switch (opt)
            {
                case BitOperator.And:
                    return string.Concat("BITAND(", val1, ", ", val2, ")");
                case BitOperator.Or:
                    return string.Concat("((", val1, " + ", val2, ") - BITAND(", val1, ", ", val2, "))");
                case BitOperator.Xor:
                    return string.Concat("((", val1, " + ", val2, ") - BITAND(", val1, ", ", val2, ") * 2)");
                default:
                    throw new ArgumentOutOfRangeException("opt");
            }
        }

        protected override string StringTrim(string target, string arg)
        {
            if (arg == null)
            {
                return string.Concat("trim(", target, ")");
            }
            else
            {
                return string.Concat("ltrim(rtrim(", target, ", ", arg, "), ", arg, ")");
            }
        }

        protected override string StringTrimEnd(string target, string arg)
        {
            if (arg == null)
            {
                return string.Concat("rtrim(", target, ")");
            }
            else
            {
                return string.Concat("rtrim(", target, ", ", arg, ")");
            }
        }

        protected override string StringTrimStart(string target, string arg)
        {
            if (arg == null)
            {
                return string.Concat("ltrim(", target, ")");
            }
            else
            {
                return string.Concat("ltrim(", target, ", ", arg, ")");
            }
        }

        protected override string StringLength(string target)
        {
            return string.Concat("LENGTH(", target, ")");
        }

        protected override string SrtingToNumber(string target)
        {
            return string.Concat("TO_NUMBER(", target, ")");
        }

        protected override string AliasSeparator { get { return " "; } }

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
                    if (format == null)
                    {
                        return string.Concat("TO_CHAR(", target, ")");
                    }
                    return string.Concat("TO_CHAR(", target, ",", format, ")");
                case DustType.Array:
                    break;
                case DustType.Boolean:
                    break;
                case DustType.DateTime:
                    if (format == null)
                    {
                        return string.Concat("TO_CHAR(", target, ",'yyyy-mm-dd HH:mi:ss')");
                    }
                    format = format.Replace("m", "mi").Replace("mimi", "mi");
                    return string.Concat("TO_CHAR(", target, ",", format, ")");
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
                    return string.Concat("EXTRACT(YEAR FROM ", datetime, ")");
                case DateTimeField.Month:
                    return string.Concat("EXTRACT(MONTH FROM ", datetime, ")");
                case DateTimeField.Day:
                    return string.Concat("EXTRACT(DAY FROM ", datetime, ")");
                case DateTimeField.Hour:
                    return string.Concat("EXTRACT(HOUR FROM ", datetime, ")");
                case DateTimeField.Minute:
                    return string.Concat("EXTRACT(MINUTE FROM ", datetime, ")");
                case DateTimeField.Second:
                    return string.Concat("EXTRACT(SECOND FROM ", datetime, ")");
                case DateTimeField.Week:
                default:
                    throw new ArgumentOutOfRangeException("field");
            }
        }

        public override string WarpName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            name = name.ToUpper();
            if (name.Contains(".") || KeyWords.Contains(name))
            {
                return string.Concat("\"", name, "\"");
            }
            return name;
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
