using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaVeguita.Entities
{
    public class Compost
    {
        // ID_COMPOST NUMBER
        public int IdCompost { get; set; }

        // PESO_ACUM_ACTUAL NUMBER
        public decimal PesoAcumActual { get; set; }

        // STOCK_SACOS_DISP NUMBER
        public int StockSacosDisp { get; set; }

        // PRECIO_SACO NUMBER
        public decimal PrecioSaco { get; set; }

        // FECHA_ULT_CARGA DATE
        public DateTime FechaUltCarga { get; set; }

        // Relaciones (FKs)
        public int IdProduccion { get; set; }
        public int IdProveedor { get; set; }
    }
}
