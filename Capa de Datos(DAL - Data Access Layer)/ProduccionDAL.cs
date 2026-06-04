using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace LaVeguita.DAL
{
    public class ProduccionDAL
    {
        public bool RegistrarFlujoProduccion(int idLote, int cantEntrada, int cantPrimCalidad, int cantSegCalidad, int idProducto, int idEmpleado, int idProveedor, out string mensajeResultado)
        {
            bool exitoTransaccion = false;
            mensajeResultado = string.Empty;

            try
            {
                using (OracleConnection cn = new Conexion().LeerConexion())
                {
                    using (OracleCommand cmd = new OracleCommand("SP_REGISTRAR_PRODUCCION", cn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // 🚀 PARAMETROS DE ENTRADA (IN) - Incluye el nuevo idLote al inicio
                        cmd.Parameters.Add("p_id_lote", OracleDbType.Int32).Value = idLote;
                        cmd.Parameters.Add("p_cant_entrada", OracleDbType.Int32).Value = cantEntrada;
                        cmd.Parameters.Add("p_cant_prim_calidad", OracleDbType.Int32).Value = cantPrimCalidad;
                        cmd.Parameters.Add("p_cant_seg_calidad", OracleDbType.Int32).Value = cantSegCalidad;
                        cmd.Parameters.Add("p_id_producto", OracleDbType.Int32).Value = idProducto;
                        cmd.Parameters.Add("p_id_empleado", OracleDbType.Int32).Value = idEmpleado;
                        cmd.Parameters.Add("p_id_proveedor", OracleDbType.Int32).Value = idProveedor;

                        // PARAMETROS DE SALIDA (OUT)
                        OracleParameter pExito = new OracleParameter("p_exito", OracleDbType.Int32, ParameterDirection.Output);
                        OracleParameter pMensaje = new OracleParameter("p_mensaje", OracleDbType.Varchar2, 200, null, ParameterDirection.Output);

                        cmd.Parameters.Add(pExito);
                        cmd.Parameters.Add(pMensaje);

                        cn.Open();
                        cmd.ExecuteNonQuery();

                        // Leer los resultados del procedimiento almacenado en Oracle
                        int resultado = Convert.ToInt32(pExito.Value.ToString());
                        mensajeResultado = pMensaje.Value.ToString();

                        if (resultado == 1)
                        {
                            exitoTransaccion = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                exitoTransaccion = false;
                mensajeResultado = "Error critico en la capa DAL: " + ex.Message;
            }

            return exitoTransaccion;
        }
    }
}