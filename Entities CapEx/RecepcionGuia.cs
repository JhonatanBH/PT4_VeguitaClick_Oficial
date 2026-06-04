
using System;
using System.Collections.Generic;

namespace LaVeguita.Entities
{
    public class RecepcionGuia
    {
        public int IdRecepcion { get; set; }
        public string NumGuiaFisica { get; set; }
        public int IdProveedor { get; set; }
        public DateTime FechaIngreso { get; set; }

        // Propiedad de navegacion para mostrar el nombre del agricultor en la tabla
        public string NomProveedor { get; set; }

        // El corazon de tu diseño: La lista que guardara todas las verduras de este camion
        public List<DetalleLote> ItemsCargamento { get; set; } = new List<DetalleLote>();
    }
}