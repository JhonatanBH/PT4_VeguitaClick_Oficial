using Oracle.ManagedDataAccess.Client;
using LaVeguita.Entities;
using System.Data;
using System;
using System.Collections.Generic;

namespace LaVeguita.DAL
{
    public class DespachoDAL
    {
        private readonly Conexion _conexion = new Conexion();

        public List<Despacho> ListarDespachosPendientes()
        {
            List<Despacho> lista = new List<Despacho>();
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                // Mantenemos la consulta con PESO_TOTAL_KG
                string query = "SELECT ID_DESPACHO, KM_OBTENIDOS, ID_VENTA, ID_TRANSPORTE, ESTADO_PEDIDO, PESO_TOTAL_KG FROM DESPACHO WHERE ESTADO_PEDIDO != 'ENTREGADO'";

                OracleCommand cmd = new OracleCommand(query, cn);
                try
                {
                    cn.Open();
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new Despacho
                            {
                                // Blindaje aplicado a todos los campos para evitar InvalidCastException
                                IdDespacho = dr["ID_DESPACHO"] != DBNull.Value ? Convert.ToInt32(dr["ID_DESPACHO"]) : 0,

                                KmObtenidos = dr["KM_OBTENIDOS"] != DBNull.Value ? Convert.ToDecimal(dr["KM_OBTENIDOS"]) : 0,

                                IdVenta = dr["ID_VENTA"] != DBNull.Value ? Convert.ToInt32(dr["ID_VENTA"]) : 0,

                                IdTransporte = dr["ID_TRANSPORTE"] != DBNull.Value ? Convert.ToInt32(dr["ID_TRANSPORTE"]) : 0,

                                EstadoPedido = dr["ESTADO_PEDIDO"] != DBNull.Value ? dr["ESTADO_PEDIDO"].ToString() : "PENDIENTE",

                                // Ahora esta línea no dará error porque la propiedad ya existe en la entidad
                                PesoTotalKg = dr["PESO_TOTAL_KG"] != DBNull.Value ? Convert.ToDecimal(dr["PESO_TOTAL_KG"]) : 0
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al listar despachos pendientes: " + ex.Message);
                }
            }
            return lista;
        }

        public void ActualizarEstadoEntregado(int idDespacho)
        {
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                string query = "UPDATE DESPACHO SET ESTADO_PEDIDO = 'ENTREGADO', HORA_ENTR = :hora WHERE ID_DESPACHO = :id";
                OracleCommand cmd = new OracleCommand(query, cn);
                cmd.BindByName = true;

                // Formateo de hora como número HHmm (ej: 1430)
                int horaActual = int.Parse(DateTime.Now.ToString("HHmm"));

                cmd.Parameters.Add("hora", horaActual);
                cmd.Parameters.Add("id", idDespacho);

                try
                {
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al actualizar despacho a entregado: " + ex.Message);
                }
            }
        }
    }
}