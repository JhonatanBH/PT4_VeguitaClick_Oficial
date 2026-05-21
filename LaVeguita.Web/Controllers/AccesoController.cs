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
                HttpContext.Session.SetInt32("IdCliente", usuarioLogueado.IdDireccion);

                switch (usuarioLogueado.IdRolUsuario)
                {
                    case 1: // Gerente
                    case 2: // Jefe Adm
                        return RedirectToAction("Index", "Home");

                    case 6: // Asistente Despacho / Bodeguero
                        // JUGADA MAESTRA: Si entra el Bodeguero, directo a despachar
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
        public IActionResult Registrar(Usuario nuevoUsuario, string confirmarPass)
        {
            ModelState.Remove("NombreRol");
            ModelState.Remove("IdUsuario");
            ModelState.Remove("IdDireccion");
            ModelState.Remove("FechaRegistro");

            nuevoUsuario.IdRolUsuario = 8; // Rol Cliente estricto
            nuevoUsuario.IdDireccion = 100; // Constante de consistencia para la base de datos Oracle

            try
            {
                // SOLUCIONADO: Se utiliza la propiedad real 'Contrasena' de la Entidad
                if (nuevoUsuario.Contrasena != confirmarPass)
                {
                    ViewData["Error"] = "Las contrasenas ingresadas no coinciden de forma simetrica.";
                    return View(nuevoUsuario);
                }

                bool exito = _usuarioBll.AgregarUsuario(nuevoUsuario);

                if (exito)
                {
                    TempData["ExitoRegistro"] = "Su cuenta de Cliente ha sido creada de forma exitosa. Inicie sesion para comprar.";
                    return RedirectToAction("Login");
                }
                else
                {
                    ViewData["Error"] = "No se pudo completar la transaccion de registro en Oracle Cloud.";
                }
            }
            catch (Exception ex)
            {
                ViewData["Error"] = "Error transaccional en base de datos distribuida: " + ex.Message;
            }

            return View(nuevoUsuario);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Acceso");
        }
    }
}