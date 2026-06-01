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
                string query = "INSERT INTO USUARIOS (ID_USUARIO, NOMBRE_USER, CONTRASENA, FONO, CORREO_USU, ID_ROL_USUARIO, ID_DIRECCION) " +
                               "VALUES (SEQ_USUARIO.NEXTVAL, :nombre, :pass, :fono, :correo, :idRol, :idDir)";

                OracleCommand cmd = new OracleCommand(query, conn);
                cmd.BindByName = true;

                cmd.Parameters.Add("nombre", OracleDbType.Varchar2).Value = nuevoUser.NombreUser;
                cmd.Parameters.Add("pass", OracleDbType.Varchar2).Value = nuevoUser.Contrasena;
                cmd.Parameters.Add("fono", OracleDbType.Int64).Value = nuevoUser.Fono;
                cmd.Parameters.Add("correo", OracleDbType.Varchar2).Value = nuevoUser.CorreoUsu;
                cmd.Parameters.Add("idRol", OracleDbType.Int32).Value = nuevoUser.IdRolUsuario;

                // JUGADA DE PROTECCIÓN: Si el ID es 0 o menor (o sea, no se ingresó dirección), 
                // le pasamos DBNull.Value para que Oracle lo guarde como NULL en vez de rebotar con error.
                cmd.Parameters.Add("idDir", OracleDbType.Int32).Value = nuevoUser.IdDireccion > 0 ? nuevoUser.IdDireccion : (object)DBNull.Value;

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
                cmd.BindByName = true; // <-- AGREGA ESTO SIEMPRE

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

        public bool RegistrarUsuarioCompleto(Usuario nuevoUser, Direccion nuevaDir)
        {
            DireccionDAL dirDal = new DireccionDAL();

            using (OracleConnection conn = conexion.LeerConexion())
            {
                conn.Open();
                // INICIAMOS TRANSACCIÓN: Si falla el usuario, la dirección se borra automáticamente
                using (OracleTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Insertamos la dirección y capturamos su nuevo ID real (Ej: 101, 102...)
                        int idDireccionGenerado = dirDal.InsertarDireccionRetornandoId(nuevaDir, conn);

                        // 2. Asignamos ese ID real al usuario antes de guardarlo
                        nuevoUser.IdDireccion = idDireccionGenerado;

                        // 3. Insertamos el Usuario
                        string queryUser = @"INSERT INTO USUARIOS (ID_USUARIO, NOMBRE_USER, CONTRASENA, FONO, CORREO_USU, ID_ROL_USUARIO, ID_DIRECCION) 
                                     VALUES (SEQ_USUARIO.NEXTVAL, :nombre, :pass, :fono, :correo, :idRol, :idDir)";

                        using (OracleCommand cmdUser = new OracleCommand(queryUser, conn))
                        {
                            cmdUser.BindByName = true;
                            cmdUser.Parameters.Add("nombre", OracleDbType.Varchar2).Value = nuevoUser.NombreUser;
                            cmdUser.Parameters.Add("pass", OracleDbType.Varchar2).Value = nuevoUser.Contrasena;
                            cmdUser.Parameters.Add("fono", OracleDbType.Int64).Value = nuevoUser.Fono;
                            cmdUser.Parameters.Add("correo", OracleDbType.Varchar2).Value = nuevoUser.CorreoUsu;
                            cmdUser.Parameters.Add("idRol", OracleDbType.Int32).Value = nuevoUser.IdRolUsuario;

                            // AQUÍ ESTÁ LA MAGIA: Pasamos el ID real de la base de datos
                            cmdUser.Parameters.Add("idDir", OracleDbType.Int32).Value = nuevoUser.IdDireccion;

                            cmdUser.ExecuteNonQuery();
                        }

                        // Si todo salió perfecto, confirmamos los cambios en Oracle
                        tr.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // Si algo falla (ej: correo duplicado), cancelamos todo para que no queden direcciones huérfanas
                        tr.Rollback();
                        throw new Exception("Error al registrar cliente y dirección: " + ex.Message);
                    }
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

        public List<LaVeguita.Entities.Empleado> ListarEmpleadosDespacho()
        {
            List<LaVeguita.Entities.Empleado> lista = new List<LaVeguita.Entities.Empleado>();
            using (OracleConnection conn = conexion.LeerConexion())
            {
                // AGREGAMOS ID_ROL_USUARIO A LA CONSULTA SQL
                string query = "SELECT ID_EMPLEADO, PNOMBRE_EMP, APPATERNO, ID_ROL_USUARIO FROM EMPLEADOS ORDER BY PNOMBRE_EMP ASC";
                OracleCommand cmd = new OracleCommand(query, conn);

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new LaVeguita.Entities.Empleado
                            {
                                IdEmpleado = Convert.ToInt32(reader["ID_EMPLEADO"]),
                                PnombreEmp = reader["PNOMBRE_EMP"].ToString(),
                                Appaterno = reader["APPATERNO"].ToString(),
                                // LEEMOS EL ROL DESDE ORACLE CLOUD
                                IdRolUsuario = Convert.ToInt32(reader["ID_ROL_USUARIO"])
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al listar empleados desde la base de datos: " + ex.Message);
                }
            }
            return lista;
        }

    }
}
