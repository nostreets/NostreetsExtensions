﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using NostreetsExtensions.Extend.Basic;
using NostreetsExtensions.Interfaces;
using NostreetsExtensions.Utilities;

namespace NostreetsExtensions.Extend.Data
{
    public static class DataExtensions
    {
        public static string[] GetColumnNames(this ISqlExecutor reader, Func<SqlConnection> dataSouce, string tableName)
        {
            KeyValuePair<string, Type>[] result = GetSchema(reader, dataSouce, tableName);
            return result.Select(a => a.Key).ToArray();
        }

        public static string[] GetColumnNames(this IDataReader reader)
        {
            return reader.GetSchemaTable().Rows.Cast<DataRow>().Select(c => c["ColumnName"].ToString()).ToArray();
        }

        /// <summary>
        /// Gets the columns.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static List<string> GetColumns(this DbContext dbContext, Type type)
        {
            string statment = String.Format("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME like N'{0}s'", type.Name);
            DbRawSqlQuery<string> result = dbContext.Database.SqlQuery<string>(statment);
            return result.ToList();
        }

        public static Type[] GetColumnTypes(this ISqlExecutor reader, Func<SqlConnection> dataSouce, string tableName)
        {
            KeyValuePair<string, Type>[] result = GetSchema(reader, dataSouce, tableName);
            return result.Select(a => a.Value).ToArray();
        }

        public static Type[] GetColumnTypes(this IDataReader reader)
        {
            List<Type> result = new List<Type>();
            string[] columns = reader.GetColumnNames();
            for (int i = 0; i < columns.Length; i++)
            {
                result.Add(reader.GetValue(i).GetType());
            }

            return result.ToArray();
        }

        public static double GetDouble(this DataRow dr, string column_name)
        {
            double dbl = 0;
            double.TryParse(dr[column_name].ToString(), out dbl);
            return dbl;
        }

        public static double GetDouble(this DataRow dr, int column_index)
        {
            double dbl = 0;
            double.TryParse(dr[column_index].ToString(), out dbl);
            return dbl;
        }

        public static double GetDouble(this IDataReader dr, string column_name)
        {
            double dbl = 0;
            double.TryParse(dr[column_name].ToString(), out dbl);
            return dbl;
        }

        public static double GetDouble(this IDataReader dr, int column_index)
        {
            double dbl = 0;
            double.TryParse(dr[column_index].ToString(), out dbl);
            return dbl;
        }

        public static List<PropertyInfo> GetPropertiesByKeyAttribute(this Type type)
        {
            List<PropertyInfo> result = null;

            using (AttributeScanner<KeyAttribute> scanner = new AttributeScanner<KeyAttribute>())
            {
                foreach (var item in scanner.ScanForAttributes(Assembly.GetCallingAssembly(), ClassTypes.Properties, type))
                {
                    if (result == null)
                        result = new List<PropertyInfo>();

                    result.Add((PropertyInfo)item.Item2);
                }
            }

            return result;
        }

        public static List<PropertyInfo> GetPropertiesByNotMappedAttribute(this Type type)
        {
            List<PropertyInfo> result = null;

            using (AttributeScanner<NotMappedAttribute> scanner = new AttributeScanner<NotMappedAttribute>())
            {
                foreach (var item in scanner.ScanForAttributes(Assembly.GetCallingAssembly(), ClassTypes.Properties, type))
                {
                    if (result == null)
                        result = new List<PropertyInfo>();

                    result.Add((PropertyInfo)item.Item2);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the schema.
        /// </summary>
        /// <param name="srv">The SRV.</param>
        /// <param name="dataSouce">The data souce.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <returns></returns>
        /// <exception cref="Exception">dataSouce param must not be null or return null...
        /// or
        /// dataSouce param must not be null or return null...</exception>
        public static KeyValuePair<string, Type>[] GetSchema(this ISqlExecutor srv, Func<SqlConnection> dataSouce, string tableName)
        {
            SqlDataReader reader = null;
            SqlCommand cmd = null;
            SqlConnection conn = null;
            KeyValuePair<string, Type>[] result = null;

            try
            {
                if (dataSouce == null)
                    throw new Exception("dataSouce param must not be null or return null...");

                using (conn = dataSouce())
                {
                    if (conn == null)
                        throw new Exception("dataSouce param must not be null or return null...");

                    if (conn.State != ConnectionState.Open)
                        conn.Open();

                    string query = "SELECT * FROM {0}".FormatString(tableName);
                    cmd = srv.GetCommand(conn, query);
                    cmd.CommandType = CommandType.Text;

                    if (cmd != null)
                    {
                        reader = cmd.ExecuteReader();

                        result = reader.GetSchemaTable().Rows.Cast<DataRow>().Select(
                                    c => new KeyValuePair<string, Type>(c["ColumnName"].ToString(), (Type)c["DataType"]))
                                .ToArray();

                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (conn != null && conn.State != ConnectionState.Closed)
                    conn.Close();
            }

            return result;
        }

        public static KeyValuePair<string, Type>[] GetSchema(this IDataReader reader)
        {
            return reader.GetSchemaTable().Rows.Cast<DataRow>().Select(
                       c => new KeyValuePair<string, Type>(c["ColumnName"].ToString(), (Type)c["DataType"]))
                   .ToArray();
        }

        /// <summary>
        /// To the data table.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="iList">The i list.</param>
        /// <returns></returns>
        public static DataTable ToDataTable<T>(this List<T> iList)
        {
            DataTable dataTable = new DataTable();
            List<PropertyDescriptor> propertyDescriptorCollection = TypeDescriptor.GetProperties(typeof(T)).Cast<PropertyDescriptor>().ToList();

            for (int i = 0; i < propertyDescriptorCollection.Count; i++)
            {
                PropertyDescriptor propertyDescriptor = propertyDescriptorCollection[i];

                Type type = propertyDescriptor.PropertyType ?? typeof(int);

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    type = Nullable.GetUnderlyingType(type);

                dataTable.Columns.Add(propertyDescriptor.Name);
                dataTable.Columns[i].AllowDBNull = true;
            }

            int id = 0;
            foreach (object iListItem in iList)
            {
                ArrayList values = new ArrayList();
                for (int i = 0; i < propertyDescriptorCollection.Count; i++)
                {
                    values.Add(
                        propertyDescriptorCollection[i].GetValue(iListItem) == null && propertyDescriptorCollection[i].PropertyType == typeof(string)
                        ? String.Empty
                        : (i == 0 && propertyDescriptorCollection[i].Name.Contains("Id") && propertyDescriptorCollection[i].PropertyType == typeof(int))
                        ? id += 1
                        : propertyDescriptorCollection[i].GetValue(iListItem) == null
                        ? DBNull.Value
                        : propertyDescriptorCollection[i].GetValue(iListItem));
                }
                dataTable.Rows.Add(values.ToArray());

                values = null;
            }

            return dataTable;
        }

        /// <summary>
        /// To the data table.
        /// </summary>
        /// <param name="iList">The i list.</param>
        /// <param name="objType">Type of the object.</param>
        /// <returns></returns>
        public static DataTable ToDataTable(this List<object> iList, Type objType)
        {
            DataTable dataTable = new DataTable();
            List<PropertyDescriptor> propertyDescriptorCollection = TypeDescriptor.GetProperties(objType).Cast<PropertyDescriptor>().ToList();

            for (int i = 0; i < propertyDescriptorCollection.Count; i++)
            {
                PropertyDescriptor propertyDescriptor = propertyDescriptorCollection[i];

                Type type = propertyDescriptor.PropertyType ?? typeof(int);

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    type = Nullable.GetUnderlyingType(type);

                dataTable.Columns.Add(propertyDescriptor.Name);
                dataTable.Columns[i].AllowDBNull = true;
            }

            int id = 0;
            foreach (object iListItem in iList)
            {
                ArrayList values = new ArrayList();
                for (int i = 0; i < propertyDescriptorCollection.Count; i++)
                {
                    values.Add(
                        propertyDescriptorCollection[i].GetValue(iListItem) == null && propertyDescriptorCollection[i].PropertyType == typeof(string)
                        ? String.Empty
                        : (i == 0 && propertyDescriptorCollection[i].Name.Contains("Id") && propertyDescriptorCollection[i].PropertyType == typeof(int))
                        ? id += 1
                        : propertyDescriptorCollection[i].GetValue(iListItem) == null
                        ? DBNull.Value
                        : propertyDescriptorCollection[i].GetValue(iListItem));
                }
                dataTable.Rows.Add(values.ToArray());

                values = null;
            }

            return dataTable;
        }

        public static SqlDbType GetDbType(this Type giveType)
        {
            Dictionary<Type, SqlDbType> _typeMap = new Dictionary<Type, SqlDbType>() {
                { typeof(string), SqlDbType.NVarChar },
                { typeof(char[]), SqlDbType.NVarChar },
                { typeof(byte), SqlDbType.TinyInt },
                { typeof(short), SqlDbType.SmallInt },
                { typeof(int), SqlDbType.Int },
                { typeof(long), SqlDbType.BigInt },
                { typeof(byte[]), SqlDbType.Image },
                { typeof(bool), SqlDbType.Bit },
                { typeof(DateTime), SqlDbType.DateTime2 },
                { typeof(DateTimeOffset), SqlDbType.DateTimeOffset },
                { typeof(decimal), SqlDbType.Money },
                { typeof(float), SqlDbType.Real },
                { typeof(double), SqlDbType.Float },
                { typeof(TimeSpan), SqlDbType.Time }
        };


            giveType = Nullable.GetUnderlyingType(giveType) ?? giveType;

            if (_typeMap.ContainsKey(giveType))
            {
                return _typeMap[giveType];
            }

            throw new ArgumentException($"{giveType.FullName} is not a supported .NET class");
        }

        public static SqlDbType GetDbType<T>(this T type)
        {

            return GetDbType(typeof(T));
        }

    }
}