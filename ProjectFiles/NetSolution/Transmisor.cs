#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.WebUI;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.Report;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CommunicationDriver;
using FTOptix.EventLogger;
using FTOptix.Alarm;
using FTOptix.Core;
using System.IO;
using System.Threading;
#endregion

public class Transmisor : BaseNetLogic
{
    public override void Start()
    {
        RefreshWeB2();// Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
    public void RefreshWeB2()
    {
        Owner.Get<WebBrowser>("SIS1").Visible = false;
        String projectPath = (ResourceUri.FromProjectRelativePath("").Uri);
        String folderSeparator = Path.DirectorySeparatorChar.ToString();

        // Get template name and create destination path
        string templatePath = projectPath + folderSeparator + "eCharts" + folderSeparator + "pieChartSIS1.js";
        string filePath = projectPath + folderSeparator + "eCharts" + folderSeparator + "pieSIS1.js";

        // Read template page content
        string text = File.ReadAllText(templatePath);



        text = text.Replace("$01", (Project.Current.GetVariable("Model/Results/SIS_General_Result/transmitter/1oo1").Value * 1).ToString());
        text = text.Replace("$02", (Project.Current.GetVariable("Model/Results/SIS_General_Result/transmitter/1oo2").Value * 1).ToString());
        text = text.Replace("$03", (Project.Current.GetVariable("Model/Results/SIS_General_Result/transmitter/2oo2").Value * 1).ToString());
        text = text.Replace("$04", (Project.Current.GetVariable("Model/Results/SIS_General_Result/transmitter/2oo3").Value * 1).ToString());
        text = text.Replace("$05", (Project.Current.GetVariable("Model/Results/SIS_General_Result/transmitter/2oo4").Value * 1).ToString());

        // Write to file
        File.WriteAllText(filePath, text);

        // Refresh WebBrowser page
        Owner.Get<WebBrowser>("SIS1").Refresh();
        Log.Debug("eCharts", "Finished");
        Thread.Sleep(500);
        Owner.Get<WebBrowser>("SIS1").Visible = true; // Insert code to be executed by the method
    }
}
