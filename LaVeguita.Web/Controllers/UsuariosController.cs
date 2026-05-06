using Microsoft.AspNetCore.Mvc;
using LaVeguita.BLL;
using LaVeguita.Entities;
using Microsoft.AspNetCore.Http; // Necesario para usar Session

namespace LaVeguita.Web.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly UsuarioBLL _usuarioBll = new UsuarioBLL();

        // 1. LISTAR USUARIOS
        public IActionResult Index()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");

            // SEGURIDAD: Si no hay sesión o no es Gerente (1), fuera.
            if (rol == null) return RedirectToAction("Login", "Acceso");
            if (rol != 1) return RedirectToAction("Index", "Home");

            var lista = _usuarioBll.ObtenerTodos();
            return View(lista);
        }

        // 2. CREAR USUARIO (POST)
        [HttpPost]
        public IActionResult Crear(Usuario nuevoUser)
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || rol != 1) return RedirectToAction("Login", "Acceso");

            if (nuevoUser != null)
            {
                bool inserto = _usuarioBll.AgregarUsuario(nuevoUser);
                if (inserto) return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
        }

        // 3. EDITAR USUARIO (GET - Cargar formulario)
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

        // 4. EDITAR USUARIO (POST - Guardar cambios)
        [HttpPost]
        public IActionResult Editar(Usuario usuarioEditado)
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || rol != 1) return RedirectToAction("Login", "Acceso");

            usuarioEditado.IdDireccion = 100; // Valor por defecto para Oracle

            if (_usuarioBll.ActualizarUsuario(usuarioEditado))
            {
                return RedirectToAction("Index");
            }

            ViewBag.Roles = _usuarioBll.ListarRoles(); // Recargar roles si falla
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
