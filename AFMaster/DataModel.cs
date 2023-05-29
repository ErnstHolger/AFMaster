#region using section

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using AFMaster.Util;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Time;

#endregion

namespace AFMaster
{
    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class equipment
    {
        private equipmentItem[] itemField;

        /// <remarks />
        [XmlElement("item")]
        public equipmentItem[] item
        {
            get => itemField;
            set => itemField = value;
        }
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class equipmentItem
    {
        private equipmentItem[] itemField;

        private string levelField;

        private string[] textField;

        /// <remarks />
        [XmlElement("item")]
        public equipmentItem[] item
        {
            get => itemField;
            set => itemField = value;
        }

        /// <remarks />
        [XmlText]
        public string[] Text
        {
            get => textField;
            set => textField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string level
        {
            get => levelField;
            set => levelField = value;
        }
    }

    public class AFValueTimeEqualityComparer : IEqualityComparer<AFValue>
    {
        #region IEqualityComparer<ThisClass> Members

        public bool Equals(AFValue x, AFValue y)
        {
            //no null check here, you might want to do that, or correct that to compare just one part of your object
            return x.Timestamp.LocalTime == y.Timestamp.LocalTime;
        }

        public int GetHashCode(AFValue obj)
        {
            unchecked
            {
                var hash = 17;
                //same here, if you only want to get a hashcode on a, remove the line with b
                hash = hash * 23 + obj.Timestamp.LocalTime.GetHashCode();
                return hash;
            }
        }

        #endregion
    }

    public class Model
    {
        public static readonly Dictionary<string, string> DeltaVDictionary = new Dictionary<string, string>
        {
            {"Area", "Area local"},
            {"ProcessCell", "Process Cell local"},
            {"Unit", "Unit local"},
            {"ControlModule", "Control Module local"},
            {"EquipmentModule", "Equipment Module local"},
            {"UnitPhase", "Equipment Module local"}
        };
        private static string[] InitString(int count)
        {
            var result = new string[count];
            for (var index = 0; index < result.Length; index++)
                result[index] = "";
            return result;
        }
        private static object[] InitObject(int count)
        {
            var result = new object[count];
            for (var index = 0; index < result.Length; index++)
                result[index] = double.NaN;
            return result;
        }
        private static double[] InitDouble(int count)
        {
            var result = new double[count];
            for (var index = 0; index < result.Length; index++)
                result[index] = double.NaN;
            return result;
        }
        private static DateTime[] InitDateTime(int count)
        {
            var result = new DateTime[count];
            for (var index = 0; index < result.Length; index++)
                result[index] = DateTime.MaxValue;
            return result;
        }

        public class NameTimeValue
        {
            private string v1;
            private DateTime localTime;
            private double v2;

            public NameTimeValue()
            {

            }

            public NameTimeValue(string v1, DateTime localTime, double v2)
            {
                this.v1 = v1;
                this.localTime = localTime;
                this.v2 = v2;
            }
        }

        public class ModelTable
        {
            public string[] Columns { set; get; }
            public List<object[]> Data { set; get; }
            public ModelTable(int row, int col)
            {
                Columns = InitString(col);
                Data=new List<object[]>();
                for (var index = 0; index < col; index++)
                {
                    Data.Add(InitObject(row));
                }
            }
        }
        public class ModelBase
        {
            public ModelBase(int count)
            {
                Selected = InitString(count);
                Parent = InitString(count);
                ParentId = InitString(count);
                Name = InitString(count);
                ObjectType = InitString(count);
                Path = InitString(count);
            }

            public string[] Selected { set; get; }
            public string[] Parent { set; get; }
            public string[] ParentId { set; get; }
            public string[] Name { set; get; }
            public string[] ObjectType { set; get; }
            public string[] Path { set; get; }
        }

        public class Template : ModelBase
        {
            public Template(int count) : base(count)
            {
                Id = InitString(count);
                Type = InitString(count);
                BaseTemplate = InitString(count);
                AllowElementToExtend = new bool[count];
            }

            // others
            public string[] Id { set; get; }
            public string[] BaseTemplate { set; get; }
            public string[] Type { set; get; }
            public bool[] AllowElementToExtend { set; get; }
        }

        public class Connection
        {
            public Connection()
            {
                IsAFConnected = false;
                IsPIConnected = false;
                DefaultPIServer = "";
                DefaultAFServer = "";
                DefaultAFDatabase = "";
                SelectedPIServer = "";
                SelectedAFServer = "";
                SelectedAFDatabase = "";
                IsConnected = false;
                MaxItemReturn = 500;
            }

            public bool IsAFConnected { get; set; }
            public bool IsPIConnected { get; set; }
            public string DefaultPIServer { get; set; }
            public string DefaultAFServer { get; set; }
            public string DefaultAFDatabase { get; set; }
            public string SelectedPIServer { get; set; }
            public string SelectedAFServer { get; set; }
            public string SelectedAFDatabase { get; set; }
            public bool IsConnected { get; set; }
            public int MaxItemReturn { get; set; }
        }

        public class Element : ModelBase
        {
            public Element(int count) : base(count)
            {
                Id = InitString(count);
                TemplateName = InitString(count);
                TemplateId = InitString(count);
                Attribute = InitString(count);
                Path = InitString(count);
                Category = InitString(count);
            }

            public string[] Id { set; get; }
            public string[] TemplateName { set; get; }
            public string[] TemplateId { set; get; }
            public string[] Attribute { set; get; }
            public string[] Category { set; get; }
            public string[] Path { set; get; }

        }

        public class Attribute : ModelBase
        {
            public Attribute(int count) : base(count)
            {
                Id = InitString(count);
                DataReference = InitString(count);
                ConfigString = InitString(count);
                PointName = InitString(count);
            }

            public string[] Id { set; get; }
            public string[] DataReference { get; set; }
            public string[] PointName { get; set; }
            public string[] ConfigString { get; set; }
        }

        public class NameValuePair
        {
            public string[] Name { get; }
            public string[] Value { get; }
            public NameValuePair(string[] name, string[] value)
            {
                Name = name;
                Value = value;
            }
        }
        public class ValuePair
        {
            public ValuePair(int count)
            {
                DateTime1 = InitDateTime(count);
                Vector1 = InitObject(count);
                DateTime2 = InitDateTime(count);
                Vector2 = InitObject(count);
            }

            public DateTime[] DateTime1 { get; }
            public object[] Vector1 { get; }
            public DateTime[] DateTime2 { get; }
            public object[] Vector2 { get; }
        }

        public class DataMatrix
        {
            public object[,] Values { private set; get; }
            //public DateTime[] TimeStamp { private set; get; }
            public double[] UTCSeconds { private set; get; }
            public string[] ColumnHeader { set; get; }

            public DataMatrix(int rows, int columns)
            {
                Initialize(rows, columns);
            }
            private void Initialize(int rows, int columns)
            {
                Values = new object[rows, columns];
                for (var r = 0; r < rows; r++)
                    for (var c = 0; c < columns; c++)
                        Values[r, c] = Double.NaN;

                //TimeStamp = InitDateTime(rows);
                UTCSeconds = InitDouble(rows);
                ColumnHeader = InitString(columns);
            }
        }

        public static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }
        public class FrameMultiDataVector : MultiDataVector
        {
            public string[] FrameId {  get; }
            public FrameMultiDataVector(int count) : base(count)
            {
                FrameId=InitString(count);
            }
        }
        public class MultiDataVector : DataVector
        {
            public string[] PointId { get; }
            public MultiDataVector(int count) : base(count)
            {
                PointId=InitString(count);
            }
        }
        public class DataVector
        {
            public DataVector(int count)
            {
                Initialize(count);
            }
            public double[] UTCSeconds { private set; get; }
            //public DateTime[] TimeStamp { private set; get; }
            public object[] Values { private set; get; }


            private void Initialize(int count)
            {
                Values = InitObject(count);
                UTCSeconds = InitDouble(count);
                //TimeStamp = new DateTime[count];
                for (var r = 0; r < count; r++)
                    Values[r] = Double.NaN;
                //TimeStamp = InitDateTime(count);
            }
        }

        public class Summary
        {
            public DateTime[] TimeStamp { set; get; }
            public object[] Count { set; get; }
            public object[] Average { set; get; }
            public object[] Maximum { set; get; }
            public object[] Minimum { set; get; }
            public object[] PercentGood { set; get; }
            public object[] PopulationStdDev { set; get; }
            public object[] Range { set; get; }
            public object[] StdDev { set; get; }
            public object[] Total { set; get; }

            public Summary(int count)
            {
                TimeStamp = InitDateTime(count);
                Count = InitObject(count);
                Average = InitObject(count);
                Maximum = InitObject(count);
                Minimum = InitObject(count);
                PercentGood = InitObject(count);
                PopulationStdDev = InitObject(count);
                Range = InitObject(count);
                StdDev = InitObject(count);
                Total = InitObject(count);
            }
        }

        public class FrameEvent : Frame
        {
            public FrameEvent(int count) : base(count)
            {
                QueueTime = InitDouble(count);
                LocalTime = InitDouble(count);
                Action = InitString(count);
            }

            public double[] QueueTime { set; get; }
            public double[] LocalTime { set; get; }
            public string[] Action { set; get; }
        }

        public class Frame : ModelBase
        {
            public Frame(int count) : base(count)
            {
                Initialize(count);
            }

            public Frame(int count, int noAttributes) : base(count)
            {
                Initialize(count, noAttributes);
            }

            public string[] Id { set; get; }
            public string[] Element { set; get; }
            public string[] ElementId { set; get; }
            public string[] ElementPath { set; get; }
            public string[] Template { set; get; }
            public double[] StartTime { set; get; }
            public double[] EndTime { set; get; }
            public object[] Duration { set; get; }
            //public bool IsNull { set; get; } = true;

            public void Initialize(int count, int noAttributes = 0)
            {
                Template = InitString(count);
                StartTime = InitDouble(count);
                EndTime = InitDouble(count);
                Duration = InitObject(count);
                Element = InitString(count);
                ElementId = InitString(count);
                ElementPath = InitString(count);
                Id = InitString(count);
            }
        }
        public class Transfer : ModelBase
        {
            public Transfer(int count) : base(count)
            {
                Initialize(count);
            }

            public Transfer(int count, int noAttributes) : base(count)
            {
                Initialize(count, noAttributes);
            }

            public string[] Id { set; get; }
            public string[] Source { set; get; }
            public string[] SourceId { set; get; }
            public string[] Destination { set; get; }
            public string[] DestinationId { set; get; }

            public string[] Template { set; get; }
            public double[] StartTime { set; get; }
            public double[] EndTime { set; get; }
            public object[] Duration { set; get; }
            //public bool IsNull { set; get; } = true;

            public void Initialize(int count, int noAttributes = 0)
            {
                Template = InitString(count);
                StartTime = InitDouble(count);
                EndTime = InitDouble(count);
                Duration = InitObject(count);
                Source = InitString(count);
                SourceId = InitString(count);
                Destination = InitString(count);
                DestinationId = InitString(count);
                Id = InitString(count);
            }
        }
        public class Storage
        {
            public Storage(int count)
            {
                Name = InitString(count);
                StartTime = InitDateTime(count);
                EndTime = InitDateTime(count);
                ValueType = InitString(count);
                NoOfPoints = InitObject(count);
                MeanDistance = InitObject(count);
                MedianDistance = InitObject(count);
                PointsPerMinute = InitObject(count);
                PercentGood = InitObject(count);
                ExceptionDeviation = InitObject(count);
                CompressionDeviation = InitObject(count);
            }

            public string[] Name { set; get; }
            public DateTime[] StartTime { set; get; }
            public DateTime[] EndTime { set; get; }
            public object[] MeanDistance { set; get; }
            public object[] MedianDistance { set; get; }
            public object[] PointsPerMinute { set; get; }
            public object[] PercentGood { set; get; }
            public object[] ExceptionDeviation { set; get; }
            public object[] CompressionDeviation { set; get; }
            public string[] ValueType { set; get; }
            public object[] NoOfPoints { set; get; }
        }

        public class Stats
        {
            public Stats(int count)
            {
                Init(count);
            }

            public Stats(double[] exceptionDeviation, double[] compressionDeviation
                , double[] segmentLength
                , double[] rmsecv
                , double[] rmsee
                , double[] duration
                , double[] noOfSegments)
            {
                Init(exceptionDeviation.Length);
                for (var index = 0; index < exceptionDeviation.Length; index++)
                {
                    CompressionDeviation[index] = compressionDeviation[index];
                    Duration[index] = duration[index];
                    ExceptionDeviation[index] = exceptionDeviation[index];
                    NoOfSegments[index] = noOfSegments[index];
                    RMSECV[index] = rmsecv[index];
                    RMSEE[index] = rmsee[index];
                    SegmentLength[index] = segmentLength[index];
                }
            }

            public object[] ExceptionDeviation { set; get; }
            public object[] CompressionDeviation { set; get; }
            public object[] SegmentLength { set; get; }
            public object[] RMSECV { set; get; }
            public object[] RMSEE { set; get; }
            public object[] Duration { set; get; }
            public object[] NoOfSegments { set; get; }

            public void Init(int count)
            {
                CompressionDeviation = InitObject(count);
                Duration = InitObject(count);
                ExceptionDeviation = InitObject(count);
                NoOfSegments = InitObject(count);
                RMSECV = InitObject(count);
                RMSEE = InitObject(count);
                SegmentLength = InitObject(count);
            }
        }

        public class TimeValue
        {
            public AFTime TimeStamp;
            public double Value;

            public TimeValue(AFTime TimeStamp, double Value)
            {
                this.TimeStamp = TimeStamp;
                this.Value = Value;
            }

            public static TimeValue GetTimeValue(object dateTime, object value)
            {
                return new TimeValue(dateTime.ToAFTime(), (double)value);
            }

            public static TimeValue GetTimeValue(double dateTime, object value)
            {
                return new TimeValue(new AFTime(dateTime), (double)value);
            }

            public static List<TimeValue> GetTimeValues(double[] dateTime, object[] value)
            {
                //if (dateTime[0] is double)
                //{
                return dateTime.Select((t, index) => GetTimeValue(
                    Convert.ToDouble(t), value[index])).ToList();
                //}
                //return dateTime.Select((t, index) => GetTimeValue(
                //    t.ToString(), value[index])).ToList();

            }
            public TimeValue Clone()
            {
                return new TimeValue(TimeStamp, Value);
            }
        }

        public class Coefficient
        {
            public Coefficient(double slope, double intercept)
            {
                Slope = slope;
                Intercept = intercept;
            }

            public double Slope { set; get; }
            public double Intercept { set; get; }
        }
    }
}