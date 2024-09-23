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
#endregion

public class RuntimeNetLogic2 : BaseNetLogic
{
    public override void Start()
    {
        SetCurrentDate();
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void SetCurrentDate()
    {
        string formattedDate = DateTime.Now.ToString("dd/MM/yyyy-HH:MM");
        var stringfecha = Owner.Get<Label>("Header/Fecha");
        stringfecha.Text = formattedDate;
    }
}
