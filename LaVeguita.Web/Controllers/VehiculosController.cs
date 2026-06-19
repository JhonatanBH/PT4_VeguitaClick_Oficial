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
            return View(flota); // Envía la lista estructurada a la vista
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
            // 1. Limpieza preventiva: Si no mandan estado o ID, los ignoramos
            ModelState.Remove("IdTransporte");
            ModelState.Remove("Estado");
            ModelState.Remove("KmTotal");

            // Validamos que los campos esenciales vengan con datos
            if (string.IsNullOrEmpty(nuevoVehiculo.TipoMovil) || string.IsNullOrEmpty(nuevoVehiculo.Serial))
            {
                ModelState.AddModelError("", "Por favor, complete todos los campos obligatorios (Serial y Tipo).");
                return View(nuevoVehiculo);
            }

            // Convertimos a mayúsculas para evitar problemas de case sensitive en las reglas
            string tipo = nuevoVehiculo.TipoMovil.ToUpper();

            // 2. Validaciones lógicas del Cubicaje (Caso 4)
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
                // Forzamos el paso del tipo en mayúsculas a la DAL
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
                // 🚀 ESTO ES CLAVE: Pintará el error exacto de Oracle en pantalla si se cae
                ModelState.AddModelError("", "Falla Crítica Oracle: " + ex.Message);
                return View(nuevoVehiculo);
            }
        }

        // 🔧 FORMULARIO DE MANTENCIÓN (POST - CORREGIDO A 4 PARÁMETROS)
        [HttpPost]
        public IActionResult RegistrarMantencion(int idTransporte, string detalleTecnico, int costoMantencion, int diasEstimados)
        {
            // Validamos preventivamente que los días sean mayores a cero
            if (idTransporte <= 0 || diasEstimados <= 0 || string.IsNullOrEmpty(detalleTecnico))
            {
                ModelState.AddModelError("", "Por favor, complete todos los campos obligatorios del informe técnico.");
                ViewBag.Vehiculos = _vehiculoDal.ListarVehiculos();
                return View();
            }

            // Normalización de caracteres especiales para blindar la base de datos (Regla 12)
            string detalleLimpio = detalleTecnico.Normalize(System.Text.NormalizationForm.FormD)
                                                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u");

            // 🚀 CORREGIDO: Ahora le pasamos los 4 argumentos que la DAL necesita para calcular el SYSDATE + días
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
    }
}