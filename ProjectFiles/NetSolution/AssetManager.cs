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
using System.Data.Common;
using FTOptix.Report;
#endregion

public class AssetManager : BaseNetLogic
{

    Store myStore;
    Table myTable;
    string[] dbColumns = { "Nombre", "Descripcion", "Assetpadre" };
    public override void Start()
    {
        //myStore = Project.Current.Get<Store>("DataStores/AssetManager");
        //myTable = myStore.Tables.Get<Table>("Assets");
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {

    }

    [ExportMethod]
    public void CreateAsset()
    {
            var values = new object[1, 3];
            values[0, 0] = (string)Project.Current.GetVariable("Model/AssetManager/Asset/Nombre").Value;
            values[0, 1] = (string)Project.Current.GetVariable("Model/AssetManager/Asset/Descripcion").Value;
            values[0, 2] = (string)Project.Current.GetVariable("Model/AssetManager/SelectedAsset").Value; ;

   
            myTable.Insert(dbColumns, values);
        
    }

    [ExportMethod]
    public void DeleteAll()
    {
        // Insert code to be executed by the method
        Object[,] ResultSet;
        String[] Header;

        // Ejecutar el query DELETE
        myStore.Query("DELETE FROM Assets", out Header, out ResultSet);
    }
}
