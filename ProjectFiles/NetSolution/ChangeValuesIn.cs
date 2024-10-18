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

public class ChangeValuesIn : BaseNetLogic
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
        string templatePath = projectPath + folderSeparator + "eCharts" + folderSeparator + "pieChart.js";
        string filePath = projectPath + folderSeparator + "eCharts" + folderSeparator + "pie.js";

        // Read template page content
        string text = File.ReadAllText(templatePath);

    

        var value1 = Project.Current.GetVariable("Model/Results/BCPS_General_Result/Distribution/Manual").Value * 1; // Primer valor
        var value2 = Project.Current.GetVariable("Model/Results/BCPS_General_Result/Distribution/Auto").Value * 1; // Segundo valor

        // Reemplaza los marcadores de posición en el texto con los valores de las variables
        text = text.Replace("$01", value1.ToString())
                   .Replace("$02", value2.ToString());

        // Write to file
        File.WriteAllText(filePath, text);

        // Refresh WebBrowser page
        Owner.Get<WebBrowser>("AlarmDIS").Refresh();
        Log.Debug("eCharts", "Finished");
        Thread.Sleep(500);
        Owner.Get<WebBrowser>("AlarmDIS").Visible = true; // Insert code to be executed by the method
    }
}
