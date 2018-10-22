using System.Xml.Serialization;
using System.Collections.Generic;
using System;

namespace CostToInvoiceButton
{

    //RATES
    [XmlRoot(ElementName = "G_1_RATES")]
    public class G_1_RATES
    {
        [XmlElement(ElementName = "CONVERSION_RATE")]
        public string CONVERSION_RATE { get; set; }
        [XmlElement(ElementName = "CONVERSION_DATE")]
        public string CONVERSION_DATE { get; set; }
    }

    [XmlRoot(ElementName = "G_N_RATES")]
    public class G_N_RATES
    {
        [XmlElement(ElementName = "USER_CONVERSION_TYPE")]
        public string USER_CONVERSION_TYPE { get; set; }
        [XmlElement(ElementName = "G_1_RATES")]
        public G_1_RATES G_1_RATES { get; set; }
    }

    [XmlRoot(ElementName = "DATA_DS_RATES")]
    public class DATA_DS_RATES
    {
        [XmlElement(ElementName = "P_EXCHANGE_DATE")]
        public string P_EXCHANGE_DATE { get; set; }
        [XmlElement(ElementName = "G_N_RATES")]
        public G_N_RATES G_N_RATES { get; set; }
    }


    //INPC

    [XmlRoot(ElementName = "G_1_INPC")]
    public class G_1_INPC
    {
        [XmlElement(ElementName = "PRICE_INDEX_VALUE_ID")]
        public string PRICE_INDEX_VALUE_ID { get; set; }
        [XmlElement(ElementName = "PERIOD_NAME")]
        public string PERIOD_NAME { get; set; }
        [XmlElement(ElementName = "PRICE_INDEX_VALUE")]
        public string PRICE_INDEX_VALUE { get; set; }
    }

    [XmlRoot(ElementName = "G_N_INPC")]
    public class G_N_INPC
    {
        [XmlElement(ElementName = "PRICE_INDEX_ID")]
        public string PRICE_INDEX_ID { get; set; }
        [XmlElement(ElementName = "PRICE_INDEX_NAME")]
        public string PRICE_INDEX_NAME { get; set; }
        [XmlElement(ElementName = "G_1_INPC")]
        public List<G_1_INPC> G_1_INPC { get; set; }
    }

    [XmlRoot(ElementName = "DATA_DS_INPC")]
    public class DATA_DS_INPC
    {
        [XmlElement(ElementName = "P_PERIODO")]
        public string P_PERIODO { get; set; }
        [XmlElement(ElementName = "P_PERIOD_START")]
        public string P_PERIOD_START { get; set; }
        [XmlElement(ElementName = "P_PERIOD_END")]
        public string P_PERIOD_END { get; set; }
        [XmlElement(ElementName = "G_N_INPC")]
        public G_N_INPC G_N_INPC { get; set; }
    }

    //--de productos asociados a un proveedor

    [XmlRoot(ElementName = "G_1_ITEMSUP")]
    public class G_1_ITEMSUP
    {
        [XmlElement(ElementName = "ASL_ID")]
        public string ASL_ID { get; set; }
        [XmlElement(ElementName = "VENDOR_ID")]
        public string VENDOR_ID { get; set; }
        [XmlElement(ElementName = "PARTY_ID")]
        public string PARTY_ID { get; set; }
        [XmlElement(ElementName = "PARTY_NUMBER")]
        public string PARTY_NUMBER { get; set; }
        [XmlElement(ElementName = "PARTY_NAME")]
        public string PARTY_NAME { get; set; }
        [XmlElement(ElementName = "INVENTORY_ITEM_ID")]
        public string INVENTORY_ITEM_ID { get; set; }
        [XmlElement(ElementName = "ITEM_NUMBER")]
        public string ITEM_NUMBER { get; set; }
        [XmlElement(ElementName = "DESCRIPTION")]
        public string DESCRIPTION { get; set; }
        [XmlElement(ElementName = "PRIMARY_UOM_CODE")]
        public string PRIMARY_UOM_CODE { get; set; }
    }

    [XmlRoot(ElementName = "G_N_ITEMSUP")]
    public class G_N_ITEMSUP
    {
        [XmlElement(ElementName = "ORGANIZATION_CODE")]
        public string ORGANIZATION_CODE { get; set; }
        [XmlElement(ElementName = "G_1_ITEMSUP")]
        public List<G_1_ITEMSUP> G_1_ITEMSUP { get; set; }
    }

    [XmlRoot(ElementName = "DATA_DS_ITEMSUP")]
    public class DATA_DS_ITEMSUP
    {
        [XmlElement(ElementName = "G_N_ITEMSUP")]
        public List<G_N_ITEMSUP> G_N_ITEMSUP { get; set; }
    }
    //SUPPLIER
    [XmlRoot(ElementName = "G_N_SUPPLIER")]
    public class G_N_SUPPLIER
    {
        [XmlElement(ElementName = "SEQUENCE")]
        public string SEQUENCE { get; set; }
        [XmlElement(ElementName = "SUPPLIER")]
        public string SUPPLIER { get; set; }
        [XmlElement(ElementName = "SITES")]
        public string SITES { get; set; }
        [XmlElement(ElementName = "PAYMENT_TERMS")]
        public string PAYMENT_TERMS { get; set; }
        [XmlElement(ElementName = "ID")]
        public string ID { get; set; }
    }

    [XmlRoot(ElementName = "DATA_DS_SUPPLIER")]
    public class DATA_DS_SUPPLIER
    {
        [XmlElement(ElementName = "G_N_SUPPLIER")]
        public List<G_N_SUPPLIER> G_N_SUPPLIER { get; set; }
    }

    public class Sup
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }

    public class WHours
    {
        public DateTime Opens { get; set; }
        public DateTime Closes { get; set; }
        public string Type { get; set; }
    }
}