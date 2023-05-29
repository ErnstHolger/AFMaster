#region using section

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.EventFrame;
using OSIsoft.AF.Time;

#endregion

namespace AFMaster.Util
{
    public class Conversion
    {
        private const string RFormatString = "yyyy-MM-dd HH:mm:ss.fff";

        private static readonly List<AFSummaryTypes> SummaryTypes = new List<AFSummaryTypes>
        {
            AFSummaryTypes.Count,
            AFSummaryTypes.Average,
            AFSummaryTypes.Maximum,
            AFSummaryTypes.Minimum,
            AFSummaryTypes.PercentGood,
            AFSummaryTypes.PopulationStdDev,
            AFSummaryTypes.Range,
            AFSummaryTypes.StdDev,
            AFSummaryTypes.Total
        };

        public static bool IsNumericType(Type type)
        {
            return type == typeof(short) ||
                   type == typeof(int) ||
                   type == typeof(long) ||
                   type == typeof(decimal) ||
                   type == typeof(float) ||
                   type == typeof(double) ||
                   type == typeof(bool);
        }

        public static DataTable ToDataTable(object[,] values, string tableName)
        {
            var dataTable = new DataTable(tableName);
            // 1st row is headers
            var rows = values.GetLength(0);
            var columns = values.GetLength(1);
            for (var index = 0; index < columns; index++)
            {
                var columnName = values[0, index].ToString();
                columnName = columnName.Replace("(x)", "");
                dataTable.Columns.Add(columnName);
            }
            for (var index = 1; index < rows; index++)
            {
                var newRow = dataTable.NewRow();
                for (var jndex = 0; jndex < columns; jndex++)
                    newRow[jndex] = values[index, jndex];
                dataTable.Rows.Add(newRow);
            }
            return dataTable;
        }

        public static bool IsNumeric(object Expression)
        {
            if (Expression == null || Expression is DateTime || Expression is string)
                return false;

            return Expression is short || Expression is int || Expression is long || Expression is decimal ||
                   Expression is float || Expression is double || Expression is bool;
        }

        public static double AFValue2Double(AFValue value)
        {
            try
            {
                return Convert.ToDouble(value.Value.ToString());
            }
            catch (Exception ex)
            {
                SimpleLog.Error(ex.Message);
                return double.NaN;
            }
        }

        //public static string DateTime2RString(DateTime dateTime, bool isTrue)
        //{
        //    return dateTime.ToString(RFormatString);
        //}

        //public static string DateTime2RString(AFTime dateTime)
        //{
        //    return dateTime.ToString(RFormatString);
        //}

        //public static AFTime RString2DateTime(string dateTime)
        //{
        //var provider = CultureInfo.InvariantCulture;
        //
        //    return AFTime.Parse(dateTime, provider);
        //}

        public static SecureString ConvertToSecureString(string password)
        {
            if (password == null)
                throw new ArgumentNullException("password");

            var securePassword = new SecureString();

            foreach (var c in password)
                securePassword.AppendChar(c);

            securePassword.MakeReadOnly();
            return securePassword;
        }

        public static string[] GetFullPath(IList<AFElement> afElements)
        {
            if (afElements.Count == 0) return null;
            var paths = new string[afElements.Count];
            var afDatabase = afElements[0].Database;
            var piSystem = afElements[0].PISystem;
            for (var index = 0; index < afElements.Count; index++)
                paths[index] = GetFullPath(afElements[index]);
            return paths;
        }

        public static string GetFullPath(AFElement afElement)
        {
            var afDatabase = afElement.Database;
            var piSystem = afElement.PISystem;
            var pathList = new List<string>();
            var fullPath = @"\\" + piSystem.Name + @"\" + afDatabase.Name + @"\";
            var stack = new Stack<AFElement>();
            stack.Push(afElement);
            while (stack.Count > 0)
            {
                var currentElement = stack.Pop();
                pathList.Add(currentElement.Name);
                if (currentElement.Parent != null) stack.Push(currentElement.Parent);
            }
            pathList.Reverse();
            fullPath += string.Join(@"\", pathList);
            return fullPath;
        }

        public static Dictionary<string, object> CreateConnection()
        {
            var result = new Dictionary<string, object>();
            try
            {
                result["DefaultAFDatabase"] = Library.Connector.DefaultAFDatabase != null
                        ? Library.Connector.DefaultAFDatabase.Name
                        : "";
                result["DefaultAFServer"] = Library.Connector.DefaultAFServer != null
                    ? Library.Connector.DefaultAFServer.Name
                    : "";
                result["DefaultPIServer"] = Library.Connector.DefaultPIServer != null
                    ? Library.Connector.DefaultPIServer.Name
                    : "";
                result["MaxItemReturn"] = Library.Connector.MaxItemReturn;
                result["SelectedAFDatabase"] = Library.Connector.SelectedAFDatabase != null
                    ? Library.Connector.SelectedAFDatabase.Name
                    : "";
                result["SelectedAFServer"] = Library.Connector.SelectedAFServer != null
                    ? Library.Connector.SelectedAFServer.Name
                    : "";
                result["SelectedPIServer"] = Library.Connector.SelectedPIServer != null
                    ? Library.Connector.SelectedPIServer.Name
                    : "";
                result["IsAFConnected"] = Library.Connector.GetAFServer() != null &&
                                Library.Connector.GetAFServer().ConnectionInfo.IsConnected;
                result["IsPIConnected"] = Library.Connector.GetPIServer() != null &&
                                Library.Connector.GetPIServer().ConnectionInfo.IsConnected;
            }
            catch(Exception ex)
            {

            }
            return result;
        }

        public static Dictionary<string, object>  CreateDictionary(IList<AFElement> afElements, bool includePath = false)
        {
            if (afElements == null || afElements.Count == 0) return null;
            var result = new Dictionary<string, object>();
            var element = new Model.Element(afElements.Count);
            for (var index = 0; index < afElements.Count; index++)
            {
                element.Selected[index] = "x";
                element.Name[index] = afElements[index].Name;
                element.Parent[index] = afElements[index].Parent != null ? afElements[index].Parent.Name : "";
                element.ParentId[index] = afElements[index].Parent != null ? afElements[index].Parent.ID.ToString() : "";
                element.ObjectType[index] = "Element";
                element.Path[index] = afElements[index].GetPath();
                element.Id[index] = afElements[index].ID.ToString();
                element.TemplateName[index] = afElements[index].Template != null ? afElements[index].Template.Name : "";
                element.TemplateId[index] = afElements[index].Template != null
                    ? afElements[index].Template.ID.ToString()
                    : "";
                element.Category[index] = afElements[index].CategoriesString != null
                    ? afElements[index].CategoriesString
                    : "";
            }
            result["Selected"] = element.Selected;
            result["Name"] = afElements.Select(n=>n.Name);
            result["Parent"] = element.Parent;
            result["ParentId"] = element.ParentId;
            result["ObjectType"] = element.ObjectType;
            result["Path"] = element.Path;
            result["Id"] = element.Id;
            result["TemplateName"] = element.TemplateName;
            result["TemplateId"] = element.TemplateId;
            result["Category"] = element.Category;
            
            //if (includePath) element.Path = GetFullPath(afElements);
            return result;
        }

        public static Dictionary<string, object> CreateDictionary(IList<AFAttributeTemplate> afAttributes)
        {
            if (afAttributes == null || afAttributes.Count == 0) return null;
            var result = new Dictionary<string, object>();
            var attribute = new Model.Attribute(afAttributes.Count)
            {
                Name = new string[afAttributes.Count],
                Id = new string[afAttributes.Count]
            };

            for (var index = 0; index < afAttributes.Count; index++)
            {
                var afAttribute = afAttributes[index];
                attribute.Name[index] = afAttribute.Name;
                attribute.Id[index] = afAttribute.ID.ToString();
                if (afAttribute.DataReference != null)
                    attribute.DataReference[index] = afAttributes[index].DataReference.Name;
                if (afAttribute.DataReference != null && afAttribute.DataReference.PIPoint != null)
                    attribute.PointName[index] = afAttribute.DataReference.PIPoint.Name;
                if (!string.IsNullOrEmpty(afAttributes[index].ConfigString))
                    attribute.ConfigString[index] = afAttributes[index].ConfigString;
            }
            
            result["Name"] = attribute.Name;
            result["Id"] = attribute.Id;
            result["DataReference"] = attribute.DataReference;
            result["PointName"] = attribute.PointName;
            result["ConfigString"] = attribute.ConfigString;
            return result;
        }

        public static Dictionary<string, object> CreateDictionary(IList<AFAttribute> afAttributes)
        {
            if (afAttributes == null || afAttributes.Count == 0) return null;
            var result = new Dictionary<string, object>();
            var attribute = new Model.Attribute(afAttributes.Count);
            for (var index = 0; index < afAttributes.Count; index++)
            {
                var afAttribute = afAttributes[index];
                attribute.Selected[index] = "x";
                attribute.Parent[index] = afAttributes[index].Parent != null ? afAttributes[index].Parent.Name : "";
                attribute.ParentId[index] = afAttributes[index].Parent != null ? afAttributes[index].Parent.ID.ToString() : "";
                attribute.ObjectType[index] = "Attribute";
                attribute.Name[index] = afAttribute.Name;
                attribute.Id[index] = afAttribute.ID.ToString();
                attribute.Path[index] = afAttribute.GetPath();
                if (afAttribute.DataReference != null)
                    attribute.DataReference[index] = afAttributes[index].DataReference.Name;
                try
                {
                    if (afAttribute.DataReference != null && afAttribute.DataReference.PIPoint != null)
                        attribute.PointName[index] = afAttribute.DataReference.PIPoint.Name;
                }
                catch (Exception ex)
                {
                    attribute.PointName[index] = "";
                    SimpleLog.Error(ex.Message);
                }
                attribute.ConfigString[index] = afAttributes[index].ConfigString != null
                    ? afAttributes[index].ConfigString
                    : "";
            }
            result["Selected"] = attribute.Selected;
            result["Name"] = attribute.Name;
            result["Parent"] = attribute.Parent;
            result["ParentId"] = attribute.ParentId;
            result["ObjectType"] = attribute.ObjectType;
            result["Path"] = attribute.Path;
            result["Id"] = attribute.Id;
            result["DataReference"] = attribute.DataReference;
            result["ConfigString"] = attribute.ConfigString;
            result["PointName"] = attribute.PointName;
            return result;
        }

        public static Dictionary<string,object> CreateDictionary(IList<AFElementTemplate> templates)
        {
            if (templates == null || templates.Count == 0) return null;
            var result = new Dictionary<string, object>();
            var template = new Model.Template(templates.Count);
            for (var index = 0; index < templates.Count; index++)
            {
                template.Selected[index] = "x";
                template.Name[index] = templates[index].Name;
                template.ObjectType[index] = "EventFrameTemplate";
                template.Id[index] = templates[index].ID.ToString();
                template.Type[index] = templates[index].Type.ToString();
                template.BaseTemplate[index] = templates[index].BaseTemplate != null
                    ? templates[index].BaseTemplate.Name
                    : "";
                template.AllowElementToExtend[index] = templates[index].AllowElementToExtend;
            }
            result["Selected"] = template.Selected;
            result["Name"] = template.Name;
            result["ObjectType"] = template.ObjectType;
            result["Id"] = template.Id;
            result["Type"] = template.Type;
            result["BaseTemplate"] = template.BaseTemplate;
            result["AllowElementToExtend"] = template.AllowElementToExtend;
            return result;
        }

        //public static Model.ValuePair CreateValuePairs(List<AFValue> values, List<string> context = null)
        //{
        //    if (values.Count == 0) return new Model.DataVector(0);
        //}
        public static Dictionary<string, object> CreateDictionary(List<Model.TimeValue> values)
        {
            var result = new Dictionary<string, object>();
            result.Add("UTCSeconds", values.ToList().Select(n => n.TimeStamp.UtcSeconds));
            result.Add("Value", values.ToList().Select(n => n.Value));
            return result;
        }
        public static Dictionary<string, object> CreateDictionary(IDictionary<AFSummaryTypes, AFValue> values)
        {
            var result = new Dictionary<string, object>();
            var afValue = values[AFSummaryTypes.Count];
            result["UTCSeconds"] = afValue.Timestamp.LocalTime;
            result["Count"] = afValue.ValueAsDouble();

            afValue = values[AFSummaryTypes.Maximum];
            result["Maximum"] = afValue.ValueAsDouble();

            afValue = values[AFSummaryTypes.Maximum];
            result["Maximum"] = afValue.ValueAsDouble();

            afValue = values[AFSummaryTypes.Minimum];
            result["Minimum"] = afValue.ValueAsDouble();

            afValue = values[AFSummaryTypes.PercentGood];
            result["PercentGood"] = afValue.ValueAsDouble();

            afValue = values[AFSummaryTypes.PopulationStdDev];
            result["PopulationStdDev"] = afValue.ValueAsDouble();

            afValue = values[AFSummaryTypes.Range];
            result["Range"] = afValue.ValueAsDouble();

            afValue = values[AFSummaryTypes.StdDev];
            result["StdDev"] = afValue.ValueAsDouble();

            afValue = values[AFSummaryTypes.Total];
            result["Total"] = afValue.ValueAsDouble();
            //
            return result;
        }
        public static Dictionary<string, object> CreateDictionary(IDictionary<AFSummaryTypes, AFValues> values)
        {

            var max = 0;
            var result = new Dictionary<string, object>();
            foreach (var keyValuePair in values)
                if (keyValuePair.Value != null && keyValuePair.Value.Count > max)
                {
                    if (keyValuePair.Value.Count <= max) continue;
                    max = keyValuePair.Value.Count;
                }

            // map to object
            var afValues = values[AFSummaryTypes.Count];
            result["UTCSeconds"] = afValues.Select(n => n.Timestamp.LocalTime).ToArray();
            result["Count"] = afValues.Select(n => n.Value).ToArray();

            afValues = values[AFSummaryTypes.Average];
            result["Average"] = afValues.Select(n => n.Value).ToArray();

            afValues = values[AFSummaryTypes.Maximum];
            result["Maximum"] = afValues.Select(n => n.Value).ToArray();

            afValues = values[AFSummaryTypes.Minimum];
            result["Minimum"] = afValues.Select(n => n.Value).ToArray();

            afValues = values[AFSummaryTypes.PercentGood];
            result["PercentGood"] = afValues.Select(n => n.Value).ToArray();

            afValues = values[AFSummaryTypes.PopulationStdDev];
            result["PopulationStdDev"] = afValues.Select(n => n.Value).ToArray();

            afValues = values[AFSummaryTypes.Range];
            result["Range"] = afValues.Select(n => n.Value).ToArray();

            afValues = values[AFSummaryTypes.StdDev];
            result["Average"] = afValues.Select(n => n.Value).ToArray();

            afValues = values[AFSummaryTypes.Total];
            result["Total"] = afValues.Select(n => n.Value).ToArray();
            //
            return result;
        }
        public static Dictionary<string, object> CreateDictionary(IList<AFEventFrame> afEventFrames)
        {
            if (afEventFrames.Count == 0) return null;
            var result = new Dictionary<string, object>();
            var frame = new Model.Frame(afEventFrames.Count);
            for (var index = 0; index < afEventFrames.Count; index++)
            {
                frame.Name[index] = afEventFrames[index].Name;
                frame.Id[index] = afEventFrames[index].ID.ToString();
                frame.StartTime[index] = afEventFrames[index].StartTime.UtcSeconds;
                frame.EndTime[index] = afEventFrames[index].EndTime.UtcSeconds;
                frame.Duration[index] = (afEventFrames[index].EndTime - afEventFrames[index].StartTime).TotalSeconds;
                frame.Template[index] = afEventFrames[index].Template != null ? afEventFrames[index].Template.Name : "";
                frame.Element[index] = afEventFrames[index].ReferencedElements.Count > 0
                    ? afEventFrames[index].ReferencedElements[0].Name
                    : "";
                frame.ElementId[index] = afEventFrames[index].ReferencedElements.Count > 0
                    ? afEventFrames[index].ReferencedElements[0].ID.ToString()
                    : "";
                frame.ElementPath[index] = afEventFrames[index].ReferencedElements.Count > 0
                    ? afEventFrames[index].ReferencedElements[0].GetPath()
                    : "";
                frame.Parent[index] = afEventFrames[index].Parent != null ? afEventFrames[index].Parent.Name : "";
                frame.ParentId[index] = afEventFrames[index].Parent != null ? afEventFrames[index].Parent.ID.ToString() : "";
            }
            result["Name"] = frame.Name;
            result["Id"] = frame.Id;
            result["StartTime"] = frame.StartTime;
            result["EndTime"] = frame.EndTime;
            result["Duration"] = frame.Duration;
            result["Template"] = frame.Template;
            result["Element"] = frame.Element;
            result["ElementId"] = frame.ElementId;
            result["ElementPath"] = frame.ElementPath;
            result["Parent"] = frame.Parent;
            result["ParentId"] = frame.ParentId;

            return result;
        }
        public static Dictionary<string, object> CreateDictionary(IList<AFTransfer> afTransfers)
        {
            var result = new Dictionary<string, object>();
            var transfer = new Model.Transfer(afTransfers.Count);
            for (var index = 0; index < afTransfers.Count; index++)
            {
                transfer.Name[index] = afTransfers[index].Name;
                transfer.Id[index] = afTransfers[index].ID.ToString();
                transfer.StartTime[index] = afTransfers[index].StartTime.UtcSeconds;
                transfer.EndTime[index] = afTransfers[index].EndTime.UtcSeconds;
                transfer.Duration[index] = (afTransfers[index].EndTime - afTransfers[index].StartTime).TotalSeconds;
                transfer.Template[index] = afTransfers[index].Template != null ? afTransfers[index].Template.Name : "";
                transfer.Source[index] = afTransfers[index].Source.Name;
                transfer.SourceId[index] = afTransfers[index].Source.ID.ToString();
                transfer.Destination[index] = afTransfers[index].Destination.Name;
                transfer.DestinationId[index] = afTransfers[index].Destination.ID.ToString();
                transfer.Parent[index] = afTransfers[index].Parent != null ? afTransfers[index].Parent.Name : "";
                transfer.ParentId[index] = afTransfers[index].Parent != null ? afTransfers[index].Parent.ID.ToString() : "";
            }
            result["Name"] = transfer.Name;
            result["Id"] = transfer.Id;
            result["StartTime"] = transfer.StartTime;
            result["EndTime"] = transfer.EndTime;
            result["Duration"] = transfer.Duration;
            result["Template"] = transfer.Template;
            result["Source"] = transfer.Source;
            result["SourceId"] = transfer.SourceId;
            result["Destination"] = transfer.Destination;
            result["DestinationId"] = transfer.DestinationId;
            result["Parent"] = transfer.Parent;
            result["ParentId"] = transfer.ParentId;

            return result;
        }
        public static Dictionary<string, object> CreateDictionary(List<AFValue> values)
        {
            var result = new Dictionary<string, object>();
            result.Add("UTCSeconds", values.ToList().Select(n => n.Timestamp.UtcSeconds));
            result.Add("Value", values.ToList().Select(n => n.Value));
            result.Add("IsGood", values.ToList().Select(n => n.IsGood));
            return result;
        }
        public static Dictionary<string, object> CreateDictionary(List<AFValue> values,
            List<string> names = null,
            List<string> frames = null)
        {
            var result = new Dictionary<string, object>();
            result.Add("UTCSeconds", values.ToList().Select(n => n.Timestamp.UtcSeconds));
            result.Add("Value", values.ToList().Select(n => n.Value));
            result.Add("IsGood", values.ToList().Select(n => n.IsGood));
            result.Add("Name", names);
            result.Add("Frame", frames);

            return result;
        }
        public static Dictionary<string, object> CreateDictionary(List<AFValue> values,
            List<string> names = null)
        {
            var result = new Dictionary<string, object>();
            result.Add("UTCSeconds", values.ToList().Select(n => n.Timestamp.UtcSeconds));
            result.Add("Name", names);
            result.Add("Value", values.ToList().Select(n => n.Value));
            result.Add("IsGood", values.ToList().Select(n => n.IsGood));
            return result;
        }
        public static Dictionary<string, object> CreateDictionary(List<AFValues> values,
            List<string> names, List<string> frameNames)
        {
            //TODO: still need to design
            var result = new Dictionary<string, object>();
            for (var index = 0; index < names.Count; index++)
            {
                if (index == 0)
                    result.Add("UTCSeconds", values[index].Select(n => n.Timestamp.UtcSeconds));
                result.Add(names[index], values[index].Select(n => n.Value));
            }
            return result;
        }
        public static Dictionary<string, object> CreateDictionary(List<AFValues> values,
        List<string> names)
        {
            var result = new Dictionary<string, object>();
            for (var index = 0; index < names.Count; index++)
            {
                if(index==0)
                    result.Add("UTCSeconds", values[index].Select(n => n.Timestamp.UtcSeconds));
                result.Add(names[index], values[index].Select(n => n.Value));
            }
            return result;
        }
        [Obsolete]
        public static Model.DataVector CreateDataValue_Obsolete(List<AFValue> values)
        {
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            if (values.Count == 0) return new Model.DataVector(1);
            ;
            var value = new Model.DataVector(values.Count);


            for (var index = 0; index < values.Count; index++)
            {
                value.Values[index] = values[index].Value;
                value.UTCSeconds[index] = values[index].Timestamp.UtcSeconds;
            }
            return value;
        }
        public static Model.FrameMultiDataVector CreateFrameMultipleDataValue_Obsolete(
            List<AFValue> values,
            List<string> names = null,
            List<string> frames = null)
        {
            var result = new Model.FrameMultiDataVector(values.Count);
            for (var row = 0; row < values.Count; row++)
            {
                result.FrameId[row] = frames[row];
                result.PointId[row] = names[row];
                result.UTCSeconds[row] = values[row].Timestamp.UtcSeconds;
                //result.TimeStamp[index] = timeValues[index].TimeStamp;
                result.Values[row] = values[row].Value;
            }
            return result;
        }
        public static Model.MultiDataVector CreateMultipleDataValue_Obsolete(
            List<AFValue> values,
            List<string> names = null)
        {
            var result = new Model.MultiDataVector(values.Count);
            for (var row = 0; row < values.Count; row++)
            {
                result.PointId[row] = names[row];
                result.UTCSeconds[row] = values[row].Timestamp.UtcSeconds;
                //result.TimeStamp[index] = timeValues[index].TimeStamp;
                if (values[row].Value is AFEnumerationValue)
                    result.Values[row] = values[row].IsGood ? values[row].Value.ToString() : null;
                else
                    result.Values[row] = values[row].IsGood ? values[row].Value : null;
            }
            return result;
        }

        public static Model.DataMatrix CreateDataMatrix_Obsolete(
            List<AFValues> matrix,
            List<string> columnHeader = null)
        {
            if (matrix.Count == 0) return null;
            if (matrix[0].Count == 0) return null;

            var rows = matrix.Count;
            var columns = matrix.Max(n => n.Count);
            var value = new Model.DataMatrix(rows, columns);

            if (columnHeader != null)
                value.ColumnHeader = columnHeader.ToArray();

            for (var row = 0; row < matrix.Count; row++)
            {
                for (var column = 0; column < matrix[row].Count; column++)
                {
                    value.Values[row, column] = matrix[row][column].ValueAsDouble();
                    value.UTCSeconds[row] = matrix[row][column].Timestamp.UtcSeconds;
                    //value.TimeStamp[row] = matrix[row][column].Timestamp.LocalTime;
                }
            }
            return value;
        }

        public static Model.DataVector CreateDataValue_Obsolete(List<Model.TimeValue> timeValues)
        {
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            var result = new Model.DataVector(timeValues.Count);
            for (var index = 0; index < timeValues.Count; index++)
            {
                result.UTCSeconds[index] = timeValues[index].TimeStamp.UtcSeconds;
                //result.TimeStamp[index] = timeValues[index].TimeStamp;
                result.Values[index] = timeValues[index].Value;
            }
            return result;
        }
        public static Model.Transfer CreateTransfer_Obsolete(IList<AFTransfer> afTransfers)
        {
            if (afTransfers.Count == 0) return new Model.Transfer(1);
            ;
            var transfer = new Model.Transfer(afTransfers.Count);
            for (var index = 0; index < afTransfers.Count; index++)
            {
                transfer.Name[index] = afTransfers[index].Name;
                transfer.Id[index] = afTransfers[index].ID.ToString();
                transfer.StartTime[index] = afTransfers[index].StartTime.UtcSeconds;
                transfer.EndTime[index] = afTransfers[index].EndTime.UtcSeconds;
                transfer.Duration[index] = (afTransfers[index].EndTime - afTransfers[index].StartTime).TotalSeconds;
                transfer.Template[index] = afTransfers[index].Template != null ? afTransfers[index].Template.Name : "";
                transfer.Source[index] = afTransfers[index].Source.Name;
                transfer.SourceId[index] = afTransfers[index].Source.ID.ToString();
                transfer.Destination[index] = afTransfers[index].Destination.Name;
                transfer.DestinationId[index] = afTransfers[index].Destination.ID.ToString();
                transfer.Parent[index] = afTransfers[index].Parent != null ? afTransfers[index].Parent.Name : "";
                transfer.ParentId[index] = afTransfers[index].Parent != null ? afTransfers[index].Parent.ID.ToString() : "";
            }
            return transfer;
        }
        public static Model.Frame CreateFrame_Obsolete(IList<AFEventFrame> afEventFrames)
        {
            if (afEventFrames.Count == 0) return new Model.Frame(1);
            ;
            var frame = new Model.Frame(afEventFrames.Count);
            for (var index = 0; index < afEventFrames.Count; index++)
            {
                frame.Name[index] = afEventFrames[index].Name;
                frame.Id[index] = afEventFrames[index].ID.ToString();
                frame.StartTime[index] = afEventFrames[index].StartTime.UtcSeconds;
                frame.EndTime[index] = afEventFrames[index].EndTime.UtcSeconds;
                frame.Duration[index] = (afEventFrames[index].EndTime - afEventFrames[index].StartTime).TotalSeconds;
                frame.Template[index] = afEventFrames[index].Template != null ? afEventFrames[index].Template.Name : "";
                frame.Element[index] = afEventFrames[index].ReferencedElements.Count > 0
                    ? afEventFrames[index].ReferencedElements[0].Name
                    : "";
                frame.ElementId[index] = afEventFrames[index].ReferencedElements.Count > 0
                    ? afEventFrames[index].ReferencedElements[0].ID.ToString()
                    : "";
                frame.ElementPath[index] = afEventFrames[index].ReferencedElements.Count > 0
                    ? afEventFrames[index].ReferencedElements[0].GetPath()
                    : "";
                frame.Parent[index] = afEventFrames[index].Parent != null ? afEventFrames[index].Parent.Name : "";
                frame.ParentId[index] = afEventFrames[index].Parent != null ? afEventFrames[index].Parent.ID.ToString() : "";
            }
            return frame;
        }
        public static Model.Summary CreateSummary_Obsolete(IDictionary<AFSummaryTypes, AFValues> values)
        {

            var max = 0;
            foreach (var keyValuePair in values)
                if (keyValuePair.Value != null && keyValuePair.Value.Count > max)
                {
                    if (keyValuePair.Value.Count <= max) continue;
                    max = keyValuePair.Value.Count;
                }

            Model.Summary summary = new Model.Summary(max);
            // map to object
            var afValues = values[AFSummaryTypes.Count];
            summary.TimeStamp = afValues.Select(n => n.Timestamp.LocalTime).ToArray();
            summary.Count = afValues.Select(n => n.Value).ToArray();

            afValues = values[AFSummaryTypes.Average];
            summary.Average = afValues.Select(n => n.Value).ToArray();

            afValues = values[AFSummaryTypes.Maximum];
            summary.Maximum = afValues.Select(n => n.Value).ToArray();

            afValues = values[AFSummaryTypes.Minimum];
            summary.Minimum = afValues.Select(n => n.Value).ToArray();

            afValues = values[AFSummaryTypes.PercentGood];
            summary.PercentGood = afValues.Select(n => n.Value).ToArray();

            afValues = values[AFSummaryTypes.PopulationStdDev];
            summary.PopulationStdDev = afValues.Select(n => n.Value).ToArray();

            afValues = values[AFSummaryTypes.Range];
            summary.Range = afValues.Select(n => n.Value).ToArray();

            afValues = values[AFSummaryTypes.StdDev];
            summary.StdDev = afValues.Select(n => n.Value).ToArray();

            afValues = values[AFSummaryTypes.Total];
            summary.Total = afValues.Select(n => n.Value).ToArray();
            //
            return summary;
        }
        public static Model.Summary CreateSummary_Obsolete(IDictionary<AFSummaryTypes, AFValue> values)
        {

            Model.Summary summary = new Model.Summary(1);
            // map to object
            var afValue = values[AFSummaryTypes.Count];
            summary.TimeStamp[0] = afValue.Timestamp.LocalTime;
            summary.Count[0] = afValue.ValueAsDouble();

            afValue = values[AFSummaryTypes.Average];
            summary.Average[0] = afValue.ValueAsDouble();

            afValue = values[AFSummaryTypes.Maximum];
            summary.Maximum[0] = afValue.ValueAsDouble();

            afValue = values[AFSummaryTypes.Minimum];
            summary.Minimum[0] = afValue.ValueAsDouble();

            afValue = values[AFSummaryTypes.PercentGood];
            summary.PercentGood[0] = afValue.ValueAsDouble();

            afValue = values[AFSummaryTypes.PopulationStdDev];
            summary.PopulationStdDev[0] = afValue.ValueAsDouble();

            afValue = values[AFSummaryTypes.Range];
            summary.Range[0] = afValue.ValueAsDouble();

            afValue = values[AFSummaryTypes.StdDev];
            summary.StdDev[0] = afValue.ValueAsDouble();

            afValue = values[AFSummaryTypes.Total];
            summary.Total[0] = afValue.ValueAsDouble();
            //
            return summary;
        }
    }
}