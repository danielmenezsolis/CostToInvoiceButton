using MoreLinq;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;


namespace CostToInvoiceButton
{
    public partial class DoubleScreen : Form
    {
        public DoubleScreen()
        {
            InitializeComponent();
            dataGridInvoice.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }
        private void dataGridServicios_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            ClearTxtBoxes();
            dataGridSuppliers.DataSource = null;
            try
            {
                if (e.RowIndex != -1)
                {
                    txtItem.Text = dataGridServicios.Rows[e.RowIndex].Cells[0].FormattedValue.ToString().Trim();
                    txtItemNumber.Text = dataGridServicios.Rows[e.RowIndex].Cells[1].FormattedValue.ToString().Trim();
                    txtIdService.Text = dataGridServicios.Rows[e.RowIndex].Cells[3].FormattedValue.ToString().Trim();
                    txtSupplierName.Text = dataGridServicios.Rows[e.RowIndex].Cells[4].FormattedValue.ToString().Trim();
                    txtCost.Text = dataGridServicios.Rows[e.RowIndex].Cells[5].FormattedValue.ToString().Trim();
                    txtPrice.Text = dataGridServicios.Rows[e.RowIndex].Cells[6].FormattedValue.ToString().Trim();
                    txtInvoice.Text = dataGridServicios.Rows[e.RowIndex].Cells[7].FormattedValue.ToString().Trim();

                    string airtport = dataGridServicios.Rows[e.RowIndex].Cells[2].FormattedValue.ToString().Trim();
                    airtport = airtport.Replace('_', '-').Trim();

                    /*
                    var client = new RestClient("https://iccs.bigmachines.com/");
                    client.Authenticator = new HttpBasicAuthenticator("implementador", "Sinergy*2018");
                    string definicion = "?totalResults=false&q={str_item_number:'" + dataGridServicios.Rows[e.RowIndex].Cells[1].FormattedValue.ToString().Trim() + "',str_icao_iata_code:'" + airtport + "'}";
                    var request = new RestRequest("rest/v6/customCostos/" + definicion, Method.GET);
                    IRestResponse response = client.Execute(request);
                    RootObject rootObject = JsonConvert.DeserializeObject<RootObject>(response.Content);
                    if (rootObject.items.Count > 0)
                    {
                        List<Item> list = rootObject.items.DistinctBy(p => p.str_vendor_name).ToList();
                        var query = from i in list select new { VendorId = i.int_vendor_id, VendorName = i.str_vendor_name, UOMCode = i.str_uom_code, CurrencyCode = i.str_currency_code, Cost = i.flo_cost };
                        dataGridSuppliers.DataSource = query.ToList();
                        dataGridSuppliers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
                        dataGridSuppliers.Columns[1].Width = 900;
                    }
                    else
                    {

                    }
                    */
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
            if (ValidateData())
            {
                if (dataGridInvoice.RowCount <= dataGridServicios.RowCount - 1)
                {
                    if (ValidateRows())
                    {
                        double amount = (Convert.ToDouble(txtPrice.Text) * Convert.ToInt32(txtQty.Text));
                        dataGridInvoice.Rows.Add(txtInvoice.Text, txtItem.Text, txtSupplierName.Text, txtQty.Text, txtCost.Text, txtPrice.Text, amount, txtIdService.Text);
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
                if (Convert.ToInt32(txtInvoice.Text) >= 6 || Convert.ToInt32(txtInvoice.Text) == 0)
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


        private void txtQty_TextChanged(object sender, EventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {
                
            }
        }

        private void txtPrice_TextChanged(object sender, EventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {

            }
        }

        public bool IsFloatValue(string text)
        {
            Regex regex = new Regex(@"^\d*\.?\d{1,2}$");
            return regex.IsMatch(text);
        }
        public bool IsNumber(string text)
        {
            return text.All(char.IsDigit);
        }

        private void SetTotal()
        {
            txtAmount.Text = (Convert.ToDouble(txtPrice.Text) * Convert.ToInt32(txtQty.Text)).ToString();
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


    }

}
