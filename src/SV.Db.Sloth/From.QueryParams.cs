using SV.Db.Sloth.Statements;
using System.Text;

namespace SV.Db.Sloth
{
    public static partial class From
    {
        public static IDictionary<string, string> ParseToQueryParams(this SelectStatement statement)
        {
            var dict = new Dictionary<string, string>();
            if (statement != null)
            {
                if (statement.Where != null && statement.Where.Condition != null)
                {
                    StringBuilder sb = new();
                    ParseConditionStatementToQuery(sb, statement.Where.Condition);
                    dict.Add("Where", sb.ToString());
                }
            }
            return dict;
        }

        public static void ParseConditionStatementToQuery(StringBuilder sb, ConditionStatement condition)
        {
            if (condition is OperaterStatement os)
            {
                ParseOperaterStatementToQuery(sb, os);
            }
            else if (condition is UnaryOperaterStatement uo)
            {
                ParseUnaryOperaterStatementToQuery(sb, uo);
            }
            else if (condition is InOperaterStatement io)
            {
                ParseInOperaterStatementToQuery(sb, io);
            }
            else if (condition is ConditionsStatement conditions)
            {
                if (conditions.Condition == Condition.And)
                {
                    sb.Append(" (");
                    ParseConditionStatementToQuery(sb, conditions.Left);
                    sb.Append(" and ");
                    ParseConditionStatementToQuery(sb, conditions.Right);
                    sb.Append(") ");
                }
                else
                {
                    sb.Append(" (");
                    ParseConditionStatementToQuery(sb, conditions.Left);
                    sb.Append(" or ");
                    ParseConditionStatementToQuery(sb, conditions.Right);
                    sb.Append(") ");
                }
            }
        }

        private static void ParseInOperaterStatementToQuery(StringBuilder sb, InOperaterStatement io)
        {
            sb.Append(' ');
            ParseBuildValueStatementToQuery(io.Left, sb);
            sb.Append(" in (");
            ParseArrayValueStatementToQuery(io.Right, sb);
            sb.Append(") ");
        }

        private static void ParseArrayValueStatementToQuery(ArrayValueStatement array, StringBuilder sb)
        {
            if (array is StringArrayValueStatement s)
            {
                for (var i = 0; i < s.Value.Count; i++)
                {
                    if (i > 0)
                        sb.Append(',');

                    sb.Append("'");
                    sb.Append(s.Value[i].Replace("'", "\\'"));
                    sb.Append("'");
                }
            }
            else if (array is BooleanArrayValueStatement b)
            {
                for (var i = 0; i < b.Value.Count; i++)
                {
                    var bb = b.Value[i];
                    if (i > 0)
                        sb.Append(',');
                    sb.Append(bb ? "true" : "false");
                }
            }
            else if (array is NumberArrayValueStatement n)
            {
                for (var i = 0; i < n.Value.Count; i++)
                {
                    var bb = n.Value[i];
                    if (i > 0)
                        sb.Append(',');
                    sb.Append(bb);
                }
            }
        }

        private static void ParseUnaryOperaterStatementToQuery(StringBuilder sb, UnaryOperaterStatement uo)
        {
            if (uo.Operater == "not")
            {
                sb.Append(" not (");
                ParseConditionStatementToQuery(sb, uo.Right);
                sb.Append(") ");
            }
        }

        private static void ParseOperaterStatementToQuery(StringBuilder sb, OperaterStatement os)
        {
            sb.Append(' ');
            ParseBuildValueStatementToQuery(os.Left, sb);
            sb.Append(' ');
            switch (os.Operater)
            {
                case "is-null":
                    sb.Append("= null");
                    break;

                case "like":
                    sb.Append("like ");
                    var rf = os.Right as StringValueStatement;
                    sb.Append($"'%{rf.Value}%'");
                    break;

                case "prefix-like":
                    sb.Append("like ");
                    var lrf = os.Right as StringValueStatement;
                    sb.Append($"'{lrf.Value}%'");
                    break;

                case "suffix-like":
                    sb.Append("like ");
                    var srf = os.Right as StringValueStatement;
                    sb.Append($"'%{srf.Value}'");
                    break;

                default:
                    sb.Append(os.Operater);
                    sb.Append(' ');
                    ParseBuildValueStatementToQuery(os.Right, sb);
                    break;
            }

            sb.Append(' ');
        }

        private static void ParseBuildValueStatementToQuery(ValueStatement v, StringBuilder sb)
        {
            if (v is FieldValueStatement f)
            {
                sb.Append(f.Field);
            }
            else if (v is StringValueStatement s)
            {
                sb.Append("'");
                sb.Append(s.Value.Replace("'", "\\'"));
                sb.Append("'");
            }
            else if (v is BooleanValueStatement b)
            {
                sb.Append(b.Value ? "true" : "false");
            }
            else if (v is NumberValueStatement n)
            {
                sb.Append(n.Value.ToString());
            }
        }
    }
}