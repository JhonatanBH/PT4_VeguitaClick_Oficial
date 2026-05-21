using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using LaVeguita.Entities;

namespace LaVeguita.DAL
{
    public class EmpleadoDAL
    {
        private readonly Conexion _conexion = new Conexion();

        // LISTAR SOLAMENTE EMPLEADOS (Roles 1 al 7)
        public List<Empleado> ListarEmpleados()
        {
            List<Empleado> lista = new List<Empleado>();
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                string query = "SELECT * FROM EMPLEADOS ORDER BY ID_EMPLEADO DESC";
                OracleCommand cmd = new OracleCommand(query, cn);
                try
                {
                    cn.Open();
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new Empleado
                            {
                                IdEmpleado = Convert.ToInt32(dr["ID_EMPLEADO"]),
                                PnombreEmp = dr["PNOMBRE_EMP"].ToString(),
                                Appaterno = dr["APPATERNO"].ToString(),
                                CorreoEmp = dr["CORREO_EMP"] != DBNull.Value ? dr["CORREO_EMP"].ToString() : "",
                                TelefonoEmp = dr["TELEFONO_EMP"] != DBNull.Value ? Convert.ToInt64(dr["TELEFONO_EMP"]) : 0,
                                IdRolUsuario = Convert.ToInt32(dr["ID_ROL_USUARIO"])
                            });
                        }
                    }
                }
                catch (Exception ex) { throw new Exception("Error en EmpleadoDAL al listar: " + ex.Message); }
            }
            return lista;
        }

        // CREAR MEDIANTE EL PROCEDIMIENTO PL/SQL
        public bool InsertarEmpleadoCompleto(Empleado emp, string username, string contrasena)
        {
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                OracleCommand cmd = new OracleCommand("PRC_REGISTRAR_EMPLEADO", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.BindByName = true;

                // Parámetros del procedimiento
                cmd.Parameters.Add("p_username", OracleDbType.Varchar2).Value = username;
                cmd.Parameters.Add("p_contrasena", OracleDbType.Varchar2).Value = contrasena;
                cmd.Parameters.Add("p_pnombre", OracleDbType.Varchar2).Value = emp.PnombreEmp;
                cmd.Parameters.Add("p_snombre", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(emp.SnombreEmp) ? (object)DBNull.Value : emp.SnombreEmp;
                cmd.Parameters.Add("p_appaterno", OracleDbType.Varchar2).Value = emp.Appaterno;
                cmd.Parameters.Add("p_apmaterno", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(emp.Apmaterno) ? (object)DBNull.Value : emp.Apmaterno;
                cmd.Parameters.Add("p_fec_nac", OracleDbType.Date).Value = emp.FecNacEmp;
                cmd.Parameters.Add("p_correo", OracleDbType.Varchar2).Value = emp.CorreoEmp;
                cmd.Parameters.Add("p_telefono", OracleDbType.Int64).Value = emp.TelefonoEmp;
                cmd.Parameters.Add("p_id_rol", OracleDbType.Int32).Value = emp.IdRolUsuario;
                cmd.Parameters.Add("p_id_direccion", OracleDbType.Int32).Value = emp.IdDireccion != 0 ? emp.IdDireccion : (object)DBNull.Value;

                OracleParameter outExito = new OracleParameter("p_exito", OracleDbType.Int32, ParameterDirection.Output);
                cmd.Parameters.Add(outExito);

                try
                {
                    cn.Open();
                    cmd.ExecuteNonQuery();
                    return Convert.ToInt32(outExito.Value.ToString()) == 1;
                }
                catch (Exception ex) { throw new Exception("Error al ejecutar PRC_REGISTRAR_EMPLEADO: " + ex.Message); }
            }
        }

        public Empleado ObtenerPorId(int id)
        {
            Empleado emp = null;
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                string query = "SELECT * FROM EMPLEADOS WHERE ID_EMPLEADO = :id";
                OracleCommand cmd = new OracleCommand(query, cn);
                cmd.Parameters.Add("id", OracleDbType.Int32).Value = id;
                try
                {
                    cn.Open();
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            emp = new Empleado
                            {
                                IdEmpleado = Convert.ToInt32(dr["ID_EMPLEADO"]),
                                PnombreEmp = dr["PNOMBRE_EMP"].ToString(),
                                Appaterno = dr["APPATERNO"].ToString(),
                                CorreoEmp = dr["CORREO_EMP"] != DBNull.Value ? dr["CORREO_EMP"].ToString() : "",
                                TelefonoEmp = dr["TELEFONO_EMP"] != DBNull.Value ? Convert.ToInt64(dr["TELEFONO_EMP"]) : 0,
                                IdRolUsuario = Convert.ToInt32(dr["ID_ROL_USUARIO"])
                            };
                        }
                    }
                }
                catch (Exception ex) { throw new Exception("Error al buscar empleado: " + ex.Message); }
            }
            return emp;
        }

        // ACTUALIZAR MEDIANTE PL/SQL
        public bool ActualizarEmpleado(Empleado emp)
        {
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                OracleCommand cmd = new OracleCommand("PRC_ACTUALIZAR_EMPLEADO", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.BindByName = true; // Vital para que Oracle asocie bien por nombre de variable

                // PARAMETROS EXACTOS ENLAZADOS CON TU PROCEDIMIENTO EN ORACLE CLOUD
                cmd.Parameters.Add("p_id_empleado", OracleDbType.Int32).Value = emp.IdEmpleado;
                cmd.Parameters.Add("p_pnombre", OracleDbType.Varchar2).Value = emp.PnombreEmp;

                // Controlamos nulos para el Segundo Nombre por si el usuario lo deja vacío
                cmd.Parameters.Add("p_snombre", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(emp.SnombreEmp) ? (object)DBNull.Value : emp.SnombreEmp;

                cmd.Parameters.Add("p_appaterno", OracleDbType.Varchar2).Value = emp.Appaterno;

                // Controlamos nulos para el Apellido Materno
                cmd.Parameters.Add("p_apmaterno", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(emp.Apmaterno) ? (object)DBNull.Value : emp.Apmaterno;

                // Pasamos la Fecha de Nacimiento que capturamos con el input date
                cmd.Parameters.Add("p_fec_nac", OracleDbType.Date).Value = emp.FecNacEmp;

                cmd.Parameters.Add("p_correo", OracleDbType.Varchar2).Value = emp.CorreoEmp;
                cmd.Parameters.Add("p_telefono", OracleDbType.Int64).Value = emp.TelefonoEmp;
                cmd.Parameters.Add("p_id_rol", OracleDbType.Int32).Value = emp.IdRolUsuario;

                // Parámetro de salida OUT de control
                OracleParameter outExito = new OracleParameter("p_exito", OracleDbType.Int32, ParameterDirection.Output);
                cmd.Parameters.Add(outExito);

                try
                {
                    cn.Open();
                    cmd.ExecuteNonQuery();
                    return Convert.ToInt32(outExito.Value.ToString()) == 1;
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al actualizar registro formal en Cloud: " + ex.Message);
                }
            }
        }

        // ELIMINAR MEDIANTE PL/SQL
        public bool EliminarEmpleado(int id)
        {
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                OracleCommand cmd = new OracleCommand("PRC_ELIMINAR_EMPLEADO", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("p_id_empleado", OracleDbType.Int32).Value = id;

                OracleParameter outExito = new OracleParameter("p_exito", OracleDbType.Int32, ParameterDirection.Output);
                cmd.Parameters.Add(outExito);

                try
                {
                    cn.Open();
                    cmd.ExecuteNonQuery();
                    return Convert.ToInt32(outExito.Value.ToString()) == 1;
                }
                catch (Exception ex) { throw new Exception("Error al eliminar en Cloud: " + ex.Message); }
            }
        }




    }
}