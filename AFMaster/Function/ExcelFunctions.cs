#region using section

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using OSIsoft.AF.PI;

#endregion

namespace AFMaster
{
    public partial class Library
    {
        public class ExcelFunctions
        {
            public void OpenPIServer(string serverName)
            {
                Connector.SetPIServer(serverName);
            }

            public List<string> GetPIServers()
            {
                return AFConnector.GetPIServers().Select(n => n.Name).ToList();
            }

            public DataTable GetPointSourceInfo(string serverName)
            {
                var dataTable = CreateTable("Table PointSourceInfo", new List<object>
                    {"Header", "pointsource", "location2", "datasecurity", "ptSecurity"});
                var pointList = PIPoint.FindPIPoints(Connector.GetPIServer(), "*", null, null);

                //for (int index = 0; index < result.GetLength(0); index++)
                //{
                //    AddRow(ref dataTable,new  List<object>
                //    {
                //        "PointSourceInfo",result[index,0],result[index,1],result[index,2],result[index,3]
                //    });
                //}
                return dataTable;
            }


            public DataTable GetPIPoint(string serverName, string tagName, string pointSource, string location2,
                List<string> attributes)
            {
                throw new NotImplementedException();
            }

            public DataTable GetOPCExample(string serverName, string tagName, string pointSource, string location2)
            {
                throw new NotImplementedException();
                // not implemented
            }

            public DataTable GetPItoPIExample(string serverName, string tagName, string pointSource, string location2)
            {
                throw new NotImplementedException();
                // not implemented
            }

            public DataTable GetOptimizedCompression(string serverName, string tagName, string pointSource,
                string location2,
                double target,
                double exceptionCompressionRatio, int windowSize)
            {
                throw new NotImplementedException();
            }

            public DataTable GetOptimizedCompression2(string serverName, string tagName, string pointSource,
                string location2,
                double target, double exceptionCompressionRatio, int windowSize)
            {
                throw new NotImplementedException();
            }

            public DataTable GetWriteSpeed(string serverName, DataTable pointSourceTable, double lowerSpeedLimit,
                double upperSpeedLimit,
                int windowSize = 10)
            {
                throw new NotImplementedException();
            }

            public DataTable GetWriteSpeed(string serverName, string tagName, string pointSource, string location2,
                int windowSize = 10)
            {
                throw new NotImplementedException();
            }

            public DataTable GetLastGoodValue(string serverName, DataTable pointSourceTable, int windowSize = 10)
            {
                throw new NotImplementedException();
            }

            public DataTable GetLastGoodValue(string serverName, string tagName, string pointSource, string location2,
                int windowSize = 50)
            {
                throw new NotImplementedException();
            }

            public object[,] GetLastGoodValue(string serverName, string tagName, int windowSize)
            {
                throw new NotImplementedException();
            }

            public object[,] GetCount(string serverName, string tagName, int windowSize)
            {
                throw new NotImplementedException();
            }

            public object[,] GetSnapShot(string serverName, string tagName, int windowSize)
            {
                throw new NotImplementedException();
            }

            public object[,] GetLastArchivedValue(string serverName, string tagName, int windowSize)
            {
                throw new NotImplementedException();
            }

            //public List<Alias> ListAllAliases(string serverName, string rootModule, List<string> matchList)
            //{
            //    throw new NotImplementedException();
            //}

            //public List<AliasValue> GetAliasValues(List<Alias> aliasList, string startTime, string endTime, Regex regex)
            //{
            //    throw new NotImplementedException();
            //}

            public List<string> GetAFServers()
            {
                throw new NotImplementedException();
            }

            public List<string> GetFrameTemplates()
            {
                throw new NotImplementedException();
            }

            public List<string> GetAFDatabases()
            {
                throw new NotImplementedException();
            }

            public object[,] GetFramesAndAttributes(DateTime startTIme, DateTime endTime)
            {
                throw new NotImplementedException();
            }

            private DataTable CreateTable(string tableName, List<object> tableHeader)
            {
                var dataTable = new DataTable(tableName);
                foreach (var columnHeader in tableHeader)
                    dataTable.Columns.Add(columnHeader.ToString());
                return dataTable;
            }

            private void AddRow(ref DataTable dataTable, List<object> rowData)
            {
                // 1st check if we need to add columns!
                for (var index = dataTable.Columns.Count; index < rowData.Count; index++)
                    dataTable.Columns.Add();
                var dataRow = dataTable.NewRow();
                for (var index = 0; index < rowData.Count; index++)
                    dataRow[index] = rowData[index];

                dataTable.Rows.Add(dataRow);
            }
        }
    }
}