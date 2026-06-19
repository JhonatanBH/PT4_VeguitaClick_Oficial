using System;

namespace LaVeguita.Entities
{
    public class Mantencion
    {
        public int IdMantencion { get; set; }
        public int IdTransporte { get; set; }
        public DateTime FechaMantencion { get; set; }
        public string DetalleTecnico { get; set; }
        public int CostoMantencion { get; set; }
    }
}