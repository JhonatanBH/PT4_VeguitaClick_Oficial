using LaVeguita.DAL;
using LaVeguita.Entities;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;

namespace LaVeguita.DAL
{
    public class TransporteDAL
    {
        private readonly Conexion conexion = new Conexion();

        // --- GESTIÓN DE DESPACHOS ---

        /// <summary>
        /// Lista los despachos pendientes filtrando por las restricciones del Caso 4:
        /// 1. Tipo de vehículo (Bicicleta/Triciclo)[cite: 34].
        /// 2. Radio máximo de 15 kilómetros.
        /// 3. Capacidad de carga (5kg Bici / 50kg Triciclo).
                    /// </summary>
        public List<Despacho> ListarMisDespachos(string tipoVehiculo)
        {
            List<Despacho> lista = new List<Despacho>();
            using (OracleConnection cn = conexion.LeerConexion())
            {
                // Definición de límites de peso según electromovilidad del Caso 4 
                decimal limitePeso = (tipoVehiculo.ToUpper() == "BICICLETA") ? 5m : 50m;

                string query = @"SELECT d.ID_DESPACHO, d.KM_OBTENIDOS, d.PESO_TOTAL_KG, d.ESTADO_PEDIDO, d.ID_VENTA,
                        d.LATITUD_DESTINO, d.LONGITUD_DESTINO
                FROM DESPACHO d 
                JOIN TRANSPORTE t ON d.ID_TRANSPORTE = t.ID_TRANSPORTE
                WHERE t.TIPO_MOVIL = :tipo 
                AND d.ESTADO_PEDIDO = 'PENDIENTE'
                AND d.KM_OBTENIDOS <= 15
                AND d.PESO_TOTAL_KG <= :pesoMax";

                OracleCommand cmd = new OracleCommand(query, cn);
                cmd.BindByName = true;
                cmd.Parameters.Add("tipo", tipoVehiculo);
                cmd.Parameters.Add("pesoMax", limitePeso);

                try
                {
                    cn.Open();
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new Despacho
                            {
                                IdDespacho = Convert.ToInt32(dr["ID_DESPACHO"]),
                                KmObtenidos = Convert.ToDecimal(dr["KM_OBTENIDOS"]),
                                PesoTotalKg = Convert.ToDecimal(dr["PESO_TOTAL_KG"]),
                                EstadoPedido = dr["ESTADO_PEDIDO"].ToString(),
                                IdVenta = dr["ID_VENTA"] != DBNull.Value ? Convert.ToInt32(dr["ID_VENTA"]) : 0,
                                // MUEVE ESTO AQUÍ:
                                LatitudDestino = dr["LATITUD_DESTINO"] != DBNull.Value ? Convert.ToDouble(dr["LATITUD_DESTINO"]) : 0,
                                LongitudDestino = dr["LONGITUD_DESTINO"] != DBNull.Value ? Convert.ToDouble(dr["LONGITUD_DESTINO"]) : 0
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al listar despachos para el transportista: " + ex.Message);
                }
            }
            return lista;
        }

        // --- GESTIÓN DE VEHÍCULOS Y MANTENCIONES ---

        /// <summary>
        /// Lista móviles disponibles excluyendo aquellos en mantención, 
        /// cumpliendo con la gestión de activos y calidad de la empresa.
                    /// </summary>
        public List<Transporte> ListarVehiculosDisponibles()
        {
            List<Transporte> lista = new List<Transporte>();
            using (OracleConnection cn = conexion.LeerConexion())
            {
                // Filtro para excluir móviles con mantenciones registradas hoy 
                string query = @"SELECT * FROM TRANSPORTE 
                                WHERE ID_TRANSPORTE NOT IN (
                                    SELECT ID_TRANSPORTE FROM MANTENCIONES_MOVIL 
                                    WHERE TRUNC(FECHA_MANTENCION) = TRUNC(SYSDATE)
                                ) AND ESTADO = 'DISPONIBLE'";

                OracleCommand cmd = new OracleCommand(query, cn);
                try
                {
                    cn.Open();
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new Transporte
                            {
                                IdTransporte = Convert.ToInt32(dr["ID_TRANSPORTE"]),
                                TipoMovil = dr["TIPO_MOVIL"].ToString(),
                                Serial = dr["SERIAL"]?.ToString(),
                                CapMaxKg = Convert.ToDecimal(dr["CAP_MAX_KG"]),
                                Estado = dr["ESTADO"].ToString()

                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al listar móviles disponibles: " + ex.Message);
                }
            }
            return lista;
        }

        // --- CÁLCULOS GEOGRÁFICOS (Soporte para GPS y Radio de 15km) ---

        /// <summary>
        /// Calcula la distancia entre dos puntos para validar el radio de 15km.
                    /// </summary>
        public double CalcularDistancia(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Radio de la Tierra en Kilómetros
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double angle) => Math.PI * angle / 180.0;
    }
}