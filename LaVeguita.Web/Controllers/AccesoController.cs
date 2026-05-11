using LaVeguita.BLL;
using LaVeguita.DAL;
using LaVeguita.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace LaVeguita.Web.Controllers
{
    public class AccesoController : Controller // <--- 1. IMPORTANTE: Agregar ": Controller"
    {
        // 2. Declarar la variable de la DAL
        private readonly UsuarioDAL _usuarioDal = new UsuarioDAL();

        [HttpPost]
        public IActionResult Login(string user, string pass)
        {
            Usuario usuarioLogueado = _usuarioDal.ObtenerUsuarioParaLogin(user, pass);

            if (usuarioLogueado != null)
            {
                // 1. Datos universales (Todos los tienen)
                HttpContext.Session.SetInt32("IdUsuario", usuarioLogueado.IdUsuario);
                HttpContext.Session.SetInt32("RolUsuario", usuarioLogueado.IdRolUsuario);
                HttpContext.Session.SetString("UsuarioNombre", usuarioLogueado.NombreUser);

                // 2. Datos opcionales (Solo si existen)
                // Usamos 0 si es nulo para que no explote el cast
                HttpContext.Session.SetInt32("IdCliente", usuarioLogueado.IdDireccion);

                // 3. Redirección por Caso de Uso
                switch (usuarioLogueado.IdRolUsuario)
                {
                    case 1: case 2: return RedirectToAction("Index", "Home");      // Gestión
                    case 7: return RedirectToAction("MisDespachos", "Transporte"); // Logística
                    case 8: return RedirectToAction("Catalogo", "Tienda");        // Venta
                    default: return RedirectToAction("Index", "Home");
                }
            }
            ViewBag.Error = "Credenciales incorrectas";
            return View();
        }


        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Borra toda la información de la sesión
            return RedirectToAction("Login", "Acceso");
        }




    }









}

