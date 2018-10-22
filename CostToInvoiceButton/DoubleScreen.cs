using MoreLinq;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using RightNow.AddIns.AddInViews;
using CostToInvoiceButton.SOAPICCS;

namespace CostToInvoiceButton
{
    public partial class DoubleScreen : Form
    {

        private IRecordContext recordContext { get; set; }
        private IGlobalContext global { get; set; }
        public List<WHours> WHoursList { get; set; }
        private RightNowSyncPortClient clientORN { get; set; }

        public DoubleScreen(IGlobalContext globalContext, IRecordContext record)
        {
            try
            {
                recordContext = record;
                global = globalContext;
                InitializeComponent();
                Init();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException.ToString());
            }
        }

        //Controls Events
        private void dataGridServicios_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex != -1)
                {
                    lblQty.Text = "Qty";
                    txtPrice.Enabled = true;
                    txtCost.Enabled = true;
                    cboCurrency.Enabled = true;
                    ClearTxtBoxes();
                    dataGridSuppliers.DataSource = null;
                    txtIdService.Text = dataGridServicios.Rows[e.RowIndex].Cells[0].FormattedValue.ToString().Trim();
                    txtItinerary.Text = dataGridServicios.Rows[e.RowIndex].Cells[8].FormattedValue.ToString().Trim();
                    txtItemNumber.Text = dataGridServicios.Rows[e.RowIndex].Cells[1].FormattedValue.ToString().Trim();
                    txtItem.Text = dataGridServicios.Rows[e.RowIndex].Cells[2].FormattedValue.ToString().Trim();
                    txtRoyaltyItem.Text = dataGridServicios.Rows[e.RowIndex].Cells[15].FormattedValue.ToString().Trim();
                    string airtport = dataGridServicios.Rows[e.RowIndex].Cells[3].FormattedValue.ToString().Trim();
                    txtAirport.Text = "IO_AEREO_" + airtport.Replace("-", "_").Trim();
                    //txtClientName.Text = dataGridServicios.Rows[e.RowIndex].Cells[4].FormattedValue.ToString().Trim();
                    txtInvoice.Text = dataGridServicios.Rows[e.RowIndex].Cells[7].FormattedValue.ToString().Trim();
                    if (lblSrType.Text == "FBO" || lblSrType.Text == "FCC")
                    {
                        txtFBO.Text = GetFBOValue((String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells[8].FormattedValue.ToString()) ? 0 : Convert.ToInt32(dataGridServicios.Rows[e.RowIndex].Cells[8].FormattedValue.ToString())));
                        GetItineraryHours(String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells[8].FormattedValue.ToString()) ? 0 : Convert.ToInt32(dataGridServicios.Rows[e.RowIndex].Cells[8].FormattedValue.ToString()));
                        txtMainHour.Text = GetMainHour();
                    }
                    if (lblSrType.Text == "FUEL")
                    {
                        txtFuelDateCharge.Text = GetFuelDataCharge(String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells[14].FormattedValue.ToString()) ? 0 : Convert.ToInt32(dataGridServicios.Rows[e.RowIndex].Cells[14].FormattedValue.ToString()));
                        txtGalones.Text = GetGalones(String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells[14].FormattedValue.ToString()) ? 0 : Convert.ToInt32(dataGridServicios.Rows[e.RowIndex].Cells[14].FormattedValue.ToString()));
                    }
                    if (lblSrType.Text == "CATERING")
                    {
                        txtInvoice.Text = "1";
                    }
                    txtCategorias.Text = dataGridServicios.Rows[e.RowIndex].Cells[13].FormattedValue.ToString().Trim();
                    if (String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells[5].FormattedValue.ToString()))
                    {
                        txtCost.Text = GetCosts(out string Currency).ToString();
                        if (!String.IsNullOrEmpty(Currency))
                        {
                            cboCurrency.Text = Currency;
                        }
                    }
                    else
                    {
                        txtCost.Text = dataGridServicios.Rows[e.RowIndex].Cells[5].FormattedValue.ToString();
                    }

                    if (String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells[6].FormattedValue.ToString()))
                    {
                        if (lblSrType.Text != "FUEL")
                        {
                            txtPrice.Text = GetPrices().ToString();
                        }
                    }
                    /*
                    else
                    {
                        txtPrice.Text = dataGridServicios.Rows[e.RowIndex].Cells[6].FormattedValue.ToString();
                    }
                    */

                    if (lblSrType.Text == "FUEL")
                    {
                        if (txtItemNumber.Text == "AGASIAS0270" || txtItemNumber.Text == "JFUEIAS0269" || txtItemNumber.Text == "AGASIAS0011" || txtItemNumber.Text == "JFUEIAS0010")
                        {
                            double b;
                            if (double.TryParse(txtCost.Text, out b))
                            {
                                txtPrice.Text = GetFuelPrice();
                            }

                        }
                        else
                        {
                            txtPrice.Text = Math.Round(GetPrices(), 4).ToString();
                        }
                    }
                    if ((lblSrType.Text == "FBO" && txtItemNumber.Text == "ASFIEAP357") || (lblSrType.Text == "FCC" && txtItemNumber.Text == "AIPRTFE0101"))
                    {
                        //List<ItiPrices> itiPrices = new List<ItiPrices>();
                        //itiPrices = getInvoiceItineraries();
                        double pricesum = 0;
                        // foreach (var item in itiPrices)
                        //{
                        int arrival = GetArrivalAirport(Convert.ToInt32(txtItinerary.Text));
                        double catcollectionfee = Convert.ToDouble(getAirportCateringCollectionFee(arrival)) / 100;
                        double airportfee = Convert.ToDouble(getAirportCollectionFee(arrival)) / 100;
                        double deductionfee = Convert.ToDouble(getAirportCollectionDeductionFee(arrival)) / 100;
                        foreach (DataGridViewRow dgvRenglon in dataGridInvoice.Rows)
                        {
                            int itinerarycompare = Convert.ToInt32(dgvRenglon.Cells[9].Value);
                            double price = Convert.ToDouble(dgvRenglon.Cells[5].Value);

                            if (Convert.ToInt32(txtItinerary.Text) == itinerarycompare)
                            {
                                if (dgvRenglon.Cells[10].Value.ToString() == "1" && dgvRenglon.Cells[1].Value.ToString().Contains("CATERING"))
                                {
                                    pricesum = pricesum + (price + (price * (catcollectionfee)));

                                }
                                if (dgvRenglon.Cells[10].Value.ToString() == "1" && !dgvRenglon.Cells[1].Value.ToString().Contains("CATERING"))
                                {
                                    pricesum = pricesum + (price + (price * (airportfee)));
                                }
                            }
                        }
                        pricesum = pricesum - (pricesum * (deductionfee));
                        //}

                        txtPrice.Text = Math.Round((pricesum), 4).ToString();
                        txtPrice.Enabled = false;
                        txtCost.Enabled = false;

                    }
                    if (lblSrType.Text == "FCC")
                    {
                        if (txtItemNumber.Text == "ASECSAS0073")
                        {
                            lblQty.Text = "Periods";

                            double minutehour = GetMinutesLeg();
                            txtQty.Text = minutehour.ToString();
                            txtPrice.Text = Math.Round((GetPrices() * minutehour), 4).ToString();
                        }
                        if ((txtAirport.Text.Contains("MHLM") || txtAirport.Text.Contains("MGGT")) && GetCountItinerary() > 1 && txtClientName.Text.Contains("GULF AND CAR") && GetIncidentFlightType())
                        {
                            double p = GetPrices();
                            txtPrice.Text = Math.Round(p - (p * 0.025), 4).ToString();
                        }
                        else
                        {
                            txtPrice.Text = Math.Round(GetPrices(), 4).ToString();
                        }


                    }


                    getSuppliers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ServiceDobleClic: " + ex.Message + "Det:" + ex.StackTrace);
            }
        }
        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void dataGridSuppliers_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex != -1)
                {
                    //txtClientName.Text = dataGridSuppliers.Rows[e.RowIndex].Cells[1].FormattedValue.ToString().Trim();
                    txtCost.Text = dataGridSuppliers.Rows[e.RowIndex].Cells[4].FormattedValue.ToString().Trim();
                    txtPrice.Text = dataGridSuppliers.Rows[e.RowIndex].Cells[4].FormattedValue.ToString().Trim();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "Det:" + ex.StackTrace);
            }
        }
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                if (ValidateData())
                {
                    if (dataGridInvoice.RowCount <= dataGridServicios.RowCount - 1)
                    {
                        if (ValidateRows())
                        {
                            double amount = Math.Round((Convert.ToDouble(txtPrice.Text) * Convert.ToInt32(txtQty.Text)), 4);
                            dataGridInvoice.Rows.Add(txtInvoice.Text, txtItem.Text, cboSuppliers.Text, txtQty.Text, txtCost.Text, txtPrice.Text, amount, txtIdService.Text, cboCurrency.Text, txtItinerary.Text, txtRoyaltyItem.Text);
                            ClearTxtBoxes();
                        }
                        else
                        {
                            MessageBox.Show("Item has been already added");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Cannot add more suppliers than services");
                    }
                }
                else
                {
                    MessageBox.Show("All data must be filled correctly");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ButtonAddClic: " + ex.Message + "Det:" + ex.StackTrace);
            }
        }
        private void txtQty_TextChanged(object sender, EventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }
        private void txtPrice_TextChanged(object sender, EventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "Det:" + ex.StackTrace);
            }
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!validateFBOFee())
            {
                int i = 0;
                if (dataGridInvoice.Rows.Count == 0)
                {
                    MessageBox.Show("Cannot be saved if there is no data");
                }
                else
                {
                    foreach (DataGridViewRow dgvRenglon in dataGridInvoice.Rows)
                    {
                        var client = new RestClient("https://iccsmx.custhelp.com/");
                        var request = new RestRequest("/services/rest/connect/v1.4/CO.Services/" + dgvRenglon.Cells[7].Value.ToString() + "", Method.POST)
                        {
                            RequestFormat = DataFormat.Json
                        };
                        var body = "{";
                        // Información de precios costos
                        body += "\"Precio\":\"" + dgvRenglon.Cells[5].Value.ToString() + "\"," +
                            "\"Costo\":\"" + dgvRenglon.Cells[4].Value.ToString() + "\"," +
                            "\"InternalInvoice\":" + Convert.ToInt32(dgvRenglon.Cells[0].Value.ToString()) + "";
                        if (!String.IsNullOrEmpty(dgvRenglon.Cells[2].Value.ToString()))
                        {
                            body += ",\"IDProveedor\":\"" + dgvRenglon.Cells[2].Value.ToString() + "\"";
                        }
                        body += "}";
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
                            i = i + 1;
                        }
                        else
                        {
                            MessageBox.Show(response.Content);
                        }
                    }
                }
                if (i > 0)
                {
                    MessageBox.Show("Data saved");
                }
            }
            //this.Close();
        }
        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridInvoice.Rows.Count > 0)
            {
                int row = dataGridInvoice.CurrentCell.RowIndex;
                dataGridInvoice.Rows.RemoveAt(row);
            }
        }
        private void dataGridInvoice_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex != -1)
            {
                string action = dataGridInvoice.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                if (action == "Edit")
                {

                }
                if (action == "Delete")
                {
                    DialogResult dialogResult = MessageBox.Show("¿Want to erase row?", "Double Screen", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        dataGridInvoice.Rows.RemoveAt(e.RowIndex);
                        ClearTxtBoxes();
                    }
                }
            }
        }
        private void dataGridServicios_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
        private void txtCost_TextChanged(object sender, EventArgs e)
        {
            try
            {

                if (lblSrType.Text == "CATERING")
                {
                    if (txtUtilidad.Text == "A")
                    {
                        if (txtCost.Text != "0" && !String.IsNullOrEmpty(txtCost.Text))
                        {
                            txtPrice.Text = txtCost.Text;
                        }
                        //txtPrice.Text = GetPrices().ToString();
                    }
                    else
                    {
                        if (IsFloatValue(txtCost.Text))
                        {
                            txtPrice.Text = Math.Round((Convert.ToDouble(txtCost.Text) + (Convert.ToDouble(txtCost.Text) * ((GetUtilidadPercentage(txtUtilidad.Text) / 100)))), 4).ToString();
                        }
                    }
                }
                if (lblSrType.Text == "FBO")
                {

                    if (txtItemNumber.Text == "ASFIEAP357")
                    {
                        txtPrice.Text = GetFBOPrice().ToString();
                    }
                    if (IsFloatValue(txtCost.Text))
                    {
                        txtPrice.Text = Math.Round((Convert.ToDouble(txtCost.Text) * 1.30), 4).ToString();
                    }

                }
                if (lblSrType.Text == "FUEL")
                {
                    if (txtItemNumber.Text == "AGASIAS0270" || txtItemNumber.Text == "JFUEIAS0269" || txtItemNumber.Text == "AGASIAS0011" || txtItemNumber.Text == "JFUEIAS0010")
                    {
                        double b;
                        if (double.TryParse(txtCost.Text, out b))
                        {
                            txtPrice.Text = GetFuelPrice();
                        }

                    }
                }
                if (lblSrType.Text == "FCC")
                {
                    if (IsNumber(txtCost.Text) && GetPrices() == 0)
                    {
                        cboCurrency.Text = "USD";
                        DateTime date = DateTime.Parse(txtATA.Text);
                        txtPrice.Text = ((Convert.ToDouble(txtCost.Text) + (Convert.ToDouble(txtCost.Text) * GetUtilidadPercentage(txtUtilidad.Text) / 100)) / getExchangeRate(date)).ToString();
                    }

                    if (txtItemNumber.Text == "ASECSAS0073")
                    {
                        if (IsNumber(txtCost.Text))
                        {
                            double minutehour = GetMinutesLeg();
                            txtPrice.Text = Math.Round((Convert.ToDouble(txtCost.Text) * minutehour), 4).ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                global.LogMessage("Error en txtCost.Text:" + ex.Message + "Det:" + ex.StackTrace);
            }
        }
        //Functions

        public double GetMinutesLeg()
        {
            try
            {
                double minutes = 0;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT (Date_Diff(ATA_ZUTC,ATD_ZUTC)/60) FROM CO.Itinerary WHERE ID =" + txtItinerary.Text + "";
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

                if (txtClientName.Text.Contains("HEALTHCARE GLOBAL"))
                {
                    minutes = minutes - 120;
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

        public double GetFBOPrice()
        {
            double prices = 2;

            foreach (DataGridViewRow dgvRenglon in dataGridServicios.Rows)
            {


            }


            return prices;
        }
        public string GetFuelPrice()
        {
            try
            {
                double galonprice = Convert.ToDouble(txtCost.Text) * 3.7853;
                DateTime datecharge = DateTime.Parse(txtFuelDateCharge.Text);
                double rate = getExchangeRate(datecharge);
                double galonrate = galonprice / rate; // costo por galon
                double catcombus = GetCombCents(txtCombustible.Text);
                galonrate = (galonrate + catcombus);
                double IVA = (galonrate * .16);
                galonrate = galonrate + IVA;
                if (txtItemNumber.Text == "AGASIAS0270" || txtItemNumber.Text == "JFUEIAS0269")
                {
                    galonrate = galonrate - GetCombCentI(txtCombustibleI.Text);
                }
                galonrate = Math.Round(galonrate * Convert.ToDouble(txtGalones.Text), 4);

                return Math.Round((galonrate), 4).ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetFuelPrice: " + ex.Message + "Det:" + ex.StackTrace);
                return "";
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
                MessageBox.Show("Error en INIT: " + ex.Message + "Det:" + ex.StackTrace);
                return false;

            }
        }
        private string GetFBOValue(int Itinerary)
        {
            try
            {
                string FBO = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ArrivalAirport.FBO.Name FROM CO.Itinerary WHERE Incident1 =" + lblIdIncident.Text + " AND ID =" + Itinerary + "";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        FBO = data;
                    }
                }
                return FBO;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetFBOValue" + ex.Message + "DEtalle: " + ex.StackTrace);
                return "";
            }
        }

        private int GetCountItinerary()
        {
            try
            {
                int itineraries = 0;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT COUNT(ID) FROM CO.Itinerary WHERE Incident1 =" + lblIdIncident.Text + "";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        itineraries = String.IsNullOrEmpty(data) ? 0 : Convert.ToInt32(data);
                    }
                }
                return itineraries;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetFBOValue" + ex.Message + "DEtalle: " + ex.StackTrace);
                return 0;
            }
        }

        private bool GetIncidentFlightType()
        {
            try
            {
                bool cargo = true;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT CustomFields.c.flight_type.name FROM Incident  WHERE ID =" + lblIdIncident.Text + "";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        cargo = data == "CARGO" ? true : false;
                    }
                }
                return cargo;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetFBOValue" + ex.Message + "DEtalle: " + ex.StackTrace);
                return false;
            }
        }


        public bool validateFBOFee()
        {
            bool vali = true;
            List<ItiPrices> itiPrices = new List<ItiPrices>();
            itiPrices = getInvoiceItineraries();
            double pricecompare = 0;
            foreach (var item in itiPrices)
            {
                foreach (DataGridViewRow dgvRenglon in dataGridInvoice.Rows)
                {
                    int itinerarycompare = Convert.ToInt32(dgvRenglon.Cells[9].Value);
                    if (lblSrType.Text == "FBO" && dgvRenglon.Cells[1].Value.ToString().Contains("LOGISTIC / LOGISTICA") && item.Itinerarie == itinerarycompare)
                    {
                        pricecompare = +Convert.ToDouble(dgvRenglon.Cells[5].Value);
                    }
                }
                if (item.Limit < pricecompare)
                {
                    vali = false;
                    MessageBox.Show("The prices of Logistic Fee excedees Flight Logistic Limit in Itinerary:" + item.Itinerarie.ToString());
                }
            }
            return vali;
        }


        private void GetItineraryHours(int Itinerary)
        {
            try
            {
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ATA_ZUTC,ATD_ZUTC,ArrivalAirport,ArrivalAirport.Type.Name,ToAirport.Type.Name,FromAirport.Type.Name FROM CO.Itinerary WHERE Incident1 =" + lblIdIncident.Text + " AND ID =" + Itinerary + "";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Char delimiter = '|';
                        String[] substrings = data.Split(delimiter);
                        txtATA.Text = DateTimeOffset.Parse(substrings[0]).ToString();
                        txtATD.Text = DateTimeOffset.Parse(substrings[1]).ToString();
                        getArrivalHours(String.IsNullOrEmpty(substrings[2]) ? 0 : Convert.ToInt32(substrings[2]), substrings[0].Substring(0, 10), substrings[1].Substring(0, 10));
                        txtArrivalAiport.Text = substrings[3];
                        txtLimit.Text = getGrupoLogLimit(String.IsNullOrEmpty(substrings[2]) ? 0 : Convert.ToInt32(substrings[2]));

                        txtAirportFee.Text = getAirportCollectionFee(String.IsNullOrEmpty(substrings[2]) ? 0 : Convert.ToInt32(substrings[2]));
                        txtCateringCollection.Text = getAirportCateringCollectionFee(String.IsNullOrEmpty(substrings[2]) ? 0 : Convert.ToInt32(substrings[2]));
                        txtCollectionDeduction.Text = getAirportCollectionDeductionFee(String.IsNullOrEmpty(substrings[2]) ? 0 : Convert.ToInt32(substrings[2]));
                        txtToAirtport.Text = substrings[4];
                        txtFromAirport.Text = substrings[5];
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetItineraryHours" + ex.Message + "DEtalle: " + ex.StackTrace);
            }
        }
        public int GetArrivalAirport(int Itinerary)
        {
            try
            {
                int arrival = 0;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ArrivalAirport FROM CO.Itinerary WHERE Incident1 =" + lblIdIncident.Text + " AND ID =" + Itinerary + "";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        arrival = Convert.ToInt32(data);
                    }
                }
                return arrival;
            }
            catch (Exception ex)
            {
                MessageBox.Show("ArrivalAirport" + ex.Message + "DEtalle: " + ex.StackTrace);
                return 0;
            }

        }

        private void getArrivalHours(int Arrival, string Open, string Close)
        {
            try
            {
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT OpensZULUTime,ClosesZULUTime,Type FROM CO.Airport_WorkingHours WHERE Airports =" + Arrival + "";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                WHoursList = new List<WHours>();
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        WHours hours = new WHours();
                        Char delimiter = '|';
                        String[] substrings = data.Split(delimiter);
                        hours.Opens = DateTime.Parse(Open + " " + substrings[0].Trim());
                        hours.Closes = DateTime.Parse(Close + " " + substrings[1].Trim());
                        switch (substrings[02].Trim())
                        {
                            case "1":
                                hours.Type = "EXTRAORDINARIO";
                                break;
                            case "2":
                                hours.Type = "CRITICO";
                                break
                                    ;
                            case "25":
                                hours.Type = "NORMAL";
                                break;
                        }

                        WHoursList.Add(hours);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("getArrivalHours" + ex.Message + "DEtalle: " + ex.StackTrace);

            }
        }
        private void getSuppliers()
        {
            cboSuppliers.DataSource = null;
            cboSuppliers.Enabled = false;
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
    "				<pub:attributeLocale></pub:attributeLocale>" +
    "				<pub:attributeTemplate></pub:attributeTemplate>" +
    "				<pub:reportAbsolutePath>Custom/Integracion/XX_ITEM_SUPPLIER_ORG_REP.xdo</pub:reportAbsolutePath>" +
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

                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                // Write the xml payload to the request
                XDocument doc;
                XmlDocument docu = new XmlDocument();
                string result;
                Dictionary<string, string> test = new Dictionary<string, string>();
                List<Sup> sups = new List<Sup>();
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
                        if (desiredNode != null)
                        {
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
                                        XmlSerializer serializer = new XmlSerializer(typeof(DATA_DS_ITEMSUP));
                                        DATA_DS_ITEMSUP res = (DATA_DS_ITEMSUP)serializer.Deserialize(reader);
                                        var lista = res.G_N_ITEMSUP.Find(x => (x.ORGANIZATION_CODE.Trim() == txtAirport.Text));
                                        if (lista != null)
                                        {
                                            foreach (G_1_ITEMSUP item in lista.G_1_ITEMSUP)
                                            {
                                                if (item.ITEM_NUMBER == txtItemNumber.Text.Trim())
                                                {
                                                    Sup sup = new Sup();
                                                    sup.Id = item.VENDOR_ID;
                                                    sup.Name = item.PARTY_NAME;
                                                    sups.Add(sup);
                                                }

                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                test = sups.DistinctBy(y => y.Id).ToDictionary(k => k.Id, k => k.Name);
                test.Add("0", "NO SUPPLIER");
                cboSuppliers.DataSource = test.ToArray();
                cboSuppliers.DisplayMember = "Value";
                cboSuppliers.ValueMember = "Key";
                cboSuppliers.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetSupliers" + ex.Message + "DEtalle: " + ex.StackTrace);
            }
        }
        private double getExchangeRate(DateTime date)
        {
            try
            {
                double rate = 1;
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
                   "<pub:name>P_EXCHANGE_DATE</pub:name>" +
                   "<pub:values>" +
                      "<pub:item>" + date.ToString("yyyy-MM-dd") + "</pub:item>" +
                   "</pub:values>" +
                "</pub:item>" +
                 "</pub:parameterNameValues>" +

     "				<pub:reportAbsolutePath>Custom/Integracion/XX_DAILY_RATES_REP.xdo</pub:reportAbsolutePath>" +
     "				<pub:sizeOfDataChunkDownload>-1</pub:sizeOfDataChunkDownload>" +
     "			</pub:reportRequest>" +
     "		</pub:runReport>" +
     "	</soap:Body>" +
     "</soap:Envelope>";
                global.LogMessage(envelope);
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
                                    XmlSerializer serializer = new XmlSerializer(typeof(DATA_DS_RATES));
                                    DATA_DS_RATES res = (DATA_DS_RATES)serializer.Deserialize(reader);
                                    if (res.G_N_RATES != null)
                                    {
                                        rate = Convert.ToDouble(res.G_N_RATES.G_1_RATES.CONVERSION_RATE);
                                    }
                                }
                            }
                        }
                    }
                }

                return rate;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                return 1;
            }
        }
        private bool ValidateData()
        {
            try
            {
                bool res = true;

                if (!IsNumber(txtQty.Text) || txtQty.Text == "0")
                {
                    res = false;
                }
                if (!IsFloatValue(txtPrice.Text))
                {
                    res = false;
                }
                if (!IsNumber(txtInvoice.Text) || string.IsNullOrEmpty(txtInvoice.Text))
                {
                    res = false;
                }
                else
                {
                    if (Convert.ToInt32(txtInvoice.Text) >= 10 || Convert.ToInt32(txtInvoice.Text) == 0)
                    {
                        res = false;
                    }
                }
                if (!IsFloatValue(txtCost.Text))
                {
                    res = false;
                }

                return res;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                return true;
            }
        }
        private bool ValidateRows()
        {
            try
            {
                bool res = true;
                foreach (DataGridViewRow dgvRenglon in dataGridInvoice.Rows)
                {
                    if (txtIdService.Text == dgvRenglon.Cells[7].Value.ToString())
                    {
                        res = false;
                    }
                }
                return res;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                return false;
            }
        }
        public List<ItiPrices> getInvoiceItineraries()
        {
            List<ItiPrices> itineraries = new List<ItiPrices>();
            foreach (DataGridViewRow dgvRenglon in dataGridInvoice.Rows)
            {
                ItiPrices itiPrices = new ItiPrices();
                itiPrices.Itinerarie = String.IsNullOrEmpty(dgvRenglon.Cells[9].Value.ToString()) ? 0 : Convert.ToInt32(dgvRenglon.Cells[9].Value);
                itiPrices.Limit = getGrupoLogLimitItinerary(itiPrices.Itinerarie);
                itineraries.Add(itiPrices);
            }
            return itineraries.DistinctBy(x => x.Itinerarie).ToList();
        }

        private void ClearTxtBoxes()
        {
            txtAmount.Text = "0";
            txtCost.Text = "";
            txtIdService.Text = "";
            txtInvoice.Text = "";
            txtItem.Text = "";
            txtItemNumber.Text = "";
            txtPrice.Text = "";
            txtQty.Text = "1";

        }
        public bool IsFloatValue(string text)
        {
            Regex regex = new Regex(@"^\d*\.?\d{1,2}$");
            return regex.IsMatch(text);
        }
        public bool IsNumber(string text)
        {
            try
            {
                return text.All(char.IsDigit);
            }
            catch
            {
                return false;
            }
        }
        private void SetTotal()
        {
            txtAmount.Text = Math.Round(Convert.ToDouble(txtPrice.Text) * Convert.ToInt32(txtQty.Text), 4).ToString();
        }
        private double GetCosts(out string Currency)
        {
            try
            {
                string Curr = "";
                double cost = 0;

                if (lblSrType.Text == "CATERING")
                {
                    if (txtUtilidad.Text == "A")
                    {
                        cost = GetTicketSumCatA();
                    }
                }
                else
                {
                    string definicion = "";
                    var client = new RestClient("https://iccs.bigmachines.com/");
                    string User = Encoding.UTF8.GetString(Convert.FromBase64String("aW1wbGVtZW50YWRvcg=="));
                    string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJneTIwMTgu"));
                    client.Authenticator = new HttpBasicAuthenticator("servicios", "Sinergy*2018");
                    // string definicion = "?totalResults=false&q={str_item_number:'" + dataGridServicios.Rows[e.RowIndex].Cells[1].FormattedValue.ToString().Trim() + "',str_icao_iata_code:'" + airtport + "'}";
                    if (lblSrType.Text == "FBO")
                    {
                        definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',bol_int_fbo:1,str_schedule_type:'" + txtMainHour.Text + "',str_aircraft_type:'" + txtICAOD.Text + "',str_client_category:'" + txtUtilidad.Text + "'} ";
                        if (txtCategorias.Text.Contains("AERO"))
                        {
                            definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',bol_int_fbo:1,str_schedule_type:'" + txtMainHour.Text + "',str_aircraft_type:'" + txtICAOD.Text + "',str_ft_arrival: '" + txtFromAirport.Text.ToUpper() + "', str_ft_depart: '" + txtToAirtport.Text.ToUpper() + "' ,str_client_category:'" + txtUtilidad.Text + "'} ";
                        }
                    }
                    if (lblSrType.Text == "FCC")
                    {
                        definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',bol_intat:1,str_schedule_type:'" + txtMainHour.Text + "',str_aircraft_type:'" + txtICAOD.Text + "',str_client_category:'" + txtUtilidad.Text + "'} ";
                        if (txtCategorias.Text.Contains("AERO"))
                        {
                            definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',bol_intat:1,str_schedule_type:'" + txtMainHour.Text + "',str_aircraft_type:'" + txtICAOD.Text + "',str_ft_arrival: '" + txtFromAirport.Text.ToUpper() + "', str_ft_depart: '" + txtToAirtport.Text.ToUpper() + "' ,str_client_category:'" + txtUtilidad.Text + "'} ";
                        }
                        if (txtItemNumber.Text == "ASECSAS0073" || txtItemNumber.Text == "IPFERPS0052")// || txtItemNumber.Text == "JFUEIAS0269" || txtItemNumber.Text == "AGASIAS0011" || txtItemNumber.Text == "JFUEIAS0010" || txtItemNumber.Text == "AFMURAS0016")
                        {
                            //definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',str_aircraft_type:'" + txtICAOD.Text + "'}";
                            definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "'}";
                        }
                    }

                    if (lblSrType.Text == "FUEL")
                    {
                        /*
                        if (txtItemNumber.Text == "AFMURAS0016")
                        {
                            definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "'}";
                        }*/
                        if (txtItemNumber.Text == "ANFERAS0013" || txtItemNumber.Text == "ANIASAS0015" || txtItemNumber.Text == "AGASIAS0270" || txtItemNumber.Text == "JFUEIAS0269" || txtItemNumber.Text == "AGASIAS0011" || txtItemNumber.Text == "JFUEIAS0010" || txtItemNumber.Text == "AFMURAS0016")
                        {
                            //definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',str_aircraft_type:'" + txtICAOD.Text + "'}";
                            definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',str_schedule_type:'NORMAL'}";

                            if (txtItemNumber.Text == "AGASIAS0270")
                            {
                                definicion = "?totalResults=true&q={str_item_number:'AGASIAS0011',str_icao_iata_code:'" + txtAirport.Text + "',str_schedule_type:'NORMAL'}";
                            }
                            if (txtItemNumber.Text == "JFUEIAS0269")
                            {
                                definicion = "?totalResults=true&q={str_item_number:'JFUEIAS0010',str_icao_iata_code:'" + txtAirport.Text + "',str_schedule_type:'NORMAL'}";
                            }
                            if (txtItemNumber.Text == "ANFERAS0013")
                            {
                                definicion = "?totalResults=true&q={$or:[{str_icao_iata_code:{$exists:false}},{str_icao_iata_code:'" + txtAirport.Text + "'}],str_item_number:'ANFERAS0013',str_aircraft_type:'" + txtICAOD.Text + "'}";
                            }
                        }
                    }
                    global.LogMessage("GETCostDef:" + definicion + "SRType:" + lblSrType.Text);
                    var request = new RestRequest("rest/v6/customCostos/" + definicion, Method.GET);
                    IRestResponse response = client.Execute(request);
                    ClaseParaCostos.RootObject rootObjectCosts = JsonConvert.DeserializeObject<ClaseParaCostos.RootObject>(response.Content);
                    if (rootObjectCosts != null && rootObjectCosts.items.Count > 0)
                    {
                        if (lblSrType.Text == "FUEL")
                        {
                            foreach (ClaseParaCostos.Item item in rootObjectCosts.items)
                            {
                                DateTime inicio = DateTime.Parse(item.str_start_date);
                                DateTime fin = DateTime.Parse(item.str_end_date);
                                DateTime fecha = DateTime.Parse(txtFuelDateCharge.Text);
                                if (fecha.CompareTo(inicio) >= 0 && fecha.CompareTo(fin) <= 0)
                                {
                                    cost = item.flo_cost;
                                    Curr = item.str_currency_code;
                                }
                            }
                        }
                        else
                        {
                            cost = rootObjectCosts.items[0].flo_cost;
                            Curr = rootObjectCosts.items[0].str_currency_code;
                        }
                    }
                    else
                    {
                        cost = 0;
                    }
                }
                Currency = Curr;
                return cost;
            }
            catch (Exception ex)
            {
                global.LogMessage("GetCost:" + ex.Message + "Det:" + ex.StackTrace);
                Currency = "";
                return 0;
            }
        }
        private double GetPrices()
        {
            double price = 0;
            try
            {
                var client = new RestClient("https://iccs.bigmachines.com/");
                string User = Encoding.UTF8.GetString(Convert.FromBase64String("aW1wbGVtZW50YWRvcg=="));
                string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJneTIwMTgu"));
                client.Authenticator = new HttpBasicAuthenticator("servicios", "Sinergy*2018");
                string definicion = "";
                //string definicion = "?totalResults=false&q={str_item_number:'" + dataGridServicios.Rows[e.RowIndex].Cells[1].FormattedValue.ToString().Trim() + "',str_icao_iata_code:'" + airtport + "'}";
                // string definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "'}";
                if (lblSrType.Text == "CATERING")
                {
                    if (txtUtilidad.Text == "A")
                    {
                        definicion = "?totalResults=false&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "'}";
                    }
                    else
                    {
                        price = 0;
                    }
                }
                if (lblSrType.Text == "FBO")
                {
                    definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',bol_int_fbo:1,str_schedule_type:'" + txtMainHour.Text + "',str_aircraft_type:'" + txtICAOD.Text + "',str_client_category:'" + txtUtilidad.Text + "'} ";
                    if (txtItemNumber.Text == "DISONAP249")
                    {
                        definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',bol_int_fbo:1,str_schedule_type:'" + txtMainHour.Text + "',str_aircraft_type:'" + txtICAOD.Text + "',str_client_category:'" + txtUtilidad.Text + "'} ";
                    }
                }

                if (lblSrType.Text == "FUEL")
                {
                    if (txtItemNumber.Text == "IAFMUAS0271")
                    {
                        if (txtClientName.Text.Contains("NETJETS"))
                        {
                            definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',str_client_category:'NetJets'}";
                        }
                        definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "'}";
                    }
                    if (txtItemNumber.Text == "ANFERAS0013")
                    {
                        definicion = "?totalResults=true&q={$or:[{str_icao_iata_code:{$exists:false}},{str_icao_iata_code:'" + txtAirport.Text + "'}],str_item_number:'ANFERAS0013',str_aircraft_type:'" + txtICAOD.Text + "'}&orderby=str_icao_iata_code:asc";
                    }
                    if (txtItemNumber.Text == "AFMURAS0016")
                    {
                        if (txtClientName.Text.Contains("NETJETS"))
                        {
                            definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_client_category:'NetJets'}";
                        }
                        definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "'}";
                    }
                    if (txtItemNumber.Text == "ANIASAS0015")
                    {
                        definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_aircraft_type:'" + txtICAOD.Text + "'}";
                    }
                }
                if (lblSrType.Text == "FCC")
                {
                    definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',bol_int_at:1,bol_int_flight_cargo:1,str_schedule_type:'" + txtMainHour.Text + "',str_aircraft_type:'" + txtICAOD.Text + "',str_client_category:'" + txtUtilidad.Text + "'} ";
                    if (txtCategorias.Text.Contains("AERO"))
                    {
                        definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',bol_int_at:1,bol_int_flight_cargo:1,str_schedule_type:'" + txtMainHour.Text + "',str_aircraft_type:'" + txtICAOD.Text + "',str_ft_arrival: '" + txtFromAirport.Text.ToUpper() + "', str_ft_depart: '" + txtToAirtport.Text.ToUpper() + "' ,str_client_category:'" + txtUtilidad.Text + "'} ";
                    }
                    if (txtItemNumber.Text == "ASECSAS0073")
                    {
                        definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',str_client_category:'ASI_SECURITY'} ";
                    }
                    if (txtItemNumber.Text == "IPFERPS0052")
                    {
                        //definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',str_aircraft_type:'" + txtICAOD.Text + "'}";
                        definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "'}";
                    }
                    if (txtItemNumber.Text == "OHANIAS0129")
                    {
                        definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "'}";
                    }

                }
                global.LogMessage("GETPricesdef:" + definicion + "SRType:" + lblSrType.Text);
                var request = new RestRequest("rest/v6/customPrecios/" + definicion, Method.GET);
                IRestResponse response = client.Execute(request);
                ClaseParaPrecios.RootObject rootObjectPrices = JsonConvert.DeserializeObject<ClaseParaPrecios.RootObject>(response.Content);
                if (rootObjectPrices != null && rootObjectPrices.items.Count > 0)
                {
                    if (lblSrType.Text == "FUEL")
                    {
                        foreach (ClaseParaPrecios.Item item in rootObjectPrices.items)
                    {
                        //HolaPaps
                            DateTime inicio = DateTime.Parse(item.str_start_date);
                            DateTime fin = DateTime.Parse(item.str_end_date);
                            DateTime fecha = DateTime.Parse(txtFuelDateCharge.Text);

                            if (fecha.CompareTo(inicio) >= 0 && fecha.CompareTo(fin) <= 0)
                            {
                                price = item.flo_amount;
                            }
                        }
                    } else {
                        price = rootObjectPrices.items[0].flo_amount;
                    }
                }
                else
                {
                    price = 0;
                }
                return price;
            }
            catch (Exception ex)
            {
                global.LogMessage("GetPrices: " + ex.Message + "Detalle: " + ex.StackTrace);

                return 0;
            }
        }
        private double GetUtilidadPercentage(string Utilidad)
        {
            try
            {
                double amount = 0;
                var client = new RestClient("https://iccs.bigmachines.com/");
                string User = Encoding.UTF8.GetString(Convert.FromBase64String("aW1wbGVtZW50YWRvcg=="));
                string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJneSoyMDE4"));
                client.Authenticator = new HttpBasicAuthenticator("servicios", "Sinergy*2018");

                //string definicion = "?totalResults=false&q={str_item_number:'" + dataGridServicios.Rows[e.RowIndex].Cells[1].FormattedValue.ToString().Trim() + "',str_icao_iata_code:'" + airtport + "'}";
                string definicion = "?q={str_tipo:'UTILIDAD',str_categoria:'" + Utilidad + "'} ";
                var request = new RestRequest("rest/v6/customCategorias/" + definicion, Method.GET);
                IRestResponse response = client.Execute(request);
                ClaseParaCategorias.RootObject rootObjectCat = JsonConvert.DeserializeObject<ClaseParaCategorias.RootObject>(response.Content);
                if (rootObjectCat.items.Count > 0)
                {
                    amount = rootObjectCat.items[0].flo_value;
                }
                else
                {
                    amount = 0;
                }

                return amount;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException.ToString());
                return 0;

            }


        }
        private double GetCombCents(string Combustible)
        {
            try
            {
                double amount = 0;
                var client = new RestClient("https://iccs.bigmachines.com/");
                string User = Encoding.UTF8.GetString(Convert.FromBase64String("aW1wbGVtZW50YWRvcg=="));
                string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJneSoyMDE4"));
                client.Authenticator = new HttpBasicAuthenticator("servicios", "Sinergy*2018");

                //string definicion = "?totalResults=false&q={str_item_number:'" + dataGridServicios.Rows[e.RowIndex].Cells[1].FormattedValue.ToString().Trim() + "',str_icao_iata_code:'" + airtport + "'}";
                string definicion = "?q={str_tipo:'FUEL',str_categoria:'" + Combustible + "'} ";
                var request = new RestRequest("rest/v6/customCategorias/" + definicion, Method.GET);
                IRestResponse response = client.Execute(request);
                ClaseParaCategorias.RootObject rootObjectCat = JsonConvert.DeserializeObject<ClaseParaCategorias.RootObject>(response.Content);
                if (rootObjectCat.items.Count > 0)
                {
                    amount = rootObjectCat.items[0].flo_value;
                }
                else
                {
                    amount = 0;
                }

                return amount;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException.ToString());
                return 0;

            }
        }
        private double GetCombCentI(string Combustible)
        {
            try
            {
                double amount = 0;
                var client = new RestClient("https://iccs.bigmachines.com/");
                string User = Encoding.UTF8.GetString(Convert.FromBase64String("aW1wbGVtZW50YWRvcg=="));
                string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJneSoyMDE4"));
                client.Authenticator = new HttpBasicAuthenticator("servicios", "Sinergy*2018");

                //string definicion = "?totalResults=false&q={str_item_number:'" + dataGridServicios.Rows[e.RowIndex].Cells[1].FormattedValue.ToString().Trim() + "',str_icao_iata_code:'" + airtport + "'}";
                string definicion = "?q={str_tipo:'FUEL_I',str_categoria:'" + Combustible + "'} ";
                var request = new RestRequest("rest/v6/customCategorias/" + definicion, Method.GET);
                IRestResponse response = client.Execute(request);
                ClaseParaCategorias.RootObject rootObjectCat = JsonConvert.DeserializeObject<ClaseParaCategorias.RootObject>(response.Content);
                if (rootObjectCat.items.Count > 0)
                {
                    amount = rootObjectCat.items[0].flo_value;
                }
                else
                {
                    amount = 0;
                }

                return amount;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException.ToString());
                return 0;

            }



        }
        private string GetFuelDataCharge(int idFueling)
        {
            try
            {
                string Fueling = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT VoucherDateTime FROM CO.Fueling WHERE ID =" + idFueling + " ";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Fueling = data;
                    }
                }
                return String.IsNullOrEmpty(Fueling) ? "" : DateTime.Parse(Fueling).ToString();
            }

            catch (Exception ex)
            {
                MessageBox.Show("GetFuelDataCharge" + ex.Message + "Det:" + ex.StackTrace);
                return null;
            }
        }
        private double GetTicketSumCatA()
        {
            try
            {
                double sum = 0;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT SUM(TicketAmount) FROM Co.Payables WHERE Services.ID =" + txtIdService.Text + " ";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        sum = String.IsNullOrEmpty(data) ? 0 : Convert.ToDouble(data);
                    }
                }
                return Math.Round(sum, 4);
            }

            catch (Exception ex)
            {
                MessageBox.Show("GetTicketSumCatA" + ex.Message + "Det:" + ex.StackTrace);
                return 0;
            }
        }
        private string GetGalones(int idFueling)
        {
            try
            {
                string Fueling = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT Liters FROM CO.Fueling WHERE ID =" + idFueling + " ";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Fueling = Math.Round((Convert.ToDouble(data) / 3.7853), 4).ToString();
                    }
                }
                return Fueling;
            }

            catch (Exception ex)
            {
                MessageBox.Show("GetGalones" + ex.Message + "Det:" + ex.StackTrace);
                return null;
            }
        }
        private double getGrupoLogLimitItinerary(int Itinerary)
        {
            try
            {
                double limit = 0;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ArrivalAirport.Flightloglimit FROM Co.Itinerary WHERE ID =" + Itinerary + " ";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        limit = String.IsNullOrEmpty(data) ? 0 : Convert.ToDouble(data);
                    }
                }
                return limit;
            }
            catch (Exception ex)
            {
                MessageBox.Show("getGrupoLogLimitItinerary" + ex.Message + "Det:" + ex.StackTrace);
                return 0;
            }
        }
        private string getGrupoLogLimit(int Airport)
        {
            try
            {
                string limit = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT flightloglimit FROM CO.Airports WHERE ID =" + Airport + " ";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        limit = data;
                    }
                }
                return limit;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "Det:" + ex.StackTrace);
                return null;
            }
        }
        private string getAirportCollectionFee(int Airport)
        {
            try
            {
                double Fee = 1;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                DateTime ata = DateTime.Parse(txtATA.Text);
                String queryString = "SELECT CollectionFee  FROM CO.AirportFee WHERE Airports = " + Airport + " AND ClientCategory.Name = '" + txtUtilidad.Text + "' AND DueDate > '" + ata.ToString("yyyy-MM-dd") + "'";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Fee = String.IsNullOrEmpty(data) ? 1 : Convert.ToDouble(data);
                    }
                }
                return Fee.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("getAirportCollectionFee: " + ex.Message + "Det:" + ex.StackTrace);
                return null;
            }
        }
        private string getAirportCateringCollectionFee(int Airport)
        {
            try
            {
                double CateringFee = 0;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                DateTime ata = DateTime.Parse(txtATA.Text);
                String queryString = "SELECT CateringCollectionFee  FROM CO.AirportFee WHERE Airports = " + Airport + " AND ClientCategory.Name = '" + txtUtilidad.Text + "' AND DueDate > '" + ata.ToString("yyyy-MM-dd") + "'";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        CateringFee = String.IsNullOrEmpty(data) ? 0 : Convert.ToDouble(data);
                    }
                }
                return CateringFee.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("getAirportCateringCollectionFee: " + ex.Message + "Det:" + ex.StackTrace);
                return null;
            }
        }
        private string getAirportCollectionDeductionFee(int Airport)
        {
            try
            {
                double DeductionFee = 0;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                DateTime ata = DateTime.Parse(txtATA.Text);
                String queryString = "SELECT CollectionDeduction FROM CO.AirportFee WHERE Airports = " + Airport + " AND ClientCategory.Name = '" + txtUtilidad.Text + "' AND DueDate > '" + ata.ToString("yyyy-MM-dd") + "'";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        DeductionFee = String.IsNullOrEmpty(data) ? 0 : Convert.ToDouble(data);
                    }
                }
                return DeductionFee.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("getAirportCollectionDeductionFee: " + ex.Message + "Det:" + ex.StackTrace);
                return null;
            }
        }

        private string GetMainHour()
        {
            try
            {
                DateTime ArriveDate = DateTime.Parse(txtATA.Text);
                DateTime DeliverDate = DateTime.Parse(txtATD.Text);
                string hour = "EXTRAORDINARIO";
                if (WHoursList.Count > 0)
                {
                    foreach (WHours w in WHoursList)
                    {
                        double totalminutesOpen = (ArriveDate - w.Opens).TotalMinutes;
                        double totalminutesClose = (w.Closes - DeliverDate).TotalMinutes;
                        if (ArriveDate.CompareTo(w.Opens) >= 0 && ArriveDate.CompareTo(w.Closes) <= 0 && w.Type == "NORMAL" &&
                                                    DeliverDate.CompareTo(w.Opens) >= 0 && DeliverDate.CompareTo(w.Closes) <= 0)
                        {
                            hour = "NORMAL";
                        }
                        else if (ArriveDate.CompareTo(w.Opens) >= 0 && ArriveDate.CompareTo(w.Closes) <= 0 && w.Type == "CRITICO" &&
                            DeliverDate.CompareTo(w.Opens) >= 0 && DeliverDate.CompareTo(w.Closes) <= 0)
                        {
                            hour = "CRITICO";
                        }
                    }
                }
                return hour;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "Det:" + ex.StackTrace);
                return "";
            }
        }
        private bool FboProductExist()
        {
            bool res = true;
            foreach (DataGridViewRow dgvRenglon in dataGridServicios.Rows)
            {
                if (txtGpoAero.Text == dgvRenglon.Cells[1].Value.ToString())
                {
                    res = false;
                }

            }
            return res;

        }
        public ComponentChild GetSData(ComponentChild component)
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
                                          "<typ1:value>" + component.Airport + "</typ1:value>" +
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
                                                    if (nodeDeff.LocalName == "xxCategoriaRoyalty")
                                                    {
                                                        component.CategoriaRoyalty = nodeDeff.InnerText;
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
                MessageBox.Show(ex.Message + "Det:" + ex.StackTrace);
                return null;
            }
        }
        public void CreateFBOProduct(ComponentChild component)
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
                if (String.IsNullOrEmpty(component.CategoriaRoyalty))
                {
                    body += "\"CategoriaRoyalty\":null,";
                }
                else
                {
                    body += "\"CategoriaRoyalty\":\"" + component.CategoriaRoyalty + "\",";
                }
                if (String.IsNullOrEmpty(component.ClasificacionPagos))
                {
                    body += "\"ClasificacionPagos\":null,";
                }
                else
                {
                    body += "\"ClasificacionPagos\":\"" + component.ClasificacionPagos + "\",";
                }
                if (String.IsNullOrEmpty(component.Componente))
                {
                    body += "\"Componente\":null,";
                }
                else
                {
                    body += "\"Componente\":\"" + component.Componente + "\",";
                }
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
                if (String.IsNullOrEmpty(component.Precio))
                {
                    body += "\"Precio\":null";
                }
                else
                {
                    body += "\"Precio\":\"" + component.Precio + "\"";
                }
                body += "}";

                /*
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                request.AddHeader("Authorization", "Basic ZW9saXZhczpTaW5lcmd5KjIwMTg=");
                request.AddHeader("X-HTTP-Method-Override", "POST");
                request.AddHeader("OSvC-CREST-Application-Context", "Create Service");
                IRestResponse response = client.Execute(request);
                var content = response.Content;
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    RootObject rootObject = JsonConvert.DeserializeObject<RootObject>(response.Content);
                }
                else
                {
                    MessageBox.Show(content);
                }
                */
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en creación de child: " + ex.Message + "Det:" + ex.StackTrace);
            }
        }

        private void cboCurrency_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (!String.IsNullOrEmpty(txtItemNumber.Text))
                {
                    applyExchangeRate(cboCurrency.Text);
                }
            }
            catch (Exception ex)
            {
                global.LogMessage("Error en txtCost.Text:" + ex.Message + "Det:" + ex.StackTrace);
            }
        }

        private void applyExchangeRate(String moneda)
        {
            double rate = 1;
            DateTime dateEx = DateTime.Today;

            if (lblSrType.Text == "FUEL")
            {
                dateEx = DateTime.Parse(txtFuelDateCharge.Text);
            }
            else if (lblSrType.Text == "FBO" || lblSrType.Text == "FCC")
            {
                dateEx = DateTime.Parse(GetItineraryArrivalDate(Convert.ToInt32(txtItinerary.Text)).ToString());
            }
            else
            {
                dateEx = DateTime.Parse(GetIncidentCreationDate(Convert.ToInt32(txtIdService.Text)).ToString());
            }

            rate = getExchangeRate(dateEx);
            rate = 18.78;

            if (moneda == "MXN")
            {
                txtCost.Text = Math.Round((Convert.ToDouble(txtCost.Text) * rate), 4).ToString();
                txtPrice.Text = Math.Round((Convert.ToDouble(txtPrice.Text) * rate), 4).ToString();
            }
            else if (moneda == "USD")
            {
                txtCost.Text = Math.Round((Convert.ToDouble(txtCost.Text) / rate), 4).ToString();
                txtPrice.Text = Math.Round((Convert.ToDouble(txtPrice.Text) / rate), 4).ToString();
            }

        }

        private string GetItineraryArrivalDate(int idItinerary)
        {
            try
            {
                string date = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT date_trunc(ATA_ZUTC,'day') FROM Co.Itinerary WHERE ID =" + idItinerary + " ";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        date = data;
                    }
                }
                return String.IsNullOrEmpty(date) ? "" : DateTime.Parse(date).ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("getFechaItinerario" + ex.Message + "Det:" + ex.StackTrace);
                return "";
            }
        }

        private string GetIncidentCreationDate(int idServicio)
        {
            try
            {
                string date = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT Incident.CreatedTime FROM Co.Services WHERE ID =" + idServicio + " ";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        date = data;
                    }
                }
                return String.IsNullOrEmpty(date) ? "" : DateTime.Parse(date).ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("getfechaIncidente" + ex.Message + "Det:" + ex.StackTrace);
                return "";
            }
        }
    }
    public class ItiPrices
    {
        public int Itinerarie { get; set; }
        public double Limit { get; set; }
    }
}
