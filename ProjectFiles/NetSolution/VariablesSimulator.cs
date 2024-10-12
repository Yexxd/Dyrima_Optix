#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.SQLiteStore;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.DataLogger;
using FTOptix.NativeUI;
using FTOptix.Store;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using FTOptix.EventLogger;
using FTOptix.Core;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.Alarm;
using FTOptix.WebUI;
using FTOptix.Report;
using FTOptix.OPCUAClient;
using FTOptix.ODBCStore;
#endregion

public class VariablesSimulator : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
}
