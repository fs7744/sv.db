﻿using SV.Db.Sloth.Statements;
using System.Text;

namespace SV.Db.Sloth
{
    public static partial class From
    {
        public static IDictionary<string, string> ParseToQueryParams(this SelectStatement statement)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (statement != null)
            {
                if (statement.Where != null && statement.Where.Condition != null)
                {
                    StringBuilder sb = new();
                    ParseConditionStatementToQuery(sb, statement.Where.Condition);
                    dict.Add("Where", sb.ToString());
                }
                dict.Add("Rows", statement.Rows.ToString());
                if (statement.Offset > 0)
                {
                    dict.Add("Offset", statement.Offset.ToString());
                }
                if (statement.OrderBy.IsNotNullOrEmpty())
                {
                    var order = statement.OrderBy;
                    StringBuilder sb = new();
                    ParseFields(order, sb);
                    dict.Add("OrderBy", sb.ToString());
                }
                if (statement.GroupBy.IsNotNullOrEmpty())
                {
                    var order = statement.GroupBy;
                    StringBuilder sb = new();
                    ParseFields(order, sb);
                    dict.Add("GroupBy", sb.ToString());
                }
                if (statement.Fields.IsNotNullOrEmpty())
                {
                    var fs = statement.Fields;
                    if (statement.HasTotalCount)
                    {
                        dict.Add("TotalCount", "true");
                    }

                    if (fs?.Any(i => i is FieldStatement f && f.Field.Equals("*", StringComparison.OrdinalIgnoreCase)) != true)
                    {
                        StringBuilder sb = new();
                        ParseFields(fs.Where(i => i is FieldStatement && i.Field != "*"), sb);
                        dict.Add("Fields", sb.ToString());
                    }
                }
                else
                {
                    dict.Add("NoRows", "true");
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

                case "not-null":
                    sb.Append("!= null");
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
            if (v is JsonFieldStatement js)
            {
                sb.Append("json(");
                sb.Append(js.Field);
                sb.Append(",");
                sb.Append("'");
                sb.Append(js.Path.Replace("'", "\\'"));
                sb.Append("'");
                if (!string.IsNullOrWhiteSpace(js.As))
                {
                    sb.Append(",");
                    sb.Append(js.As);
                }
                sb.Append(")");
                if (v is IOrderByField order)
                {
                    sb.Append(" ");
                    sb.Append(Enums<OrderByDirection>.GetName(order.Direction));
                }
            }
            else if (v is GroupByFuncFieldStatement g)
            {
                sb.Append(g.Func);
                sb.Append("(");
                sb.Append(g.Field);
                if (!string.IsNullOrWhiteSpace(g.As))
                {
                    sb.Append(",");
                    sb.Append(g.As);
                }
                sb.Append(")");
            }
            else if (v is FieldStatement f)
            {
                sb.Append(f.Field);
                if (v is IOrderByField order)
                {
                    sb.Append(" ");
                    sb.Append(Enums<OrderByDirection>.GetName(order.Direction));
                }
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

        public static void ParseFields(IEnumerable<FieldStatement> fields, StringBuilder sb)
        {
            var notFirst = false;
            foreach (var item in fields)
            {
                if (notFirst)
                {
                    sb.Append(",");
                }
                else
                {
                    notFirst = true;
                }
                ParseBuildValueStatementToQuery(item, sb);
            }
        }
    }
}