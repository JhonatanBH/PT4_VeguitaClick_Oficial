using Microsoft.AspNetCore.Mvc;
using LaVeguita.DAL;
using LaVeguita.Entities;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace LaVeguita.Web.Controllers
{
    public class TransporteController : Controller
    {
        // Usamos ambas DALs para separar la gestión administrativa de la operativa
        private readonly DespachoDAL _despachoDal = new DespachoDAL();
        private readonly TransporteDAL _transporteDal = new TransporteDAL();

        // 1. VISTA DE SELECCIÓN (Paso previo obligatorio para el Rol 7)
        public IActionResult SeleccionarVehiculo()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 7)) return RedirectToAction("Login", "Acceso");

            // Listamos móviles que no estén en mantención según el Caso 4 [cite: 46]
            var disponibles = _transporteDal.ListarVehiculosDisponibles();
            return View(disponibles);
        }

        // 2. PROCESO DE INICIO DE TURNO (Captura datos del móvil en Sesión)
        [HttpPost]
        public IActionResult IniciarTurno(int idTransporte, string tipoVehiculo, decimal capMax)
        {
            // Guardamos la herramienta de trabajo para aplicar filtros de carga 
            HttpContext.Session.SetInt32("IdVehiculoAsignado", idTransporte);
            HttpContext.Session.SetString("TipoVehiculo", tipoVehiculo.ToUpper());
            HttpContext.Session.SetString("CapacidadMaxima", capMax.ToString());

            return RedirectToAction("MisDespachos");
        }

        // 3. HOJA DE RUTA (Tu función original con seguridad reforzada)
        public IActionResult MisDespachos()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 7)) return RedirectToAction("Login", "Acceso");

            // Si el transportista no ha elegido vehículo, lo devolvemos a la selección
            string tipoVehiculo = HttpContext.Session.GetString("TipoVehiculo");
            if (string.IsNullOrEmpty(tipoVehiculo) && rol == 7)
            {
                return RedirectToAction("SeleccionarVehiculo");
            }

            // Usamos el método que ya tiene los filtros de Peso y Distancia (15km) 
            var despachos = _transporteDal.ListarMisDespachos(tipoVehiculo ?? "BICICLETA");

            // Lógica de Tiempos basada en velocidades del Caso 4 (15km/h bici, 10km/h triciclo) [cite: 34, 48]
            ViewBag.Velocidad = (tipoVehiculo?.ToUpper() == "BICICLETA") ? 15 : 10;
            ViewBag.TipoVehiculo = tipoVehiculo;

            return View(despachos);
        }

        // 4. CIERRE DE ENTREGA (Tu función original)
        public IActionResult MarcarEntregado(int id)
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 7)) return RedirectToAction("Login", "Acceso");

            _despachoDal.ActualizarEstadoEntregado(id);

            TempData["Exito"] = "Pedido marcado como entregado correctamente.";
            return RedirectToAction("MisDespachos");
        }
    }
}