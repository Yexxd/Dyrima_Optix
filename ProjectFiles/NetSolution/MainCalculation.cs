#region Using directives
using System;
using System.Collections.Generic;
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
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.EventLogger;
using FTOptix.Alarm;
using FTOptix.Core;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using System.Reflection;
using FTOptix.DataLogger;
using FTOptix.ODBCStore;
#endregion

public class MainCalculation : BaseNetLogic
{
    Store mainDb, historicsDb;

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        calculationTask = new PeriodicTask(CalculationCycle, 60000, LogicObject);
        calculationTask.Start();
        mainDb = Project.Current.Get<Store>("DataStores/MainDatabase");
        historicsDb = Project.Current.Get<Store>("DataStores/Historics");
    }

    public override void Stop()
    {
        calculationTask?.Dispose();
    }

    private void CalculationCycle()
    {
        var now = DateTime.Now;

        var station = Project.Current.Get<Station>($"CommDrivers/RAEtherNet_IPDriver1/ControlLogix");
        station.ChildrenRemoteRead();

        List<Dictionary<string, object>> layers_result = new List<Dictionary<string, object>>();

        mainDb.Query("SELECT * FROM Datamappings", out string[] headers, out object[,] results);
        string res = "";
        foreach (var item in headers)
        {
            res += $"{item}, ";
        }

        for (int i = 0; i < results.Length / headers.Length; i++)
        {
            try
            {
                var finded = layers_result.Find(x =>
                    x["AssetId"].ToString() == results[i, 2].ToString()
                    && x["Layer"].ToString() == results[i, 4].ToString());

                string property = results[i, 3].ToString();
                if (finded != null)
                {
                    var value = ReadTag(station, results[i, 1].ToString());
                    finded[property] = value.Value;
                }
                else
                {
                    var new_entry = new Dictionary<string, object>();
                    var value = ReadTag(station, results[i, 1].ToString());
                    new_entry["AssetId"] = results[i, 2];
                    new_entry["Layer"] = results[i, 4];
                    new_entry[property] = value.Value;
                    layers_result.Add(new_entry);
                }
            }
            catch (Exception e)
            {
                Log.Error("RemoteRead failed: " + e.Message);
            }
        }

        foreach (var item in layers_result)
        {
            string mystring = "";
            foreach (var val in item.Keys)
            {
                mystring += $"{val}: {item[val]}, ";
            }
            switch(item["Layer"])
            {
                case "BPCS":
                    Log.Info("BPCS", $"Item keys: {item.Keys.Count}");
                    string[] bcps_columns = { "Timestamp", "LocalTimestamp", "ProcessValue", "ControlValue", "SetPoint", "Mode", "Asset"};
                    var scValues = new object[1, 7];
                    scValues[0, 0] = now;
                    scValues[0, 1] = now;
                    scValues[0, 2] = item.ContainsKey("ProcessValue") ? item["ProcessValue"] ?? 0 : 0;
                    scValues[0, 3] = item.ContainsKey("ControlValue") ? item["ControlValue"] ?? 0 : 0;
                    scValues[0, 4] = item.ContainsKey("SetPoint") ? item["SetPoint"] ?? 0 : 0;
                    scValues[0, 5] = item.ContainsKey("Mode") ? item["Mode"] ?? false : false;
                    string query = $"SELECT LoopID FROM BPCS_Layers WHERE Scenario = \"{item["AssetId"]}\"";
                    mainDb.Query(query, out string[] Header, out object[,] ResultSet);
                    var msg1 = "Inserting ";
                    for (int i = 0; i < bcps_columns.Length; i++)
                    {
                        msg1 += $"{bcps_columns[i]}: {scValues[0, i]}";
                    }
                    Log.Info("BPCS INSERT", msg1);
                    if (ResultSet.Length > 0)
                    { 
                        scValues[0, 6] = ResultSet[0,0];
                        Table scenariosTable = historicsDb.Tables.Get<Table>("BPCS_Historics");
                        scenariosTable.Insert(bcps_columns, scValues);
                    }
                    break;
                case "SIS":
                    string[] sis_columns = { "Timestamp", "LocalTimestamp", "isActive", "isForced", "isInhibited", "sensorHealtGood", "actuatorHealtGood", "setPointValue", "processValue", "Asset" };
                    var sis_values = new object[1, 10];
                    sis_values[0, 0] = now;
                    sis_values[0, 1] = now;
                    sis_values[0, 2] = item.ContainsKey("isActive") ? item["isActive"] ?? false : false;
                    sis_values[0, 3] = item.ContainsKey("isForced") ? item["isForced"] ?? false : false;
                    sis_values[0, 4] = item.ContainsKey("isInhibited") ? item["isInhibited"] ?? false : false;
                    sis_values[0, 5] = item.ContainsKey("sensorHealtGood") ? item["sensorHealtGood"] ?? true : true;
                    sis_values[0, 6] = item.ContainsKey("actuatorHealtGood") ? item["actuatorHealtGood"] ?? true : true;
                    sis_values[0, 7] = item.ContainsKey("setPointValue") ? item["setPointValue"] ?? 0 : 0;
                    sis_values[0, 8] = item.ContainsKey("processValue") ? item["processValue"] ?? 0 : 0;

                    string query2 = $"SELECT SIFID FROM SIS_Layers WHERE Scenario = \"{item["AssetId"]}\"";
                    mainDb.Query(query2, out _, out object[,] ResultSet2);
                    if (ResultSet2.Length > 0)
                    {
                        sis_values[0, 9] = ResultSet2[0, 0];
                        Table scenariosTable = historicsDb.Tables.Get<Table>("SIS_Historics");
                        scenariosTable.Insert(sis_columns, sis_values);
                        var msg = "Inserting ";
                        for (int i = 0; i < sis_columns.Length; i++)
                        {
                            msg += $"{sis_columns[i]}: {sis_values[0, i]}";
                        }
                        Log.Info("SIS INSERT", msg );
                    }
                    break;
                case "FYG":
                    Log.Info("FYG", $"Item keys: {item.Keys.Count}");
                    break;
                case "AP":
                    Log.Info("AP", $"Item keys: {item.Keys.Count}");
                    break;
                case "OPWIN":
                    Log.Info("OPWIN", $"Item keys: {item.Keys.Count}");
                    break;
            }
        }

        var calculation = Project.Current.GetObject("NetLogic/Layers/BPCS_Calculation");
        calculation.ExecuteMethod("Calculate");
        var sis_calculation = Project.Current.GetObject("NetLogic/Layers/SIS_Calculation");
        calculation.ExecuteMethod("Calculate");
    }

    UAValue ReadTag(Station station, string TagName)
    {
        Log.Info("Reading", $"CommDrivers/RAEtherNet_IPDriver1/{station.BrowseName}/Tags/{TagName}");
        var read_tag = Project.Current.Get<FTOptix.RAEtherNetIP.Tag>($"CommDrivers/RAEtherNet_IPDriver1/{station.BrowseName}/Tags/{TagName}");
        return read_tag.Value;
    }


    private PeriodicTask calculationTask;
}
