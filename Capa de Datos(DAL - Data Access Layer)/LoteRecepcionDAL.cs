using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using LaVeguita.Entities;

namespace LaVeguita.DAL
{
    public class LoteRecepcionDAL
    {
        // 1. GUARDAR GUIA Y DETALLES EN UNA SOLA TRANSACCION ATOMICA
        public bool RegistrarRecepcionCompleta(RecepcionGuia guia, out string mensajeResultado)
        {
            bool exito = false;
            mensajeResultado = string.Empty;

            using (OracleConnection cn = new Conexion().LeerConexion())
            {
                cn.Open();
                // Iniciamos la transaccion para asegurar la cabecera-detalle
                OracleTransaction transaccion = cn.BeginTransaction();

                try
                {
                    // A. Insertar la Cabecera (RECEPCION_GUIAS) y rescatar el ID generado por el ALWAYS AS IDENTITY
                    string queryCabecera = @"
                        INSERT INTO RECEPCION_GUIAS (NUM_GUIA_FISICA, ID_PROVEEDOR, FECHA_INGRESO) 
                        VALUES (:numGuia, :idProv, SYSDATE) 
                        RETURNING ID_RECEPCION INTO :idGen";

                    int idRecepcionGenerado = 0;

                    using (OracleCommand cmdCab = new OracleCommand(queryCabecera, cn))
                    {
                        cmdCab.Transaction = transaccion;
                        cmdCab.Parameters.Add("numGuia", OracleDbType.Varchar2).Value = guia.NumGuiaFisica;
                        cmdCab.Parameters.Add("idProv", OracleDbType.Int32).Value = guia.IdProveedor;

                        // Parametro de salida para atrapar el ID autogenerado por Oracle
                        OracleParameter pIdGen = new OracleParameter("idGen", OracleDbType.Int32, ParameterDirection.Output);
                        cmdCab.Parameters.Add(pIdGen);

                        cmdCab.ExecuteNonQuery();
                        idRecepcionGenerado = Convert.ToInt32(pIdGen.Value.ToString());
                    }

                    // B. Insertar cada item del Carrito de Compras (DETALLE_LOTES)
                    string queryDetalle = @"
                        INSERT INTO DETALLE_LOTES (ID_RECEPCION, ID_PRODUCTO, PESO_REAL_LOCAL, PESO_DECLARADO_GD, DESCRIPCION_LOTE, ESTADO) 
                        VALUES (:idRecepcion, :idProd, :pesoReal, :pesoGd, :descLote, 'PENDIENTE')";

                    foreach (var item in guia.ItemsCargamento)
                    {
                        using (OracleCommand cmdDet = new OracleCommand(queryDetalle, cn))
                        {
                            cmdDet.Transaction = transaccion;
                            cmdDet.Parameters.Add("idRecepcion", OracleDbType.Int32).Value = idRecepcionGenerado;
                            cmdDet.Parameters.Add("idProd", OracleDbType.Int32).Value = item.IdProducto;
                            cmdDet.Parameters.Add("pesoReal", OracleDbType.Decimal).Value = item.PesoRealLocal;
                            cmdDet.Parameters.Add("pesoGd", OracleDbType.Decimal).Value = item.PesoDeclaradoGd;
                            cmdDet.Parameters.Add("descLote", OracleDbType.Varchar2).Value = item.DescripcionLote ?? (object)DBNull.Value;

                            cmdDet.ExecuteNonQuery();
                        }
                    }

                    // Si todo se inserto bien, confirmamos los datos de golpe
                    transaccion.Commit();
                    exito = true;
                    mensajeResultado = "Recepcion de guia y lotes almacenada con exito en Oracle Cloud.";
                }
                catch (Exception ex)
                {
                    // Si algo falla, deshacemos todo para evitar registros huerfanos
                    transaccion.Rollback();
                    exito = false;
                    mensajeResultado = "Error transaccional en Oracle: " + ex.Message;
                }
            }

            return exito;
        }

        // 2. LISTAR LAS GUIAS INGRESADAS (Para el historial del Asistente)
        public List<RecepcionGuia> ListarGuiasIngresadas()
        {
            List<RecepcionGuia> lista = new List<RecepcionGuia>();
            string query = @"
                SELECT G.ID_RECEPCION, G.NUM_GUIA_FISICA, G.ID_PROVEEDOR, G.FECHA_INGRESO, P.NOM_PROVEEDOR
                FROM RECEPCION_GUIAS G
                JOIN PROVEEDORES P ON G.ID_PROVEEDOR = P.ID_PROVEEDOR
                ORDER BY G.ID_RECEPCION DESC";

            try
            {
                using (OracleConnection cn = new Conexion().LeerConexion())
                {
                    using (OracleCommand cmd = new OracleCommand(query, cn))
                    {
                        cn.Open();
                        using (OracleDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                lista.Add(new RecepcionGuia
                                {
                                    IdRecepcion = Convert.ToInt32(dr["ID_RECEPCION"]),
                                    NumGuiaFisica = dr["NUM_GUIA_FISICA"].ToString(),
                                    IdProveedor = Convert.ToInt32(dr["ID_PROVEEDOR"]),
                                    FechaIngreso = Convert.ToDateTime(dr["FECHA_INGRESO"]),
                                    NomProveedor = dr["NOM_PROVEEDOR"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
            return lista;
        }
    }
}