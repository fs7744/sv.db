﻿using SV.Db.Sloth.Statements;
using SV.Db.Sloth;

namespace SV.Db
{
    public class ConnectionStringProviders : ConnectionStringProvider, IConnectionFactory
    {
        private readonly IConnectionStringProvider[] providers;
        private readonly IDbEntityInfoProvider entityInfoProvider;

        public ConnectionStringProviders(IConnectionStringProvider[] providers, IDbEntityInfoProvider entityInfoProvider, IServiceProvider serviceProvider)
        {
            this.providers = providers;
            this.entityInfoProvider = entityInfoProvider;
            ConnectionFactory.Init(serviceProvider);
        }

        public override bool ContainsKey(string key)
        {
            foreach (var item in providers)
            {
                if (item.ContainsKey(key))
                    return true;
            }
            return false;
        }

        public override (string dbType, string connectionString) Get(string key)
        {
            foreach (var item in providers)
            {
                if (item.ContainsKey(key))
                    return item.Get(key);
            }
            throw new KeyNotFoundException(key);
        }

        public DbEntityInfo GetDbEntityInfoOfT<T>()
        {
            var r = DbEntityInfo<T>.Get();
            if (entityInfoProvider != null)
            {
                var rr = entityInfoProvider?.GetDbEntityInfo(r.DbKey);
                if (rr != null)
                    return rr;
            }
            return r;
        }

        public DbEntityInfo GetDbEntityInfo(string key)
        {
            return entityInfoProvider?.GetDbEntityInfo(key);
        }

        public SelectStatementBuilder<T> From<T>()
        {
            var r = new SelectStatementBuilder<T>();
            r.dbEntityInfo = GetDbEntityInfoOfT<T>();
            r.factory = this;
            r.statement.Fields = new List<FieldStatement> { new FieldStatement { Field = "*" } };
            return r;
        }

        public SelectStatementBuilder From(string key)
        {
            var r = new SelectStatementBuilder();
            r.dbEntityInfo = GetDbEntityInfo(key);
            r.factory = this;
            r.statement.Fields = new List<FieldStatement> { new FieldStatement { Field = "*" } };
            return r;
        }
    }
}