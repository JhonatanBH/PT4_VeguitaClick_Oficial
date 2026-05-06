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
            int rolEncontrado = _usuarioDal.ValidarUsuario(user, pass);

            if (rolEncontrado > 0)
            {
                HttpContext.Session.SetInt32("RolUsuario", rolEncontrado);
                HttpContext.Session.SetString("UsuarioNombre", user);

                // REGLA DE NEGOCIO: Redirección por Rol
                if (rolEncontrado == 8) // Rol 8 = Cliente
                {
                    return RedirectToAction("Catalogo", "Tienda");
                }
                else // Admin, Gerente, Asistentes, etc.
                {
                    return RedirectToAction("Index", "Home");
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

