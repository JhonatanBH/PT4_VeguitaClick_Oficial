using Microsoft.AspNetCore.Mvc;
using LaVeguita.BLL;
using LaVeguita.Entities;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace LaVeguita.Web.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly UsuarioBLL _usuarioBll = new UsuarioBLL();

        // 1. LISTAR USUARIOS
        public IActionResult Index()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || rol != 1) return RedirectToAction("Login", "Acceso");

            var lista = _usuarioBll.ObtenerTodos();
            return View(lista);
        }

        // 2. CREAR USUARIO (GET - Cargar formulario)
        [HttpGet]
        public IActionResult Crear()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || rol != 1) return RedirectToAction("Login", "Acceso");

            // 1. EL MOLDE: Enviamos un usuario nuevo para que asp-for no de error
            var nuevoUser = new Usuario();

            // 2. LA LISTA: Cargamos los roles. 
            // Si la BLL devuelve null, inicializamos una lista vacía para evitar el error de objeto nulo.
            var listaRoles = _usuarioBll.ListarRoles();
            ViewBag.Roles = listaRoles ?? new List<Rol>();

            return View(nuevoUser);
        }

        // 2. CREAR USUARIO (POST - Guardar en DB)
        [HttpPost]
        public IActionResult Crear(Usuario nuevoUser)
        {
            // 1. Seguridad de Rol (Igual que antes)
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || rol != 1) return RedirectToAction("Login", "Acceso");

            // 2. ELIMINAR VALIDACIONES QUE NO CORRESPONDEN AL INSERT
            // Quitamos NombreRol porque es un campo de "Solo Lectura" para el Index
            ModelState.Remove("NombreRol");
            ModelState.Remove("IdUsuario");
            ModelState.Remove("IdDireccion");

            try
            {
                if (ModelState.IsValid)
                {
                    nuevoUser.IdDireccion = 0; // Valor fijo para Oracle

                    bool inserto = _usuarioBll.AgregarUsuario(nuevoUser);

                    if (inserto)
                    {
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    // Si entra aquí, el 'asp-validation-summary="All"' te dirá qué otro campo falta
                    ModelState.AddModelError(string.Empty, "Error: Verifique los campos obligatorios.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Error de Oracle: " + ex.Message);
            }

            // Siempre recargar roles para la vista
            ViewBag.Roles = _usuarioBll.ListarRoles() ?? new List<Rol>();
            return View(nuevoUser);
        }

        // 3. EDITAR USUARIO (GET)
        [HttpGet]
        public IActionResult Editar(int id)
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || rol != 1) return RedirectToAction("Login", "Acceso");

            var usuario = _usuarioBll.ObtenerPorId(id);
            if (usuario == null) return NotFound();

            ViewBag.Roles = _usuarioBll.ListarRoles();
            return View(usuario);
        }

        // 4. EDITAR USUARIO (POST)
        [HttpPost]
        public IActionResult Editar(Usuario usuarioEditado)
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || rol != 1) return RedirectToAction("Login", "Acceso");

            usuarioEditado.IdDireccion = 0;

            if (_usuarioBll.ActualizarUsuario(usuarioEditado))
            {
                return RedirectToAction("Index");
            }

            ViewBag.Roles = _usuarioBll.ListarRoles();
            return View(usuarioEditado);
        }

        // 5. ELIMINAR USUARIO
        [HttpPost]
        public IActionResult Eliminar(int idUsuario)
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || rol != 1) return RedirectToAction("Login", "Acceso");

            _usuarioBll.BorrarUsuario(idUsuario);
            return RedirectToAction("Index");
        }
    }
}