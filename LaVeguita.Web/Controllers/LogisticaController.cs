using LaVeguita.DAL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
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
                    // Mensaje plano para cumplir la Regla 12 y evitar caracteres raros
                    TempData["Exito"] = $"Pedido de Venta #{IdVenta} asignado con exito al movil #{IdTransporte}";
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

        [HttpPost]
        public IActionResult Despachar(int idVenta)
        {
            if (idVenta <= 0)
            {
                TempData["Error"] = "Código de venta inválido para procesar el despacho.";
                return RedirectToAction("GestionLogistica");
            }

            try
            {
                using (OracleConnection cn = new Conexion().LeerConexion())
                {
                    // 🚀 CORREGIDO: Usamos el nombre real de tu tabla obtenido de VentasController
                    string query = "UPDATE ADMIN.ORDEN_VENTA SET ESTADO = 'ENVIADO' WHERE ID_VENTA = :id";

                    using (OracleCommand cmd = new OracleCommand(query, cn))
                    {
                        cmd.Parameters.Add("id", OracleDbType.Int32).Value = idVenta;

                        cn.Open();
                        int filasAfectadas = cmd.ExecuteNonQuery();

                        if (filasAfectadas > 0)
                        {
                            TempData["Mensaje"] = $"El pedido #{idVenta} ha sido entregado al transportista y va en camino.";
                        }
                        else
                        {
                            TempData["Error"] = $"No se encontró la orden de venta #{idVenta} para actualizar.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error de comunicación con Oracle Cloud: " + ex.Message;
            }

            // Te devuelve limpio a la vista del monitor de logística
            return RedirectToAction("GestionLogistica");
        }




    }
}