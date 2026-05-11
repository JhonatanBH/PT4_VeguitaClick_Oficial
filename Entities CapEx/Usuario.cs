using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaVeguita.Entities
{
    public class Usuario
    {
        // Identificador (NUMBER -> int)
        public int IdUsuario { get; set; }

        // Credenciales
        public string NombreUser { get; set; }
        public string Contrasena { get; set; } // VARCHAR2(50) para el hash

        // Contacto
        public long Fono { get; set; } // NUMBER(10) cabe perfecto en long
        public string CorreoUsu { get; set; }

        // Relaciones (FKs)
        public int IdRolUsuario { get; set; }
        public int IdDireccion { get; set; }

        public string NombreRol { get; set; }

        public DateTime? FechaRegistro { get; set; }
    }
}
