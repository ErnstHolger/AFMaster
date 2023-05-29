#region using section

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AFMaster.Util;

#endregion

namespace AFMaster
{
    public partial class Library
    {
        public enum CompressionType
        {
            PIException = 0,
            SimpleException,
            PICompression,
            PIExceptionAndCompression
        }

        public enum EmaType
        {
            Previous = -1,
            Interpolated = 0,
            Next = 1
        }

        public enum OperatorType
        {
            Ema = 0,
            Nema,
            Ma,
            Msd,
            ZScore,
            Outlier,
            Derivative
        }

        public interface ICompression
        {
            Model.TimeValue Calculate(Model.TimeValue dv);
            double GetThreshold();
            List<Model.TimeValue> Drain();
        }

        public interface ICalculate
        {
            Model.TimeValue Calculate(Model.TimeValue dv);
        }

        public abstract class Calculation : ICompression
        {
            public double CompThresh;
            public double ExcThresh;
            public bool initialized;
            public Model.TimeValue LastRecordedValue;
            public Model.TimeValue PreviousValue;
            public Queue<Model.TimeValue> queue = new Queue<Model.TimeValue>();
            public double Threshold { set; get; }

            public List<Model.TimeValue> Drain()
            {
                if (PreviousValue != null &&
                    PreviousValue.TimeStamp != LastRecordedValue.TimeStamp)
                    queue.Enqueue(PreviousValue);
                if (queue.Count > 0) return queue.ToList();
                return null;
            }

            public virtual Model.TimeValue Calculate(Model.TimeValue dv)
            {
                throw new NotImplementedException();
            }

            public double GetThreshold()
            {
                return Threshold;
            }
        }

        public class Calculations
        {
            private static Model.Coefficient CalculateSlopeAndOffset(Model.TimeValue first, Model.TimeValue last)
            {
                try
                {
                    return new Model.Coefficient
                    (
                        (last.Value - first.Value) / (last.TimeStamp - first.TimeStamp).TotalSeconds,
                        first.Value
                    );
                }
                catch 
                {
                    return null;
                }
            }

            private static double Interpolation(Model.Coefficient coefficient, Model.TimeValue First,
                Model.TimeValue value)
            {
                try
                {
                    return coefficient.Slope * (value.TimeStamp - First.TimeStamp).TotalSeconds + coefficient.Intercept;
                }
                catch
                {
                    return Double.NaN;
                }
            }

            public static List<Model.TimeValue> Interpolation(List<Model.TimeValue> Raw,
                List<Model.TimeValue> Compressed)
            {
                var Result = new List<Model.TimeValue>();


                foreach (var t in Raw)
                {
                    double value;
                    var first = Compressed.Find(n => n.TimeStamp >= t.TimeStamp);
                    var last = Compressed.FindLast(n => n.TimeStamp <= t.TimeStamp);
                    if (first == last)
                    {
                        value = first.Value;
                    }
                    else if (first == null && last != null)
                    {
                        value = last.Value;
                    }
                    else if (first == null)
                    {
                        value = double.NaN;
                    }
                    else
                    {
                        try
                        {
                            var coefficient = CalculateSlopeAndOffset(first, last);
                            value = coefficient.Slope * (t.TimeStamp - first.TimeStamp).TotalSeconds +
                                    coefficient.Intercept;
                        }
                        catch
                        {
                            value = Double.NaN;
                        }
                    }
                    Result.Add(new Model.TimeValue(t.TimeStamp, value));
                }

                return Result;
            }

            [Method(Description = "Apply Exception and Compression to Data Series ...")]
            public static Dictionary<string, object> CalculateCompressedValues(
                [Parameter("PIException,SimpleException,PICompression,PIExceptionAndCompression")]string type,
                [Parameter("Exception Deviation")]double exceptionDeviation,
                [Parameter("Compression Deviation")]double compressionDeviation,
                [Parameter("Vector of Time Stamps")]double[] dateTimes,
                [Parameter("Vector of Values")]object[] values)
            {
                CompressionType mode;
                if (!Enum.TryParse(type, true, out mode)) return null;
                var timeValues = Model.TimeValue.GetTimeValues(dateTimes, values);
                var result = Compression(mode, exceptionDeviation,
                    compressionDeviation, timeValues);

                return Conversion.CreateDictionary(result);
            }

            public static List<Model.TimeValue> Compression(CompressionType type, double excDev, double compDev,
                List<Model.TimeValue> data)
            {
                var comp = GetExceptionAndCompressionFunction(type, excDev, compDev);
                var result = data.Select(item => comp.Calculate(item)).Where(e => e != null).ToList();
                var es = comp.Drain();
                if (es != null) result.AddRange(comp.Drain());
                return result;
            }

            [Method(Description = "List all Compression Types ...")]
            public static string[] GetCompressionTypes()
            {
                return new[]
                {
                    "PIException",
                    "SimpleException",
                    "PICompression",
                    "PIExceptionAndCompression"
                };
            }

            public static ICompression GetExceptionAndCompressionFunction(CompressionType type,
                double exceptionDeviation,
                double compressionDeviation)
            {
                ICompression comp;
                switch (type)
                {
                    case CompressionType.PIException:
                        comp = new PIException(exceptionDeviation);
                        break;
                    case CompressionType.SimpleException:
                        comp = new SimpleException(exceptionDeviation);
                        break;
                    case CompressionType.PICompression:
                        comp = new PICompression(compressionDeviation);
                        break;
                    case CompressionType.PIExceptionAndCompression:
                        comp = new PIExceptionCompression(exceptionDeviation, compressionDeviation);
                        break;

                    default:
                        return null;
                }
                return comp;
            }

            [Method(Description = "Calculate Compression Statistics ...")]
            public static Model.Stats Statistics(
                [Parameter("PIException, SimpleException, PICompression, PIExceptionAndCompression")]
                string type,
                [Parameter("Exception Deviation")]double exceptionDeviation,
                [Parameter("Compression Deviation")]double compressionDeviation,
                [Parameter("Vector of Time Stamps")]double[] dateTimes, 
                [Parameter("Vector of Values")] object[] values)
            {
                CompressionType mode;
                if (!Enum.TryParse(type, true, out mode)) return new Model.Stats(1);
                var timeValues = Model.TimeValue.GetTimeValues(dateTimes, values);
                return Statistics(mode, exceptionDeviation, compressionDeviation, timeValues);
            }

            public static Model.Stats Statistics(CompressionType type, double exceptionDeviation,
                double comrpessionDeviation, List<Model.TimeValue> data)
            {
                var stats = new Model.Stats(1);

                double ne = 0;
                double nc = 0;
                double ns = 0;
                double msecv = 0;
                double msee = 0;
                double segmentlength = 0;

                // Fitting
                var compressedValues = Compression(type, exceptionDeviation, comrpessionDeviation,
                    data);
                var interpolatedValues = Interpolation(data, compressedValues);
                for (var r = 0; r < data.Count; r++)
                {
                    msee = msee + Math.Pow(data[r].Value - interpolatedValues[r].Value, 2);
                    ne = ne + 1;
                }
                // segment calculation
                for (var r = 0; r < compressedValues.Count - 1; r++)
                {
                    segmentlength = segmentlength +
                                    (compressedValues[r + 1].TimeStamp - compressedValues[r].TimeStamp)
                                    .TotalSeconds;
                    ns = ns + 1;
                }
                // LOOCV
                var sw = new Stopwatch();
                sw.Start();
                for (var r = 1; r < data.Count; r++)
                {
                    var test = new List<Model.TimeValue>(data);
                    test.RemoveAt(r);
                    compressedValues = Compression(type, exceptionDeviation, comrpessionDeviation, test);
                    interpolatedValues = Interpolation(new List<Model.TimeValue> {data[r]}, compressedValues);
                    if (interpolatedValues.Count == 1)
                    {
                        msecv = msecv + Math.Pow(data[r].Value - interpolatedValues[0].Value, 2);
                        nc = nc + 1;
                    }
                }
                sw.Stop();
                stats.ExceptionDeviation[0] = exceptionDeviation;
                stats.CompressionDeviation[0] = comrpessionDeviation;
                stats.SegmentLength[0] = segmentlength / (ns * 1000);
                stats.RMSECV[0] = Math.Sqrt(msecv / nc);
                stats.RMSEE[0] = Math.Sqrt(msee / ne);
                stats.Duration[0] = sw.ElapsedMilliseconds;
                stats.NoOfSegments[0] = nc;
                return stats;
            }

            [Method(Description = "Calculate Stats on Exception and Compression ...")]
            // ReSharper disable once InconsistentNaming
            public static Model.Stats GetMESCV(
                [Parameter("PIException, SimpleException, PICompression, PIExceptionAndCompression")]string type,
                [Parameter("Vector of Compression Deviation")]double[] exceptionDeviation,
                [Parameter("Vector of Comrpession Deviation")]double[] compressionDeviation,
                [Parameter("Vector of Time Stamps")]double[] dateTimes,
                [Parameter("Vector of Values")]object[] values)
            {
                CompressionType mode;
                if (!Enum.TryParse(type, true, out mode)) return null;
                var timeValues = Model.TimeValue.GetTimeValues(dateTimes, values);
                return GetMESCV(mode, exceptionDeviation, compressionDeviation, timeValues);
            }

            // ReSharper disable once InconsistentNaming
            public static Model.Stats GetMESCV(CompressionType type, double[] exceptionDeviation,
                double[] compressionDeviation, List<Model.TimeValue> data)
            {
                var stats = new Model.Stats(exceptionDeviation.Length * compressionDeviation.Length);
                for (var q = 0; q < compressionDeviation.Length; q++)
                for (var p = 0; p < exceptionDeviation.Length; p++)
                {
                    var r = q * exceptionDeviation.Length + p;
                    var temp = Statistics(type, exceptionDeviation[p], compressionDeviation[q], data);
                    stats.CompressionDeviation[r] = temp.CompressionDeviation[0];
                    stats.Duration[r] = temp.Duration[0];
                    stats.ExceptionDeviation[r] = temp.ExceptionDeviation[0];
                    stats.NoOfSegments[r] = temp.NoOfSegments[0];
                    stats.RMSECV[r] = temp.RMSECV[0];
                    stats.RMSEE[r] = temp.RMSEE[0];
                    stats.SegmentLength[r] = temp.SegmentLength[0];
                }
                return stats;
            }

            [Method(Description = "Calculate EMA ...")]
            public static Dictionary<string,object> CalculateEMA(
                [Parameter("Vector of Time Stamps")]double[] dateTimes,
                [Parameter("Vector of Values")]object[] values,
                [Parameter("Window in Seconds")]double seconds)
            {
                // convert to list of values
                var timeSpan = TimeSpan.FromSeconds(seconds);
                var timeValues = Model.TimeValue.GetTimeValues(dateTimes, values);
                var result = new List<Model.TimeValue>();
                var ema = new EMA(timeSpan, EmaType.Interpolated);
                foreach (var timeValue in timeValues)
                {
                    var temp = ema.Calculate(timeValue);
                    if (temp != null) result.Add(temp.Clone());
                }
                return Conversion.CreateDictionary(result);
            }

            [Method(Description = "Calculate NEMA ...")]
            public static Dictionary<string, object> CalculateNEMA(
                [Parameter("Vector of Time Stamps")]double[] dateTimes,
                [Parameter("Vector of Values")]object[] values,
                [Parameter("Window in Seconds")]double seconds)
            {
                // convert to list of values
                var timeSpan = TimeSpan.FromSeconds(seconds);
                var timeValues = Model.TimeValue.GetTimeValues(dateTimes, values);
                var result = new List<Model.TimeValue>();
                var nema = new NEMA(timeSpan, 10, EmaType.Interpolated);
                foreach (var timeValue in timeValues)
                {
                    var temp = nema.Calculate(timeValue);
                    if (temp != null) result.Add(temp.Clone());
                }
                return Conversion.CreateDictionary(result);
            }

            [Method(Description = "Calculate MA ...")]
            public static Dictionary<string, object> CalculateMA(
                [Parameter("Vector of Time Stamps")]double[] dateTimes,
                [Parameter("Vector of Values")]object[] values,
                [Parameter("Window in Seconds")]double seconds)
            {
                // convert to list of values
                var timeSpan = TimeSpan.FromSeconds(seconds);
                var timeValues = Model.TimeValue.GetTimeValues(dateTimes, values);
                var result = new List<Model.TimeValue>();
                var ma = new MA(timeSpan, 10);
                foreach (var timeValue in timeValues)
                {
                    var temp = ma.Calculate(timeValue);
                    if (temp != null) result.Add(temp.Clone());
                }
                return Conversion.CreateDictionary(result);
            }

            [Method(Description = "Calculate MSD ...")]
            public static Dictionary<string, object> CalculateMSD(
                [Parameter("Vector of Time Stamps")]double[] dateTimes,
                [Parameter("Vector of Values")]object[] values,
                [Parameter("Window in Seconds")]double seconds)
            {
                // convert to list of values
                var timeValues = Model.TimeValue.GetTimeValues(dateTimes, values);
                var result = new List<Model.TimeValue>();
                var msd = new MSD(seconds, 10);
                foreach (var timeValue in timeValues)
                {
                    var temp = msd.Calculate(timeValue);
                    if (temp != null) result.Add(temp.Clone());
                }
                return Conversion.CreateDictionary(result);
            }

            [Method(Description = "Calculate ZScore ...")]
            public static Dictionary<string, object> CalculateZScore(
                [Parameter("Vector of Time Stamps")]double[] dateTimes,
                [Parameter("Vector of Values")]object[] values,
                [Parameter("Window in Seconds")]double seconds)
            {
                // convert to list of values
                var timeValues = Model.TimeValue.GetTimeValues(dateTimes, values);
                var result = new List<Model.TimeValue>();
                var zscore = new ZScore(seconds, 10);
                foreach (var timeValue in timeValues)
                {
                    var temp = zscore.Calculate(timeValue);
                    if (temp != null) result.Add(temp.Clone());
                }
                return Conversion.CreateDictionary(result);
            }

            [Method(Description = "Calculate Outlier ...")]
            public static Dictionary<string, object> CalculateOutlier(
                [Parameter("Vector of Time Stamps")]double[] dateTimes,
                [Parameter("Vector of Values")]object[] values,
                [Parameter("Window in Seconds")]double seconds,
                [Parameter("Threshold; n times Sigma")]double threshold)
            {
                // convert to list of values
                var timeSpan = TimeSpan.FromSeconds(seconds);
                var timeValues = Model.TimeValue.GetTimeValues(dateTimes, values);
                var result = new List<Model.TimeValue>();
                var otulier = new Outlier(timeSpan, 10, threshold);
                foreach (var timeValue in timeValues)
                {
                    var temp = otulier.Calculate(timeValue);
                    if (temp != null) result.Add(temp.Clone());
                }
                return Conversion.CreateDictionary(result);
            }

            [Method(Description = "Calculate Derivative ...")]
            public static Dictionary<string, object> CalculateDerivative(
                [Parameter("Vector of Time Stamps")]double[] dateTimes,
                [Parameter("Vector of Values")]object[] values,
                [Parameter("Window in Seconds")]double seconds)
            {
                // convert to list of values
                var timeSpan = TimeSpan.FromSeconds(seconds);
                var timeValues = Model.TimeValue.GetTimeValues(dateTimes, values);
                var result = new List<Model.TimeValue>();
                var diff = new Differential(timeSpan, 10);
                foreach (var timeValue in timeValues)
                {
                    var temp = diff.Calculate(timeValue);
                    if (temp != null) result.Add(temp.Clone());
                }
                return Conversion.CreateDictionary(result);
            }

            public static ICalculate GetRealTimeOperator(string operatorTypeName,
                string emaTypeName, double tau, int n1 = 1, int n2 = 10, double delta = 3)

            {
                OperatorType operatorType;
                EmaType emaType;
                if (!Enum.TryParse(operatorTypeName, out operatorType)) operatorType = OperatorType.Ma;
                if (!Enum.TryParse(emaTypeName, out emaType)) emaType = EmaType.Interpolated;
                return GetRealTimeOperator(operatorType, emaType, tau, n1, n2, delta);
            }

            public static ICalculate GetRealTimeOperator(OperatorType operatorType,
                EmaType emaType, double tau, int n1 = 1, int n2 = 10, double delta = 3)

            {
                switch (operatorType)
                {
                    case OperatorType.Ema:
                        return new EMA(tau, emaType);
                    case OperatorType.Nema:
                        return new NEMA(tau, n1, emaType);
                    case OperatorType.Ma:
                        return new MA(tau, n1, n2);
                    case OperatorType.Msd:
                        return new MSD(tau, n1, n2);
                    case OperatorType.ZScore:
                        return new ZScore(tau, n1, n2);
                    case OperatorType.Outlier:
                        return new Outlier(tau, n1, n2, delta);
                    default:
                        return null;
                }
            }

            private static Model.DataVector CalculateAny(string[] dateTimes, double[] values, double seconds)
            {
                return null;
            }

            #region exception and compression

            public class PIExceptionCompression : Calculation
            {
                private readonly PICompression comp;
                private readonly PIException exc;

                public PIExceptionCompression(double ExcThresh, double CompThresh)
                {
                    Threshold = CompThresh;
                    exc = new PIException(ExcThresh);
                    comp = new PICompression(CompThresh);
                }


                public override Model.TimeValue Calculate(Model.TimeValue dv)
                {
                    // 1st exception
                    var dve = exc.Calculate(dv);
                    Model.TimeValue dvc = null;
                    if (dve != null) dvc = comp.Calculate(dve);
                    if (dvc != null)
                    {
                        LastRecordedValue = dvc;
                        queue.Enqueue(dvc);
                    }
                    PreviousValue = dvc;
                    if (queue.Count > 0) return queue.Dequeue();
                    return null;
                }
            }

            public class PIException : Calculation
            {
                public PIException(double Threshold)
                {
                    this.Threshold = Threshold;
                }

                public override Model.TimeValue Calculate(Model.TimeValue dv)
                {
                    // no news here
                    if (!initialized)
                    {
                        LastRecordedValue = dv;
                        PreviousValue = null;
                        initialized = true;
                        return dv;
                    }
                    // Math.Abs(z-this.zp)>this.threshold
                    // this is wrong. it should be the difference between the current
                    // and the last RECORDED value.
                    // 18-Feb-2011
                    if (Math.Abs(dv.Value - LastRecordedValue.Value) > Threshold)
                    {
                        LastRecordedValue = dv;
                        if (PreviousValue != null) queue.Enqueue(PreviousValue);
                        queue.Enqueue(dv);
                        PreviousValue = null;
                    }
                    else
                    {
                        PreviousValue = dv;
                    }

                    if (queue.Count > 0) return queue.Dequeue();
                    return null;
                }
            }

            public class SimpleException : Calculation
            {
                public SimpleException(double Threshold)
                {
                    this.Threshold = Threshold;
                }

                public override Model.TimeValue Calculate(Model.TimeValue dv)
                {
                    if (!initialized)
                    {
                        LastRecordedValue = dv;
                        initialized = true;
                        return dv;
                    }
                    if (Math.Abs(dv.Value - LastRecordedValue.Value) > Threshold)
                    {
                        LastRecordedValue = dv;
                        PreviousValue = null;
                        queue.Enqueue(dv);
                    }
                    else
                    {
                        PreviousValue = dv;
                    }
                    if (queue.Count > 0) return queue.Dequeue();
                    return null;
                }
            }

            public class PICompression : Calculation
            {
                public Model.TimeValue ArchivedPoint;
                public Model.TimeValue HeldPoint;
                public double UpperSlope, LowerSlope;

                public PICompression(double Threshold)
                {
                    this.Threshold = Threshold;
                }

                public override Model.TimeValue Calculate(Model.TimeValue dv)
                {
                    if (!initialized)
                    {
                        initialized = true;
                        ArchivedPoint = dv;
                        LastRecordedValue = dv;
                        return dv;
                    }
                    // no valid held point
                    if (ArchivedPoint == null)
                    {
                        ArchivedPoint = dv;
                        queue.Enqueue(dv);
                    }
                    else if (HeldPoint == null)
                    {
                        HeldPoint = dv;
                        UpperSlope = LinearRegression(ArchivedPoint, HeldPoint, Threshold);
                        LowerSlope = LinearRegression(ArchivedPoint, HeldPoint, -Threshold);
                        return null;
                    }
                    // archived point, held point and current point exist
                    // calculate new slopws
                    else
                    {
                        var PointSlope = LinearRegression(ArchivedPoint, dv, 0);
                        var NewUpperSlope = LinearRegression(ArchivedPoint, dv, Threshold);
                        var NewLowerSlope = LinearRegression(ArchivedPoint, dv, -Threshold);
                        if (PointSlope <= UpperSlope && PointSlope >= LowerSlope)
                        {
                            UpperSlope = Math.Min(UpperSlope, NewUpperSlope);
                            LowerSlope = Math.Max(LowerSlope, NewLowerSlope);
                            HeldPoint = dv;
                        }
                        else
                        {
                            ArchivedPoint = HeldPoint;
                            HeldPoint = dv;
                            UpperSlope = LinearRegression(ArchivedPoint, HeldPoint, Threshold);
                            LowerSlope = LinearRegression(ArchivedPoint, HeldPoint, -Threshold);
                            LastRecordedValue = dv;
                            queue.Enqueue(ArchivedPoint);
                        }
                    }
                    PreviousValue = dv;
                    if (queue.Count > 0) return queue.Dequeue();
                    return null;
                }

                private double LinearRegression(Model.TimeValue First, Model.TimeValue Last, double Threshold)
                {
                    return (Last.Value + Threshold - First.Value) / (Last.TimeStamp - First.TimeStamp)
                           .TotalSeconds;
                }
            }

            #endregion

            #region real time operator

            public class EMA : ICalculate
            {
                private readonly double tau;
                private readonly EmaType Type;
                private Model.TimeValue dvp;
                private Model.TimeValue ema;
                private bool initialized;
                private double nu, mu, alpha, delta;

                public EMA(TimeSpan tau, EmaType Type)
                {
                    this.tau = tau.TotalSeconds;
                    this.Type = Type;
                }

                public EMA(double tauInMilliSeconds, EmaType Type)
                {
                    tau = tauInMilliSeconds;
                    this.Type = Type;
                }

                public Model.TimeValue Calculate(Model.TimeValue dv)
                {
                    // take care of Double.NaN, Inf and others
                    if (double.IsNaN(dv.Value) || double.IsInfinity(dv.Value))
                        return null;

                    if (!initialized)
                    {
                        dvp = dv;
                        ema = dv;
                        initialized = true;
                        return ema;
                    }
                    delta = (dv.TimeStamp - dvp.TimeStamp).TotalSeconds;
                    // this shouldn't happen
                    if (delta <= 0)
                        delta = new TimeSpan(0, 0, 0, 0, 1).TotalSeconds;
                    alpha = delta / tau;
                    mu = Math.Exp(-alpha);
                    switch (Type)
                    {
                        case EmaType.Interpolated:
                            nu = (1 - mu) / alpha;
                            break;
                        case EmaType.Next:
                            nu = mu;
                            break;
                        case EmaType.Previous:
                            nu = 1;
                            break;
                        default:
                            throw new ApplicationException("interpolation setting not defined");
                    }
                    ema.TimeStamp = dv.TimeStamp;
                    ema.Value = mu * ema.Value + (nu - mu) * dvp.Value + (1 - nu) * dv.Value;
                    // update tp, zp
                    dvp = dv;
                    return ema;
                }
            }

            public class NEMA : ICalculate
            {
                private int n;
                private double tau;

                public NEMA(double tauInMilliSeconds, int n, EmaType Type)
                {
                    Initialize(tauInMilliSeconds, n, Type);
                }

                public NEMA(TimeSpan tau, int n, EmaType Type)
                {
                    Initialize(tau.TotalSeconds, n, Type);
                }

                public List<EMA> emas { get; set; }

                public Model.TimeValue Calculate(Model.TimeValue dv)
                {
                    // take care of Double.NaN, Inf and others
                    if (double.IsNaN(dv.Value) || double.IsInfinity(dv.Value))
                        return null;
                    var Last = dv;
                    for (var r = 0; r < n; r++)
                        Last = emas[r].Calculate(Last);
                    return Last;
                }

                public void Initialize(double tauInMilliSeconds, int n, EmaType Type)
                {
                    this.n = n;
                    // NEMA has range tau
                    tau = tauInMilliSeconds / this.n;
                    emas = new List<EMA> {new EMA(tau, Type)};
                    for (var r = 1; r < n; r++)
                        emas.Add(new EMA(tau, 0));
                }
            }

            public class MA : ICalculate
            {
                public MA(double tau, int n1, int n2)
                {
                    init(tau, n1, n2);
                }

                public MA(TimeSpan tau, int n1, int n2)
                {
                    init(tau.TotalSeconds, n1, n2);
                }

                public MA(TimeSpan tau, int n2)
                {
                    init(tau.TotalSeconds, 1, n2);
                }

                public List<NEMA> nemas { get; set; }
                public int n1 { get; set; }
                public int n2 { get; set; }
                public double tau { get; set; }
                public double delay { get; set; }
                public double LastValue { get; set; }

                public Model.TimeValue Calculate(Model.TimeValue dv)
                {
                    // take care of Double.NaN, Inf and others
                    if (double.IsNaN(dv.Value) || double.IsInfinity(dv.Value))
                        return null;
                    // initialization using Summary
                    double nsum = 0;
                    var Last = dv;
                    for (var r = 0; r < n2 - n1 + 1; r++)
                        nsum = nsum + nemas[r].Calculate(dv).Value;
                    Last.Value = nsum / (n2 - (double) n1 + 1);
                    return Last;
                }

                public void init(double tau, int n1, int n2)
                {
                    this.n1 = n1;
                    this.n2 = n2;
                    this.tau = tau / (n1 + n2);
                    nemas = new List<NEMA>();
                    // i = 0 -> just interpolation scheme
                    // should be extended to other cases
                    for (var r = this.n1 - 1; r < this.n2; r++)
                        nemas.Add(new NEMA(new TimeSpan(0, 0, 0, 0, Convert.ToInt32(this.tau)), r, 0));
                }
            }

            public class MSD : ICalculate
            {
                public double LastValue;

                public MSD(double tau, int n1, int n2)
                {
                    init(tau, n1, n2);
                }

                public MSD(double tau, int n2)
                {
                    init(tau, 0, n2);
                }

                public MSD(TimeSpan tau, int n1, int n2)
                {
                    init(tau.TotalSeconds, n1, n2);
                }

                public MSD(TimeSpan tau, int n2)
                {
                    init(tau.TotalSeconds, 1, n2);
                }

                private Model.TimeValue zdp { get; set; }
                public double map { get; set; }
                public double msdp { get; set; }
                private MA ma0 { get; set; }
                private MA ma1 { get; set; }

                public Model.TimeValue Calculate(Model.TimeValue dv)
                {
                    // take care of Double.NaN, Inf and others
                    if (double.IsNaN(dv.Value) || double.IsInfinity(dv.Value))
                        return null;
                    // initialization is done on the ma level
                    zdp = dv;
                    var Last = dv;
                    zdp.Value = Math.Pow(dv.Value - ma0.Calculate(dv).Value, 2);
                    Last.Value = Math.Sqrt(ma1.Calculate(zdp).Value);
                    if (Last.Value <= 0) Last.Value = 1e-6; //shouldn't happen
                    return Last;
                }

                public void init(double tau, int n1, int n2)
                {
                    ma0 = new MA(tau, n1, n2);
                    ma1 = new MA(tau, n1, n2);
                }
            }

            public class Outlier : ICalculate
            {
                private bool initialized;
                private Model.TimeValue last;

                public Outlier(double tau, int n1, int n2, double delta)
                {
                    init(tau, n1, n2, delta);
                }

                public Outlier(double tau, int n2, double delta)
                {
                    init(tau, 1, n2, delta);
                }

                public Outlier(TimeSpan tau, int n2, double delta)
                {
                    init(tau.TotalSeconds, 1, n2, delta);
                }

                private ZScore zscore { get; set; }
                private double delta { get; set; }

                public Model.TimeValue Calculate(Model.TimeValue dv)
                {
                    // take care of Double.NaN, Inf and others
                    if (double.IsNaN(dv.Value) || double.IsInfinity(dv.Value))
                        return null;
                    if (!initialized)
                    {
                        initialized = true;
                        last = zscore.Calculate(dv);
                        return zscore.Calculate(dv);
                    }
                    var temp = Math.Abs((dv.Value - zscore.ma0.LastValue) / zscore.msd0.LastValue);
                    if (dv.Value > temp) return null;
                    zscore.Calculate(dv); // calculate zscore
                    return dv; // return value
                }

                public void init(double tau, int n1, int n2, double delta)
                {
                    zscore = new ZScore(tau, n1, n2);
                    this.delta = delta;
                }
            }

            public class ZScore : ICalculate
            {
                public ZScore(double tau, int n1, int n2)
                {
                    init(tau, n1, n2);
                }

                public ZScore(TimeSpan tau, int n1, int n2)
                {
                    init(tau.TotalSeconds, n1, n2);
                }

                public ZScore(double tau, int n2)
                {
                    init(tau, 1, n2);
                }

                public MA ma0 { get; private set; }
                public MSD msd0 { get; private set; }

                public Model.TimeValue Calculate(Model.TimeValue dv)
                {
                    // take care of Double.NaN, Inf and others
                    if (double.IsNaN(dv.Value) || double.IsInfinity(dv.Value))
                        return null;
                    // initialization is done on the ma level
                    var currentma = ma0.Calculate(dv);
                    var currentmsd = msd0.Calculate(dv);
                    var Last = new Model.TimeValue(dv.TimeStamp, -1);
                    Last.Value = (dv.Value - currentma.Value) / currentmsd.Value;
                    return Last;
                }

                public void init(double tau, int n1, int n2)
                {
                    ma0 = new MA(tau, n1, n2);
                    msd0 = new MSD(tau, n1, n2);
                }
            }

            public class Differential : ICalculate
            {
                public Differential(TimeSpan tau, int n1, int n2)
                {
                    init(tau.TotalSeconds, n1, n2);
                }

                public Differential(TimeSpan tau, int n2)
                {
                    init(tau.TotalSeconds, 1, n2);
                }

                private NEMA Nema1 { get; set; }
                private NEMA Nema2 { get; set; }
                private NEMA Nema4 { get; set; }
                private double Gamma { get; set; }

                public Model.TimeValue Calculate(Model.TimeValue dv)
                {
                    // take care of Double.NaN, Inf and others
                    if (double.IsNaN(dv.Value) || double.IsInfinity(dv.Value))
                        return null;
                    // initialization is done on the ma level
                    var currentnema1 = Nema1.Calculate(dv);
                    var currentnema2 = Nema2.Calculate(dv);
                    var currentnema4 = Nema4.Calculate(dv);
                    var last = new Model.TimeValue(dv.TimeStamp, -1)
                    {
                        Value = Gamma * (currentnema1.Value + currentnema2.Value -
                                         2 * currentnema4.Value)
                    };
                    return last;
                }

                public void init(double tau, int n1, int n2)
                {
                    Gamma = 1.22208;
                    var beta = 0.65;
                    var alpha = 1 / (Gamma * (8 * beta - 3));
                    Nema1 = new NEMA(TimeSpan.FromMilliseconds(alpha * tau), 1, EmaType.Interpolated);
                    Nema2 = new NEMA(TimeSpan.FromMilliseconds(alpha * tau), 2, EmaType.Interpolated);
                    Nema4 = new NEMA(TimeSpan.FromMilliseconds(alpha * beta * tau), 4,
                        EmaType.Interpolated);
                }
            }

            #endregion
        }
    }
}