using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaVeguita.Entities
{
    public class Transporte
    {
        // ID_TRANSPORTE NUMBER
        public int IdTransporte { get; set; }

        public string TipoMovil { get; set; }

        public string Serial { get; set; } // Patente o número de serie

        // CAP_MAX_KG NUMBER
        public decimal CapMaxKg { get; set; }

        public string Estado { get; set; } // Ej: Disponible, En Ruta, Taller

        // KM_TOTAL NUMBER
        public decimal KmTotal { get; set; }
    }
}
