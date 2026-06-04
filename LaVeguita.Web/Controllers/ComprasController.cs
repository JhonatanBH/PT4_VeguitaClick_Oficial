using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using LaVeguita.DAL;
using LaVeguita.Entities;

namespace LaVeguita.Web.Controllers
{
    public class ComprasController : Controller
    {
        private readonly LoteRecepcionDAL _loteDal = new LoteRecepcionDAL();
        private readonly ProductoDAL _productoDal = new ProductoDAL();

        // 1. PANTALLA PRINCIPAL: Carga historial de guias y los selects
        [HttpGet]
        public IActionResult Index()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 2 && rol != 4))
            {
                return RedirectToAction("Login", "Acceso");
            }

            // Cargamos el historial de guias ingresadas para mostrar en la tabla inferior
            var guias = _loteDal.ListarGuiasIngresadas();

            // Rellenamos las bolsas de datos para los combobox del formulario
            ViewBag.Productos = _productoDal.ListarProductos();
            ViewBag.Proveedores = _productoDal.ListarProveedores();

            return View(guias);
        }

        // 2. RECEPCION AJAX: Recibe el carrito JSON de la vista y gatilla la transaccion
        [HttpPost]
        public IActionResult GuardarGuiaCompleta([FromBody] RecepcionGuia modeloGuia)
        {
            if (modeloGuia == null || string.IsNullOrEmpty(modeloGuia.NumGuiaFisica) || modeloGuia.IdProveedor == 0 || modeloGuia.ItemsCargamento.Count == 0)
            {
                return Json(new { exito = false, mensaje = "Datos invalidos o el carrito de cargamientos esta vacio." });
            }

            string mensajeDeOracle;
            bool seGuardo = _loteDal.RegistrarRecepcionCompleta(modeloGuia, out mensajeDeOracle);

            return Json(new { exito = seGuardo, mensaje = mensajeDeOracle });
        }
    }
}