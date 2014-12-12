using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TncFastCode.Data
{
    public interface IUnitOfWork
    {
        IQueryable<T> Table<T>() where T : class;

        void DeleteAllOnSubmit<T>(IEnumerable<T> entities) where T : class;

        void DeleteOnSubmit<T>(T entity) where T : class;

        void InsertOnSubmit<T>(T entity) where T : class;

        void SubmitChanges();

        IUnitOfWork CreateNewInstance();
    }
}
