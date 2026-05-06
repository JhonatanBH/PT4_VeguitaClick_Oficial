using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaVeguita.Entities
{
    public class Proveedor
    {
        // En tu SQL es ID_PROVEEDOR
        public int IdProveedor { get; set; }

        public string NomProveedor { get; set; }

        // Usamos long por si el número tiene muchos dígitos (FONO_PROVEEDOR)
        public long FonoProveedor { get; set; }

        public string CorreoProveedor { get; set; }

        public string RutProveedor { get; set; }

        // Relación con la tabla DIRECCION
        public int IdDireccion { get; set; }
    }
}
