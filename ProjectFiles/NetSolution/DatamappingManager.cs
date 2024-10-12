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

public class DatamappingManager : BaseNetLogic
{
    Store myStore;
    public override void Start()
    {
        myStore = Project.Current.Get<Store>("DataStores/MainDatabase");
        myStore.Query("DELETE FROM Datamapping WHERE Asset = \"\"", out _, out _);
        myStore.Query("DELETE FROM Datamapping WHERE Asset = \"\"", out _, out _);
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void SelectedAsset()
    {
        var selectedAsset = Owner.Get<Asset_Model>("SelectedAsset");
        string query = $"SELECT * FROM Assets WHERE Name = \"{selectedAsset.Name}\"";
        Log.Info("query", query);
        myStore.Query(query, out string[] Header, out object[,] ResultSet);
        var message = "";
        for (int i = 0; i < Header.Length; i++)
        {
            message += $"{Header[i]}: {ResultSet[0, i]}, ";
        }

        Log.Info("Asset", message);
        selectedAsset.Details = ResultSet[0, 1].ToString();
        selectedAsset.ParentAsset = ResultSet[0, 2].ToString();
        selectedAsset.AssetType = ResultSet[0, 3].ToString();
    }

    [ExportMethod]
    public void SelectedVariable(string selectedVariable)
    {
        var nodeid = LogicObject.GetVariable("MappingInstance");
        var actualMapping = InformationModel.Get<Datamapping>((NodeId)nodeid.Value.Value);
        var selectedAsset = Owner.Get<Asset_Model>("SelectedAsset");

        var query = $"SELECT * FROM Datamappings WHERE Asset = \"{selectedAsset.Name}\" AND Variable = \"{selectedVariable}\"";
        Log.Info("Query", query);
        myStore.Query(query, out string[] Header, out object[,] ResultSet);
        Log.Info("QueryResult", ResultSet.Length.ToString());

        if (ResultSet.Length>0)
        {
            actualMapping.Datasource = ResultSet[0,0].ToString();
            actualMapping.Datapoint = ResultSet[0, 1].ToString();
        }
        else
        {
            actualMapping.Datasource = "";
            actualMapping.Datapoint = "";
        }
    }

    [ExportMethod]
    public void SaveDatamapping()
    {
        var nodeid = LogicObject.GetVariable("MappingInstance").Value.Value;
        var actualMapping = InformationModel.Get<Datamapping>((NodeId)nodeid);
        var dmtable = myStore.Tables.Get("Datamappings");
        var selectedAsset = Owner.Get<Asset_Model>("SelectedAsset");

        var query = $"SELECT * FROM Datamappings WHERE Asset = \"{selectedAsset.Name}\" AND Variable = \"{actualMapping.Variable}\"";
        Log.Info("Query", query);
        myStore.Query(query, out _, out object[,] ResultSet);
        Log.Info("QueryResult", ResultSet.Length.ToString());

        string[] scColumns = { "Datasource", "Datapoint", "Asset", "Variable", "Layer" };
        if (ResultSet.Length > 0)
        {
            query = $"UPDATE Datamappings SET Datasource = \"{actualMapping.Datasource}\", Datapoint = \"{actualMapping.Datapoint}\"" +
                $"WHERE Asset = \"{selectedAsset.Name}\" AND Variable = \"{actualMapping.Variable}\" AND Layer = \"{actualMapping.Layer}\"";
            myStore.Query(query, out _, out _);
            Log.Info("Updated");
        }
        else
        {
            var values = new object[1, 5];
            values[0, 0] = actualMapping.Datasource;
            values[0, 1] = actualMapping.Datapoint;
            values[0, 2] = selectedAsset.Name;
            values[0, 3] = actualMapping.Variable;
            values[0, 4] = actualMapping.Layer;
            dmtable.Insert(scColumns, values);
            Log.Info("Inserted");
        }
    }
}
