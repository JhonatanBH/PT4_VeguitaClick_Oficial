using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using LaVeguita.Entities;

namespace LaVeguita.DAL
{
    public class VentaDAL
    {
        private readonly Conexion _conexion = new Conexion();

        public bool GenerarOrdenCompleta(int idCliente, int idUsuario, decimal total, List<CarritoItem> detalles)
        {
            // Usamos tu método para obtener la conexión configurada con la Wallet
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                cn.Open();
                // Iniciamos una transacción: Si falla un solo producto, no se guarda nada
                using (OracleTransaction tr = cn.BeginTransaction())
                {
                    try
                    {
                        // 1. REGISTRAR CABECERA (Llamada al Package)
                        int idVentaGenerada;
                        using (OracleCommand cmdOrden = new OracleCommand("PKG_VENTAS.SP_REGISTRAR_ORDEN", cn))
                        {
                            cmdOrden.CommandType = CommandType.StoredProcedure;
                            cmdOrden.Parameters.Add("p_id_cliente", OracleDbType.Int32).Value = idCliente;
                            cmdOrden.Parameters.Add("p_id_usuario", OracleDbType.Int32).Value = idUsuario;
                            cmdOrden.Parameters.Add("p_total", OracleDbType.Decimal).Value = total;

                            // Parámetro de salida para recuperar el ID de la venta
                            OracleParameter pOutId = new OracleParameter("p_id_venta", OracleDbType.Int32);
                            pOutId.Direction = ParameterDirection.Output;
                            cmdOrden.Parameters.Add(pOutId);

                            cmdOrden.ExecuteNonQuery();
                            idVentaGenerada = int.Parse(pOutId.Value.ToString());
                        }

                        // 2. REGISTRAR CADA DETALLE
                        foreach (var item in detalles)
                        {
                            using (OracleCommand cmdDet = new OracleCommand("PKG_VENTAS.SP_REGISTRAR_DETALLE", cn))
                            {
                                cmdDet.CommandType = CommandType.StoredProcedure;
                                cmdDet.Parameters.Add("p_id_venta", OracleDbType.Int32).Value = idVentaGenerada;
                                cmdDet.Parameters.Add("p_id_producto", OracleDbType.Int32).Value = item.IdProducto;
                                cmdDet.Parameters.Add("p_cantidad", OracleDbType.Int32).Value = item.Cantidad;
                                cmdDet.Parameters.Add("p_precio", OracleDbType.Decimal).Value = item.Precio;

                                cmdDet.ExecuteNonQuery();
                            }
                        }

                        using (OracleCommand cmdDespacho = new OracleCommand("PKG_VENTAS.SP_ASIGNAR_DESPACHO", cn))
                        {
                            cmdDespacho.CommandType = CommandType.StoredProcedure;
                            cmdDespacho.Parameters.Add("p_id_venta", OracleDbType.Int32).Value = idVentaGenerada;

                            // Calculamos el peso total desde la lista de detalles del carrito
                            decimal pesoTotal = detalles.Sum(x => x.PesoSubtotal);
                            cmdDespacho.Parameters.Add("p_peso_total", OracleDbType.Decimal).Value = pesoTotal;

                            cmdDespacho.ExecuteNonQuery();
                        }


                        // Si todo salió bien, confirmamos en Oracle
                        tr.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        // Esto enviará el error real (ej: "Stock insuficiente") hacia arriba
                        throw new Exception("Error en Oracle: " + ex.Message);
                    }
                }
            }
        }

        public bool GenerarOrdenCompletaConEnvio(int idCliente, int idUsuario, decimal total, List<CarritoItem> detalles, string tipoEnvio, DateTime fechaEntrega)
        {
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                cn.Open();
                using (OracleTransaction tr = cn.BeginTransaction())
                {
                    try
                    {
                        // 1. REGISTRAR CABECERA DE VENTA
                        int idVentaGenerada;
                        using (OracleCommand cmdOrden = new OracleCommand("PKG_VENTAS.SP_REGISTRAR_ORDEN", cn))
                        {
                            cmdOrden.CommandType = CommandType.StoredProcedure;
                            cmdOrden.Parameters.Add("p_id_cliente", OracleDbType.Int32).Value = idCliente;
                            cmdOrden.Parameters.Add("p_id_usuario", OracleDbType.Int32).Value = idUsuario;
                            cmdOrden.Parameters.Add("p_total", OracleDbType.Decimal).Value = total;

                            OracleParameter pOutId = new OracleParameter("p_id_venta", OracleDbType.Int32);
                            pOutId.Direction = ParameterDirection.Output;
                            cmdOrden.Parameters.Add(pOutId);

                            cmdOrden.ExecuteNonQuery();
                            idVentaGenerada = int.Parse(pOutId.Value.ToString());
                        }

                        // 2. REGISTRAR CADA DETALLE DEL CARRITO
                        foreach (var item in detalles)
                        {
                            using (OracleCommand cmdDet = new OracleCommand("PKG_VENTAS.SP_REGISTRAR_DETALLE", cn))
                            {
                                cmdDet.CommandType = CommandType.StoredProcedure;
                                cmdDet.Parameters.Add("p_id_venta", OracleDbType.Int32).Value = idVentaGenerada;
                                cmdDet.Parameters.Add("p_id_producto", OracleDbType.Int32).Value = item.IdProducto;
                                cmdDet.Parameters.Add("p_cantidad", OracleDbType.Int32).Value = item.Cantidad;
                                cmdDet.Parameters.Add("p_precio", OracleDbType.Decimal).Value = item.Precio;

                                cmdDet.ExecuteNonQuery();
                            }
                        }

                        // 3. ASIGNAR DESPACHO CON SUS 2 PARAMETROS ORIGINALES (EVITA EL ERROR PLS-00306)
                        using (OracleCommand cmdDespacho = new OracleCommand("PKG_VENTAS.SP_ASIGNAR_DESPACHO", cn))
                        {
                            cmdDespacho.CommandType = CommandType.StoredProcedure;
                            cmdDespacho.Parameters.Add("p_id_venta", OracleDbType.Int32).Value = idVentaGenerada;

                            decimal pesoTotal = detalles.Sum(x => x.PesoSubtotal);
                            cmdDespacho.Parameters.Add("p_peso_total", OracleDbType.Decimal).Value = pesoTotal;

                            cmdDespacho.ExecuteNonQuery();
                        }

                        // 4. JUGADA MAESTRA: GUARDAR EL TIPO DE ENVIO Y LA FECHA ESTIMADA CON UN UPDATE DIRECTO
                        // Nota: Si tu tabla de cabecera se llama VENTAS en vez de ORDEN_VENTA, cambia el nombre aqui.
                        string sqlUpdate = "UPDATE ORDEN_VENTA SET TIPO_ENVIO = :tipo, FECHA_ESTIMADA = :fecha WHERE ID_VENTA = :id";
                        using (OracleCommand cmdUpd = new OracleCommand(sqlUpdate, cn))
                        {
                            cmdUpd.Parameters.Add("tipo", OracleDbType.Varchar2).Value = tipoEnvio;
                            cmdUpd.Parameters.Add("fecha", OracleDbType.Date).Value = fechaEntrega;
                            cmdUpd.Parameters.Add("id", OracleDbType.Int32).Value = idVentaGenerada;

                            cmdUpd.ExecuteNonQuery();
                        }

                        tr.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw new Exception("Error transaccional en el checkout de Oracle: " + ex.Message);
                    }
                }
            }
        }




    }
}