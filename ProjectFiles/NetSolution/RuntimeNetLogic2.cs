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
using FTOptix.ODBCStore;
#endregion

public class RuntimeNetLogic2 : BaseNetLogic
{
    Store store, historicsdb;

    public override void Start()
    {
        store = Project.Current.Get<Store>("DataStores/ResultsDB");
        historicsdb = Project.Current.Get<Store>("DataStores/Historics");
    }


    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void Method1()
    {
        Log.Info("Selecting ", "BPCS G Historic");
        // Insert code to be executed by the method
        var query = "SELECT * FROM \"BPCS G Historic\" ORDER BY Timestamp";
        store.Query(query, out string[] headers, out object[,] result);
        var he = "";

        for (int j = 0; j < headers.Length; j++)
            he += headers[j] +", ";
        Log.Info("Headers ", he);

        for (int i = 0; i < result.Length / headers.Length; i++)
        {
            var message = "";
            for (int j = 0; j < headers.Length; j++)
                message += headers[j] + $": {result[i, j]}, ";
            Log.Info("Entries", message);
        }
    }

    [ExportMethod]
    public void Query(string query)
    {
        // Insert code to be executed by the method
        historicsdb.Query(query, out string[] headers, out object[,] result);
        var he = "";
        for (int j = 0; j < headers.Length; j++)
            he += headers[j] + ", ";
        Log.Info("Headers ", he);

        for (int i = 0; i < result.Length / headers.Length; i++)
        {
            var message = "";
            for (int j = 0; j < headers.Length; j++)
                message += headers[j] + $": {result[i, j]}, ";
            Log.Info("Entries", message);
        }
    }

}
