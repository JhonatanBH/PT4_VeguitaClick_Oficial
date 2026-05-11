using LaVeguita.DAL;
using LaVeguita.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LaVeguita.Web.Controllers
{
    public class TiendaController : Controller
    {
        private readonly ProductoDAL _productoDal = new ProductoDAL();

        // 1. Catálogo de Productos
        public IActionResult Catalogo()
        {
            // Blindaje de Rol: Solo Clientes (8)
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || rol != 8) return RedirectToAction("Login", "Acceso");

            var productos = _productoDal.ListarProductos();
            return View(productos);
        }

        // 2. Vista del Carrito de Compras
        public IActionResult Carrito()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || rol != 8) return RedirectToAction("Login", "Acceso");

            var carrito = ObtenerCarritoDeSesion();
            return View(carrito);
        }

        // 3. Agregar productos (Maneja el Peso para el Caso 4)
        [HttpPost]
        public IActionResult AgregarAlCarrito(int idProducto, string nombre, decimal precio, decimal peso, int cantidad)
        {
            var carrito = ObtenerCarritoDeSesion();
            var item = carrito.FirstOrDefault(x => x.IdProducto == idProducto);

            if (item == null)
            {
                carrito.Add(new CarritoItem
                {
                    IdProducto = idProducto,
                    Nombre = nombre ?? "Producto sin nombre",
                    Precio = precio,
                    PesoUnitario = peso, // Vital para decidir Bicicleta o Triciclo
                    Cantidad = cantidad
                });
            }
            else
            {
                item.Cantidad += cantidad;
            }

            GuardarCarritoEnSesion(carrito);
            TempData["Mensaje"] = $"¡{nombre} añadido correctamente!";
            return RedirectToAction("Catalogo");
        }

        // 4. Ajustar cantidad (+ o -)
        public IActionResult AjustarCantidad(int id, int cambio)
        {
            var carrito = ObtenerCarritoDeSesion();
            var item = carrito.FirstOrDefault(x => x.IdProducto == id);

            if (item != null)
            {
                item.Cantidad += cambio;
                if (item.Cantidad <= 0) carrito.Remove(item);
            }

            GuardarCarritoEnSesion(carrito);
            return RedirectToAction("Carrito");
        }

        // 5. Procesar compra en Oracle (Blindado contra ORA-02291)
        [HttpPost]
        public IActionResult FinalizarCompra()
        {
            // Validación de contenido del carrito
            var carrito = ObtenerCarritoDeSesion();
            if (carrito == null || !carrito.Any())
            {
                TempData["Error"] = "El carrito está vacío.";
                return RedirectToAction("Catalogo");
            }

            // Rescate de sesión con blindaje contra nulos
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            int? idClienteSesion = HttpContext.Session.GetInt32("IdCliente");

            // Si no hay usuario en sesión, abortar (Seguridad)
            if (idUsuario == null || idUsuario == 0)
            {
                TempData["Error"] = "Sesión inválida. Por favor, inicia sesión nuevamente.";
                return RedirectToAction("Login", "Acceso");
            }

            // Mantenemos el ID 100 de emergencia para asegurar que la FK de Oracle no falle
            // mientras los datos de la tabla CLIENTE se terminan de poblar.
            int idClienteFinal = (idClienteSesion == null || idClienteSesion == 0) ? 100 : idClienteSesion.Value;

            decimal total = carrito.Sum(x => x.Subtotal);
            VentaDAL dal = new VentaDAL();

            try
            {
                // Enviamos los datos a la DAL. Aquí el Package decidirá el transporte por peso.
                bool exito = dal.GenerarOrdenCompleta(idClienteFinal, idUsuario.Value, total, carrito);

                if (exito)
                {
                    HttpContext.Session.Remove("CarritoVeguita");
                    return RedirectToAction("Confirmacion");
                }
            }
            catch (Exception ex)
            {
                // Captura de errores técnicos (Integridad, Stock, Errores de Package)
                TempData["Error"] = $"Error al procesar la compra: {ex.Message}";
            }

            return RedirectToAction("Carrito");
        }

        // 6. Confirmación de pedido
        public IActionResult Confirmacion()
        {
            return View();
        }

        // --- MÉTODOS DE APOYO (Private) ---

        private List<CarritoItem> ObtenerCarritoDeSesion()
        {
            try
            {
                var sessionData = HttpContext.Session.GetString("CarritoVeguita");
                return string.IsNullOrEmpty(sessionData)
                    ? new List<CarritoItem>()
                    : JsonConvert.DeserializeObject<List<CarritoItem>>(sessionData);
            }
            catch
            {
                return new List<CarritoItem>(); // Blindaje ante error de serialización
            }
        }

        private void GuardarCarritoEnSesion(List<CarritoItem> carrito)
        {
            var settings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
            HttpContext.Session.SetString("CarritoVeguita", JsonConvert.SerializeObject(carrito, settings));
        }
    }
}