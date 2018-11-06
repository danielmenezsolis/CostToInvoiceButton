using CostToInvoiceButton.SOAPICCS;
using MoreLinq;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using RightNow.AddIns.AddInViews;
using RightNow.AddIns.Common;
using System;
using System.AddIn;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace CostToInvoiceButton
{
    public class WorkspaceRibbonAddIn : Panel, IWorkspaceRibbonButton
    {
        DoubleScreen doubleScreen;
        public static List<G_1_INPC> INPC { get; set; }
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
        public string SRType { get; set; }


        public WorkspaceRibbonAddIn(bool inDesignMode, IRecordContext RecordContext, IGlobalContext globalContext)
        {
            if (inDesignMode == false)
            {
                global = globalContext;
                recordContext = RecordContext;
                this.inDesignMode = inDesignMode;
                RecordContext.Saving += new CancelEventHandler(RecordContext_Saving);
            }
        }

        private void RecordContext_Saving(object sender, CancelEventArgs e)
        {
            try
            {
                Init();
            }
            catch (Exception ex)
            {
                MessageBox.Show("RecordContext_Saving" + ex.Message + " Det :" + ex.StackTrace);
                throw ex;
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
                    string ClientType = "";
                    string ClientName = "";
                    string FuelType = "";
                    string CateringDeliveryDate = "";
                    string AircraftCategory = "";
                    string cClass = "";
                    Incident = (IIncident)recordContext.GetWorkspaceRecord(WorkspaceRecordType.Incident);
                    IList<ICfVal> IncCustomFieldList = Incident.CustomField;
                    DateTime? incidentCreation = Incident.Created;
                    if (IncCustomFieldList != null)
                    {
                        foreach (ICfVal inccampos in IncCustomFieldList)
                        {
                            if (inccampos.CfId == 37)
                            {
                                CateringDeliveryDate = inccampos.ValDttm.ToString();
                            }
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
                    cClass = getCustomerClass(IncidentID);
                    SRType = GetSRType();
                    AircraftCategory = GetCargoGroup(ICAO);
                    ClientType = GetClientType();
                    FuelType = GetFuelType(IncidentID);
                    GetDeleteMCreated();
                    //CreateChildComponents();
                    if (SRType != "FBO" || SRType != "FCC")
                    {
                        ArrivalAirportIncident = GetArrivalAirportIncident(IncidentID);
                        DepartureAirportIncident = GetDepartureAirportIncident(IncidentID);
                    }
                    if (SRType == "FUEL")
                    {
                        GetDeleteFuelItems();
                        GetFuelData(ClientType, FuelType, 0, "");
                        CreateFuelMinimun(ClientType, FuelType, 0, "");
                        if (SENEAM == "1")
                        {
                            CreateAirNavFee();
                            if (Utilidad != "A" && !String.IsNullOrEmpty(Utilidad))
                            {
                                CreateAirNavICCS();
                            }
                        }
                    }
                    if (SRType == "GYCUSTODIA")
                    {
                        CreateDeposit();
                    }
                    if (SRType == "SENEAM")
                    {
                        CreateOvers();
                        CreateSENEAMFee();
                    }
                    servicios = GetListServices();
                    if (SRType == "FBO")
                    {
                        var FBOServices = servicios.DistinctBy(b => b.Itinerary).ToList();
                        foreach (Services item in FBOServices)
                        {
                            ComponentChild component = new ComponentChild();
                            component.Airport = item.Airport.Replace("-", "_");

                            component.ItemNumber = "ASFIEAP357";
                            if (!String.IsNullOrEmpty(component.ItemNumber))
                            {
                                component = GetComponentData(component);
                                if (!String.IsNullOrEmpty(component.ItemDescription))
                                {
                                    component.Incident = IncidentID;
                                    component.Itinerary = Convert.ToInt32(item.Itinerary);
                                    component.Categories = GetCategories(component.ItemNumber, component.Airport);
                                    component.MCreated = "1";
                                    component.Componente = "0";
                                    component.ParentPaxId = IncidentID;
                                    InsertComponent(component);
                                }
                            }

                            if (ClientName.Contains("NETJETS"))
                            {
                                component.ItemNumber = "AIPRTFE0101";
                                if (!String.IsNullOrEmpty(component.ItemNumber))
                                {
                                    component = GetComponentData(component);
                                    if (!String.IsNullOrEmpty(component.ItemDescription))
                                    {
                                        component.Incident = IncidentID;
                                        component.Itinerary = Convert.ToInt32(item.Itinerary);
                                        component.Categories = GetCategories(component.ItemNumber, component.Airport);
                                        component.MCreated = "1";
                                        component.MCreated = "0";
                                        component.ParentPaxId = IncidentID;
                                        InsertComponent(component);
                                    }
                                }
                            }

                            if (!GetItineraryCountries(int.Parse(item.Itinerary)))
                            {
                                component.ItemNumber = "IISNNAP248";
                                if (!String.IsNullOrEmpty(component.ItemNumber))
                                {
                                    component = GetComponentData(component);
                                    if (!String.IsNullOrEmpty(component.ItemDescription))
                                    {
                                        component.Incident = IncidentID;
                                        component.Itinerary = Convert.ToInt32(item.Itinerary);
                                        component.Categories = GetCategories(component.ItemNumber, component.Airport);
                                        component.MCreated = "1";
                                        component.MCreated = "0";
                                        component.ParentPaxId = IncidentID;
                                        InsertComponent(component);
                                    }
                                }
                            }
                            GetDeleteFuelItems();
                            GetFuelData(ClientType, FuelType, int.Parse(item.Itinerary), item.Airport);
                            CreateFuelMinimun(ClientType, FuelType, int.Parse(item.Itinerary), item.Airport);
                            if (SENEAM == "1")
                            {
                                CreateAirNavFee();
                                if (Utilidad != "A" && !String.IsNullOrEmpty(Utilidad))
                                {
                                    CreateAirNavICCS();
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
                            if (ClientName.Contains("NETJETS"))
                            {
                                component.ItemNumber = "AIPRTFE0101";
                                if (!String.IsNullOrEmpty(component.ItemNumber))
                                {
                                    component = GetComponentData(component);
                                    if (!String.IsNullOrEmpty(component.ItemDescription))
                                    {
                                        component.Incident = IncidentID;
                                        component.Itinerary = Convert.ToInt32(item.Itinerary);
                                        component.Categories = GetCategories(component.ItemNumber, component.Airport);
                                        component.MCreated = "1";
                                        component.Componente = "0";
                                        component.ParentPaxId = IncidentID;
                                        InsertComponent(component);
                                    }
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
                                        component.MCreated = "1";
                                        component.Componente = "0";
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
                                        component.Componente = "0";
                                        component.MCreated = "1";
                                        component.ParentPaxId = IncidentID;
                                        InsertComponent(component);
                                    }
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
                                        component.MCreated = "1";
                                        component.Componente = "0";
                                        component.ParentPaxId = IncidentID;
                                        InsertComponent(component);
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
                    ((System.Windows.Forms.Label)doubleScreen.Controls["lblCurrencyPrice"]).Text = GetCurrency();
                    ((TextBox)doubleScreen.Controls["txtUtilidad"]).Text = Utilidad;
                    ((TextBox)doubleScreen.Controls["txtClientName"]).Text = ClientName;
                    ((TextBox)doubleScreen.Controls["txtRoyalty"]).Text = Royalty;
                    ((TextBox)doubleScreen.Controls["txtCombustible"]).Text = Combustible;
                    ((TextBox)doubleScreen.Controls["txtCombustibleI"]).Text = CombustibleI;
                    ((TextBox)doubleScreen.Controls["txtClientInfo"]).Text = ClientType;
                    ((TextBox)doubleScreen.Controls["txtICAOD"]).Text = ICAO;
                    ((TextBox)doubleScreen.Controls["txtCustomerClass"]).Text = cClass;
                    ((TextBox)doubleScreen.Controls["txtCargoGroup"]).Text = AircraftCategory;
                    ((TextBox)doubleScreen.Controls["txtCateringDDate"]).Text = CateringDeliveryDate;
                    ((TextBox)doubleScreen.Controls["txtArrivalIncident"]).Text = ArrivalAirportIncident;
                    ((TextBox)doubleScreen.Controls["txtDepartureIncident"]).Text = DepartureAirportIncident;

                    ((TextBox)doubleScreen.Controls["txtSemeam"]).Text = SeneamCat;

                    ((TextBox)doubleScreen.Controls["txtCreationIncidentDate"]).Text = incidentCreation.ToString();

                    ((ComboBox)doubleScreen.Controls["cboCurrency"]).Text = SRType == "FUEL" ? "MXN" : GetCurrency();


                    UpdatePackageCost();
                    doubleScreen.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en Click: " + ex.Message + "Det" + ex.StackTrace);
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
        public string getAirportById(int airportId)
        {
            string air = "";
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT LookupName FROM CO.Airports WHERE ID = " + airportId.ToString();
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    air = data.Replace("-", "_");
                }
            }
            MessageBox.Show("Airport: " + air);
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
            String queryString = "SELECT SUM(Liters) Suma,date_trunc(VoucherDateTime,'day'),ID FROM CO.Fueling WHERE Incident = " + IncidentID + " AND ArrivalAirport.AirportUse.Name = 'Federal'  GROUP BY date_trunc(VoucherDateTime,'day')";
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
                    component.FuelId = int.Parse(substrings[2]);
                    component.MCreated = "1";
                    component.Componente = "0";
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
            String queryString = "SELECT SUM(Liters) Suma,date_trunc(VoucherDateTime,'day'),Id FROM CO.Fueling WHERE Incident = " + IncidentID + " AND ArrivalAirport.AirportUse.Name = 'Federal'  GROUP BY date_trunc(VoucherDateTime,'day')";
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
                    component.FuelId = int.Parse(substrings[2]);
                    component.Componente = "0";
                    component.MCreated = "1";
                    component = GetComponentData(component);
                    component.Categories = GetCategories(component.ItemNumber, component.Airport);
                    if (!string.IsNullOrEmpty(component.ItemDescription))
                    {
                        InsertComponent(component);
                    }
                }
            }
        }
        public void CreateDeposit()
        {
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT COUNT(*) FROM CO.Services WHERE ItemNumber = 'DEPEGAR0358' AND Incident = " + IncidentID;
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    if (Convert.ToInt32(data) < 1)
                    {
                        Services service = new Services();
                        Char delimiter = '|';
                        string[] substrings = data.Split(delimiter);
                        ComponentChild component = new ComponentChild();
                        component.Airport = ArrivalAirportIncident;
                        component.ItemNumber = "DEPEGAR0358";
                        component.Incident = IncidentID;
                        component.ParentPaxId = IncidentID;
                        component.MCreated = "1";
                        component.Componente = "0";
                        component = GetComponentData(component);
                        component.Categories = GetCategories(component.ItemNumber, component.Airport);
                        if (!string.IsNullOrEmpty(component.ItemDescription))
                        {
                            InsertComponent(component);
                        }
                    }
                }
            }
        }
        public bool SENEAMNot()
        {
            try
            {
                bool not = false;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT CustomFields.c.seneamfeetype.name FROM Incident WHERE ID =" + IncidentID;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                if (queryCSV.CSVTables.Length > 0)
                {
                    foreach (CSVTable table in queryCSV.CSVTables)
                    {
                        String[] rowData = table.Rows;
                        foreach (String data in rowData)
                        {
                            not = data == "Notification" ? true : false;
                        }
                    }
                }
                return not;
            }
            catch (Exception ex)
            {
                MessageBox.Show("SENEAMNot:" + ex.Message + "Det: " + ex.StackTrace);
                return false;
            }
        }
        public string GetSeneamRequiredDate()
        {
            try
            {
                string required = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT CustomFields.c.period FROM Incident WHERE ID =" + IncidentID;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                if (queryCSV.CSVTables.Length > 0)
                {
                    foreach (CSVTable table in queryCSV.CSVTables)
                    {
                        String[] rowData = table.Rows;
                        foreach (String data in rowData)
                        {
                            required = data;
                        }
                    }
                }
                return required;
            }
            catch (Exception ex)
            {

                MessageBox.Show("SENEAMNot:" + ex.Message + "Det: " + ex.StackTrace);
                return "";
            }
        }
        public string GetSeneamPresDate()
        {
            try
            {
                string presentation = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT CustomFields.c.presentationdate FROM Incident WHERE ID =" + IncidentID;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                if (queryCSV.CSVTables.Length > 0)
                {
                    foreach (CSVTable table in queryCSV.CSVTables)
                    {
                        String[] rowData = table.Rows;
                        foreach (String data in rowData)
                        {
                            presentation = data;
                        }
                    }
                }
                return presentation;
            }
            catch (Exception ex)
            {

                MessageBox.Show("SENEAMNot:" + ex.Message + "Det: " + ex.StackTrace);
                return "";
            }
        }

        static IEnumerable<DateTime> monthsBetween(DateTime d0, DateTime d1)
        {
            return Enumerable.Range(0, (d1.Year - d0.Year) * 12 + (d1.Month - d0.Month + 1))
                             .Select(m => new DateTime(d0.Year, d0.Month, 1).AddMonths(m));
        }

        public void CreateOvers()
        {
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT Type,Cost,Time,Amount FROM CO.SENEAMOvers WHERE Incident = " + IncidentID;

            double factorA = 0;
            double tRec = 0;

            if (SENEAMNot())
            {
                //FECHAS
                string Required = GetSeneamRequiredDate();
                string Presentation = GetSeneamPresDate();

                //MessageBox.Show("Fecha Required: " + Required.ToString());
                //MessageBox.Show("Fecha Presentation: " + Presentation.ToString());

                //INPC'S
                getINPC(Required, Presentation);
                Dictionary<string, string> INPCSs = new Dictionary<string, string>();
                foreach (var item in INPC)
                {
                    INPCSs.Add(item.PERIOD_NAME, item.PRICE_INDEX_VALUE);
                    //MessageBox.Show("Periodo: " + item.PERIOD_NAME + "   Valor INPC: " + item.PRICE_INDEX_VALUE);
                }
                factorA = Convert.ToDouble(INPCSs.First().Value) / Convert.ToDouble(INPCSs.Last().Value);
                //MessageBox.Show("Factor A: " + factorA);

                //TASA DE RECARGO
                double sumaRec = 0;
                IEnumerable<DateTime> meses = monthsBetween(DateTime.Parse(Required), DateTime.Parse(Presentation));
                foreach (var mes in meses)
                {
                    sumaRec += GetTasaAnual(mes.Year.ToString());
                    //MessageBox.Show("sumaRec actual: " + sumaRec.ToString());
                }
                tRec = sumaRec;
                //MessageBox.Show("sumaRec total: " + sumaRec.ToString());
            }

            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 100, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    Char delimiter = '|';
                    string[] substrings = data.Split(delimiter);
                    string iNumber = "OSSEIAS0185";
                    if (substrings[0] == "2")
                    {
                        iNumber = "AFVLEAP257";
                    }
                    Services service = new Services();
                    ComponentChild component = new ComponentChild();
                    component.Airport = "MTS_ITEM";
                    component.ItemNumber = iNumber;
                    component.Incident = IncidentID;
                    component.ParentPaxId = IncidentID;
                    component.MCreated = "1";
                    component.Componente = "0";
                    component.Costo = substrings[1];
                    if (factorA > 0 || tRec > 0)
                    {
                        double pricef = Convert.ToDouble(substrings[3]);
                        pricef = (factorA * pricef);
                        pricef = pricef + ((pricef * tRec) / 100);
                        component.Precio = Math.Round(pricef,4).ToString();
                    }
                    else
                    {
                        component.Precio = substrings[3];
                    }
                    component = GetComponentData(component);
                    component.Categories = GetCategories(component.ItemNumber, component.Airport);
                    if (!string.IsNullOrEmpty(component.ItemDescription))
                    {
                        InsertComponent(component);
                    }
                }
            }
        }
        private double GetTasaAnual(string ano)
        {
            try
            {
                double tasa = 0;
                var client = new RestClient("https://iccs.bigmachines.com/");
                string User = Encoding.UTF8.GetString(Convert.FromBase64String("aW1wbGVtZW50YWRvcg=="));
                string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJneSoyMDE4"));
                client.Authenticator = new HttpBasicAuthenticator("servicios", "Sinergy*2018");
                string definicion = "?totalResults=true&q={inicio_tasa:'" + ano + "'}";
                var request = new RestRequest("rest/v6/customRecargos_Seneam/" + definicion, Method.GET);
                IRestResponse response = client.Execute(request);
                ClaseRecargos.RootObject rootObjectCat = JsonConvert.DeserializeObject<ClaseRecargos.RootObject>(response.Content);
                if (rootObjectCat.items.Count > 0)
                {
                    tasa = rootObjectCat.items[0].tasa_recargo;
                }
                else
                {
                    tasa = 0;
                }

                return tasa;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException.ToString());
                return 0;
            }
        }
        public void CreateSENEAMFee()
        {
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT ItemNumber, SUM(Precio) FROM CO.Services WHERE Incident = " + IncidentID + " AND (ItemNumber = 'OSSEIAS0185' OR ItemNumber = 'AFVLEAP257')";
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    Char delimiter = '|';
                    string[] substrings = data.Split(delimiter);
                    string iNumber = "SOMFEAP325";
                    if (substrings[0] == "AFVLEAP257")
                    {
                        iNumber = "SOMFEAP260";
                    }
                    Services service = new Services();
                    ComponentChild component = new ComponentChild();
                    component.Airport = "MTS_ITEM";
                    component.ItemNumber = iNumber;
                    component.Incident = IncidentID;
                    component.ParentPaxId = IncidentID;
                    component.MCreated = "1";
                    component.Componente = "0";
                    component.Costo = substrings[1];
                    component.Precio = substrings[1];
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
                String queryString = "SELECT ID,ItemNumber,ItemDescription,Airport,IDProveedor,Costo,Precio,InternalInvoice,Itinerary,Paquete,Componente,Informativo,ParentPaxID,Categories,fuel_id,CobroParticipacionNj,ParticipacionCobro FROM CO.Services WHERE Incident =" + IncidentID + " AND Informativo = '0' ORDER BY ID ASC, Itinerary ASC, ParentPaxId ASC";
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
                        service.CobroParticipacionNj = substrings[15];
                        service.ParticipacionCobro = substrings[16];
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
                String queryString = "SELECT ID FROM CO.Services WHERE fuel_id NOT IN (0)  AND Incident = " + IncidentID;
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
        public void GetDeleteMCreated()
        {
            try
            {

                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ID FROM CO.Services WHERE ManualCreated = '1' AND Incident = " + IncidentID;
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
                global.LogMessage(envelope);
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
                                            componentchild.ServiceParent = component.ID;
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
                                           "<typ1:operator>=</typ1:operator>";
                if (component.Airport == "MTS_ITEM")
                {
                    envelope += "<typ1:value>MTS_ITEM</typ1:value>";
                }
                else
                {
                    envelope += "<typ1:value>IO_AEREO_" + component.Airport + "</typ1:value>";
                }
                envelope += "</typ1:item>" +

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
                                                    }
                                                    if (nodeDeff.LocalName == "xxCobroParticipacionNj")
                                                    {
                                                        component.CobroParticipacionNj = nodeDeff.InnerText == "SI" ? "1" : "0";
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

                if (!String.IsNullOrEmpty(component.ServiceParent.ToString()) && component.ServiceParent > 0)
                {
                    body += "\"Services\":";
                    body += "{";
                    body += "\"id\":" + component.ServiceParent + "";
                    body += "},";
                }

                if (String.IsNullOrEmpty(component.CobroParticipacionNj))
                {
                    body += "\"CobroParticipacionNj\":null,";
                }
                else
                {
                    body += "\"CobroParticipacionNj\":\"" + component.CobroParticipacionNj + "\",";
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
                body += "\"ManualCreated\":\"" + component.MCreated + "\",";
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
                MessageBox.Show("Error en creación de child: " + ex.Message + "Det" + ex.StackTrace);
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
        public string getCustomerClass(int Incident)
        {
            string clase = "GENERALES";
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT CustomFields.c.clase FROM Incident WHERE ID =" + Incident;
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    clase = data;
                }
            }
            return clase;
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
        public void GetFuelData(string ClientType, string FuelType, int Itinerar, string Air)
        {
            string[] LookCountry = new string[2];

            if (SRType == "FUEL")
            {
                LookCountry = GetCountryLook();
            }
            else
            {
                ArrivalAirportIncident = Air;
                LookCountry = GetCountryLookItinerary(Itinerar);
            }
            string ItemN = "";

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
                            MCreated = "1",
                            Itinerary = Itinerar,
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
        public void CreateFuelMinimun(string ClientType, string FuelType, int Itinerar, string Air)
        {
            string[] LookCountry = new string[2];
            string ItemN = "AFMURAS0016";
            if (SRType == "FUEL")
            {
                LookCountry = GetCountryLook();
            }
            else
            {
                ArrivalAirportIncident = Air;
                LookCountry = GetCountryLookItinerary(Itinerar);
            }
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
            queryString = "SELECT SUM(Liters) Suma,date_trunc(VoucherDateTime,'day'),ID FROM CO.Fueling WHERE Incident = " + IncidentID + " AND ArrivalAirport.AirportUse.Name = 'Federal'  GROUP BY date_trunc(VoucherDateTime,'day')  HAVING SUM(Liters) < 1500";
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
                    component.Airport = ArrivalAirportIncident.Replace("-", "_");
                    component.ItemNumber = ItemN;
                    component.Incident = IncidentID;
                    component.ParentPaxId = IncidentID;
                    component.Itinerary = Itinerar;
                    component.FuelId = int.Parse(substrings[2]);
                    component.MCreated = "1";
                    component.Componente = "0";
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
        public string[] GetCountryLookItinerary(int itinerary)
        {
            try
            {
                string[] res = new string[2];
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ArrivalAirport.Country.LookupName,ToAirport.LookupName FROM CO.Itinerary WHERE ID =" + itinerary + "";
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
                                           "<typ1:operator>=</typ1:operator>";
                if (Airport == "MTS_ITEM")
                {
                    envelope += "<typ1:value>MTS_ITEM</typ1:value>";
                }
                else
                {
                    envelope += "<typ1:value>IO_AEREO_" + Airport + "</typ1:value>";
                }
                envelope += "</typ1:item>" +
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
        public string GetCargoGroup(string strIcao)
        {
            string cGroup = "";
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT CargoGroup.LookupName FROM CO.AircraftType WHERE ICAODesignator = '" + strIcao + "'";
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    cGroup = data;
                }
            }
            return cGroup;
        }
        public void UpdatePackageCost()
        {
            try
            {
                List<Services> services = new List<Services>();
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ID,Services FROM CO.Services  WHERE Incident =" + IncidentID + "  AND Paquete = '1' Order BY Services.CreatedTime ASC";
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
                        service.ParentPax = substrings[1];
                        services.Add(service);
                    }
                }
                if (services.Count > 0)
                {
                    foreach (Services item in services)
                    {
                        double price = 0;
                        double priceP = 0;
                        double PriceCh = 0;
                        if (!String.IsNullOrEmpty(item.ParentPax))
                        {
                            priceP = getPaxPrice(item.ParentPax);
                            PriceCh = getPaxPrice(item.ID);
                            price = PriceCh + priceP;
                            UpdatePaxPrice(item.ID, PriceCh);
                            UpdatePaxPrice(item.ParentPax, price);
                        }
                        else
                        {
                            price = getPaxPrice(item.ID);
                            UpdatePaxPrice(item.ID, price);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " Det: " + ex.StackTrace);
            }

        }
        public void UpdatePaxPrice(string id, double price)
        {
            try
            {
                var client = new RestClient("https://iccsmx.custhelp.com/");
                var request = new RestRequest("/services/rest/connect/v1.4/CO.Services/" + id + "", Method.POST)
                {
                    RequestFormat = DataFormat.Json
                };
                var body = "{";
                // Información de precios costos
                body +=
                    "\"Costo\":\"" + price + "\"";

                body += "}";
                global.LogMessage(body);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                // easily add HTTP Headers
                request.AddHeader("Authorization", "Basic ZW9saXZhczpTaW5lcmd5KjIwMTg=");
                request.AddHeader("X-HTTP-Method-Override", "PATCH");
                request.AddHeader("OSvC-CREST-Application-Context", "Update Service {id}");
                // execute the request
                IRestResponse response = client.Execute(request);
                var content = response.Content; // raw content as string
                if (content == "")
                {

                }
                else
                {
                    MessageBox.Show(response.Content);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " Det: " + ex.StackTrace);
            }

        }
        public double getPaxPrice(string PaxId)
        {
            double price = 0;
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT SUM(TicketAmount) FROM CO.Payables WHERE Services.Incident =" + IncidentID + "  AND Services.Services = " + PaxId + " GROUP BY Services.Services   ";
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    price = String.IsNullOrEmpty(data) ? 0 : Convert.ToDouble(data);
                }
            }
            return price;
        }

        static void getINPC(string fechaI, string fechaF)
        {
            try
            {
                string envelope = "<soap:Envelope " +
               "	xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\"" +
    "	xmlns:pub=\"http://xmlns.oracle.com/oxp/service/PublicReportService\">" +
      "<soap:Header/>" +
    "	<soap:Body>" +
    "		<pub:runReport>" +
    "			<pub:reportRequest>" +
    "			<pub:attributeFormat>xml</pub:attributeFormat>" +
    "				<pub:attributeLocale>en</pub:attributeLocale>" +
    "				<pub:attributeTemplate>default</pub:attributeTemplate>" +
                "<pub:parameterNameValues>" +
                    "<pub:item>" +
                        "<pub:name>P_PERIOD_START</pub:name>" +
                        "<pub:values>" +
                            "<pub:item>"+ fechaI +"</pub:item>" +
                        "</pub:values>" +
                    "</pub:item>" +
                    "<pub:item>" +
                        "<pub:name>P_PERIOD_END</pub:name>" +
                        "<pub:values>" +
                            "<pub:item>" + fechaF + "</pub:item>" +
                        "</pub:values>" +
                    "</pub:item>" +
                "</pub:parameterNameValues>" +
    "				<pub:reportAbsolutePath>Custom/Integracion/XX_ASSET_PRICE_INDEX_REP.xdo</pub:reportAbsolutePath>" +
    "				<pub:sizeOfDataChunkDownload>-1</pub:sizeOfDataChunkDownload>" +
    "			</pub:reportRequest>" +
    "		</pub:runReport>" +
    "	</soap:Body>" +
    "</soap:Envelope>";
                byte[] byteArray = Encoding.UTF8.GetBytes(envelope);
                // Construct the base 64 encoded string used as credentials for the service call
                byte[] toEncodeAsBytes = ASCIIEncoding.ASCII.GetBytes("itotal" + ":" + "Oracle123");
                string credentials = Convert.ToBase64String(toEncodeAsBytes);
                // Create HttpWebRequest connection to the service
                HttpWebRequest request =
                 (HttpWebRequest)WebRequest.Create("https://egqy-test.fa.us6.oraclecloud.com:443/xmlpserver/services/ExternalReportWSSService");
                // Configure the request content type to be xml, HTTP method to be POST, and set the content length
                request.Method = "POST";
                request.ContentType = "application/soap+xml; charset=UTF-8;action=\"\"";
                request.ContentLength = byteArray.Length;
                // Configure the request to use basic authentication, with base64 encoded user name and password, to invoke the service.
                request.Headers.Add("Authorization", "Basic " + credentials);
                // Set the SOAP action to be invoked; while the call works without this, the value is expected to be set based as per standards
                //request.Headers.Add("SOAPAction", "http://xmlns.oracle.com/apps/cdm/foundation/parties/organizationService/applicationModule/findOrganizationProfile");
                // Write the xml payload to the request
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                // Write the xml payload to the request
                XDocument doc;
                XmlDocument docu = new XmlDocument();
                string result;
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        doc = XDocument.Load(stream);
                        result = doc.ToString();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(result);
                        XmlNamespaceManager nms = new XmlNamespaceManager(xmlDoc.NameTable);
                        nms.AddNamespace("env", "http://schemas.xmlsoap.org/soap/envelope/");
                        nms.AddNamespace("ns2", "http://xmlns.oracle.com/oxp/service/PublicReportService");

                        XmlNode desiredNode = xmlDoc.SelectSingleNode("//ns2:runReportReturn", nms);
                        if (desiredNode.HasChildNodes)
                        {
                            for (int i = 0; i < desiredNode.ChildNodes.Count; i++)
                            {
                                if (desiredNode.ChildNodes[i].LocalName == "reportBytes")
                                {
                                    byte[] data = Convert.FromBase64String(desiredNode.ChildNodes[i].InnerText);
                                    string decodedString = Encoding.UTF8.GetString(data);
                                    XmlTextReader reader = new XmlTextReader(new System.IO.StringReader(decodedString));
                                    reader.Read();
                                    XmlSerializer serializer = new XmlSerializer(typeof(DATA_DS_INPC));
                                    DATA_DS_INPC res = (DATA_DS_INPC)serializer.Deserialize(reader);
                                    INPC = res.G_N_INPC.G_1_INPC;
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
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