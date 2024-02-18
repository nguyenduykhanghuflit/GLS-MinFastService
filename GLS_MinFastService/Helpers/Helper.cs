using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GLS_MinFastService.Helpers
{
    internal static class Helper
    {
        public static Hashtable ModelToHashtableParamSQL<T>(T model)
        {
            Hashtable parameters = new Hashtable();

            PropertyInfo[] properties = typeof(T).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                string propertyName = "@" + property.Name;
                object propertyValue = property.GetValue(model);
                parameters[propertyName] = propertyValue;
            }

            return parameters;
        }
        public static List<T> DataTableToList<T>(this DataTable table) where T : class, new()
        {

            try
            {
                List<T> list = new List<T>();

                foreach (var row in table.AsEnumerable())
                {
                    T obj = new T();

                    foreach (var prop in obj.GetType().GetProperties())
                    {
                        try
                        {
                            PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);
                            propertyInfo?.SetValue(obj, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    list.Add(obj);
                }

                return list;
            }
            catch
            {
                throw;
            }
        }
        public static T DataTableClass<T>(this DataTable table) where T : class, new()
        {
            try
            {
                foreach (var row in table.AsEnumerable())
                {
                    T obj = new T();

                    foreach (var prop in obj.GetType().GetProperties())
                    {
                        try
                        {
                            PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);
                            propertyInfo?.SetValue(obj, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    return obj;
                }

                return null;
            }
            catch
            {

                throw;
            }
        }

        public static Dictionary<TKey, TValue> ConvertDataTableToDictionary<TKey, TValue>(DataTable dataTable, string keyColumnName)
        {
            Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

            if (dataTable != null && dataTable.Rows.Count > 0)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    TKey key = (TKey)row[keyColumnName];
                    TValue value = Activator.CreateInstance<TValue>();

                    foreach (DataColumn column in dataTable.Columns)
                    {
                        if (column.ColumnName != keyColumnName)
                        {
                            PropertyInfo property = typeof(TValue).GetProperty(column.ColumnName);
                            if (property != null && row[column] != DBNull.Value)
                            {
                                property.SetValue(value, row[column]);
                            }
                        }
                    }

                    dictionary.Add(key, value);
                }
            }

            return dictionary;
        }
    }
}
