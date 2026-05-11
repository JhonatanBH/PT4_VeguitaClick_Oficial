using Microsoft.AspNetCore.Mvc;
using LaVeguita.DAL;
using LaVeguita.Entities;
using Microsoft.AspNetCore.Http;

namespace LaVeguita.Web.Controllers
{
    public class TransporteController : Controller
    {
        private readonly DespachoDAL _despachoDal = new DespachoDAL();

        // 1. Ver la lista de despachos asignados
        public IActionResult MisDespachos()
        {
            // Seguridad: Solo permitimos a Admin (1) y Empleado/Transportista (2)
            int? rol = HttpContext.Session.GetInt32("RolUsuario");

            if (rol == null || (rol != 1 && rol != 7))
            {
                return RedirectToAction("Login", "Acceso");
            }

            // Llamamos a la DAL que ya configuramos con tu tabla DESPACHO
            var despachos = _despachoDal.ListarDespachosPendientes();
            return View(despachos);
        }

        // 2. Acción para el botón de la vista
        // Este método recibe el ID, llama a la DAL para hacer el UPDATE en Oracle y vuelve a la lista
        public IActionResult MarcarEntregado(int id)
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 7)) return RedirectToAction("Login", "Acceso");

            // Ejecuta el UPDATE: ESTADO_PEDIDO = 'ENTREGADO' y HORA_ENTR = HHmm
            _despachoDal.ActualizarEstadoEntregado(id);

            TempData["Exito"] = "Pedido marcado como entregado correctamente.";
            return RedirectToAction("MisDespachos");
        }
    }
}