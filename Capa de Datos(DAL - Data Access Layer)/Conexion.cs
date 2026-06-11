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
            // 1. Intentar leer la cadena limpia desde Azure (Environment Variable directa o Connection String)
            string stringConexion = Environment.GetEnvironmentVariable("DefaultConnection")
                                 ?? Environment.GetEnvironmentVariable("SQLAZURECONNSTR_DefaultConnection")
                                 ?? Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection");

            // 2. Si no existe en internet (caso local en tu PC con tu Wallet local)
            if (string.IsNullOrEmpty(stringConexion))
            {
                if (!_configurado)
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string walletPath = Path.Combine(baseDir, "Wallet_VCDB2026");

                    if (!Directory.Exists(walletPath))
                    {
                        DirectoryInfo parent = Directory.GetParent(baseDir);
                        if (parent != null) parent = parent.Parent;
                        if (parent != null) parent = parent.Parent;
                        if (parent != null) parent = parent.Parent;

                        if (parent != null)
                        {
                            string rutaSolucion = Path.Combine(parent.Parent.FullName, "Wallet_VCDB2026");
                            if (Directory.Exists(rutaSolucion))
                            {
                                walletPath = rutaSolucion;
                            }
                        }
                    }

                    OracleConfiguration.TnsAdmin = walletPath;
                    OracleConfiguration.WalletLocation = walletPath;
                    _configurado = true;
                }

                // Tu conexión local de siempre con la mTLS requerida por la Wallet local
                stringConexion = "User Id=admin;Password=@Gyoutube160661;Data Source=vcdb2026_high;Validate Connection=true;Connection Timeout=60;";
            }

            try
            {
                return new OracleConnection(stringConexion);
            }
            catch (Exception ex)
            {
                throw new Exception("Error en la configuración o sintaxis de la conexión: " + ex.Message);
            }
        }
    }
}