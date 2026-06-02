using BAEClassLibrary;
using SnowTrolleyProduction.Controllers.service;
using SnowTrolleyProduction.Models;
using SnowTrolleyProduction.Models.Dtos;
using System.Collections.Generic;
using System.Web.Mvc;

namespace SnowTrolleyProduction.Controllers
{
    public class TrolleyProcessController : BAEController
    {
        private readonly TrolleyProcessService _svc =
            new TrolleyProcessService(new BAESystemsGuaymasEntitiesSmtPlan());

        private readonly TrolleySetupService _setupSvc =
            new TrolleySetupService(new BAESystemsGuaymasEntitiesSmtPlan());

        public ActionResult Index()
        {
            ViewBag.Lineas = _svc.GetLineas();
            return View();
        }

        [HttpPost]
        public JsonResult ObtenerProcesos(int linea)
        {
            var data = _svc.GetProcesosDisponibles(linea);
            return Json(data);
        }

        [HttpPost]
        public JsonResult ObtenerTrabajosEnArranque(int linea)
        {
            var data = _svc.GetTrabajosEnProceso(linea);
            return Json(data);
        }

        [HttpPost]
        public JsonResult ObtenerTrolleys(List<int> idProcesos)
        {
            if (idProcesos == null || idProcesos.Count == 0)
                return Json(new List<ProcesoItem>());

            var data = _svc.GetProcesosConTrolleys(idProcesos);
            return Json(data);
        }

        [HttpPost]
        public JsonResult GuardarOrdenProcesos(List<int> idProcesos)
        {
            if (idProcesos != null && idProcesos.Count > 0)
                _svc.GuardarOrden(idProcesos);
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult GuardarSetup(int idProceso, int linea)
        {
            var (success, message) = _svc.IniciarSetup(idProceso, linea);
            if (!success) return Json(new { success = false, message });

            return Json(new { success = true, redirectUrl = Url.Action("Index", "TrolleySetup", new { linea }) });
        }
    }
}
