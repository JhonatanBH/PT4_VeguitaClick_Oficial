using Microsoft.AspNetCore.Mvc;
using LaVeguita.DAL;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace LaVeguita.Web.Controllers
{
    public class LogisticaController : Controller
    {
        private readonly DespachoDAL _despachoDal = new DespachoDAL();

        public IActionResult GestionLogistica()
        {
            // 1. Seguridad: Permitimos al Gerente (1) y al encargado de Despacho (6)
            int? rol = HttpContext.Session.GetInt32("RolUsuario");

            if (rol == null || (rol != 1 && rol != 6))
            {
                return RedirectToAction("Login", "Acceso");
            }

            // 2. Usamos el método optimizado que trae TODO lo que no ha sido entregado
            // Esto incluye lo pendiente y lo que está en camino (para que el jefe supervise)
            var despachos = _despachoDal.ListarTodoParaGestion();

            // 3. Retornamos la vista con las Cartas que diseñamos
            return View(despachos);
        }

        // --- Futuros métodos para tu idea de Inspección ---

        // [HttpPost]
        // public IActionResult AsignarTransportista(int idDespacho, int idTransporte) { ... }

        // [HttpPost]
        // public IActionResult EnviarAInspeccion(int idDespacho) { ... }
    }
}