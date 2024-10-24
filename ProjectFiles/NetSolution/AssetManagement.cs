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
using System.Reflection.PortableExecutable;
using FTOptix.OPCUAClient;
using FTOptix.ODBCStore;
#endregion

public class AssetManagement : BaseNetLogic
{
    Store myStore;

    public override void Start()
    {
        myStore = Project.Current.Get<Store>("DataStores/MainDatabase");
        //myStore.Query("DROP TABLE Assets", out string[] Header, out object[,] ResultSet);
        //myStore.Query("DROP TABLE Scenarios", out string[] Header2, out object[,] ResultSet2);
        //myStore.Query("DROP TABLE SIS_Layers", out  Header2, out  ResultSet2);
        //myStore.Query("DROP TABLE BPCS_Layers", out  Header2, out ResultSet2);
    }

    [ExportMethod]
    public void CreateAsset()
    {
        var assetValues = Owner.Get<Asset_Model>("New_AssetInstance");

        if (assetValues.AssetType == "Scenario")
        {
            var scenarioValues = Owner.Get<Scenario_Details>("Scenario_DetailsInstance");
            Table scenariosTable = myStore.Tables.Get<Table>("Scenarios");
            string[] scColumns = { "Name", "threat", "consequence", "limit_event", "frequency",
                "severity", "final_risk", "design_pfd", "actual_pfd", "Area"};
            var scValues = new object[1, 10];
            scValues[0, 0] = assetValues.Name;
            scValues[0, 1] = scenarioValues.threat;
            scValues[0, 2] = scenarioValues.consequence;
            scValues[0, 3] = scenarioValues.limit_event;
            scValues[0, 4] = scenarioValues.frequency;
            scValues[0, 5] = scenarioValues.severity;
            scValues[0, 6] = scenarioValues.final_risk;
            scValues[0, 7] = scenarioValues.design_pfd;
            scValues[0, 8] = 0;
            scValues[0, 9] = assetValues.ParentAsset;
            var msg = "Inserting";
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    msg += $"{scColumns[i]}: {scValues[0, i]}, ";
                }
                catch (Exception)
                {
                }
            }
            Log.Info("Inserting", msg);
            scenariosTable.Insert(scColumns, scValues);


            if (scenarioValues.BPCS)
            {
                var layerdetails = Owner.Get<Layers_Details>("Layers_DetailsInstance");
                Table bcps_layer = myStore.Tables.Get<Table>("BPCS_Layers");
                string[] bcpsColumns = { "LoopID", "UpperControlGuide","LowerControlGuide","KP", "KI", "KD", "Scenario"};
                var bcpsValues = new object[1, 7];
                bcpsValues[0, 0] = layerdetails.B_LoopID;
                bcpsValues[0, 1] = layerdetails.B_UCG;
                bcpsValues[0, 2] = layerdetails.B_LCG;
                bcpsValues[0, 3] = layerdetails.B_P;
                bcpsValues[0, 4] = layerdetails.B_I;
                bcpsValues[0, 5] = layerdetails.B_D;
                bcpsValues[0, 6] = assetValues.Name;
                bcps_layer.Insert(bcpsColumns, bcpsValues);
            }

            if (scenarioValues.SIS)
            {
                var layerdetails = Owner.Get<Layers_Details>("Layers_DetailsInstance");
                Table layer = myStore.Tables.Get<Table>("SIS_Layers");
                string[] columns = { "SIFID", "SIL", "Transmitter", "LogicSolver", "FinalElement", "lmbs", "lmbl", "lmbe", "Scenario" };
                var lvalues = new object[1, 9];
                lvalues[0, 0] = layerdetails.S_SIF_Name;
                lvalues[0, 1] = layerdetails.S_SIL;
                lvalues[0, 2] = layerdetails.S_Transmitter;
                lvalues[0, 3] = layerdetails.S_LogicSolver;
                lvalues[0, 4] = layerdetails.S_FinalElement;
                lvalues[0, 5] = layerdetails.S_Imbs;
                lvalues[0, 6] = layerdetails.S_Imbl;
                lvalues[0, 7] = layerdetails.S_Imbe;
                lvalues[0, 8] = assetValues.Name;
                layer.Insert(columns, lvalues);
            }
        }

        Table assetsTable = myStore.Tables.Get<Table>("Assets");
        string[] dbColumns = {"Name","Description","Parent","AssetType", "image" };

        var values = new object[1, 5];
        values[0, 0] = assetValues.Name;
        values[0, 1] = assetValues.Details;
        values[0, 2] = assetValues.ParentAsset;
        values[0, 3] = assetValues.AssetType;
        if(assetValues.AssetType != "Scenario")
        {
            var urivalue = assetValues.ImageFilePath;
            values[0, 4] = urivalue.Uri;
            Log.Info("New Asset String", values[0, 4].ToString());
        }
        else
            values[0, 4] = "";

        assetsTable.Insert(dbColumns, values);
    }

    [ExportMethod]
    public void SelectedAsset()
    {
        // Insert code to be executed by the method
        var selectedAsset = Owner.Get<Asset_Model>("SelectedAsset");
        Log.Info("listbox Name", selectedAsset.Name);
        string query = $"SELECT * FROM Assets WHERE Name = \"{selectedAsset.Name}\"";
        Log.Info("query", query);
        myStore.Query(query, out string[] Header, out object[,] ResultSet);
        var message = "";
        for (int i = 0; i < Header.Length; i++)
        {
            message += $"{Header[i]}: {ResultSet[0,i]}, ";
        }

        Log.Info("Asset", message);
        selectedAsset.Details = ResultSet[0, 1].ToString();
        selectedAsset.ParentAsset = ResultSet[0, 2].ToString();
        selectedAsset.AssetType = ResultSet[0, 3].ToString();
        try
        {
            selectedAsset.ImageFilePath = ResourceUri.FromUri(ResultSet[0, 6].ToString());
        }
        catch (Exception)
        {
            selectedAsset.ImageFilePath = ResourceUri.FromUri("");
        }
    }

    [ExportMethod]
    public void DeteleAsset(string asset)
    {
        string query = $"DELETE FROM Assets WHERE Name = \"{asset}\"";
        myStore.Query(query, out string[] Header, out object[,] ResultSet);
        myStore.Query($"DELETE FROM Scenarios WHERE Name = \"{asset}\"", out Header, out ResultSet);
        myStore.Query($"DELETE FROM BPCS_Layers WHERE Scenario = \"{asset}\"", out Header, out ResultSet);
        myStore.Query($"DELETE FROM SIS_Layers WHERE Scenario = \"{asset}\"", out Header, out ResultSet);
        myStore.Query($"DELETE FROM FYG_Layers WHERE Scenario = \"{asset}\"", out Header, out ResultSet);
        myStore.Query($"DELETE FROM AP_Layers WHERE Scenario = \"{asset}\"", out Header, out ResultSet);
        myStore.Query($"DELETE FROM Datamappings WHERE Asset = \"{asset}\"", out Header, out ResultSet);
        var panel = Owner.Get<PanelLoader>("Asset_manager/ScaleLayout1/PanelLoader2");
        panel.ChangePanel("");
    }
}
