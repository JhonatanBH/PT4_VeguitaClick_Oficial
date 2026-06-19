using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using LaVeguita.DAL;
using LaVeguita.Entities;

namespace LaVeguita.Web.Controllers
{
    public class VehiculosController : Controller
    {
        private readonly VehiculoDAL _vehiculoDal = new VehiculoDAL();

        // Filtro de Seguridad Perimetral: Solo Gerente (1) y Asistente Despacho (6)
        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 6))
            {
                context.Result = RedirectToAction("Login", "Acceso");
            }
            base.OnActionExecuting(context);
        }

        // 🚛 MONITOR PRINCIPAL DE LA FLOTA
        [HttpGet]
        public IActionResult Index()
        {
            var flota = _vehiculoDal.ListarVehiculos();
            return View(flota);
        }

        // ➕ INGRESAR NUEVO MÓVIL (GET)
        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        // ➕ INGRESAR NUEVO MÓVIL (POST)
        [HttpPost]
        public IActionResult Crear(Vehiculo nuevoVehiculo)
        {
            ModelState.Remove("IdTransporte");
            ModelState.Remove("Estado");
            ModelState.Remove("KmTotal");

            if (string.IsNullOrEmpty(nuevoVehiculo.TipoMovil) || string.IsNullOrEmpty(nuevoVehiculo.Serial))
            {
                ModelState.AddModelError("", "Por favor, complete todos los campos obligatorios (Serial y Tipo).");
                return View(nuevoVehiculo);
            }

            string tipo = nuevoVehiculo.TipoMovil.ToUpper();

            if (tipo == "BICICLETA" && nuevoVehiculo.CapMaxKg >= 5)
            {
                ModelState.AddModelError("", "Regla de Negocio: Las bicicletas no soportan cargas mayores o iguales a 5kg.");
                return View(nuevoVehiculo);
            }
            if (tipo == "TRICICLO" && nuevoVehiculo.CapMaxKg < 5)
            {
                ModelState.AddModelError("", "Regla de Negocio: Los triciclos operan únicamente con cubicajes superiores o iguales a 5kg.");
                return View(nuevoVehiculo);
            }

            try
            {
                nuevoVehiculo.TipoMovil = tipo;

                bool exito = _vehiculoDal.InsertarVehiculo(nuevoVehiculo);
                if (exito)
                {
                    TempData["ExitoFlota"] = "Vehiculo incorporado a la flota activa correctamente.";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Error: Oracle Cloud no pudo registrar el móvil (Revisar permisos o secuencias).");
                    return View(nuevoVehiculo);
                }
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", "Falla Crítica Oracle: " + ex.Message);
                return View(nuevoVehiculo);
            }
        }

        // =================================================================
        // 🚀 SOLUCIÓN AL ERROR 405: LA PUERTA DE ENTRADA (GET) QUE FALTABA
        // =================================================================
        [HttpGet]
        public IActionResult RegistrarMantencion()
        {
            // Carga los vehículos para que el `<select>` del HTML no se caiga
            ViewBag.Vehiculos = _vehiculoDal.ListarVehiculos();
            return View();
        }

        // 🔧 FORMULARIO DE MANTENCIÓN (POST - CORREGIDO A 4 PARÁMETROS)
        [HttpPost]
        public IActionResult RegistrarMantencion(int idTransporte, string detalleTecnico, int costoMantencion, int diasEstimados)
        {
            if (idTransporte <= 0 || diasEstimados <= 0 || string.IsNullOrEmpty(detalleTecnico))
            {
                ModelState.AddModelError("", "Por favor, complete todos los campos obligatorios del informe técnico.");
                ViewBag.Vehiculos = _vehiculoDal.ListarVehiculos();
                return View();
            }

            // Normalización de caracteres especiales para blindar la base de datos (Regla 12)
            string detalleLimpio = detalleTecnico.Normalize(System.Text.NormalizationForm.FormD)
                                                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u");

            bool exito = _vehiculoDal.RegistrarMantencionFlota(idTransporte, detalleLimpio, costoMantencion, diasEstimados);
            if (exito)
            {
                TempData["ExitoFlota"] = $"Móvil enviado a taller de mantención. Salida estimada en {diasEstimados} días.";
                return RedirectToAction("Index");
            }

            ViewBag.Vehiculos = _vehiculoDal.ListarVehiculos();
            ModelState.AddModelError("", "Falla en la transacción distribuida de Oracle Cloud.");
            return View();
        }

        // =================================================================
        // 🚀 ACCIÓN FALTANTE: DEVOLVER EL VEHÍCULO A LA FLOTA (ALTA TÉCNICA)
        // =================================================================
        [HttpPost]
        public IActionResult HabilitarVehiculo(int idTransporte)
        {
            if (idTransporte <= 0)
            {
                TempData["Error"] = "Identificador de vehículo no válido.";
                return RedirectToAction("Index");
            }

            bool exito = _vehiculoDal.HabilitarVehiculoFlota(idTransporte);
            if (exito)
            {
                TempData["ExitoFlota"] = "El móvil ha completado su mantenimiento y vuelve a estar DISPONIBLE en la flota.";
            }
            else
            {
                TempData["Error"] = "No se pudo actualizar el estado del vehículo en la base de datos.";
            }

            return RedirectToAction("Index");
        }
    }
}