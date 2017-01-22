using System;
using System.Collections.Generic;
using System.Data;

namespace Searchlight.Base
{
    /// <summary>
    /// Basic entity dapper interface
    /// </summary>
    public interface IDap : IDisposable
    {
        string GetSqlTableName();
        string GetPrimaryKeyFieldName();
    }

    /// <summary>
    /// Basic Insert/Update interface
    /// </summary>
    /// <typeparam name="ENTITY"></typeparam>
    public interface IDap<ENTITY> : IDap
    {
        void Insert(ENTITY model, int userid);
        void Insert(IEnumerable<ENTITY> model, int userid);

        IEnumerable<ENTITY> GetAll();
        IEnumerable<ENTITY> GetTop(int count);
    }

    /// <summary>
    /// Prototype for a class that has a primary key
    /// </summary>
    /// <typeparam name="KEY"></typeparam>
    /// <typeparam name="ENTITY"></typeparam>
    public interface IDapWithPrimaryKey<KEY, ENTITY> : IDap<ENTITY>
    {
        void Update(ENTITY model, int userid);
        void Update(IEnumerable<ENTITY> model, int userid);

        KEY InsertWithId(ENTITY model, int userid);

        ENTITY GetByPrimaryKey(KEY id);

        void Delete(KEY id);
    }
}
