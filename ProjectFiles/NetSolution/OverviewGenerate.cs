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
using System.Linq;
using FTOptix.ODBCStore;
#endregion

public class OverviewGenerate : BaseNetLogic
{
    Store mainDb;

    public override void Start()
    {
        mainDb = Project.Current.Get<Store>("DataStores/MainDatabase");
        GenerateInstances();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void GenerateInstances()
    {

        var targetContainer = Owner.Get<ColumnLayout>("Rectangle10/ScrollView1/ScenariosContainer");
        foreach (var item in targetContainer.Children)
        {
            item.Delete();
        }

        mainDb.Query("SELECT * FROM Scenarios", out string[] headers, out object[,] result);
        for (int i = 0; i < result.Length / headers.Length; i++)
        {
            var newWidget = InformationModel.Make<Escenarios_riesg>("Escenario" + i.ToString());
            newWidget.GetVariable("Escenario").Value = result[i,0].ToString();
            try
            {
                newWidget.GetVariable("Area").Value = result[i, 9].ToString();
            }
            catch (Exception)
            { }
            try
            {
                newWidget.GetVariable("DesignRisk").Value = (uint)result[i, 6];
                newWidget.GetVariable("ActualRisk").Value = (uint)result[i, 6];
            }
            catch (Exception)
            { }
            try
            {
                newWidget.GetVariable("Layers").Value = (bool[])result[i, 10];
            }
            catch (Exception)
            { }
            try
            {
                newWidget.GetVariable("PFD").Value = result[i, 8].ToString();
            }
            catch (Exception)
            { }
            targetContainer.Add(newWidget);
        }
    }
}
