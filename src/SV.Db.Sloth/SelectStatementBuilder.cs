using SV.Db.Sloth.Statements;

namespace SV.Db.Sloth
{
    public class SelectStatementBuilder<T> : SelectStatementBuilder
    {
    }

    public class SelectStatementBuilder
    {
        internal readonly SelectStatement statement = new SelectStatement() { Limit = new LimitStatement() { Rows = 10 } };
        internal DbEntityInfo dbEntityInfo;
        internal IConnectionFactory factory;
        internal SelectStatementOptions options;

        public SelectStatement Build(SelectStatementOptions? options)
        {
            this.options = options ?? SelectStatementOptions.Default;
            if (NeedCheck())
            {
                statement.Visit(CheckStatement);
            }
            return statement;
        }

        private bool NeedCheck()
        {
            return !options.AllowNotFoundFields || !options.AllowNonStrictCondition;
        }

        private void CheckStatement(Statement statement)
        {
            if (!options.AllowNotFoundFields)
            {
                var fs = dbEntityInfo.SelectFields;
                if (statement is FieldStatement field && field is not FuncCallerStatement && !field.Name.Equals("*") && (fs.IsNullOrEmpty() || !fs.ContainsKey(field.Name)))
                {
                    throw new KeyNotFoundException($"Field {field.Name} not found");
                }

                if (statement is OrderByFieldStatement fieldOrderBy && (fs.IsNullOrEmpty() || !fs.ContainsKey(fieldOrderBy.Name)))
                {
                    throw new KeyNotFoundException($"Field {fieldOrderBy.Name} not found");
                }

                if (statement is FieldValueStatement fieldValue && (fs.IsNullOrEmpty() || !fs.ContainsKey(fieldValue.Field)))
                {
                    throw new KeyNotFoundException($"Field {fieldValue.Field} not found");
                }
            }
            if (!options.AllowNonStrictCondition)
            {
                if (statement is OperaterStatement os)
                {
                    if (os.Left is FieldValueStatement l && os.Right is FieldValueStatement r)
                        throw new NotSupportedException($"{os.Operater} not support two fields: {l.Field},{r.Field}");
                    if (os.Left is not FieldValueStatement && os.Right is not FieldValueStatement)
                        throw new NotSupportedException($"{os.Operater} must has one field");
                }
            }
        }
    }

    public record class SelectStatementOptions
    {
        public static readonly SelectStatementOptions Default = new SelectStatementOptions();

        public bool AllowNotFoundFields { get; init; } = false;
        public bool AllowNonStrictCondition { get; init; } = false;
    }
}