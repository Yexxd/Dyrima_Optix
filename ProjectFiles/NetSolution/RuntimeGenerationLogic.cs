#region Using directives
using System;
using System.Linq;
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using UAManagedCore;
using FTOptix.Alarm;
using FTOptix.Report;
using FTOptix.OPCUAClient;
using FTOptix.CommunicationDriver;
using FTOptix.RAEtherNetIP;
using FTOptix.DataLogger;
using FTOptix.ODBCStore;
#endregion

public class RuntimeGenerationLogic : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        var motorsContainer = InformationModel.Make<Panel>("MotorsContainer");
        Owner.Get("WorkspaceArea/RuntimeGeneratedObjects/VerticalLayout/UI_Widgets").Add(motorsContainer);
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    private void GenerateCoords(int topMargin, int leftMargin, out int newTop, out int newLeft)
    {
        int maxTop = Convert.ToInt16(Owner.Get<Rectangle>("WorkspaceArea/RuntimeGeneratedObjects/VerticalLayout/UI_Controls/UI_ControlsArea").Height);
        int maxLeft = Convert.ToInt16(Owner.Get<Rectangle>("WorkspaceArea/RuntimeGeneratedObjects/VerticalLayout/UI_Controls/UI_ControlsArea").Width);
        var rnd = new Random();
        if (topMargin == 0)
        {
            newTop = rnd.Next(10, maxTop - 40);
        }
        else
        {
            newTop = topMargin;
        }
        if (leftMargin == 0)
        {
            newLeft = rnd.Next(10, maxLeft - 100);
        }
        else
        {
            newLeft = leftMargin;
        }
    }
    [ExportMethod]
    public void GenerateButton(int topMargin, int leftMargin, string textToDisplay)
    {
        var myControl = InformationModel.Make<Button>(NodeId.Random(1).ToString().Replace("1/", ""));
        GenerateCoords(topMargin, leftMargin, out topMargin, out leftMargin);
        myControl.TopMargin = topMargin;
        myControl.LeftMargin = leftMargin;
        myControl.Text = textToDisplay;
        Owner.Get("WorkspaceArea/RuntimeGeneratedObjects/VerticalLayout/UI_Controls/UI_ControlsArea").Add(myControl);
    }

    [ExportMethod]
    public void GenerateLabel(int topMargin, int leftMargin, string textToDisplay)
    {
        var myControl = InformationModel.Make<Label>(NodeId.Random(1).ToString().Replace("1/", ""));
        GenerateCoords(topMargin, leftMargin, out topMargin, out leftMargin);
        myControl.TopMargin = topMargin;
        myControl.LeftMargin = leftMargin;
        myControl.Text = textToDisplay;
        Owner.Get("WorkspaceArea/RuntimeGeneratedObjects/VerticalLayout/UI_Controls/UI_ControlsArea").Add(myControl);
    }
    [ExportMethod]
    public void GenerateImage(int topMargin, int leftMargin)
    {
        var myControl = InformationModel.Make<Image>(NodeId.Random(1).ToString().Replace("1/", ""));
        GenerateCoords(topMargin, leftMargin, out topMargin, out leftMargin);
        myControl.TopMargin = topMargin;
        myControl.LeftMargin = leftMargin;
        myControl.Path = ResourceUri.FromProjectRelativePath("imgs/Logos/LogoFTOptixDarkGrey.svg");
        myControl.Width = 75;
        myControl.Height = 40;
        Owner.Get("WorkspaceArea/RuntimeGeneratedObjects/VerticalLayout/UI_Controls/UI_ControlsArea").Add(myControl);
    }
    [ExportMethod]
    public void GenerateInstances(int instCount)
    {
        var targetContainer = Owner.Get<ColumnLayout>("WorkspaceArea/RuntimeGeneratedObjects/VerticalLayout/UI_Widgets/UiWidgetsArea/ScrollView/VerticalLayout");
        //var motorsContainer = Owner.Get<Panel>("WorkspaceArea/RuntimeGeneratedObjects/VerticalLayout/UI_Widgets/MotorsContainer");
        int childNodes = targetContainer.Children.OfType<Escenarios_riesg>().Count();
        Log.Debug("childNodes: " + childNodes.ToString() + " - instCount: " + instCount.ToString());
        if (instCount > childNodes)
        {
            for (int i = childNodes; i < instCount; i++)
            {
                // Create new motor to link the widget
                //var newMotor = InformationModel.Make<CustomMotor>("RuntimeMotor" + (i + 1).ToString());
                //motorsContainer.Add(newMotor);
                // Create widget instance
                //var motorName = "RuntimeMotor" + (i + 1).ToString();
                var newWidget = InformationModel.Make<Escenarios_riesg>("Escenario" + i.ToString());
                //newWidget.VerticalAlignment = VerticalAlignment.Stretch;
                //newWidget.HorizontalAlignment = HorizontalAlignment.Left;
                //newWidget.TopMargin = 8;
                //Project.Current.GetVariable("Model/Prueba/testasset").Value = motorName;
                newWidget.GetVariable("Date").Value = "Date" + (i + 1).ToString();
                newWidget.GetVariable("Escenario").Value = "Escenario" + (i + 1).ToString();
                newWidget.GetVariable("Area").Value ="Escenario" + (i + 1).ToString();
                targetContainer.Add(newWidget);
            }
        }
        else if (instCount < childNodes)
        {
            for (int i = instCount; i < childNodes; i++)
            {
                targetContainer.Get("MyMotorWidget" + i.ToString()).Delete();
                //motorsContainer.Get("RuntimeMotor" + (i + 1).ToString()).Delete();
            }
        }
    }
}
