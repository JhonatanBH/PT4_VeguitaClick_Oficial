using Microsoft.AspNetCore.Mvc;
using LaVeguita.DAL;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LaVeguita.Web.Controllers
{
    public class LogisticaController : Controller
    {
        private readonly DespachoDAL _despachoDal = new DespachoDAL();
        private readonly TransporteDAL _transporteDal = new TransporteDAL();
        private readonly UsuarioDAL _usuarioDal = new UsuarioDAL();

        public IActionResult GestionLogistica()
        {
            // 1. Seguridad: Permitimos al Gerente (1) y al encargado de Despacho / Asistente (6)
            int? rol = HttpContext.Session.GetInt32("RolUsuario");

            if (rol == null || (rol != 1 && rol != 6))
            {
                return RedirectToAction("Login", "Acceso");
            }

            // 2. Cargar listas necesarias para los combobox del Modal de asignación
            // Invocamos el método de tu UsuarioDAL que lee la tabla EMPLEADOS y devuelve objetos Empleado reales
            List<LaVeguita.Entities.Empleado> listaEmpleados = _usuarioDal.ListarEmpleadosDespacho();

            // Filtramos para asegurarnos de que en el combobox solo aparezcan los Transportistas (Rol 7)
            ViewBag.Transportistas = listaEmpleados.Where(e => e.IdRolUsuario == 7).ToList();

            // Cargamos los móviles operativos y disponibles usando tu método de exclusión de mantenciones
            ViewBag.Vehiculos = _transporteDal.ListarVehiculosDisponibles();

            // 3. Usamos el método optimizado que trae TODO lo que no ha sido entregado
            var despachos = _despachoDal.ListarTodoParaGestion();

            return View(despachos);
        }

        [HttpPost]
        public IActionResult ConfirmarAsignacion(int IdVenta, int IdTransporte, int IdEmpleado, decimal PesoTotalKg, decimal KmObtenidos)
        {
            try
            {
                // Invocamos el método que acabamos de inyectar en la DAL
                bool exito = _despachoDal.InsertarDespacho(IdVenta, IdTransporte, IdEmpleado, PesoTotalKg, KmObtenidos);

                if (exito)
                {
                    TempData["Exito"] = $"¡Pedido de Venta #{IdVenta} asignado con éxito al móvil #{IdTransporte}!";
                }
                else
                {
                    TempData["Error"] = "No se pudo procesar la asignación del despacho.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar la asignación logística: " + ex.Message;
            }

            return RedirectToAction("GestionLogistica");
        }
    }
}