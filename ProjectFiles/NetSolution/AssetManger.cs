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
using FTOptix.AuditSigning;
using FTOptix.Report;
#endregion

public class AssetManger : BaseNetLogic
{
    Store myStore;
    Table myTable;
    string[] dbColumns = { "Nombre", "Descripcion", "Assetpadre" };
    public override void Start()
    {
        myStore = Project.Current.Get<Store>("DataStores/AssetManager");
        myTable = myStore.Tables.Get<Table>("Assets");
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void CreateAsset()
    {
        // Insert code to be executed by the method
        var values = new object[1, 3];
        values[0, 0] = (string)Project.Current.GetVariable("Model/AssetManager/Asset/Nombre").Value;
        values[0, 1] = (string)Project.Current.GetVariable("Model/AssetManager/Asset/Descripcion").Value;
        values[0, 2] = (string)Project.Current.Get<ComboBox>("UI/Screens/AssetManager/Asset_create/Rectangle1/ComboBox1").SelectedValue;
        // Obtener el valor de la selección del ComboBox
        ComboBox comboBox = Project.Current.Get<ComboBox>("UI/Screens/AssetManager/Asset_create/Rectangle1/ComboBox1");
         string result = Convert.ToString(comboBox.SelectedValue);

        var selectedValue = comboBox.SelectedValue;

        // Imprimir el tipo de dato y el valor sin convertir a string
        Log.Info($"Tipo de SelectedValue:{result}, Valor de SelectedValue: {selectedValue}");


        myTable.Insert(dbColumns, values);
       
    }

    [ExportMethod]
    public void DeleteAll()
    {
        // Insert code to be executed by the method
        // Borrar todos los registros de la tabla
        Object[,] ResultSet;
        String[] Header;

        // Ejecutar el query DELETE
        myStore.Query("DELETE FROM Assets", out Header, out ResultSet);
    }
}
