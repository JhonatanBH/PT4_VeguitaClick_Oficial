using LaVeguita.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace LaVeguita.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");

            if (rol == null) return RedirectToAction("Login", "Acceso");

            // Si es Cliente, directo al catálogo
            if (rol == 8) return RedirectToAction("Catalogo", "Tienda");

            // Si es Gerente (o administrativo), preparamos datos básicos
            // Aquí podrías llamar a tus DAL para traer números reales, por ahora usaremos ViewBag
            var productoDal = new LaVeguita.DAL.ProductoDAL();
            var listaProductos = productoDal.ListarProductos();

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
