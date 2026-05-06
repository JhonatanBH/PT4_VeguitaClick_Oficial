using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaVeguita.Entities
{
    public class Producto
    {
        public int? IdProducto { get; set; }
        public string NomProducto { get; set; }
        public string Descripcion { get; set; }
        public decimal PrecioUnd { get; set; } // Usamos decimal para dinero
        public int StockActual { get; set; }
        public int IdProveedor { get; set; }
        public int StockMin { get; set; }
        public int StockMax { get; set; }
        public decimal PesoUnitEstimado { get; set; }
        public string EsOrganico { get; set; } // CHAR(1) de Oracle
        public int IdTipoProducto { get; set; }
        public int IdCalidad { get; set; }
        public string Packing { get; set; }


        public string NomTipo { get; set; }
    }
}
