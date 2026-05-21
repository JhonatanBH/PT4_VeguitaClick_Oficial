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
        // Recibimos un parametro extra 'unidadPeso' que viene del select de la vista
        public IActionResult Crear(Producto p, string unidadPeso)
        {
            // LOGICA MATEMATICA DE PESO:
            // Si el usuario escribio "500" y selecciono gramos ("g")
            // Lo convertimos a 0.5 Kg para que la base de datos y la logistica no se rompan
            if (unidadPeso == "g")
            {
                p.PesoUnitEstimado = p.PesoUnitEstimado / 1000m;
            }

            try
            {
                _productoBll.InsertarProducto(p); // (Asegurate que tu DAL y BLL envien p.UnidadMedida a p_uom)
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

            // Poblamos los selectores para la edición
            ViewBag.Proveedores = _productoDAL.ListarProveedores();
            ViewBag.Tipos = _productoDAL.ListarTipos();
            ViewBag.Calidades = _productoDAL.ListarCalidades();

            return View(producto);
        }

        [HttpPost]
        public IActionResult Editar(Producto p, string unidadPeso)
        {
            // Limpiamos validaciones de objetos complejos o dinamicos para que no traben el flujo
            ModelState.ClearValidationState(nameof(Producto));

            // --- MOTOR DE REGLA MATEMATICA DE PESO (CASO 4) ---
            // Si el usuario edito el producto colocandolo en gramos (g)
            // dividimos por 1000 para forzar el almacenamiento en Kg puros en la base de datos
            if (unidadPeso == "g")
            {
                p.PesoUnitEstimado = p.PesoUnitEstimado / 1000m;
            }

            try
            {
                // Invocamos el metodo que ahora si mapea el parametro p_uom hacia el Package
                bool exito = _productoDAL.EditarProducto(p);

                if (exito)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewData["Error"] = "No se pudo actualizar el registro. Verifique si el ID existe en el inventario.";
                }
            }
            catch (Exception ex)
            {
                ViewData["Error"] = "Error de base de datos: " + ex.Message;
            }

            // Si llegamos aqui, significa que ocurrio un error transaccional. 
            // Repoblamos de forma obligatoria las bolsas de datos para los selectores de la vista.
            ViewBag.Proveedores = _productoDAL.ListarProveedores();
            ViewBag.Tipos = _productoDAL.ListarTipos();
            ViewBag.Calidades = _productoDAL.ListarCalidades();

            return View(p);
        }
    }
}
