using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaVeguita.Entities
{
    public class Venta
    {
        // En tu SQL es ID_VENTA
        public int IdVenta { get; set; }

        public DateTime FechaVenta { get; set; }

        // Usamos decimal para dinero (TOTAL en Oracle)
        public decimal Total { get; set; }

        // Relaciones (FKs)
        public int IdCliente { get; set; }
        public int IdUsuario { get; set; }
        public int IdSolicitud { get; set; }
    }
}
