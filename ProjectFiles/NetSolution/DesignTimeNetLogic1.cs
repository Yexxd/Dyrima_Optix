#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.SQLiteStore;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.DataLogger;
using FTOptix.NativeUI;
using FTOptix.WebUI;
using FTOptix.Store;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
using FTOptix.Alarm;
using FTOptix.Core;
#endregion

public class DesignTimeNetLogic1 : BaseNetLogic
{
    [ExportMethod]
    public void InsertBaseData()
    {
        Store myStore = Project.Current.Get<Store>("DataStores/MainDb");
        Table myTable = myStore.Tables.Get<Table>("loops");
        object[,] rawValues = new object[1, 2];
        rawValues[0, 0] = "LOOP_001";
        rawValues[0, 1] = "LOOP_001";
        string[] columns = new string[2] { "Name", "LoopId"};
        myTable.Insert(columns, rawValues);
    }
}