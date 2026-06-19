using System;

namespace LaVeguita.Entities
{
    public class Vehiculo
    {
        public int IdTransporte { get; set; }
        public string TipoMovil { get; set; }
        public string Serial { get; set; }
        public int CapMaxKg { get; set; }
        public string Estado { get; set; }
        public int KmTotal { get; set; }
    }
}