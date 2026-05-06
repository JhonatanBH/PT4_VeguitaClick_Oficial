using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaVeguita.Entities
{
    public class Comuna
    {
        // En tu SQL es ID_COMUNA NUMBER(2)
        public int IdComuna { get; set; }

        public string Nombre { get; set; }

        // Relación con la tabla REGION
        public int IdRegion { get; set; }
    }
}
