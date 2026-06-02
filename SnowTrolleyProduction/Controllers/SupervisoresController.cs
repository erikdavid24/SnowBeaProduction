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
    public class SupervisoresController : BAEController
    {
        private readonly TrolleySetupService _svc =
            new TrolleySetupService(new BAESystemsGuaymasEntitiesSmtPlan());

        public ActionResult Index() => View();

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

        [HttpPost]
        public JsonResult AgregarSupervisor(int employeeId, string employeeNumber, string fullName, string password)
        {
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
            try { _svc.ActualizarSupervisorPassword(id, password); return Json(new { success = true }); }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult EliminarSupervisor(int id)
        {
            try { _svc.EliminarSupervisor(id); return Json(new { success = true }); }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetEmpleadosRH()
        {
            try { return Json(_svc.GetEmpleadosRH(), JsonRequestBehavior.AllowGet); }
            catch (Exception ex) { return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet); }
        }
    }
}
