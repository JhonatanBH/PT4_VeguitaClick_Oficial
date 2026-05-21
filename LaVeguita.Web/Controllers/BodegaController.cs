using Microsoft.AspNetCore.Mvc;
using System.Data;
using LaVeguita.DAL;

namespace LaVeguita.Web.Controllers
{
    public class BodegaController : Controller
    {
        private readonly VentaDAL _ventaDAL = new VentaDAL();

        // Muestra la lista de órdenes en preparación
        public IActionResult Monitor()
        {
            DataTable dt = _ventaDAL.ListarOrdenesEnPreparacion();
            return View(dt);
        }

        // Procesa el botón de despacho
        [HttpPost]
        public IActionResult Despachar(int idVenta)
        {
            bool exito = _ventaDAL.MarcarComoDespachado(idVenta);
            if (exito)
            {
                TempData["Mensaje"] = $"¡La Orden de Venta N° {idVenta} fue despachada correctamente!";
            }
            else
            {
                TempData["Error"] = "No se pudo actualizar el estado del despacho.";
            }
            return RedirectToAction("Monitor");
        }
    }
}