#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.DataLogger;
using FTOptix.NativeUI;
using FTOptix.WebUI;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
using FTOptix.Alarm;
using FTOptix.Core;
using System.Collections.Generic;
using System.Linq;
using FTOptix.Report;
#endregion

public class BPCS_Calculate : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    public LoopResult CalculateLoopResult(LoopData data)
    {
        var loopResult = new LoopResult
        {
            ManualPercent = CalculateManualPercent(data.TimeSeries),
            TuningRequired = CalculateTuningRequired(data.TimeSeries),
            OperatorInterventions = CalculateOperatorInterventions(data.TimeSeries),
            TimesOutsideGuide = CalculateTimesOutsideGuide(data),
            MaxPV = data.TimeSeries.Max(ts => ts.ProcessValue),
            MinPV = data.TimeSeries.Min(ts => ts.ProcessValue),
            MaxCV = data.TimeSeries.Max(ts => ts.ControlValue),
            MinCV = data.TimeSeries.Min(ts => ts.ControlValue),
            MaintenanceDate = data.MaintenanceDate,
            DaysToMaintenance = CalculateDaysToMaintenance(data.MaintenanceDate),
            PVStandardDeviation = CalculateStandardDeviation(data.TimeSeries.Select(ts => ts.ProcessValue)),
            CVStandardDeviation = CalculateStandardDeviation(data.TimeSeries.Select(ts => ts.ControlValue)),
            PVMean = data.TimeSeries.Average(ts => ts.ProcessValue),
            CVMean = data.TimeSeries.Average(ts => ts.ControlValue),
            Correlation = CalculateCorrelation(data.TimeSeries),
            Covariance = CalculateCovariance(data.TimeSeries),
            IsSaturated = false,
            PFD = 0.1
        };

        loopResult.PFD = CalculatePFD(loopResult, data);
        loopResult.IsSaturated = CalculateIsSaturated(loopResult, data.TimeSeries);

        return loopResult;
    }

    private double CalculateManualPercent(List<TimeSeriesEntry> timeSeries)
    {
        var totalTime = timeSeries.Last().Timestamp - timeSeries.First().Timestamp;
        var totalManualTime = timeSeries.Where(ts => ts.Mode)
                                        .Select((ts, i) => i == 0 ? TimeSpan.Zero : ts.Timestamp - timeSeries[i - 1].Timestamp)
                                        .Aggregate(TimeSpan.Zero, (sum, interval) => sum + interval);
        return totalManualTime.TotalSeconds / totalTime.TotalSeconds;
    }

    private int CalculateOperatorInterventions(List<TimeSeriesEntry> timeSeries)
    {
        return timeSeries.Skip(1).Count(ts => ts.Mode && ts.ControlValue != timeSeries[timeSeries.IndexOf(ts) - 1].ControlValue);
    }

    private int CalculateTimesOutsideGuide(LoopData data)
    {
        var outsideGuideCount = 0;
        var wasOutside = false;

        foreach (var ts in data.TimeSeries)
        {
            var isOutside = ts.ControlValue < data.StaticInfo.LowerControlGuide || ts.ControlValue > data.StaticInfo.UpperControlGuide;

            if (isOutside && !wasOutside)
            {
                outsideGuideCount++;
            }

            wasOutside = isOutside;
        }

        return outsideGuideCount;
    }

    private int CalculateDaysToMaintenance(DateTime maintenanceDate)
    {
        return (maintenanceDate - DateTime.Now).Days;
    }

    private double CalculatePFD(LoopResult loopResult, LoopData data)
    {
        var f1 = loopResult.OperatorInterventions > 10 ? 10 : 1;
        var f2 = loopResult.DaysToMaintenance < 0 ? 10 : 1;
        var f3 = loopResult.TimesOutsideGuide > 10 ? 10 : 1;
        var f4 = loopResult.IsSaturated ? 10 : 1;

        return f1 * f2 * f3 * f4 * 0.0001;
    }

    private bool CalculateIsSaturated(LoopResult loopResult, List<TimeSeriesEntry> timeSeries)
    {
        var deltaSaturation = 0.005 * (loopResult.MaxCV - loopResult.MinCV);
        var saturatedPoints = timeSeries.Where(ts => ts.ControlValue > loopResult.MaxCV - deltaSaturation ||
                                                     ts.ControlValue < loopResult.MinCV + deltaSaturation)
                                        .Select(ts => ts.ControlValue)
                                        .ToList();

        if (saturatedPoints.Count >= 6)
        {
            var saturationIndex = saturatedPoints.Average();
            return saturationIndex > 0;
        }

        return false;
    }

    private bool CalculateTuningRequired(List<TimeSeriesEntry> timeSeries)
    {
        if (timeSeries.Count < 10)
        {
            return false;
        }

        var lastTenCV = timeSeries.Skip(Math.Max(0, timeSeries.Count - 10))
                                  .Select(ts => ts.ControlValue)
                                  .ToList();

        var meanWindowCV = lastTenCV.Average();
        var noise = Math.Abs(lastTenCV.Last() - meanWindowCV) / lastTenCV.Last();

        if (noise > 0.1)
        {
            return true;
        }

        var mse = timeSeries.Average(ts => Math.Pow(ts.Setpoint - ts.ProcessValue, 2)) / timeSeries.Last().Setpoint;

        return mse > 0.5;
    }

    private double CalculateStandardDeviation(IEnumerable<double> values)
    {
        var mean = values.Average();
        return Math.Sqrt(values.Average(v => Math.Pow(v - mean, 2)));
    }

    private double CalculateCorrelation(List<TimeSeriesEntry> timeSeries)
    {
        var pvValues = timeSeries.Select(ts => ts.ProcessValue).ToArray();
        var cvValues = timeSeries.Select(ts => ts.ControlValue).ToArray();
        var covariance = CalculateCovariance(timeSeries);
        var pvStandardDeviation = CalculateStandardDeviation(pvValues);
        var cvStandardDeviation = CalculateStandardDeviation(cvValues);

        return covariance / (pvStandardDeviation * cvStandardDeviation);
    }

    private double CalculateCovariance(List<TimeSeriesEntry> timeSeries)
    {
        var pvValues = timeSeries.Select(ts => ts.ProcessValue).ToArray();
        var cvValues = timeSeries.Select(ts => ts.ControlValue).ToArray();
        var pvMean = pvValues.Average();
        var cvMean = cvValues.Average();

        return pvValues.Zip(cvValues, (pv, cv) => (pv - pvMean) * (cv - cvMean)).Average();
    }
}



public class TimeSeriesEntry
{
    public int LoopId { get; set; }
    public DateTime Timestamp { get; set; }
    public double ProcessValue { get; set; }
    public double ControlValue { get; set; }
    public double Setpoint { get; set; }
    public double UpperControlGuide { get; set; }
    public double LowerControlGuide { get; set; }
    public bool Mode { get; set; }
}

public class LoopData
{
    public int LayerId { get; set; }
    public int ScenarioId { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public StaticInfo StaticInfo { get; set; }
    public List<TimeSeriesEntry> TimeSeries { get; set; }
    public DateTime MaintenanceDate { get; set; }
    public Dictionary<string, double> Parameters { get; set; }
}

public class StaticInfo
{
    public double UpperControlGuide { get; set; }
    public double LowerControlGuide { get; set; }
}

public class LoopResult
{
    public double ManualPercent { get; set; }
    public bool TuningRequired { get; set; }
    public int OperatorInterventions { get; set; }
    public int TimesOutsideGuide { get; set; }
    public double MaxPV { get; set; }
    public double MinPV { get; set; }
    public double MaxCV { get; set; }
    public double MinCV { get; set; }
    public DateTime MaintenanceDate { get; set; }
    public int DaysToMaintenance { get; set; }
    public double PVStandardDeviation { get; set; }
    public double CVStandardDeviation { get; set; }
    public double PVMean { get; set; }
    public double CVMean { get; set; }
    public double Correlation { get; set; }
    public double Covariance { get; set; }
    public bool IsSaturated { get; set; }
    public double PFD { get; set; }
}

