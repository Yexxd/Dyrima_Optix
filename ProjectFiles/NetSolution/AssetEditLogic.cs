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
using FTOptix.DataLogger;
using FTOptix.ODBCStore;
#endregion

public class AssetEditLogic : BaseNetLogic
{
    object[,] result;
    string selected;

    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void InstantiateChildrens(string parent, NodeId panel)
    {
        // Insert code to be executed by the method
        var targetContainer = Owner.Get<Image>("Asset_edit/AssetImage");
        Log.Info("Panel", panel.ToString());
        Log.Info("Targetcontainer", targetContainer.Width.ToString());

        Log.Info("Editting ", parent);

        var myStore = Project.Current.Get<Store>("DataStores/MainDatabase");
        myStore.Query($"SELECT * FROM Assets WHERE Parent = \"{parent}\"", out string[] headers, out result);
        for (int i = 0; i < result.Length/headers.Length; i++)
        {
            var message = "";
            for (int j = 0; j < headers.Length; j++)
                message += headers[j] + $": {result[i,j]}, ";
            Log.Info("Headers ", message);

            var newWidget = InformationModel.Make<Area>("Area" + result[i, 0].ToString());
            newWidget.Get<Label>("Label").Text = result[i, 0].ToString();
            newWidget.VerticalAlignment = VerticalAlignment.Top;
            newWidget.HorizontalAlignment = HorizontalAlignment.Left;
            newWidget.LeftMargin = ((float)(result[i, 4]??0f)) * 0.01f * targetContainer.Width;
            newWidget.TopMargin = (float)(result[i, 5] ?? 0f) * 0.01f * targetContainer.Height;
            targetContainer.Add(newWidget);
            //newWidget.Width = (float)result[i,4]*0.01f*targetContainer.Width;
            //newWidget.Height = (float)result[i, 5]*0.01f*targetContainer.Height;
        }
    }

    [ExportMethod]
    public void Save()
    {
        var savedImage = Owner.Get<Image>("Asset_edit/AssetImage/"+selected);
        Log.Info("Save", savedImage.BrowseName);
    }

    [ExportMethod]
    public void Onselection(string selected)
    {
        Log.Info("Selected", selected);
        this.selected = selected;
        var selectedArea = Owner.Get("SelectedArea");
        selectedArea.GetVariable("Name").Value = selected;      
    }
}
