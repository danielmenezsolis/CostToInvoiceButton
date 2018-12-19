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
        public int HourId;
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
        private void dataGridServicios_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                txtUOM.Text = "";
                if (e.RowIndex != -1)
                {
                    lblQty.Text = "Quantity";
                    txtExchangeRate.Hide();
                    lblExchangeRate.Hide();
                    lblGalons.Hide();
                    lblTotalCostFuel.Hide();
                    txtGalones.Hide();
                    txtTotalCostFuel.Hide();
                    txtPrice.Enabled = true;
                    txtCost.Enabled = true;
                    cboCurrency.Enabled = true;
                    ClearTxtBoxes();
                    dataGridSuppliers.DataSource = null;
                    txtIdService.Text = dataGridServicios.Rows[e.RowIndex].Cells["ID"].Value.ToString();
                    txtItinerary.Text = dataGridServicios.Rows[e.RowIndex].Cells["Itinerary"].Value.ToString();
                    txtPackage.Text = dataGridServicios.Rows[e.RowIndex].Cells["Pax"].Value.ToString();
                    txtItemNumber.Text = dataGridServicios.Rows[e.RowIndex].Cells["ItemNumber"].Value.ToString();
                    txtItem.Text = dataGridServicios.Rows[e.RowIndex].Cells["Description"].Value.ToString();
                    txtCobroParticipacionNj.Text = dataGridServicios.Rows[e.RowIndex].Cells["CobroParticipacionNj"].Value.ToString();
                    txtParticipacionCobro.Text = dataGridServicios.Rows[e.RowIndex].Cells["ParticipacionCobro"].Value.ToString();
                    string airtport = dataGridServicios.Rows[e.RowIndex].Cells["Airport"].Value.ToString();
                    /*
                    txtIdService.Text = dataGridServicios.Rows[e.RowIndex].Cells[0].FormattedValue.ToString().Trim();
                    txtItinerary.Text = dataGridServicios.Rows[e.RowIndex].Cells[8].FormattedValue.ToString().Trim();
                    txtPackage.Text = dataGridServicios.Rows[e.RowIndex].Cells[9].FormattedValue.ToString().Trim();
                    txtItemNumber.Text = dataGridServicios.Rows[e.RowIndex].Cells[1].FormattedValue.ToString().Trim();
                    txtItem.Text = dataGridServicios.Rows[e.RowIndex].Cells[2].FormattedValue.ToString().Trim();
                    txtCobroParticipacionNj.Text = dataGridServicios.Rows[e.RowIndex].Cells[15].FormattedValue.ToString().Trim();
                    txtParticipacionCobro.Text = dataGridServicios.Rows[e.RowIndex].Cells[16].FormattedValue.ToString().Trim();
                    string airtport = dataGridServicios.Rows[e.RowIndex].Cells[3].FormattedValue.ToString().Trim();
                    */
                    txtAirport.Text = "IO_AEREO_" + airtport.Replace("-", "_").Trim();
                    /*
                    if (lblSrType.Text == "PERMISOS")
                    {

                    }
                    if (lblSrType.Text == "GYCUSTODIA")
                    {
                    
                    }
                    */
                    if (lblSrType.Text == "SENEAM")
                    {
                        cboCurrency.Text = "USD";
                        double tipoCambio = getExchangeRateSemanal(DateTime.Today);

                        MessageBox.Show("Tipo de cambio: $" + tipoCambio.ToString());

                        /*
                        double cost = String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells[5].Value.ToString()) ? 0 : Convert.ToDouble(dataGridServicios.Rows[e.RowIndex].Cells[5].FormattedValue);
                        if (cost >= tipoCambio)
                        {
                            cost = Math.Round(cost / tipoCambio, 4);
                        }
                        txtCost.Text = cost.ToString();
                        */
                        txtCost.Text = "0";

                        double pri = String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells["Price"].Value.ToString()) ? 0 : Convert.ToDouble(dataGridServicios.Rows[e.RowIndex].Cells["Price"].FormattedValue);
                        if (pri >= tipoCambio)
                        {
                            pri = Math.Round(pri / tipoCambio, 0, MidpointRounding.AwayFromZero);
                        }
                        txtPrice.Text = pri.ToString();

                        /*
                        int qty = Convert.ToInt32(pri / cost);
                        txtQty.Text = qty.ToString();
                        */

                        if (txtItemNumber.Text == "SOMFEAP325" || txtItemNumber.Text == "SOMFEAP260")
                        {
                            double costo = String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells["Price"].Value.ToString()) ? 0 : Convert.ToDouble(dataGridServicios.Rows[e.RowIndex].Cells["Price"].FormattedValue);
                            double precio = costo;
                            txtCost.Text = "0";

                            if (precio >= tipoCambio)
                            {
                                precio = precio / tipoCambio;
                            }
                            precio = Math.Round(precio, 0, MidpointRounding.AwayFromZero);
                            txtPrice.Text = precio.ToString();
                        }
                    }
                    if (lblSrType.Text == "FBO" || lblSrType.Text == "FCC")
                    {
                        txtFuelDateCharge.Text = GetFuelDataCharge(String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells["FuelId"].FormattedValue.ToString()) ? 0 : Convert.ToInt32(dataGridServicios.Rows[e.RowIndex].Cells["FuelId"].FormattedValue.ToString()));
                        txtGalones.Text = GetGalones(String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells["FuelId"].FormattedValue.ToString()) ? 0 : Convert.ToInt32(dataGridServicios.Rows[e.RowIndex].Cells["FuelId"].FormattedValue.ToString()));
                        txtFBO.Text = GetFBOValue((string.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells["Itinerary"].FormattedValue.ToString()) ? 0 : Convert.ToInt32(dataGridServicios.Rows[e.RowIndex].Cells["Itinerary"].FormattedValue.ToString())));
                        GetItineraryHours(string.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells["Itinerary"].FormattedValue.ToString()) ? 0 : Convert.ToInt32(dataGridServicios.Rows[e.RowIndex].Cells["Itinerary"].FormattedValue.ToString()));
                        txtMainHour.Text = GetMainHourFBOFCC(txtATA.Text, txtATD.Text);
                        if (lblSrType.Text == "FCC")
                        {
                            lblExchangeRate.Show();
                            txtExchangeRate.Show();
                            txtExchangeRate.Text = getExchangeRateSemanal(DateTime.Parse(txtATA.Text)).ToString();
                        }
                    }
                    if (lblSrType.Text == "FUEL")
                    {
                        lblExchangeRate.Show();
                        txtExchangeRate.Show();
                        lblGalons.Show();
                        lblTotalCostFuel.Show();
                        txtGalones.Show();
                        txtTotalCostFuel.Show();
                        txtFuelDateCharge.Text = GetFuelDataCharge(String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells["FuelId"].FormattedValue.ToString()) ? 0 : Convert.ToInt32(dataGridServicios.Rows[e.RowIndex].Cells["FuelId"].FormattedValue.ToString()));
                        txtGalones.Text = GetGalones(String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells["FuelId"].FormattedValue.ToString()) ? 0 : Convert.ToInt32(dataGridServicios.Rows[e.RowIndex].Cells["FuelId"].FormattedValue.ToString()));
                        txtExchangeRate.Text = getExchangeRateSemanal(DateTime.Parse(txtFuelDateCharge.Text)).ToString();
                        int Arrival = GetArrivalFuelAirport();
                        DateTime fuel = DateTime.Parse(txtFuelDateCharge.Text);
                        getArrivalHours(Arrival, fuel.ToString("yyyy-MM-dd"), fuel.ToString("yyyy-MM-dd"));
                        txtMainHour.Text = GetMainHourFBOFCC(txtFuelDateCharge.Text, txtFuelDateCharge.Text);
                    }
                    if (lblSrType.Text == "CATERING")
                    {

                    }
                    txtCategorias.Text = dataGridServicios.Rows[e.RowIndex].Cells["Categorias"].FormattedValue.ToString().Trim();
                    if (String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells["Cost"].FormattedValue.ToString()))
                    {
                        txtCost.Text = GetCosts(out string Currency).ToString();
                        if (!String.IsNullOrEmpty(Currency))
                        {
                            cboCurrency.Text = Currency;
                        }
                        if (txtItemNumber.Text == "SOMFEAP325" || txtItemNumber.Text == "SOMFEAP260")
                        {
                            txtCost.Text = "0";
                        }
                    }
                    else
                    {
                        txtCost.Text = dataGridServicios.Rows[e.RowIndex].Cells["Cost"].FormattedValue.ToString();
                    }
                    if (String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells["Price"].FormattedValue.ToString()))
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
                                txtPrice.Text = GetFuelPrice().ToString();
                            }
                        }
                        else
                        {
                            txtPrice.Text = Math.Round(GetPrices(), 4).ToString();
                        }
                    }
                    if (lblSrType.Text == "FBO" || lblSrType.Text == "FCC")
                    {
                        if (txtItemNumber.Text == "AGASIAS0270" || txtItemNumber.Text == "JFUEIAS0269" || txtItemNumber.Text == "AGASIAS0011" || txtItemNumber.Text == "JFUEIAS0010")
                        {
                            double b;
                            if (double.TryParse(txtCost.Text, out b))
                            {
                                txtPrice.Text = GetFuelPrice().ToString();
                            }
                        }
                    }
                    if ((lblSrType.Text == "FBO" && (txtItemNumber.Text == "ASFIEAP357" || txtItemNumber.Text == "AIPRTFE0101")) || (lblSrType.Text == "FCC" && txtItemNumber.Text == "AIPRTFE0101"))
                    {
                        int indice = 10;
                        if (txtItemNumber.Text == "AIPRTFE0101")
                        {
                            indice = 9;
                        }
                        double pricesum = 0;
                        int arrival = GetArrivalAirport(Convert.ToInt32(txtItinerary.Text));
                        //MessageBox.Show("ID de aeropuerto: " + arrival.ToString());
                        double catcollectionfee = Convert.ToDouble(getAirportCateringCollectionFee(arrival)) / 100;
                        //MessageBox.Show("ID de CatCollFee: " + catcollectionfee.ToString());
                        double airportfee = Convert.ToDouble(getAirportCollectionFee(arrival)) / 100;
                        //MessageBox.Show("ID de AirCollFee: " + airportfee.ToString());
                        double deductionfee = Convert.ToDouble(getAirportCollectionDeductionFee(arrival)) / 100;
                        //MessageBox.Show("ID de DedCollFee: " + deductionfee.ToString());
                        foreach (DataGridViewRow dgvRenglon in dataGridInvoice.Rows)
                        {
                            int itinerarycompare = Convert.ToInt32(dgvRenglon.Cells[8].Value);
                            double price = Convert.ToDouble(dgvRenglon.Cells[5].Value);
                            double fee = 0;
                            double dfee = 0;

                            if (Convert.ToInt32(txtItinerary.Text) == itinerarycompare)
                            {
                                if (dgvRenglon.Cells[indice].Value.ToString() == "1" && dgvRenglon.Cells[0].Value.ToString().Contains("CATERING"))
                                {
                                    fee = price * catcollectionfee;
                                    //MessageBox.Show("Item de Catering");
                                }
                                if (dgvRenglon.Cells[indice].Value.ToString() == "1" && !dgvRenglon.Cells[0].Value.ToString().Contains("CATERING"))
                                {
                                    fee = price * airportfee;
                                    //MessageBox.Show("Item normal");
                                }
                                //MessageBox.Show("ItemFee: " + fee.ToString());
                                dfee = fee * deductionfee;
                                //MessageBox.Show("ItemDedFee: " + dfee.ToString());
                                pricesum = pricesum + (fee - dfee);
                            }
                            //MessageBox.Show("AFeeActual: " + pricesum.ToString());
                        }
                        //MessageBox.Show("AFeeTotal: " + pricesum.ToString());
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
                        if ((txtAirport.Text.Contains("MHLM") || txtAirport.Text.Contains("MGGT")) && GetCountItinerary() > 1 && txtClientName.Text.Contains("GULF AND CAR") && isBHInside())
                        {
                            double p = GetPrices();
                            txtPrice.Text = Math.Round(p - (p * 0.025), 4).ToString();
                        }
                        else
                        {
                            txtPrice.Text = dataGridServicios.Rows[e.RowIndex].Cells["Price"].FormattedValue.ToString();
                            if (String.IsNullOrEmpty(dataGridServicios.Rows[e.RowIndex].Cells["Price"].FormattedValue.ToString()))
                            {
                                txtPrice.Text = Math.Round(GetPrices(), 4).ToString();
                            }
                        }
                    }
                    if (txtUOM.Text == "TW")
                    {
                        double b;
                        if (double.TryParse(txtCost.Text, out b))
                        {
                            txtPrice.Text = GetMTOWPrice();
                        }
                    }
                    if (txtUOM.Text == "HHR")
                    {
                        double b;
                        if (double.TryParse(txtCost.Text, out b))
                        {
                            double hhr = GetMinutesLeg() * 2;
                            txtQty.Text = hhr.ToString();
                            // MessageBox.Show("Costo por periodo: " + txtCost.Text);
                            // MessageBox.Show("Periodos: " + hhr.ToString());
                            txtCost.Text = (Convert.ToDouble(txtCost.Text) * hhr).ToString();
                            // MessageBox.Show("Costo total: " + txtCost.Text);
                        }
                        // MessageBox.Show("¿Es componente?");
                        if (isComponent())
                        {
                            // MessageBox.Show("Es componente");
                            txtPrice.Text = "0";
                        }
                    }
                    if (txtUOM.Text == "HR")
                    {
                        double b;
                        if (double.TryParse(txtCost.Text, out b))
                        {
                            double hr = GetMinutesLeg();
                            txtQty.Text = hr.ToString();
                            txtCost.Text = (Convert.ToDouble(txtCost.Text) * hr).ToString();
                        }
                    }
                    if (txtItemNumber.Text == "ASECSAS0073")
                    {
                        double b;
                        if (double.TryParse(txtCost.Text, out b))
                        {
                            double hr = GetMinutesLeg();
                            MessageBox.Show("Horas en AIRCRAFT SECURITY: " + hr.ToString());
                            double hXper = 24;
                            if (txtCustomerClass.Text == "ASI_SECURITY")
                            {
                                hXper = 12;
                            }
                            double per = Math.Ceiling(hr / hXper);
                            MessageBox.Show("Periodos en AIRCRAFT SECURITY: " + per.ToString());
                            txtQty.Text = per.ToString();
                            txtCost.Text = txtPrice.Text;
                            txtPrice.Text = (Convert.ToDouble(txtCost.Text) * per).ToString();
                        }
                    }
                    if (txtItemNumber.Text == "MHSPSAS0091")
                    {
                        double b;
                        if (double.TryParse(txtCost.Text, out b))
                        {
                            double m2 = Convert.ToDouble(getm2(txtICAOD.Text));
                            //MessageBox.Show("M2: " + m2.ToString());
                            double utilidad = gethSGroup(txtICAOD.Text);
                            utilidad = 1 + (utilidad / 100);
                            utilidad = (Convert.ToDouble(txtCost.Text) * m2) * utilidad;
                            txtPrice.Text = Math.Round(utilidad, 4).ToString();
                        }
                    }
                    if (txtItemNumber.Text == "DEPEGAR0358")
                    {
                        double price = 0;
                        if (dataGridInvoice.Rows.Count == 0)
                        {
                            MessageBox.Show("Please add the primary item first.");
                        }
                        else
                        {
                            foreach (DataGridViewRow dgvRenglon in dataGridInvoice.Rows)
                            {
                                if (dgvRenglon.Cells[0].Value.ToString().Contains("USO DE INSTALA"))
                                {
                                    price += Convert.ToDouble(dgvRenglon.Cells[5].Value.ToString());
                                }
                            }
                        }
                        txtPrice.Text = Math.Round(price, 4).ToString();
                    }

                    if (lblSrType.Text == "SENEAM")
                    {
                        cboCurrency.Text = "USD";
                    }
                    if (txtCost.Text == "0" && (lblSrType.Text != "SENEAM" || lblSrType.Text != "CATERING"))
                    {
                        cboCurrency.Text = "MXN";
                    }
                    getSuppliers();
                    Cursor.Current = Cursors.Default;
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
                            double amount = Math.Round((Convert.ToDouble(txtPrice.Text) * Convert.ToDouble(txtQty.Text)), 4);
                            if (txtItemNumber.Text == "LANDSAF0008")
                            {
                                amount = Math.Round(Convert.ToDouble(txtPrice.Text));
                            }
                            dataGridInvoice.Rows.Add(txtItem.Text, cboSuppliers.Text, txtQty.Text, txtCost.Text, txtPrice.Text, amount, txtIdService.Text, cboCurrency.Text, txtItinerary.Text, txtCobroParticipacionNj.Text, txtParticipacionCobro.Text);
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
                if ((IsFloatValue(txtQty.Text) && IsFloatValue(txtCost.Text)) && (!string.IsNullOrEmpty(txtQty.Text) && !string.IsNullOrEmpty(txtCost.Text)))
                {
                    if (lblSrType.Text == "FBO")
                    {
                        txtPrice.Text = Math.Round(((double.Parse(txtQty.Text) * double.Parse(txtCost.Text)) * 1.30), 4).ToString();
                    }
                    else if (lblSrType.Text == "FCC")
                    {
                        if (IsFloatValue(txtCost.Text)) // && GetPrices() == 0)
                        {
                            // cboCurrency.Text = "USD";
                            DateTime date = DateTime.Parse(txtATA.Text);
                            txtPrice.Text = Math.Round((Convert.ToDouble(txtCost.Text) + (Convert.ToDouble(txtCost.Text) * GetUtilidadPercentage(txtUtilidad.Text) / 100)) / getExchangeRate(date), 2, MidpointRounding.AwayFromZero).ToString();
                        }
                        if (txtItemNumber.Text == "ASECSAS0073")
                        {
                            if (IsNumber(txtCost.Text))
                            {
                                double minutehour = GetMinutesLeg();
                                txtPrice.Text = Math.Round((Convert.ToDouble(txtCost.Text) * minutehour), 2, MidpointRounding.AwayFromZero).ToString();
                            }
                        }
                    }
                    else if (lblSrType.Text == "FUEL")
                    {
                    }
                    else
                    {
                        txtPrice.Text = Math.Round((double.Parse(txtQty.Text) * double.Parse(txtCost.Text)), 4).ToString();
                    }
                }
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
            try
            {
                if (validateFBOFee())
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
                            var request = new RestRequest("/services/rest/connect/v1.4/CO.Services/" + dgvRenglon.Cells[6].Value.ToString() + "", Method.POST)
                            {
                                RequestFormat = DataFormat.Json
                            };
                            var body = "{";
                            // Información de precios costos
                            body += "\"ListoFactura\":true," +
                                "\"Precio\":\"" + dgvRenglon.Cells[4].Value.ToString() + "\"," +
                                "\"Costo\":\"" + dgvRenglon.Cells[3].Value.ToString() + "\"";
                            if (!String.IsNullOrEmpty(dgvRenglon.Cells[1].Value.ToString()))
                            {
                                body += ",\"IDProveedor\":\"" + dgvRenglon.Cells[1].Value.ToString() + "\"";
                            }
                            body += "}";
                            global.LogMessage(body);

                            request.AddParameter("application/json", body, ParameterType.RequestBody);
                            request.AddHeader("Authorization", "Basic ZW9saXZhczpTaW5lcmd5KjIwMTg=");
                            request.AddHeader("X-HTTP-Method-Override", "PATCH");
                            request.AddHeader("OSvC-CREST-Application-Context", "Update Service {id}");
                            IRestResponse response = client.Execute(request);
                            var content = response.Content;
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
            }
            catch (Exception ex)
            {
                MessageBox.Show("saveToolStripMenuItem_Click" + ex.Message + "Det: " + ex.StackTrace);
            }

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
        /*
         * private void txtCost_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double pricefinal = 0;
                if (txtItemNumber.Text == "SOMFEAP325" || txtItemNumber.Text == "SOMFEAP260" || lblSrType.Text == "SENEAM")
                {
                    pricefinal = Convert.ToDouble(txtPrice.Text);
                }

                if (lblSrType.Text == "CATERING")
                {
                    // cboCurrency.Text = "USD";
                    double rate = getExchangeRate(DateTime.Parse(txtCateringDDate.Text));

                    if (txtUtilidad.Text == "A")
                    {
                        pricefinal = double.Parse(txtCost.Text);

                        pricefinal = pricefinal / rate;
                        pricefinal = Math.Round(pricefinal, 4);
                    }
                    else
                    {
                        if (double.Parse(txtCost.Text) > 0)
                        {
                            double precio = Convert.ToDouble(txtCost.Text);
                            double utilidad = GetUtilidadPercentage(txtUtilidad.Text) / 100;

                            precio = precio + (precio * utilidad);
                            pricefinal = Math.Round(precio, 4);

                            if (lblCurrencyPrice.Text == "USD")
                            {
                                precio = Convert.ToDouble(pricefinal);
                                precio = precio / rate;
                                pricefinal = Math.Round(precio, 4);
                            }
                        }
                    }
                }
                if (lblSrType.Text == "FBO")
                {
                    if (IsFloatValue(txtCost.Text) && txtItemNumber.Text != "ASFIEAP357")
                    {
                        pricefinal = Math.Round((Convert.ToDouble(txtCost.Text) * 1.30), 4);
                    }
                }
                if (lblSrType.Text == "FUEL")
                {
                    if (txtItemNumber.Text == "AGASIAS0270" || txtItemNumber.Text == "JFUEIAS0269" || txtItemNumber.Text == "AGASIAS0011" || txtItemNumber.Text == "JFUEIAS0010")
                    {
                        double b;
                        if (double.TryParse(txtCost.Text, out b))
                        {
                            pricefinal = GetFuelPrice();
                        }

                    }
                }
                if (lblSrType.Text == "FCC")
                {
                    if (IsNumber(txtCost.Text) && GetPrices() == 0)
                    {
                        // cboCurrency.Text = "USD";
                        DateTime date = DateTime.Parse(txtATA.Text);
                        pricefinal = ((Convert.ToDouble(txtCost.Text) + (Convert.ToDouble(txtCost.Text) * GetUtilidadPercentage(txtUtilidad.Text) / 100)) / getExchangeRate(date));
                    }

                    if (txtItemNumber.Text == "ASECSAS0073")
                    {
                        if (IsNumber(txtCost.Text))
                        {
                            double minutehour = GetMinutesLeg();
                            pricefinal = Math.Round((Convert.ToDouble(txtCost.Text) * minutehour), 4);
                        }
                    }

                    if (isComponent())
                    {
                        pricefinal = 0;
                    }
                }
                txtPrice.Text = pricefinal.ToString();
                /*
                 * if (!string.IsNullOrEmpty(txtQty.Text) && IsFloatValue(txtQty.Text))
                {
                    txtPrice.Text = (pricefinal * double.Parse(txtQty.Text)).ToString();
                    if (lblSrType.Text == "FBO")
                    {
                        txtPrice.Text = ((double.Parse(txtCost.Text) * double.Parse(txtQty.Text)) * 1.30).ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                global.LogMessage("Error en txtCost.Text:" + ex.Message + "Det:" + ex.StackTrace);
            }
        } */
        // Functions
        // IS COMPONENT - EO
        private bool isComponent()
        {
            try
            {
                bool component = true;

                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT Componente FROM CO.Services WHERE ID =" + txtIdService.Text;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        component = data == "1" ? true : false;
                    }
                }

                return component;
            }
            catch (Exception ex)
            {
                global.LogMessage("IsComponent: " + ex.Message + "Det: " + ex.StackTrace);
                return false;
            }
        }
        public bool isBHInside()
        {
            try
            {
                bool BH = false;
                MessageBox.Show("Es MHLM && MGGT");
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT COUNT(ItemNumber) FROM CO.Services WHERE Itinerary = " + txtItinerary.Text + " AND ItemNumber = 'BHANSSP0004'";
                global.LogMessage(queryString);
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        if (Convert.ToDouble(data) > 0)
                        {
                            BH = true;
                            MessageBox.Show("Si existe el producto");
                        }
                    }
                }
                return BH;
            }
            catch (Exception ex)
            {
                global.LogMessage("isBHInside: " + ex.Message + "Det: " + ex.StackTrace);
                return false;
            }
        }
        public double GetMinutesLeg()
        {
            try
            {
                double minutes = 0;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                clientInfoHeader.AppID = "Query Example";
                //String queryString = "SELECT (Date_Diff(ATA_ZUTC,ATD_ZUTC)/60) FROM CO.Itinerary WHERE ID =" + Itinerarie + "";
                String queryString = "SELECT ATA,ATATime,ATD,ATDTime  FROM CO.Itinerary WHERE ID =" + txtItinerary.Text + "";
                global.LogMessage(queryString);
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Char delimiter = '|';
                        string[] substrings = data.Split(delimiter);
                        DateTime ATA = DateTime.Parse(substrings[0] + " " + substrings[1]);
                        DateTime ATD = DateTime.Parse(substrings[2] + " " + substrings[3]);
                        minutes = (ATD - ATA).TotalMinutes;
                    }
                }
                if (txtCustomerClass.Text == "ASI_SECURITY")
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
        public double GetFuelPrice()
        {
            try
            {
                DateTime datecharge = DateTime.Now;
                double galonprice = Convert.ToDouble(txtCost.Text) * 3.7853;
                //MessageBox.Show("Costo por galon: " + galonprice);
                if (lblSrType.Text == "FUEL")
                {
                    datecharge = DateTime.Parse(txtFuelDateCharge.Text);
                }
                else
                {
                    datecharge = DateTime.Parse(txtATA.Text);
                }
                //MessageBox.Show("Fecha de carga: " + datecharge);
                double rate = getExchangeRateSemanal(datecharge);
                // MessageBox.Show("Tipo de cambio: $ " + rate);
                double galonrate = galonprice / rate; // costo por galon
                //MessageBox.Show("Costo por galon USD: " + galonrate);
                double catcombus = GetCombCents(txtCombustible.Text);
                //MessageBox.Show("Centavos : " + catcombus);
                galonrate = (galonrate + catcombus);
                //MessageBox.Show("Costo mas centavos : " + galonrate);
                double IVA = (galonrate * .16);
                //MessageBox.Show("IVA : " + IVA);
                galonrate = galonrate + IVA;
                //MessageBox.Show("Costo mas IVA : " + galonrate);
                if (txtItemNumber.Text == "AGASIAS0270" || txtItemNumber.Text == "JFUEIAS0269")
                {
                    galonrate = galonrate - GetCombCentI(txtCombustibleI.Text);
                    //MessageBox.Show("Costo menos Cents Int : " + galonrate);
                }
                double galones = Convert.ToDouble(txtGalones.Text);
                //MessageBox.Show("Galones : " + galones);
                galonrate = galonrate * galones;
                //MessageBox.Show("Costo total : " + galonrate);
                return Math.Round((galonrate), 4);
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetFuelPrice: " + ex.Message + "Det:" + ex.StackTrace);
                return 0;
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
            try
            {
                bool vali = true;
                List<ItiPrices> itiPrices = new List<ItiPrices>();
                itiPrices = getInvoiceItineraries();
                double pricecompare = 0;
                foreach (var item in itiPrices)
                {
                    foreach (DataGridViewRow dgvRenglon in dataGridInvoice.Rows)
                    {
                        int itinerarycompare = String.IsNullOrEmpty(dgvRenglon.Cells[8].Value.ToString()) ? 0 : Convert.ToInt32(dgvRenglon.Cells[8].Value);
                        if (lblSrType.Text == "FBO" && dgvRenglon.Cells[0].Value.ToString().Contains("LOGISTIC / LOGISTICA") && item.Itinerarie == itinerarycompare)
                        {
                            pricecompare = pricecompare + Convert.ToDouble(dgvRenglon.Cells[4].Value);
                        }
                    }
                    if (item.Limit < pricecompare)
                    {
                        vali = false;
                        MessageBox.Show("The prices of Logistic Fee excedees Flight Logistic Limit in Itinerary: " + item.Itinerarie.ToString());
                    }
                }
                return vali;
            }
            catch (Exception ex)
            {
                MessageBox.Show("validateFBOFee" + ex.Message + "DEtalle: " + ex.StackTrace);
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
                String queryString = "SELECT ATA,ATATime,ATD,ATDTime,ArrivalAirport,ToAirport.Country.LookupName,ToAirport.Country.LookupName,FromAirport.Country.LookupName FROM CO.Itinerary WHERE Incident1 =" + lblIdIncident.Text + " AND ID =" + Itinerary + "";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Char delimiter = '|';
                        String[] substrings = data.Split(delimiter);
                        txtATA.Text = DateTime.Parse(substrings[0] + " " + substrings[1]).ToString();
                        txtATD.Text = DateTime.Parse(substrings[2] + " " + substrings[3]).ToString();
                        int Arri = String.IsNullOrEmpty(substrings[4]) ? 0 : Convert.ToInt32(substrings[4]);
                        getArrivalHours(Arri, DateTime.Parse(txtATA.Text).ToLocalTime().ToString("yyyy-MM-dd"), DateTime.Parse(txtATD.Text).ToLocalTime().ToString("yyyy-MM-dd"));
                        txtArrivalAiport.Text = substrings[4];
                        txtLimit.Text = getGrupoLogLimit(Arri);
                        txtAirportFee.Text = getAirportCollectionFee(Arri);
                        txtCateringCollection.Text = getAirportCateringCollectionFee(Arri);
                        txtCollectionDeduction.Text = getAirportCollectionDeductionFee(Arri);
                        txtToAirtport.Text = substrings[6];
                        txtFromAirport.Text = substrings[7];
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
        private void getArrivalHours(int Arrival, string AtaDate, string ATDDate)
        {
            try
            {
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT OpensZULUTime,ClosesZULUTime,Type, ID FROM CO.Airport_WorkingHours WHERE Airports =" + Arrival + "";
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

                        hours.ATAOpens = DateTime.Parse(AtaDate + " " + substrings[0]);
                        hours.ATACloses = DateTime.Parse(AtaDate + " " + substrings[1]);
                        hours.ATDOpens = DateTime.Parse(ATDDate + " " + substrings[0]);
                        hours.ATDCloses = DateTime.Parse(ATDDate + " " + substrings[1]);
                        //MessageBox.Show(hours.Closes.ToString());

                        if (DateTime.Compare(hours.ATAOpens, hours.ATACloses) > 0)
                        {
                            hours.ATACloses = hours.ATACloses.AddDays(1);
                            hours.ATDCloses = hours.ATDCloses.AddDays(1);
                            //MessageBox.Show(hours.Closes.ToString());

                        }
                        hours.id = Convert.ToInt32(substrings[3].Trim());
                        switch (substrings[2].Trim())
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
                        //MessageBox.Show("Type: " + hours.Type.ToString() + "   Opens: " + hours.Opens.ToString() + "   Closes: " + hours.Closes.ToString());
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
                List<Rate> rates = new List<Rate>();
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
                                    var lista = res.G_N_RATES.Find(x => (x.USER_CONVERSION_TYPE.Trim() == "DOF"));
                                    if (lista != null)
                                    {
                                        rate = Convert.ToDouble(lista.G_1_RATES.CONVERSION_RATE);
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
        private double getExchangeRateSemanal(DateTime date)
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
                                    var lista = res.G_N_RATES.Find(x => (x.USER_CONVERSION_TYPE.Trim() == "Semanal"));
                                    if (lista != null)
                                    {
                                        rate = Convert.ToDouble(lista.G_1_RATES.CONVERSION_RATE);
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
                if (!IsFloatValue(txtQty.Text) || txtQty.Text == "0")
                {
                    res = false;
                }
                if (!IsFloatValue(txtPrice.Text))
                {
                    res = false;
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
            try
            {
                List<ItiPrices> itineraries = new List<ItiPrices>();
                foreach (DataGridViewRow dgvRenglon in dataGridInvoice.Rows)
                {
                    ItiPrices itiPrices = new ItiPrices();
                    itiPrices.Itinerarie = String.IsNullOrEmpty(dgvRenglon.Cells[8].Value.ToString()) ? 0 : Convert.ToInt32(dgvRenglon.Cells[8].Value);
                    itiPrices.Limit = getGrupoLogLimitItinerary(itiPrices.Itinerarie);
                    itineraries.Add(itiPrices);
                }
                return itineraries.DistinctBy(x => x.Itinerarie).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("getInvoiceItineraries" + ex.Message + "DEtalle: " + ex.StackTrace);
                return null;
            }

        }
        private void ClearTxtBoxes()
        {
            txtAmount.Text = "0";
            txtCost.Text = "";
            txtIdService.Text = "";

            txtItem.Text = "";
            txtItemNumber.Text = "";
            txtPrice.Text = "";
            txtQty.Text = "1";

        }
        public bool IsFloatValue(string text)
        {
            Regex regex = new Regex(@"^[0-9]*(?:\.[0-9]*)?$");
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
                string arr_type = "DOMESTIC";
                string dep_type = "DOMESTIC";

                if (lblSrType.Text == "FBO" || lblSrType.Text == "FCC")
                {
                    ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                    APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                    clientInfoHeader.AppID = "Query Example";
                    String queryString = "SELECT ToAirport.Country.LookupName,FromAirport.Country.LookupName FROM CO.Itinerary WHERE ID = " + txtItinerary.Text;
                    clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                    foreach (CSVTable table in queryCSV.CSVTables)
                    {
                        String[] rowData = table.Rows;
                        foreach (String data in rowData)
                        {
                            Char delimiter = '|';
                            string[] substrings = data.Split(delimiter);
                            if (substrings[1] != "MX")
                            {
                                arr_type = "INTERNATIONAL";
                            }
                            if (substrings[0] != "MX")
                            {
                                dep_type = "INTERNATIONAL";
                            }
                        }
                    }
                }
                string OUM = "";
                string Curr = "";
                double cost = 0;

                if (lblSrType.Text == "CATERING")
                {
                    if (GetTicketSumCatA() > 0)
                    {
                        cost = GetTicketSumCatA();
                    }
                    else
                    {
                        cost = 0;
                    }
                }
                else if (GetTicketSumCatA() > 0)
                {
                    if (GetTicketSumCatA() > 0)
                    {
                        cost = GetTicketSumCatA();
                    }
                }
                else if (lblSrType.Text == "GYCUSTODIA" && txtItemNumber.Text == "MHSPSAS0091")
                {
                    cost = GetHSCost();
                }
                else
                {
                    string definicion = "";
                    var client = new RestClient("https://iccs.bigmachines.com/");
                    //string User = Encoding.UTF8.GetString(Convert.FromBase64String("aW1wbGVtZW50YWRvcg=="));
                    //string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJIwMTgu"));
                    client.Authenticator = new HttpBasicAuthenticator("servicios", "Sinergy*2018");
                    // string definicion = "?totalResults=false&q={str_item_number:'" + dataGridServicios.Rows[e.RowIndex].Cells[1].FormattedValue.ToString().Trim() + "',str_icao_iata_code:'" + airtport + "'}";
                    if (lblSrType.Text == "FBO")
                    {
                        definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',bol_int_fbo:1,$or:[{str_schedule_type:{$exists:false}},{str_schedule_type:'" + txtMainHour.Text + "'}],$or:[{str_aircraft_type:{$exists:false}},{str_aircraft_type:'" + txtICAOD.Text + "'}],$or:[{str_client_category:{$exists:false}},{str_client_category:'" + txtCustomerClass.Text.Replace("&", "%") + "'}]}";
                        if (txtCategorias.Text.Contains("AERO"))
                        {
                            definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',bol_int_fbo: 1,str_schedule_type:'" + txtMainHour.Text + "',$or:[{str_aircraft_type:'" + txtICAOD.Text + "'},{str_aircraft_type:{$exists:false}}],$or:[{str_ft_arrival:'" + arr_type.ToUpper() + "'},{str_ft_arrival:{$exists: false}}],$or:[{str_ft_depart:'" + dep_type.ToUpper() + "'},{str_ft_depart:{$exists: false}}],$or:[{str_client_category:{$exists:false}},{str_client_category:'" + txtCustomerClass.Text + "'}]}";
                        }
                        if (txtItemNumber.Text == "LANDSAF0008")
                        {
                            definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',bol_int_fbo: 1,str_schedule_type:'" + txtMainHour.Text + "'}&orderby=str_icao_iata_code:asc";
                        }
                        // FUEL
                        if (txtItemNumber.Text == "ANFERAS0013" || txtItemNumber.Text == "ANIASAS0015" || txtItemNumber.Text == "AGASIAS0270" || txtItemNumber.Text == "JFUEIAS0269" || txtItemNumber.Text == "AGASIAS0011" || txtItemNumber.Text == "JFUEIAS0010" || txtItemNumber.Text == "AFMURAS0016")
                        {
                            definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',str_schedule_type:'NORMAL'}";

                            if (txtItemNumber.Text == "IAFMUAS0271" || txtItemNumber.Text == "AFMURAS0016")
                            {
                                if (txtClientName.Text.Contains("NETJETS"))
                                {
                                    definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',str_client_category:'NTJET'}";
                                }
                                definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "'}";
                            }
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
                    if (lblSrType.Text == "FCC")
                    {
                        int cargo = 0;
                        string grupo = txtPaxGroup.Text;
                        if (isCargo())
                        {
                            cargo = 1;
                            grupo = txtCargoGroup.Text;
                        }

                        definicion = "?totalResults=true&q={bol_int_fbo:0,";
                        if (isFBOPrice() && !txtCategorias.Text.Contains("PERMISOS"))
                        {
                            definicion = "?totalResults=true&q={bol_int_fbo:1,";
                        }
                        if (txtCategorias.Text.Contains("AERO"))
                        {
                            definicion += "str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',bol_int_flight_cargo:" + cargo.ToString() + ",str_schedule_type:'" + txtMainHour.Text + "',str_aircraft_type:'" + txtICAOD.Text + "',str_ft_arrival: '" + arr_type.ToString() + "', str_ft_depart: '" + dep_type.ToString() + "',$or:[{str_client_category:{$exists:false}},{str_client_category:'" + txtCustomerClass.Text.Replace("&", "%") + "'}]}";
                        }
                        else if (txtItemNumber.Text == "ASECSAS0073")
                        {
                            definicion += "str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',str_client_category:'ASI_SECURITY'} ";
                        }
                        else if (txtItemNumber.Text == "IPFERPS0052")
                        {
                            //definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',str_aircraft_type:'" + txtICAOD.Text + "'}";
                            definicion += "str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',$or:[{str_ft_arrival:'" + arr_type.ToString() + "'},{str_ft_arrival:{$exists: false}}],$or:[{str_client_category:{$exists:false}},{str_client_category:'" + txtCustomerClass.Text.Replace("&", "%") + "'}]}";
                        }
                        else if (txtItemNumber.Text == "OHANIAS0129" || txtItemNumber.Text == "OPLAIAS0128" || txtItemNumber.Text == "OFPLIAS0130")
                        {
                            definicion += "str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "'}";
                        }
                        else if (txtItemNumber.Text == "ATPRIAP304")
                        {
                            definicion = "?totalResults=true&q={bol_int_fbo:0,str_item_number:'" + txtItemNumber.Text + "'}";
                        }
                        else if (txtCategorias.Text.Contains("PERMISOS"))
                        {
                            definicion += "str_item_number:'" + txtItemNumber.Text + "', str_client_category:'" + txtCustomerClass.Text.Replace("&", "%") + "'}";
                        }
                        else
                        {
                            definicion += "str_item_number:'" + txtItemNumber.Text +
                                "',$or:[{str_icao_iata_code:{$exists:false}},{str_icao_iata_code:'" + txtAirport.Text + "'}]" +
                                ",$or:[{str_aircraft_type:{$exists:false}},{str_aircraft_type:'" + txtICAOD.Text + "'}]" +
                                ",str_ft_arrival:'" + arr_type.ToString() + "', str_ft_depart: '" + dep_type.ToString() +
                                "',str_schedule_type:'" + txtMainHour.Text + "'}";
                        }
                        definicion += "&orderby=flo_cost,flo_cost:asc";
                    }
                    if (lblSrType.Text == "FUEL")
                    {
                        if (txtItemNumber.Text == "ANFERAS0013" || txtItemNumber.Text == "ANIASAS0015" || txtItemNumber.Text == "AGASIAS0270" || txtItemNumber.Text == "JFUEIAS0269" || txtItemNumber.Text == "AGASIAS0011" || txtItemNumber.Text == "JFUEIAS0010" || txtItemNumber.Text == "AFMURAS0016")
                        {
                            definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',str_schedule_type:'NORMAL'}";

                            if (txtItemNumber.Text == "IAFMUAS0271" || txtItemNumber.Text == "AFMURAS0016")
                            {
                                if (txtClientName.Text.Contains("NETJETS"))
                                {
                                    definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',str_client_category:'NetJets'}";
                                }
                                definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "'}";
                            }
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
                    if (lblSrType.Text == "PERMISOS")
                    {
                        definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "'}";
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
                                DateTime inicio = DateTime.Parse(item.str_start_date + " " + "00:00");
                                DateTime fin = DateTime.Parse(item.str_end_date + " " + "23:59");
                                DateTime fecha = DateTime.Parse(txtFuelDateCharge.Text);

                                if (fecha.CompareTo(inicio) >= 0 && fecha.CompareTo(fin) <= 0)
                                {
                                    cost = item.flo_cost;
                                    Curr = item.str_currency_code;
                                    OUM = item.str_uom_code;
                                }
                            }
                        }
                        else if (lblSrType.Text == "FBO" || lblSrType.Text == "FCC")
                        {
                            foreach (ClaseParaCostos.Item item in rootObjectCosts.items)
                            {
                                DateTime inicio = DateTime.Parse(item.str_start_date + " " + "00:00");
                                DateTime fin = DateTime.Parse(item.str_end_date + " " + "23:59");
                                DateTime fecha = DateTime.Parse(txtATA.Text);

                                if (fecha.CompareTo(inicio) >= 0 && fecha.CompareTo(fin) <= 0)
                                {
                                    cost = item.flo_cost;
                                    Curr = item.str_currency_code;
                                    OUM = item.str_uom_code;
                                }
                            }
                        }
                        else if (lblSrType.Text == "PERMISOS")
                        {
                            foreach (ClaseParaCostos.Item item in rootObjectCosts.items)
                            {
                                DateTime inicio = DateTime.Parse(item.str_start_date + " " + "00:00");
                                DateTime fin = DateTime.Parse(item.str_end_date + " " + "23:59");
                                DateTime fecha = DateTime.Parse(GetSRCreationDate(Convert.ToInt32(lblIdIncident.Text)));

                                if (fecha.CompareTo(inicio) >= 0 && fecha.CompareTo(fin) <= 0)
                                {
                                    cost = item.flo_cost;
                                    Curr = item.str_currency_code;
                                    OUM = item.str_uom_code;
                                }
                            }
                        }
                        else
                        {
                            cost = rootObjectCosts.items[0].flo_cost;
                            Curr = rootObjectCosts.items[0].str_currency_code;
                            OUM = rootObjectCosts.items[0].str_uom_code;
                        }
                        txtUOM.Text = OUM;
                    }
                    else
                    {
                        cost = 0;
                    }
                }
                Currency = Curr;
                lblCurrencyCost.Text = Currency;
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
            string arr_type = "DOMESTIC";
            string dep_type = "DOMESTIC";

            if (lblSrType.Text == "FBO" || lblSrType.Text == "FCC")
            {
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ToAirport.Country.LookupName,FromAirport.Country.LookupName FROM CO.Itinerary WHERE ID = " + txtItinerary.Text;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Char delimiter = '|';
                        string[] substrings = data.Split(delimiter);
                        if (substrings[1] != "MX")
                        {
                            arr_type = "INTERNATIONAL";
                        }
                        if (substrings[0] != "MX")
                        {
                            dep_type = "INTERNATIONAL";
                        }
                    }
                }
            }
            string Curr = "";
            string OUM = "";
            double price = 0;
            try
            {
                var client = new RestClient("https://iccs.bigmachines.com/");
                //string User = Encoding.UTF8.GetString(Convert.FromBase64String("aW1wbGVtZW50YWRvcg=="));
                //string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJneTIwMTgu"));
                client.Authenticator = new HttpBasicAuthenticator("servicios", "Sinergy*2018");
                string definicion = "";
                // string definicion = "?totalResults=false&q={str_item_number:'" + dataGridServicios.Rows[e.RowIndex].Cells[1].FormattedValue.ToString().Trim() + "',str_icao_iata_code:'" + airtport + "'}";
                // string definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "'}";
                if (lblSrType.Text == "PERMISOS")
                {
                    definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "'}";
                }
                if (lblSrType.Text == "FBO")
                {
                    int cargo = 0;
                    string grupo = txtPaxGroup.Text;
                    if (isCargo())
                    {
                        cargo = 1;
                        grupo = txtCargoGroup.Text;
                    }
                    definicion = "?totalResults=true&q={bol_int_fbo:0,";
                    if (isFBOPrice())
                    {
                        definicion = "?totalResults=true&q={bol_int_fbo:1,";
                    }
                    if (txtCategorias.Text.Contains("AERO"))
                    {
                        definicion += "str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',bol_int_flight_cargo:1,str_schedule_type:'" + txtMainHour.Text + "',str_aircraft_type:'" + txtICAOD.Text + "',str_ft_arrival: '" + arr_type.ToString() + "', str_ft_depart: '" + dep_type.ToString() + "',$or:[{str_client_category:{$exists:false}},{str_client_category:'" + txtCustomerClass.Text.Replace("&", "%") + "'}]}";
                    }
                    if (txtItemNumber.Text == "OHANIAS0129" || txtItemNumber.Text == "OPLAIAS0128" || txtItemNumber.Text == "OFPLIAS0130")
                    {
                        definicion += "str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "'}";
                    }
                    else
                    {
                        definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',bol_int_fbo:1,$or:[{str_schedule_type:{$exists:false}},{str_schedule_type:'" + txtMainHour.Text + "'}],$or:[{str_aircraft_type:{$exists:false}},{str_aircraft_type:'" + txtICAOD.Text + "'}],$or:[{str_client_category:{$exists:false}},{str_client_category:'" + txtCustomerClass.Text.Replace("&", "%") + "'}]}";
                    }
                }
                if (lblSrType.Text == "FUEL")
                {
                    if (txtItemNumber.Text == "IAFMUAS0271")
                    {
                        if (txtClientName.Text.Contains("NETJETS"))
                        {
                            definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',str_client_category:'NTJET'}";
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
                    int cargo = 0;
                    string grupo = txtPaxGroup.Text;
                    if (isCargo())
                    {
                        cargo = 1;
                        grupo = txtCargoGroup.Text;
                    }

                    definicion = "?totalResults=true&q={bol_int_fbo:0,";
                    if (isFBOPrice() && !txtCategorias.Text.Contains("PERMISOS"))
                    {
                        definicion = "?totalResults=true&q={bol_int_fbo:1,";
                    }
                    if (txtCategorias.Text.Contains("AERO"))
                    {
                        definicion += "str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',bol_int_flight_cargo:" + cargo.ToString() + ",str_schedule_type:'" + txtMainHour.Text + "',str_aircraft_type:'" + txtICAOD.Text + "',str_ft_arrival: '" + arr_type.ToString() +  "', str_ft_depart: '" + dep_type.ToString() + "',$or:[{str_client_category:{$exists:false}},{str_client_category:'" + txtCustomerClass.Text.Replace("&", "%") + "'}]}";
                    }
                    else if (txtCustomerClass.Text == "NTJET")
                    {
                        definicion += "str_item_number:'" + txtItemNumber.Text +
                            "',$or:[{str_icao_iata_code:{$exists:false}},{str_icao_iata_code:'" + txtAirport.Text + "'}]" +
                            ",$or:[{str_aircraft_type:{$exists:false}},{str_aircraft_type:'" + txtICAOD.Text + "'}]" +
                            ",bol_int_flight_cargo:'0'" +
                            ",str_ft_arrival:'" + arr_type.ToString() + "', str_ft_depart: '" + dep_type.ToString() +
                            "',str_schedule_type:'" + txtMainHour.Text +
                            "',str_client_category:'NTJET'}";
                    }
                    else if (txtItemNumber.Text == "ASECSAS0073")
                    {
                        definicion += "str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',str_client_category:'ASI_SECURITY'} ";
                    }
                    else if (txtItemNumber.Text == "IPFERPS0052")
                    {
                        //definicion = "?totalResults=true&q={str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',str_aircraft_type:'" + txtICAOD.Text + "'}";
                        definicion += "str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "',$or:[{str_ft_arrival:'" + arr_type.ToString() + "'},{str_ft_arrival:{$exists: false}}],$or:[{str_client_category:{$exists:false}},{str_client_category:'" + txtCustomerClass.Text.Replace("&", "%") + "'}]}";
                    }
                    else if (txtItemNumber.Text == "OHANIAS0129" || txtItemNumber.Text == "OPLAIAS0128" || txtItemNumber.Text == "OFPLIAS0130")
                    {
                        definicion += "str_item_number:'" + txtItemNumber.Text + "',str_icao_iata_code:'" + txtAirport.Text + "'}";
                    }
                    else if (txtItemNumber.Text == "ATPRIAP304")
                    {
                        definicion = "?totalResults=true&q={bol_int_fbo:0,str_item_number:'" + txtItemNumber.Text + "'}";
                    }
                    else if (txtCategorias.Text.Contains("PERMISOS"))
                    {
                        definicion += "str_item_number:'" + txtItemNumber.Text + "', str_client_category:'" + txtCustomerClass.Text.Replace("&", "%") + "'}";
                    }
                    else
                    {
                        definicion += "str_item_number:'" + txtItemNumber.Text +
                            "',$or:[{str_icao_iata_code:{$exists:false}},{str_icao_iata_code:'" + txtAirport.Text + "'}]" +
                            ",$or:[{str_aircraft_type:{$exists:false}},{str_aircraft_type:'" + txtICAOD.Text + "'}]" +
                            ",$or:[{str_aircraft_group:{$exists:false}},{str_aircraft_group:'" + grupo.ToString() + "'}]" +
                            ",bol_int_flight_cargo:" + cargo.ToString() +
                            ",str_ft_arrival:'" + arr_type.ToString() + "', str_ft_depart: '" + dep_type.ToString() +
                            "',str_schedule_type:'" + txtMainHour.Text +
                            "',$or:[{str_client_category:{$exists:false}},{str_client_category:'" + txtCustomerClass.Text.Replace("&", "%") + "'}]}";
                    }
                    definicion += "&orderby=flo_amount,flo_amount:asc";
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
                            DateTime inicio = DateTime.Parse(item.str_start_date + " " + "00:00");
                            DateTime fin = DateTime.Parse(item.str_end_date + " " + "23:59");
                            DateTime fecha = DateTime.Parse(txtFuelDateCharge.Text);

                            if (fecha.CompareTo(inicio) >= 0 && fecha.CompareTo(fin) <= 0)
                            {
                                price = item.flo_amount;
                                Curr = item.str_currency_code;
                                OUM = item.str_oum_code;
                            }
                        }
                    }
                    else if (lblSrType.Text == "FBO" || lblSrType.Text == "FCC")
                    {
                        foreach (ClaseParaPrecios.Item item in rootObjectPrices.items)
                        {
                            DateTime inicio = DateTime.Parse(item.str_start_date + " " + "00:00");
                            DateTime fin = DateTime.Parse(item.str_end_date + " " + "23:59");
                            DateTime fecha = DateTime.Parse(txtATA.Text);

                            if (fecha.CompareTo(inicio) >= 0 && fecha.CompareTo(fin) <= 0)
                            {
                                price = item.flo_amount;
                                Curr = item.str_currency_code;
                                OUM = item.str_oum_code;
                            }
                        }
                    }
                    else if (lblSrType.Text == "PERMISOS")
                    {
                        foreach (ClaseParaPrecios.Item item in rootObjectPrices.items)
                        {
                            DateTime inicio = DateTime.Parse(item.str_start_date + " " + "00:00");
                            DateTime fin = DateTime.Parse(item.str_end_date + " " + "23:59");
                            DateTime fecha = DateTime.Parse(GetSRCreationDate(Convert.ToInt32(lblIdIncident.Text)));

                            if (fecha.CompareTo(inicio) >= 0 && fecha.CompareTo(fin) <= 0)
                            {
                                price = item.flo_amount;
                                Curr = item.str_currency_code;
                                OUM = item.str_oum_code;
                                lblCurrencyPrice.Text = Curr;
                            }
                        }
                    }
                    else
                    {
                        price = rootObjectPrices.items[0].flo_amount;
                        Curr = rootObjectPrices.items[0].str_currency_code;
                        OUM = rootObjectPrices.items[0].str_oum_code;
                    }
                }
                else
                {
                    price = 0;
                }
                if (lblSrType.Text == "FBO" && price == 0)
                {
                    price = Math.Round(((double.Parse(txtQty.Text) * double.Parse(txtCost.Text)) * 1.30), 4);
                }
                if (lblSrType.Text == "FCC" && price == 0 && txtPackage.Text != "Yes")
                {
                    // cboCurrency.Text = "USD";
                    DateTime date = DateTime.Parse(txtATA.Text);
                    price = ((Convert.ToDouble(txtCost.Text) + (Convert.ToDouble(txtCost.Text) * GetUtilidadPercentage(txtUtilidad.Text) / 100)) / getExchangeRateSemanal(date));
                }
                if (isComponent())
                {
                    price = 0;
                }
                if (lblSrType.Text == "CATERING")
                {
                    price = Convert.ToDouble(txtPrice.Text);
                }
                return price;
            }
            catch (Exception ex)
            {
                global.LogMessage("GetPrices: " + ex.Message + "Detalle: " + ex.StackTrace);

                return 0;
            }
        }
        private double GetSeneamPercentage(string Utilidad)
        {
            try
            {
                double amount = 0;
                var client = new RestClient("https://iccs.bigmachines.com/");
                string User = Encoding.UTF8.GetString(Convert.FromBase64String("aW1wbGVtZW50YWRvcg=="));
                string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJneSoyMDE4"));
                client.Authenticator = new HttpBasicAuthenticator("servicios", "Sinergy*2018");
                string definicion = "?q={str_tipo:'SENEAM',str_categoria:'" + Utilidad + "'} ";
                var request = new RestRequest("rest/v6/customCategorias/" + definicion, Method.GET);
                IRestResponse response = client.Execute(request);
                ClaseParaCategorias.RootObject rootObjectCat = JsonConvert.DeserializeObject<ClaseParaCategorias.RootObject>(response.Content);
                if (rootObjectCat.items.Count > 0)
                {
                    amount = Convert.ToDouble(rootObjectCat.items[0].flo_value.ToString());
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
        private double GetUtilidadPercentage(string Utilidad)
        {
            try
            {
                double amount = 0;
                var client = new RestClient("https://iccs.bigmachines.com/");
                string User = Encoding.UTF8.GetString(Convert.FromBase64String("aW1wbGVtZW50YWRvcg=="));
                string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJneSoyMDE4"));
                client.Authenticator = new HttpBasicAuthenticator("servicios", "Sinergy*2018");
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
                String queryString = "SELECT VoucherDateTime,VoucherTime FROM CO.Fueling WHERE ID =" + idFueling + " ";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Char delimiter = '|';
                        String[] substrings = data.Split(delimiter);
                        Fueling = DateTime.Parse(substrings[0] + " " + substrings[1]).ToLocalTime().ToString();
                    }
                }
                return Fueling;
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
                String queryString = "SELECT SUM(TicketAmount), Currency FROM Co.Payables WHERE Services.ID = " + txtIdService.Text + " GROUP BY Currency";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        double cost = 0;
                        String cur = "";
                        Char delimiter = '|';
                        String[] substrings = data.Split(delimiter);
                        cost = Convert.ToDouble(substrings[0]);
                        cur = substrings[1];

                        if (cur == "2")
                        {
                            double tipoCambio = getExchangeRate(DateTime.Today);
                            sum = sum + (cost * tipoCambio);
                        }
                        else
                        {
                            sum = sum + cost;
                        }
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
                        txtQty.Text = Math.Round(Convert.ToDouble(data), 4).ToString();
                        lblQty.Text = "Quantity (Lt)";
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
                String queryString = "SELECT CollectionFee  FROM CO.AirportFee WHERE Airports = " + Airport + " AND ClientCategory.Name = '" + txtRoyalty.Text + "' AND DueDate > '" + ata.ToString("yyyy-MM-dd") + "'";
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
                String queryString = "SELECT CateringCollectionFee  FROM CO.AirportFee WHERE Airports = " + Airport + " AND ClientCategory.Name = '" + txtRoyalty.Text + "' AND DueDate > '" + ata.ToString("yyyy-MM-dd") + "'";
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
        private int GetArrivalFuelAirport()
        {
            try
            {
                int airport = 0;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT CustomFields.CO.Airports FROM Incident WHERE Id =  " + lblIdIncident.Text;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        airport = String.IsNullOrEmpty(data) ? 0 : int.Parse(data);
                    }
                }
                return airport;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetArrivalFuelAirport: " + ex.Message + "Det:" + ex.StackTrace);
                return 0;
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
                String queryString = "SELECT CollectionDeduction FROM CO.AirportFee WHERE Airports = " + Airport + " AND ClientCategory.Name = '" + txtRoyalty.Text + "' AND DueDate > '" + ata.ToString("yyyy-MM-dd") + "'";
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
        public static bool IsBetween(DateTime input, DateTime date1, DateTime date2)
        {
            return (input > date1 && input < date2);
        }
        private string GetMainHourFBOFCC(string ata, string atd)
        {
            try
            {
                DateTime ATA = DateTime.Parse(ata);
                DateTime ATD = DateTime.Parse(atd);

                string hour = "EXTRAORDINARIO";
                string hourata = "EXTRAORDINARIO";
                string houratd = "EXTRAORDINARIO";

                if (WHoursList.Count > 0)
                {
                    foreach (WHours w in WHoursList)
                    {
                        if (IsBetween(ATA, w.ATAOpens, w.ATACloses) && w.Type == "CRITICO")
                        {
                            hourata = "CRITICO";
                        }
                        if (IsBetween(ATA, w.ATAOpens, w.ATACloses) && w.Type == "NORMAL")
                        {
                            hourata = "NORMAL";
                        }
                        if (IsBetween(ATD, w.ATDOpens, w.ATDCloses) && w.Type == "CRITICO")
                        {
                            houratd = "CRITICO";
                        }
                        if (IsBetween(ATD, w.ATDOpens, w.ATDCloses) && w.Type == "NORMAL")
                        {
                            houratd = "NORMAL";
                        }
                        if (hourata == houratd)
                        {
                            hour = hourata;
                        }
                        else if (hourata == "EXTRAORDINARIO" || houratd == "EXTRAORDINARIO")
                        {
                            hour = "EXTRAORDINARIO";
                        }
                        else if (hourata == "CRITICO" || houratd == "CRITICO")
                        {
                            hour = "CRITICO";
                        }
                        else
                        {
                            hour = "NORMAL";
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
                if (String.IsNullOrEmpty(component.CobroParticipacionNj))
                {
                    body += "\"CobroParticipacionNj\":null,";
                }
                else
                {
                    body += "\"CobroParticipacionNj\":\"" + component.CobroParticipacionNj + "\",";
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
            /*try
            {
                if (!String.IsNullOrEmpty(txtItemNumber.Text))
                {
                    applyExchangeRate(cboCurrency.Text);
                }
            }
            catch (Exception ex)
            {
                global.LogMessage("Error en txtCost.Text:" + ex.Message + "Det:" + ex.StackTrace);
            }*/
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

            if (rate == 1)
            {
                rate = 18.78;
            }

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
                String queryString = "SELECT ATA FROM Co.Itinerary WHERE ID =" + idItinerary + " ";
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
        public string GetMTOWPrice()
        {
            try
            {
                double mtow = Convert.ToDouble(GetMTOW(txtICAOD.Text));
                double cost = Convert.ToDouble(txtCost.Text);

                double price = (mtow * cost);

                txtQty.Text = mtow.ToString();
                return Math.Round((price), 4).ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetMTOWPrice: " + ex.Message + "Det:" + ex.StackTrace);
                return "0";
            }
        }
        private bool isFBOPrice()
        {
            try
            {
                bool fbo = false;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT SalesMethod.LookupName FROM CO.Itinerary WHERE ID =" + txtItinerary.Text;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        if (data.ToString() == "FBO")
                        {
                            fbo = true;
                        }
                    }
                }
                return fbo;
            }
            catch (Exception ex)
            {
                MessageBox.Show("isFBOPrice" + ex.Message + "Det:" + ex.StackTrace);
                return false;
            }
        }
        private bool isCargo()
        {
            try
            {
                bool cargo = false;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT Customfields.c.flight_type.Name FROM Incident WHERE Id = " + lblIdIncident.Text;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        if (data.ToString() == "CARGO")
                        {
                            cargo = true;
                        }
                    }
                }
                return cargo;
            }
            catch (Exception ex)
            {
                MessageBox.Show("isCargo" + ex.Message + "Det:" + ex.StackTrace);
                return false;
            }
        }
        private string GetMTOW(string idICAO)
        {
            try
            {
                string weight = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT Weight FROM CO.AircraftType WHERE ICAODesignator= '" + idICAO + "'";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        weight = data;
                    }
                }
                return String.IsNullOrEmpty(weight) ? "" : weight;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetMTOW" + ex.Message + "Det:" + ex.StackTrace);
                return "";
            }
        }
        private double gethSGroup(string idICAO)
        {
            try
            {
                string hSG = "H1";
                double per = 0;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT HangarSpaceGroup.LookupName FROM CO.AircraftType WHERE ICAODesignator= '" + idICAO + "'";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        hSG = data;
                    }
                }
                per = GetHSGPercentage(hSG);
                return Convert.ToDouble(per);
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetM2" + ex.Message + "Det:" + ex.StackTrace);
                return 0;
            }
        }
        private double GetHSGPercentage(string Utilidad)
        {
            try
            {
                double per = 0;
                var client = new RestClient("https://iccs.bigmachines.com/");
                string User = Encoding.UTF8.GetString(Convert.FromBase64String("aW1wbGVtZW50YWRvcg=="));
                string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJneSoyMDE4"));
                client.Authenticator = new HttpBasicAuthenticator("servicios", "Sinergy*2018");

                //string definicion = "?totalResults=false&q={str_item_number:'" + dataGridServicios.Rows[e.RowIndex].Cells[1].FormattedValue.ToString().Trim() + "',str_icao_iata_code:'" + airtport + "'}";
                string definicion = "?q={str_tipo:'HANGAR_SPACE',str_categoria:'" + Utilidad + "'} ";
                var request = new RestRequest("rest/v6/customCategorias/" + definicion, Method.GET);
                IRestResponse response = client.Execute(request);
                ClaseParaCategorias.RootObject rootObjectCat = JsonConvert.DeserializeObject<ClaseParaCategorias.RootObject>(response.Content);
                if (rootObjectCat.items.Count > 0)
                {
                    per = rootObjectCat.items[0].flo_value;
                }
                else
                {
                    per = 0;
                }

                return per;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException.ToString());
                return 0;
            }
        }
        private string getm2(string idICAO)
        {
            try
            {
                string m2 = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT Large * Wingspan FROM CO.AircraftType WHERE ICAODesignator= '" + idICAO + "'";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        m2 = data;
                    }
                }
                return String.IsNullOrEmpty(m2) ? "" : m2;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetM2" + ex.Message + "Det:" + ex.StackTrace);
                return "";
            }
        }
        private string GetSRCreationDate(int serviceR)
        {
            try
            {
                string date = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT CreatedTime From INCIDENT WHERE ID =" + serviceR;
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        date = data;
                    }
                }
                return String.IsNullOrEmpty(date) ? "" : date;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetSRCreationDate" + ex.Message + "Det:" + ex.StackTrace);
                return "";
            }
        }
        private double GetHSCost()
        {
            double flo_superficiem2 = 0;
            double flo_rentamensual = 0;
            double flo_gastos = 0;
            double finalCost = 0;
            try
            {
                string definicion = "?q={str_icaoiatacode:'" + txtAirport.Text + "'}";
                var client = new RestClient("https://iccs.bigmachines.com/");
                //string User = Encoding.UTF8.GetString(Convert.FromBase64String("aW1wbGVtZW50YWRvcg=="));
                //string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJIwMTgu"));
                client.Authenticator = new HttpBasicAuthenticator("servicios", "Sinergy*2018");

                global.LogMessage("GETFinanzasFBO:" + definicion + "SRType:" + lblSrType.Text);
                var request = new RestRequest("rest/v6/customFinanzas_FBO/" + definicion, Method.GET);
                IRestResponse response = client.Execute(request);
                ClaseFinanzas.RootObject rootObjectFinanzas = JsonConvert.DeserializeObject<ClaseFinanzas.RootObject>(response.Content);
                if (rootObjectFinanzas != null && rootObjectFinanzas.items.Count > 0)
                {
                    foreach (ClaseFinanzas.Item item in rootObjectFinanzas.items)
                    {
                        DateTime inicio = DateTime.Parse(item.str_startdate + " " + "00:00");
                        DateTime fin = DateTime.Parse(item.str_enddate + " " + "23:59");
                        DateTime fecha = DateTime.Today;

                        if (fecha.CompareTo(inicio) >= 0 && fecha.CompareTo(fin) <= 0)
                        {
                            flo_superficiem2 = Convert.ToDouble(item.flo_superficiem2);
                            flo_rentamensual = Convert.ToDouble(item.flo_rentamensual);
                            flo_gastos = (Convert.ToDouble(item.flo_depreciacion)
                                + Convert.ToDouble(item.flo_electricidad)
                                + Convert.ToDouble(item.flo_nomina)
                                + Convert.ToDouble(item.flo_seguros)
                                + Convert.ToDouble(item.flo_limpieza)
                                + Convert.ToDouble(item.flo_equipooperacion)
                                + Convert.ToDouble(item.flo_seguridad)
                                + Convert.ToDouble(item.flo_mantenimiento));
                        }
                    }
                }
                finalCost = flo_rentamensual / flo_superficiem2;
                finalCost = finalCost + flo_gastos;

                return Math.Round(finalCost, 4);
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetHSCost" + ex.Message + "Det:" + ex.StackTrace);
                return 0;
            }
        }
        private void txtUOM_TextChanged(object sender, EventArgs e)
        {

        }
        private void DoubleScreen_Load(object sender, EventArgs e)
        {

        }
        private void txtCost_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                try
                {
                    double pricefinal = 0;
                    if (txtItemNumber.Text == "SOMFEAP325" || txtItemNumber.Text == "SOMFEAP260" || lblSrType.Text == "SENEAM")
                    {
                        pricefinal = Convert.ToDouble(txtPrice.Text);
                    }
                    if (lblSrType.Text == "CATERING")
                    {
                        // cboCurrency.Text = "USD";
                        double rate = getExchangeRate(DateTime.Parse(txtCateringDDate.Text));

                        if (txtUtilidad.Text == "A")
                        {
                            pricefinal = double.Parse(txtCost.Text);

                            pricefinal = pricefinal / rate;
                            pricefinal = Math.Round(pricefinal, 4);
                        }
                        else
                        {
                            if (double.Parse(txtCost.Text) > 0)
                            {
                                double precio = Convert.ToDouble(txtCost.Text);
                                double utilidad = GetUtilidadPercentage(txtUtilidad.Text) / 100;

                                precio = precio + (precio * utilidad);
                                pricefinal = Math.Round(precio, 4);

                                if (lblCurrencyPrice.Text == "USD")
                                {
                                    precio = Convert.ToDouble(pricefinal);
                                    precio = precio / rate;
                                    pricefinal = Math.Round(precio, 4);
                                }
                            }
                        }
                    }
                    if (lblSrType.Text == "FBO")
                    {
                        if (IsFloatValue(txtCost.Text) && txtItemNumber.Text != "ASFIEAP357")
                        {
                            pricefinal = Math.Round((Convert.ToDouble(txtCost.Text) * 1.30), 4);
                        }
                    }
                    if (lblSrType.Text == "FUEL")
                    {
                        if (txtItemNumber.Text == "AGASIAS0270" || txtItemNumber.Text == "JFUEIAS0269" || txtItemNumber.Text == "AGASIAS0011" || txtItemNumber.Text == "JFUEIAS0010")
                        {
                            double b;
                            if (double.TryParse(txtCost.Text, out b))
                            {
                                pricefinal = GetFuelPrice();
                            }
                        }
                    }
                    if (lblSrType.Text == "FCC")
                    {
                        if (isComponent())
                        {
                            pricefinal = 0;
                        }
                        if (IsFloatValue(txtCost.Text)) // && GetPrices() == 0)
                        {
                            // cboCurrency.Text = "USD";
                            DateTime date = DateTime.Parse(txtATA.Text);
                            pricefinal = Math.Round(((Convert.ToDouble(txtCost.Text) + (Convert.ToDouble(txtCost.Text) * GetUtilidadPercentage(txtUtilidad.Text) / 100)) / getExchangeRateSemanal(date)), 4, MidpointRounding.AwayFromZero);
                        }
                        if (txtItemNumber.Text == "ASECSAS0073")
                        {
                            if (IsNumber(txtCost.Text))
                            {
                                double minutehour = GetMinutesLeg();
                                pricefinal = Math.Round((Convert.ToDouble(txtCost.Text) * minutehour), 4);
                            }
                        }
                    }
                    txtPrice.Text = pricefinal.ToString();
                    /*
                     * if (!string.IsNullOrEmpty(txtQty.Text) && IsFloatValue(txtQty.Text))
                    {
                        txtPrice.Text = (pricefinal * double.Parse(txtQty.Text)).ToString();
                        if (lblSrType.Text == "FBO")
                        {
                            txtPrice.Text = ((double.Parse(txtCost.Text) * double.Parse(txtQty.Text)) * 1.30).ToString();
                        }
                    }*/
                }
                catch (Exception ex)
                {
                    global.LogMessage("Error en txtCost.Text:" + ex.Message + "Det:" + ex.StackTrace);
                }
            }
        }
    }
    public class ItiPrices
    {
        public int Itinerarie { get; set; }
        public double Limit { get; set; }
    }
}