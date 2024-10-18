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

public class ChangeValuesAlarmsGeneral : BaseNetLogic
{
    public override void Start()
    {
        RefreshWeB2();// Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
    [ExportMethod]
    public void RefreshWeB2()
    {
        Owner.Get<WebBrowser>("AlarmDIS").Visible = false;
        String projectPath = (ResourceUri.FromProjectRelativePath("").Uri);
        String folderSeparator = Path.DirectorySeparatorChar.ToString();

        // Get template name and create destination path
        string templatePath = projectPath + folderSeparator + "eCharts" + folderSeparator + "pieChartAlarms.js";
        string filePath = projectPath + folderSeparator + "eCharts" + folderSeparator + "pieAlarm.js";

        // Read template page content
        string text = File.ReadAllText(templatePath);



        // Reemplaza los marcadores de posición con los valores
        for (int i = 1; i < 5; i++)
        {
            text = text.Replace(i < 10 ? "$0" + i : "$" + i, (Project.Current.GetVariable("Model/Results/Alarms_General_Result/PriorityDistribution/" + i).Value * 1).ToString());
        }

        // Write to file
        File.WriteAllText(filePath, text);

        // Refresh WebBrowser page
        Owner.Get<WebBrowser>("AlarmDIS").Refresh();
        Log.Debug("eCharts", "Finished");
        Thread.Sleep(500);
        Owner.Get<WebBrowser>("AlarmDIS").Visible = true; // Insert code to be executed by the method
    }
}
