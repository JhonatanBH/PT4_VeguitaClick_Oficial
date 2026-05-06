using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaVeguita.Entities
{
    public class Rol
    {
        // Estos nombres deben ser iguales a los de tu tabla en Oracle
        public int IdRolUsuario { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
    }
}
