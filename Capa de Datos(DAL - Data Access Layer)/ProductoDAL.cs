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
                // 1. Agregamos PESO_UNIT_ESTIMADO y UNIDAD_MEDIDA al SELECT
                string query = @"SELECT p.ID_PRODUCTO, p.NOM_PRODUCTO, p.DESCRIPCION, p.PRECIO_UND, 
                                p.STOCK_ACTUAL, p.STOCK_MIN, p.ES_ORGANICO, p.ID_TIPO_PRODUCTO,
                                p.PESO_UNIT_ESTIMADO, p.UNIDAD_MEDIDA,
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
                                // Blindaje contra nulos aplicado aqui
                                IdProducto = reader["ID_PRODUCTO"] != DBNull.Value ? Convert.ToInt32(reader["ID_PRODUCTO"]) : 0,
                                NomProducto = reader["NOM_PRODUCTO"] != DBNull.Value ? reader["NOM_PRODUCTO"].ToString() : "",
                                Descripcion = reader["DESCRIPCION"] != DBNull.Value ? reader["DESCRIPCION"].ToString() : "",
                                PrecioUnd = reader["PRECIO_UND"] != DBNull.Value ? Convert.ToDecimal(reader["PRECIO_UND"]) : 0,
                                StockActual = reader["STOCK_ACTUAL"] != DBNull.Value ? Convert.ToInt32(reader["STOCK_ACTUAL"]) : 0,
                                StockMin = reader["STOCK_MIN"] != DBNull.Value ? Convert.ToInt32(reader["STOCK_MIN"]) : 0,
                                EsOrganico = reader["ES_ORGANICO"] != DBNull.Value ? reader["ES_ORGANICO"].ToString() : "",
                                IdTipoProducto = reader["ID_TIPO_PRODUCTO"] != DBNull.Value ? Convert.ToInt32(reader["ID_TIPO_PRODUCTO"]) : 0,
                                NomTipo = reader["NOM_TIPO"] != DBNull.Value ? reader["NOM_TIPO"].ToString() : "",

                                // 2. AQUI ESTA LA SOLUCION: Leemos el peso y la unidad desde Oracle
                                PesoUnitEstimado = reader["PESO_UNIT_ESTIMADO"] != DBNull.Value ? Convert.ToDecimal(reader["PESO_UNIT_ESTIMADO"]) : 0,
                                UnidadMedida = reader["UNIDAD_MEDIDA"] != DBNull.Value ? reader["UNIDAD_MEDIDA"].ToString() : "UND"
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
                OracleCommand cmd = new OracleCommand("PKG_INVENTARIO.SP_REGISTRAR_PRODUCTO", con);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;

                cmd.Parameters.Add("p_nom", OracleDbType.Varchar2).Value = p.NomProducto;
                cmd.Parameters.Add("p_desc", OracleDbType.Varchar2).Value = (object)p.Descripcion ?? DBNull.Value;
                cmd.Parameters.Add("p_precio", OracleDbType.Decimal).Value = p.PrecioUnd;
                cmd.Parameters.Add("p_stock", OracleDbType.Int32).Value = p.StockActual;
                cmd.Parameters.Add("p_prov", OracleDbType.Int32).Value = p.IdProveedor > 0 ? (object)p.IdProveedor : DBNull.Value;
                cmd.Parameters.Add("p_smin", OracleDbType.Int32).Value = p.StockMin;
                cmd.Parameters.Add("p_smax", OracleDbType.Int32).Value = p.StockMax;
                cmd.Parameters.Add("p_peso", OracleDbType.Decimal).Value = p.PesoUnitEstimado;
                cmd.Parameters.Add("p_org", OracleDbType.Varchar2).Value = p.EsOrganico;
                cmd.Parameters.Add("p_tipo", OracleDbType.Int32).Value = p.IdTipoProducto;
                cmd.Parameters.Add("p_calid", OracleDbType.Int32).Value = p.IdCalidad > 0 ? (object)p.IdCalidad : DBNull.Value;
                cmd.Parameters.Add("p_pack", OracleDbType.Varchar2).Value = (object)p.Packing ?? DBNull.Value;

                // Pasamos la unidad de medida al registrar
                cmd.Parameters.Add("p_uom", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(p.UnidadMedida) ? "UND" : p.UnidadMedida;

                OracleParameter outExito = new OracleParameter("p_exito", OracleDbType.Int32, System.Data.ParameterDirection.Output);
                cmd.Parameters.Add(outExito);

                con.Open();
                cmd.ExecuteNonQuery();
                return Convert.ToInt32(outExito.Value.ToString()) == 1;
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
                OracleCommand cmd = new OracleCommand("PKG_INVENTARIO.SP_ACTUALIZAR_PRODUCTO", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;

                // Pasamos exactamente los mismos parámetros que el Package espera en la base de datos
                cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = p.IdProducto;
                cmd.Parameters.Add("p_nom", OracleDbType.Varchar2).Value = p.NomProducto;
                cmd.Parameters.Add("p_desc", OracleDbType.Varchar2).Value = (object)p.Descripcion ?? DBNull.Value;
                cmd.Parameters.Add("p_precio", OracleDbType.Decimal).Value = p.PrecioUnd;
                cmd.Parameters.Add("p_stock", OracleDbType.Int32).Value = p.StockActual;
                cmd.Parameters.Add("p_prov", OracleDbType.Int32).Value = p.IdProveedor > 0 ? (object)p.IdProveedor : DBNull.Value;
                cmd.Parameters.Add("p_smin", OracleDbType.Int32).Value = p.StockMin;
                cmd.Parameters.Add("p_smax", OracleDbType.Int32).Value = p.StockMax;
                cmd.Parameters.Add("p_peso", OracleDbType.Decimal).Value = p.PesoUnitEstimado;
                cmd.Parameters.Add("p_org", OracleDbType.Varchar2).Value = p.EsOrganico;
                cmd.Parameters.Add("p_tipo", OracleDbType.Int32).Value = p.IdTipoProducto;
                cmd.Parameters.Add("p_calid", OracleDbType.Int32).Value = p.IdCalidad > 0 ? (object)p.IdCalidad : DBNull.Value;
                cmd.Parameters.Add("p_pack", OracleDbType.Varchar2).Value = (object)p.Packing ?? DBNull.Value;

                // --- ESTE ERA EL PARAMETRO FALTRANTE QUE CAUSABA EL ERROR ---
                cmd.Parameters.Add("p_uom", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(p.UnidadMedida) ? "UND" : p.UnidadMedida;

                // Parámetro de salida OUT de control
                OracleParameter outExito = new OracleParameter("p_exito", OracleDbType.Int32, System.Data.ParameterDirection.Output);
                cmd.Parameters.Add(outExito);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return Convert.ToInt32(outExito.Value.ToString()) == 1;
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al ejecutar SP_ACTUALIZAR_PRODUCTO en Cloud: " + ex.Message);
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


        public List<LaVeguita.Entities.DetalleLote> ListarLotesPendientes()
        {
            List<LaVeguita.Entities.DetalleLote> lista = new List<LaVeguita.Entities.DetalleLote>();

            // Consulta SQL adaptada milimetricamente a la nueva tabla DETALLE_LOTES
            string query = @"
        SELECT D.ID_LOTE, D.ID_RECEPCION, D.ID_PRODUCTO, D.PESO_REAL_LOCAL, D.DESCRIPCION_LOTE, D.ESTADO,
               P.NOM_PRODUCTO, P.PESO_UNIT_ESTIMADO, G.ID_PROVEEDOR, PROV.NOM_PROVEEDOR
        FROM DETALLE_LOTES D
        JOIN RECEPCION_GUIAS G ON D.ID_RECEPCION = G.ID_RECEPCION
        JOIN PRODUCTOS P ON D.ID_PRODUCTO = P.ID_PRODUCTO
        JOIN PROVEEDORES PROV ON G.ID_PROVEEDOR = PROV.ID_PROVEEDOR
        WHERE D.ESTADO = 'PENDIENTE'
        ORDER BY D.ID_LOTE ASC";

            try
            {
                using (Oracle.ManagedDataAccess.Client.OracleConnection cn = new Conexion().LeerConexion())
                {
                    using (Oracle.ManagedDataAccess.Client.OracleCommand cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(query, cn))
                    {
                        cn.Open();
                        using (Oracle.ManagedDataAccess.Client.OracleDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                lista.Add(new LaVeguita.Entities.DetalleLote
                                {
                                    IdLote = Convert.ToInt32(dr["ID_LOTE"]),
                                    IdRecepcion = Convert.ToInt32(dr["ID_RECEPCION"]),
                                    IdProducto = Convert.ToInt32(dr["ID_PRODUCTO"]),
                                    PesoRealLocal = Convert.ToDecimal(dr["PESO_REAL_LOCAL"]),
                                    Estado = dr["ESTADO"].ToString(),
                                    DescripcionLote = dr["DESCRIPCION_LOTE"].ToString(),
                                    NomProducto = dr["NOM_PRODUCTO"].ToString(),
                                    PesoUnitEstimado = Convert.ToDecimal(dr["PESO_UNIT_ESTIMADO"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Retorna lista vacia en caso de error transaccional para evitar caidas en cascada
            }

            return lista;
        }




    }
}