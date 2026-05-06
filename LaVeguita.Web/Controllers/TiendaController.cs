using Microsoft.AspNetCore.Mvc;
using LaVeguita.DAL;
using Microsoft.AspNetCore.Http;

namespace LaVeguita.Web.Controllers
{
    public class TiendaController : Controller
    {
        private readonly ProductoDAL _productoDal = new ProductoDAL();

        // Esta es la página donde llegan los CLIENTES (Rol 8)
        public IActionResult Catalogo()
        {
            // Seguridad: Si no es cliente, mandarlo al login
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || rol != 8)
            {
                return RedirectToAction("Login", "Acceso");
            }

            var productos = _productoDal.ListarProductos();
            return View(productos); // Asegúrate de haber creado la vista Catalogo.cshtml
        }
    }
}