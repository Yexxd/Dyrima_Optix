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
        var station = Project.Current.Get<Station>($"CommDrivers/RAEtherNet_IPDriver1/ControlLogix");
        station.ChildrenRemoteRead();

        List<Dictionary<string, object>> layers_result = new List<Dictionary<string, object>>();

        mainDb.Query("SELECT * FROM Datamappings", out string[] headers, out object[,] results);
        string res = "";
        foreach (var item in headers)
        {
            res += $"{item}, ";
        }
        Log.Info("Headers", res);
        Log.Info("Mappings", $"{results.Length / headers.Length} mappings");

        for (int i = 0; i < results.Length / headers.Length; i++)
        {
            try
            {
                Log.Info("Res", results[i, 4].ToString());
                var finded = layers_result.Find(x =>
                    x["AssetId"].ToString() == results[i, 2].ToString()
                    && x["Layer"].ToString() == results[i, 4].ToString());

                string property = results[i, 3].ToString();
                if (finded != null)
                {
                    var value = ReadTag(station, results[i, 1].ToString());
                    Log.Info("Setting variable", $"{property} to {value}");
                    finded[property] = value.Value;
                }
                else
                {
                    var new_entry = new Dictionary<string, object>();
                    var value = ReadTag(station, results[i, 1].ToString());
                    Log.Info("Setting variable", $"{property} to {value}");
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
            Log.Info(item["AssetId"].ToString(), mystring);

            switch(item["Layer"])
            {
                case "BPCS":
                    Log.Info("BPCS", $"Item keys: {item.Keys.Count}");
                    break;
                case "SIS":
                    Log.Info("SIS", $"Item keys: {item.Keys.Count}");
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
    }




    UAValue ReadTag(Station station, string TagName)
    {
        Log.Info("Reading", $"CommDrivers/RAEtherNet_IPDriver1/{station.BrowseName}/Tags/{TagName}");
        var read_tag = Project.Current.Get<FTOptix.RAEtherNetIP.Tag>($"CommDrivers/RAEtherNet_IPDriver1/{station.BrowseName}/Tags/{TagName}");
        return read_tag.Value;
    }


    private PeriodicTask calculationTask;
}
