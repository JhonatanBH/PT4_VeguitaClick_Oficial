using LaVeguita.DAL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace LaVeguita.Web.Controllers
{
    public class ProduccionController : Controller
    {
        private readonly ProduccionDAL _produccionDal = new ProduccionDAL();

        public IActionResult Index()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 2 && rol != 3))
            {
                return RedirectToAction("Login", "Acceso");
            }
            return View();
        }

        public IActionResult ProcesosPlanta()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 2 && rol != 3))
            {
                return RedirectToAction("Login", "Acceso");
            }

            var productoDal = new ProductoDAL();
            ViewBag.LotesPendientes = productoDal.ListarLotesPendientes();

            return View();
        }

        [HttpPost]
        public IActionResult ProcesarLote(int idLote, int cantEntrada, int cantPrimCalidad, int cantSegCalidad, int idProducto, int idProveedor)
        {
            int? idEmpleado = HttpContext.Session.GetInt32("IdUsuario");

            if (idEmpleado == null)
            {
                return Json(new { exito = false, mensaje = "Sesión expirada. Por favor vuelva a iniciar sesión." });
            }

            string mensajeOracle = string.Empty;

            bool resultadoTransaccion = _produccionDal.RegistrarFlujoProduccion(
                idLote, cantEntrada, cantPrimCalidad, cantSegCalidad, idProducto, idEmpleado.Value, idProveedor, out mensajeOracle
            );

            if (resultadoTransaccion) return Json(new { exito = true, mensaje = mensajeOracle });
            else return Json(new { exito = false, mensaje = mensajeOracle });
        }

        [HttpGet]
        public IActionResult Packing()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 2 && rol != 3))
                return RedirectToAction("Index", "Home");

            ViewBag.StockLimpio = _produccionDal.ObtenerStockLimpioMonitor();
            return View();
        }

        [HttpPost]
        public IActionResult ProcesarVentaDirecta(string idMateriaPrima, string idFormatoSalida, int unidadesFabricar)
        {
            if (string.IsNullOrEmpty(idMateriaPrima) || string.IsNullOrEmpty(idFormatoSalida) || unidadesFabricar <= 0)
            {
                TempData["Error"] = "Formato de empaque o cantidad inválida.";
                return RedirectToAction("Packing");
            }

            // A. Calcular el factor de multiplicación para la rebaja de kilos (Malla 3kg = 3 veces las unidades)
            decimal factor = 1m;
            if (idFormatoSalida == "malla_3kg_papa")
            {
                factor = 3m;
            }

            decimal kilosTotales = unidadesFabricar * factor;

            // B. Mapear claves de texto a IDs reales de tu tabla de PRODUCTOS
            int idProductoFinal = MapearFormatoAId(idFormatoSalida);

            if (idProductoFinal == 0)
            {
                TempData["Error"] = "El SKU comercial seleccionado no está enlazado a un ID de producto activo.";
                return RedirectToAction("Packing");
            }

            // C. Consumir la lógica transaccional de la Capa DAL
            bool exito = _produccionDal.ProcesarEmpaqueDirecto(idMateriaPrima, idProductoFinal, unidadesFabricar, kilosTotales);

            if (exito)
            {
                TempData["Mensaje"] = $"Empaque exitoso: {unidadesFabricar} unidades enviadas a tienda. Se descontaron {kilosTotales} Kg de planta.";
            }
            else
            {
                TempData["Error"] = "No se pudo completar el movimiento de stock. Verifique disponibilidad en planta.";
            }

            return RedirectToAction("Packing");
        }

        [HttpPost]
        public IActionResult ProcesarKits(string idFormatoKit, int cantidadCajas)
        {
            if (string.IsNullOrEmpty(idFormatoKit) || cantidadCajas <= 0)
            {
                TempData["Error"] = "Especifique un tipo de kit y cantidades válidas.";
                return RedirectToAction("Packing");
            }

            // A. Definir recetas correspondientes a los programas nutricionales de la vista
            var recetasKits = new Dictionary<string, Dictionary<string, decimal>>
            {
                { "soltero", new Dictionary<string, decimal> { { "papa", 1.5m }, { "zanahoria", 1.0m }, { "manzana", 1.5m } } },
                { "pareja", new Dictionary<string, decimal> { { "papa", 3.0m }, { "zanahoria", 2.0m }, { "manzana", 3.0m } } },
                { "familiar3", new Dictionary<string, decimal> { { "papa", 4.5m }, { "zanahoria", 3.0m }, { "manzana", 4.5m } } },
                { "familiar4", new Dictionary<string, decimal> { { "papa", 6.0m }, { "zanahoria", 4.0m }, { "manzana", 6.0m } } }
            };

            if (!recetasKits.ContainsKey(idFormatoKit.ToLower()))
            {
                TempData["Error"] = "Formato nutricional no reconocido.";
                return RedirectToAction("Packing");
            }

            // B. Asignar un ID de producto de venta para el Kit (Usamos el ID 20 de pasas/kits genéricos de tu tabla)
            int idProductoKit = 20;

            var recetaSeleccionada = recetasKits[idFormatoKit.ToLower()];
            bool exito = _produccionDal.ProcesarKitsNutricionales(idProductoKit, cantidadCajas, recetaSeleccionada);

            if (exito) TempData["Mensaje"] = $"📦 PROGRAMA NUTRICIONAL: Se ensamblaron {cantidadCajas} cajas de tipo [{idFormatoKit.ToUpper()}] correctamente.";
            else TempData["Error"] = "Fallo en el descuento de ingredientes de la receta. Verifique los balances de stock limpio.";

            return RedirectToAction("Packing");
        }

        // =========================================================
        // TRADUCTORES LÓGICOS DE SEGURIDAD
        // =========================================================
        private int MapearFormatoAId(string formato)
        {
            // Vinculación real a los IDs de tu tabla PRODUCTOS según tu DDL enviado
            switch (formato)
            {
                // === FORMATOS DE PRIMERA / PREMIUM ===
                case "malla_1kg_papa": return 4;    // Se mapea a Manzana Roja Editada (Úsala como papa de prueba)
                case "malla_3kg_papa": return 18;   // Mapeado a Tomate en tu DDL (Úsalo como formato grande)
                case "bolsa_1kg_zan": return 5;     // Mapeado a Lentejas en tu DDL
                case "bandeja_1kg_man": return 9;   // Mapeado a Manzana en tu DDL

                // === FORMATOS DE SEGUNDA CALIDAD / ECONÓMICOS ===
                case "papa_2da": return 11;         // Mapeado a Pajaros (Úsalo para simular papas de segunda baratas)
                case "zanahoria_2da": return 12;    // Mapeado a Plátanos (Úsalo para simular zanahoria económica)
                case "manzana_2da": return 19;      // Mapeado a Uva en tu DDL (Úsalo como manzana madura)
                default: return 0;
            }
        }

        [HttpGet]
        public IActionResult Reportes(string tipo)
        {
            ViewBag.TipoReporte = tipo?.ToUpper() ?? "GENERAL";
            ViewBag.FechaEmision = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            return View("Reportes");
        }


        [HttpPost]
        public IActionResult GenerarBackupAnual()
        {
            string nombreGerente = HttpContext.Session.GetString("UsuarioNombre") ?? "Gerente General";
            int exito = 0;
            string mensajeOracle = string.Empty;

            // 1. Llamamos a Oracle para procesar y auditar el respaldo interno
            try
            {
                using (OracleConnection cn = new Conexion().LeerConexion())
                {
                    using (OracleCommand cmd = new OracleCommand("ADMIN.SP_EJECUTAR_BACKUP_ANUAL", cn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("p_usuario_gerente", OracleDbType.Varchar2).Value = nombreGerente;

                        cmd.Parameters.Add("p_exito", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("p_mensaje", OracleDbType.Varchar2, 200).Direction = ParameterDirection.Output;

                        cn.Open();
                        cmd.ExecuteNonQuery();

                        exito = Convert.ToInt32(cmd.Parameters["p_exito"].Value);
                        mensajeOracle = cmd.Parameters["p_mensaje"].Value.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al conectar con Oracle: " + ex.Message;
                return RedirectToAction("Index");
            }

            // 2. Si Oracle guardó el registro con éxito, generamos el archivo físico descargable en caliente
            if (exito == 1)
            {
                // Simulamos un volcado de datos estructurado (Data Dump) en formato CSV
                var sb = new StringBuilder();
                sb.AppendLine("=== LA VEGUITA CLICK - RESPALDO LOGICO ANUAL ===");
                sb.AppendLine($"Fecha Emision: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");
                sb.AppendLine($"Responsable: {nombreGerente}");
                sb.AppendLine("================================================");
                sb.AppendLine();
                sb.AppendLine("TABLA_PRODUCTOS_DUMP;");
                sb.AppendLine("ID_PRODUCTO;NOM_PRODUCTO;STOCK_ACTUAL;PRECIO_UND");
                sb.AppendLine("4;Manzana Premium;150;1200");
                sb.AppendLine("5;Zanahoria Bolsa;95;890");
                sb.AppendLine("18;Malla de Papa 3kg;120;3200");
                sb.AppendLine("20;Kit Nutricional;45;7500");
                sb.AppendLine();
                sb.AppendLine("=== FIN DEL RESPALDO ===");

                byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
                string nombreArchivo = $"Backup_Anual_LaVeguita_{DateTime.Now.Year}.sql";

                // Devolvemos el archivo directamente al navegador para su descarga física automática
                return File(buffer, "application/octet-stream", nombreArchivo);
            }
            else
            {
                TempData["Error"] = mensajeOracle;
                return RedirectToAction("Index");
            }
        }



    }
}