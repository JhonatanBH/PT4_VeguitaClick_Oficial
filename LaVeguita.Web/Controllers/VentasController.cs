using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using LaVeguita.DAL;
using LaVeguita.Entities;

namespace LaVeguita.Web.Controllers
{
    public class VentasController : Controller
    {
        private readonly VentaDAL _ventaDal = new VentaDAL();
        private readonly ProductoDAL _productoDal = new ProductoDAL();

        private List<CarritoItem> ObtenerCarritoAsistente()
        {
            var json = HttpContext.Session.GetString("CarritoAsistente");
            return json == null ? new List<CarritoItem>() : JsonSerializer.Deserialize<List<CarritoItem>>(json);
        }

        private void GuardarCarritoAsistente(List<CarritoItem> carrito)
        {
            HttpContext.Session.SetString("CarritoAsistente", JsonSerializer.Serialize(carrito));
        }

        [HttpGet]
        public IActionResult AsistenteVentas()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 2 && rol != 5))
            {
                return RedirectToAction("Login", "Acceso");
            }

            HttpContext.Session.Remove("CarritoAsistente");
            HttpContext.Session.Remove("ProcesandoVentaAsistente"); // Limpieza preventiva
            ViewBag.Productos = _productoDal.ListarProductos();

            List<dynamic> listaComunas = new List<dynamic>();
            try
            {
                using (OracleConnection cn = new Conexion().LeerConexion())
                {
                    cn.Open();
                    string query = "SELECT ID_COMUNA, NOMBRE AS NOMBRE_COMUNA FROM COMUNAS ORDER BY NOMBRE ASC";
                    using (OracleCommand cmd = new OracleCommand(query, cn))
                    {
                        using (OracleDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                listaComunas.Add(new
                                {
                                    IdComuna = dr["ID_COMUNA"].ToString(),
                                    NombreComuna = dr["NOMBRE_COMUNA"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                listaComunas.Add(new { IdComuna = "1", NombreComuna = "Peñalolén (Local)" });
                listaComunas.Add(new { IdComuna = "2", NombreComuna = "Santiago Centro (Local)" });
                listaComunas.Add(new { IdComuna = "3", NombreComuna = "Providencia (Local)" });
                System.Diagnostics.Debug.WriteLine("Error Oracle Comunas: " + ex.Message);
            }

            ViewBag.Comunas = listaComunas;
            return View();
        }

        [HttpPost]
        public IActionResult VerificarYAñadir(int idProducto, int cantidad)
        {
            try
            {
                var producto = _productoDal.ObtenerPorId(idProducto);
                if (producto == null) return Json(new { exito = false, mensaje = "Producto no encontrado." });

                var carrito = ObtenerCarritoAsistente();
                var itemExistente = carrito.FirstOrDefault(x => x.IdProducto == idProducto);
                int cantidadTotalEvaluar = cantidad + (itemExistente?.Cantidad ?? 0);

                int stockSeguro = producto.StockActual - 5;

                if (cantidadTotalEvaluar > stockSeguro)
                {
                    int disponibles = stockSeguro < 0 ? 0 : stockSeguro;
                    return Json(new { exito = false, mensaje = $"Rechazado por Stock Crítico. Máximo disponible para venta externa: {disponibles} unidades." });
                }

                if (itemExistente == null)
                {
                    carrito.Add(new CarritoItem
                    {
                        IdProducto = idProducto,
                        Nombre = producto.NomProducto,
                        Precio = producto.PrecioUnd,
                        PesoUnitario = producto.PesoUnitEstimado,
                        Cantidad = cantidad
                    });
                }
                else
                {
                    itemExistente.Cantidad += cantidad;
                }

                GuardarCarritoAsistente(carrito);
                return Json(new { exito = true, mensaje = "Añadido", items = carrito, total = carrito.Sum(x => x.Subtotal) });
            }
            catch (Exception ex)
            {
                return Json(new { exito = false, mensaje = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult QuitarItem(int idProducto)
        {
            var carrito = ObtenerCarritoAsistente();
            carrito.RemoveAll(x => x.IdProducto == idProducto);
            GuardarCarritoAsistente(carrito);
            return Json(new { exito = true, items = carrito, total = carrito.Sum(x => x.Subtotal) });
        }

        [HttpPost]
        public IActionResult FinalizarVentaTradicional(string nombreCliente, string rut, string calle, string numero, int idComuna, string tipoEnvio)
        {
            // 1. Validar al operador logueado
            int? idUsuarioOperador = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuarioOperador == null)
                return Json(new { exito = false, mensaje = "Sesión expirada. Debe reautenticarse." });

            // 🚀 BLINDAJE ATÓMICO: Evita ejecuciones duplicadas en paralelo en la BD
            if (HttpContext.Session.GetString("ProcesandoVentaAsistente") == "true")
            {
                return Json(new { exito = false, mensaje = "Ya existe una transacción en curso para esta orden. Por favor, espere." });
            }
            HttpContext.Session.SetString("ProcesandoVentaAsistente", "true");

            // 2. Controlar estado de la canasta
            var carrito = ObtenerCarritoAsistente();
            if (carrito == null || carrito.Count == 0)
            {
                HttpContext.Session.Remove("ProcesandoVentaAsistente");
                return Json(new { exito = false, mensaje = "El carrito de atención comercial está vacío." });
            }

            try
            {
                decimal totalVenta = carrito.Sum(x => x.Subtotal);
                tipoEnvio = tipoEnvio?.ToUpper() ?? "NORMAL";
                DateTime fechaEstimada = tipoEnvio == "URGENTE" ? DateTime.Now.AddHours(2) : DateTime.Now.AddHours(6);

                // 3. Ejecución de la lógica en la capa de datos (Oracle Cloud)
                bool exito = _ventaDal.GenerarOrdenCompletaConEnvio(
                    0,
                    idUsuarioOperador.Value,
                    totalVenta,
                    carrito,
                    tipoEnvio,
                    fechaEstimada,
                    calle,
                    int.Parse(numero),
                    idComuna,
                    rut,
                    "presencial@veguita.cl",
                    nombreCliente,
                    "999999999"
                );

                if (exito)
                {
                    int idUltimaVenta = 0;
                    using (OracleConnection cn = new Conexion().LeerConexion())
                    {
                        cn.Open();
                        string query = "SELECT MAX(ID_VENTA) FROM ORDEN_VENTA WHERE ID_USUARIO = :idUser";
                        using (OracleCommand cmd = new OracleCommand(query, cn))
                        {
                            cmd.Parameters.Add("idUser", OracleDbType.Int32).Value = idUsuarioOperador.Value;
                            var res = cmd.ExecuteScalar();
                            if (res != null) idUltimaVenta = Convert.ToInt32(res);
                        }
                    }

                    // Limpieza de estados y estructuras temporales por éxito
                    HttpContext.Session.Remove("CarritoAsistente");
                    HttpContext.Session.Remove("ProcesandoVentaAsistente");

                    return Json(new { exito = true, redirectUrl = Url.Action("Confirmacion", "Tienda", new { idVenta = idUltimaVenta }) });
                }

                HttpContext.Session.Remove("ProcesandoVentaAsistente");
                return Json(new { exito = false, mensaje = "Oracle rechazó la transacción distribuida por quiebre de stock o mermas." });
            }
            catch (Exception ex)
            {
                // En caso de error, liberamos el candado para permitir reintentos al operador
                HttpContext.Session.Remove("ProcesandoVentaAsistente");
                return Json(new { exito = false, mensaje = "Quiebre en base de datos Oracle Cloud: " + ex.Message });
            }
        } // 🌟 ¡LLAVE CORRECTAMENTE COLOCADA AQUÍ PARA CERRAR EL MÉTODO!

        [HttpGet]
        public IActionResult ContarMensajesInternos()
        {
            int totalInternos = 0;
            int? rolActual = HttpContext.Session.GetInt32("RolUsuario");

            if (rolActual == null) return Json(new { count = 0 });

            try
            {
                using (OracleConnection cn = new Conexion().LeerConexion())
                {
                    cn.Open();
                    // Cuenta cuántos mensajes han sido dirigidos estrictamente al rol de la sesión actual
                    string query = "SELECT COUNT(*) FROM ADMIN.MENSAJES_INTERNOS WHERE ID_ROL_DESTINO = :rol AND ESTADO = 'NO_LEIDO'";
                    using (OracleCommand cmd = new OracleCommand(query, cn))
                    {
                        cmd.Parameters.Add("rol", OracleDbType.Int32).Value = rolActual.Value;
                        totalInternos = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch { /* Silencioso para evitar caídas visuales */ }

            return Json(new { count = totalInternos });
        }

        [HttpGet]
        public IActionResult BandejaSoporte()
        {
            int? rolActual = HttpContext.Session.GetInt32("RolUsuario");
            if (rolActual == null) return RedirectToAction("Login", "Acceso");

            List<dynamic> alertasClientes = new List<dynamic>();
            List<dynamic> chatsInternos = new List<dynamic>();

            using (OracleConnection cn = new Conexion().LeerConexion())
            {
                cn.Open();

                // 🚀 NUEVO: MARCAR MENSAJES COMO LEÍDOS AUTOMÁTICAMENTE AL ENTRAR A LA VISTA
                string qUpdateInternos = "UPDATE ADMIN.MENSAJES_INTERNOS SET ESTADO = 'LEIDO' WHERE ID_ROL_DESTINO = :rol AND ESTADO = 'NO_LEIDO'";
                using (OracleCommand cmdUpd = new OracleCommand(qUpdateInternos, cn))
                {
                    cmdUpd.Parameters.Add("rol", OracleDbType.Int32).Value = rolActual.Value;
                    cmdUpd.ExecuteNonQuery();
                }

                // A. Cargar alertas de clientes web
                string qClientes = "SELECT ID_LOG, ASUNTO, CUERPO, TELEFONO_CONTACTO, FECHA_REGISTRO FROM ADMIN.LOG_NOTIFICACIONES WHERE ESTADO = 'SOPORTE_PENDIENTE' ORDER BY FECHA_REGISTRO DESC";
                using (OracleCommand cmd = new OracleCommand(qClientes, cn))
                using (OracleDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        alertasClientes.Add(new
                        {
                            Id = dr["ID_LOG"],
                            Asunto = dr["ASUNTO"].ToString(),
                            Cuerpo = dr["CUERPO"].ToString(),
                            Fono = dr["TELEFONO_CONTACTO"].ToString(),
                            Fecha = Convert.ToDateTime(dr["FECHA_REGISTRO"]).ToString("dd/MM HH:mm")
                        });
                    }
                }

                // B. Cargar mensajes internos dirigidos al rol logueado actual
                string qInternos = "SELECT NOM_EMISOR, TEXTO_MENSAJE, FECHA_ENVIO, ID_ROL_ORIGEN FROM ADMIN.MENSAJES_INTERNOS WHERE ID_ROL_DESTINO = :rol ORDER BY FECHA_ENVIO DESC";
                using (OracleCommand cmd = new OracleCommand(qInternos, cn))
                {
                    cmd.Parameters.Add("rol", OracleDbType.Int32).Value = rolActual.Value;
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            chatsInternos.Add(new
                            {
                                Emisor = dr["NOM_EMISOR"].ToString(),
                                Mensaje = dr["TEXTO_MENSAJE"].ToString(),
                                Fecha = Convert.ToDateTime(dr["FECHA_ENVIO"]).ToString("dd/MM HH:mm"),
                                RolOrigen = Convert.ToInt32(dr["ID_ROL_ORIGEN"])
                            });
                        }
                    }
                }
            }

            ViewBag.AlertasClientes = alertasClientes;
            ViewBag.ChatsInternos = chatsInternos;
            return View();
        }

        // 2. Procesar el envío de un mensaje interno de rol a rol
        [HttpPost]
        public IActionResult EnviarMensajeInterno(int rolDestino, string mensaje)
        {
            int? rolOrigen = HttpContext.Session.GetInt32("RolUsuario");
            string nombreEmisor = HttpContext.Session.GetString("UsuarioNombre") ?? "Empleado";

            if (rolOrigen == null || string.IsNullOrEmpty(mensaje))
                return Json(new { exito = false, mensaje = "Datos invalidos." });

            try
            {
                // Limpieza preventiva de caracteres peligrosos o tildes para cumplir la restricción del caso
                string msgLimpio = mensaje.Normalize(System.Text.NormalizationForm.FormD)
                                          .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u");

                using (OracleConnection cn = new Conexion().LeerConexion())
                {
                    cn.Open();
                    string query = "INSERT INTO ADMIN.MENSAJES_INTERNOS (ID_ROL_ORIGEN, NOM_EMISOR, ID_ROL_DESTINO, TEXTO_MENSAJE) VALUES (:orig, :nom, :dest, :msg)";
                    using (OracleCommand cmd = new OracleCommand(query, cn))
                    {
                        cmd.Parameters.Add("orig", OracleDbType.Int32).Value = rolOrigen.Value;
                        cmd.Parameters.Add("nom", OracleDbType.Varchar2).Value = nombreEmisor;
                        cmd.Parameters.Add("dest", OracleDbType.Int32).Value = rolDestino;
                        cmd.Parameters.Add("msg", OracleDbType.Varchar2).Value = msgLimpio;
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

        // 3. Resolver o dar por cerrado un SOS de cliente
        [HttpPost]
        public IActionResult ResolverSoporteCliente(int idLog)
        {
            try
            {
                using (OracleConnection cn = new Conexion().LeerConexion())
                {
                    cn.Open();
                    string query = "UPDATE ADMIN.LOG_NOTIFICACIONES SET ESTADO = 'RESOLVIDO' WHERE ID_LOG = :id";
                    using (OracleCommand cmd = new OracleCommand(query, cn))
                    {
                        cmd.Parameters.Add("id", OracleDbType.Int32).Value = idLog;
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

        private void EnviarCorreoConfirmacion(string destinatario, int idVenta, decimal total)
        {
            try
            {
                string asunto = $"🌿 Confirmacion de Pedido #{idVenta} - La VeguitaClick";
                string cuerpo = $"Gracias por tu compra. Tu orden #{idVenta} por ${total:N0} esta siendo procesada bajo los altos estandares de inclusion de La VeguitaClick.";

                // Inserción directa en la cola de mensajería de Oracle Cloud
                using (OracleConnection cn = new Conexion().LeerConexion())
                {
                    cn.Open();
                    string query = "INSERT INTO ADMIN.LOG_NOTIFICACIONES (DESTINATARIO, ASUNTO, CUERPO, ESTADO) VALUES (:dest, :asunto, :cuerpo, 'ENVIADO_MOCK')";
                    using (OracleCommand cmd = new OracleCommand(query, cn))
                    {
                        cmd.Parameters.Add("dest", OracleDbType.Varchar2).Value = destinatario;
                        cmd.Parameters.Add("asunto", OracleDbType.Varchar2).Value = asunto;
                        cmd.Parameters.Add("cuerpo", OracleDbType.Clob).Value = cuerpo;

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error en Cola de Mensajería Oracle: " + ex.Message);
            }
        }
    }
}