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

        public bool GenerarOrdenCompletaConEnvio(int idCliente, int idUsuario, decimal total, List<CarritoItem> detalles, string tipoEnvio, DateTime fechaEntrega, string calleInvitado = null, int? numeroInvitado = null, int? comunaInvitado = null, string rutInvitado = null, string correoInvitado = null, string nombreInvitado = null, string telefonoInvitado = null)
        {
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                cn.Open();
                using (OracleTransaction tr = cn.BeginTransaction())
                {
                    try
                    {
                        int idUsuarioFinal = idUsuario;
                        int idClienteFinal = idCliente;

                        // ====================================================================
                        // 🌿 PROCESO EN CALIENTE PARA EL COMPRADOR (Invitado o Tradicional Presencial)
                        // JUGADA MAESTRA: Si viaja una calleInvitado, forzamos el registro del cliente
                        // independientemente de si el ID que gatilla el flujo viene de un operador.
                        // ====================================================================
                        if (!string.IsNullOrEmpty(calleInvitado))
                        {
                            int idDireccionGenerada = 0;

                            // Paso A: Insertar Dirección usando el IDENTITY nativo de Oracle
                            string queryDir = "INSERT INTO ADMIN.DIRECCION (NOMBRE_DIR, NUMERO_DIR, ID_COMUNA) VALUES (:nom, :num, :com) RETURNING ID_DIRECCION INTO :idOut";
                            using (OracleCommand cmdDir = new OracleCommand(queryDir, cn))
                            {
                                cmdDir.Parameters.Add("nom", OracleDbType.Varchar2).Value = calleInvitado;
                                cmdDir.Parameters.Add("num", OracleDbType.Int32).Value = numeroInvitado ?? 0;
                                cmdDir.Parameters.Add("com", OracleDbType.Int32).Value = comunaInvitado ?? 1;

                                OracleParameter pOutDir = new OracleParameter("idOut", OracleDbType.Int32, ParameterDirection.Output);
                                cmdDir.Parameters.Add(pOutDir);

                                cmdDir.ExecuteNonQuery();
                                idDireccionGenerada = int.Parse(pOutDir.Value.ToString());
                            }

                            // Paso B: Solicitar el correlativo del usuario a tu secuencia actual
                            int idNuevoUsuario = 0;
                            string querySeq = "SELECT ADMIN.SEQ_USUARIO.NEXTVAL FROM DUAL";
                            using (OracleCommand cmdSeq = new OracleCommand(querySeq, cn))
                            {
                                idNuevoUsuario = Convert.ToInt32(cmdSeq.ExecuteScalar());
                            }

                            // Paso C: Crear la cuenta express de Invitado guardando sus canales reales
                            string queryUser = "INSERT INTO ADMIN.USUARIOS (ID_USUARIO, NOMBRE_USER, CONTRASENA, FONO, CORREO_USU, ID_ROL_USUARIO, ID_DIRECCION) " +
                                               "VALUES (:id, :username, 'invitado123', :fono, :correo, 9, :idDir)";
                            using (OracleCommand cmdUser = new OracleCommand(queryUser, cn))
                            {
                                cmdUser.Parameters.Add("id", OracleDbType.Int32).Value = idNuevoUsuario;
                                cmdUser.Parameters.Add("username", OracleDbType.Varchar2).Value = !string.IsNullOrEmpty(nombreInvitado) ? nombreInvitado.Trim() : "cliente_" + idNuevoUsuario;
                                cmdUser.Parameters.Add("fono", OracleDbType.Int32).Value = !string.IsNullOrEmpty(telefonoInvitado) ? int.Parse(telefonoInvitado) : 99999999;
                                cmdUser.Parameters.Add("correo", OracleDbType.Varchar2).Value = !string.IsNullOrEmpty(correoInvitado) ? correoInvitado.Trim() : "presencial@veguita.cl";
                                cmdUser.Parameters.Add("idDir", OracleDbType.Int32).Value = idDireccionGenerada;

                                cmdUser.ExecuteNonQuery();
                            }

                            // Paso D: Registrar el RUT en la tabla CLIENTE para las búsquedas de seguimiento de despacho
                            string queryCliente = "INSERT INTO ADMIN.CLIENTE (ID_CLIENTE, RUT_CLI, ID_USUARIO, ID_DIRECCION) " +
                                                  "VALUES (:idCli, :rut, :idUsu, :idDir)";
                            using (OracleCommand cmdCli = new OracleCommand(queryCliente, cn))
                            {
                                cmdCli.Parameters.Add("idCli", OracleDbType.Int32).Value = idNuevoUsuario;
                                cmdCli.Parameters.Add("rut", OracleDbType.Varchar2).Value = !string.IsNullOrEmpty(rutInvitado) ? rutInvitado.Trim() : "11111111-1";
                                cmdCli.Parameters.Add("idUsu", OracleDbType.Int32).Value = idNuevoUsuario;
                                cmdCli.Parameters.Add("idDir", OracleDbType.Int32).Value = idDireccionGenerada;

                                cmdCli.ExecuteNonQuery();
                            }

                            idClienteFinal = idNuevoUsuario;
                            // Mantenemos que la cabecera sea dueña de la cuenta generada, 
                            // pero si idUsuario original correspondía a un operador interno, 
                            // la orden quedará linkeada a este último para mantener la auditoría de ventas.
                            if (idUsuario == 0)
                            {
                                idUsuarioFinal = idNuevoUsuario;
                            }
                        }

                        // ====================================================================
                        // 1. REGISTRAR CABECERA DE VENTA (Usa tu firma original de Package)
                        // ====================================================================
                        int idVentaGenerada;
                        using (OracleCommand cmdOrden = new OracleCommand("PKG_VENTAS.SP_REGISTRAR_ORDEN", cn))
                        {
                            cmdOrden.CommandType = CommandType.StoredProcedure;
                            cmdOrden.Parameters.Add("p_id_cliente", OracleDbType.Int32).Value = idClienteFinal;
                            cmdOrden.Parameters.Add("p_id_usuario", OracleDbType.Int32).Value = idUsuarioFinal;
                            cmdOrden.Parameters.Add("p_total", OracleDbType.Decimal).Value = total;

                            OracleParameter pOutId = new OracleParameter("p_id_venta", OracleDbType.Int32);
                            pOutId.Direction = ParameterDirection.Output;
                            cmdOrden.Parameters.Add(pOutId);

                            cmdOrden.ExecuteNonQuery();
                            idVentaGenerada = int.Parse(pOutId.Value.ToString());
                        }

                        // ====================================================================
                        // 2. REGISTRAR CADA DETALLE DEL CARRITO
                        // ====================================================================
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

                        // ====================================================================
                        // 3. ASIGNAR DESPACHO CON SUS 2 PARAMETROS ORIGINALES 
                        // ====================================================================
                        using (OracleCommand cmdDespacho = new OracleCommand("PKG_VENTAS.SP_ASIGNAR_DESPACHO", cn))
                        {
                            cmdDespacho.CommandType = CommandType.StoredProcedure;
                            cmdDespacho.Parameters.Add("p_id_venta", OracleDbType.Int32).Value = idVentaGenerada;

                            decimal pesoTotal = detalles.Sum(x => x.PesoSubtotal);
                            cmdDespacho.Parameters.Add("p_peso_total", OracleDbType.Decimal).Value = pesoTotal;

                            cmdDespacho.ExecuteNonQuery();
                        }

                        // ====================================================================
                        // 4. GUARDAR EL TIPO DE ENVIO Y LA FECHA ESTIMADA CON EL UPDATE DIRECTO
                        // ====================================================================
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

        public DataTable ListarOrdenesEnPreparacion()
        {
            DataTable dt = new DataTable();
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                // Unimos ORDEN_VENTA con DESPACHO para mostrar la info completa al bodeguero
                string query = @"SELECT v.ID_VENTA, v.FECHA_VENTA, v.TOTAL, v.TIPO_ENVIO, d.ESTADO_ENTREGA, d.PESO_TOTAL_KG 
                                 FROM ORDEN_VENTA v
                                 JOIN DESPACHO d ON v.ID_VENTA = d.ID_VENTA
                                 WHERE d.ESTADO_ENTREGA = 'EN PREPARACION'
                                 ORDER BY v.FECHA_VENTA DESC";

                using (OracleCommand cmd = new OracleCommand(query, cn))
                {
                    using (OracleDataAdapter da = new OracleDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }

        // 2. Cambiar el estado de la entrega a "DESPACHADO"
        public bool MarcarComoDespachado(int idVenta)
        {
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                cn.Open();
                string query = "UPDATE DESPACHO SET ESTADO_ENTREGA = 'DESPACHADO' WHERE ID_VENTA = :idVenta";
                using (OracleCommand cmd = new OracleCommand(query, cn))
                {
                    cmd.Parameters.Add("idVenta", OracleDbType.Int32).Value = idVenta;
                    int filasAfectadas = cmd.ExecuteNonQuery();
                    return filasAfectadas > 0;
                }
            }
        }




        // 3. Obtener los datos de la boleta (Cabecera y detalles unidos)
        public DataTable ObtenerDetalleBoleta(int idVenta)
        {
            DataTable dt = new DataTable();
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                // SQL TOTALMENTE AJUSTADO: Agregamos v.TIPO_ENVIO de vuelta
                string query = @"SELECT v.ID_VENTA, 
                                v.FECHA_VENTA, 
                                v.TOTAL, 
                                v.TIPO_ENVIO, 
                                d.CANTIDAD, 
                                d.PRECIO, 
                                (d.CANTIDAD * d.PRECIO) AS SUBTOTAL,
                                p.NOM_PRODUCTO AS NOMBRE_PROD
                         FROM ORDEN_VENTA v
                         JOIN DETALLE_VENTA d ON v.ID_VENTA = d.ID_VENTA
                         JOIN PRODUCTOS p ON d.ID_PRODUCTO = p.ID_PRODUCTO
                         WHERE v.ID_VENTA = :idVenta";

                using (OracleCommand cmd = new OracleCommand(query, cn))
                {
                    cmd.Parameters.Add("idVenta", OracleDbType.Int32).Value = idVenta;
                    using (OracleDataAdapter da = new OracleDataAdapter(cmd))
                    {
                        try
                        {
                            cn.Open();
                            da.Fill(dt);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Error al rellenar el DataTable de la boleta: " + ex.Message);
                        }
                    }
                }
            }
            return dt;
        }

        public DataTable ObtenerHistorialCliente(int idUsuario)
        {
            DataTable dt = new DataTable();
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                // Usamos LEFT JOIN para que si el despacho aún no se crea o no se asigna, 
                // el pedido aparezca de todas formas en el historial del cliente.
                string query = @"SELECT v.ID_VENTA, v.FECHA_VENTA, v.TOTAL, v.TIPO_ENVIO, 
                                NVL(d.ESTADO_ENTREGA, 'EN ESPERA DE PAGO') AS ESTADO_ENTREGA
                         FROM ORDEN_VENTA v
                         LEFT JOIN DESPACHO d ON v.ID_VENTA = d.ID_VENTA
                         WHERE v.ID_USUARIO = :idUsu
                         ORDER BY v.FECHA_VENTA DESC";

                using (OracleCommand cmd = new OracleCommand(query, cn))
                {
                    cmd.Parameters.Add("idUsu", OracleDbType.Int32).Value = idUsuario;
                    using (OracleDataAdapter da = new OracleDataAdapter(cmd))
                    {
                        try
                        {
                            cn.Open();
                            da.Fill(dt);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Error al obtener historial del cliente: " + ex.Message);
                        }
                    }
                }
            }
            return dt;
        }




    }
}



