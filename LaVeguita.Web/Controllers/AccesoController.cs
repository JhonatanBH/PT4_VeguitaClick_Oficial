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
                HttpContext.Session.SetInt32("IdUsuario", usuarioLogueado.IdUsuario);
                HttpContext.Session.SetInt32("RolUsuario", usuarioLogueado.IdRolUsuario);
                HttpContext.Session.SetString("UsuarioNombre", usuarioLogueado.NombreUser);
                HttpContext.Session.SetInt32("IdCliente", usuarioLogueado.IdDireccion);

                switch (usuarioLogueado.IdRolUsuario)
                {
                    case 1: // Gerente [cite: 109]
                    case 2: // Jefe Adm [cite: 111]
                        return RedirectToAction("Index", "Home");

                    case 7: // Transportista [cite: 121]
                                            // En lugar de ir directo a los pedidos, debe elegir su herramienta de trabajo
                        return RedirectToAction("SeleccionarVehiculo", "Transporte");

                    case 8: // Cliente [cite: 124]
                        return RedirectToAction("Catalogo", "Tienda");

                    default:
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

