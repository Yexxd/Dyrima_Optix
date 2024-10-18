#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.SQLiteStore;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.Store;
using FTOptix.Report;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Core;
using System.IO;
using System.Threading;
using FTOptix.WebUI;
#endregion

public class ChangeValues : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        RefreshWeB();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void RefreshWeB()
    {
        Owner.Get<WebBrowser>("MonthDis").Visible = false;
        String projectPath = (ResourceUri.FromProjectRelativePath("").Uri);
        String folderSeparator = Path.DirectorySeparatorChar.ToString();

        // Get template name and create destination path
        string templatePath = projectPath + folderSeparator + "eCharts" + folderSeparator + "pieChart.js";
        string filePath = projectPath + folderSeparator + "eCharts" + folderSeparator + "pie.js";

        // Read template page content
        string text = File.ReadAllText(templatePath);
        
        var value1 = Project.Current.GetVariable("Model/Results/BCPS_General_Result/Distribution/Manual").Value * 1;
        var value2 = Project.Current.GetVariable("Model/Results/BCPS_General_Result/Distribution/Auto").Value * 1;

        // Reemplaza los marcadores de posición con los valores
        text = text.Replace("$01", value1.ToString())
                   .Replace("$02", value2.ToString());

        // Write to file
        File.WriteAllText(filePath, text);

        // Refresh WebBrowser page
        Owner.Get<WebBrowser>("MonthDis").Refresh();
        Log.Debug("eCharts", "Finished");
        Thread.Sleep(500);
        Owner.Get<WebBrowser>("MonthDis").Visible = true; // Insert code to be executed by the method
    }
}
