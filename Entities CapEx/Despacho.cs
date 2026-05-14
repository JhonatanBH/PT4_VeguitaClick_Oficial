using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaVeguita.Entities
{
    public class Despacho
    {
        // ID_DESPACHO NUMBER
        public int IdDespacho { get; set; }

        // KM_OBTENIDOS NUMBER
        public decimal KmObtenidos { get; set; }

        // HORA_SAL y HORA_ENTR (En tu SQL son NUMBER, pero representaremos decimal/int según tu lógica de hora)
        public decimal HoraSal { get; set; }
        public decimal HoraEntr { get; set; }

        public string EstadoPedido { get; set; } // VARCHAR2(20)

        // Relaciones (FKs)
        public int IdVenta { get; set; }
        public int IdTransporte { get; set; }
        public int IdEmpleado { get; set; }


        public decimal PesoTotalKg { get; set; }

        public double LatitudDestino { get; set; }
        public double LongitudDestino { get; set; }

    }
}
