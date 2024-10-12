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
using FTOptix.Report;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
using FTOptix.Alarm;
using FTOptix.Core;
using static System.Formats.Asn1.AsnWriter;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Linq;
using System.Reflection.PortableExecutable;
using FTOptix.ODBCStore;
#endregion

public class BPCS_Calculation : BaseNetLogic
{
    Store store, hist_db, main_db;
    List<ControlLoop> loops;

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        store = Project.Current.Get<Store>("DataStores/ResultsDB");
        hist_db = Project.Current.Get<Store>("DataStores/Historics");
        main_db = Project.Current.Get<Store>("DataStores/MainDatabase");
        Calculate();
    }

    [ExportMethod]
    public void Calculate()
    {
        store.Query("DELETE FROM \"BPCS G Historic\"", out _, out _);
        Log.Info("Calculating", "BPCS");
        var query = "SELECT Timestamp, SUM(Mode) AS ModeCount, ProcessValue, ControlValue, SetPoint, Mode, Asset FROM BPCS_Historics GROUP BY Timestamp ORDER BY Timestamp";
        hist_db.Query(query, out string[] headers, out object[,] result);
        Table scenariosTable = store.Tables.Get<Table>("BPCS G Historic");
        string[] scColumns = { "Timestamp", "LocalTimestamp", "Count" };
        List<BPCS_TimeSeriesEntry> bpcs_entries = new List<BPCS_TimeSeriesEntry>();
        for (int i = 0; i < result.Length / headers.Length; i++)
        {
            var scValues = new object[1, 3];
            scValues[0, 0] = result[i, 0];
            scValues[0, 1] = result[i, 0];
            scValues[0, 2] = result[i, 1];
            scenariosTable.Insert(scColumns, scValues);
            var entrypoint = new BPCS_TimeSeriesEntry()
            {
                Timestamp = (DateTime)result[i, 0],
                ProcessValue = (double)result[i, 2],
                ControlValue = (double)result[i, 3],
                Setpoint = (double)result[i, 4],
                Mode = Convert.ToBoolean((long)result[i, 5]),
                LoopId = result[i, 6].ToString()
            };
            bpcs_entries.Add(entrypoint);
        }
        main_db.Query("SELECT * FROM BPCS_Layers ORDER BY LoopID", out headers, out result);

        loops = new List<ControlLoop>();
        for (int i = 0; i < result.Length / headers.Length; i++)
        {
            var loop = new ControlLoop()
            {
                LoopID = (string)result[i, 0],
                UpperControlGuide = (double)result[i, 1],
                LowerControlGuide = (double)result[i, 2],
                Maintenance = (string)result[i, 3],
                MaintenanceDone = (string)result[i, 4],
                KP = (double)result[i, 5],
                KI = (double)result[i, 6],
                KD = (double)result[i, 7],
                Scenario = (string)result[i, 8]
            };
            loops.Add(loop);
        }

        foreach (var item in loops)
        {
            item.timeseries = bpcs_entries.Where(loop => loop.LoopId == item.LoopID).ToList();
            var calc_result = CalculateLoopResult(item);
            Log.Info("Result", calc_result.ToString());
            item.result = calc_result;
        }
        if (loops.Count > 0)
            CalculateGeneral(loops);

        var trendId = LogicObject.GetVariable("Trend").Value.Value;
        var trend = InformationModel.Get<Trend>((NodeId)trendId);
        trend.XAxis.Window = (uint)((DateTime.Now - bpcs_entries.FirstOrDefault().Timestamp).TotalMilliseconds);
        trend.YAxis.MaxValue = loops.Count + 2;
    }

    [ExportMethod]
    public void SelectedAsset(string LoopId)
    {
        var result_object = InformationModel.Get<BPCS>((NodeId)LogicObject.GetVariable("ResultObject").Value.Value);
        Log.Info("SelectedLoop", LoopId);
        var finded_loop = loops.Find(loop => loop.LoopID == LoopId);
        LoopResult result = finded_loop.result;
        result_object.manual_percent = (float)Math.Round((float)result.ManualPercent, 1);
        result_object.tuning_required = result.TuningRequired;
        result_object.opeator_interventions = result.OperatorInterventions;
        result_object.times_outside_guide = result.TimesOutsideGuide;
        result_object.max_pv = (float)result.MaxPV;
        result_object.min_pv = (float)result.MinPV;
        result_object.max_cv = (float)result.MaxCV;
        result_object.min_cv = (float)result.MinCV;
        result_object.maint_date = "2024-10-11";
        result_object.days_to_maint = (DateTime.Parse("2024-10-11") - DateTime.Now).Days;
        result_object.pv_standard_deviation = (float)result.PVStandardDeviation;
        result_object.cv_standard_deviation = (float)result.CVStandardDeviation;
        result_object.pv_media = (float)result.PVMean;
        result_object.cv_media = (float)result.CVMean;
        result_object.correlation = (float)result.Correlation;
        result_object.covariance = (float)result.Covariance;
        result_object.is_saturated = result.IsSaturated;
        result_object.PFD = (float)result.PFD;
        var scatterplot = new float[2,100];

        for (int i = 0; i < 100; i++)
        {
            if(i<finded_loop.timeseries.Count)
            {
                scatterplot[0, i] = (float)finded_loop.timeseries[i].ControlValue;
                scatterplot[1, i] = (float)finded_loop.timeseries[i].ProcessValue;
            }
            else
            {
                scatterplot[0, i] = 0;
                scatterplot[1, i] = 0;
            }
        }
        result_object.Scatter = scatterplot;
    }


    public LoopResult CalculateLoopResult(ControlLoop data)
    {
        if (data.timeseries.Count == 0)
            return new LoopResult();

        var maint = DateTime.Now;
        if (data.Maintenance != null || data.Maintenance != "")
            _ = DateTime.TryParse(data.Maintenance, out maint);

        var loopResult = new LoopResult
        {
            ManualPercent = CalculateManualPercent(data.timeseries),
            TuningRequired = CalculateTuningRequired(data.timeseries),
            OperatorInterventions = CalculateOperatorInterventions(data.timeseries),
            TimesOutsideGuide = CalculateTimesOutsideGuide(data, data.timeseries),
            MaxPV = data.timeseries.Max(ts => ts.ProcessValue),
            MinPV = data.timeseries.Min(ts => ts.ProcessValue),
            MaxCV = data.timeseries.Max(ts => ts.ControlValue),
            MinCV = data.timeseries.Min(ts => ts.ControlValue),
            MaintenanceDate = maint,
            DaysToMaintenance = CalculateDaysToMaintenance(maint),
            PVStandardDeviation = CalculateStandardDeviation(data.timeseries.Select(ts => ts.ProcessValue)),
            CVStandardDeviation = CalculateStandardDeviation(data.timeseries.Select(ts => ts.ControlValue)),
            PVMean = data.timeseries.Average(ts => ts.ProcessValue),
            CVMean = data.timeseries.Average(ts => ts.ControlValue),
            Correlation = CalculateCorrelation(data.timeseries),
            Covariance = CalculateCovariance(data.timeseries),
            IsSaturated = false,
            PFD = 0.1
        };

        loopResult.PFD = CalculatePFD(loopResult);
        loopResult.IsSaturated = CalculateIsSaturated(loopResult, data.timeseries);

        return loopResult;
    }

    private double CalculateManualPercent(List<BPCS_TimeSeriesEntry> timeSeries)
    {
        var totalTime = timeSeries.Last().Timestamp - timeSeries.First().Timestamp;
        var totalManualTime = timeSeries.Where(ts => ts.Mode)
                                        .Select((ts, i) => i == 0 ? TimeSpan.Zero : ts.Timestamp - timeSeries[i - 1].Timestamp)
                                        .Aggregate(TimeSpan.Zero, (sum, interval) => sum + interval);
        return totalManualTime.TotalSeconds / totalTime.TotalSeconds;
    }

    private int CalculateOperatorInterventions(List<BPCS_TimeSeriesEntry> timeSeries)
    {
        return timeSeries.Skip(1).Count(ts => ts.Mode && ts.ControlValue != timeSeries[timeSeries.IndexOf(ts) - 1].ControlValue);
    }

    private int CalculateTimesOutsideGuide(ControlLoop data, List<BPCS_TimeSeriesEntry> timeseries)
    {
        var outsideGuideCount = 0;
        var wasOutside = false;

        foreach (var ts in timeseries)
        {
            var isOutside = ts.ControlValue < data.LowerControlGuide || ts.ControlValue > data.UpperControlGuide;

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

    private double CalculatePFD(LoopResult loopResult)
    {
        var f1 = loopResult.OperatorInterventions > 10 ? 10 : 1;
        var f2 = loopResult.DaysToMaintenance < 0 ? 10 : 1;
        var f3 = loopResult.TimesOutsideGuide > 10 ? 10 : 1;
        var f4 = loopResult.IsSaturated ? 10 : 1;

        return f1 * f2 * f3 * f4 * 0.0001;
    }

    private bool CalculateIsSaturated(LoopResult loopResult, List<BPCS_TimeSeriesEntry> timeSeries)
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

    private bool CalculateTuningRequired(List<BPCS_TimeSeriesEntry> timeSeries)
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

    private double CalculateCorrelation(List<BPCS_TimeSeriesEntry> timeSeries)
    {
        var pvValues = timeSeries.Select(ts => ts.ProcessValue).ToArray();
        var cvValues = timeSeries.Select(ts => ts.ControlValue).ToArray();
        var covariance = CalculateCovariance(timeSeries);
        var pvStandardDeviation = CalculateStandardDeviation(pvValues);
        var cvStandardDeviation = CalculateStandardDeviation(cvValues);

        return covariance / (pvStandardDeviation * cvStandardDeviation);
    }

    private double CalculateCovariance(List<BPCS_TimeSeriesEntry> timeSeries)
    {
        var pvValues = timeSeries.Select(ts => ts.ProcessValue).ToArray();
        var cvValues = timeSeries.Select(ts => ts.ControlValue).ToArray();
        var pvMean = pvValues.Average();
        var cvMean = cvValues.Average();

        return pvValues.Zip(cvValues, (pv, cv) => (pv - pvMean) * (cv - cvMean)).Average();
    }


    void CalculateGeneral(List<ControlLoop> layersBPCS)
    {
        var loops2Calculate = layersBPCS.Where(res => res.timeseries.Count>0).ToList();
        TotalResults totalResults = new TotalResults
        {
            CurrentManual = CalculateCurrentManual(loops2Calculate),
            CurrentOutsideGuide = CalculateOutsideControlGuideCount(loops2Calculate),
            CurrentSaturated = CalculateSaturatedLoops(loops2Calculate),
            ManualPercentage = CalculateManualPercentage(loops2Calculate),
            ManualThisMonth = CalculateManualHistorics(loops2Calculate),
            SaturatedThisMonth = CalculateSaturatedMonth(loops2Calculate),
            RetunedThisMonth = LoopsRetuned(loops2Calculate),
            RetuningRequired = CalculateRetuningRequired(loops2Calculate),
            TopError = CalculateError(loops2Calculate),
            TopNoise = CalculateNoise(loops2Calculate),
        };
        var resultObject = Project.Current.Get<BCPS_General_Result>("Model/Results/BCPS_General_Result");

        resultObject.current_manual_count = totalResults.CurrentManual.Count;
        resultObject.current_outside_guide_count = totalResults.CurrentOutsideGuide.Count;
        resultObject.current_saturated_count = (short)totalResults.CurrentSaturated.Count;
        resultObject.manual_percentage = (float)totalResults.ManualPercentage;
        resultObject.manual_this_month_count = totalResults.ManualThisMonth.Count;
        resultObject.retuned_this_month_count = totalResults.RetunedThisMonth;
        resultObject.synth_required = totalResults.RetuningRequired.Count;
        resultObject.Distribution.GetVariable("Manual").Value = totalResults.ManualPercentage;
        resultObject.Distribution.GetVariable("Auto").Value = 100-totalResults.ManualPercentage;

        foreach (IUANode item in resultObject.top_error.Children)
            resultObject.top_error.Remove(item);


        foreach (var item in totalResults.TopError)
        {
            IUAVariable newVariable = InformationModel.MakeVariable(item.Name, OpcUa.DataTypes.Float);
            newVariable.Value = new UAValue(item.value);
            resultObject.top_error.Add(newVariable);
        }

        foreach (IUANode item in resultObject.top_noise.Children)
            resultObject.top_noise.Remove(item);

        foreach (var item in totalResults.TopNoise)
        {
            IUAVariable newVariable = InformationModel.MakeVariable(item.Name, OpcUa.DataTypes.Float);
            newVariable.Value = new UAValue(item.value);
            resultObject.top_noise.Add(newVariable);
        }
    }

    private static List<string> CalculateCurrentManual(List<ControlLoop> layersBPCS)
    {
        var currentManual = new List<string>();
        foreach (var layer in layersBPCS)
        {
            var lastEntry = layer.timeseries.Last();
            if (lastEntry.ControlValue > 0 && !currentManual.Contains(layer.LoopID))
            {
                currentManual.Add(layer.LoopID);
            }
        }
        return currentManual;
    }

    private static List<string> CalculateOutsideControlGuideCount(List<ControlLoop> layersBPCS)
    {
        var currentOutsideGuide = new List<string>();
        foreach (var layer in layersBPCS)
        {
            var lastEntry = layer.timeseries.Last();
            double controlValue = lastEntry.ControlValue;
            double upperGuide = layer.UpperControlGuide;
            double lowerGuide = layer.LowerControlGuide;

            if (controlValue < lowerGuide || controlValue > upperGuide)
            {
                currentOutsideGuide.Add(layer.LoopID);
            }
        }
        return currentOutsideGuide;
    }

    private static List<string> CalculateSaturatedLoops(List<ControlLoop> layersBPCS)
    {
        var currentSaturatedLoops = new List<string>();
        foreach (var layer in layersBPCS)
        {
            if (layer.result.IsSaturated)
            {
                currentSaturatedLoops.Add(layer.LoopID);
            }
        }
        return currentSaturatedLoops;
    }

    private static double CalculateManualPercentage(List<ControlLoop> layersBPCS)
    {
        double manualPercentage = 0;
        foreach (var layer in layersBPCS)
        {
            manualPercentage += layer.result.ManualPercent;
        }
        return manualPercentage / layersBPCS.Count;
    }

    private static List<Dictionary<string, object>> CalculateManualHistoricsCount(List<ControlLoop> layersBPCS)
    {
        var manualHistoric = new List<string>();
        var dictManualHistoric = new List<Dictionary<string, object>>();
        DateTime now = DateTime.Now;

        foreach (var layer in layersBPCS)
        {
            if (layer.result.ManualPercent > 0 && !manualHistoric.Contains(layer.LoopID))
            {
                manualHistoric.Add(layer.LoopID);
                dictManualHistoric.Add(new Dictionary<string, object>
                {
                    { "_time", now.ToString("yyyy-MM-ddTHH:mm:ss") },
                    { "count", manualHistoric.Count }
                });
            }
        }
        return dictManualHistoric;
    }

    private static List<string> CalculateManualHistorics(List<ControlLoop> layersBPCS)
    {
        var manualHistoric = new List<string>();
        foreach (var layer in layersBPCS)
        {
            if (layer.result.ManualPercent > 0 && !manualHistoric.Contains(layer.LoopID))
            {
                manualHistoric.Add(layer.LoopID);
            }
        }
        return manualHistoric;
    }

    private static List<string> CalculateSaturatedMonth(List<ControlLoop> layersBPCS)
    {
        var saturatedMonth = new List<string>();
        foreach (var layer in layersBPCS)
        {
            if (layer.result.IsSaturated && !saturatedMonth.Contains(layer.LoopID))
            {
                saturatedMonth.Add(layer.LoopID);
            }
        }
        return saturatedMonth;
    }

    private static List<NameValuePair> CalculateError(List<ControlLoop> layersBPCS)
    {
        var topError = new List<NameValuePair>();
        foreach (var layer in layersBPCS)
        {
            var lastEntry = layer.timeseries.Last();
            double absoluteError = Math.Abs(lastEntry.ProcessValue - lastEntry.Setpoint);
            topError.Add(new NameValuePair { Name = layer.LoopID, value = absoluteError });
        }
        return topError.OrderByDescending(e => e.value).ToList();
    }

    private static List<NameValuePair> CalculateNoise(List<ControlLoop> layersBPCS)
    {
        var topNoise = new List<NameValuePair>();
        foreach (var layer in layersBPCS)
        {
            if (layer.timeseries.Count >= 10)
            {
                var lastTenCV = layer.timeseries.Skip(layer.timeseries.Count - 10).Select(ts => ts.ControlValue).ToList();
                double meanWindowCV = lastTenCV.Average();
                double noise = Math.Abs(layer.timeseries.Last().ControlValue - meanWindowCV);
                topNoise.Add(new NameValuePair { Name = layer.LoopID, value = noise });
            }
        }
        return topNoise.OrderByDescending(n => n.value).ToList();
    }

    private static int LoopsRetuned(List<ControlLoop> layersBPCS)
    {
        //int counterLoopsRetuned = 0;
        //var originalParams = new Dictionary<string, Dictionary<string, double>>();

        //foreach (var layer in layersBPCS)
        //{
        //    string loopName = layer.LoopID;
        //    if (!originalParams.ContainsKey(loopName))
        //    {
        //        originalParams[loopName] = layer.Parameters;
        //    }

        //    var originalKP = originalParams[loopName]["KP"];
        //    var originalKI = originalParams[loopName]["KI"];
        //    var originalKD = originalParams[loopName]["KD"];

        //    var currentKP = layer.Parameters["KP"];
        //    var currentKI = layer.Parameters["KI"];
        //    var currentKD = layer.Parameters["KD"];

        //    if (originalKP != currentKP || originalKI != currentKI || originalKD != currentKD)
        //    {
        //        counterLoopsRetuned++;
        //        originalParams[loopName] = layer.Parameters;
        //    }
        //}
        //return counterLoopsRetuned;
        return 0;
    }

    private static List<string> CalculateRetuningRequired(List<ControlLoop> layersBPCS)
    {
        var tuningRequired = new List<string>();
        foreach (var layer in layersBPCS)
        {
            if (layer.result.TuningRequired)
            {
                tuningRequired.Add(layer.LoopID);
            }
        }
        return tuningRequired;
    }
}

public class NameValuePair
{
    public string Name { get; set; }
    public double value { get; set; }
}


public class ControlLoop
{
    public string LoopID { get; set; }
    public double UpperControlGuide { get; set; }
    public double LowerControlGuide { get; set; }
    public string Maintenance { get; set; }
    public string MaintenanceDone { get; set; }
    public double KP { get; set; }
    public double KI { get; set; }
    public double KD { get; set; }
    public string Scenario { get; set; }
    public LoopResult result { get; set; }
    public List<BPCS_TimeSeriesEntry> timeseries;

    public override string ToString()
    {
        return $"LoopId: {LoopID}, " +
               $"UpperControlGuide: {UpperControlGuide}, " +
               $"LowerControlGuide: {LowerControlGuide}, " +
               $"Scenario: {Scenario}";
    }
}


public class BPCS_TimeSeriesEntry
{
    public string LoopId { get; set; }
    public DateTime Timestamp { get; set; }
    public double ProcessValue { get; set; }
    public double ControlValue { get; set; }
    public double Setpoint { get; set; }
    public bool Mode { get; set; }

    public override string ToString()
    {
        return $"LoopId: {LoopId}, " +
               $"Timestamp: {Timestamp}, " +
               $"ProcessValue: {ProcessValue}, " +
               $"ControlValue: {ControlValue}, " +
               $"Setpoint: {Setpoint}, " +
               $"Mode: {Mode}";
    }
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

    public override string ToString()
    {
        return $"ManualPercent: {ManualPercent}, TuningRequired: {TuningRequired}, OperatorInterventions: {OperatorInterventions}, " +
               $"TimesOutsideGuide: {TimesOutsideGuide}, MaxPV: {MaxPV}, MinPV: {MinPV}, MaxCV: {MaxCV}, MinCV: {MinCV}, " +
               $"MaintenanceDate: {MaintenanceDate}, DaysToMaintenance: {DaysToMaintenance}, PVStandardDeviation: {PVStandardDeviation}, " +
               $"CVStandardDeviation: {CVStandardDeviation}, PVMean: {PVMean}, CVMean: {CVMean}, Correlation: {Correlation}, " +
               $"Covariance: {Covariance}, IsSaturated: {IsSaturated}, PFD: {PFD}";
    }
}
public class TotalResults
{
    public List<string> CurrentManual { get; set; }
    public List<string> CurrentOutsideGuide { get; set; }
    public List<string> CurrentSaturated { get; set; }
    public double ManualPercentage { get; set; }
    public List<string> ManualThisMonth { get; set; }
    public List<string> SaturatedThisMonth { get; set; }
    public int RetunedThisMonth { get; set; }
    public List<string> RetuningRequired { get; set; }
    public List<NameValuePair> TopError { get; set; }
    public List<NameValuePair> TopNoise { get; set; }
}
