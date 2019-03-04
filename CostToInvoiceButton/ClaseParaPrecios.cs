using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CostToInvoiceButton
{
    class ClaseParaPrecios
    {
        public class Link
        {
            public string rel { get; set; }
            public string href { get; set; }
        }

        public class Item
        {
            public int bol_int_flight_cargo { get; set; }
            public string str_icao_iata_code { get; set; }
            public string str_oum_code { get; set; }
            public string str_ft_arrival { get; set; }
            public string str_item_name { get; set; }
            public double flo_amount { get; set; }
            public int id { get; set; }
            public string str_client_category { get; set; }
            public int bol_int_at { get; set; }
            public int ind_id_iccs { get; set; }
            public int bol_int_fbo { get; set; }
            public string str_ft_depart { get; set; }
            public string str_item_number { get; set; }
            public string str_aircraft_group { get; set; }
            public object str_vendor_name { get; set; }
            public int int_vendor_id { get; set; }
            public string str_currency_code { get; set; }
            public string str_end_date { get; set; }
            public string str_schedule_type { get; set; }
            public string str_start_date { get; set; }
            public string str_aircraft_type { get; set; }
            public List<Link> links { get; set; }
        }

        public class Link2
        {
            public string rel { get; set; }
            public string href { get; set; }
        }

        public class RootObject
        {
            public bool hasMore { get; set; }
            public List<Item> items { get; set; }
            public List<Link2> links { get; set; }
        }
    }
}