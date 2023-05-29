#region using section

using System;
using System.Collections.Generic;
using System.Linq;
using AFMaster.Util;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using OSIsoft.AF.Search;
using OSIsoft.AF.Time;

#endregion

namespace AFMaster
{
    public partial class Library
    {
        public class Point
        {
            [Method(Description = "Remove Point created ...")]
            public static void RemovePtCreated(
                [Parameter("Name")] string name)
            {
                var piPoints = PIPoint.FindPIPoints(Connector.GetPIServer(), name);
                foreach (var piPoint in piPoints)
                {
                    var value = piPoint.CurrentValue();
                    if (value != null && !value.IsGood)
                        piPoint.UpdateValue(value, AFUpdateOption.Remove);
                }
            }
            [Method(Description = "Create a Double Precision Calculation Point for Forecasting ...")]
            public static bool CreateCalculationTag(
                [Parameter("name")]string name)
            {
                var piPoints = PIPoint.FindPIPoints(Connector.GetPIServer(), name);
                if(piPoints.Any()) return true;
                var piPoint =Connector.GetPIServer().CreatePIPoint(name,
                    new Dictionary<string, object>
                    {
                        {"compdev",0 },
                        {"excdev",0 },
                        {"compressing",0 },
                        {"future",0 },
                        {"pointsource","L" },
                        {"pointtype","Float32"}


                    });
                return true;

            }

            [Method(Description = "Compare a Point Value against a Source Value ...")]
            public static List<object> ValueTest(
                [Parameter("name")]string name,
                [Parameter("Time Stamp")]double timeStamp,
                [Parameter("Value")]object value)
            {

                var afTime = new AFTime(timeStamp);

                var result = new List<object>();
                var piPoint = PIPoint.FindPIPoint(Connector.GetPIServer(), name);
                piPoint.LoadAttributes("excdev", "compdev");
               
                var referenceValue = piPoint.InterpolatedValue(afTime);
                result.AddRange(new[]
                {
                    referenceValue.Timestamp.UtcTime.ToString(),
                    referenceValue.Value.ToString(),
                    referenceValue.IsGood,
                    piPoint.GetAttribute("excdev"),
                    piPoint.GetAttribute("compdev")
                });
                if (double.TryParse(value.ToString(), out var doubleValue) &&
                    double.TryParse(referenceValue.Value.ToString(), out var doubleReferenceValue))
                {
                    double doubleCompDev;
                    double.TryParse(piPoint.GetAttribute("compdev").ToString(),
                        out doubleCompDev);
                    var difference = Math.Abs(doubleValue - doubleReferenceValue);
                    result.Add(difference);
                    result.Add(difference <= doubleCompDev
                        ? "pass"
                        : "fail");
                }
                else
                {
                    result.AddRange(new[] {"n/a", "n/a"});
                }
                return result;
            }

            [Method(Description = "Get Point Data Statistics ...")]
            public static Model.Storage GetPointStatistics(
                [Parameter("Name with Wildcard")]string nameFilter,
                [Parameter("Point Source with Wildcard")]string sourceFilter,
                [Parameter("Number of Data Points (100)")]int numberOfPoints)
            {
                try
                {
                    IEnumerable<string> attributeNames = new[] {"excdev", "compdev"};
                    var points = PIPoint.FindPIPoints(Connector.GetPIServer(), nameFilter, sourceFilter,
                        attributeNames).ToList();
                    var storage = new Model.Storage(points.Count());
                    if (points == null || points.Count == 0) return storage;
                    //SimpleLog.Info("Point count" + points.Count);
                    for (var index = 0; index < points.Count(); index++)
                    {
                        storage.Name[index] = points[index].Name;
                        storage.ValueType[index] = points[index].PointType.ToString();
                        if (points[index].PointType == PIPointType.Blob ||
                            points[index].PointType == PIPointType.Timestamp ||
                            points[index].PointType == PIPointType.Digital ||
                            points[index].PointType == PIPointType.Null ||
                            points[index].PointType == PIPointType.String)
                        {
                            // do nothing
                        }
                        var values = points[index].RecordedValuesByCount(AFTime.Now, numberOfPoints, false,
                            AFBoundaryType.Inside, "", true);

                        storage.ExceptionDeviation[index] =
                            Convert.ToDouble(points[index].GetAttribute("excdev").ToString());
                        storage.CompressionDeviation[index] =
                            Convert.ToDouble(points[index].GetAttribute("compdev").ToString());

                        if (values.Count <= 1) continue;
                        storage.StartTime[index] = values[0].Timestamp;
                        storage.EndTime[index] = values[values.Count - 1].Timestamp;
                        storage.PercentGood[index] = Convert.ToDouble(values.Count(n => n.IsGood)) /
                                                     Convert.ToDouble(values.Count) * 100.0;
                        storage.MeanDistance[index] = Convert.ToDouble((values[0].Timestamp -
                                                                        values[values.Count - 1].Timestamp)
                                                          .TotalSeconds) /
                                                      Convert.ToDouble(values.Count);
                        storage.NoOfPoints[index] = Convert.ToDouble(values.Count);
                        var temp = new List<double>();
                        for (var jndex = 1; jndex < values.Count; jndex++)
                            temp.Add((values[jndex - 1].Timestamp - values[jndex].Timestamp).TotalSeconds);
                        storage.MedianDistance[index] = temp.Median();
                        storage.PointsPerMinute[index] =(60.0 / (double)storage.MedianDistance[index]);
                    }
                    return storage;
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            public static IEnumerable<PIPoint> GetPoints(string name,
                string pointsource, string datatype, string pointclass)
            {
                var criteria = new List<PIPointQuery>( );
                if (!string.IsNullOrEmpty(name))
                    criteria.Add(new PIPointQuery("Tag", AFSearchOperator.Equal, name));
                if (!string.IsNullOrEmpty(pointsource))
                    criteria.Add(new PIPointQuery("PointSource", AFSearchOperator.Equal, pointsource));
                if (!string.IsNullOrEmpty(datatype))
                    criteria.Add(new PIPointQuery("DataType", AFSearchOperator.Equal, datatype));
                if (!string.IsNullOrEmpty(pointclass))
                    criteria.Add(new PIPointQuery("PointClass", AFSearchOperator.Equal, pointclass));

                return PIPoint.FindPIPoints(Connector.GetPIServer(), criteria);
            }
            [Method(Description = "Get Points ...")]
            public static string[] GetPoints(
                [Parameter("Name")] string name,
                [Parameter("PointSource")] string pointsource,
                [Parameter("DataType")] string datatype,
                [Parameter("PointClass")] string pointclass,
                [Parameter("As Model")] bool asModel)
            {
                return GetPoints(name,pointsource,datatype,pointclass).Select(n => n.Name).ToArray();
            }


            public static object[,] GetPointSourceInfo(string serverName, bool asModel)
            {
                var dictionary = GetPointSourceInfo(serverName);
                var result = new object[dictionary.Keys.Count, 4];
                var temp = dictionary.Values.ToList();
                for (var index = 0; index < dictionary.Values.Count; index++)
                for (var jndex = 0; jndex < 4; jndex++) result[index, jndex] = temp[index][jndex];
                return result;
            }

            [Method(Description = "Get PI Point Attributes...")]
            public static Model.NameValuePair GetPointAttributes(
                [Parameter("Name")] string name,
                [Parameter("Vector of Attributes")]string[] attributes)
            {
                var point= GetPoints(name,"","","").FirstOrDefault();
                if (point == null) return null;
                point.LoadAttributes(attributes);
                var values= attributes.ToList().
                    Select(n=>point.GetAttribute(n).ToString()).ToArray();
                return new Model.NameValuePair(attributes,values);
            }
            private static object SafeGetAttribute(PIPoint piPoint, string name)
            {
                try
                {
                    return piPoint.GetAttribute(name);
                }
                catch
                {
                    return "error";
                }
            }

            public static Dictionary<string, List<string>> GetPointSourceInfo(string serverName)
            {
                Connector.SetPIServer(serverName);
                var pointSourceDictionary = new Dictionary<string, List<string>>();
                string[] mainAttributes =
                {
                    "pointsource",
                    "location1",
                    "datasecurity",
                    "ptSecurity"
                };
                var pointIEnumerable = PIPoint.FindPIPoints(Connector.GetPIServer(), "*");
                var piPointList = new PIPointList(pointIEnumerable);
                piPointList.LoadAttributes(mainAttributes);
                foreach (var piPoint in pointIEnumerable)
                {
                    if (pointSourceDictionary.ContainsKey(
                        SafeGetAttribute(piPoint, "pointsource") + "-" + SafeGetAttribute(piPoint, "location1")))
                        continue;
                    pointSourceDictionary.Add(SafeGetAttribute(piPoint, "pointsource") +
                                              "-" + SafeGetAttribute(piPoint, "location1"), new List<string>
                    {
                        SafeGetAttribute(piPoint, "pointsource").ToString(),
                        SafeGetAttribute(piPoint, "location1").ToString(),
                        SafeGetAttribute(piPoint, "datasecurity").ToString(),
                        SafeGetAttribute(piPoint, "ptSecurity").ToString()
                    });
                }
                return pointSourceDictionary;
            }
        }
    }
}