using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaVeguita.Entities
{
    public class CarritoItem
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public decimal PesoUnitario { get; set; }

        // Propiedades calculadas
        public decimal Subtotal => Precio * Cantidad;
        public decimal PesoSubtotal => PesoUnitario * Cantidad;
    }
}
