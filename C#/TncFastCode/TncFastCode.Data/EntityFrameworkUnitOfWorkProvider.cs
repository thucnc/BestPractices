using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;


namespace TncFastCode.Data
{
    public class EntityFrameworkUnitOfWorkProvider<T> : IUnitOfWork where T : DbContext
    {
        private readonly T _db;
        private readonly string _connectionString;
        private readonly int _commandTimeout;

        public EntityFrameworkUnitOfWorkProvider(string connectionString)
            : this(connectionString, -1)
        {
            // Nothing more to do
        }

        public EntityFrameworkUnitOfWorkProvider(string connectionString, int commandTimeout)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("connection string must have a value");
            }
            _connectionString = connectionString;
            _commandTimeout = commandTimeout;

            var t = typeof(T);
            _db = (T)Activator.CreateInstance(t, new object[] { connectionString });

            ((IObjectContextAdapter)_db).ObjectContext.CommandTimeout =
                commandTimeout < 0 ? _db.Database.Connection.ConnectionTimeout : commandTimeout;
        }

        public IQueryable<TEntity> Table<TEntity>() where TEntity : class
        {
            return _db.Set<TEntity>();
        }

        public void DeleteAllOnSubmit<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
            entities.ToList().ForEach(entity => _db.Set<TEntity>().Remove(entity));
        }

        public void DeleteOnSubmit<TEntity>(TEntity entity) where TEntity : class
        {
            _db.Set<TEntity>().Remove(entity);
        }

        public void InsertOnSubmit<TEntity>(TEntity entity) where TEntity : class
        {
            _db.Set<TEntity>().Add(entity);
        }

        public void SubmitChanges()
        {
            _db.SaveChanges();
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        public IUnitOfWork CreateNewInstance()
        {
            return new EntityFrameworkUnitOfWorkProvider<T>(_connectionString, _commandTimeout);
        }
    }
}
