#region using section

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AFMaster.Util;
using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using OSIsoft.AF.UnitsOfMeasure;
using System.Text.Json;
using System.Text.Json.Serialization;
using OSIsoft.AF.EventFrame;

#endregion

namespace AFMaster
{
    public partial class Library
    {
        //[ErrorAspect]
        public class Value
        {

            public static void CleanseData(string pointName, object startTime, object endTime)
            {
                var piPoint = PIPoint.FindPIPoint(Connector.GetPIServer(), pointName);
                var values = piPoint.RecordedValues(new AFTimeRange(
                        startTime.ToAFTime(), endTime.ToAFTime()),
                    AFBoundaryType.Inside,
                    "", true, 0);
                var removedlist = values.Where(n => !n.IsGood).ToList();
                if (removedlist.Count > 100) removedlist = removedlist.Take(100).ToList();
                if (removedlist.Count > 0)
                    piPoint.UpdateValues(removedlist, AFUpdateOption.Remove);
            }

            [Method(Description = "Get Snapshot Data for Single Point ...")]
            public static Dictionary<string, object> GetSnapShotData(
                [Parameter("Point Name")] string pointName,
                [Parameter("Duration in Seconds")] double durationInSeconds)
            {
                var piPoint = PIPoint.FindPIPoint(Connector.GetPIServer(), pointName);
                List<AFValue> values = new AFValues();
                var piSnapshotDataPipe = new PIDataPipe(AFDataPipeType.Snapshot);
                piSnapshotDataPipe.AddSignups(new List<PIPoint> { piPoint });
                var startTime = DateTime.Now;
                while (DateTime.Now < startTime + TimeSpan.FromSeconds(durationInSeconds))
                {
                    var afDataPipeEvents = piSnapshotDataPipe.GetUpdateEvents(1000);
                    values.AddRange(afDataPipeEvents.Select(afDataPipeEvent => afDataPipeEvent.Value));
                    Thread.Sleep(350);
                }
                piSnapshotDataPipe.Dispose();
                return Conversion.CreateDictionary(values);
            }

            #region EnumsAndUtils

            private static readonly StringComparison _sc = StringComparison.InvariantCultureIgnoreCase;

            public enum RetrievalType
            {
                Interpolated = 0,
                Recorded,
                Plot
            }

            [Method(Description = "Get Retrieval Types ...")]
            public static string[] GetRetrievalTypes()
            {
                return Enum.GetNames(typeof(RetrievalType)).ToArray();
            }

            public static RetrievalType GetRetrievalType(string retrievalType)
            {
                RetrievalType mode;
                if (!Enum.TryParse(retrievalType, true, out mode))
                    mode = RetrievalType.Interpolated;
                return mode;
            }

            [Method(Description = "Get Retrieval Modes ...")]
            public static string[] GetRetrievalModes()
            {
                return Enum.GetNames(typeof(AFRetrievalMode)).ToArray();
            }

            public static AFRetrievalMode GetRetrievalMode(string retrievalMode)
            {
                AFRetrievalMode mode;
                if (!Enum.TryParse(retrievalMode, true, out mode))
                    mode = AFRetrievalMode.Exact;
                return mode;
            }
            [Method(Description = "Get AF AF Calculation Basis ...")]
            public static string[] GetAFCalculationBasis()
            {
                return Enum.GetNames(typeof(AFCalculationBasis)).ToArray();
            }
            [Method(Description = "Get Summary Types ...")]
            public static string[] GetSummaryTypes()
            {
                return Enum.GetNames(typeof(AFSummaryTypes)).ToArray();
            }
            public static AFCalculationBasis GetAFCalculationBasis(string calculationBasis)
            {
                AFCalculationBasis mode;
                if (!Enum.TryParse(calculationBasis, true, out mode))
                    mode = AFCalculationBasis.TimeWeighted;
                return mode;
            }
            public static AFSummaryTypes GetSummaryType(string summaryType)
            {
                AFSummaryTypes mode;
                if (!Enum.TryParse(summaryType, true, out mode))
                    mode = AFSummaryTypes.All;
                return mode;
            }

            [Method(Description = "Get UOM's ...")]
            public static string[] GetUOMs()
            {
                return UOM.FindUOMs(Connector.GetAFServer().UOMDatabase, "*", AFSearchField.Name,
                    AFSortField.Name, AFSortOrder.Ascending, Connector.MaxItemReturn).Select(n => n.Name).ToArray();
            }

            public static UOM GetUOM(string uomName)
            {
                var uoms = UOM.FindUOMs(Connector.GetAFServer().UOMDatabase, "*", AFSearchField.Name,
                        AFSortField.Name, AFSortOrder.Ascending, Connector.MaxItemReturn)
                    .ToList();
                return uoms.Count > 0 ? uoms[0] : null;
            }

            #endregion

            #region PIPoint

            [Method(Description = "Delete Point Values ...")]
            public static Model.DataVector DeletePointValues(
                [Parameter("Point Name")] string pointName,
                [Parameter("Start Time")] object startTime,
                [Parameter("End Time")] object endTime)
            {
                try
                {
                    var piPoint = PIPoint.FindPIPoint(Connector.GetPIServer(), pointName);
                    var values = piPoint.RecordedValues(new AFTimeRange(
                            startTime.ToAFTime(), endTime.ToAFTime()),
                        AFBoundaryType.Inside,
                        "", true, 0);
                    piPoint.UpdateValues(values, AFUpdateOption.Remove);
                    return new Model.DataVector(1);
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            [Method(Description = "Set Point Value ...")]
            public static Dictionary<string, object> SetPointValue(
                [Parameter("Point Name")] string pointName,
                [Parameter("Time Stamp")] double time,
                [Parameter("Value")] object value)
            {
                SimpleLog.SetLogDir(@":c\temp\");
                SimpleLog.Info("test");
                try
                {
                    var piPoint = PIPoint.FindPIPoint(Connector.GetPIServer(), pointName);
                    var afValue = new AFValue(null, value, new AFTime(time));
                    piPoint.UpdateValue(afValue, AFUpdateOption.Insert);
                    return Conversion.CreateDictionary(new List<AFValue> { afValue });
                }
                catch (Exception ex)
                {
                    SimpleLog.SetLogDir(@":c\temp\");
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            [Method(Description = "Set Point Values ...")]
            public static Dictionary<string, object> SetPointValues(
                [Parameter("Point Name")] string pointName,
                [Parameter("Vector of Time Stamps")] double[] times,
                [Parameter("Vector of Values")] object[] values)
            {
                try
                {
                    var piPoint = PIPoint.FindPIPoint(Connector.GetPIServer(), pointName);
                    var afValues = times.Select((t, index) =>
                        new AFValue(null, values[index], new AFTime(t))).ToList();

                    var errorsWithBuffer = piPoint.UpdateValues(afValues, AFUpdateOption.Insert);
                    return Conversion.CreateDictionary(afValues);
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            private static AFValues _getPointValues(string pointName, object startTime, object endTime,
                string retrievalType, double intervalInSeconds)
            {
                var piPoint = PIPoint.FindPIPoint(Connector.GetPIServer(), pointName);
                AFValues afValues = null;
                if (retrievalType.Equals("interpolated", _sc))
                    afValues = piPoint.InterpolatedValues(new AFTimeRange(
                            startTime.ToAFTime(), endTime.ToAFTime()),
                        new AFTimeSpan(0, 0, 0, 0, 0, Convert.ToInt32(intervalInSeconds), 0), "", true);
                if (retrievalType.Equals("recorded", _sc))
                    afValues = piPoint.RecordedValues(new AFTimeRange(
                            startTime.ToAFTime(), endTime.ToAFTime()),
                        AFBoundaryType.Interpolated,
                        "", true, 0);
                if (retrievalType.Equals("plot", _sc))
                    afValues = piPoint.PlotValues(new AFTimeRange(
                            startTime.ToAFTime(), endTime.ToAFTime()),
                        Convert.ToInt32(intervalInSeconds));
                return afValues;
            }
            private static AFValues _getAttributeValues(string type, string parentId, string attributeId, object startTime, object endTime,
                string retrievalType, int numberOfValues, double intervalInSeconds)
            {
                IList<AFAttribute> attributes = Attribute.GetAttributes(type, parentId, attributeId, "", "");
                var attribute = attributes[0];
                AFValues afValues = null;
                if (retrievalType.Equals("getvalue", _sc))
                    afValues = new AFValues() { attribute.GetValue(startTime.ToAFTime(), null) };
                if (retrievalType.Equals("getvalues", _sc))
                    afValues = attribute.GetValues(new AFTimeRange(
                            startTime.ToAFTime(), endTime.ToAFTime()), numberOfValues, null);
                if (retrievalType.Equals("interpolated", _sc))
                    afValues = attribute.Data.InterpolatedValues(new AFTimeRange(
                            startTime.ToAFTime(), endTime.ToAFTime()),
                        new AFTimeSpan(0, 0, 0, 0, 0, Convert.ToInt32(intervalInSeconds), 0), null, "", true);
                if (retrievalType.Equals("recorded", _sc))
                    afValues = attribute.Data.RecordedValues(new AFTimeRange(
                            startTime.ToAFTime(), endTime.ToAFTime()),
                        AFBoundaryType.Interpolated, null,
                        "", true, 0);
                if (retrievalType.Equals("plot", _sc))
                    afValues = attribute.Data.PlotValues(new AFTimeRange(
                            startTime.ToAFTime(), endTime.ToAFTime()),
                        Convert.ToInt32(intervalInSeconds), null);
                return afValues;
            }

            [Method(Description = "Get Point Value ...")]
            public static Dictionary<string, object> GetPointValue(
            [Parameter("Point Name")] string pointName,
            [Parameter("Time Stamp")] object time,
            [Parameter("Retrieval Type; see function GetRetrievalTypes")] string retrievalType,
            [Parameter("Retrieval Mode; see function GetRetrievalModes")] string retrievalMode)
            {
                try
                {
                    var piPoint = PIPoint.FindPIPoint(Connector.GetPIServer(), pointName);
                    AFValue afvalue = null;
                    if (retrievalType.Equals("interpolated", _sc))
                        afvalue = piPoint.InterpolatedValue(time.ToAFTime());
                    if (retrievalType.Equals("recorded", _sc))
                        afvalue = piPoint.RecordedValue(time.ToAFTime(),
                            GetRetrievalMode(retrievalMode));
                    return Conversion.CreateDictionary(new List<AFValue> { afvalue });
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            [Method(Description = "Get Point Values ...")]
            public static Dictionary<string, object> GetPointValues(
                [Parameter("Point Name")] string pointName,
                [Parameter("Start Time")] object startTime,
                [Parameter("End Time")] object endTime,
                [Parameter("Retrieval Type; see function GetRetrievalTypes")] string retrievalType,
                [Parameter("Interval in Seconds")] double intervalInSeconds)
            {
                try
                {
                    var afValues = _getPointValues(pointName, startTime, endTime,
                        retrievalType, intervalInSeconds);
                    return Conversion.CreateDictionary(afValues);
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            [Method(Description = "Get Multiple Frames with Point Values ...")]
            public static Dictionary<string, object> GetFramesMultiplePointValues(
                [Parameter("Vector of Frame Ids")] string[] ids,
                [Parameter("Vector of Point Names")] string[] pointNames,
                [Parameter("Retrieval Type; see function GetRetrievalTypes")]
                string retrievalType,
                [Parameter("Interval in Seconds")] double intervalInSeconds)
            {

                var values = new List<AFValue>();
                var names = new List<string>();
                var frames = new List<string>();
                try
                {
                    foreach (var id in ids)
                    {
                        var frame = Frame.GetFrames("", id, "", "", "", "", "").FirstOrDefault();
                        var startTime = frame.StartTime.ToString();
                        var endTime = frame.EndTime.ToString();
                        foreach (var pointName in pointNames)
                        {
                            var afValues = _getPointValues(pointName, startTime, endTime,
                                retrievalType, intervalInSeconds); ;
                            values.AddRange(afValues.Select(n => n));
                            names.AddRange(afValues.Select(n => pointName));
                            frames.AddRange(afValues.Select(n => id));
                        }
                    }
                }
                catch { return null; }
                return Conversion.CreateDictionary(values, names, frames);
            }
            public class Test
            {

            }

            [Method(Description = "Get Multiple Times with Point Values ...")]
            public static Dictionary<string, object> GetTimesMultiplePointValues(
                [Parameter("Equipment Id")] string equipmentId,
                [Parameter("Attribute Names")] string[] attributes,
                [Parameter("Start Times")] object[] startTimes,
                [Parameter("End Times")] object[] endTimes,
                [Parameter("Capsules Names")] string[] capsules,
                [Parameter("Retrieval Type; see function GetRetrievalTypes")]
                    string retrievalType,
                [Parameter("Interval in Seconds")] double intervalInSeconds)
            {
                //System.Diagnostics.Debugger.Launch();
                if (startTimes.Length != endTimes.Length)
                    return null;

                var pointNames = new List<Tuple<string, string>>();
                var _attributes = Element.GetAttributesInElement("", equipmentId, "", "", true);
                foreach (string attribute in attributes)
                    for (var index = 0; index < ((IList<string>)_attributes["Name"]).Count; index++)
                        if (string.Equals(((IList<string>)_attributes["Name"])[index], attribute, StringComparison.OrdinalIgnoreCase))
                            pointNames.Add(new Tuple<string, string>(((IList<string>)_attributes["PointName"])[index],
                                ((IList<string>)_attributes["Name"])[index]));

                var values = new List<AFValue>();
                var names = new List<string>();
                var frames = new List<string>();
                try
                {
                    for (var index = 0; index < startTimes.Length; index++)
                    {
                        for (var jndex = 0; jndex < pointNames.Count; jndex++)
                        {
                            var afValues = _getPointValues(pointNames[jndex].Item1, startTimes[index], endTimes[index],
                                retrievalType, intervalInSeconds); ;
                            values.AddRange(afValues.Select(n => n));
                            names.AddRange(afValues.Select(n => pointNames[jndex].Item2));
                            frames.AddRange(afValues.Select(n => capsules[index]));
                        }
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
                return Conversion.CreateDictionary(values, names, frames);
            }

            [Method(Description = "Get Multiple Point Values ...")]
            public static Dictionary<string, object> GetMultiplePointValues(
            [Parameter("Vector of Point Names")] string[] pointNames,
            [Parameter("Start Time")] object startTime,
            [Parameter("End Time")] object endTime,
            [Parameter("Retrieval Type; see function GetRetrievalTypes")] string retrievalType,
            [Parameter("Interval in Seconds")] double intervalInSeconds)
            {
                
                var values = new List<AFValue>();
                var names = new List<string>();
                foreach (var pointName in pointNames)
                {
                    try
                    {
                        var afValues = _getPointValues(pointName, startTime, endTime,
                            retrievalType, intervalInSeconds);
                        names.AddRange(afValues.Select(n => pointName));
                        values.AddRange(afValues.Select(n => n));
                    }
                    catch (Exception ex)
                    {
                        SimpleLog.Error(ex.Message);
                        return null;
                    }

                }
                return Conversion.CreateDictionary(values, names);
            }

            [Method(Description = "Get Matrix Multiple Point Values ...")]
            public static Dictionary<string, object> GetMultiplePointValuesByVariable(
                [Parameter("Vector of Point Names")] string[] pointNames,
                [Parameter("Start Time")] object startTime,
                [Parameter("End Time")] object endTime,
                [Parameter("Interval in Seconds")] double intervalInSeconds)
            {
                //System.Diagnostics.Debugger.Launch();
                var values = new List<AFValues>();
                var names = new List<string>();
                foreach (var pointName in pointNames)
                {
                    try
                    {
                        var retrievalType = "interpolated";
                        AFValues afValues = _getPointValues(pointName, startTime, endTime,
                            retrievalType, intervalInSeconds);
                        names.Add(pointName);
                        values.Add(afValues);
                    }
                    catch (Exception ex)
                    {
                        SimpleLog.Error(ex.Message);
                        return null;
                    }

                }
                return Conversion.CreateDictionary(values, names);
            }
            [Method(Description = "Get Point Summary ...")]
            public static Dictionary<string, object> GetPointSummary(
                [Parameter("Point Name")] string pointName,
                [Parameter("Start Time")] object startTime,
                [Parameter("End Time")] object endTime,
                [Parameter("Summary Type; see function GetSummaryTypes")] string summaryType,
                [Parameter("Calculation Basis; see function GetAFCalculationBasis")] string calculationBasis)
            {
                try
                {
                    var piPoint = PIPoint.FindPIPoint(Connector.GetPIServer(), pointName);
                    var values = piPoint.Summary(new AFTimeRange(
                            startTime.ToAFTime(), endTime.ToAFTime()),
                        GetSummaryType(summaryType), GetAFCalculationBasis(calculationBasis),
                        AFTimestampCalculation.Auto);
                    return Conversion.CreateDictionary(values);
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            [Method(Description = "Get Point Summaries ...")]
            public static Dictionary<string, object> GetPointSummaries(
                [Parameter("Point Name")] string pointName,
                [Parameter("Start Time")] object startTime,
                [Parameter("End Time")] object endTime,
                [Parameter("Summary Type; see function GetSummaryTypes")] string summaryType,
                [Parameter("Calculation Basis; see function GetAFCalculationBasis")] string calculationBasis,
                [Parameter("Interval in Seconds")] double seconds)
            {
                try
                {
                    var piPoint = PIPoint.FindPIPoint(Connector.GetPIServer(), pointName);
                    IDictionary<AFSummaryTypes, AFValues> values = piPoint.Summaries(new AFTimeRange(
                            startTime.ToAFTime(), endTime.ToAFTime()),
                        new AFTimeSpan(0, 0, 0, 0, 0, seconds), GetSummaryType(summaryType),
                        GetAFCalculationBasis(calculationBasis), AFTimestampCalculation.Auto);
                    return Conversion.CreateDictionary(values);
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            #endregion

            #region Attribute

            [Method(Description = "Get Attribute Value ...")]
            public static Dictionary<string, object> GetAttributeValue(
                [Parameter("Parent Id")] string parentId,
                [Parameter("Id")] string id,
                [Parameter("Time Stamp")] string time,
                [Parameter("Retrieval Type; see function GetRetrievalTypes")] string retrievalType,
                [Parameter("Retrieval Mode; see function GetRetrievalModes")] string retrievalMode,
                [Parameter("Unit of Meusare")] string uom)
            {
                try
                {
                    var attributes = Attribute.GetAttributes("element", parentId, id, "", "");
                    if (attributes.Count == 0) return null;
                    AFValue value = null;
                    if (retrievalType.Equals("interpolated", _sc))
                        value = attributes[0].Data.InterpolatedValue(new AFTime(
                            time), GetUOM(uom));
                    if (retrievalType.Equals("recorded", _sc))
                        value = attributes[0].Data.RecordedValue(new AFTime(
                                time), GetRetrievalMode(retrievalType),
                            GetUOM(uom));
                    return Conversion.CreateDictionary(new List<AFValue> { value });
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            [Method(Description = "Get Attribute Values ...")]
            public static Dictionary<string, object> GetAttributeValues(
                [Parameter("Parent Id")] string parentId,
                [Parameter("Attribute Id")] string attributeId,
                [Parameter("Start Time")] string startTime,
                [Parameter("End Time")] string endTime,
                [Parameter("Retrieval Type; see function GetRetrievalTypes")] string retrievalType,
                [Parameter("Interval in Seconds")] double intervalInSeconds,
                [Parameter("Number of Values")] int numberOfValues,
                string uom)
            {
                try
                {
                    var afValues = _getAttributeValues("element", parentId, attributeId, startTime, endTime,
                    retrievalType, numberOfValues, intervalInSeconds);
                    return Conversion.CreateDictionary(afValues);
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }
            [Method(Description = "Get Multiple Attributes Values ...")]
            public static Dictionary<string, object> GetMultipleAttributeValues(
                [Parameter("Parent Id")] string parentId,
                [Parameter("Attribute Ids")] string[] attributeIds,
                [Parameter("Start Time")] object startTime,
                [Parameter("End Time")] object endTime,
                [Parameter("Retrieval Type; see function GetRetrievalTypes")] string retrievalType,
                [Parameter("Interval in Seconds")] double intervalInSeconds,
                [Parameter("Number of Values")] int numberOfValues,
                string uom)
            {
                var values = new List<AFValue>();
                var names = new List<string>();
                foreach (var attributeId in attributeIds)
                {
                    try
                    {
                        var afValues = _getAttributeValues("element", parentId, attributeId, startTime, endTime,
                            retrievalType, numberOfValues, intervalInSeconds);
                        names.AddRange(afValues.Select(n => attributeId));
                        values.AddRange(afValues.Select(n => n));
                    }
                    catch (Exception ex)
                    {
                        SimpleLog.Error(ex.Message);
                        return null;
                    }

                }
                return Conversion.CreateDictionary(values, names);
            }

            [Method(Description = "Get Multiple Attributes Values By Variable...")]
            public static Dictionary<string, object> GetMultipleAttributeValuesByVariable(
                [Parameter("Parent Id")] string parentId,
                [Parameter("Attribute Ids")] string[] attributeIds,
                [Parameter("Start Time")] object startTime,
                [Parameter("End Time")] object endTime,
                [Parameter("Interval in Seconds")] double intervalInSeconds,
                [Parameter("Number of Values")] int numberOfValues,
                string uom)
            {
                //System.Diagnostics.Debugger.Launch();
                var values = new List<AFValues>();
                var names = new List<string>();
                foreach (var attributeId in attributeIds)
                {
                    try
                    {
                        var retrievalType = "interpolated";
                        var afValues = _getAttributeValues("element", parentId, attributeId, startTime, endTime,
                            retrievalType, numberOfValues, intervalInSeconds);
                        names.Add(attributeId);
                        values.Add(afValues);
                    }
                    catch (Exception ex)
                    {
                        SimpleLog.Error(ex.Message);
                        return null;
                    }

                }
                return Conversion.CreateDictionary(values, names);
            }
            [Method(Description = "Get Multiple Attributes Values by Frame ...")]
            public static Dictionary<string, object> GetMultipleAttributeValuesByFrame(
                [Parameter("Frame Id")] string frameId,
                [Parameter("Parent Id")] string parentId,
                [Parameter("Attribute Ids")] string[] attributeIds,
                [Parameter("Start Time")] object startTime,
                [Parameter("End Time")] object endTime,
                [Parameter("Interval in Seconds")] double intervalInSeconds,
                [Parameter("Number of Values")] int numberOfValues,
                string uom)
            {
                //System.Diagnostics.Debugger.Launch();
                var values = new List<AFValues>();
                var names = new List<string>();
                var frameNames = new List<string>();
                IList<AFEventFrame> frames = Frame.GetFrames(frameId, startTime, endTime);
                var result = new Dictionary<string, List<object>>();
                var outResult = new Dictionary<string, object>();
                result.Add("UTCSeconds", new List<object>());
                result.Add("FrameId", new List<object>());
                foreach (var attributeId in attributeIds) result.Add(attributeId, new List<object>());
                
                foreach (var frame in frames)
                {
                    var init = true;
                    foreach (var attributeId in attributeIds)
                    {
                        try
                        {
                            var retrievalType = "interpolated";
                            var afValues = _getAttributeValues("element", parentId, attributeId,
                                frame.StartTime, frame.EndTime,
                                retrievalType, numberOfValues, intervalInSeconds);
                            if (init)
                            {
                                result["UTCSeconds"].AddRange(afValues.Select(n => (object)n.Timestamp.UtcSeconds));
                                result["FrameId"].AddRange(afValues.Select(n => (object)frame.Name));
                                init = false;
                            }
                            result[attributeId].AddRange(afValues.Select(n => n.Value));

                        }
                        catch (Exception ex)
                        {
                            SimpleLog.Error(ex.Message);
                            return null;
                        }
                    }
                }
                foreach (var kv in result) outResult.Add(kv.Key, kv.Value);
                return outResult;
            }
            [Method(Description = "Get Attribute Summary ...")]
            public static Dictionary<string, object> GetAttributeSummary(
                [Parameter("Type")] string type,
                [Parameter("Parent Id")] string parentId,
                [Parameter("Id")] string id,
                [Parameter("Start Time")] string startTime,
                [Parameter("End Time")] string endTime,
                [Parameter("Summary Type; see function GetSummaryTypes")] string summaryType,
                [Parameter("Calculation Basis; see function GetAFCalculationBasis")] string calculationBasis
                )
            {
                try
                {
                    var attributes = Attribute.GetAttributes(type, parentId, id, "", "");
                    if (attributes.Count == 0) return null;
                    var values = attributes[0].Data.Summary(new AFTimeRange(
                            startTime, endTime),
                        GetSummaryType(summaryType), GetAFCalculationBasis(calculationBasis),
                        AFTimestampCalculation.Auto);
                    return Conversion.CreateDictionary(values);
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            [Method(Description = "Get Attribute Summaries ...")]
            public static Dictionary<string, object> GetAttributeSummaries(
                [Parameter("Type")] string type,
                [Parameter("Parent Id")] string parentId,
                [Parameter("Id")] string id,
                [Parameter("Start Time")] string startTime,
                [Parameter("End Time")] string endTime,
                [Parameter("Summary Type")] string summaryType,
                [Parameter("Calculation Basis; see function GetAFCalculationBasis")] string calculationBasis,
                [Parameter("Interval in Seconds")] double seconds)
            {
                try
                {
                    var attributes = Attribute.GetAttributes(type, parentId, id, "", "");
                    if (attributes.Count == 0) return null;
                    var values = attributes[0].Data.Summaries(new AFTimeRange(
                            startTime, endTime),
                        new AFTimeSpan(0, 0, 0, 0, 0, seconds), GetSummaryType(summaryType),
                        GetAFCalculationBasis(calculationBasis), AFTimestampCalculation.Auto);
                    return Conversion.CreateDictionary(values);
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            [Method(Description = "Convert Datetime String or UTC Seconds to SEEQ time string")]
            public static string ToSEEQTime(
                [Parameter("Time Stamp")] object timeStamp)
            {
                var afTime = timeStamp.ToAFTime();
                return ((DateTime)afTime).ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFZ");
            }
            [Method(Description = "Set Attribute Value")]
            public static bool SetAttributeValue(
                [Parameter("Type (element or frame)")] string type,
                [Parameter("Parent Id")] string parentId,
                [Parameter("Attribute Id")] string attributeId,
                [Parameter("Value")] object value,
                [Parameter("Time Stamp")] object timeStamp)
            {
                SimpleLog.Info("setvalue 0 ...");
                var attributes = Attribute.GetAttributes(type, parentId, attributeId, "", "");
                SimpleLog.Info("setvalue 1 ...");
                if (attributes.Count == 0) return false;
                SimpleLog.Info("setvalue ok ...");
                attributes[0].SetValue(null, new AFValue(null,
                        value, timeStamp.ToAFTime(), null));
                return true;
            }

            #endregion

            #region Matrix

            #endregion
        }
    }
}