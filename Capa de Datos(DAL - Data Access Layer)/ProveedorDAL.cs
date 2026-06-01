using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using LaVeguita.Entities;

namespace LaVeguita.DAL
{
    public class ProveedorDAL
    {
        private readonly Conexion _conexion = new Conexion();

        // 1. LISTAR PROVEEDORES
        public List<Proveedor> ListarProveedores()
        {
            List<Proveedor> lista = new List<Proveedor>();
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                string query = "SELECT ID_PROVEEDOR, NOM_PROVEEDOR, FONO_PROVEEDOR, CORREO_PROVEEDOR, RUT_PROVEEDOR, ID_DIRECCION FROM PROVEEDOR ORDER BY ID_PROVEEDOR DESC";
                OracleCommand cmd = new OracleCommand(query, cn);
                try
                {
                    cn.Open();
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new Proveedor
                            {
                                IdProveedor = Convert.ToInt32(dr["ID_PROVEEDOR"]),
                                NomProveedor = dr["NOM_PROVEEDOR"].ToString(),
                                FonoProveedor = Convert.ToInt64(dr["FONO_PROVEEDOR"]),
                                CorreoProveedor = dr["CORREO_PROVEEDOR"].ToString(),
                                RutProveedor = dr["RUT_PROVEEDOR"].ToString(),
                                IdDireccion = dr["ID_DIRECCION"] != DBNull.Value ? Convert.ToInt32(dr["ID_DIRECCION"]) : 0
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error en ProveedorDAL al listar: " + ex.Message);
                }
            }
            return lista;
        }

        // 2. JUGADA MAESTRA: INSERTAR PROVEEDOR Y DIRECCIÓN EN UNA SOLA TRANSACCIÓN
        public bool RegistrarProveedorCompleto(Proveedor nuevoProv, Direccion nuevaDir)
        {
            DireccionDAL dirDal = new DireccionDAL();

            using (OracleConnection cn = _conexion.LeerConexion())
            {
                cn.Open();
                // Iniciamos la transacción atómica
                using (OracleTransaction tr = cn.BeginTransaction())
                {
                    try
                    {
                        // A. Insertamos primero la dirección del proveedor y capturamos su nuevo ID
                        int idDireccionGenerado = dirDal.InsertarDireccionRetornandoId(nuevaDir, cn);

                        // B. Asignamos ese ID fresco al objeto proveedor antes de guardarlo
                        nuevoProv.IdDireccion = idDireccionGenerado;

                        // C. Insertamos el Proveedor en su respectiva tabla usando su secuencia
                        // Nota: Si tu secuencia de proveedor se llama distinto (ej: SEQ_PROVEEDOR), ajusta el nombre aquí.
                        string queryProv = @"INSERT INTO PROVEEDOR (ID_PROVEEDOR, NOM_PROVEEDOR, FONO_PROVEEDOR, CORREO_PROVEEDOR, RUT_PROVEEDOR, ID_DIRECCION) 
                                             VALUES (SEQ_PROVEEDOR.NEXTVAL, :nombre, :fono, :correo, :rut, :idDir)";

                        using (OracleCommand cmdProv = new OracleCommand(queryProv, cn))
                        {
                            cmdProv.BindByName = true;
                            cmdProv.Parameters.Add("nombre", OracleDbType.Varchar2).Value = nuevoProv.NomProveedor;
                            cmdProv.Parameters.Add("fono", OracleDbType.Int64).Value = nuevoProv.FonoProveedor;
                            cmdProv.Parameters.Add("correo", OracleDbType.Varchar2).Value = nuevoProv.CorreoProveedor;
                            cmdProv.Parameters.Add("rut", OracleDbType.Varchar2).Value = nuevoProv.RutProveedor;
                            cmdProv.Parameters.Add("idDir", OracleDbType.Int32).Value = nuevoProv.IdDireccion;

                            cmdProv.ExecuteNonQuery();
                        }

                        // Si ambos inserts fueron exitosos, consolidamos los datos en Oracle Cloud
                        tr.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // Si falla el RUT duplicado o el formato, deshacemos todo para evitar basura en la base de datos
                        tr.Rollback();
                        throw new Exception("Error transaccional al registrar proveedor: " + ex.Message);
                    }
                }
            }
        }

        // 3. ELIMINAR PROVEEDOR
        public bool EliminarProveedor(int id)
        {
            using (OracleConnection cn = _conexion.LeerConexion())
            {
                string query = "DELETE FROM PROVEEDOR WHERE ID_PROVEEDOR = :id";
                using (OracleCommand cmd = new OracleCommand(query, cn))
                {
                    cmd.Parameters.Add("id", OracleDbType.Int32).Value = id;
                    try
                    {
                        cn.Open();
                        return cmd.ExecuteNonQuery() > 0;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error al eliminar proveedor: " + ex.Message);
                    }
                }
            }
        }
    }
}