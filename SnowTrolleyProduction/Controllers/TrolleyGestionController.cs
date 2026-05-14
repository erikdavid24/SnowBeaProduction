using BAEClassLibrary;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using SnowTrolleyProduction.Controllers.service;
using SnowTrolleyProduction.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace SnowTrolleyProduction.Controllers
{
    public class TrolleyGestionController : BAEController
    {
        private readonly TrolleyGestionService _svc = new TrolleyGestionService(new BAESystemsGuaymasEntities());

        [HttpGet]
        public ActionResult Index()
        {
            var vm = new TrolleyGestionIndexViewModel
            {
                Maquinas  = _svc.GetMaquinas(),
                Ensambles = _svc.GetEnsambles(null),
                Programas = _svc.GetProgramasTable(null),
                Acomodos  = _svc.GetAcomodos()
            };
            return View(vm);
        }

        // M�quinas

        [HttpGet]
        public ActionResult MaquinasTable()
        {
            var maquinas = _svc.GetMaquinas();
            return PartialView("Tables/_MaquinasTable", maquinas);
        }

        [HttpGet]
        public ActionResult AgregarMaquinaModal()
        {
            var lineas = _svc.GetLineasSMT();
            ViewBag.Maquina = null;
            return PartialView("Modals/_MaquinaModal", lineas);
        }

        [HttpGet]
        public ActionResult EditarMaquinaModal(int maquinaId)
        {
            var lineas = _svc.GetLineasSMT();
            var maquina = _svc.GetMaquina(maquinaId);
            ViewBag.Maquina = maquina;
            return PartialView("Modals/_MaquinaModal", lineas);
        }

        [HttpGet]
        public JsonResult EquiposPorLinea(int lineaId, int? equipoActual)
        {
            var equipos = _svc.GetEquiposPorLinea(lineaId);
            return Json(new { equipos, equipoActual }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult AgregarMaquina(int equipoId, int lineaId)
        {
            try
            {
                _svc.AgregarMaquina(equipoId, lineaId);
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult EditarMaquina(int maquinaId, int equipoId)
        {
            try
            {
                _svc.EditarMaquina(maquinaId, equipoId);
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult EliminarMaquina(int maquinaId)
        {
            var error = _svc.EliminarMaquina(maquinaId);
            if (error != null) return Json(new { success = false, message = error });
            return Json(new { success = true });
        }

        // Ensambles

        [HttpGet]
        public ActionResult EnsamblesTable(string numero)
        {
            var ensambles = _svc.GetEnsambles(numero);
            return PartialView("Tables/_EnsamblesTable", ensambles);
        }

        [HttpGet]
        public ActionResult AgregarEnsambleModal()
        {
            var lineas = _svc.GetLineasSMT();
            ViewBag.Ensamble = null;
            return PartialView("Modals/_EnsambleModal", lineas);
        }

        [HttpGet]
        public ActionResult EditarEnsambleModal(int ensambleId)
        {
            var lineas = _svc.GetLineasSMT();
            var ensamble = _svc.GetEnsamble(ensambleId);
            ViewBag.Ensamble = ensamble;
            return PartialView("Modals/_EnsambleModal", lineas);
        }

        [HttpPost]
        public JsonResult AgregarEnsamble(string numero, int lineaId)
        {
            try
            {
                _svc.AgregarEnsamble(numero, lineaId);
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult EditarEnsamble(int ensambleId, string numero, int lineaId)
        {
            try
            {
                _svc.EditarEnsamble(ensambleId, numero, lineaId);
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult EliminarEnsamble(int ensambleId)
        {
            try
            {
                _svc.EliminarEnsamble(ensambleId);
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // Programas

        [HttpGet]
        public ActionResult ProgramasTable(string numero)
        {
            var programas = _svc.GetProgramasTable(numero);
            return PartialView("Tables/_ProgramasTable", programas);
        }

        [HttpGet]
        public ActionResult AgregarProgramaModal()
        {
            var ensambles = _svc.GetAllEnsambles();
            ViewBag.Programa = null;
            return PartialView("Modals/_ProgramaModal", ensambles);
        }

        [HttpGet]
        public ActionResult EditarProgramaModal(int programaId)
        {
            var ensambles = _svc.GetAllEnsambles();
            var programa = _svc.GetPrograma(programaId);
            ViewBag.Programa = programa;
            return PartialView("Modals/_ProgramaModal", ensambles);
        }

        [HttpPost]
        public JsonResult AgregarPrograma(string numero, int ensambleId)
        {
            try
            {
                _svc.AgregarPrograma(numero, ensambleId);
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult EditarPrograma(int programaId, int ensambleId, string numero)
        {
            try
            {
                _svc.EditarPrograma(programaId, ensambleId, numero);
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult EliminarPrograma(int programaId)
        {
            try
            {
                _svc.EliminarPrograma(programaId);
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }


        [HttpGet]
        public ActionResult AcomodoTable()
        {
            var acomodos = _svc.GetAcomodos();
            return PartialView("Tables/_AcomodoTable", acomodos);
        }

        [HttpGet]
        public ActionResult AgregarAcomodoModal()
        {
            var lineas = _svc.GetLineasSMT();
            ViewBag.Acomodo = null;
            return PartialView("Modals/_AcomodoModal", lineas);
        }

        [HttpGet]
        public ActionResult EditarAcomodoModal(int acomodoId)
        {
            var lineas = _svc.GetLineasSMT();
            var acomodo = _svc.GetAcomodo(acomodoId);
            ViewBag.Acomodo = acomodo;
            return PartialView("Modals/_AcomodoModal", lineas);
        }

        [HttpGet]
        public ActionResult AcomodoAutocompletado(int linea, string orden = "numero")
        {
            var lineaObj = _svc.GetLineasSMT().FirstOrDefault(l => l.Id_Linea == linea);
            int lineaId = lineaObj != null ? lineaObj.Id_Linea : linea;

            var trolleys = _svc.GetTrolleysPorLinea(lineaId);
            var programas = _svc.GetProgramasPorLinea(lineaId, orden);
            var maquinas = _svc.GetMaquinasPorLinea(lineaId);

            ViewBag.Trollies = trolleys;
            ViewBag.Programas = programas;
            ViewBag.Maquinas = maquinas;
            ViewData["orden"] = orden;

            return PartialView("Modals/_AcomodoModalAutocompletado", linea);
        }

        [HttpPost]
        public JsonResult AgregarAcomodo(AcomodoFormViewModel vm)
        {
            try
            {
                if (vm.ProgramaId <= 0)
                    return Json(new { success = false, message = "Selecciona un programa v�lido." });
                if (vm.MaquinaId <= 0 && vm.MaquinaId2 <= 0)
                    return Json(new { success = false, message = "No hay m�quinas configuradas para esta l�nea." });

                _svc.GuardarAcomodo(vm);
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult EditarAcomodo(int acomodoId, int programaId, int trolleyId, int locacion, int maquinaId)
        {
            try
            {
                _svc.EditarAcomodo(acomodoId, programaId, trolleyId, locacion, maquinaId);
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult EliminarAcomodo(int acomodoId)
        {
            try
            {
                _svc.EliminarAcomodo(acomodoId);
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult MaquinasJson([DataSourceRequest] DataSourceRequest request)
        {
            var data = _svc.GetMaquinas().Select(m => new MaquinaGridRow
            {
                Id      = m.Id,
                Maquina = m.Equipo != null ? m.Equipo.EquipoDescripcion : null,
                Linea   = m.Equipo != null ? m.Equipo.NumeroLinea : (int?)null
            }).ToList();
            return Json(data.ToDataSourceResult(request));
        }

        [HttpPost]
        public JsonResult EnsamblesJson([DataSourceRequest] DataSourceRequest request)
        {
            var data = _svc.GetEnsambles(null).Select(e => new EnsambleGridRow
            {
                Id    = e.Id,
                Numero = e.Numero,
                Linea = e.Linea != null ? (int?)e.Linea.Numero_Linea : null
            }).ToList();
            return Json(data.ToDataSourceResult(request));
        }

        [HttpPost]
        public JsonResult ProgramasJson([DataSourceRequest] DataSourceRequest request)
        {
            var data = _svc.GetProgramasTable(null).Select(p => new ProgramaGridRow
            {
                Id       = p.Id,
                Numero   = p.Numero,
                Ensamble = p.Ensamble != null ? p.Ensamble.Numero : null,
                Linea    = p.Ensamble != null && p.Ensamble.Linea != null
                             ? (int?)p.Ensamble.Linea.Numero_Linea : null
            }).ToList();
            return Json(data.ToDataSourceResult(request));
        }

        [HttpPost]
        public JsonResult AcomodoJson([DataSourceRequest] DataSourceRequest request)
        {
            var data = _svc.GetAcomodos().Select(g => new AcomodoGridRow
            {
                EnsambleId = g.Ensamble.Id,
                Ensamble   = g.Ensamble.Numero,
                Linea      = g.Ensamble.Linea != null ? (int?)g.Ensamble.Linea.Numero_Linea : null
            }).ToList();
            return Json(data.ToDataSourceResult(request));
        }
    }
}
