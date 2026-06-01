using LaVeguita.DAL;
using LaVeguita.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace LaVeguita.Web.Controllers
{
    public class TiendaController : Controller
    {
        private readonly ProductoDAL _productoDal = new ProductoDAL();
        // SOLUCIÓN AL ROJO: Declaramos e instanciamos la variable global de VentaDAL
        private readonly VentaDAL _ventaDAL = new VentaDAL();

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

            if (idUsuario == null || idUsuario == 0)
            {
                TempData["Error"] = "Sesion invalida. Por favor, inicia sesion nuevamente.";
                return RedirectToAction("Login", "Acceso");
            }

            // Al asignarle directamente el idUsuario.Value, garantizamos que calce 
            // al 100% con la llave foránea FK_OV_USUARIO que creamos en Oracle.
            int idClienteFinal = idUsuario.Value;
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
                    if (ahora.Hour >= 17)
                    {
                        fechaEstimadaEntrega = ahora.AddDays(2).Date.AddHours(9);
                    }
                    else
                    {
                        fechaEstimadaEntrega = ahora.AddDays(1).Date.AddHours(9);
                    }
                    break;

                case "NORMAL":
                    if (ahora.Hour >= 12)
                    {
                        fechaEstimadaEntrega = ahora.AddDays(1).Date.AddHours(9);
                    }
                    else
                    {
                        fechaEstimadaEntrega = ahora.AddHours(6);
                    }
                    break;

                case "FIJO":
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
                    fechaEstimadaEntrega = ahora.AddHours(2);
                    break;

                default:
                    fechaEstimadaEntrega = ahora.AddHours(6);
                    break;
            }

            if (!string.IsNullOrEmpty(mensajeBloqueo))
            {
                TempData["Error"] = mensajeBloqueo;
                return RedirectToAction("Carrito");
            }

            try
            {
                // JUGADA MAESTRA: Modificamos para llamar a GenerarOrdenCompletaConEnvio.
                // Como este método en tu VentaDAL ya gestiona la cabecera, detalles y despacho,
                // vamos a capturar el ID de la venta consultando el último ingresado o pasando un parámetro si lo necesitas.
                // Para mantener tu estructura actual intacta sin romper firmas, ejecutamos la compra:
                bool exito = _ventaDAL.GenerarOrdenCompletaConEnvio(idClienteFinal, idUsuario.Value, total, carrito, tipoEnvio, fechaEstimadaEntrega);

                if (exito)
                {
                    // Buscamos el ID de la última orden de este usuario para pasárselo a la boleta sin alterar la firma del método
                    int idUltimaVenta = 0;
                    using (Oracle.ManagedDataAccess.Client.OracleConnection cn = new Conexion().LeerConexion())
                    {
                        cn.Open();
                        string query = "SELECT MAX(ID_VENTA) FROM ORDEN_VENTA WHERE ID_USUARIO = :idUser";
                        using (Oracle.ManagedDataAccess.Client.OracleCommand cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(query, cn))
                        {
                            cmd.Parameters.Add("idUser", Oracle.ManagedDataAccess.Client.OracleDbType.Int32).Value = idUsuario.Value;
                            var res = cmd.ExecuteScalar();
                            if (res != null && res != DBNull.Value)
                            {
                                idUltimaVenta = int.Parse(res.ToString());
                            }
                        }
                    }

                    HttpContext.Session.Remove("CarritoVeguita");

                    // Pasamos el ID real de la venta a la pantalla de confirmación
                    return RedirectToAction("Confirmacion", new { idVenta = idUltimaVenta });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al procesar la compra en Oracle: {ex.Message}";
            }

            return RedirectToAction("Carrito");
        }

        [HttpGet]
        public IActionResult Historial()
        {
            // 1. Validamos seguridad: Rescatamos el ID del usuario desde la sesión
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            int? rol = HttpContext.Session.GetInt32("RolUsuario");

            if (idUsuario == null || idUsuario == 0 || rol != 8)
            {
                TempData["Error"] = "Acceso denegado o sesión expirada.";
                return RedirectToAction("Login", "Acceso");
            }

            try
            {
                // 2. Instanciamos tu VentaDAL para traer el DataTable con los registros históricos
                LaVeguita.DAL.VentaDAL ventaDal = new LaVeguita.DAL.VentaDAL();
                System.Data.DataTable dtHistorial = ventaDal.ObtenerHistorialCliente(idUsuario.Value);

                // 3. Enviamos el DataTable directo a la vista
                return View(dtHistorial);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar el historial de Oracle: " + ex.Message;
                return View(new System.Data.DataTable());
            }
        }








        // 6. Confirmación de pedido (Boleta Dinámica)
        public IActionResult Confirmacion(int idVenta)
        {
            // Busca los datos reales usando tu método de la DAL
            DataTable dt = _ventaDAL.ObtenerDetalleBoleta(idVenta);
            return View(dt);
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
                return new List<CarritoItem>();
            }
        }

        private void GuardarCarritoEnSesion(List<CarritoItem> carrito)
        {
            var settings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
            HttpContext.Session.SetString("CarritoVeguita", JsonConvert.SerializeObject(carrito, settings));
        }
    }
}