using Microsoft.AspNetCore.Mvc;
using LaVeguita.DAL;
using LaVeguita.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace LaVeguita.Web.Controllers
{
    public class TransporteController : Controller
    {
        private readonly DespachoDAL _despachoDal = new DespachoDAL();
        private readonly TransporteDAL _transporteDal = new TransporteDAL();

        // Coordenadas geográficas base del Galpón Central de La VeguitaClick (Caso 4)
        private const double GalponLat = -33.4850;
        private const double GalponLon = -70.5530;

        // 1. VISTA DE SELECCIÓN (Paso previo obligatorio para el Rol 7)
        public IActionResult SeleccionarVehiculo()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 7)) return RedirectToAction("Login", "Acceso");

            var disponibles = _transporteDal.ListarVehiculosDisponibles();
            return View(disponibles);
        }

        // 2. PROCESO DE INICIO DE TURNO (Captura datos del móvil en Sesión)
        [HttpPost]
        public IActionResult IniciarTurno(int idTransporte, string tipoVehiculo, decimal capMax)
        {
            HttpContext.Session.SetInt32("IdVehiculoAsignado", idTransporte);
            HttpContext.Session.SetString("TipoVehiculo", tipoVehiculo.ToUpper());
            HttpContext.Session.SetString("CapacidadMaxima", capMax.ToString());

            return RedirectToAction("MisDespachos");
        }

        // 3. HOJA DE RUTA GENERAL
        public IActionResult MisDespachos()
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 7)) return RedirectToAction("Login", "Acceso");

            string tipoVehiculo = HttpContext.Session.GetString("TipoVehiculo");
            if (string.IsNullOrEmpty(tipoVehiculo) && rol == 7)
            {
                return RedirectToAction("SeleccionarVehiculo");
            }

            var despachos = _transporteDal.ListarMisDespachos(tipoVehiculo ?? "BICICLETA");

            ViewBag.Velocidad = (tipoVehiculo?.ToUpper() == "BICICLETA") ? 15 : 10;
            ViewBag.TipoVehiculo = tipoVehiculo;

            return View(despachos);
        }

        // 4. VER MAPA DE NAVEGACIÓN GPS (Nuevo método integrado para abrir Leaflet)
        public IActionResult VerMapaDespacho(int idDespacho)
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 7)) return RedirectToAction("Login", "Acceso");

            // Pasamos los datos del despacho y galpón a la vista del mapa
            ViewBag.IdDespacho = idDespacho;
            ViewBag.GalponLat = GalponLat;
            ViewBag.GalponLon = GalponLon;

            // Coordenadas simuladas del domicilio del cliente (Peñalolén / Macul)
            // En tu entrega real, puedes dejarlas fijas o leerlas de la comuna del despacho
            ViewBag.ClienteLat = -33.4910;
            ViewBag.ClienteLon = -70.5620;

            return View("HojaDeRuta"); // Renderiza la vista del mapa OpenStreetMap
        }

        // 5. ENDPOINT INTEGRADO: VALIDACIÓN DE CERCANÍA POR GEOLOCALIZACIÓN (CASO 4)
        [HttpPost]
        public JsonResult ValidarEntregaGps(int idDespacho, double transportistaLat, double transportistaLon, double clienteLat, double clienteLon)
        {
            // Regrabamos el radio de contingencia: Máximo 15 KM desde el Galpón Central
            double distanciaAlGalpon = CalcularHaversine(GalponLat, GalponLon, clienteLat, clienteLon);
            if (distanciaAlGalpon > 15.0)
            {
                return Json(new { exito = false, mensaje = "⚠️ ALERTA LOGÍSTICA: El destino de esta orden excede el radio de cobertura de 15 KM." });
            }

            // Calculamos la distancia real entre el GPS del celular y la casa del cliente
            double distanciaAlCliente = CalcularHaversine(transportistaLat, transportistaLon, clienteLat, clienteLon);

            // Umbral de tolerancia: 0.20 kilómetros = 200 metros de cercanía
            if (distanciaAlCliente <= 0.20)
            {
                // Si está en el rango, invocamos de forma segura tu método DAL original
                _despachoDal.ActualizarEstadoEntregado(idDespacho);
                return Json(new { exito = true, mensaje = "¡Ubicación confirmada! Despacho actualizado a 'ENTREGADO' en Oracle Cloud de forma exitosa." });
            }
            else
            {
                int metrosFaltantes = Convert.ToInt32(distanciaAlCliente * 1000);
                return Json(new { exito = false, mensaje = $"❌ Bloqueo Geográfico: No estás en el destino. Te encuentras a {metrosFaltantes} metros de la casa del cliente." });
            }
        }

        // Fórmula Trigonométrica de Haversine para cálculo de arcos sobre esfera terrestre
        private double CalcularHaversine(double lat1, double lon1, double lat2, double lon2)
        {
            double r = 6371; // Radio medio de la Tierra en Kilómetros
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Asin(Math.Sqrt(a));
            return r * c;
        }

        private double ToRadians(double val) => (Math.PI / 180) * val;

        // 6. CIERRE DE ENTREGA TRADICIONAL (Mantenemos tu firma original por compatibilidad)
        public IActionResult MarcarEntregado(int id)
        {
            int? rol = HttpContext.Session.GetInt32("RolUsuario");
            if (rol == null || (rol != 1 && rol != 7)) return RedirectToAction("Login", "Acceso");

            _despachoDal.ActualizarEstadoEntregado(id);

            TempData["Exito"] = "Pedido marcado como entregado correctamente.";
            return RedirectToAction("MisDespachos");
        }
    }
}