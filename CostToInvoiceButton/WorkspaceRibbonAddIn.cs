﻿using CostToInvoiceButton.SOAPICCS;
using MoreLinq;
using Newtonsoft.Json;
using RestSharp;
using RightNow.AddIns.AddInViews;
using RightNow.AddIns.Common;
using System;
using System.AddIn;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace CostToInvoiceButton
{
    public class WorkspaceRibbonAddIn : Panel, IWorkspaceRibbonButton
    {
        DoubleScreen doubleScreen;
        public List<RootObject> InsertedServices { get; set; }
        private IRecordContext recordContext { get; set; }
        private IGlobalContext global { get; set; }
        private bool inDesignMode { get; set; }
        private RightNowSyncPortClient clientORN { get; set; }
        public DataGridView DgvServicios { get; set; }
        public List<Services> servicios { get; set; }
        public IIncident Incident { get; set; }
        public int IncidentID { get; set; }
        public string ArrivalAirportIncident { get; set; }
        public string DepartureAirportIncident { get; set; }


        public WorkspaceRibbonAddIn(bool inDesignMode, IRecordContext RecordContext, IGlobalContext globalContext)
        {
            if (inDesignMode == false)
            {
                global = globalContext;
                recordContext = RecordContext;
                this.inDesignMode = inDesignMode;
            }
        }
        public new void Click()
        {
            try
            {
                if (Init())
                {
                    string Utilidad = "";
                    string Royalty = "";
                    string Combustible = "";
                    string CombustibleI = "";
                    string SENEAM = "";
                    string SeneamCat = "";
                    string ICAO = "";
                    string SRType = "";
                    string ClientType = "";
                    string ClientName = "";
                    string FuelType = "";


                    Incident = (IIncident)recordContext.GetWorkspaceRecord(WorkspaceRecordType.Incident);
                    IList<ICfVal> IncCustomFieldList = Incident.CustomField;
                    if (IncCustomFieldList != null)
                    {
                        foreach (ICfVal inccampos in IncCustomFieldList)
                        {
                            if (inccampos.CfId == 58)
                            {
                                ClientName = inccampos.ValStr;
                            }
                            if (inccampos.CfId == 61)
                            {
                                Royalty = inccampos.ValStr;
                            }
                            if (inccampos.CfId == 62)
                            {
                                Utilidad = inccampos.ValStr;
                            }
                            if (inccampos.CfId == 63)
                            {
                                Combustible = inccampos.ValStr;
                            }
                            if (inccampos.CfId == 81)
                            {
                                SeneamCat = inccampos.ValStr;
                            }
                            if (inccampos.CfId == 82)
                            {
                                CombustibleI = inccampos.ValStr;
                            }
                            if (inccampos.CfId == 85)
                            {
                                SENEAM = inccampos.ValStr;
                            }

                        }
                    }
                    IncidentID = Incident.ID;

                    ICAO = getICAODesi(IncidentID);
                    SRType = GetSRType();
                    ClientType = GetClientType();
                    GetDeleteComponents();
                    CreateChildComponents();
                    if (SRType != "FBO" || SRType != "FCC")
                    {
                        ArrivalAirportIncident = GetArrivalAirportIncident(IncidentID);
                        DepartureAirportIncident = GetDepartureAirportIncident(IncidentID);
                    }


                    if (SRType == "FUEL")
                    {
                        GetDeleteFuelItems();
                        FuelType = GetFuelType(IncidentID);
                        GetFuelData(ClientType, FuelType);
                        CreateFuelMinimun(ClientType, FuelType);
                        if (SENEAM == "1")
                        {
                            CreateAirNavFee();
                            if (Utilidad != "A" && !String.IsNullOrEmpty(Utilidad))
                            {
                                CreateAirNavICCS();
                            }
                        }
                    }
                    servicios = GetListServices();
                    if (SRType == "FBO")
                    {
                        var FBOServices = servicios.DistinctBy(b => b.Itinerary).ToList();
                        foreach (Services item in FBOServices)
                        {
                            ComponentChild component = new ComponentChild();
                            component.Airport = item.Airport.Replace("-", "_");
                            //component.ItemNumber = getFBOItemNumber(Convert.ToInt32(item.Itinerary));
                            component.ItemNumber = "ASFIEAP357";
                            if (!String.IsNullOrEmpty(component.ItemNumber))
                            {
                                component = GetComponentData(component);
                                if (!String.IsNullOrEmpty(component.ItemDescription))
                                {
                                    component.Incident = IncidentID;
                                    component.Itinerary = Convert.ToInt32(item.Itinerary);
                                    component.Categories = GetCategories(component.ItemNumber, component.Airport);
                                    component.Componente = "1";
                                    component.ParentPaxId = IncidentID;
                                    InsertComponent(component);
                                }
                            }
                        }
                        servicios.Clear();
                        servicios = GetListServices();
                    }
                    if (SRType == "FCC")
                    {
                        var FCCServices = servicios.DistinctBy(b => b.Itinerary).ToList();
                        ComponentChild component = new ComponentChild();
                        foreach (Services item in FCCServices)
                        {

                            component.Airport = item.Airport.Replace("-", "_");
                            //component.ItemNumber = getFBOItemNumber(Convert.ToInt32(item.Itinerary));
                            component.ItemNumber = "AIPRTFE0101";
                            if (!String.IsNullOrEmpty(component.ItemNumber))
                            {
                                component = GetComponentData(component);
                                if (!String.IsNullOrEmpty(component.ItemDescription))
                                {
                                    component.Incident = IncidentID;
                                    component.Itinerary = Convert.ToInt32(item.Itinerary);
                                    component.Categories = GetCategories(component.ItemNumber, component.Airport);
                                    component.Componente = "1";
                                    component.ParentPaxId = IncidentID;
                                    InsertComponent(component);
                                }
                            }
                            if (!AirportOpen24(Convert.ToInt32(item.Itinerary)))
                            {
                                int arrival = getArrivalAirport(Convert.ToInt32(item.Itinerary));
                                if (arrival != 0)
                                {
                                    DateTime openDate;
                                    DateTime closeDate;
                                    string open = getOpenArrivalAirport(arrival);
                                    string close = getCloseArrivalAirport(arrival);
                                    DateTime ATA = getATAItinerary(Convert.ToInt32(item.Itinerary));
                                    DateTime ATD = getATDItinerary(Convert.ToInt32(item.Itinerary));
                                    openDate = DateTime.Parse(ATA.Date.ToShortDateString() + " " + open);
                                    closeDate = DateTime.Parse(ATD.Date.ToShortDateString() + " " + close);

                                    double antelacion = (openDate - ATA).TotalMinutes;
                                    double extension = ((closeDate - ATD).TotalMinutes) + 15;
                                    double minover = 0;
                                    if (ATA.Date != ATD.Date)
                                    {
                                        if (antelacion > 0 && extension > 0)
                                        {
                                            minover = (antelacion < 0 ? 0 : antelacion) + (extension < 0 ? 0 : extension);
                                        }
                                    }

                                    minover = extension < 0 ? 0 : extension;


                                }
                            }
                            if (!GetItineraryCountries(Convert.ToInt32(item.Itinerary)))
                            {
                                component.Airport = item.Airport.Replace("-", "_");
                                //component.ItemNumber = getFBOItemNumber(Convert.ToInt32(item.Itinerary));
                                component.ItemNumber = "IPFERPS0052";
                                if (!String.IsNullOrEmpty(component.ItemNumber))
                                {
                                    component = GetComponentData(component);
                                    if (!String.IsNullOrEmpty(component.ItemDescription))
                                    {
                                        component.Incident = IncidentID;
                                        component.Itinerary = Convert.ToInt32(item.Itinerary);
                                        component.Categories = GetCategories(component.ItemNumber, component.Airport);
                                        component.Componente = "1";
                                        component.ParentPaxId = IncidentID;
                                        InsertComponent(component);
                                    }
                                }
                            }

                            if (GetMinutesLeg(Convert.ToInt32(item.Itinerary)) >= 2 && GetMinutesLeg(Convert.ToInt32(item.Itinerary)) < 8)
                            {
                                component.Airport = item.Airport.Replace("-", "_");
                                //component.ItemNumber = getFBOItemNumber(Convert.ToInt32(item.Itinerary));
                                component.ItemNumber = "OVEAIAS0131";
                                if (!String.IsNullOrEmpty(component.ItemNumber))
                                {
                                    component = GetComponentData(component);
                                    if (!String.IsNullOrEmpty(component.ItemDescription))
                                    {
                                        component.Incident = IncidentID;
                                        component.Itinerary = Convert.ToInt32(item.Itinerary);
                                        component.Categories = GetCategories(component.ItemNumber, component.Airport);
                                        component.Componente = "1";
                                        component.ParentPaxId = IncidentID;
                                        InsertComponent(component);
                                    }
                                }
                                if (GetMinutesLeg(Convert.ToInt32(item.Itinerary)) >= 8)
                                {
                                    component.Airport = item.Airport.Replace("-", "_");
                                    //component.ItemNumber = getFBOItemNumber(Convert.ToInt32(item.Itinerary));
                                    component.ItemNumber = "OHANIAS0129";
                                    if (!String.IsNullOrEmpty(component.ItemNumber))
                                    {
                                        component = GetComponentData(component);
                                        if (!String.IsNullOrEmpty(component.ItemDescription))
                                        {
                                            component.Incident = IncidentID;
                                            component.Itinerary = Convert.ToInt32(item.Itinerary);
                                            component.Categories = GetCategories(component.ItemNumber, component.Airport);
                                            component.Componente = "1";
                                            component.ParentPaxId = IncidentID;
                                            InsertComponent(component);
                                        }
                                    }

                                }

                            }


                        }
                        servicios.Clear();
                        servicios = GetListServices();
                    }

                    doubleScreen = new DoubleScreen(global, recordContext);
                    DgvServicios = ((DataGridView)doubleScreen.Controls["dataGridServicios"]);
                    DgvServicios.DataSource = servicios;
                    /*
                    DgvServicios.Columns[3].Visible = false;
                    DgvServicios.Columns[4].Visible = false;
                    DgvServicios.Columns[5].Visible = false;
                    DgvServicios.Columns[6].Visible = false;
                    DgvServicios.Columns[7].Visible = false;
                    DgvServicios.Columns[8].Visible = false;
                    DgvServicios.Columns[13].Visible = false;
                    */

                    DgvServicios.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
                    ((System.Windows.Forms.Label)doubleScreen.Controls["lblSrType"]).Text = SRType.ToUpper();
                    ((System.Windows.Forms.Label)doubleScreen.Controls["lblIdIncident"]).Text = IncidentID.ToString();
                    ((System.Windows.Forms.Label)doubleScreen.Controls["lblCurrency"]).Text = GetCurrency();
                    ((TextBox)doubleScreen.Controls["txtUtilidad"]).Text = Utilidad;
                    ((TextBox)doubleScreen.Controls["txtClientName"]).Text = ClientName;
                    ((TextBox)doubleScreen.Controls["txtRoyalty"]).Text = Royalty;
                    ((TextBox)doubleScreen.Controls["txtCombustible"]).Text = Combustible;
                    ((TextBox)doubleScreen.Controls["txtCombustibleI"]).Text = CombustibleI;
                    ((TextBox)doubleScreen.Controls["txtClientInfo"]).Text = ClientType;
                    ((TextBox)doubleScreen.Controls["txtICAOD"]).Text = ICAO;

                    ((TextBox)doubleScreen.Controls["txtArrivalIncident"]).Text = ArrivalAirportIncident;
                    ((TextBox)doubleScreen.Controls["txtDepartureIncident"]).Text = DepartureAirportIncident;



                    ((ComboBox)doubleScreen.Controls["cboCurrency"]).Text = SRType == "FUEL" ? "MXN" : GetCurrency();

                    if (SRType == "CATERING")
                    {
                        ((TextBox)doubleScreen.Controls["txtInvoice"]).Visible = false;
                        ((System.Windows.Forms.Label)doubleScreen.Controls["lblInvoice"]).Text = "";
                    }



                    doubleScreen.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en Click: " + ex.Message + "DEt" + ex.StackTrace);
            }
        }
        public bool Init()
        {
            try
            {
                bool result = false;
                EndpointAddress endPointAddr = new EndpointAddress(global.GetInterfaceServiceUrl(ConnectServiceType.Soap));
                BasicHttpBinding binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential);
                binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;
                binding.ReceiveTimeout = new TimeSpan(0, 10, 0);
                binding.MaxReceivedMessageSize = 1048576; //1MB
                binding.SendTimeout = new TimeSpan(0, 10, 0);
                clientORN = new RightNowSyncPortClient(binding, endPointAddr);
                BindingElementCollection elements = clientORN.Endpoint.Binding.CreateBindingElements();
                elements.Find<SecurityBindingElement>().IncludeTimestamp = false;
                clientORN.Endpoint.Binding = new CustomBinding(elements);
                global.PrepareConnectSession(clientORN.ChannelFactory);
                if (clientORN != null)
                {
                    result = true;
                }

                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en INIT: " + ex.Message);
                return false;

            }
        }
        public double GetMinutesLeg(int Itinerarie)
        {
            try
            {
                double minutes = 0;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT (Date_Diff(ATA_ZUTC,ATD_ZUTC)/60) FROM CO.Itinerary WHERE ID =" + Itinerarie + "";
                global.LogMessage(queryString);
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        minutes = Convert.ToDouble(data);
                    }
                }

                TimeSpan t = TimeSpan.FromMinutes(minutes);
                return Math.Ceiling(t.TotalHours);
            }
            catch (Exception ex)
            {
                global.LogMessage("GetMinutesLeg: " + ex.Message + "Det: " + ex.StackTrace);
                return 0;
            }
        }

        public string GetCurrency()
        {
            string cur = "";
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT CustomFields.c.sr_currency.name FROM Incident WHERE ID = " + IncidentID + "";
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    cur = data;
                }
            }

            return cur;
        }

        public string GetFirstAirport()
        {
            string air = "";
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT DISTINCT ArrivalAirport.ICAO_IATACODE FROM CO.Fueling WHERE Incident = " + IncidentID + " ORDER BY CreatedTime";
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    air = data.Replace("-", "_");
                }
            }

            return air;
        }

        public double GetLitersSum(int FuelingId)
        {
            double sum = 0;
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT Liters FROM CO.Fueling WHERE ID =" + FuelingId + "";
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    sum = Convert.ToDouble(data);
                }
            }

            return sum;
        }
        public string GetAirportUse(int Fueling)

        {
            try
            {
                string Use = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ArrivalAirport.AirportUse.Name FROM CO.Fueling WHERE ID =" + Fueling + "";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Use = data;
                    }
                }

                return Use;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetAirportUse: " + ex.Message + "Detail: " + ex.StackTrace);
                return null;

            }
        }
        public string getFBOItemNumber(int Itinerary)
        {
            try
            {
                string Grupo = "";
                string IM = "";

                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT DISTINCT Itinerary.ArrivalAirport.AirportGroup.Name FROM CO.Services  WHERE Incident = " + IncidentID + "  AND Itinerary =" + Itinerary + "";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Grupo = data;
                    }
                }
                switch (Grupo)
                {
                    case "GAP":
                        IM = "AGFERAF0141";
                        break;
                    case "OEA":
                        IM = "AOFERAF0142";
                        break;
                    case "ASUR":
                        IM = "AAFERAF0140";
                        break;
                    case "AIQ":
                        IM = "AAFERAF298";
                        break;
                    case "AMAIT":
                        IM = "AAFERAF0143";
                        break;
                    case "OMA":
                        IM = "AOFERAF0139";
                        break;
                    case "SEA":
                        IM = "ASFERAF0144";
                        break;
                    default:
                        IM = "";
                        break;
                }
                return IM;
            }
            catch (Exception ex)
            {

                MessageBox.Show("getFBOItemNumber: " + ex.Message + "Detail: " + ex.StackTrace);

                return null;
            }
        }

        public bool AirportOpen24(int Itinerarie)
        {
            bool open = true;

            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT ArrivalAirport.HoursOpen24 FROM Co.Itinerary  WHERE ID =" + Itinerarie;
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    open = data == "1" ? true : false;
                }
            }

            return open;
        }

        public int getArrivalAirport(int Itinerarie)
        {
            int arriv = 0;

            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT ArrivalAirport FROM Co.Itinerary  WHERE ID =" + Itinerarie;
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    arriv = String.IsNullOrEmpty(data) ? 0 : Convert.ToInt32(data);
                }
            }
            return arriv;
        }

        public string getOpenArrivalAirport(int Arrival)
        {
            string opens = "";
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT OpensZuluTime FROM Co.Airport_WorkingHours  WHERE Airports =" + Arrival;
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    opens = data;
                }
            }
            return opens;
        }

        public string getCloseArrivalAirport(int Arrival)
        {
            string closes = "";
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT ClosesZuluTime  FROM Co.Airport_WorkingHours  WHERE Airports =" + Arrival;
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    closes = data;
                }
            }
            return closes;
        }
        public DateTime getATAItinerary(int Itinerarie)
        {
            try
            {
                string ATA = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ATA_ZUTC FROM Co.Itinerary WHERE ID = " + Itinerarie;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        ATA = data;
                    }
                }
                return DateTime.Parse(ATA);
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetListServices: " + ex.Message + "Detail: " + ex.StackTrace);
                return DateTime.Now;
            }
        }

        public DateTime getATDItinerary(int Itinerarie)
        {
            try
            {
                string ATD = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ATA_ZUTC FROM Co.Itinerary WHERE ID = " + Itinerarie;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        ATD = data;
                    }
                }
                return DateTime.Parse(ATD);
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetListServices: " + ex.Message + "Detail: " + ex.StackTrace);
                return DateTime.Now;
            }
        }
        public void CreateAirNavFee()
        {
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT SUM(Liters) Suma,date_trunc(VoucherDateTime,'day') FROM CO.Fueling WHERE Incident = " + IncidentID + " AND ArrivalAirport.AirportUse.Name = 'Federal'  GROUP BY date_trunc(VoucherDateTime,'day')";
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    Services service = new Services();
                    Char delimiter = '|';
                    string[] substrings = data.Split(delimiter);
                    ComponentChild component = new ComponentChild();
                    component.Airport = ArrivalAirportIncident;
                    component.ItemNumber = "ANFERAS0013";
                    component.Incident = IncidentID;
                    component.ParentPaxId = IncidentID;
                    component.FuelId = int.Parse(DateTime.Parse(substrings[1]).ToString("yyMMdd"));
                    component.Componente = "1";
                    component = GetComponentData(component);
                    component.Categories = GetCategories(component.ItemNumber, component.Airport);
                    if (!string.IsNullOrEmpty(component.ItemDescription))
                    {
                        InsertComponent(component);
                    }
                }
            }
        }
        public void CreateAirNavICCS()
        {
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT SUM(Liters) Suma,date_trunc(VoucherDateTime,'day') FROM CO.Fueling WHERE Incident = " + IncidentID + " AND ArrivalAirport.AirportUse.Name = 'Federal'  GROUP BY date_trunc(VoucherDateTime,'day')";
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    Services service = new Services();
                    Char delimiter = '|';
                    string[] substrings = data.Split(delimiter);
                    ComponentChild component = new ComponentChild();
                    component.Airport = ArrivalAirportIncident;
                    component.ItemNumber = "ANIASAS0015";
                    component.Incident = IncidentID;
                    component.ParentPaxId = IncidentID;
                    component.FuelId = int.Parse(DateTime.Parse(substrings[1]).ToString("yyMMdd"));
                    component.Componente = "1";
                    component = GetComponentData(component);
                    component.Categories = GetCategories(component.ItemNumber, component.Airport);
                    if (!string.IsNullOrEmpty(component.ItemDescription))
                    {
                        InsertComponent(component);
                    }
                }
            }
        }
        public List<Services> GetListServices()
        {
            try
            {
                List<Services> services = new List<Services>();
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ID,ItemNumber,ItemDescription,Airport,IDProveedor,Costo,Precio,InternalInvoice,Itinerary,Paquete,Componente,Informativo,ParentPaxID,Categories,fuel_id,CategoriaRoyalty FROM CO.Services WHERE Incident =" + IncidentID + " ORDER BY ID ASC, Itinerary ASC, ParentPaxId ASC";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;

                    foreach (String data in rowData)
                    {
                        Services service = new Services();
                        Char delimiter = '|';
                        string[] substrings = data.Split(delimiter);
                        service.ID = substrings[0];
                        service.ItemNumber = substrings[1];
                        service.Description = substrings[2].Replace('"', ' ').Trim();
                        service.Airport = substrings[3].Replace('_', '-').Trim();
                        service.Supplier = substrings[4].Replace('"', ' ').Trim();
                        service.Cost = substrings[5];
                        service.Price = substrings[6];
                        service.InvoiceInternal = substrings[7];
                        service.Itinerary = substrings[8];
                        service.Pax = substrings[9] == "1" ? "Yes" : "No";
                        service.Task = substrings[10] == "1" ? "Yes" : "No";
                        service.Informative = substrings[11] == "1" ? "Yes" : "No";
                        service.ParentPax = substrings[12];
                        service.Categorias = substrings[13];
                        service.FuelId = substrings[14];
                        service.RoyaltyItem = substrings[15];
                        services.Add(service);
                    }
                }
                return services;
            }
            catch (Exception ex)
            {

                MessageBox.Show("GetListServices: " + ex.Message + "Detail: " + ex.StackTrace);

                return null;
            }
        }
        public void GetDeleteFuelItems()
        {
            try
            {

                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ID FROM CO.Services  WHERE fuel_id IS NOT NULL AND Incident = " + IncidentID;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                if (queryCSV.CSVTables.Length > 0)
                {
                    foreach (CSVTable table in queryCSV.CSVTables)
                    {
                        String[] rowData = table.Rows;
                        foreach (String data in rowData)
                        {
                            DeleteServices(Convert.ToInt32(data));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void GetDeleteComponents()
        {
            try
            {

                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ID FROM CO.Services WHERE Componente = '1' AND Incident = " + IncidentID;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                if (queryCSV.CSVTables.Length > 0)
                {
                    foreach (CSVTable table in queryCSV.CSVTables)
                    {
                        String[] rowData = table.Rows;
                        foreach (String data in rowData)
                        {
                            DeleteServices(Convert.ToInt32(data));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void DeleteServices(int id)
        {
            try
            {

                var client = new RestClient("https://iccsmx.custhelp.com/");
                var request = new RestRequest("/services/rest/connect/v1.4/CO.Services/" + id, Method.DELETE)
                {
                    RequestFormat = DataFormat.Json
                };
                request.AddHeader("Authorization", "Basic ZW9saXZhczpTaW5lcmd5KjIwMTg=");
                request.AddHeader("X-HTTP-Method-Override", "DELETE");
                request.AddHeader("OSvC-CREST-Application-Context", "Delete Service");

                IRestResponse response = client.Execute(request);
                var content = response.Content;
                if (String.IsNullOrEmpty(content))
                {
                    //MessageBox.Show(content);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("DElete: " + ex.InnerException.ToString());
            }
        }
        public void CreateChildComponents()
        {
            try
            {

                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ID,Airport,ItemNumber,Itinerary FROM CO.Services WHERE Paquete = '1' AND COMPONENTE IS NULL AND  Incident =  " + IncidentID;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                if (queryCSV.CSVTables.Length > 0)
                {
                    foreach (CSVTable table in queryCSV.CSVTables)
                    {
                        String[] rowData = table.Rows;
                        foreach (String data in rowData)
                        {
                            ComponentChild component = new ComponentChild();
                            Char delimiter = '|';
                            String[] substrings = data.Split(delimiter);
                            component.ID = Convert.ToInt32(substrings[0]);
                            component.Airport = substrings[1];
                            component.ItemNumber = substrings[2];
                            component.Itinerary = Convert.ToInt32(substrings[3]);
                            GetComponents(component);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void GetComponents(ComponentChild component)
        {
            try
            {

                string envelope = "<soapenv:Envelope" +
                 "   xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"" +
                 "   xmlns:typ=\"http://xmlns.oracle.com/apps/scm/productModel/items/structures/structureServiceV2/types/\"" +
                 "   xmlns:typ1=\"http://xmlns.oracle.com/adf/svc/types/\">" +
                 "<soapenv:Header/>" +
                 "<soapenv:Body>" +
                 "<typ:findStructure>" +
                 "<typ:findCriteria>" +
                 "<typ1:fetchStart>0</typ1:fetchStart>" +
                 "<typ1:fetchSize>-1</typ1:fetchSize>" +
                 "<typ1:filter>" +
                 "<typ1:group>" +
                 "<typ1:item>" +
                 "<typ1:conjunction>And</typ1:conjunction>" +
                 "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                 "<typ1:attribute>ItemNumber</typ1:attribute>" +
                 "<typ1:operator>CONTAINS</typ1:operator>" +
                 "<typ1:value>" + component.ItemNumber + "</typ1:value>" +
                 "</typ1:item>" +
                 "<typ1:item>" +
                 "<typ1:conjunction>And</typ1:conjunction>" +
                 "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                 "<typ1:attribute>OrganizationCode</typ1:attribute>" +
                 "<typ1:operator>CONTAINS</typ1:operator>" +
                 "<typ1:value>" + component.Airport + "</typ1:value>" +
                 "</typ1:item>" +
                 "</typ1:group> " +
                 "</typ1:filter>" +
                 "<typ1:findAttribute>Component</typ1:findAttribute>" +
                 "<typ1:childFindCriteria>" +
                 "<typ1:findAttribute>ComponentItemNumber</typ1:findAttribute>" +
                 "<typ1:childAttrName>Component</typ1:childAttrName>" +
                 "</typ1:childFindCriteria>" +
                 "</typ:findCriteria>" +
                 "<typ:findControl>" +
                 "<typ1:retrieveAllTranslations>true</typ1:retrieveAllTranslations>" +
                 "</typ:findControl>" +
                 "</typ:findStructure>" +
                 "</soapenv:Body>" +
                 "</soapenv:Envelope>";
                byte[] byteArray = Encoding.UTF8.GetBytes(envelope);
                byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes("itotal" + ":" + "Oracle123");
                string credentials = System.Convert.ToBase64String(toEncodeAsBytes);
                HttpWebRequest request =
                 (HttpWebRequest)WebRequest.Create("https://egqy-test.fa.us6.oraclecloud.com:443/fscmService/StructureServiceV2");
                request.Method = "POST";
                request.ContentType = "text/xml;charset=UTF-8";
                request.ContentLength = byteArray.Length;
                request.Headers.Add("Authorization", "Basic " + credentials);
                request.Headers.Add("SOAPAction", "http://xmlns.oracle.com/apps/scm/productModel/items/structures/structureServiceV2/findStructure");
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                XDocument doc;
                XmlDocument docu = new XmlDocument();
                string result = "";
                List<ComponentChild> components = new List<ComponentChild>();
                using (WebResponse responseComponent = request.GetResponse())
                {
                    using (Stream stream = responseComponent.GetResponseStream())
                    {
                        doc = XDocument.Load(stream);
                        result = doc.ToString();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(result);
                        XmlNamespaceManager nms = new XmlNamespaceManager(xmlDoc.NameTable);
                        nms.AddNamespace("env", "http://schemas.xmlsoap.org/soap/envelope/");
                        nms.AddNamespace("wsa", "http://www.w3.org/2005/08/addressing");
                        nms.AddNamespace("typ", "http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/types/");
                        nms.AddNamespace("ns1", "http://xmlns.oracle.com/apps/scm/productModel/items/structures/structureServiceV2/");
                        XmlNodeList nodeList = xmlDoc.SelectNodes("//ns1:Component", nms);
                        foreach (XmlNode node in nodeList)
                        {
                            ComponentChild componentchild = new ComponentChild();
                            if (node.HasChildNodes)
                            {
                                if (node.LocalName == "Component")
                                {
                                    XmlNodeList nodeListvalue = node.ChildNodes;
                                    foreach (XmlNode nodeValue in nodeListvalue)
                                    {
                                        if (nodeValue.LocalName == "ComponentItemNumber")
                                        {
                                            componentchild.ParentPaxId = component.ID;
                                            componentchild.Airport = component.Airport;
                                            componentchild.Incident = IncidentID;
                                            componentchild.Componente = "1";
                                            componentchild.ItemNumber = nodeValue.InnerText;
                                            componentchild.Itinerary = component.Itinerary;
                                            componentchild.Categories = GetCategories(componentchild.ItemNumber, componentchild.Airport);
                                        }
                                    }
                                }
                            }
                            components.Add(componentchild);
                        }
                        responseComponent.Close();
                    }
                }

                if (components.Count > 0)
                {
                    foreach (ComponentChild comp in components)
                    {
                        ComponentChild comp2 = new ComponentChild();
                        comp2 = GetComponentData(comp);
                        if (!String.IsNullOrEmpty(comp2.ItemDescription))
                        {
                            InsertComponent(comp2);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        public ComponentChild GetComponentData(ComponentChild component)
        {
            try
            {
                string envelope = "<soapenv:Envelope" +
                                          "   xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"" +
                                          "   xmlns:typ=\"http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/types/\"" +
                                          "   xmlns:typ1=\"http://xmlns.oracle.com/adf/svc/types/\">" +
                                          "<soapenv:Header/>" +
                                          "<soapenv:Body>" +
                                          "<typ:findItem>" +
                                          "<typ:findCriteria>" +
                                          "<typ1:fetchStart>0</typ1:fetchStart>" +
                                          "<typ1:fetchSize>-1</typ1:fetchSize>" +
                                          "<typ1:filter>" +
                                          "<typ1:group>" +
                                          "<typ1:item>" +
                                          "<typ1:conjunction>And</typ1:conjunction>" +
                                          "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                                          "<typ1:attribute>ItemNumber</typ1:attribute>" +
                                          "<typ1:operator>=</typ1:operator>" +
                                          "<typ1:value>" + component.ItemNumber + "</typ1:value>" +
                                          "</typ1:item>" +
                                          "<typ1:item>" +
                                          "<typ1:conjunction>And</typ1:conjunction>" +
                                          "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                                          "<typ1:attribute>OrganizationCode</typ1:attribute>" +
                                          "<typ1:operator>=</typ1:operator>" +
                                          "<typ1:value>IO_AEREO_" + component.Airport + "</typ1:value>" +
                                          "</typ1:item>" +
                                          /*  "<typ1:item>" +
                                      "<typ1:conjunction>And</typ1:conjunction>" +
                                      "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                                      "<typ1:attribute>ItemCategory</typ1:attribute>" +
                                      "<typ1:nested>" +
                                      "<typ1:group>" +
                                      "<typ1:item>" +
                                      "<typ1:conjunction>And</typ1:conjunction>" +
                                      "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                                      "<typ1:attribute>CategoryName</typ1:attribute>" +
                                      "<typ1:operator>=</typ1:operator>" +
                                      "<typ1:value>FCC</typ1:value>" +
                                      "</typ1:item>" +
                                      "</typ1:group>" +
                                      "</typ1:nested>" +
                                      "</typ1:item>" +*/
                                          "</typ1:group>" +
                                          "</typ1:filter>" +
                                          "<typ1:findAttribute>ItemDescription</typ1:findAttribute>" +
                                          "<typ1:findAttribute>ItemDFF</typ1:findAttribute>" +
                                          "</typ:findCriteria>" +
                                          "<typ:findControl>" +
                                          "<typ1:retrieveAllTranslations>true</typ1:retrieveAllTranslations>" +
                                          "</typ:findControl>" +
                                          "</typ:findItem>" +
                                          "</soapenv:Body>" +
                                          "</soapenv:Envelope>";
                global.LogMessage(envelope);

                byte[] byteArray = Encoding.UTF8.GetBytes(envelope);
                byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes("itotal" + ":" + "Oracle123");
                string credentials = System.Convert.ToBase64String(toEncodeAsBytes);
                HttpWebRequest request =
                 (HttpWebRequest)WebRequest.Create("https://egqy-test.fa.us6.oraclecloud.com:443/fscmService/ItemServiceV2");
                request.Method = "POST";
                request.ContentType = "text/xml;charset=UTF-8";
                request.ContentLength = byteArray.Length;
                request.Headers.Add("Authorization", "Basic " + credentials);
                request.Headers.Add("SOAPAction", "http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/findItem");
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                XDocument doc;
                XmlDocument docu = new XmlDocument();
                string result = "";
                using (WebResponse responseComponentGet = request.GetResponse())
                {
                    using (Stream stream = responseComponentGet.GetResponseStream())
                    {
                        doc = XDocument.Load(stream);
                        result = doc.ToString();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(result);
                        XmlNamespaceManager nms = new XmlNamespaceManager(xmlDoc.NameTable);
                        nms.AddNamespace("env", "http://schemas.xmlsoap.org/soap/envelope/");
                        nms.AddNamespace("wsa", "http://www.w3.org/2005/08/addressing");
                        nms.AddNamespace("typ", "http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/types/");
                        nms.AddNamespace("ns0", "http://xmlns.oracle.com/adf/svc/types/");
                        nms.AddNamespace("ns1", "http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/");

                        XmlNodeList nodeList = xmlDoc.SelectNodes("//ns0:Value", nms);
                        foreach (XmlNode node in nodeList)
                        {
                            if (node.HasChildNodes)
                            {
                                if (node.LocalName == "Value")
                                {
                                    XmlNodeList nodeListvalue = node.ChildNodes;
                                    foreach (XmlNode nodeValue in nodeListvalue)
                                    {
                                        if (nodeValue.LocalName == "ItemDescription")
                                        {
                                            component.ItemDescription = nodeValue.InnerText.Trim().Replace("/", "");
                                        }
                                        if (nodeValue.LocalName == "ItemDFF")
                                        {
                                            XmlNodeList nodeListDeff = nodeValue.ChildNodes;
                                            {
                                                foreach (XmlNode nodeDeff in nodeListDeff)
                                                {
                                                    if (nodeDeff.LocalName == "xxParticipacionCobro")
                                                    {
                                                        component.ParticipacionCobro = nodeDeff.InnerText == "SI" ? "1" : "0";
                                                        component.CategoriaRoyalty = nodeDeff.InnerText == "SI" ? "1" : "0";
                                                    }
                                                    if (nodeDeff.LocalName == "xxCategoriaRoyalty")
                                                    {
                                                        //component.CategoriaRoyalty = nodeDeff.InnerText;
                                                    }
                                                    if (nodeDeff.LocalName == "xxPagos")
                                                    {
                                                        component.Pagos = nodeDeff.InnerText;
                                                    }
                                                    if (nodeDeff.LocalName == "xxClasificacionPago")
                                                    {
                                                        component.ClasificacionPagos = nodeDeff.InnerText;
                                                    }
                                                    if (nodeDeff.LocalName == "cuentaGastoCx")
                                                    {
                                                        component.CuentaGasto = nodeDeff.InnerText;
                                                    }
                                                    if (nodeDeff.LocalName == "xxInformativo")
                                                    {
                                                        component.Informativo = nodeDeff.InnerText == "SI" ? "1" : "0";
                                                    }
                                                    if (nodeDeff.LocalName == "xxPaqueteInv")
                                                    {
                                                        component.Paquete = nodeDeff.InnerText == "SI" ? "1" : "0";
                                                    }
                                                }
                                            }

                                        }

                                    }
                                }
                            }
                        }

                    }
                    responseComponentGet.Close();
                }
                return component;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                return null;
            }
        }
        public void InsertComponent(ComponentChild component)
        {
            try
            {
                var client = new RestClient("https://iccsmx.custhelp.com/");
                var request = new RestRequest("/services/rest/connect/v1.4/CO.Services/", Method.POST)
                {
                    RequestFormat = DataFormat.Json
                };
                string body = "{";
                body += "\"Airport\":\"" + component.Airport + "\",";
                body += "\"ParentPaxId\":\"" + component.ParentPaxId + "\",";
                if (String.IsNullOrEmpty(component.CategoriaRoyalty))
                {
                    body += "\"CategoriaRoyalty\":null,";
                }
                else
                {
                    body += "\"CategoriaRoyalty\":\"" + component.CategoriaRoyalty + "\",";
                }
                if (String.IsNullOrEmpty(component.Categories))
                {
                    body += "\"Categories\":null,";
                }
                else
                {
                    body += "\"Categories\":\"" + component.Categories + "\",";
                }

                if (String.IsNullOrEmpty(component.ClasificacionPagos))
                {
                    body += "\"ClasificacionPagos\":null,";
                }
                else
                {
                    body += "\"ClasificacionPagos\":\"" + component.ClasificacionPagos + "\",";
                }
                body += "\"Componente\":\"" + component.Componente + "\",";

                if (String.IsNullOrEmpty(component.Costo))
                {
                    body += "\"Costo\":null,";
                }
                else
                {
                    body += "\"Costo\":\"" + component.Costo + "\",";
                }
                body += "\"Incident\":";
                body += "{";
                body += "\"id\":" + Convert.ToInt32(component.Incident) + "";
                body += "},";
                body += "\"Informativo\":\"" + component.Informativo + "\"," +
                 "\"ItemDescription\":\"" + component.ItemDescription + "\"," +
                 "\"ItemNumber\":\"" + component.ItemNumber + "\",";

                if (component.Itinerary != 0)
                {
                    body += "\"Itinerary\":";
                    body += "{";
                    body += "\"id\":" + component.Itinerary + "";
                    body += "},";
                }
                if (String.IsNullOrEmpty(component.Pagos))
                {
                    body += "\"Pagos\":null,";
                }
                else
                {
                    body += "\"Pagos\":\"" + component.Pagos + "\",";
                }
                body += "\"Paquete\":\"" + component.Paquete + "\",";
                if (String.IsNullOrEmpty(component.ParticipacionCobro))
                {
                    body += "\"ParticipacionCobro\":null,";
                }
                else
                {
                    body += "\"ParticipacionCobro\":\"" + component.ParticipacionCobro + "\",";
                }
                if (String.IsNullOrEmpty(component.FuelId.ToString()))
                {
                    body += "\"fuel_id\":null,";
                }
                else
                {
                    body += "\"fuel_id\":" + component.FuelId + ",";
                }
                if (String.IsNullOrEmpty(component.Precio))
                {
                    body += "\"Precio\":null";
                }
                else
                {
                    body += "\"Precio\":\"" + component.Precio + "\"";
                }
                body += "}";
                global.LogMessage(body);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                request.AddHeader("Authorization", "Basic ZW9saXZhczpTaW5lcmd5KjIwMTg=");
                request.AddHeader("X-HTTP-Method-Override", "POST");
                request.AddHeader("OSvC-CREST-Application-Context", "Create Service");
                IRestResponse response = client.Execute(request);
                var content = response.Content;
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    RootObject rootObject = JsonConvert.DeserializeObject<RootObject>(response.Content);
                    if (component.Paquete == "1")
                    {
                        component.ID = rootObject.id;
                        GetComponents(component);
                    }
                }
                else
                {
                    MessageBox.Show(content);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en creación de child: " + ex.Message);
            }

        }
        public string GetSRType()
        {
            try
            {
                string SRTYPE = "";
                if (IncidentID != 0)
                {
                    ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                    APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                    clientInfoHeader.AppID = "Query Example";
                    String queryString = "SELECT I.Customfields.c.sr_type.LookupName FROM Incident I WHERE id=" + IncidentID + "";
                    clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                    foreach (CSVTable table in queryCSV.CSVTables)
                    {
                        String[] rowData = table.Rows;
                        foreach (String data in rowData)
                        {
                            SRTYPE = data;
                        }
                    }
                }
                switch (SRTYPE)
                {
                    case "Catering":
                        SRTYPE = "CATERING";
                        break;
                    case "FCC":
                        SRTYPE = "FCC";
                        break;
                    case "FBO":
                        SRTYPE = "FBO";
                        break;
                    case "Fuel":
                        SRTYPE = "FUEL";
                        break;
                    case "Hangar Space":
                        SRTYPE = "GYCUSTODIA";
                        break;
                    case "SENEAM Fee":
                        SRTYPE = "SENEAM";
                        break;
                    case "Permits":
                        SRTYPE = "PERMISOS";
                        break;
                }
                return SRTYPE;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetType: " + ex.Message + "Detail: " + ex.StackTrace);
                return "";
            }
        }


        public string GetClientType()
        {
            try
            {
                string ClientType = "Nacional";
                if (IncidentID != 0)
                {
                    ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                    APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                    clientInfoHeader.AppID = "Query Example";
                    String queryString = "SELECT CustomFields.c.rfcerp FROM Incident WHERE ID =" + IncidentID + "";
                    clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                    foreach (CSVTable table in queryCSV.CSVTables)
                    {
                        String[] rowData = table.Rows;
                        foreach (String data in rowData)
                        {
                            ClientType = data;
                        }
                    }
                }
                if (ClientType == "XEXX010101000")
                {
                    ClientType = "Internacional";
                }

                return ClientType;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetType: " + ex.InnerException.ToString());
                return "";
            }
        }

        public string GetArrivalAirportIncident(int incident)
        {
            try
            {
                string Arrival = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT CustomFields.CO.Airports.LookupName  FROM Incident WHERE Id =" + incident + "";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Arrival = data.Replace("-", "_");
                    }
                }
                return Arrival;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetArrivalAirportIncident: " + ex.InnerException.ToString());
                return "";
            }
        }

        public string GetDepartureAirportIncident(int incident)
        {
            try
            {
                string Departure = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT CustomFields.CO.Airports1.LookupName  FROM Incident WHERE Id =" + incident + "";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Departure = data.Replace("-", "_");
                    }
                }
                return Departure;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetDepartureAirportIncident: " + ex.InnerException.ToString());
                return "";
            }
        }


        public string getICAODesi(int Incident)
        {
            string Icao = "";
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT CustomFields.co.Aircraft.AircraftType1.ICAODesignator  FROM Incident WHERE ID =" + Incident;
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    Icao = data;
                }
            }
            return Icao;
        }



        public string GetFuelType(int Incident)
        {
            try
            {
                string Type = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT CustomFields.co.Aircraft.AircraftType1.FuelType.Name  FROM Incident WHERE ID =" + Incident;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Type = data;
                    }
                }
                return Type;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                return "0";

            }
        }
        public bool GetItineraryCountries(int Itineray)
        {
            try
            {
                bool res = true;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ToAirport.Country.LookupName,FromAirport.Country.LookupName FROM CO.Itinerary WHERE ID  = " + Itineray + "";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Char delimiter = '|';
                        string[] substrings = data.Split(delimiter);
                        if (substrings[0] != "MX" || substrings[1] != "MX")
                        {
                            res = false;
                        }
                    }
                }

                return res;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                return true;
            }
        }
        public void GetFuelData(string ClientType, string FuelType)
        {
            string ItemN = "";
            string[] LookCountry = GetCountryLook();
            try
            {
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ArrivalAirport.ICAO_IATACODE,ToAirport.Country.LookupName ,ArrivalAirport.FuelType.Name FuelType,Id FROM CO.Fueling WHERE Incident = " + IncidentID + "";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Char delimiter = '|';
                        string[] substrings = data.Split(delimiter);

                        if (ClientType == "Internacional" && LookCountry[0] != "MX" && substrings[2] == "International")
                        {
                            if (FuelType == "AVGAS")
                            {
                                ItemN = "AGASIAS0270";
                            }
                            else
                            {
                                ItemN = "JFUEIAS0269";
                            }
                        }
                        else
                        {
                            if (FuelType == "AVGAS")
                            {
                                ItemN = "AGASIAS0011";
                            }
                            else
                            {
                                ItemN = "JFUEIAS0010";
                            }
                        }
                        ComponentChild component = new ComponentChild()
                        {
                            ItemNumber = ItemN,
                            Incident = IncidentID,
                            Airport = ArrivalAirportIncident.Replace('-', '_').Trim(),
                            Componente = "0",
                            FuelId = Convert.ToInt32(substrings[3]),
                        };
                        component.Categories = GetCategories(component.ItemNumber, component.Airport);
                        component = GetComponentData(component);
                        InsertComponent(component);

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetFuelData: " + ex.Message + "Det:" + ex.StackTrace);
            }
        }
        public void CreateFuelMinimun(string ClientType, string FuelType)
        {
            string ItemN = "AFMURAS0016";
            string[] LookCountry = GetCountryLook();

            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT ArrivalAirport.ICAO_IATACODE,ToAirport.Country.LookupName ,ArrivalAirport.FuelType.Name FuelType,Id FROM CO.Fueling WHERE Incident = " + IncidentID + "";
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    Char delimiter = '|';
                    string[] substrings = data.Split(delimiter);

                    if (ClientType == "Internacional" && LookCountry[0] != "MX" && substrings[2] == "International")
                    {
                        ItemN = "IAFMUAS0271";
                    }
                }
            }
            queryString = "SELECT SUM(Liters) Suma,date_trunc(VoucherDateTime,'day') FROM CO.Fueling WHERE Incident = " + IncidentID + " AND ArrivalAirport.AirportUse.Name = 'Federal'  GROUP BY date_trunc(VoucherDateTime,'day')  HAVING SUM(Liters) < 1500";
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10000, "|", false, false, out queryCSV, out FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    Services service = new Services();
                    Char delimiter = '|';
                    string[] substrings = data.Split(delimiter);
                    ComponentChild component = new ComponentChild();
                    component.Airport = ArrivalAirportIncident;
                    component.ItemNumber = ItemN;
                    component.Incident = IncidentID;
                    component.ParentPaxId = IncidentID;
                    component.FuelId = int.Parse(DateTime.Parse(substrings[1]).ToString("yyMMdd"));
                    component.Componente = "1";
                    component = GetComponentData(component);
                    component.Categories = GetCategories(component.ItemNumber, component.Airport);
                    if (!string.IsNullOrEmpty(component.ItemDescription))
                    {
                        InsertComponent(component);
                    }
                }
            }
        }
        public string[] GetCountryLook()
        {
            try
            {
                string[] res = new string[2];
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT CustomFields.CO.Airports1.Country.LookupName,CustomFields.CO.Airports1.LookupName FROM Incident WHERE ID =" + IncidentID + "";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Char delimiter = '|';
                        string[] substrings = data.Split(delimiter);
                        res = substrings;
                    }
                }
                return res;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                return null;
            }
        }


        public string GetCategories(string ItemN, string Airport)
        {
            try
            {
                string cats = "";
                string envelope = "<soapenv:Envelope" +
                                "   xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"" +
                                "   xmlns:typ=\"http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/types/\"" +
                                "   xmlns:typ1=\"http://xmlns.oracle.com/adf/svc/types/\">" +
                                "<soapenv:Header/>" +
                                "<soapenv:Body>" +
                       "<typ:findItem>" +
                           "<typ:findCriteria>" +
                               "<typ1:fetchStart>0</typ1:fetchStart>" +
                               "<typ1:fetchSize>-1</typ1:fetchSize>" +
                               "<typ1:filter>" +
                                   "<typ1:group>" +
                                       "<typ1:item>" +
                                           "<typ1:conjunction>And</typ1:conjunction>" +
                                           "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                                           "<typ1:attribute>ItemNumber</typ1:attribute>" +
                                           "<typ1:operator>=</typ1:operator>" +
                                           "<typ1:value>" + ItemN + "</typ1:value>" +
                                       "</typ1:item>" +
                                       "<typ1:item>" +
                                           "<typ1:conjunction>And</typ1:conjunction>" +
                                           "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                                           "<typ1:attribute>OrganizationCode</typ1:attribute>" +
                                           "<typ1:operator>=</typ1:operator>" +
                                           "<typ1:value>IO_AEREO_" + Airport + "</typ1:value>" +
                                       "</typ1:item>" +
                                   "</typ1:group>" +
                               "</typ1:filter>" +
                               "<typ1:findAttribute>ItemCategory</typ1:findAttribute>" +
                           "</typ:findCriteria>" +
                           "<typ:findControl>" +
                               "<typ1:retrieveAllTranslations>true</typ1:retrieveAllTranslations>" +
                           "</typ:findControl>" +
                       "</typ:findItem>" +
                    "</soapenv:Body>" +
                    "</soapenv:Envelope>";

                byte[] byteArray = Encoding.UTF8.GetBytes(envelope);
                byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes("itotal" + ":" + "Oracle123");
                string credentials = System.Convert.ToBase64String(toEncodeAsBytes);
                HttpWebRequest request =
                 (HttpWebRequest)WebRequest.Create("https://egqy-test.fa.us6.oraclecloud.com:443/fscmService/ItemServiceV2");
                request.Method = "POST";
                request.ContentType = "text/xml;charset=UTF-8";
                request.ContentLength = byteArray.Length;
                request.Headers.Add("Authorization", "Basic " + credentials);
                request.Headers.Add("SOAPAction", "http://xmlns.oracle.com/apps/scm/productModel/items/fscmService/ItemServiceV2");
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                XDocument doc;
                XmlDocument docu = new XmlDocument();
                string result = "";
                using (WebResponse responseComponent = request.GetResponse())
                {
                    using (Stream stream = responseComponent.GetResponseStream())
                    {
                        doc = XDocument.Load(stream);
                        result = doc.ToString();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(result);
                        XmlNamespaceManager nms = new XmlNamespaceManager(xmlDoc.NameTable);
                        nms.AddNamespace("env", "http://schemas.xmlsoap.org/soap/envelope/");
                        nms.AddNamespace("wsa", "http://www.w3.org/2005/08/addressing");
                        nms.AddNamespace("typ", "http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/types/");
                        nms.AddNamespace("ns1", "http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/");
                        XmlNodeList nodeList = xmlDoc.SelectNodes("//ns1:ItemCategory", nms);
                        foreach (XmlNode node in nodeList)
                        {
                            ComponentChild component = new ComponentChild();
                            if (node.HasChildNodes)
                            {
                                if (node.LocalName == "ItemCategory")
                                {
                                    XmlNodeList nodeListvalue = node.ChildNodes;
                                    foreach (XmlNode nodeValue in nodeListvalue)
                                    {
                                        if (nodeValue.LocalName == "CategoryName")
                                        {
                                            cats += nodeValue.InnerText + "+";
                                        }
                                    }
                                }
                            }

                        }
                        responseComponent.Close();
                    }
                }

                return cats;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                return "";
            }
        }


    }

    [AddIn("Invoice to Cost", Version = "1.0.0.0")]
    public class WorkspaceRibbonButtonFactory : IWorkspaceRibbonButtonFactory
    {
        IGlobalContext globalContext { get; set; }
        public IWorkspaceRibbonButton CreateControl(bool inDesignMode, IRecordContext RecordContext)
        {
            return new WorkspaceRibbonAddIn(inDesignMode, RecordContext, globalContext);
        }
        public System.Drawing.Image Image32
        {
            get { return Properties.Resources.money32; }
        }
        public System.Drawing.Image Image16
        {
            get { return Properties.Resources.money16; }
        }
        public string Text
        {
            get { return "Invoice to Cost"; }
        }
        public string Tooltip
        {
            get { return "Create Invoice"; }
        }
        public bool Initialize(IGlobalContext GlobalContext)
        {
            globalContext = GlobalContext;
            return true;
        }
    }
}

