using BAEClassLibrary;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using SHE.SafetyTours.Models;
using SHE.SafetyTours.Models.Service;
using SnowTrolleyProduction.Models;
using System.Web.Mvc;

namespace SHE.SafetyTours.Controllers
{
    public class TrolleySetupController : BAEController
    {
        TrolleySetupServices svc = new TrolleySetupServices(new BAESystemsGuaymasEntities1());

        public ActionResult Index() => View();

        public JsonResult Read([DataSourceRequest] DataSourceRequest req, string start, string end)
        {
            var data = svc.Read(start, end);
            return Json(data.ToDataSourceResult(req), JsonRequestBehavior.AllowGet);
        }

        public ActionResult Create([DataSourceRequest] DataSourceRequest req, TrolleySetupViewModel item)
        {
            if (item != null && ModelState.IsValid)
                svc.Create(item);

            return Json(new[] { item }.ToDataSourceResult(req, ModelState));
        }

        public ActionResult Update([DataSourceRequest] DataSourceRequest req, TrolleySetupViewModel item)
        {
            svc.Update(item);
            return Json(new[] { item }.ToDataSourceResult(req, ModelState));
        }

        public ActionResult Delete([DataSourceRequest] DataSourceRequest req, TrolleySetupViewModel item)
        {
            svc.Delete(item);
            return Json(new[] { item }.ToDataSourceResult(req, ModelState));
        }

        public JsonResult GuardarRapido(TrolleySetupViewModel item)
        {
            var res = new AnswerResult();

            if (item == null) return Json(res);

            if (string.IsNullOrEmpty(item.WorkOrder))
            {
                res.AddWarning("La Orden de Trabajo no puede estar vacía.");
                return Json(res);
            }

            svc.Create(item);
            res.AddSuccess("Trolley configurado correctamente.");
            return Json(res);
        }
    }
}