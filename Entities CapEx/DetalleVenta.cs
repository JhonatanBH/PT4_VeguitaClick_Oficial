using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaVeguita.Entities
{
    public class DetalleVenta
    {
        // En tu SQL es ID_DETALLE_VEN
        public int IdDetalleVen { get; set; }

        public int Cantidad { get; set; }

        // Usamos decimal para el precio unitario del momento
        public decimal Precio { get; set; }

        // Relaciones (FKs)
        public int IdVenta { get; set; }
        public int IdProducto { get; set; }
    }
}
