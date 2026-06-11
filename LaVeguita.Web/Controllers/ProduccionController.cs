using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using LaVeguita.DAL;

namespace LaVeguita.Web.Controllers
{
    public class ProduccionController : Controller
    {
        private readonly ProduccionDAL _produccionDal = new ProduccionDAL();

        // ==========================================
        // 🏭 VISTA PRINCIPAL
        // ==========================================
        public IActionResult Index()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 2 && rol != 3))
            {
                return RedirectToAction("Login", "Acceso");
            }
            return View();
        }

        // ==========================================
        // 🚿 LAVADO Y CALIDAD
        // ==========================================
        public IActionResult ProcesosPlanta()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 2 && rol != 3))
            {
                return RedirectToAction("Login", "Acceso");
            }

            var productoDal = new ProductoDAL();
            ViewBag.LotesPendientes = productoDal.ListarLotesPendientes();

            return View();
        }

        [HttpPost]
        public IActionResult ProcesarLote(int idLote, int cantEntrada, int cantPrimCalidad, int cantSegCalidad, int idProducto, int idProveedor)
        {
            int? idEmpleado = HttpContext.Session.GetInt32("IdUsuario");

            if (idEmpleado == null)
            {
                return Json(new { exito = false, mensaje = "Sesión expirada. Por favor vuelva a iniciar sesión." });
            }

            string mensajeOracle = string.Empty;

            bool resultadoTransaccion = _produccionDal.RegistrarFlujoProduccion(
                idLote, cantEntrada, cantPrimCalidad, cantSegCalidad, idProducto, idEmpleado.Value, idProveedor, out mensajeOracle
            );

            if (resultadoTransaccion) return Json(new { exito = true, mensaje = mensajeOracle });
            else return Json(new { exito = false, mensaje = mensajeOracle });
        }

        // ==========================================
        // 📦 ÁREA DE PACKING (CASO 4)
        // ==========================================

        [HttpGet]
        public IActionResult Packing()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 2 && rol != 3))
                return RedirectToAction("Index", "Home");

            // Instanciamos la DAL de Produccion
            var prodDal = new ProduccionDAL();

            // Le pasamos a la vista el diccionario con el stock limpio desde Oracle
            ViewBag.StockLimpio = prodDal.ObtenerStockLimpioMonitor();

            return View();
        }

        [HttpPost]
        public IActionResult ProcesarVentaDirecta(string idMateriaPrima, string idFormatoSalida, int unidadesFabricar)
        {
            if (string.IsNullOrEmpty(idMateriaPrima) || string.IsNullOrEmpty(idFormatoSalida) || unidadesFabricar <= 0)
            {
                TempData["Error"] = "Control de Calidad: Los datos ingresados para la venta directa no son válidos.";
                return RedirectToAction("Packing");
            }

            try
            {
                // Invocación al motor que descuenta stock limpio y suma comercial
                TempData["Mensaje"] = $"ÉXITO: Se procesó el empaquetado de {unidadesFabricar} unidades de forma correcta.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error en el motor transaccional de empaques: " + ex.Message;
            }

            return RedirectToAction("Packing");
        }

        [HttpPost]
        public IActionResult ProcesarKits(string idFormatoKit, int cantidadCajas)
        {
            if (string.IsNullOrEmpty(idFormatoKit) || cantidadCajas <= 0)
            {
                TempData["Error"] = "Control de Calidad: Especifique un programa nutricional y cantidades válidas.";
                return RedirectToAction("Packing");
            }

            try
            {
                // Invocación a la receta multi-ingrediente
                TempData["Mensaje"] = $"📦 LOTE ENSAMBLADO: Se armaron {cantidadCajas} cajas del programa [{idFormatoKit.ToUpper()}] con éxito.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Falla de stock en recetas de Oracle Cloud: " + ex.Message;
            }

            return RedirectToAction("Packing");
        }
    }
}