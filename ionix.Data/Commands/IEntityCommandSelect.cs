﻿namespace ionix.Data
{
    using Utils.Extensions;
    using Utils.Reflection;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;

    public interface IEntityCommandSelect//Select ler Entity üzerinden otomatik yazılan Select ifadeleri. Query ise custom için.
    {
        bool ConvertType { get; set; }

        TEntity SelectById<TEntity>(IEntityMetaDataProvider provider, params object[] keys);

        TEntity SelectSingle<TEntity>(IEntityMetaDataProvider provider, SqlQuery extendedQuery);

        IList<TEntity> Select<TEntity>(IEntityMetaDataProvider provider, SqlQuery extendedQuery);

        TEntity QuerySingle<TEntity>(IEntityMetaDataProvider provider, SqlQuery query);//Property adı kolondan farklı olan durumlar için IEntityMetaDataProvider provider eklendi.
        Tuple<TEntity1, TEntity2> QuerySingle<TEntity1, TEntity2>(IEntityMetaDataProvider provider, SqlQuery query,MapBy by);
        Tuple<TEntity1, TEntity2, TEntity3> QuerySingle<TEntity1, TEntity2, TEntity3>(IEntityMetaDataProvider provider, SqlQuery query, MapBy by);
        Tuple<TEntity1, TEntity2, TEntity3, TEntity4> QuerySingle<TEntity1, TEntity2, TEntity3, TEntity4>(IEntityMetaDataProvider provider, SqlQuery query, MapBy by);
        Tuple<TEntity1, TEntity2, TEntity3, TEntity4, TEntity5> QuerySingle<TEntity1, TEntity2, TEntity3, TEntity4, TEntity5>(IEntityMetaDataProvider provider, SqlQuery query,MapBy by);
        Tuple<TEntity1, TEntity2, TEntity3, TEntity4, TEntity5, TEntity6> QuerySingle<TEntity1, TEntity2, TEntity3, TEntity4, TEntity5, TEntity6>(IEntityMetaDataProvider provider,SqlQuery query, MapBy by);
        Tuple<TEntity1, TEntity2, TEntity3, TEntity4, TEntity5, TEntity6, TEntity7> QuerySingle<TEntity1, TEntity2, TEntity3, TEntity4, TEntity5, TEntity6, TEntity7>(IEntityMetaDataProvider provider, SqlQuery query, MapBy by);


        IList<TEntity> Query<TEntity>(IEntityMetaDataProvider provider, SqlQuery query);
        IList<Tuple<TEntity1, TEntity2>> Query<TEntity1, TEntity2>(IEntityMetaDataProvider provider, SqlQuery query, MapBy by);
        IList<Tuple<TEntity1, TEntity2, TEntity3>> Query<TEntity1, TEntity2, TEntity3>(IEntityMetaDataProvider provider, SqlQuery query, MapBy by);
        IList<Tuple<TEntity1, TEntity2, TEntity3, TEntity4>> Query<TEntity1, TEntity2, TEntity3, TEntity4>(IEntityMetaDataProvider provider, SqlQuery query, MapBy by);
        IList<Tuple<TEntity1, TEntity2, TEntity3, TEntity4, TEntity5>> Query<TEntity1, TEntity2, TEntity3, TEntity4, TEntity5>(IEntityMetaDataProvider provider, SqlQuery query, MapBy by);
        IList<Tuple<TEntity1, TEntity2, TEntity3, TEntity4, TEntity5, TEntity6>> Query<TEntity1, TEntity2, TEntity3, TEntity4, TEntity5, TEntity6>(IEntityMetaDataProvider provider, SqlQuery query, MapBy by);
        IList<Tuple<TEntity1, TEntity2, TEntity3, TEntity4, TEntity5, TEntity6, TEntity7>> Query<TEntity1, TEntity2, TEntity3, TEntity4, TEntity5, TEntity6, TEntity7>(IEntityMetaDataProvider provider, SqlQuery query, MapBy by);
    }

    public partial class EntityCommandSelect : IEntityCommandSelect
    {
        public EntityCommandSelect(IDbAccess dataAccess, char parameterPrefix)
        {
            if (null == dataAccess)
                throw new ArgumentNullException(nameof(dataAccess));

            this.DataAccess = dataAccess;
            this.ParameterPrefix = parameterPrefix;
        }

        public IDbAccess DataAccess { get; }

        public char ParameterPrefix { get; }

        public bool ConvertType { get; set; }

        private enum MapType
        {
            Select,
            Query
        }

        private void Map<TEntity>(TEntity entity, IEntityMetaData metaData, IDataReader dr, MapType mapType)
        {
            switch (mapType)
            {
                case MapType.Select:
                    foreach (PropertyMetaData md in metaData.Properties)
                    {
                        string columnName = md.Schema.ColumnName;
                        PropertyInfo pi = md.Property;
                        if (pi.GetSetMethod() != null)
                        {
                            object dbValue = dr[columnName];
                            if (dbValue == DBNull.Value)
                            {
                                pi.SetValue(entity, null, null);
                            }
                            else
                            {
                                if (this.ConvertType)
                                    pi.SetValueSafely(entity, dbValue);
                                else
                                    pi.SetValue(entity, dbValue, null);
                            }
                        }
                    }
                    break;
                case MapType.Query:
                    int fieldCount = dr.FieldCount;
                    for (int j = 0; j < fieldCount; ++j)
                    {
                        string columnName = dr.GetName(j);
                        PropertyMetaData md = metaData[columnName];// metaData.Properties.FirstOrDefault(p => String.Equals(columnName, p.Schema.ColumnName));
                        if (null != md)
                        {
                            PropertyInfo pi = md.Property;
                            if (pi.GetSetMethod() != null)
                            {
                                object dbValue = dr[j];
                                if (dbValue == DBNull.Value)
                                {
                                    pi.SetValue(entity, null, null);
                                }
                                else
                                {
                                    if (this.ConvertType)
                                        pi.SetValueSafely(entity, dbValue);
                                    else
                                        pi.SetValue(entity, dbValue, null);
                                }
                            }
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException(mapType.ToString());
            }
        }

        private TEntity ReadEntity<TEntity>(IEntityMetaData metaData, SqlQuery query, MapType mapType)
        {
            IDataReader dr = null;
            try
            {
                dr = this.DataAccess.CreateDataReader(query, CommandBehavior.SingleRow);

                if (dr.Read())
                {
                    TEntity entity = Activator.CreateInstance<TEntity>();
                    this.Map<TEntity>(entity, metaData, dr, mapType);
                    return entity;
                }
            }
            finally
            {
                if (dr != null) dr.Dispose();
            }

            return default(TEntity);
        }

        private IList<TEntity> ReadEntityList<TEntity>(IEntityMetaData metaData, SqlQuery query, MapType mapType)
        {
            List<TEntity> ret = new List<TEntity>();

            IDataReader dr = null;
            try
            {
                dr = this.DataAccess.CreateDataReader(query, CommandBehavior.Default);
                //ret.Capacity = dr.FieldCount; ??? ne bu
                while (dr.Read())
                {
                    TEntity entity = Activator.CreateInstance<TEntity>();
                    this.Map<TEntity>(entity, metaData, dr, mapType);
                    ret.Add(entity);
                }
            }
            finally
            {
                if (dr != null) dr.Dispose();
            }

            return ret;
        }

        public virtual TEntity SelectById<TEntity>(IEntityMetaDataProvider provider, params object[] keys)
        {
            if (keys.IsEmptyList())
                throw new ArgumentNullException(nameof(keys));

            IEntityMetaData metaData = provider.EnsureCreateEntityMetaData<TEntity>();

            SqlQueryBuilderSelect builder = new SqlQueryBuilderSelect(metaData);
            SqlQuery query = builder.ToQuery();//Select sql yazıldı.

            FilterCriteriaList filters = new FilterCriteriaList(this.ParameterPrefix);

            IList<PropertyMetaData> keySchemas = metaData.OfKeys(true);//Order a göre geldiği için böyle.
            if (keySchemas.Count != keys.Length)
                throw new InvalidOperationException("Keys and Valus count does not match");

            int index = -1;
            foreach (PropertyMetaData keyProperty in keySchemas)
            {
                string parameterName = metaData.GetParameterName(keyProperty, 0);
                filters.Add(keyProperty.Schema.ColumnName, parameterName, ConditionOperator.Equals, keys[++index]);
            }

            query.Combine(filters.ToQuery());//Where ifadesi oluşturuldu. Eğer ki

            return this.ReadEntity<TEntity>(metaData, query, MapType.Select);
        }

        public virtual TEntity SelectSingle<TEntity>(IEntityMetaDataProvider provider, SqlQuery extendedQuery)
        {
            IEntityMetaData metaData = provider.EnsureCreateEntityMetaData<TEntity>();

            SqlQueryBuilderSelect builder = new SqlQueryBuilderSelect(metaData);
            SqlQuery query = builder.ToQuery();
            if (null != extendedQuery)
                query.Combine(extendedQuery);

            return this.ReadEntity<TEntity>(metaData, query, MapType.Select);
        }

        public IList<TEntity> Select<TEntity>(IEntityMetaDataProvider provider, SqlQuery extendedQuery)
        {
            if (null == provider)
                throw new ArgumentNullException(nameof(provider));

            IEntityMetaData metaData = provider.EnsureCreateEntityMetaData<TEntity>();

            SqlQueryBuilderSelect builder = new SqlQueryBuilderSelect(metaData);
            SqlQuery query = builder.ToQuery();
            if (null != extendedQuery)
                query.Combine(extendedQuery);

            return this.ReadEntityList<TEntity>(metaData, query, MapType.Select);
        }
    
        public virtual TEntity QuerySingle<TEntity>(IEntityMetaDataProvider provider, SqlQuery query)
        {
            if (null == query)
                throw new ArgumentNullException(nameof(query));

            IEntityMetaData metaData = provider.EnsureCreateEntityMetaData<TEntity>();

            return this.ReadEntity<TEntity>(metaData, query, MapType.Query);
        }

        public virtual IList<TEntity> Query<TEntity>(IEntityMetaDataProvider provider, SqlQuery query)
        {
            if (null == query)
                throw new ArgumentNullException(nameof(query));

            IEntityMetaData metaData = provider.EnsureCreateEntityMetaData<TEntity>();

            return this.ReadEntityList<TEntity>(metaData, query, MapType.Query);
        }
    }
}
