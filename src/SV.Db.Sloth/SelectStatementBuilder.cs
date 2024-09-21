﻿using SV.Db.Sloth.Statements;

namespace SV.Db.Sloth
{
    public class SelectStatementBuilder<T>
    {
        internal readonly SelectStatement statement = new SelectStatement() { Limit = new LimitStatement() { Rows = 10 } };

        public (string key, SelectStatement statement) Build() => (null, statement);
    }
}