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
                dataGridInvoice.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
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
            ClearTxtBoxes();
            dataGridSuppliers.DataSource = null;
            try
            {
                if (e.RowIndex != -1)
                {
                    txtIdService.Text = dataGridServicios.Rows[e.RowIndex].Cells[0].FormattedValue.ToString().Trim();
                    txtItemNumber.Text = dataGridServicios.Rows[e.RowIndex].Cells[1].FormattedValue.ToString().Trim();
                    txtItem.Text = dataGridServicios.Rows[e.RowIndex].Cells[2].FormattedValue.ToString().Trim();
                    string airtport = dataGridServicios.Rows[e.RowIndex].Cells[3].FormattedValue.ToString().Trim();
                    txtSupplierName.Text = dataGridServicios.Rows[e.RowIndex].Cells[4].FormattedValue.ToString().Trim();
                    txtInvoice.Text = dataGridServicios.Rows[e.RowIndex].Cells[7].FormattedValue.ToString().Trim();

                    GetItineraryHours(String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells[8].FormattedValue.ToString()) ? 0 : Convert.ToInt32(dataGridServicios.Rows[e.RowIndex].Cells[8].FormattedValue.ToString()));
                    txtCategorias.Text = dataGridServicios.Rows[e.RowIndex].Cells[13].FormattedValue.ToString().Trim();
                    txtAirport.Text = "IO_AEREO_" + airtport.Replace("-", "_").Trim();
                    txtMainHour.Text = GetMainHour();

                    if (String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells[5].FormattedValue.ToString()))
                    {
                        txtCost.Text = GetCosts().ToString();
                    }
                    else
                    {
                        txtCost.Text = dataGridServicios.Rows[e.RowIndex].Cells[5].FormattedValue.ToString();
                    }
                    if (String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells[6].FormattedValue.ToString()))
                    {
                        txtPrice.Text = GetPrices().ToString();
                    }
                    else
                    {
                        txtPrice.Text = dataGridServicios.Rows[e.RowIndex].Cells[6].FormattedValue.ToString();
                    }
                    getSuppliers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException.ToString());
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
                    txtSupplierName.Text = dataGridSuppliers.Rows[e.RowIndex].Cells[1].FormattedValue.ToString().Trim();
                    txtCost.Text = dataGridSuppliers.Rows[e.RowIndex].Cells[4].FormattedValue.ToString().Trim();
                    txtPrice.Text = dataGridSuppliers.Rows[e.RowIndex].Cells[4].FormattedValue.ToString().Trim();

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
                            double amount = (Convert.ToDouble(txtPrice.Text) * Convert.ToInt32(txtQty.Text));

                            dataGridInvoice.Rows.Add(txtInvoice.Text, txtItem.Text, cboSuppliers.Text, txtQty.Text, txtCost.Text, txtPrice.Text, amount, txtIdService.Text);
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
                global.LogMessage(ex.InnerException.ToString());
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
                MessageBox.Show(ex.Message);
            }
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
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
            this.Close();
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
            if (lblSrType.Text == "CATERING")
            {
                if (txtUtilidad.Text == "A")
                {
                    txtPrice.Text = GetPrices().ToString();
                }
                else
                {
                    if (IsFloatValue(txtCost.Text))
                    {
                        txtPrice.Text = (Convert.ToDouble(txtCost.Text) + (Convert.ToDouble(txtCost.Text) * ((GetCateringPercentage(txtUtilidad.Text) / 100)))).ToString();
                    }
                }
            }
            if (lblSrType.Text == "FBO")
            {
                if (IsFloatValue(txtCost.Text))
                {
                    txtPrice.Text = (Convert.ToDouble(txtCost.Text) * 1.30).ToString();
                }

            }
        }
        //Functions
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
        private void GetItineraryHours(int Itinerary)
        {
            try
            {
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ATA_ZUTC,ATD_ZUTC,ArrivalAirport,ToAirport,FromAirport FROM CO.Itinerary WHERE Incident1 =" + lblIdIncident.Text + " AND ID =" + Itinerary + "";
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
                        txtArrivalAiport.Text = getNIAirport(String.IsNullOrEmpty(substrings[2]) ? 0 : Convert.ToInt32(substrings[2]));
                        txtToAirtport.Text = getNIAirport(String.IsNullOrEmpty(substrings[3]) ? 0 : Convert.ToInt32(substrings[3]));
                        txtFromAirport.Text = getNIAirport(String.IsNullOrEmpty(substrings[4]) ? 0 : Convert.ToInt32(substrings[4]));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.InnerException.ToString());
            }
        }
        private void getArrivalHours(int Arrival, string Open, string Close)
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
                            hours.Type = "Extraordinary";
                            break;
                        case "2":
                            hours.Type = "Critical";
                            break
                                ;
                        default:
                            hours.Type = "Normal";
                            break;
                    }

                    WHoursList.Add(hours);
                }
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
                                        if (lista.G_1_ITEMSUP.Count > 0)
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
                MessageBox.Show(ex.Message);
            }
        }
        private bool ValidateData()
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
            if (!IsNumber(txtInvoice.Text))
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
        private bool ValidateRows()
        {
            bool res = true;
            foreach (DataGridViewRow dgvRenglon in dataGridInvoice.Rows)
            {

                if (txtItem.Text == dgvRenglon.Cells[1].Value.ToString())
                {
                    res = false;
                }
            }
            return res;
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
            txtSupplierName.Text = "";
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
            txtAmount.Text = (Convert.ToDouble(txtPrice.Text) * Convert.ToInt32(txtQty.Text)).ToString();
        }
        private double GetCosts()
        {
            try
            {
                double cost = 0;
                if (lblSrType.Text == "CATERING")
                {
                    cost = 0;
                }
                else
                {
                    var client = new RestClient("https://iccs.bigmachines.com/");
                    string User = Encoding.UTF8.GetString(Convert.FromBase64String("aW1wbGVtZW50YWRvcg=="));
                    string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJneSoyMDE4"));
                    client.Authenticator = new HttpBasicAuthenticator(User, Pass);
                    // string definicion = "?totalResults=false&q={str_item_number:'" + dataGridServicios.Rows[e.RowIndex].Cells[1].FormattedValue.ToString().Trim() + "',str_icao_iata_code:'" + airtport + "'}";
                    string definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',str_aircraft_type:'" + txtICAOD.Text + "'}";
                    var request = new RestRequest("rest/v6/customCostos/" + definicion, Method.GET);
                    IRestResponse response = client.Execute(request);
                    ClaseParaCostos.RootObject rootObjectCosts = JsonConvert.DeserializeObject<ClaseParaCostos.RootObject>(response.Content);
                    if (rootObjectCosts.items.Count > 0)
                    {
                        cost = rootObjectCosts.items[0].flo_cost;
                    }
                    else
                    {
                        cost = 0;
                    }
                }
                return cost;
            }
            catch (Exception ex)
            {
                global.LogMessage("GetCost:" + ex.Message);
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
                string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJneSoyMDE4"));
                client.Authenticator = new HttpBasicAuthenticator(User, Pass);

                //string definicion = "?totalResults=false&q={str_item_number:'" + dataGridServicios.Rows[e.RowIndex].Cells[1].FormattedValue.ToString().Trim() + "',str_icao_iata_code:'" + airtport + "'}";
                string definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "'}";
                var request = new RestRequest("rest/v6/customPrecios/" + definicion, Method.GET);
                IRestResponse response = client.Execute(request);
                ClaseParaPrecios.RootObject rootObjectPrices = JsonConvert.DeserializeObject<ClaseParaPrecios.RootObject>(response.Content);
                if (rootObjectPrices.items.Count > 0)
                {
                    price = rootObjectPrices.items[0].flo_amount;
                }
                else
                {
                    price = 0;
                }

                return price;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetPrice:" + ex.Message);
                return 0;
            }
        }
        private double GetCateringPercentage(string Utilidad)
        {
            try
            {
                double amount = 0;
                var client = new RestClient("https://iccs.bigmachines.com/");
                string User = Encoding.UTF8.GetString(Convert.FromBase64String("aW1wbGVtZW50YWRvcg=="));
                string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJneSoyMDE4"));
                client.Authenticator = new HttpBasicAuthenticator(User, Pass);

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
        private string getNIAirport(int Airport)
        {
            try
            {
                string Nac = "";

                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT Type.Name FROM CO.Airports WHERE ID =" + Airport + " ";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Nac = data;
                    }
                }
                return Nac;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return "";
            }
        }
        private string GetMainHour()
        {
            try
            {
                string hour = "";
                DateTime ArriveDate = DateTime.Parse(txtATA.Text);
                DateTime DeliverDate = DateTime.Parse(txtATD.Text);
                hour = "Extraordinary";
                if (WHoursList.Count > 0)
                {
                    foreach (WHours w in WHoursList)
                    {
                        double totalminutesOpen = (ArriveDate - w.Opens).TotalMinutes;
                        double totalminutesClose = (w.Closes - DeliverDate).TotalMinutes;
                        if (w.Type == "Normal")
                        {
                            if (totalminutesOpen > 0 && totalminutesClose > 0)
                            {
                                hour = w.Type;
                            }
                        }
                        if (w.Type == "Critical")
                        {
                            if (totalminutesOpen > 0 && totalminutesClose > 0)
                            {
                                hour = w.Type;
                            }
                        }
                    }
                }
                return hour;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return "";
            }
        }
    }

}
