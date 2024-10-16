using SV.Db.Sloth.Statements;

namespace SV.Db.Sloth
{
    public class SelectStatementBuilder<T> : SelectStatementBuilder
    {
    }

    public class SelectStatementBuilder
    {
        internal readonly SelectStatement statement = new SelectStatement();
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
            if (this.options.Visiter != null)
            {
                statement.Visit(this.options.Visiter);
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
                if (statement is FieldStatement field && !field.Field.Equals("*") && (fs.IsNullOrEmpty() || !fs.ContainsKey(field.Field)))
                {
                    throw new KeyNotFoundException($"Field {field.Field} not found");
                }

                if (statement is JsonFieldStatement js)
                {
                    if (!dbEntityInfo.Columns.TryGetValue(js.Field, out var column)
                        || !column.IsJson)
                    {
                        throw new NotSupportedException($"Field {js.Field} not support json");
                    }
                }
            }
            if (!options.AllowNonStrictCondition)
            {
                if (statement is OperaterStatement os)
                {
                    if (os.Left is FieldStatement l && os.Right is FieldStatement r)
                        throw new NotSupportedException($"{os.Operater} not support two fields: {l.Field},{r.Field}");
                    if (os.Left is not FieldStatement && os.Right is not FieldStatement)
                        throw new NotSupportedException($"{os.Operater} must has one field");
                }
            }
        }
    }
}