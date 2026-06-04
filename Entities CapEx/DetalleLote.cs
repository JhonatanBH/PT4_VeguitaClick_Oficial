namespace LaVeguita.Entities
{
    public class DetalleLote
    {
        public int IdLote { get; set; }
        public int IdRecepcion { get; set; }
        public int IdProducto { get; set; }
        public decimal PesoRealLocal { get; set; }
        public decimal PesoDeclaradoGd { get; set; }
        public string DescripcionLote { get; set; }
        public string Estado { get; set; }

        // Propiedades de navegacion para pintar textos legibles en pantalla
        public string NomProducto { get; set; }
        public decimal PesoUnitEstimado { get; set; }
    }
}