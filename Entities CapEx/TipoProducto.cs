using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaVeguita.Entities
{
    public class TipoProducto
    {
        // En tu SQL es ID_TIPO_PRODUCTO NUMBER(6)
        public int IdTipoProducto { get; set; }

        public string NomTipo { get; set; }
    }
}
