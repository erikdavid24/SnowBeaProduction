using BAEClassLibrary;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using SnowTrolleyProduction.Controllers.service;
using SnowTrolleyProduction.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace SnowTrolleyProduction.Controllers
{
    public class TrolleySetupController : BAEController
    {
        private readonly TrolleySetupService _svc =
            new TrolleySetupService(new BAESystemsGuaymasEntitiesSmtPlan());

        [HttpGet]
        public ActionResult Index(int? linea)
        {
            ViewBag.LineaSeleccionada = linea;
            return View();
        }

        [HttpGet]
        public ActionResult Supervisores()
        {
            return View();
        }

        [HttpPost]
        public JsonResult ObtenerDatosPorLinea(int linea)
        {
            var datos = _svc.GetDatosPorLinea(linea);
            if (datos == null)
                return Json(new { programaSeleccionado = "", message = "No hay trabajos En Proceso para esta l�nea." });

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
        public JsonResult VerificarPasswordAdmin(string employeeNumber, string password)
        {
            bool ok = _svc.VerificarSupervisor(employeeNumber, password);
            return ok
                ? Json(new { success = true })
                : Json(new { success = false, message = "Credenciales incorrectas o no tienes permiso de supervisor." });
        }

        [HttpGet]
        public JsonResult GetSupervisores([DataSourceRequest] DataSourceRequest request)
        {
            var data = _svc.GetSupervisores()
                .Select(e => new Models.Dtos.SupervisorDto {
                    Id             = (int)e.Id,
                    EmployeeNumber = (string)e.EmployeeNumber,
                    FullName       = (string)e.FullName,
                    FechaAlta      = (DateTime)e.FechaAlta
                }).ToList();
            return Json(data.ToDataSourceResult(request), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetEmpleadosRH()
        {
            var data = _svc.GetEmpleadosRH()
                .Select(e => new { employeeId = (int)e.EmployeeID, employeeNumber = (string)e.EmployeeNumber, fullName = (string)e.FullName })
                .ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult AgregarSupervisor(int employeeId, string employeeNumber, string fullName, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return Json(new { success = false, message = "La contraseña es requerida." });
            try { _svc.AgregarSupervisor(employeeId, employeeNumber, fullName, password); return Json(new { success = true }); }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetSupervisorPassword(int id)
        {
            var pass = _svc.GetSupervisorPassword(id);
            return Json(new { success = true, password = pass }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ActualizarSupervisorPassword(int id, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return Json(new { success = false, message = "La contraseña es requerida." });
            try { _svc.ActualizarSupervisorPassword(id, password); return Json(new { success = true }); }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult EliminarSupervisor(int id)
        {
            try { _svc.EliminarSupervisor(id); return Json(new { success = true }); }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
    }
}
