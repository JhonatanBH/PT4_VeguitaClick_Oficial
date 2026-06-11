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
                    using (OracleCommand cmd = new OracleCommand("SP_REGISTRAR_PRODUCCION", cn))
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

                        // 💡 Recordatorio para Oracle: 
                        // Asegúrate de que dentro de 'SP_REGISTRAR_PRODUCCION' exista un UPDATE/INSERT 
                        // que sume el valor de 'p_cant_prim_calidad' a la tabla STOCK_LIMPIO.
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
                    // Consulta los kilos disponibles de la nueva tabla puente
                    string query = "SELECT LOWER(NOMBRE_MATERIA_PRIMA) AS MP, KILOS_DISPONIBLES FROM ADMIN.STOCK_LIMPIO";
                    using (OracleCommand cmd = new OracleCommand(query, cn))
                    {
                        cn.Open();
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                stock.Add(reader["MP"].ToString(), Convert.ToDecimal(reader["KILOS_DISPONIBLES"]));
                            }
                        }
                    }
                }
            }
            catch { /* Manejo interno, devolverá diccionario vacío si falla */ }
            return stock;
        }

        // =========================================================
        // 3. ARMADO DE VENTA DIRECTA (Transacción de Doble Impacto)
        // =========================================================
        public bool ProcesarEmpaqueDirecto(string idMateriaPrima, string skuFormato, int unidades, decimal kilosTotales)
        {
            try
            {
                using (OracleConnection cn = new Conexion().LeerConexion())
                {
                    cn.Open();
                    // Bloqueo transaccional: Si una consulta falla, se revierte todo (Rollback)
                    using (OracleTransaction trx = cn.BeginTransaction())
                    {
                        try
                        {
                            // A. Descontar kilos de la materia prima en planta
                            string qryResta = "UPDATE ADMIN.STOCK_LIMPIO SET KILOS_DISPONIBLES = KILOS_DISPONIBLES - :kilos WHERE LOWER(NOMBRE_MATERIA_PRIMA) = :mp";
                            using (OracleCommand cmdResta = new OracleCommand(qryResta, cn))
                            {
                                cmdResta.Parameters.Add("kilos", OracleDbType.Decimal).Value = kilosTotales;
                                cmdResta.Parameters.Add("mp", OracleDbType.Varchar2).Value = idMateriaPrima.ToLower();
                                cmdResta.ExecuteNonQuery();
                            }

                            // B. Sumar unidades terminadas al catálogo global para la venta
                            string qrySuma = "UPDATE ADMIN.PRODUCTOS SET STOCK = STOCK + :unidades WHERE CODIGO_SKU = :sku";
                            using (OracleCommand cmdSuma = new OracleCommand(qrySuma, cn))
                            {
                                cmdSuma.Parameters.Add("unidades", OracleDbType.Int32).Value = unidades;
                                cmdSuma.Parameters.Add("sku", OracleDbType.Varchar2).Value = skuFormato;
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
        // 4. ARMADO DE KITS NUTRICIONALES (Receta Multi-Ingrediente)
        // =========================================================
        public bool ProcesarKitsNutricionales(string idFormatoKit, int cantidadCajas, Dictionary<string, decimal> receta)
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
                            // A. Iteramos la receta dictada por la nutricionista para descontar ingrediente por ingrediente
                            foreach (var item in receta)
                            {
                                string ingrediente = item.Key.ToLower();
                                decimal kilosPorCaja = item.Value;
                                decimal totalKilosDescontar = kilosPorCaja * cantidadCajas;

                                string qryResta = "UPDATE ADMIN.STOCK_LIMPIO SET KILOS_DISPONIBLES = KILOS_DISPONIBLES - :kilos WHERE LOWER(NOMBRE_MATERIA_PRIMA) = :mp";
                                using (OracleCommand cmdResta = new OracleCommand(qryResta, cn))
                                {
                                    cmdResta.Parameters.Add("kilos", OracleDbType.Decimal).Value = totalKilosDescontar;
                                    cmdResta.Parameters.Add("mp", OracleDbType.Varchar2).Value = ingrediente;
                                    cmdResta.ExecuteNonQuery();
                                }
                            }

                            // B. Sumamos las unidades de cajas terminadas al catálogo global (PRODUCTOS)
                            // El SKU comercial se puede armar de manera dinámica (Ej: 'KIT_SOLTERO', 'KIT_PAREJA')
                            string skuKit = "KIT_" + idFormatoKit.ToUpper();

                            string qrySuma = "UPDATE ADMIN.PRODUCTOS SET STOCK = STOCK + :unidades WHERE CODIGO_SKU = :sku";
                            using (OracleCommand cmdSuma = new OracleCommand(qrySuma, cn))
                            {
                                cmdSuma.Parameters.Add("unidades", OracleDbType.Int32).Value = cantidadCajas;
                                cmdSuma.Parameters.Add("sku", OracleDbType.Varchar2).Value = skuKit;
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