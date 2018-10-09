using System;
using System.AddIn;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using CostToInvoiceButton.SOAPICCS;
using Newtonsoft.Json;
using RestSharp;
using RightNow.AddIns.AddInViews;
using RightNow.AddIns.Common;

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
                    string Seneam = "";
                    int AircraftId = 0;
                    string ICAO = "";
                    Incident = (IIncident)recordContext.GetWorkspaceRecord(WorkspaceRecordType.Incident);
                    IList<ICfVal> IncCustomFieldList = Incident.CustomField;
                    if (IncCustomFieldList != null)
                    {
                        foreach (ICfVal inccampos in IncCustomFieldList)
                        {
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
                                Seneam = inccampos.ValStr;
                            }
                            if (inccampos.CfId == 82)
                            {
                                CombustibleI = inccampos.ValStr;
                            }

                        }
                    }
                    IncidentID = Incident.ID;
                    IList<ICustomAttribute> customAttributes = Incident.CustomAttributes;
                    foreach (ICustomAttribute custom in customAttributes)
                    {
                        if (custom.GenericField.Name == "CO$Aircraft")
                        {
                            AircraftId = Convert.ToInt32(custom.GenericField.DataValue.Value);
                        }
                    }
                    ICAO = getTail(AircraftId);
                    string SRType = GetSRType();
                    GetDeleteComponents();
                    CreateChildComponents();
                    servicios = GetListServices();
                    doubleScreen = new DoubleScreen(global, recordContext);
                    DgvServicios = ((DataGridView)doubleScreen.Controls["dataGridServicios"]);
                    DgvServicios.DataSource = servicios;

                    DgvServicios.Columns[3].Visible = false;
                    DgvServicios.Columns[4].Visible = false;
                    DgvServicios.Columns[5].Visible = false;
                    DgvServicios.Columns[6].Visible = false;
                    DgvServicios.Columns[7].Visible = false;
                    DgvServicios.Columns[8].Visible = false;
                    DgvServicios.Columns[13].Visible = false;

                    DgvServicios.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    ((System.Windows.Forms.Label)doubleScreen.Controls["lblSrType"]).Text = SRType.ToUpper();
                    ((System.Windows.Forms.Label)doubleScreen.Controls["lblIdIncident"]).Text = IncidentID.ToString();
                    ((TextBox)doubleScreen.Controls["txtUtilidad"]).Text = Utilidad;
                    ((TextBox)doubleScreen.Controls["txtRoyalty"]).Text = Royalty;
                    ((TextBox)doubleScreen.Controls["txtCombustible"]).Text = Combustible;
                    ((TextBox)doubleScreen.Controls["txtCombustibleI"]).Text = CombustibleI;
                    ((TextBox)doubleScreen.Controls["txtICAOD"]).Text = ICAO;

                    doubleScreen.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en Click: " + ex.InnerException.ToString());
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
        public List<Services> GetListServices()
        {
            try
            {

                List<Services> services = new List<Services>();
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ID,ItemNumber,ItemDescription,Airport,IDProveedor,Costo,Precio,InternalInvoice,Itinerary,Paquete,Componente,Informativo,ParentPaxID,Categories FROM CO.Services WHERE Incident =" + IncidentID + " ORDER BY ID ASC, ParentPaxId ASC";
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
                        services.Add(service);
                    }
                }
                return services;
            }
            catch (Exception ex)
            {

                MessageBox.Show("Error en GetServices: " + ex.StackTrace);

                return null;
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
                            DeleteComponents(Convert.ToInt32(data));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void DeleteComponents(int id)
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
                MessageBox.Show(ex.InnerException.ToString());
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
                if (String.IsNullOrEmpty(component.Precio))
                {
                    body += "\"Precio\":null";
                }
                else
                {
                    body += "\"Precio\":\"" + component.Precio + "\"";
                }
                body += "}";
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
                MessageBox.Show("GetType: " + ex.InnerException.ToString());
                return "";
            }
        }
        public string getTail(int Tail)
        {
            string Icao = "";
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT LookupName,AircraftType1 FROM CO.Aircraft WHERE ID =" + Tail;
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    Char delimiter = '|';
                    String[] substrings = data.Split(delimiter);
                    Icao = getICAODesi(Convert.ToInt32(substrings[1]));
                }
            }
            return Icao;
        }
        public string getICAODesi(int IdAircraftType)
        {
            string Icao = "";
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT ICAODesignator FROM CO.AircraftType WHERE ID = " + IdAircraftType;
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
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

