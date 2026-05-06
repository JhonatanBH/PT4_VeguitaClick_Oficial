using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaVeguita.Entities
{
    public class Direccion
    {
        public int IdDireccion { get; set; }
        public string NombreDir { get; set; }
        public int NumeroDir { get; set; }
        public int IdComuna { get; set; }
    }
}
