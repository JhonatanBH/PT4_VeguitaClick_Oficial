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
    public class UsuarioDAL
    {
        private Conexion conexion = new Conexion();

        public int ValidarUsuario(string username, string password)
        {
            int idRol = 0;
            using (OracleConnection conn = conexion.LeerConexion())
            {
                // Llamamos a la función del Package que creamos
                OracleCommand cmd = new OracleCommand("SELECT PKG_USUARIOS.FN_LOGIN(:u, :p) FROM DUAL", conn);
                cmd.Parameters.Add("u", username);
                cmd.Parameters.Add("p", password);

                conn.Open();
                idRol = Convert.ToInt32(cmd.ExecuteScalar());
            }
            return idRol; // Retorna el ID del Rol (1 al 8) o 0 si falla [cite: 108]
        }









        public List<Usuario> ListarUsuarios()
        {
            List<Usuario> lista = new List<Usuario>();

            using (OracleConnection conn = conexion.LeerConexion())
            {
                string query = @"SELECT u.ID_USUARIO, u.NOMBRE_USER, u.FONO, u.CORREO_USU, 
                        u.ID_ROL_USUARIO, u.ID_DIRECCION, r.NOMBRE AS NOMBRE_ROL
                 FROM USUARIOS u
                 JOIN ROL r ON u.ID_ROL_USUARIO = r.ID_ROL_USUARIO";

                OracleCommand cmd = new OracleCommand(query, conn);

                try
                {
                    conn.Open();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Usuario user = new Usuario();

                            // Campos obligatorios (PK) no suelen necesitar validación si son NOT NULL en SQL
                            user.IdUsuario = Convert.ToInt32(reader["ID_USUARIO"]);
                            user.NombreUser = reader["NOMBRE_USER"].ToString();

                            // VALIDACIÓN DE NULOS (Vital para evitar el error de DBNull)
                            user.Fono = reader["FONO"] != DBNull.Value ? Convert.ToInt64(reader["FONO"]) : 0;

                            user.CorreoUsu = reader["CORREO_USU"] != DBNull.Value ? reader["CORREO_USU"].ToString() : "";

                            user.IdRolUsuario = reader["ID_ROL_USUARIO"] != DBNull.Value ? Convert.ToInt32(reader["ID_ROL_USUARIO"]) : 0;

                            user.IdDireccion = reader["ID_DIRECCION"] != DBNull.Value ? Convert.ToInt32(reader["ID_DIRECCION"]) : 0;

                            user.NombreRol = reader["NOMBRE_ROL"] != DBNull.Value ? reader["NOMBRE_ROL"].ToString() : "Sin Rol";

                            lista.Add(user);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Con esto sabrás si el error persiste o es en otra línea
                    throw new Exception("Error en UsuarioDAL al listar con roles: " + ex.Message);
                }
            }
            return lista;
        }




        public bool InsertarUsuario(Usuario nuevoUser)
        {
            using (OracleConnection conn = conexion.LeerConexion())
            {
                // SQL ajustado a tus nombres de tabla y secuencia
                string query = "INSERT INTO USUARIOS (ID_USUARIO, NOMBRE_USER, CONTRASENA, FONO, CORREO_USU, ID_ROL_USUARIO, ID_DIRECCION) " +
                               "VALUES (SEQ_USUARIO.NEXTVAL, :nombre, :pass, :fono, :correo, :idRol, :idDir)";

                OracleCommand cmd = new OracleCommand(query, conn);

                // Pasamos los datos desde tu clase Usuario
                cmd.Parameters.Add(new OracleParameter("nombre", nuevoUser.NombreUser));
                cmd.Parameters.Add(new OracleParameter("pass", nuevoUser.Contrasena));
                cmd.Parameters.Add(new OracleParameter("fono", nuevoUser.Fono));
                cmd.Parameters.Add(new OracleParameter("correo", nuevoUser.CorreoUsu));
                cmd.Parameters.Add(new OracleParameter("idRol", nuevoUser.IdRolUsuario));
                cmd.Parameters.Add(new OracleParameter("idDir", nuevoUser.IdDireccion));

                try
                {
                    conn.Open();
                    int filas = cmd.ExecuteNonQuery();
                    return filas > 0;
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al insertar usuario: " + ex.Message);
                }
            }
        }

        public bool ActualizarUsuario(Usuario u)
        {
            using (OracleConnection conn = conexion.LeerConexion())
            {
                // 1. Agregamos ID_ROL_USUARIO = :idRol a la cadena de texto
                string query = "UPDATE USUARIOS SET NOMBRE_USER = :nombre, FONO = :fono, " +
                               "CORREO_USU = :correo, ID_ROL_USUARIO = :idRol, ID_DIRECCION = :idDir " +
                               "WHERE ID_USUARIO = :id";

                OracleCommand cmd = new OracleCommand(query, conn);

                // 2. IMPORTANTE: El orden de los parámetros debe ser IGUAL al de la query arriba
                cmd.Parameters.Add(new OracleParameter("nombre", u.NombreUser));
                cmd.Parameters.Add(new OracleParameter("fono", u.Fono));
                cmd.Parameters.Add(new OracleParameter("correo", u.CorreoUsu));

                // --- ESTE ES EL NUEVO ---
                cmd.Parameters.Add(new OracleParameter("idRol", u.IdRolUsuario));
                // -------------------------

                cmd.Parameters.Add(new OracleParameter("idDir", u.IdDireccion));
                cmd.Parameters.Add(new OracleParameter("id", u.IdUsuario));

                try
                {
                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al actualizar: " + ex.Message);
                }
            }
        }





        public Usuario ObtenerPorId(int id)
        {
            using (OracleConnection conn = conexion.LeerConexion())
            {
                string query = "SELECT * FROM USUARIOS WHERE ID_USUARIO = :id";
                OracleCommand cmd = new OracleCommand(query, conn);
                cmd.Parameters.Add(new OracleParameter("id", id));

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Usuario
                            {
                                IdUsuario = Convert.ToInt32(reader["ID_USUARIO"]),
                                NombreUser = reader["NOMBRE_USER"].ToString(),

                                // Protegemos la contraseña por si acaso
                                Contrasena = reader["CONTRASENA"] != DBNull.Value ? reader["CONTRASENA"].ToString() : "",

                                // Este ya lo tenías bien
                                Fono = reader["FONO"] != DBNull.Value ? Convert.ToInt64(reader["FONO"]) : 0,

                                CorreoUsu = reader["CORREO_USU"] != DBNull.Value ? reader["CORREO_USU"].ToString() : "",

                                // PROTECCIÓN PARA LOS CAMPOS QUE ESTÁN FALLANDO:
                                IdRolUsuario = reader["ID_ROL_USUARIO"] != DBNull.Value ? Convert.ToInt32(reader["ID_ROL_USUARIO"]) : 0,
                                IdDireccion = reader["ID_DIRECCION"] != DBNull.Value ? Convert.ToInt32(reader["ID_DIRECCION"]) : 0
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al buscar usuario: " + ex.Message);
                }
            }
            return null;
        }




        // MÉTODO PARA ELIMINAR
        public bool EliminarUsuario(int id)
        {
            using (OracleConnection conn = conexion.LeerConexion())
            {
                string query = "DELETE FROM USUARIOS WHERE ID_USUARIO = :id";
                OracleCommand cmd = new OracleCommand(query, conn);
                cmd.Parameters.Add(new OracleParameter("id", id));

                try
                {
                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al eliminar: " + ex.Message);
                }
            }
        }


        public List<Rol> ListarRoles()
        {
            List<Rol> lista = new List<Rol>();
            using (OracleConnection conn = conexion.LeerConexion())
            {
                string query = "SELECT ID_ROL_USUARIO, NOMBRE FROM ROL";
                OracleCommand cmd = new OracleCommand(query, conn);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Rol
                        {
                            IdRolUsuario = Convert.ToInt32(reader["ID_ROL_USUARIO"]),
                            Nombre = reader["NOMBRE"].ToString()
                        });
                    }
                }
            }
            return lista;
        }

        public Usuario ObtenerUsuarioParaLogin(string username, string password)
        {
            Usuario user = null;
            using (OracleConnection conn = conexion.LeerConexion())
            {
                string query = "SELECT ID_USUARIO, NOMBRE_USER, ID_ROL_USUARIO, ID_DIRECCION FROM USUARIOS WHERE NOMBRE_USER = :u AND CONTRASENA = :p";

                OracleCommand cmd = new OracleCommand(query, conn);
                cmd.Parameters.Add("u", username);
                cmd.Parameters.Add("p", password);

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = new Usuario
                            {
                                // Protección total contra DBNull en todos los campos
                                IdUsuario = reader["ID_USUARIO"] != DBNull.Value
                                            ? Convert.ToInt32(reader["ID_USUARIO"]) : 0,

                                NombreUser = reader["NOMBRE_USER"] != DBNull.Value
                                             ? reader["NOMBRE_USER"].ToString() : "Sin Nombre",

                                IdRolUsuario = reader["ID_ROL_USUARIO"] != DBNull.Value
                                               ? Convert.ToInt32(reader["ID_ROL_USUARIO"]) : 0,

                                IdDireccion = reader["ID_DIRECCION"] != DBNull.Value
                                              ? Convert.ToInt32(reader["ID_DIRECCION"]) : 0
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al obtener datos de login: " + ex.Message);
                }
            }
            return user;
        }

    }
}
