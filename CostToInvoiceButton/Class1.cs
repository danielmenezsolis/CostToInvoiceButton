using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ClaseRecargos
{
    public class Link
    {
        public string rel { get; set; }
        public string href { get; set; }
    }

    public class Item
    {
        public int id { get; set; }
        public double flo_superficiem2 { get; set; }
        public string str_startdate { get; set; }
        public double flo_depreciacion { get; set; }
        public double flo_rentamensual { get; set; }
        public string str_icaoiatacode { get; set; }
        public double flo_electricidad { get; set; }
        public double flo_nomina { get; set; }
        public double flo_seguros { get; set; }
        public double flo_limpieza { get; set; }
        public double Flo_equipooperacion { get; set; }
        public double flo_seguridad { get; set; }
        public string str_enddate { get; set; }
        public double flo_mantenimiento { get; set; }
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