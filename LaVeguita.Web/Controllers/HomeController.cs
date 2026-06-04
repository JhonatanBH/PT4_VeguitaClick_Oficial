using LaVeguita.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using LaVeguita.DAL; // Asegúrate de tener este using

namespace LaVeguita.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ProductoDAL _productoDal = new ProductoDAL();

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");

            // 1. Si no hay sesion, al Login
            if (rol == null) return RedirectToAction("Login", "Acceso");

            // 2. Redireccion por Rol (El "Semaforo")
            switch (rol)
            {
                case 8: // CLIENTE
                    return RedirectToAction("Catalogo", "Tienda");

                case 7: // TRANSPORTISTA
                    return RedirectToAction("MisDespachos", "Transporte");

                case 6: // DESPACHO / BODEGA
                    return RedirectToAction("Monitor", "Bodega");

                case 5: // ASISTENTE DE VENTAS
                    return RedirectToAction("AsistenteVentas", "Ventas");

                case 4: // COMPRAS
                    return RedirectToAction("Index", "Compras"); // Ajusta el controlador si se llama distinto

                case 3: // JEFE DE PRODUCCION 🚀 (¡AQUÍ ESTÁ LA SOLUCIÓN!)
                    return RedirectToAction("Index", "Produccion");

                case 1: // GERENTE
                case 2: // JEFE ADMINISTRACION
                    break; // Estos sí pasan de largo a ver los gráficos de abajo

                default:
                    return RedirectToAction("Login", "Acceso");
            }

            // 3. Logica del Dashboard para el Gerente (Rol 1 y 2)
            // 3. Logica del Dashboard para el Gerente (Rol 1 y 2)
            var listaProductos = _productoDal.ListarProductos();
            var despachoDal = new LaVeguita.DAL.DespachoDAL();
            var despachosPendientes = despachoDal.ListarDespachosPendientes();

            ViewBag.PendientesEntrega = despachosPendientes.Count;
            ViewBag.TotalProductos = listaProductos.Count;
            ViewBag.StockCritico = listaProductos.Count(p => p.StockActual <= p.StockMin);
            ViewBag.NombreUsuario = HttpContext.Session.GetString("UsuarioNombre");

            // --- MAPEO DIGITAL Y DINAMICO PARA CHART.JS (BORRA EL COLOR ROJO) ---
            // Invocamos el nuevo motor de agrupacion de Oracle
            var conteosDinamicos = despachoDal.ObtenerConteosPorTipoEnvio();

            // Extraemos los valores de forma directa y fluida desde el Diccionario
            ViewBag.GraficoNormal = conteosDinamicos["NORMAL"];
            ViewBag.GraficoEconomico = conteosDinamicos["ECONOMICO"];
            ViewBag.GraficoUrgente = conteosDinamicos["URGENTE"];
            ViewBag.GraficoFijo = conteosDinamicos["FIJO"];

            // Rendimiento dinamico de la flota ecologica
            // Filtramos la lista real de despachos en curso segun las cotas metricas fijadas (< 5kg y >= 5kg)
            ViewBag.FlotaBicicletas = despachosPendientes.Count(d => d.PesoTotalKg < 5);
            ViewBag.FlotaTriciclos = despachosPendientes.Count(d => d.PesoTotalKg >= 5);

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}