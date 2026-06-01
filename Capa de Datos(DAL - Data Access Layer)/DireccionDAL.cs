using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using LaVeguita.Entities;

namespace LaVeguita.DAL
{
    public class DireccionDAL
    {
        private Conexion _conexion = new Conexion();

        // Inserta la dirección y devuelve el ID automático generado por Oracle
        public int InsertarDireccionRetornandoId(Direccion dir, OracleConnection conn)
        {
            // FÍJATE AQUÍ: No enviamos ID_DIRECCION porque Oracle lo crea solo. 
            // Usamos RETURNING para que nos devuelva cuál ID le asignó.
            string query = @"INSERT INTO DIRECCION (NOMBRE_DIR, NUMERO_DIR, ID_COMUNA) 
                             VALUES (:nom, :num, :com) 
                             RETURNING ID_DIRECCION INTO :outId";

            using (OracleCommand cmd = new OracleCommand(query, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("nom", OracleDbType.Varchar2).Value = dir.NombreDir;
                cmd.Parameters.Add("num", OracleDbType.Int32).Value = dir.NumeroDir;
                cmd.Parameters.Add("com", OracleDbType.Int32).Value = dir.IdComuna;

                // Parámetro de SALIDA para atrapar el ID
                OracleParameter outId = new OracleParameter("outId", OracleDbType.Int32);
                outId.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(outId);

                cmd.ExecuteNonQuery();

                // Convertimos el valor devuelto a entero y lo retornamos
                return Convert.ToInt32(outId.Value.ToString());
            }
        }
    }
}