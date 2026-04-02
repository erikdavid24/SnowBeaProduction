using BAEClassLibrary;
using SnowTrolleyProduction.Controllers.service;
using SnowTrolleyProduction.Models;
using System.Web.Mvc;

namespace SnowTrolleyProduction.Controllers
{
    public class TrolleySetupController : BAEController
    {
        private readonly TrolleySetupService _svc =
            new TrolleySetupService(new BAESystemsGuaymasEntities());

        [HttpGet]
        public ActionResult Index(int? linea)
        {
            ViewBag.LineaSeleccionada = linea;
            return View();
        }

        [HttpPost]
        public JsonResult ObtenerDatosPorLinea(int linea)
        {
            var datos = _svc.GetDatosPorLinea(linea);
            if (datos == null)
                return Json(new { programaSeleccionado = "", message = "No hay trabajos En Proceso para esta línea." });

            return Json(datos);
        }

        [HttpPost]
        public JsonResult FinalizarProceso(int idProceso, int piezasProducidas, int piezasProgramadas, string comentarios)
        {
            try
            {
                _svc.FinalizarProceso(idProceso, piezasProducidas, comentarios);
                return Json(new { success = true });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ActualizarArranque(int idProceso)
        {
            var (success, message) = _svc.ActualizarArranque(idProceso);
            return Json(new { success, message });
        }

        [HttpGet]
        public JsonResult GetProcesoActivo(int? linea)
        {
            var (exists, lineaActiva) = _svc.GetProcesoActivo(linea);

            if (exists)
                return Json(new { exists = true, linea = lineaActiva }, JsonRequestBehavior.AllowGet);

            return Json(new { exists = false }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult VerificarPasswordAdmin(string userNumber, string password)
        {
            bool ok = _svc.VerificarAdmin(userNumber, password);
            return ok
                ? Json(new { success = true })
                : Json(new { success = false, message = "Credenciales incorrectas." });
        }
    }
}
