﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;


namespace CostToInvoiceButton
{
    public class Services
    {
        public string InvoiceReady { get; set; }
        public string ID { get; set; }
        public string ItemNumber { get; set; }
        public string Description { get; set; }
        public string Airport { get; set; }
        public string Supplier { get; set; }
        public string Quantity { get; set; }
        public string UnitCost { get; set; }
        public string CostCurrency { get; set; }
        public string TotalCost { get; set; }
        public string UnitPrice { get; set; }
        public string PriceCurrency { get; set; }
        public string TotalPrice { get; set; }
        public string InvoiceInternal { get; set; }
        public string Itinerary { get; set; }
        public string Pax { get; set; }
        public string Task { get; set; }
        public string Informative { get; set; }
        public string ParentPax { get; set; }
        public string Categorias { get; set; }
        public string FuelId { get; set; }
        public string CobroParticipacionNj { get; set; }
        public string ParticipacionCobro { get; set; }
        public string Site { get; set; }
        public string Tax { get; set; }
        public string Fee { get; set; }
    }
    public class ComponentChild
    {
        public string Airport { get; set; }
        public string CobroParticipacionNj { get; set; }
        public string ClasificacionPagos { get; set; }
        public string Componente { get; set; }
        public string Costo { get; set; }
        public string CuentaGasto { get; set; }
        public int Incident { get; set; }
        public string Informativo { get; set; }
        public string ItemDescription { get; set; }
        public string ItemNumber { get; set; }
        public int Itinerary { get; set; }
        public string Pagos { get; set; }
        public string Paquete { get; set; }
        public string ParticipacionCobro { get; set; }
        public string Precio { get; set; }
        public int ID { get; set; }
        public int ParentPaxId { get; set; }
        public int FuelId { get; set; }
        public string Categories { get; set; }
        public int ServiceParent { get; set; }
        public string MCreated { get; set; }
        public string Tax { get; set; }
    }
    public class Link
    {
        public string rel { get; set; }
        public string href { get; set; }
        public string mediaType { get; set; }
    }
    public class CreatedByAccount
    {
        public List<Link> links { get; set; }
    }
    public class Link2
    {
        public string rel { get; set; }
        public string href { get; set; }
        public string mediaType { get; set; }
    }
    public class UpdatedByAccount
    {
        public List<Link2> links { get; set; }
    }
    public class Link3
    {
        public string rel { get; set; }
        public string href { get; set; }
        public string mediaType { get; set; }
    }
    public class Incident
    {
        public List<Link3> links { get; set; }
    }
    public class Link4
    {
        public string rel { get; set; }
        public string href { get; set; }
        public string mediaType { get; set; }
    }
    public class Itinerary
    {
        public List<Link4> links { get; set; }
    }
    public class Link5
    {
        public string rel { get; set; }
        public string href { get; set; }
        public bool? templated { get; set; }
    }
    public class Notes
    {
        public List<Link5> links { get; set; }
    }
    public class Link6
    {
        public string rel { get; set; }
        public string href { get; set; }
        public string mediaType { get; set; }
    }
    public class RootObject
    {
        public int id { get; set; }
        public string lookupName { get; set; }
        public DateTime createdTime { get; set; }
        public DateTime updatedTime { get; set; }
        public CreatedByAccount CreatedByAccount { get; set; }
        public UpdatedByAccount UpdatedByAccount { get; set; }
        public string Airport { get; set; }
        public object CobroParticipacionNj { get; set; }
        public object ClasificacionPagos { get; set; }
        public string Componente { get; set; }
        public object Costo { get; set; }
        public object CuentaGasto { get; set; }
        public object ERPInvoice { get; set; }
        public object FacturaProveedor { get; set; }
        public object IDProveedor { get; set; }
        public Incident Incident { get; set; }
        public string Informativo { get; set; }
        public object InternalInvoice { get; set; }
        public string ItemDescription { get; set; }
        public string ItemNumber { get; set; }
        public Itinerary Itinerary { get; set; }
        public Notes Notes { get; set; }
        public object Pagos { get; set; }
        public string Paquete { get; set; }
        public string ParticipacionCobro { get; set; }
        public object Precio { get; set; }
        public object SupplierSite { get; set; }
        public object UUID { get; set; }
        public List<Link6> links { get; set; }
    }
}
