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
            if (rol == null || (rol != 1 && rol != 2 && rol != 4)) return RedirectToAction("Login", "Acceso");

            var guias = _loteDal.ListarGuiasIngresadas();

            // Evaluamos qué guías tienen documentos adjuntos para la interfaz
            var diccionarioDocumentos = new Dictionary<int, bool>();
            foreach (var g in guias)
            {
                diccionarioDocumentos.Add(g.IdRecepcion, _loteDal.ExisteGuiaDocumento(g.IdRecepcion));
            }
            ViewBag.Documentos = diccionarioDocumentos;

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

        [HttpPost]
        public async Task<IActionResult> SubirGuiaProveedorBlob([FromForm] int idRecepcionGuia, [FromForm] IFormFile archivoPdf)
        {
            if (archivoPdf == null || archivoPdf.Length == 0)
            {
                TempData["Error"] = "Debe adjuntar un archivo PDF válido.";
                return RedirectToAction("Index");
            }

            if (!archivoPdf.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Restricción de Seguridad: El documento debe ser estrictamente PDF.";
                return RedirectToAction("Index");
            }

            try
            {
                byte[] binarioBlob;
                using (var ms = new System.IO.MemoryStream())
                {
                    await archivoPdf.CopyToAsync(ms);
                    binarioBlob = ms.ToArray();
                }

                // 🚀 Ahora sí activamos la llamada real a Oracle con los nombres perfectamente mapeados
                bool exito = _loteDal.GuardarGuiaProveedorBlob(idRecepcionGuia, binarioBlob, archivoPdf.FileName, out string mensajeDeOracle);

                if (exito) TempData["Mensaje"] = mensajeDeOracle;
                else TempData["Error"] = mensajeDeOracle;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Falla al guardar el archivo en Oracle: " + ex.Message;
            }

            return RedirectToAction("Index");
        }



        [HttpGet]
        public IActionResult DescargarGuia(int id)
        {
            string nombreArchivo;
            byte[] archivoBinario = _loteDal.DescargarGuiaProveedorBlob(id, out nombreArchivo);

            if (archivoBinario == null || archivoBinario.Length == 0)
            {
                TempData["Error"] = "El documento solicitado no se encuentra en la base de datos.";
                return RedirectToAction("Index");
            }

            return File(archivoBinario, "application/pdf", nombreArchivo);
        }

        [HttpPost]
        public IActionResult Anular(int id)
        {
            bool exito = _loteDal.AnularGuiaRecepcion(id);
            if (exito) TempData["Mensaje"] = "La guía de recepción ha sido anulada por error de digitación.";
            else TempData["Error"] = "No se pudo anular la guía seleccionada.";

            return RedirectToAction("Index");
        }

    }
}