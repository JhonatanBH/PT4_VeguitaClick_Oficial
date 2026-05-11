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

        // 1. PARA EL TRANSPORTISTA: Filtra por su vehículo (Bici/Triciclo)
        public List<Despacho> ListarDespachosPorVehiculo(string tipoVehiculo)
        {
            List<Despacho> lista = new List<Despacho>();
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                string query = @"SELECT d.ID_DESPACHO, d.KM_OBTENIDOS, d.ID_VENTA, d.ID_TRANSPORTE, 
                                       d.ESTADO_PEDIDO, d.PESO_TOTAL_KG, t.TIPO_MOVIL
                                FROM DESPACHO d
                                JOIN TRANSPORTE t ON d.ID_TRANSPORTE = t.ID_TRANSPORTE
                                WHERE t.TIPO_MOVIL = :tipo 
                                AND d.ESTADO_PEDIDO = 'PENDIENTE'";

                OracleCommand cmd = new OracleCommand(query, cn);
                cmd.Parameters.Add("tipo", tipoVehiculo);

                try
                {
                    cn.Open();
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new Despacho
                            {
                                IdDespacho = dr["ID_DESPACHO"] != DBNull.Value ? Convert.ToInt32(dr["ID_DESPACHO"]) : 0,
                                KmObtenidos = dr["KM_OBTENIDOS"] != DBNull.Value ? Convert.ToDecimal(dr["KM_OBTENIDOS"]) : 0,
                                IdVenta = dr["ID_VENTA"] != DBNull.Value ? Convert.ToInt32(dr["ID_VENTA"]) : 0,
                                IdTransporte = dr["ID_TRANSPORTE"] != DBNull.Value ? Convert.ToInt32(dr["ID_TRANSPORTE"]) : 0,
                                EstadoPedido = dr["ESTADO_PEDIDO"]?.ToString() ?? "PENDIENTE",
                                PesoTotalKg = dr["PESO_TOTAL_KG"] != DBNull.Value ? Convert.ToDecimal(dr["PESO_TOTAL_KG"]) : 0
                            });
                        }
                    }
                }
                catch (Exception ex) { throw new Exception("Error en ListarDespachosPorVehiculo: " + ex.Message); }
            }
            return lista;
        }

        // 2. PARA EL JEFE (ASISTENTE): Ve todo lo que falta por entregar (Cartas de Gestión)
        // Consolidamos ListarDespachosGlobales y ListarTodoParaGestion aquí.
        public List<Despacho> ListarTodoParaGestion()
        {
            List<Despacho> lista = new List<Despacho>();
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                string query = "SELECT * FROM DESPACHO WHERE ESTADO_PEDIDO != 'ENTREGADO' ORDER BY ID_DESPACHO DESC";
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
                                IdDespacho = dr["ID_DESPACHO"] != DBNull.Value ? Convert.ToInt32(dr["ID_DESPACHO"]) : 0,
                                IdVenta = dr["ID_VENTA"] != DBNull.Value ? Convert.ToInt32(dr["ID_VENTA"]) : 0,
                                KmObtenidos = dr["KM_OBTENIDOS"] != DBNull.Value ? Convert.ToDecimal(dr["KM_OBTENIDOS"]) : 0,
                                PesoTotalKg = dr["PESO_TOTAL_KG"] != DBNull.Value ? Convert.ToDecimal(dr["PESO_TOTAL_KG"]) : 0,
                                EstadoPedido = dr["ESTADO_PEDIDO"]?.ToString() ?? "PENDIENTE"
                            });
                        }
                    }
                }
                catch (Exception ex) { throw new Exception("Error en ListarTodoParaGestion: " + ex.Message); }
            }
            return lista;
        }

        // 3. ACCIÓN: Marcar como entregado
        public void ActualizarEstadoEntregado(int idDespacho)
        {
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                string query = "UPDATE DESPACHO SET ESTADO_PEDIDO = 'ENTREGADO', HORA_ENTR = :hora WHERE ID_DESPACHO = :id";
                OracleCommand cmd = new OracleCommand(query, cn);
                cmd.BindByName = true;
                int horaActual = int.Parse(DateTime.Now.ToString("HHmm"));
                cmd.Parameters.Add("hora", horaActual);
                cmd.Parameters.Add("id", idDespacho);

                try
                {
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex) { throw new Exception("Error al actualizar despacho: " + ex.Message); }
            }
        }

        // --- MÉTODO DE COMPATIBILIDAD ---
        // Agrega esto para que el HomeController y otros dejen de dar error
        public List<Despacho> ListarDespachosPendientes()
        {
            // Redirigimos al nuevo método que ya tiene blindaje contra nulos
            return ListarTodoParaGestion();
        }




    }
}