using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using System.IO;

namespace LaVeguita.DAL
{
    public class Conexion
    {
        // Esta variable estática recordará si ya configuramos la Wallet
        private static bool _configurado = false;
        private string walletPath = @"C:\Users\Equipo\Desktop\Wallet_VCDB2026";

        public OracleConnection LeerConexion()
        {
            // Solo configuramos si es la primera vez que se llama en toda la vida de la App
            if (!_configurado)
            {
                OracleConfiguration.TnsAdmin = walletPath;
                OracleConfiguration.WalletLocation = walletPath;
                _configurado = true; // Marcamos como listo
            }

            string stringConexion = "User Id=admin;Password=@Gyoutube160661;Data Source=vcdb2026_high;";

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
