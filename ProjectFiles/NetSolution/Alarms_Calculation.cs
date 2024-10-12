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
using System.Collections.Generic;
using System.Collections;
#endregion

public class Alarms_Calculation : BaseNetLogic
{
    Store resultsDb;
    public override void Start()
    {
        resultsDb = Project.Current.Get<Store>("DataStores/ResultsDB");
        resultsDb.Query("DELETE FROM AlarmsPerDay", out _, out _);

        AlarmsResponse data = new AlarmsResponse
        {
            Media_dia = 491,
            Media_hora = 144,
            Media_10min = 90,
            Max_inundaciones = 880,
            Media_inundaciones = 537,
            Menor_150 = 2,
            Menor_300 = 3,
            P1_por = 3,
            P2_por = 32,
            P3_por = 8,
            P4_por = 55,
            Contribucion_mas_repetidas = 4,
            Data_hora_mayor = 41,
            Tiempo_inundacion = 0,
            Porcentaje_inun = 33,
            Parloteo = 1,
            Alarms_announced = 4910,
            Likely_missed_alarms = 96,
            Tiempo = new List<string>
            {
                "2024-10-06T00:00:00.000000000",
                "2024-10-07T00:00:00.000000000",
                "2024-10-08T00:00:00.000000000",
                "2024-10-09T00:00:00.000000000",
                "2024-10-10T00:00:00.000000000"
            },
            Alarmas_dia = new List<int> { 0, 1233, 1776, 4, 34},
            Top10_frequentalarms = new List<string>
            {
                "FIT01_BIA04_Alm_HiHi", "FIT01_BIA04_Alm_Hi", "FIT01_BIA04_Alm_Lo",
                "FIT01_BIA04_Alm_LoLo", "PIT01_BIA04_Alm_Hi", "PIT01_BIA04_Alm_HiHi",
                "[PLC]", "FIT01_BIA05_Alm_Hi", "FIT01_BIA05_Alm_HiHi", "FIT01_BIA05_Alm_Lo"
            },
            Top10_frequentalarmsdata = new List<int> { 36, 34, 30, 30, 17, 15, 11, 11, 11, 11 },
            Top10chattering = new List<string> { "[PLC]" },
            Top10chatteringdata = new List<int> { 3 }
        };
        string[] scColumns = { "Timestamp", "LocalTimestamp", "Count" };

        for (int i = 0; i < data.Alarmas_dia.Count; i++)
        {
            var values = new object[1, 3];
            values[0, 0] = DateTime.Parse(data.Tiempo[i]);
            values[0, 1] = DateTime.Parse(data.Tiempo[i]);
            values[0, 2] = data.Alarmas_dia[i];
            resultsDb.Insert("AlarmsPerDay", scColumns, values);
        }
        var result_object = Project.Current.Get<Alarms_General_Result>("Model/Results/Alarms_General_Result");
        result_object.max_10_minutes = data.Media_10min;
        result_object.flood_mean = data.Media_inundaciones;
        result_object.days_less_150 = data.Menor_150;
        result_object.days_less_300 = data.Menor_300;
        result_object.likely_missed = data.Likely_missed_alarms;
        result_object.anoounced_count = data.Alarms_announced;
        result_object.top_30_contribution = data.Contribucion_mas_repetidas;
        result_object.hours_with_more_30 = data.Menor_300;
        result_object.hours_with_more_30 = data.Data_hora_mayor;
        result_object.half_hour_with_10_alarms = data.Media_hora;
        result_object.time_in_flood = data.Tiempo_inundacion;
        result_object.time_in_chattering = 0;


        try
        {
            var hora = result_object.AlarmsPerDay.GetVariable("Hora");
            Log.Info("Hora", hora.GetType().Name);

            result_object.AlarmsPerDay.GetVariable("Dia").Value = data.Media_dia;
            result_object.AlarmsPerDay.GetVariable("Hora").Value = data.Media_hora;
            result_object.AlarmsPerDay.GetVariable("10min").Value = data.Media_10min;
        }
        catch (Exception ex) {
            Log.Warning("Error", ex.Message);
        }

        try
        {
            result_object.PriorityDistribution.GetVariable("1").Value = data.P1_por;
            result_object.PriorityDistribution.GetVariable("2").Value = data.P2_por;
            result_object.PriorityDistribution.GetVariable("3").Value = data.P3_por;
            result_object.PriorityDistribution.GetVariable("4").Value = data.P4_por;
        }
        catch (Exception ex) { }
        CleanAndFill(result_object.Top10Chattering, data.Top10chattering);
        CleanAndFill(result_object.Top10Frequent, data.Top10_frequentalarms);
        CleanAndFill(result_object.Top10Permanent, new List<string>());

    }

    void CleanAndFill(IUANode node, List<string> names)
    {
        foreach (IUANode item in node.Children)
            node.Remove(item);

        foreach (var item in names)
        {
            IUAVariable newVariable = InformationModel.MakeVariable(item, OpcUa.DataTypes.Float);
            newVariable.Value = 0;
            node.Add(newVariable);
        }
    }
    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
}


public class AlarmsResponse
{
    public int Media_dia { get; set; }
    public int Media_hora { get; set; }
    public int Media_10min { get; set; }
    public int Max_inundaciones { get; set; }
    public int Media_inundaciones { get; set; }
    public int Menor_150 { get; set; }
    public int Menor_300 { get; set; }
    public int P1_por { get; set; }
    public int P2_por { get; set; }
    public int P3_por { get; set; }
    public int P4_por { get; set; }
    public int Contribucion_mas_repetidas { get; set; }
    public int Data_hora_mayor { get; set; }
    public int Tiempo_inundacion { get; set; }
    public int Porcentaje_inun { get; set; }
    public int Parloteo { get; set; }
    public int Alarms_announced { get; set; }
    public int Likely_missed_alarms { get; set; }

    // List for "tiempo" which contains DateTime strings
    public List<string> Tiempo { get; set; }

    // List for "alarmas_dia" which contains string numbers
    public List<int> Alarmas_dia { get; set; }

    // List for "top10_frequentalarms" which contains alarm strings
    public List<string> Top10_frequentalarms { get; set; }

    // List for "top10_frequentalarmsdata" which contains integers
    public List<int> Top10_frequentalarmsdata { get; set; }

    // List for "top10chattering" which contains chattering strings
    public List<string> Top10chattering { get; set; }

    // List for "top10chatteringdata" which contains integers
    public List<int> Top10chatteringdata { get; set; }
}