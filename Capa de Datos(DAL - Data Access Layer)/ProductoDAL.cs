using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using LaVeguita.Entities;
using LaVeguita.DAL;

namespace LaVeguita.DAL
{
    public class ProductoDAL
    {
        private readonly Conexion conexion = new Conexion();

        // Método para listar todos los productos con su tipo (JOIN)
        public List<Producto> ListarProductos()
        {
            List<Producto> lista = new List<Producto>();

            using (OracleConnection conn = conexion.LeerConexion())
            {
                string query = @"SELECT p.ID_PRODUCTO, p.NOM_PRODUCTO, p.DESCRIPCION, p.PRECIO_UND, 
                                       p.STOCK_ACTUAL, p.STOCK_MIN, p.ES_ORGANICO, p.ID_TIPO_PRODUCTO,
                                       t.NOM_TIPO
                                FROM PRODUCTOS p
                                JOIN TIPO_PRODUCTO t ON p.ID_TIPO_PRODUCTO = t.ID_TIPO_PRODUCTO
                                ORDER BY p.ID_PRODUCTO DESC";

                OracleCommand cmd = new OracleCommand(query, conn);

                try
                {
                    conn.Open();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Producto prod = new Producto
                            {
                                // Blindaje contra nulos aplicado aquí
                                IdProducto = reader["ID_PRODUCTO"] != DBNull.Value ? Convert.ToInt32(reader["ID_PRODUCTO"]) : 0,
                                NomProducto = reader["NOM_PRODUCTO"] != DBNull.Value ? reader["NOM_PRODUCTO"].ToString() : "",
                                Descripcion = reader["DESCRIPCION"] != DBNull.Value ? reader["DESCRIPCION"].ToString() : "",
                                PrecioUnd = reader["PRECIO_UND"] != DBNull.Value ? Convert.ToDecimal(reader["PRECIO_UND"]) : 0,
                                StockActual = reader["STOCK_ACTUAL"] != DBNull.Value ? Convert.ToInt32(reader["STOCK_ACTUAL"]) : 0,
                                StockMin = reader["STOCK_MIN"] != DBNull.Value ? Convert.ToInt32(reader["STOCK_MIN"]) : 0,
                                EsOrganico = reader["ES_ORGANICO"] != DBNull.Value ? reader["ES_ORGANICO"].ToString() : "",
                                IdTipoProducto = reader["ID_TIPO_PRODUCTO"] != DBNull.Value ? Convert.ToInt32(reader["ID_TIPO_PRODUCTO"]) : 0,
                                NomTipo = reader["NOM_TIPO"] != DBNull.Value ? reader["NOM_TIPO"].ToString() : ""
                            };
                            lista.Add(prod);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error en ProductoDAL al listar: " + ex.Message);
                }
            }
            return lista;
        }

        public bool InsertarProducto(Producto p)
        {
            using (OracleConnection con = conexion.LeerConexion())
            {
                string query = @"INSERT INTO PRODUCTOS (
                            ID_PRODUCTO, NOM_PRODUCTO, DESCRIPCION, PRECIO_UND, 
                            STOCK_ACTUAL, ID_PROVEEDOR, STOCK_MIN, STOCK_MAX, 
                            PESO_UNIT_ESTIMADO, ES_ORGANICO, ID_TIPO_PRODUCTO, 
                            ID_CALIDAD, PACKING
                        ) VALUES (
                            SEQ_PRODUCTOS.NEXTVAL, :nom, :descr, :precio, 
                            :stock, :prov, :smin, :smax, 
                            :peso, :organico, :tipoID, 
                            :calid, :pack
                        )";

                OracleCommand cmd = new OracleCommand(query, con);

                cmd.BindByName = true;

                cmd.Parameters.Add("nom", p.NomProducto);
                cmd.Parameters.Add("descr", (object)p.Descripcion ?? DBNull.Value);
                cmd.Parameters.Add("precio", p.PrecioUnd);
                cmd.Parameters.Add("stock", p.StockActual);
                cmd.Parameters.Add("prov", p.IdProveedor);
                cmd.Parameters.Add("smin", p.StockMin);
                cmd.Parameters.Add("smax", p.StockMax);
                cmd.Parameters.Add("peso", p.PesoUnitEstimado);
                cmd.Parameters.Add("organico", p.EsOrganico);
                cmd.Parameters.Add("tipoID", p.IdTipoProducto);
                cmd.Parameters.Add("calid", p.IdCalidad);
                cmd.Parameters.Add("pack", (object)p.Packing ?? DBNull.Value);

                con.Open();
                int filas = cmd.ExecuteNonQuery();
                return filas > 0;
            }
        }

        public List<TipoProducto> ListarTipos()
        {
            List<TipoProducto> lista = new List<TipoProducto>();
            using (OracleConnection con = conexion.LeerConexion())
            {
                string query = "SELECT ID_TIPO_PRODUCTO, NOM_TIPO FROM TIPO_PRODUCTO ORDER BY NOM_TIPO ASC";
                OracleCommand cmd = new OracleCommand(query, con);
                con.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new TipoProducto
                        {
                            // Blindaje aplicado a dr.GetInt32 y dr.GetString
                            IdTipoProducto = dr["ID_TIPO_PRODUCTO"] != DBNull.Value ? Convert.ToInt32(dr["ID_TIPO_PRODUCTO"]) : 0,
                            NomTipo = dr["NOM_TIPO"] != DBNull.Value ? dr["NOM_TIPO"].ToString() : ""
                        });
                    }
                }
            }
            return lista;
        }

        // 1. Para llenar el DropDownList en la Vista
        public List<Proveedor> ListarProveedores()
        {
            List<Proveedor> lista = new List<Proveedor>();
            using (var conn = conexion.LeerConexion())
            {
                string sql = "SELECT ID_PROVEEDOR, NOM_PROVEEDOR FROM PROVEEDORES ORDER BY NOM_PROVEEDOR";
                OracleCommand cmd = new OracleCommand(sql, conn);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new Proveedor
                        {
                            // Blindaje contra nulos aplicado aquí
                            IdProveedor = dr["ID_PROVEEDOR"] != DBNull.Value ? Convert.ToInt32(dr["ID_PROVEEDOR"]) : 0,
                            NomProveedor = dr["NOM_PROVEEDOR"] != DBNull.Value ? dr["NOM_PROVEEDOR"].ToString() : ""
                        });
                    }
                }
            }
            return lista;
        }

        // 2. Método Editar
        public bool EditarProducto(Producto p)
        {
            using (var conn = conexion.LeerConexion())
            {
                string sql = @"UPDATE PRODUCTOS SET 
                        NOM_PRODUCTO = :v_nom, 
                        DESCRIPCION = :v_desc, 
                        PRECIO_UND = :v_precio, 
                        STOCK_ACTUAL = :v_stock, 
                        ID_PROVEEDOR = :v_idProv, 
                        STOCK_MIN = :v_sMin, 
                        STOCK_MAX = :v_sMax, 
                        PESO_UNIT_ESTIMADO = :v_peso, 
                        ES_ORGANICO = :v_org, 
                        ID_TIPO_PRODUCTO = :v_tipo, 
                        ID_CALIDAD = :v_calidad, 
                        PACKING = :v_pack
                      WHERE ID_PRODUCTO = :v_id";

                OracleCommand cmd = new OracleCommand(sql, conn);
                cmd.BindByName = true; // Indispensable

                cmd.Parameters.Add("v_nom", p.NomProducto);
                cmd.Parameters.Add("v_desc", (object)p.Descripcion ?? DBNull.Value);
                cmd.Parameters.Add("v_precio", p.PrecioUnd);
                cmd.Parameters.Add("v_stock", p.StockActual);
                cmd.Parameters.Add("v_idProv", p.IdProveedor > 0 ? (object)p.IdProveedor : DBNull.Value);
                cmd.Parameters.Add("v_sMin", p.StockMin);
                cmd.Parameters.Add("v_sMax", p.StockMax);
                cmd.Parameters.Add("v_peso", p.PesoUnitEstimado);
                cmd.Parameters.Add("v_org", p.EsOrganico);
                cmd.Parameters.Add("v_tipo", p.IdTipoProducto);
                cmd.Parameters.Add("v_calidad", p.IdCalidad > 0 ? (object)p.IdCalidad : DBNull.Value);
                cmd.Parameters.Add("v_pack", (object)p.Packing ?? DBNull.Value);
                cmd.Parameters.Add("v_id", p.IdProducto);

                try
                {
                    conn.Open();
                    int filas = cmd.ExecuteNonQuery();
                    return filas > 0;
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al ejecutar UPDATE en Oracle: " + ex.Message);
                }
            }
        }

        // 3. Método Eliminar
        public bool EliminarProducto(int id)
        {
            Conexion con = new Conexion();
            using (var conn = con.LeerConexion())
            {
                string sql = "DELETE FROM PRODUCTOS WHERE ID_PRODUCTO = :id";
                OracleCommand cmd = new OracleCommand(sql, conn);
                cmd.Parameters.Add("id", id);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // Este método debe estar dentro de la clase ProductoDAL en LaVeguita.DAL
        public Producto ObtenerPorId(int id)
        {
            Producto p = null;
            using (OracleConnection conn = conexion.LeerConexion())
            {
                string query = "SELECT * FROM PRODUCTOS WHERE ID_PRODUCTO = :id";
                OracleCommand cmd = new OracleCommand(query, conn);
                cmd.Parameters.Add("id", id);

                try
                {
                    conn.Open();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            p = new Producto
                            {
                                // Blindaje total contra nulos en cada propiedad
                                IdProducto = reader["ID_PRODUCTO"] != DBNull.Value ? Convert.ToInt32(reader["ID_PRODUCTO"]) : 0,
                                NomProducto = reader["NOM_PRODUCTO"] != DBNull.Value ? reader["NOM_PRODUCTO"].ToString() : "",
                                Descripcion = reader["DESCRIPCION"] != DBNull.Value ? reader["DESCRIPCION"].ToString() : "",
                                PrecioUnd = reader["PRECIO_UND"] != DBNull.Value ? Convert.ToDecimal(reader["PRECIO_UND"]) : 0,
                                StockActual = reader["STOCK_ACTUAL"] != DBNull.Value ? Convert.ToInt32(reader["STOCK_ACTUAL"]) : 0,
                                IdProveedor = reader["ID_PROVEEDOR"] != DBNull.Value ? Convert.ToInt32(reader["ID_PROVEEDOR"]) : 0,
                                StockMin = reader["STOCK_MIN"] != DBNull.Value ? Convert.ToInt32(reader["STOCK_MIN"]) : 0,
                                StockMax = reader["STOCK_MAX"] != DBNull.Value ? Convert.ToInt32(reader["STOCK_MAX"]) : 0,
                                PesoUnitEstimado = reader["PESO_UNIT_ESTIMADO"] != DBNull.Value ? Convert.ToDecimal(reader["PESO_UNIT_ESTIMADO"]) : 0,
                                EsOrganico = reader["ES_ORGANICO"] != DBNull.Value ? reader["ES_ORGANICO"].ToString() : "",
                                IdTipoProducto = reader["ID_TIPO_PRODUCTO"] != DBNull.Value ? Convert.ToInt32(reader["ID_TIPO_PRODUCTO"]) : 0,
                                IdCalidad = reader["ID_CALIDAD"] != DBNull.Value ? Convert.ToInt32(reader["ID_CALIDAD"]) : 0,
                                Packing = reader["PACKING"] != DBNull.Value ? reader["PACKING"].ToString() : ""
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al obtener el producto para editar: " + ex.Message);
                }
            }
            return p;
        }

        public List<Calidad> ListarCalidades()
        {
            List<Calidad> lista = new List<Calidad>();
            using (var conn = conexion.LeerConexion())
            {
                string sql = "SELECT ID_CALIDAD, NOM_CALIDAD FROM CALIDAD ORDER BY NOM_CALIDAD";
                OracleCommand cmd = new OracleCommand(sql, conn);
                try
                {
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new Calidad
                            {
                                // Blindaje contra nulos aplicado aquí
                                IdCalidad = dr["ID_CALIDAD"] != DBNull.Value ? Convert.ToInt32(dr["ID_CALIDAD"]) : 0,
                                NomCalidad = dr["NOM_CALIDAD"] != DBNull.Value ? dr["NOM_CALIDAD"].ToString() : ""
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al listar calidades: " + ex.Message);
                }
            }
            return lista;
        }

    }
}