#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.SQLiteStore;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.WebUI;
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
using System.Linq;
using System.Xml.Linq;
using System.Reflection;
using FTOptix.ODBCStore;

#endregion

public class SIS_Calculation : BaseNetLogic
{
    Store store, hist_db, main_db;
    List<SIS_SIF> sifs;

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        store = Project.Current.Get<Store>("DataStores/ResultsDB");
        hist_db = Project.Current.Get<Store>("DataStores/Historics");
        main_db = Project.Current.Get<Store>("DataStores/MainDatabase");
        Calculate();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }


    [ExportMethod]
    public void Calculate()
    {
        Log.Info("Calculating", "SIS");

        sifs = new List<SIS_SIF>();
        main_db.Query("SELECT * FROM SIS_Layers ORDER BY SIFID", out string[] headers, out object[,] result);

        for (int i = 0; i < result.Length / headers.Length; i++)
        {
            var sif_entry = new Dictionary<string, object>();   
            for (int j = 0; j < headers.Length; j++)
            {
                sif_entry[headers[j]] = result[i,j];  
            }

            var sif = new SIS_SIF()
            { 
                SIFID = sif_entry["SIFID"].ToString(),
                SIL = (long)sif_entry["SIL"],
                Transmitter = sif_entry["Transmitter"].ToString(),
                LogicSolver = sif_entry["LogicSolver"].ToString(),
                FinalElement = sif_entry["FinalElement"].ToString(),
                lmbs = (double)sif_entry["lmbs"],
                lmbl = (double)sif_entry["lmbl"],
                lmbe = (double)sif_entry["lmbl"],
                maintenance = DateTime.Now,
                maintenance_done = false,
                corrective_maintenance = DateTime.Now,
                proofTest = DateTime.Now,
                Scenario = sif_entry["Scenario"].ToString(),
            };
            
            sifs.Add(sif);
        }
        var query = "SELECT * FROM SIS_Historics GROUP BY Timestamp ORDER BY Timestamp";
        hist_db.Query(query, out headers, out result);
        List<SIS_TimeSeriesEntry> sis_entries = new List<SIS_TimeSeriesEntry>();
        for (int i = 0; i < result.Length / headers.Length; i++)
        {
            var ts_entry = new Dictionary<string, object>();
            for (int j = 0; j < headers.Length; j++)
            {
                ts_entry[headers[j]] = result[i, j];
            }
            var entrypoint = new SIS_TimeSeriesEntry()
            {
                Asset = ts_entry["Asset"].ToString(),
                Timestamp = (DateTime)ts_entry["Timestamp"],
                IsActive = Convert.ToBoolean((long)ts_entry["isActive"]),
                IsForced = Convert.ToBoolean((long)ts_entry["isForced"]),
                IsInhibited = Convert.ToBoolean((long)ts_entry["isInhibited"]),
                SensorHealthGood = Convert.ToBoolean((long)ts_entry["sensorHealtGood"]),
                ActuatorHealthGood = Convert.ToBoolean((long)ts_entry["actuatorHealtGood"]),
                SetPointValue = (double)ts_entry["setPointValue"],
                ProcessValue = (double)ts_entry["processValue"],
            };
            sis_entries.Add(entrypoint);
        }

        foreach (var item in sifs)
        {
            try
            {
                item.timeseries = sis_entries.Where(point => point.Asset == item.SIFID).ToList();
                item.result = CalculateSIS(item);
                Log.Info("SIF", item.ToString());
            }
            catch (Exception e)
            {
                Log.Warning("Error calculating sif", $"{item.SIFID} : {e.Message}");
            }
        }
        if (sifs.Count > 0)
            CalculateGeneral(sifs);
    }

    [ExportMethod]
    public void SelectedAsset(string assetId)
    {
        Log.Info("SIS SelectedInfo", assetId);
        var finded_loop = sifs.Find(loop => loop.SIFID == assetId);
        SIS_Result result = finded_loop.result;
        var result_object = InformationModel.Get<SIS>((NodeId)LogicObject.GetVariable("ResultObject").Value.Value);
        result_object.SIL = result.SIL;
        result_object.countactivated = result.CountActivated;
        result_object.countactivatedyear = result.CountActivatedYear;
        result_object.countactivatedespuria = result.CountActivatedSpuria;
        result_object.countactivatedespuriayear = result.CountActivatedSpuriaYear;
        result_object.isinhibited = result.IsInhibited;
        result_object.isforced = result.IsForced;
        result_object.correctiveMainteinance = result.CorrectiveMaintenance;
        result_object.sensorhealtGood = result.SensorHealthGood;
        result_object.actuatorhealtGood = result.ActuatorHealthGood;
        result_object.setpoint = result.SetPoint;
        result_object.processvalue = result.ProcessValue;
        result_object.porcentualgauge = result.PercentualGauge;
        result_object.actualPFD = result.ActualPFD;
        result_object.pfdmax = result.PfdMax;
        result_object.maintenanceDate = result.maintenanceDate;
        result_object.proofTestDate = result.proofTestDate;
    }

    SIS_Result CalculateSIS(SIS_SIF sif)
    {
        if(sif.timeseries.Count == 0)
        {
            Log.Warning("Timeseries empty", sif.SIFID);
            return new SIS_Result();    
        }

        var last = sif.timeseries.Last();
        return new SIS_Result()
        {
            IsInhibited = last.IsInhibited,
            IsForced = last.IsForced,
            SensorHealthGood = last.SensorHealthGood,
            ActuatorHealthGood = last.ActuatorHealthGood,
            SetPoint = (float)last.SetPointValue,
            ProcessValue = (float)last.ProcessValue,
            PercentualGauge = (int)(last.SetPointValue - last.ProcessValue),
            maintenanceDate = sif.maintenance.ToShortDateString(),
            proofTestDate = sif.proofTest.ToShortDateString(),
            SIL = (int)sif.SIL
        };
    }
    
    Dictionary<string, int> votings = new Dictionary<string, int>()
    {
    { "1oo1", 0 },
    { "1oo2", 1 },
    { "2oo2", 2 },
    { "2oo3", 3 },
    { "2oo4", 4 },
    };
    string[] votingss = new string[] { "1oo1", "1oo2", "2oo2", "2oo3", "2oo4" };

void CalculateGeneral(List<SIS_SIF> sifs)
    {
  
        var total = sifs.Count();
        var percentageinhibited = 0;
        var percentageforce = 0;
        var functionaltesting = 0;
        var maintenance = 0;
        var timeToNormal = 0;

        var silscount = new int[]{ 0, 0, 0, 0 };
        var transmitterVoting = new int[] { 0, 0, 0, 0, 0};
        var solverVoting = new int[] { 0, 0, 0, 0, 0 };
        var finalVoting = new int[] { 0, 0, 0, 0, 0 };

        foreach (var item in sifs)
        {
            if (item.result.IsInhibited)
                percentageinhibited++;
            if (item.result.IsForced)
                percentageforce++;
            if (item.result.pastProoftestDays >= 0)
                functionaltesting++;
            if(item.result.daysToMaintenance >= 0)
                maintenance++;

            silscount[item.SIL - 1]++;
            try
            {
                transmitterVoting[votings[item.Transmitter]]++;
                solverVoting[votings[item.LogicSolver]]++;
                finalVoting[votings[item.FinalElement]]++;
            }
            catch (Exception e)
            {
                Log.Warning("SIS ERROR", e.Message);
            }
        }


        var resultObject = Project.Current.Get<SIS_General_Result>("Model/Results/SIS_General_Result");
        resultObject.percentageinhibited = percentageinhibited;
        resultObject.percentageforce = percentageforce;
        resultObject.functionaltesting = functionaltesting;
        resultObject.maintenance = maintenance;
        resultObject.timeToNormal = timeToNormal;
        resultObject.totalSifs = total;


        for (int i = 1; i < 5; i++)
        {
            resultObject.SIL_Distribution.GetVariable("SIL" + i).Value = silscount[i-1];
        }
        for (int i = 0; i < votings.Count; i++)
        {
            Log.Info("SILS", $"{votingss[i]} {transmitterVoting[i]}");
            resultObject.transmitter.GetVariable(votingss[i]).Value = transmitterVoting[i];
            resultObject.solver.GetVariable(votingss[i]).Value = solverVoting[i];
            resultObject.actuator.GetVariable(votingss[i]).Value = finalVoting[i];
        }

        foreach (IUANode item in resultObject.MostActiveSifs.Children)
            resultObject.MostActiveSifs.Remove(item);
        sifs.Sort((p1, p2) => p1.result.CountActivated.CompareTo(p2.result.CountActivated));

        foreach (var item in sifs)
        {
            IUAVariable newVariable = InformationModel.MakeVariable(item.SIFID, OpcUa.DataTypes.Float);
            newVariable.Value = new UAValue(item.result.CountActivated);
            resultObject.MostActiveSifs.Add(newVariable);
        }
    }
}

public class SIS_SIF
{
    public string SIFID;
    public long SIL;
    public string Transmitter;
    public string LogicSolver;
    public string FinalElement;
    public double lmbs;
    public double lmbl;
    public double lmbe;
    public DateTime maintenance;
    public bool maintenance_done;
    public DateTime corrective_maintenance;
    public DateTime proofTest;
    public string Scenario;
    public SIS_Result result { get; set; }
    public List<SIS_TimeSeriesEntry> timeseries;
    public override string ToString()
    {
        return $"SIS_SIF: [SIFID={SIFID}, SIL={SIL}, Transmitter={Transmitter}, " +
               $"LogicSolver={LogicSolver}, FinalElement={FinalElement}, lmbs={lmbs}, " +
               $"lmbl={lmbl}, lmbe={lmbe}, maintenance={maintenance:yyyy-MM-dd}, " +
               $"maintenance_done={maintenance_done}, corrective_maintenance={corrective_maintenance:yyyy-MM-dd}, " +
               $"proofTest={proofTest:yyyy-MM-dd}, Scenario={Scenario}]";
    }
}

public class SIS_TimeSeriesEntry
{
    public string Asset { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsActive { get; set; }
    public bool IsForced { get; set; }
    public bool IsInhibited { get; set; }
    public bool SensorHealthGood { get; set; }
    public bool ActuatorHealthGood { get; set; }
    public double SetPointValue { get; set; }
    public double ProcessValue { get; set; }

    public override string ToString()
    {
        return $"SIS_Entry(Asset={Asset}, Timestamp={Timestamp}, IsActive={IsActive}, " +
               $"IsForced={IsForced}, IsInhibited={IsInhibited}, " +
               $"SensorHealthGood={SensorHealthGood}, " +
               $"ActuatorHealthGood={ActuatorHealthGood}, " +
               $"SetPointValue={SetPointValue}, ProcessValue={ProcessValue})";
    }
}

public class SIS_Result
{
    public int SIL { get; set; }
    public int CountActivated { get; set; }
    public int CountActivatedYear { get; set; }
    public int CountActivatedSpuria { get; set; }
    public int CountActivatedSpuriaYear { get; set; }
    public bool IsInhibited { get; set; }
    public bool IsForced { get; set; }
    public bool CorrectiveMaintenance { get; set; }
    public bool SensorHealthGood { get; set; }
    public bool ActuatorHealthGood { get; set; }
    public float SetPoint { get; set; }
    public float ProcessValue { get; set; }
    public int PercentualGauge { get; set; }
    public float ActualPFD { get; set; }
    public float PfdMax { get; set; }
    public float PfdAvg { get; set; }
    public string maintenanceDate { get; set; }
    public int daysToMaintenance { get; set; }
    public string proofTestDate { get; set; }
    public int pastProoftestDays { get; set; }
}
