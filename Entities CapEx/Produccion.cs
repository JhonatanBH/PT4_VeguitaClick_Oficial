using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaVeguita.Entities
{
    public class Produccion
    {
        // ID_PRODUCCION NUMBER
        public int IdProduccion { get; set; }

        // Cantidades (Entrada vs Resultados)
        public int CantEntrada { get; set; }
        public int CantPrimCalidad { get; set; }
        public int CantSegCalidad { get; set; }

        // Fecha del proceso
        public DateTime FechaProceso { get; set; }

        // Relaciones (FKs)
        public int IdProducto { get; set; }
        public int IdEmpleado { get; set; } // Quién supervisó el proceso
    }
}
