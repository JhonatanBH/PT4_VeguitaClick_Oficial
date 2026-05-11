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

            // 1. Si no hay sesión, al Login
            if (rol == null) return RedirectToAction("Login", "Acceso");

            // 2. Redirección por Rol (El "Semáforo")
            switch (rol)
            {
                case 8: // CLIENTE
                    return RedirectToAction("Catalogo", "Tienda");

                case 7: // TRANSPORTISTA
                    return RedirectToAction("MisDespachos", "Transporte");

                case 1: // GERENTE
                case 2: // JEFE ADMINISTRACIÓN
                    // El Gerente sí se queda en el Home para ver el Dashboard
                    break;

                default:
                    // Roles intermedios (Producción, Ventas, etc.) pueden ir al inventario
                    return RedirectToAction("Index", "Productos");
            }

            // 3. Lógica del Dashboard para el Gerente (Rol 1 y 2)
            var listaProductos = _productoDal.ListarProductos();
            var despachoDal = new LaVeguita.DAL.DespachoDAL();
            var despachosPendientes = despachoDal.ListarDespachosPendientes();

            ViewBag.PendientesEntrega = despachosPendientes.Count;
            ViewBag.TotalProductos = listaProductos.Count;
            ViewBag.StockCritico = listaProductos.Count(p => p.StockActual <= p.StockMin);
            ViewBag.NombreUsuario = HttpContext.Session.GetString("UsuarioNombre");

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