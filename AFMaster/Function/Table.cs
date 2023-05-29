using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using AFMaster.Util;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Time;


namespace AFMaster
{
    public partial class Library
    {
        public class Table
        {
            [Method(Description = "Delete Table ...")]
            public static bool DeleteTable(
                [Parameter("Array of Ids")] string[] ids)
            {
                foreach (var id in ids)
                {
                    try
                    {
                        var guid = Guid.Parse(id);
                        AFTable.DeleteTables(Connector.GetAFDatabase().PISystem,new List<Guid> {guid});
                    }
                    catch (Exception ex)
                    {
                        SimpleLog.Error(ex.Message);
                    }
                }

                return true;
            }

            [Method(Description = "Get Table ...")]
            public static Model.ModelTable GetTable(
                [Parameter("Name")] string name)
            {
                AFTable afTable = Connector.GetAFDatabase().Tables.FirstOrDefault(_ => _.Name == name);
                if (afTable == null) return null;

                var table = afTable.Table;
                var row = table.Rows.Count;
                var col= table.Columns.Count;
                var modelTable=new Model.ModelTable(row,col);
                for (var c = 0; c < col; c++)
                {
                    modelTable.Columns[c] = table.Columns[c].ColumnName;
                    for (var r = 0; r < row; r++)
                        modelTable.Data[c][r] = table.Rows[r].ItemArray[c];
                }

                return modelTable;
            }

            [Method(Description = "Query Table ...")]
            public static Model.ModelTable QueryTable(
                [Parameter("Name")] string name,
                [Parameter("QueryString")] string queryString)
            {
                var afTable = Connector.GetAFDatabase().Tables.FirstOrDefault(_ => _.Name == name);
                if (afTable == null) return null;
                
                var table = afTable.Table;
                DataRow[] results = table.Select(queryString);
                if (results.Length == 0) return null;
                var row = results.Length;
                var col = table.Columns.Count;
                var modelTable = new Model.ModelTable(row, col);
                for (var c = 0; c < col; c++)
                {
                    modelTable.Columns[c] = table.Columns[c].ColumnName;
                    for (var r = 0; r < row; r++)
                        modelTable.Data[c][r] = results[r].ItemArray[c];
                }

                return modelTable;
            }

            [Method(Description = "Set Table ...")]
            public static bool SetTable(
                [Parameter("Name")] string name,
                [Parameter("Description")] string description,
                [Parameter("Columns")] string[] columns,
                [Parameter("Data Types")] string[] types)
            {

                try
                {
                    DataTable table = new DataTable {Locale = CultureInfo.CurrentCulture};
                    var afTable = new AFTable(name)
                    {
                        TimeZone = AFTimeZone.UtcTimeZone, Description = description
                    };

                    for (var index = 0; index < columns.Length; index++)
                    {
                        try
                        {
                            table.Columns.Add(columns[index], Type.GetType("System."+types[index]));
                        }
                        catch (Exception ex)
                        {
                            SimpleLog.Error(ex.Message);
                            return false;
                        }
                    }
                    afTable.Table = table;
                    Connector.GetAFDatabase().Tables.Add(afTable);
                    afTable.CheckIn();
                    Connector.GetAFDatabase().CheckIn();
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                }

                return true;
            }

            [Method(Description = "Add Row to Table ...")]
            public static bool AddRow(
                [Parameter("Name")] string name,
                [Parameter("Columns")] string[] columns,
                [Parameter("Values")] object[] values)
            {
                var afTable = Connector.GetAFDatabase().Tables.FirstOrDefault(_ => _.Name == name);
                if (afTable == null) return false;

                var table = afTable.Table;
                var row = table.NewRow();
                for (var index = 0; index < columns.Length; index++)
                {
                    try
                    {
                        row[columns[index]] = values[index];
                    }
                    catch (Exception ex)
                    {
                        SimpleLog.Error(ex.Message);
                    }
                }
                table.Rows.Add(row);
                table.AcceptChanges();
                Connector.GetAFDatabase().CheckIn();
                return true;
            }
            [Method(Description = "Update Row in Table ...")]
            public static bool UpdateRow(
                [Parameter("Name")] string name,
                [Parameter("Query")] string query,
                [Parameter("Columns")] string[] columns,
                [Parameter("Values")] object[] values,
                [Parameter("Create Table")] bool createIfNotExist)
            {
                var afTable = Connector.GetAFDatabase().Tables.FirstOrDefault(_ => _.Name == name);
                if (afTable == null) return false;
                var table = afTable.Table;

                var row = table.Select(query).FirstOrDefault();
                if (row == null)
                {
                    if (!createIfNotExist) return false;
                    AddRow(name, columns, values);
                    return true;
                }
                for (var index = 0; index < columns.Length; index++)
                {
                    try
                    {
                        row[columns[index]] = values[index];
                    }
                    catch (Exception ex)
                    {
                        SimpleLog.Error(ex.Message);
                        return false;
                    }
                }
                table.AcceptChanges();
                Connector.GetAFDatabase().CheckIn();
                return true;
            }
        }
    }
}