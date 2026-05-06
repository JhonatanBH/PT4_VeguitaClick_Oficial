using LaVeguita.BLL;
using LaVeguita.DAL;
using LaVeguita.Entities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace LaVeguita.Web.Controllers
{
    public class ProductosController : Controller
    {
        private readonly ProductoBLL _productoBll = new ProductoBLL();
        private readonly ProductoDAL _productoDAL = new ProductoDAL();

        public IActionResult Index()
        {
            // Capturamos el rol de la sesión
            int? rol = HttpContext.Session.GetInt32("RolUsuario");

            // 1. Si no hay sesión, al Login
            if (rol == null) return RedirectToAction("Login", "Acceso");

            // 2. Si es Cliente (8), lo mandamos a SU vista (Catálogo)
            if (rol == 8) return RedirectToAction("Catalogo", "Tienda");

            // 3. Si llega aquí, es Admin/Gerente/Asistente y puede ver la tabla
            var lista = _productoBll.ListarProductos();
            return View(lista);
        }

        // ==========================================
        // ACCIONES PARA CREAR
        // ==========================================

        [HttpGet]
        public IActionResult Crear()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");

            // Solo permitimos roles administrativos (Gerente=1, Jefe=2, Asistente=4, etc.)
            // Si es Cliente (8) o Transportista (7), no deberían estar aquí
            if (rol == 8 || rol == 7 || rol == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Tipos = _productoDAL.ListarTipos();
            ViewBag.Proveedores = _productoDAL.ListarProveedores();
            ViewBag.Calidades = _productoDAL.ListarCalidades();
            return View();
        }

        [HttpPost]
        public IActionResult Crear(Producto p)
        {
            try
            {
                _productoBll.InsertarProducto(p);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al crear: " + ex.Message);

                // Recargamos TODO para que la vista no explote al reintentar
                ViewBag.Tipos = _productoDAL.ListarTipos();
                ViewBag.Proveedores = _productoDAL.ListarProveedores();
                ViewBag.Calidades = _productoDAL.ListarCalidades();

                return View(p);
            }
        }

        // ==========================================
        // ACCIONES PARA EDITAR
        // ==========================================

        [HttpGet]
        public IActionResult Editar(int id)
        {
            var producto = _productoDAL.ObtenerPorId(id);
            if (producto == null) return NotFound();

            // Poblamos los selectores para la edición
            ViewBag.Proveedores = _productoDAL.ListarProveedores();
            ViewBag.Tipos = _productoDAL.ListarTipos();
            ViewBag.Calidades = _productoDAL.ListarCalidades();

            return View(producto);
        }

        [HttpPost]
        public IActionResult Editar(Producto p)
        {
            // Eliminamos la línea 'var producto = _productoDAL.ObtenerPorId(id);' 
            // ya que el ID ya viene dentro del objeto 'p' (p.IdProducto)

            // Limpiamos validaciones de objetos complejos (NomTipo, etc)
            ModelState.ClearValidationState(nameof(Producto));

            try
            {
                bool exito = _productoDAL.EditarProducto(p);

                if (exito)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewData["Error"] = "No se pudo actualizar. Verifique si el ID existe.";
                }
            }
            catch (Exception ex)
            {
                ViewData["Error"] = "Error de base de datos: " + ex.Message;
            }

            // Si llegamos aquí, algo falló. Repoblamos TODAS las listas.
            ViewBag.Proveedores = _productoDAL.ListarProveedores();
            ViewBag.Tipos = _productoDAL.ListarTipos();
            ViewBag.Calidades = _productoDAL.ListarCalidades();

            return View(p);
        }
    }
}
