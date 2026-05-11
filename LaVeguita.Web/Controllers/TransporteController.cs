using Microsoft.AspNetCore.Mvc;
using LaVeguita.DAL;
using LaVeguita.Entities;
using Microsoft.AspNetCore.Http;

namespace LaVeguita.Web.Controllers
{
    public class TransporteController : Controller
    {
        private readonly DespachoDAL _despachoDal = new DespachoDAL();

        public IActionResult MisDespachos()
        {
            // Seguridad: Rol 7 es el Transportista
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 7))
            {
                return RedirectToAction("Login", "Acceso");
            }

            // --- Lógica de Filtrado por Vehículo ---
            // Obtenemos el tipo de vehículo de la sesión. 
            // (Asegúrate de que al hacer Login guardes "Bicicleta" o "Triciclo" en esta variable de sesión)
            string tipoVehiculo = HttpContext.Session.GetString("TipoVehiculo") ?? "BICICLETA";

            // Llamamos al nuevo método que creamos en el DAL
            var despachos = _despachoDal.ListarDespachosPorVehiculo(tipoVehiculo);

            // --- Lógica de Tiempos para la Vista ---
            // Pasamos la velocidad al ViewBag para que la vista pueda mostrar el cálculo si lo necesitas
            ViewBag.Velocidad = (tipoVehiculo.ToUpper() == "BICICLETA") ? 15 : 10;
            ViewBag.TipoVehiculo = tipoVehiculo;

            return View(despachos);
        }

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