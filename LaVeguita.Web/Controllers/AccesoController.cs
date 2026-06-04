using LaVeguita.BLL;
using LaVeguita.DAL;
using LaVeguita.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;

namespace LaVeguita.Web.Controllers
{
    public class AccesoController : Controller
    {
        private readonly UsuarioDAL _usuarioDal = new UsuarioDAL();
        private readonly UsuarioBLL _usuarioBll = new UsuarioBLL();

        // ==========================================
        // INICIO DE SESION (LOGIN) CON BLOQUEO PROGRESIVO
        // ==========================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string user, string pass)
        {
            Usuario usuarioLogueado = _usuarioDal.ObtenerUsuarioParaLogin(user, pass);

            if (usuarioLogueado != null)
            {
                HttpContext.Session.Remove("IntentosLogin");

                HttpContext.Session.SetInt32("IdUsuario", usuarioLogueado.IdUsuario);
                HttpContext.Session.SetInt32("RolUsuario", usuarioLogueado.IdRolUsuario);
                HttpContext.Session.SetString("UsuarioNombre", usuarioLogueado.NombreUser);
                HttpContext.Session.SetInt32("IdCliente", usuarioLogueado.IdUsuario);

                switch (usuarioLogueado.IdRolUsuario)
                {
                    case 1: // Gerente
                    case 2: // Jefe Adm
                        return RedirectToAction("Index", "Home");

                    case 3: // Jefe de Produccion 🚀
                        return RedirectToAction("Index", "Produccion");

                    case 5: // Asistente de Ventas
                        return RedirectToAction("AsistenteVentas", "Ventas");

                    case 6: // Asistente Despacho / Bodeguero
                        return RedirectToAction("Monitor", "Bodega");

                    case 7: // Transportista
                        return RedirectToAction("SeleccionarVehiculo", "Transporte");

                    case 8: // Cliente
                        return RedirectToAction("Catalogo", "Tienda");

                    default:
                        return RedirectToAction("Index", "Home");
                }
            }

            int intentos = HttpContext.Session.GetInt32("IntentosLogin") ?? 0;
            intentos++;
            HttpContext.Session.SetInt32("IntentosLogin", intentos);

            if (intentos >= 3)
            {
                ViewBag.MostrarRecuperar = true;
                ViewBag.Error = "Has superado el limite de 3 intentos permitidos. Puede restablecer sus credenciales presionando el boton de abajo.";
            }
            else
            {
                ViewBag.Error = $"Credenciales invalidas. Intento {intentos} de 3. Ingrese nuevamente.";
            }

            return View();
        }

        // ==========================================
        // REGISTRO AUTONOMO DE CLIENTES (ROL 8)
        // ==========================================

        [HttpGet]
        public IActionResult Registrar()
        {
            var nuevoUsuario = new Usuario();
            return View(nuevoUsuario);
        }

        [HttpPost]
        public IActionResult Registrar(Usuario nuevoUsuario, string confirmarPass, string calleDir, int numeroDir, int idComunaDir)
        {
            ModelState.Remove("NombreRol");
            ModelState.Remove("IdUsuario");
            ModelState.Remove("IdDireccion");
            ModelState.Remove("FechaRegistro");

            nuevoUsuario.IdRolUsuario = 8; // Rol Cliente estricto

            try
            {
                if (nuevoUsuario.Contrasena != confirmarPass)
                {
                    ViewData["Error"] = "Las contraseñas ingresadas no coinciden.";
                    return View(nuevoUsuario);
                }

                // 1. Instanciamos la DAL directamente para usar la Transacción Maestra
                UsuarioDAL _usuarioDalLocal = new UsuarioDAL();

                // 2. Creamos el objeto Dirección con lo que tipeó el usuario
                Direccion nuevaDir = new Direccion
                {
                    NombreDir = calleDir,
                    NumeroDir = numeroDir,
                    IdComuna = idComunaDir
                };

                // 3. Ejecutamos la inserción doble (Dirección + Usuario)
                bool exito = _usuarioDalLocal.RegistrarUsuarioCompleto(nuevoUsuario, nuevaDir);

                if (exito)
                {
                    TempData["ExitoRegistro"] = "Cuenta creada. Inicie sesión para comprar.";
                    return RedirectToAction("Login");
                }
                else
                {
                    ViewData["Error"] = "No se pudo registrar en Oracle Cloud.";
                }
            }
            catch (Exception ex)
            {
                ViewData["Error"] = ex.Message;
            }

            return View(nuevoUsuario);
        }


        [HttpGet]
        public IActionResult Logout()
        {
            try
            {
                // 1. Limpiamos las variables de sesión del servidor
                HttpContext.Session.Clear();

                // 2. Borramos la cookie de sesión para obligar al navegador a desloguearse por completo
                if (Request.Cookies[".AspNetCore.Session"] != null)
                {
                    Response.Cookies.Delete(".AspNetCore.Session");
                }
            }
            catch (Exception)
            {
                // Si ya estaba vacía, ignoramos el error para no trabar la redirección
            }

            // 3. Redirección absoluta y limpia hacia la vista de Login
            return RedirectToAction("Login", "Acceso");
        }





    }
}