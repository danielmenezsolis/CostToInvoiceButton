using System.Xml.Serialization;
using System.Collections.Generic;

namespace CostToInvoiceButton
{
    [XmlRoot(ElementName = "G_1")]
    public class G_1
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
    [XmlRoot(ElementName = "G_5")]
    public class G_5
    {
        [XmlElement(ElementName = "ORGANIZATION_CODE")]
        public string ORGANIZATION_CODE { get; set; }
        [XmlElement(ElementName = "G_1")]
        public G_1 G_1 { get; set; }
    }
    [XmlRoot(ElementName = "DATA_DS")]
    public class DATA_DS
    {
        [XmlElement(ElementName = "G_5")]
        public List<G_5> G_5 { get; set; }
    }
}