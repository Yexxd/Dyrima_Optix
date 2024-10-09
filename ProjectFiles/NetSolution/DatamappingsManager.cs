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
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.EventLogger;
using FTOptix.Alarm;
using FTOptix.Core;
using System.Reflection.Emit;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.DataLogger;
#endregion

public class DatamappingsManager : BaseNetLogic
{
    Store myStore;

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        myStore = Project.Current.Get<Store>("DataStores/MainDatabase");
        //myStore.Query("DROP TABLE Datamappings", out string[] Header2, out object[,] ResultSet2);
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void SelectedAsset()
    {
        // Insert code to be executed by the method
        var selectedAsset = Owner.Get<Asset_Model>("SelectedAsset");
        Log.Info("listbox Name", selectedAsset.Name);
        string query = $"SELECT * FROM Assets WHERE Name = \"{selectedAsset.Name}\"";
        myStore.Query(query, out string[] Header, out object[,] ResultSet);
        selectedAsset.Details = ResultSet[0, 1].ToString();
        selectedAsset.ParentAsset = ResultSet[0, 2].ToString();
        selectedAsset.AssetType = ResultSet[0, 3].ToString();
    }

    [ExportMethod]
    public void SelectedVariable(string layer, string variable, string asset)
    {
        var mapping = Owner.Get<Datamapping>("Actual_Mapping");
        string query = $"SELECT * FROM Datamappings WHERE Asset = \"{mapping.Asset}\" AND Variable =  \"{mapping.Variable}\" AND Layer =  \"{mapping.Layer}\"";
        Log.Info($"Asset = \"{mapping.Asset}\" AND Variable =  \"{mapping.Variable}\" AND Layer =  \"{mapping.Layer}\"");

        myStore.Query(query, out string[] Header, out object[,] ResultSet);
        if (ResultSet.Length > 0) 
        {
            mapping.Datasource = (string)ResultSet[0,0];
            mapping.Datapoint = (string)ResultSet[0, 1];
            string map = "";
            for (int i = 0; i < Header.Length; i++)
            {
                map += $"{Header[i]}: {ResultSet[0,i]}, ";
            }
            Log.Info("Selected", map);
            Log.Info("Selected", mapping.Datasource + ", " + mapping.Datapoint);
        }
        else
        {
            mapping.Datapoint = "";
            mapping.Datasource = "";
            Log.Info("No Mapping finded");
        }
    }

    [ExportMethod]
    public void SaveDatamapping()
    {
        var mapping = Owner.Get<Datamapping>("Actual_Mapping");
        var selectedAsset = Owner.Get<Asset_Model>("SelectedAsset");
        string query = $"SELECT * FROM Datamappings WHERE Asset = \"{mapping.Asset}\" AND Variable =  \"{mapping.Variable}\" AND Layer =  \"{mapping.Layer}\"";
        Log.Info("Query", query);
        myStore.Query(query, out string[] Header, out object[,] ResultSet);
        if (ResultSet.Length > 0)
        {
            query = $"UPDATE Datamappings SET Datasource = \"{mapping.Datasource}\", Datapoint = \"{mapping.Datapoint}\"";
            query += $"WHERE Asset = \"{mapping.Asset}\" AND Variable =  \"{mapping.Variable}\" AND Layer =  \"{mapping.Layer}\"";
            myStore.Query(query, out Header, out ResultSet);
            Log.Info("Updated", $"Datasource = \"{mapping.Datasource}\", Datapoint = \"{mapping.Datapoint}\"");
        }
        else
        {
            Table mappingsTable = myStore.Tables.Get<Table>("Datamappings");
            string[] dbColumns = { "Datasource", "Datapoint", "Asset", "Variable", "Layer"};
            var values = new object[1, 5];
            values[0, 0] = mapping.Datasource;
            values[0, 1] = mapping.Datapoint;
            values[0, 2] = mapping.Asset;
            values[0, 3] = mapping.Variable;
            values[0, 4] = mapping.Layer;
            mappingsTable.Insert(dbColumns, values);
            Log.Info("Inserted", $"DS: {values[0, 0]}  DP: {values[0, 1]} AS: {values[0, 2]} Va: {values[0, 3]} La: {values[0, 4]}");
        }
    }
}
