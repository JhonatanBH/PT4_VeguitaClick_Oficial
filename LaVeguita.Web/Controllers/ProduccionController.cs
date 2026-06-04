using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using LaVeguita.DAL;

namespace LaVeguita.Web.Controllers
{
    public class ProduccionController : Controller
    {
        private readonly ProduccionDAL _produccionDal = new ProduccionDAL();

        // Vista principal del panel de produccion
        public IActionResult Index()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 2 && rol != 3))
            {
                return RedirectToAction("Login", "Acceso");
            }
            return View();
        }

        // Vista de procesos de lavado, calidad y compostaje
        public IActionResult ProcesosPlanta()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 2 && rol != 3))
            {
                return RedirectToAction("Login", "Acceso");
            }

            // 🚀 ACTUALIZADO: Ahora invoca la DAL que extrae el desglose de DETALLE_LOTES
            var productoDal = new ProductoDAL();
            ViewBag.LotesPendientes = productoDal.ListarLotesPendientes();

            return View();
        }

        // 🚀 METODO ACTUALIZADO: El gatillo que procesa el lote e impacta Oracle
        [HttpPost]
        public IActionResult ProcesarLote(int idLote, int cantEntrada, int cantPrimCalidad, int cantSegCalidad, int idProducto, int idProveedor)
        {
            // 1. Rescatamos al empleado logueado desde la sesion activa
            int? idEmpleado = HttpContext.Session.GetInt32("IdUsuario");

            if (idEmpleado == null)
            {
                return Json(new { exito = false, mensaje = "Sesion expirada. Por favor vuelva a iniciar sesion." });
            }

            string mensajeOracle = string.Empty;

            // 2. Invocamos la DAL enviando el nuevo idLote al Stored Procedure
            bool resultadoTransaccion = _produccionDal.RegistrarFlujoProduccion(
                idLote,
                cantEntrada,
                cantPrimCalidad,
                cantSegCalidad,
                idProducto,
                idEmpleado.Value,
                idProveedor,
                out mensajeOracle
            );

            // 3. Devolvemos la respuesta al JavaScript de la vista
            if (resultadoTransaccion)
            {
                return Json(new { exito = true, mensaje = mensajeOracle });
            }
            else
            {
                return Json(new { exito = false, mensaje = mensajeOracle });
            }
        }
    }
}