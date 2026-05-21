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
                           d.ESTADO_PEDIDO, d.PESO_TOTAL_KG, t.TIPO_MOVIL,
                           d.LATITUD_DESTINO, d.LONGITUD_DESTINO
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
                                PesoTotalKg = dr["PESO_TOTAL_KG"] != DBNull.Value ? Convert.ToDecimal(dr["PESO_TOTAL_KG"]) : 0,


                                LatitudDestino = dr["LATITUD_DESTINO"] != DBNull.Value ? Convert.ToDouble(dr["LATITUD_DESTINO"]) : 0,
                                LongitudDestino = dr["LONGITUD_DESTINO"] != DBNull.Value ? Convert.ToDouble(dr["LONGITUD_DESTINO"]) : 0
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

        // 4. ACCIÓN: Crear un nuevo despacho desde el Panel de Control Logístico
        // 4. ACCIÓN: Actualiza el despacho existente desde el Panel de Control Logístico
        // JUGADA MAESTRA: Cambiamos el INSERT por un UPDATE para modificar la fila real de Oracle Cloud
        public bool InsertarDespacho(int idVenta, int idTransporte, int idUsuarioEmpleado, decimal pesoTotal, decimal kmObtenidos)
        {
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                // Modificamos el registro existente usando el ID_VENTA como llave.
                // Cambiamos ESTADO_ENTREGA a 'ASIGNADO' para sacarlo de la bodega de forma definitiva.
                string query = @"UPDATE DESPACHO 
                                SET KM_OBTENIDOS = :km, 
                                    ID_TRANSPORTE = :transporte, 
                                    ID_EMPLEADO = :empleado, 
                                    ESTADO_PEDIDO = 'PENDIENTE', 
                                    ESTADO_ENTREGA = 'ASIGNADO', 
                                    PESO_TOTAL_KG = :peso
                                WHERE ID_VENTA = :venta";

                OracleCommand cmd = new OracleCommand(query, cn);
                cmd.BindByName = true;

                cmd.Parameters.Add("km", OracleDbType.Decimal).Value = kmObtenidos;
                cmd.Parameters.Add("transporte", OracleDbType.Int32).Value = idTransporte;
                cmd.Parameters.Add("empleado", OracleDbType.Int32).Value = idUsuarioEmpleado;
                cmd.Parameters.Add("peso", OracleDbType.Decimal).Value = pesoTotal;
                cmd.Parameters.Add("venta", OracleDbType.Int32).Value = idVenta;

                try
                {
                    cn.Open();
                    int filas = cmd.ExecuteNonQuery();
                    return filas > 0;
                }
                catch (Exception ex)
                {
                    throw new Exception("Error en DespachoDAL al actualizar la asignación logística: " + ex.Message);
                }
            }
        }

        public Dictionary<string, int> ObtenerConteosPorTipoEnvio()
        {
            Dictionary<string, int> resultados = new Dictionary<string, int>();

            // Inicializamos las 4 modalidades base exigidas por el Caso 4 para que no arranquen en vacio
            resultados["NORMAL"] = 0;
            resultados["ECONOMICO"] = 0;
            resultados["URGENTE"] = 0;
            resultados["FIJO"] = 0;

            using (OracleConnection cn = _conexion.LeerConexion())
            {
                // Cruzamos la tabla DESPACHO con la de cabecera ORDEN_VENTA para agrupar por el tipo real
                string query = @"SELECT o.TIPO_ENVIO, COUNT(*) AS TOTAL
                        FROM DESPACHO d
                        JOIN ORDEN_VENTA o ON d.ID_VENTA = o.ID_VENTA
                        WHERE d.ESTADO_PEDIDO = 'PENDIENTE'
                        GROUP BY o.TIPO_ENVIO";

                OracleCommand cmd = new OracleCommand(query, cn);
                try
                {
                    cn.Open();
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            string tipo = dr["TIPO_ENVIO"]?.ToString()?.ToUpper() ?? "NORMAL";
                            int total = dr["TOTAL"] != DBNull.Value ? Convert.ToInt32(dr["TOTAL"]) : 0;

                            // Almacenamos el conteo real de la base de datos distribuidora
                            resultados[tipo] = total;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Si por alguna razon la tabla de cabecera tiene nombres vacios, evitamos que el sistema colapse
                    throw new Exception("Error en ObtenerConteosPorTipoEnvio: " + ex.Message);
                }
            }
            return resultados;
        }

    }
}