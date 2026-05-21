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
        public IActionResult FinalizarCompra(string tipoEnvio, string horaFijo)
        {
            var carrito = ObtenerCarritoDeSesion();
            if (carrito == null || !carrito.Any())
            {
                TempData["Error"] = "El carrito esta vacio.";
                return RedirectToAction("Catalogo");
            }

            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            int? idClienteSesion = HttpContext.Session.GetInt32("IdCliente");

            if (idUsuario == null || idUsuario == 0)
            {
                TempData["Error"] = "Sesion invalida. Por favor, inicia sesion nuevamente.";
                return RedirectToAction("Login", "Acceso");
            }

            int idClienteFinal = (idClienteSesion == null || idClienteSesion == 0) ? 100 : idClienteSesion.Value;
            decimal total = carrito.Sum(x => x.Subtotal);

            // --- MOTOR DE REGLAS DE TIEMPO DEL CASO 4 ---
            DateTime ahora = DateTime.Now; // Hora real del servidor Cloud
            DateTime fechaEstimadaEntrega = ahora;
            string mensajeBloqueo = "";

            // Forzamos el tipo de envio a mayusculas para evitar problemas
            tipoEnvio = tipoEnvio?.ToUpper() ?? "NORMAL";

            switch (tipoEnvio)
            {
                case "ECONOMICO":
                    // Regra: Se reciben agendamientos hasta las 17:00 hrs. Envio dia habil siguiente entre 9:00 y 18:00 [cite: 36, 37]
                    if (ahora.Hour >= 17)
                    {
                        // Si pasa de las 17:00, se cuenta como agendado el dia siguiente, por ende entrega el subsiguiente
                        fechaEstimadaEntrega = ahora.AddDays(2).Date.AddHours(9);
                    }
                    else
                    {
                        fechaEstimadaEntrega = ahora.AddDays(1).Date.AddHours(9);
                    }
                    break;

                case "NORMAL":
                    // Regra: Envio dentro de las siguientes 6 horas[cite: 38]. Si es despues de las 12:00, se realiza el dia siguiente dentro de las 6 primeras horas [cite: 39]
                    if (ahora.Hour >= 12)
                    {
                        fechaEstimadaEntrega = ahora.AddDays(1).Date.AddHours(9); // Siguiente dia a primera hora (9:00 am)
                    }
                    else
                    {
                        fechaEstimadaEntrega = ahora.AddHours(6);
                    }
                    break;

                case "FIJO":
                    // Regra: El cliente elige la hora. No antes de 2 horas de ser agendado [cite: 40, 41]
                    if (string.IsNullOrEmpty(horaFijo))
                    {
                        mensajeBloqueo = "Para la opcion de envio FIJO debe especificar una hora valida.";
                    }
                    else
                    {
                        DateTime horaSeleccionada = DateTime.Parse(ahora.ToString("yyyy-MM-dd") + " " + horaFijo);
                        if (horaSeleccionada < ahora.AddHours(2))
                        {
                            mensajeBloqueo = "Regra de Negocio: El envio FIJO debe agendarse con un minimo de 2 horas de anticipacion.";
                        }
                        else
                        {
                            fechaEstimadaEntrega = horaSeleccionada;
                        }
                    }
                    break;

                case "URGENTE":
                    // Regra: Gestion inmediata, maximo de finalizacion de 2 horas desde agendamiento [cite: 42]
                    fechaEstimadaEntrega = ahora.AddHours(2);
                    break;

                default:
                    fechaEstimadaEntrega = ahora.AddHours(6);
                    break;
            }

            // Si una regra de restriccion no se cumplio, devolvemos al carrito con el aviso
            if (!string.IsNullOrEmpty(mensajeBloqueo))
            {
                TempData["Error"] = mensajeBloqueo;
                return RedirectToAction("Carrito");
            }

            VentaDAL dal = new VentaDAL();

            try
            {
                // Pasamos el tipo de envio y la fecha exacta calculada por el motor logico a la DAL
                bool exito = dal.GenerarOrdenCompletaConEnvio(idClienteFinal, idUsuario.Value, total, carrito, tipoEnvio, fechaEstimadaEntrega);

                if (exito)
                {
                    HttpContext.Session.Remove("CarritoVeguita");
                    return RedirectToAction("Confirmacion");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al procesar la compra en Oracle: {ex.Message}";
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