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

[CustomBehavior]
public class Screen1Behavior : BaseNetBehavior
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined behavior is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined behavior is stopped
    }

#region Auto-generated code, do not edit!
    protected new Screen1 Node => (Screen1)base.Node;
#endregion
}
