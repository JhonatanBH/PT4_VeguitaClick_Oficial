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
            ViewBag.Productos = _productoDal.ListarProductos();

            // Carga explícita y robusta de comunas desde Oracle
            List<dynamic> listaComunas = new List<dynamic>();
            try
            {
                using (OracleConnection cn = new Conexion().LeerConexion())
                {
                    cn.Open();
                    string query = "SELECT ID_COMUNA, NOMBRE_COMUNA FROM COMUNAS ORDER BY NOMBRE_COMUNA ASC";
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
                // Si la tabla se llama distinto (ej: COMUNA), dejamos Peñalolén y Santiago por defecto para el testeo local
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
        public IActionResult FinalizarVentaTradicional(string nombreCliente, string calle, string numero, int idComuna, string tipoEnvio)
        {
            int? idUsuarioOperador = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuarioOperador == null) return Json(new { exito = false, mensaje = "Sesión expirada." });

            var carrito = ObtenerCarritoAsistente();
            if (carrito == null || carrito.Count == 0) return Json(new { exito = false, mensaje = "El carrito de atención está vacío." });

            try
            {
                decimal totalVenta = carrito.Sum(x => x.Subtotal);
                tipoEnvio = tipoEnvio?.ToUpper() ?? "NORMAL";
                DateTime fechaEstimada = DateTime.Now.AddHours(6);

                bool exito = _ventaDal.GenerarOrdenCompletaConEnvio(
                    idUsuarioOperador.Value,
                    idUsuarioOperador.Value,
                    totalVenta,
                    carrito,
                    tipoEnvio,
                    fechaEstimada
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

                    HttpContext.Session.Remove("CarritoAsistente");
                    return Json(new { exito = true, redirectUrl = Url.Action("Confirmacion", "Tienda", new { idVenta = idUltimaVenta }) });
                }

                return Json(new { exito = false, mensaje = "Oracle rechazó la transacción distribuida." });
            }
            catch (Exception ex)
            {
                return Json(new { exito = false, mensaje = "Quiebre en Oracle: " + ex.Message });
            }
        }
    }
}