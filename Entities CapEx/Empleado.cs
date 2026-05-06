using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaVeguita.Entities
{
    public class Empleado
    {
        // Identificador único (NUMBER -> int)
        public int IdEmpleado { get; set; }

        // Nombres y Apellidos (VARCHAR2 -> string)
        public string PnombreEmp { get; set; }
        public string SnombreEmp { get; set; }
        public string Appaterno { get; set; }
        public string Apmaterno { get; set; }

        // Fechas (DATE -> DateTime)
        public DateTime FecNacEmp { get; set; }
        public DateTime FechaIngreso { get; set; }

        // Contacto
        public string CorreoEmp { get; set; }
        public long TelefonoEmp { get; set; } // NUMBER de teléfono mejor como long por la cantidad de dígitos

        // Relaciones (FKs)
        public int IdRolUsuario { get; set; }
        public int IdDireccion { get; set; }
    }
}
