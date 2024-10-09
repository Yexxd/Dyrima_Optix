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
using FTOptix.EventLogger;
using FTOptix.Alarm;
using FTOptix.Core;
using FTOptix.OPCUAClient;
using FTOptix.OmronEthernetIP;
using System.Linq;
using FTOptix.CommunicationDriver;
using FTOptix.DataLogger;
#endregion

public class SourcesManagement : BaseNetLogic
{
    Store myStore;

    public override void Start()
    {
        myStore = Project.Current.Get<Store>("DataStores/MainDatabase");
        //var client = Project.Current.Get("OPC-UA/OPCUAClient1");
        //Log.Info("Tag Importer", client.GetType().Name);
        //var importer = client.Get("TagImporter");
        //Log.Info("Tag Importer", importer.GetType().Name);

       //myStore.Query("DROP TABLE Datasources", out string[] Header, out object[,] ResultSet);
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void CreateSource()
    {
        Table assetsTable = myStore.Tables.Get<Table>("Datasources");
        string[] dbColumns = { "Name", "Path", "Type"};
        var assetValues = Owner.Get<Datasource>("Selected_Source");
        var values = new object[1, 3];
        values[0, 0] = assetValues.Name;
        values[0, 1] = assetValues.Path;
        values[0, 2] = assetValues.Type;
        assetsTable.Insert(dbColumns, values);

        //var newDriver = InformationModel.Make<FTOptix.OPCUAClient.OPCUAClientType>("ua_client1");
        //newDriver.ServerEndpointURL = "opc.tcp://localhost:4842";
        //Project.Current.Get("OPC-UA").Add(newDriver);
        //Log.Info("Created", "Created");
        //if(assetValues.Type == "Logix")
        //{
        //    var newDriver = InformationModel.Make<FTOptix.RAEtherNetIP.Station>("station1");
        //    newDriver.Route = "172.16.0.18\\Backplane\\0";
        //    Project.Current.Get("CommDrivers/RAEtherNet_IPDriver1").Add(newDriver);
        //    Log.Info("Created", "Created");
        //    Struct[] instances;
        //    Struct[] types;
        //    newDriver.Browse(out instances, out types);
        //    foreach (var item in types)
        //    {
        //        Log.Info(((PrototypeInfo)item).NodeId.ToString());
        //    }
        //}
    }

    enum OpcAttributes
    {
        NodeId = 1,
        NodeClass = 2,
        BrowseName = 3,
        DisplayName = 4,
        Description = 5,
        WriteMask = 6,
        UserWriteMask = 7,
        IsAbstract = 8,
        Symmetric = 9,
        InverseName = 10,
        ContainsNoLoops = 11,
        EventNotifier = 12,
        Value = 13,
        DataType = 14,
        ValueRank = 15,
        ArrayDimensions = 16,
        AccessLevel = 17,
        UserAccessLevel = 18,
        MinimumSamplingInterval = 19,
        Historizing = 20,
        Executable = 21,
        UserExecutable = 22,
        DataTypeDefinition = 23,
        RolePermissions = 24,
        UserRolePermissions = 25,
        AccessRestrictions = 26,
        AccessLevelEx = 27,
        BrowsePath = 64,
        Quality = 65,
        StatusCode = 66,
        SourceTimestamp = 67,
        ServerTimestamp = 68,
        ActualDataType = 69,
        ActualArrayDimensions = 70
    };

    [ExportMethod]
    public static void ReadArbitraryValueFromRemoteOpcUaServer()
    {
        // Get the OPC/UA Client from the project
        var opc = Project.Current.Get<OPCUAClient>("OPC-UA/ua_client1");

        // OPC/UA Variable to read from the remote server
        object[] opcVariable = new object[3]
        {
        new NodeId(2, "Cira/2186_pcp_yaskawa/EI_XXXX1"), // NodeId of the variable to read (UaExpert can be used to get these values)
        OpcAttributes.Value, // Attribute ID to read (13 = Value), see enumeration above
        new uint[0] // Indexes, for simple scalar tags this is an empty array
        };

        // Create a Struct object that holds the information of the remote variable to be passed to the OPC/UA Client
        // Syntax: new Struct(NodeId dataTypeId, object[] values)
        // Where:
        // - DataTypeId is the NodeId of the DataType expected as input argument of the method (50 is the ReadValues DataType)
        // - Values is an array containing the information of the variable to read
        UAManagedCore.Struct asd = new Struct(new NodeId(FTOptix.OPCUAClient.ObjectTypes.OPCUAClient.NamespaceIndex, 50), opcVariable);

        // The ExecuteMethod method of the OPC/UA Client is used to call the ReadValues method of the OPC/UA Server
        // This expects an object[] as input parameters, so we will create a mono-dimensional array with the Struct object
        object[] inputArgs = new object[1];
        // The first element of the array is the Struct object that we created
        inputArgs[0] = new UAManagedCore.Struct[1];
        // The first element of the Struct array is the variable information to read
        ((UAManagedCore.Struct[])inputArgs[0])[0] = asd;

        // The ExecuteMethod method returns an object[] with the output parameters of the method
        object[] outputArgs = new object[1];

        // Call the ReadValues method of the OPC/UA Server
        opc.ExecuteMethod("ReadValues", inputArgs, out outputArgs); // OPCUA\OPCUAClient\OPCUAClient\Module.xml.in

        // The outputArgs array contains the output parameters of the ReadValues method
        var output = (UAManagedCore.Struct[])outputArgs[0];
        Log.Info(output[0].Values[0].ToString());
    }

    [ExportMethod]
    public void DeleteSource(string datasource)
    {
        string query = $"DELETE FROM Datasources WHERE Name = \"{datasource}\"";
        myStore.Query(query, out string[] Header, out object[,] ResultSet);
    }

    [ExportMethod]
    public void SelectedSource(NodeId dataGrid)
    {
        var selectedAsset = Owner.Get<Datasource>("Selected_Source");
        Log.Info("listbox Name", selectedAsset.Name);
        string query = $"SELECT * FROM Datasources WHERE Name = \"{selectedAsset.Name}\"";
        myStore.Query(query, out string[] Header, out object[,] ResultSet);
        selectedAsset.Path = ResultSet[0, 1].ToString();
        selectedAsset.Type = ResultSet[0, 4].ToString();
        //var dataGridItem = InformationModel.Get<DataGrid>(dataGrid);
        //var query2 = $"SELECT* FROM Datapoints WHERE Datasource = \"{selectedAsset.Name}\" ORDER BY \"Path\"";
        //dataGridItem.Query = query2;
        //Log.Info("Query ", query2);
    }

    [ExportMethod]
    public void AddNewDatapoint()
    {
        var newDatapoint = Owner.Get<Datasource>("New_Datapoint");
        var selectedSource = Owner.Get<Datasource>("Selected_Source");
        Table assetsTable = myStore.Tables.Get<Table>("Datapoints");
        string[] dbColumns = {"Path", "Type", "Datasource" };
        var values = new object[1, 3];
        values[0, 0] = newDatapoint.Path;
        values[0, 1] = newDatapoint.Type;
        values[0, 2] = selectedSource.Name;
        Log.Info("Datasource ", selectedSource.Name);
        assetsTable.Insert(dbColumns, values);
    }

    [ExportMethod]
    public void DeleteDatapoint(NodeId dataGrid)
    {
        var dataGridItem = InformationModel.Get<DataGrid>(dataGrid);
        var selectedRow = InformationModel.Get(dataGridItem.UISelectedItem);
        var selectedPath = selectedRow.Children.OfType<IUAVariable>().FirstOrDefault(x => x.BrowseName == "Path");
        var selectedSource = selectedRow.Children.OfType<IUAVariable>().FirstOrDefault(x => x.BrowseName == "Datasource");
        Log.Info("Deleting ", $"{(string)selectedPath.Value}");
        myStore.Query($"DELETE FROM Datapoints WHERE Path = \"{(string)selectedPath.Value.Value}\" AND Datasource =  \"{(string)selectedSource.Value.Value}\"", out string[] Header, out object[,] ResultSet);
        dataGridItem.Refresh();

    }
}
