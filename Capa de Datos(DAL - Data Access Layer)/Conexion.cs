using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.IO;

namespace LaVeguita.DAL
{
    public class Conexion
    {
        private static bool _configurado = false;

        public OracleConnection LeerConexion()
        {
            if (!_configurado)
            {
                // 1. Ruta base donde se ejecuta la app (en Docker será /app, en VS será /bin/Debug/...)
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // Intentamos buscar la Wallet al lado del ejecutable (Caso Docker)
                string walletPath = Path.Combine(baseDir, "Wallet_VCDB2026");

                // 2. Si no existe ahí (Caso Visual Studio en Desarrollo), subimos carpetas hasta la raíz
                if (!Directory.Exists(walletPath))
                {
                    // Retrocedemos de /bin/Debug/net8.0/ hasta la raíz del proyecto para buscarla
                    DirectoryInfo parent = Directory.GetParent(baseDir); // net8.0
                    if (parent != null) parent = parent.Parent;         // Debug
                    if (parent != null) parent = parent.Parent;         // bin
                    if (parent != null) parent = parent.Parent;         // LaVeguita.Web o proyecto ejecutor

                    if (parent != null)
                    {
                        // Buscamos en la raíz de la solución (donde la pegamos hoy)
                        string rutaSolucion = Path.Combine(parent.Parent.FullName, "Wallet_VCDB2026");
                        if (Directory.Exists(rutaSolucion))
                        {
                            walletPath = rutaSolucion;
                        }
                    }
                }

                // 3. Inyectamos la ruta final encontrada en la configuración de Oracle
                OracleConfiguration.TnsAdmin = walletPath;
                OracleConfiguration.WalletLocation = walletPath;
                _configurado = true;
            }

            string stringConexion = "User Id=admin;Password=@Gyoutube160661;Data Source=vcdb2026_high;Validate Connection=true;Connection Timeout=60;";

            try
            {
                return new OracleConnection(stringConexion);
            }
            catch (Exception ex)
            {
                throw new Exception("Error en la conexión: " + ex.Message);
            }
        }
    }
}