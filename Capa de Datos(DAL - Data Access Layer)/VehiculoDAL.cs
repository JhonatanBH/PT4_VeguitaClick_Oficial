using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using LaVeguita.Entities;

namespace LaVeguita.DAL
{
    public class VehiculoDAL
    {
        // 1. LISTAR TODOS LOS VEHÍCULOS PARA EL MONITOR (Nombres reales)
        public List<Vehiculo> ListarVehiculos()
        {
            var lista = new List<Vehiculo>();
            string query = "SELECT ID_TRANSPORTE, TIPO_MOVIL, SERIAL, CAP_MAX_KG, ESTADO, KM_TOTAL FROM ADMIN.TRANSPORTE ORDER BY ID_TRANSPORTE ASC";

            using (OracleConnection cn = new Conexion().LeerConexion())
            {
                cn.Open();
                using (OracleCommand cmd = new OracleCommand(query, cn))
                using (OracleDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new Vehiculo
                        {
                            IdTransporte = Convert.ToInt32(dr["ID_TRANSPORTE"]),
                            TipoMovil = dr["TIPO_MOVIL"].ToString(),
                            Serial = dr["SERIAL"].ToString(),
                            CapMaxKg = Convert.ToInt32(dr["CAP_MAX_KG"]),
                            Estado = dr["ESTADO"].ToString(),
                            KmTotal = Convert.ToInt32(dr["KM_TOTAL"])
                        });
                    }
                }
            }
            return lista;
        }

        // 2. REGISTRAR UN NUEVO VEHÍCULO
        public bool InsertarVehiculo(Vehiculo v)
        {
            string query = "INSERT INTO ADMIN.TRANSPORTE (ID_TRANSPORTE, TIPO_MOVIL, SERIAL, CAP_MAX_KG, ESTADO, KM_TOTAL) " +
                           "VALUES ((SELECT NVL(MAX(ID_TRANSPORTE), 0) + 1 FROM ADMIN.TRANSPORTE), :tipo, :serial, :cap, 'DISPONIBLE', 0)";

            using (OracleConnection cn = new Conexion().LeerConexion())
            {
                cn.Open();
                using (OracleCommand cmd = new OracleCommand(query, cn))
                {
                    cmd.Parameters.Add("tipo", OracleDbType.Varchar2).Value = v.TipoMovil;
                    cmd.Parameters.Add("serial", OracleDbType.Varchar2).Value = v.Serial;
                    cmd.Parameters.Add("cap", OracleDbType.Int32).Value = v.CapMaxKg;

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // 3. REGISTRAR MANTENCIÓN CON DURACIÓN ESTIMADA (Corregido con tus tablas)
        public bool RegistrarMantencionFlota(int idTransporte, string detalle, int costo, int diasEstimados)
        {
            using (OracleConnection cn = new Conexion().LeerConexion())
            {
                cn.Open();
                using (OracleTransaction trans = cn.BeginTransaction())
                {
                    try
                    {
                        // A. Insertar la mantención técnica calculando la fecha estimada (SYSDATE + dias)
                        string qMantencion = @"INSERT INTO ADMIN.MANTENCIONES_MOVIL 
                                             (ID_MANTENCION, ID_TRANSPORTE, DETALLE_TECNICO, COSTO_MANTENCION, FECHA_MANTENCION, FECHA_ESTIMADA, ESTADO_PROCESO) 
                                             VALUES ((SELECT NVL(MAX(ID_MANTENCION), 0) + 1 FROM ADMIN.MANTENCIONES_MOVIL), :id, :detalle, :costo, SYSDATE, SYSDATE + :dias, 'EN_MANTENCION')";

                        using (OracleCommand cmd1 = new OracleCommand(qMantencion, cn))
                        {
                            cmd1.Parameters.Add("id", OracleDbType.Int32).Value = idTransporte;
                            cmd1.Parameters.Add("detalle", OracleDbType.Varchar2).Value = detalle;
                            cmd1.Parameters.Add("costo", OracleDbType.Int32).Value = costo;
                            cmd1.Parameters.Add("dias", OracleDbType.Int32).Value = diasEstimados;
                            cmd1.ExecuteNonQuery();
                        }

                        // B. Actualizar estado en TRANSPORTE a 'MANTENCION' para sacarlo del andén
                        string qFlota = "UPDATE ADMIN.TRANSPORTE SET ESTADO = 'MANTENCION' WHERE ID_TRANSPORTE = :id";
                        using (OracleCommand cmd2 = new OracleCommand(qFlota, cn))
                        {
                            cmd2.Parameters.Add("id", OracleDbType.Int32).Value = idTransporte;
                            cmd2.ExecuteNonQuery();
                        }

                        trans.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        trans.Rollback();
                        return false;
                    }
                }
            }
        }

        // 4. DAR DE ALTA TÉCNICA Y REHABILITAR MÓVIL (Corregido con tus tablas)
        public bool HabilitarVehiculoFlota(int idTransporte)
        {
            using (OracleConnection cn = new Conexion().LeerConexion())
            {
                cn.Open();
                using (OracleTransaction trans = cn.BeginTransaction())
                {
                    try
                    {
                        // A. Finalizar el estado del proceso en el historial técnico
                        string qHistorial = "UPDATE ADMIN.MANTENCIONES_MOVIL SET ESTADO_PROCESO = 'FINALIZADO' WHERE ID_TRANSPORTE = :id AND ESTADO_PROCESO = 'EN_MANTENCION'";
                        using (OracleCommand cmd1 = new OracleCommand(qHistorial, cn))
                        {
                            cmd1.Parameters.Add("id", OracleDbType.Int32).Value = idTransporte;
                            cmd1.ExecuteNonQuery();
                        }

                        // B. Devolver el móvil al estado 'DISPONIBLE' en la flota activa
                        string qFlota = "UPDATE ADMIN.TRANSPORTE SET ESTADO = 'DISPONIBLE' WHERE ID_TRANSPORTE = :id";
                        using (OracleCommand cmd2 = new OracleCommand(qFlota, cn))
                        {
                            cmd2.Parameters.Add("id", OracleDbType.Int32).Value = idTransporte;
                            cmd2.ExecuteNonQuery();
                        }

                        trans.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        trans.Rollback();
                        return false;
                    }
                }
            }
        }
    }
}