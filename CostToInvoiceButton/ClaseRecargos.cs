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
        public double tasa_recargo { get; set; }
        public int id_iccs { get; set; }
        public string inicio_tasa { get; set; }
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