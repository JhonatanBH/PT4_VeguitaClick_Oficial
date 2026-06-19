using LaVeguita.DAL;
using LaVeguita.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace LaVeguita.Web.Controllers
{
    public class TiendaController : Controller
    {
        private readonly ProductoDAL _productoDal = new ProductoDAL();
        private readonly VentaDAL _ventaDAL = new VentaDAL();

        // 1. Catálogo de Productos (Soporta Búsqueda en Vivo por GET)
        public IActionResult Catalogo(string buscar)
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null)
            {
                HttpContext.Session.SetInt32("RolUsuario", 9);
                HttpContext.Session.SetString("UsuarioNombre", "Invitado");
                HttpContext.Session.SetInt32("IdUsuario", 0);
                rol = 9;
            }

            if (rol != 8 && rol != 9) return RedirectToAction("Login", "Acceso");

            // Traemos todos los productos desde Oracle Cloud
            var productos = _productoDal.ListarProductos();

            // 🚀 BÚSQUEDA INTEGRADA EN TIEMPO REAL
            if (!string.IsNullOrEmpty(buscar))
            {
                buscar = buscar.ToLower().Trim();
                // Filtramos por coincidencia en el nombre del producto o categoría
                productos = productos.Where(p =>
                    (p.NomProducto != null && p.NomProducto.ToLower().Contains(buscar)) ||
                    (p.Descripcion != null && p.Descripcion.ToLower().Contains(buscar)) ||
                    (p.NomTipo != null && p.NomTipo.ToLower().Contains(buscar))
                ).ToList();

                ViewBag.BusquedaMsg = $"Resultados para la búsqueda: '{buscar}'";
            }

            return View(productos);
        }

        // 🚀 NUEVA PANTALLA: ¿Quiénes Somos? (Con datos del Contexto del Caso)
        public IActionResult QuienesSomos()
        {
            return View();
        }

        // 🚀 NUEVA PANTALLA: Productos Estrella (Filtrado Premium)
        public IActionResult ProductosEstrella()
        {
            // Traemos los productos destacados de la base de datos (Calidad 2 = Premium)
            var destacados = _productoDal.ListarProductos().Where(p => p.IdCalidad == 2).ToList();
            return View(destacados);
        }

        // 2. Vista del Carrito de Compras
        public IActionResult Carrito()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 8 && rol != 9)) return RedirectToAction("Login", "Acceso");

            var carrito = ObtenerCarritoDeSesion();
            return View(carrito);
        }

        // 3. Agregar productos
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
                    PesoUnitario = peso,
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

        // 4. Ajustar cantidad
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

        // 5. Procesar compra en Oracle
        [HttpPost]
        public IActionResult FinalizarCompra(string tipoEnvio, string horaFijo, string calleInvitado, int? numeroInvitado, int? comunaInvitado, string rutInvitado, string correoInvitado, string nombreInvitado, string telefonoInvitado)
        {
            var carrito = ObtenerCarritoDeSesion();
            if (carrito == null || !carrito.Any())
            {
                TempData["Error"] = "El carrito esta vacio.";
                return RedirectToAction("Catalogo");
            }

            // Rescatamos los datos de sesión activos
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            // 🚀 BLINDAJE MODIFICADO: Solo bloqueamos si la sesión expiró por completo (null). 
            // Si idUsuario es 0, significa que es un Invitado legítimo y lo dejamos pasar.
            if (idUsuario == null)
            {
                TempData["Error"] = "Sesión inválida o expirada. Por favor, intente nuevamente.";
                return RedirectToAction("Login", "Acceso");
            }

            // Si es un cliente registrado, hereda su ID. Si es Invitado (0), viaja el 0 para que la DAL cree su cuenta express.
            int idClienteFinal = idUsuario.Value;
            int idUsuarioFinal = idUsuario.Value;
            decimal total = carrito.Sum(x => x.Subtotal);

            DateTime ahora = DateTime.Now;
            DateTime fechaEstimadaEntrega = ahora;
            string mensajeBloqueo = "";

            tipoEnvio = tipoEnvio?.ToUpper() ?? "NORMAL";

            switch (tipoEnvio)
            {
                case "ECONOMICO":
                    if (ahora.Hour >= 17) fechaEstimadaEntrega = ahora.AddDays(2).Date.AddHours(9);
                    else fechaEstimadaEntrega = ahora.AddDays(1).Date.AddHours(9);
                    break;

                case "NORMAL":
                    if (ahora.Hour >= 12) fechaEstimadaEntrega = ahora.AddDays(1).Date.AddHours(9);
                    else fechaEstimadaEntrega = TrackedTimeHelper(ahora); // Mantiene tu lógica de 6 horas
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
                // 🚀 PASO DE PARÁMETROS: Enviamos todos los canales capturados del formulario express a la VentaDAL
                bool exito = _ventaDAL.GenerarOrdenCompletaConEnvio(
                    idClienteFinal,
                    idUsuarioFinal,
                    total,
                    carrito,
                    tipoEnvio,
                    fechaEstimadaEntrega,
                    calleInvitado,
                    numeroInvitado,
                    comunaInvitado,
                    rutInvitado,
                    correoInvitado,
                    nombreInvitado,
                    telefonoInvitado
                );

                if (exito)
                {
                    int idUltimaVenta = 0;
                    using (Oracle.ManagedDataAccess.Client.OracleConnection cn = new Conexion().LeerConexion())
                    {
                        cn.Open();

                        // JUGADA MAESTRA: Si era un invitado express (idUsuario == 0), buscamos la última venta global ingresada al sistema.
                        // Si es un cliente registrado, buscamos por su ID específico. Esto evita que el buscador devuelva un ID 0.
                        string query = idUsuarioFinal == 0
                            ? "SELECT MAX(ID_VENTA) FROM ORDEN_VENTA"
                            : "SELECT MAX(ID_VENTA) FROM ORDEN_VENTA WHERE ID_USUARIO = :idUser";

                        using (Oracle.ManagedDataAccess.Client.OracleCommand cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(query, cn))
                        {
                            if (idUsuarioFinal != 0)
                            {
                                cmd.Parameters.Add("idUser", Oracle.ManagedDataAccess.Client.OracleDbType.Int32).Value = idUsuarioFinal;
                            }

                            var res = cmd.ExecuteScalar();
                            if (res != null && res != DBNull.Value)
                            {
                                idUltimaVenta = int.Parse(res.ToString());
                            }
                        }
                    }

                    // Limpiamos la sesión del carrito de compras tras el éxito de la venta
                    HttpContext.Session.Remove("CarritoVeguita");

                    // Redireccionamos directamente a la boleta dinámica
                    return RedirectToAction("Confirmacion", new { idVenta = idUltimaVenta });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al procesar la compra en Oracle: {ex.Message}";
            }

            return RedirectToAction("Carrito");
        }

        // Método de apoyo para el cálculo de horas
        private DateTime TrackedTimeHelper(DateTime ahora) { return ahora.AddHours(6); }

        [HttpGet]
        public IActionResult Historial()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            int? rol = HttpContext.Session.GetInt32("RolUsuario");

            if (idUsuario == null || idUsuario == 0 || rol != 8)
            {
                TempData["Error"] = "Acceso denegado o sesión expirada.";
                return RedirectToAction("Login", "Acceso");
            }

            try
            {
                LaVeguita.DAL.VentaDAL ventaDal = new LaVeguita.DAL.VentaDAL();
                System.Data.DataTable dtHistorial = ventaDal.ObtenerHistorialCliente(idUsuario.Value);
                return View(dtHistorial);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar el historial de Oracle: " + ex.Message;
                return View(new System.Data.DataTable());
            }
        }

        // 6. Confirmación de pedido
        public IActionResult Confirmacion(int idVenta)
        {
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


        [HttpPost]
        public IActionResult RegistrarSolicitudAyuda(string nombre, string rut, string telefono, string mensaje)
        {
            try
            {
                string asunto = $"SOPORTE_WEB - RUT: {rut} - Cliente: {nombre}";

                using (OracleConnection cn = new Conexion().LeerConexion())
                {
                    cn.Open();
                    string query = "INSERT INTO ADMIN.LOG_NOTIFICACIONES (DESTINATARIO, ASUNTO, CUERPO, TELEFONO_CONTACTO, ESTADO) VALUES (:dest, :asunto, :cuerpo, :fono, 'SOPORTE_PENDIENTE')";
                    using (OracleCommand cmd = new OracleCommand(query, cn))
                    {
                        cmd.Parameters.Add("dest", OracleDbType.Varchar2).Value = "MÓDULO_COMERCIAL";
                        cmd.Parameters.Add("asunto", OracleDbType.Varchar2).Value = asunto;
                        cmd.Parameters.Add("cuerpo", OracleDbType.Clob).Value = mensaje;
                        cmd.Parameters.Add("fono", OracleDbType.Varchar2).Value = telefono;

                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { exito = true });
            }
            catch (Exception ex)
            {
                return Json(new { exito = false, mensaje = ex.Message });
            }
        }






        [HttpGet]
        public IActionResult Seguimiento(string rut)
        {
            DataTable dtPedidos = new DataTable();

            if (!string.IsNullOrEmpty(rut))
            {
                ViewBag.RutBuscado = rut.Trim();

                // Query limpio para buscar todas las órdenes del RUT ingresado uniendo con DESPACHO para ver los estados reales de Oracle
                string query = @"SELECT o.ID_VENTA, o.FECHA_VENTA, o.TOTAL, o.TIPO_ENVIO, 
                                        NVL(d.ESTADO_ENTREGA, 'EN PREPARACION') AS ESTADO_ENTREGA
                                 FROM ADMIN.ORDEN_VENTA o
                                 JOIN ADMIN.CLIENTE c ON o.ID_CLIENTE = c.ID_CLIENTE
                                 LEFT JOIN ADMIN.DESPACHO d ON o.ID_VENTA = d.ID_VENTA
                                 WHERE c.RUT_CLI = :rut
                                 ORDER BY o.FECHA_VENTA DESC";

                using (Oracle.ManagedDataAccess.Client.OracleConnection cn = new Conexion().LeerConexion())
                {
                    using (Oracle.ManagedDataAccess.Client.OracleCommand cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(query, cn))
                    {
                        cmd.Parameters.Add("rut", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2).Value = rut.Trim();
                        using (Oracle.ManagedDataAccess.Client.OracleDataAdapter da = new Oracle.ManagedDataAccess.Client.OracleDataAdapter(cmd))
                        {
                            da.Fill(dtPedidos);
                        }
                    }
                }
            }

            return View(dtPedidos);
        }







    }
}