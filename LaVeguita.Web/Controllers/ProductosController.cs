using LaVeguita.BLL;
using LaVeguita.DAL;
using LaVeguita.Entities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace LaVeguita.Web.Controllers
{
    public class ProductosController : Controller
    {
        private readonly ProductoBLL _productoBll = new ProductoBLL();
        private readonly ProductoDAL _productoDAL = new ProductoDAL();

        public IActionResult Index()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null) return RedirectToAction("Login", "Acceso");
            if (rol == 8) return RedirectToAction("Catalogo", "Tienda");

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
        public IActionResult Crear(Producto p, string unidadPeso)
        {
            if (unidadPeso == "g")
            {
                p.PesoUnitEstimado = p.PesoUnitEstimado / 1000m;
            }

            try
            {
                _productoBll.InsertarProducto(p);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al crear: " + ex.Message);
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

            ViewBag.Proveedores = _productoDAL.ListarProveedores();
            ViewBag.Tipos = _productoDAL.ListarTipos();
            ViewBag.Calidades = _productoDAL.ListarCalidades();

            return View(producto);
        }

        [HttpPost]
        public IActionResult Editar(Producto p, string unidadPeso)
        {
            ModelState.ClearValidationState(nameof(Producto));

            if (unidadPeso == "g")
            {
                p.PesoUnitEstimado = p.PesoUnitEstimado / 1000m;
            }

            try
            {
                bool exito = _productoDAL.EditarProducto(p);

                if (exito) return RedirectToAction("Index");
                else ViewData["Error"] = "No se pudo actualizar el registro. Verifique si el ID existe.";
            }
            catch (Exception ex)
            {
                ViewData["Error"] = "Error de base de datos: " + ex.Message;
            }

            ViewBag.Proveedores = _productoDAL.ListarProveedores();
            ViewBag.Tipos = _productoDAL.ListarTipos();
            ViewBag.Calidades = _productoDAL.ListarCalidades();

            return View(p);
        }
    }
}