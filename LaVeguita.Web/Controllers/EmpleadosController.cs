using Microsoft.AspNetCore.Mvc;
using LaVeguita.DAL;
using LaVeguita.Entities;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace LaVeguita.Web.Controllers
{
    public class EmpleadosController : Controller
    {
        private readonly EmpleadoDAL _empleadoDal = new EmpleadoDAL();
        private readonly UsuarioDAL _usuarioDal = new UsuarioDAL(); // Para reutilizar ListarRoles()

        public IActionResult Index()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 2)) return RedirectToAction("Login", "Acceso");

            var empleados = _empleadoDal.ListarEmpleados();
            return View(empleados);
        }

        public IActionResult Crear()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 2)) return RedirectToAction("Login", "Acceso");

            // Cargamos los roles en el ViewBag
            ViewBag.Roles = _usuarioDal.ListarRoles();
            return View();
        }

        [HttpPost]
        public IActionResult Crear(Empleado emp, string username, string contrasena)
        {
            try
            {
                bool exito = _empleadoDal.InsertarEmpleadoCompleto(emp, username, contrasena);
                if (exito)
                {
                    TempData["Exito"] = "¡Ficha de Colaborador y Credenciales creadas exitosamente!";
                    return RedirectToAction("Index");
                }
                ViewBag.Error = "No se pudo registrar el empleado en la base de datos.";
            }
            catch (System.Exception ex)
            {
                ViewBag.Error = "Error en el alta: " + ex.Message;
            }

            ViewBag.Roles = _usuarioDal.ListarRoles();
            return View(emp);
        }

        [HttpPost]
        public JsonResult ValidarPermisoGerente(string user, string pass)
        {
            // Reutilizamos tu método nativo de UsuarioDAL
            // Recuerda que el Rol 1 representa estrictamente al Gerente General
            int idRolObtenido = _usuarioDal.ValidarUsuario(user, pass);

            if (idRolObtenido == 1)
            {
                return Json(new { autorizado = true });
            }
            else
            {
                return Json(new { autorizado = false });
            }
        }

        // GET: Empleados/Editar/5
        public IActionResult Editar(int id)
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 2)) return RedirectToAction("Login", "Acceso");

            Empleado emp = _empleadoDal.ObtenerPorId(id);
            if (emp == null) return NotFound();

            ViewBag.Roles = _usuarioDal.ListarRoles(); // Para cargar el selector de cargos
            return View(emp);
        }

        // POST: Empleados/Editar
        [HttpPost]
        public IActionResult Editar(Empleado emp)
        {
            try
            {
                if (_empleadoDal.ActualizarEmpleado(emp))
                {
                    TempData["Exito"] = "¡La ficha laboral número #" + emp.IdEmpleado + " fue actualizada con éxito!";
                    return RedirectToAction("Index");
                }
                ViewBag.Error = "No se pudieron guardar los cambios en la base de datos.";
            }
            catch (System.Exception ex)
            {
                ViewBag.Error = "Error en la modificación: " + ex.Message;
            }

            ViewBag.Roles = _usuarioDal.ListarRoles();
            return View(emp);
        }

        // GET: Empleados/Eliminar/5 (Invocado tras validar la clave del Gerente)
        public IActionResult Eliminar(int id)
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 2)) return RedirectToAction("Login", "Acceso");

            try
            {
                if (_empleadoDal.EliminarEmpleado(id))
                {
                    TempData["Exito"] = "La ficha de personal y sus credenciales de acceso fueron removidas del sistema.";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar el registro.";
                }
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = "Error al rescindir contrato: " + ex.Message;
            }

            return RedirectToAction("Index");
        }




    }
}