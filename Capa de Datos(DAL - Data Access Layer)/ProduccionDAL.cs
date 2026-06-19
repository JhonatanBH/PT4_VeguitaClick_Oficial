using System;
using System.Data;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;

namespace LaVeguita.DAL
{
    public class ProduccionDAL
    {
        // =========================================================
        // 1. LAVADO Y CLASIFICACIÓN (PROCESOS DE PLANTA)
        // =========================================================
        public bool RegistrarFlujoProduccion(int idLote, int cantEntrada, int cantPrimCalidad, int cantSegCalidad, int idProducto, int idEmpleado, int idProveedor, out string mensajeResultado)
        {
            bool exitoTransaccion = false;
            mensajeResultado = string.Empty;

            try
            {
                using (OracleConnection cn = new Conexion().LeerConexion())
                {
                    using (OracleCommand cmd = new OracleCommand("ADMIN.PKG_PRODUCCION.SP_REGISTRAR_PRODUCCION", cn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("p_id_lote", OracleDbType.Int32).Value = idLote;
                        cmd.Parameters.Add("p_cant_entrada", OracleDbType.Int32).Value = cantEntrada;
                        cmd.Parameters.Add("p_cant_prim_calidad", OracleDbType.Int32).Value = cantPrimCalidad;
                        cmd.Parameters.Add("p_cant_seg_calidad", OracleDbType.Int32).Value = cantSegCalidad;
                        cmd.Parameters.Add("p_id_producto", OracleDbType.Int32).Value = idProducto;
                        cmd.Parameters.Add("p_id_empleado", OracleDbType.Int32).Value = idEmpleado;
                        cmd.Parameters.Add("p_id_proveedor", OracleDbType.Int32).Value = idProveedor;

                        OracleParameter pExito = new OracleParameter("p_exito", OracleDbType.Int32, ParameterDirection.Output);
                        OracleParameter pMensaje = new OracleParameter("p_mensaje", OracleDbType.Varchar2, 200, null, ParameterDirection.Output);

                        cmd.Parameters.Add(pExito);
                        cmd.Parameters.Add(pMensaje);

                        cn.Open();
                        cmd.ExecuteNonQuery();

                        int resultado = Convert.ToInt32(pExito.Value.ToString());
                        mensajeResultado = pMensaje.Value.ToString();

                        if (resultado == 1) exitoTransaccion = true;
                    }
                }
            }
            catch (Exception ex)
            {
                exitoTransaccion = false;
                mensajeResultado = "Error crítico en la capa DAL: " + ex.Message;
            }

            return exitoTransaccion;
        }

        // =========================================================
        // 2. MONITOR DE PACKING (Lee la tabla puente)
        // =========================================================
        public Dictionary<string, decimal> ObtenerStockLimpioMonitor()
        {
            var stock = new Dictionary<string, decimal>();
            try
            {
                using (OracleConnection cn = new Conexion().LeerConexion())
                {
                    string query = "SELECT LOWER(NOMBRE_MATERIA_PRIMA) AS MP, KILOS_DISPONIBLES FROM ADMIN.STOCK_LIMPIO";
                    using (OracleCommand cmd = new OracleCommand(query, cn))
                    {
                        cn.Open();
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string mp = reader["MP"].ToString().ToLower().Trim();
                                decimal kilos = Convert.ToDecimal(reader["KILOS_DISPONIBLES"]);
                                if (!stock.ContainsKey(mp))
                                {
                                    stock.Add(mp, kilos);
                                }
                            }
                        }
                    }
                }
            }
            catch { /* Retorna diccionario vacío seguro */ }
            return stock;
        }

        // =========================================================
        // 3. ARMADO DE VENTA DIRECTA (Corregido con columnas de tu BD)
        // =========================================================
        public bool ProcesarEmpaqueDirecto(string idMateriaPrima, int idProductoFinal, int unidades, decimal kilosTotales)
        {
            try
            {
                using (OracleConnection cn = new Conexion().LeerConexion())
                {
                    cn.Open();
                    using (OracleTransaction trx = cn.BeginTransaction())
                    {
                        try
                        {
                            // A. Descontar kilos de la materia prima en planta (STOCK_LIMPIO)
                            string qryResta = "UPDATE ADMIN.STOCK_LIMPIO SET KILOS_DISPONIBLES = KILOS_DISPONIBLES - :kilos WHERE LOWER(NOMBRE_MATERIA_PRIMA) = :mp";
                            using (OracleCommand cmdResta = new OracleCommand(qryResta, cn))
                            {
                                cmdResta.Parameters.Add("kilos", OracleDbType.Decimal).Value = kilosTotales;
                                cmdResta.Parameters.Add("mp", OracleDbType.Varchar2).Value = idMateriaPrima.ToLower().Trim();
                                cmdResta.ExecuteNonQuery();
                            }

                            // B. Sumar unidades terminadas al catálogo global para la venta (PRODUCTOS con STOCK_ACTUAL)
                            string qrySuma = "UPDATE ADMIN.PRODUCTOS SET STOCK_ACTUAL = STOCK_ACTUAL + :unidades WHERE ID_PRODUCTO = :idProd";
                            using (OracleCommand cmdSuma = new OracleCommand(qrySuma, cn))
                            {
                                cmdSuma.Parameters.Add("unidades", OracleDbType.Int32).Value = unidades;
                                cmdSuma.Parameters.Add("idProd", OracleDbType.Int32).Value = idProductoFinal;
                                cmdSuma.ExecuteNonQuery();
                            }

                            trx.Commit();
                            return true;
                        }
                        catch
                        {
                            trx.Rollback();
                            return false;
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        // =========================================================
        // 4. ARMADO DE KITS NUTRICIONALES (Corregido con columnas de tu BD)
        // =========================================================
        public bool ProcesarKitsNutricionales(int idProductoKit, int cantidadCajas, Dictionary<string, decimal> receta)
        {
            try
            {
                using (OracleConnection cn = new Conexion().LeerConexion())
                {
                    cn.Open();
                    using (OracleTransaction trx = cn.BeginTransaction())
                    {
                        try
                        {
                            // A. Descontar ingrediente por ingrediente del STOCK_LIMPIO
                            foreach (var item in receta)
                            {
                                string ingrediente = item.Key.ToLower().Trim();
                                decimal kilosPorCaja = item.Value; // 🚀 CORREGIDO: Espacio eliminado aquí
                                decimal totalKilosDescontar = kilosPorCaja * cantidadCajas;

                                string qryResta = "UPDATE ADMIN.STOCK_LIMPIO SET KILOS_DISPONIBLES = KILOS_DISPONIBLES - :kilos WHERE LOWER(NOMBRE_MATERIA_PRIMA) = :mp";
                                using (OracleCommand cmdResta = new OracleCommand(qryResta, cn))
                                {
                                    cmdResta.Parameters.Add("kilos", OracleDbType.Decimal).Value = totalKilosDescontar;
                                    cmdResta.Parameters.Add("mp", OracleDbType.Varchar2).Value = ingrediente;
                                    cmdResta.ExecuteNonQuery();
                                }
                            }

                            // B. Sumar las cajas armadas a la tabla PRODUCTOS
                            string qrySuma = "UPDATE ADMIN.PRODUCTOS SET STOCK_ACTUAL = STOCK_ACTUAL + :unidades WHERE ID_PRODUCTO = :idProd";
                            using (OracleCommand cmdSuma = new OracleCommand(qrySuma, cn))
                            {
                                cmdSuma.Parameters.Add("unidades", OracleDbType.Int32).Value = cantidadCajas;
                                cmdSuma.Parameters.Add("idProd", OracleDbType.Int32).Value = idProductoKit;
                                cmdSuma.ExecuteNonQuery();
                            }

                            trx.Commit();
                            return true;
                        }
                        catch
                        {
                            trx.Rollback();
                            return false;
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}