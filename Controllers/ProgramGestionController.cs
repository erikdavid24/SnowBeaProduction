using BAEClassLibrary;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using SnowTrolleyProduction.Controllers.service;
using SnowTrolleyProduction.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SnowTrolleyProduction.Controllers
{
    public class ProgramGestionController : BAEController
    {
        private readonly ProgramGestionService _svc   = new ProgramGestionService(new BAESystemsGuaymasEntities());
        private readonly TrolleySetupService   _setup = new TrolleySetupService(new BAESystemsGuaymasEntities());

        [HttpGet]
        public ActionResult Index() => View();

        [HttpPost]
        public JsonResult Read([DataSourceRequest] DataSourceRequest req, string startDate, string endDate)
        {
            try   { return Json(_svc.Read(startDate, endDate).ToDataSourceResult(req)); }
            catch (Exception ex) { var r = new DataSourceResult(); r.Errors = ex.Message; return Json(r); }
        }

        [HttpPost]
        public JsonResult ReadKanban(string startDate, string endDate)
        {
            try   { return Json(new { Data = _svc.Read(startDate, endDate) }); }
            catch (Exception ex) { return Json(new { Data = new List<ProgramGestionViewModel>(), error = ex.Message }); }
        }

        [HttpPost]
        public JsonResult ReadByStatus([DataSourceRequest] DataSourceRequest req, string startDate, string endDate, string status)
        {
            try
            {
                var filtered = _svc.Read(startDate, endDate).Where(x => x.Status == status).ToList();
                return Json(filtered.ToDataSourceResult(req));
            }
            catch (Exception ex) { var r = new DataSourceResult(); r.Errors = ex.Message; return Json(r); }
        }

        [HttpPost]
        public ActionResult Create([DataSourceRequest] DataSourceRequest req, ProgramGestionViewModel item)
        {
            if (item != null && ModelState.IsValid)
            {
                try
                {
                    item.Id_Proceso    = 1;
                    item.FechaCreacion = DateTime.Now;
                    item.Status        = item.Status ?? "Pendiente";
                    _svc.Create(item);
                }
                catch (Exception ex) { ModelState.AddModelError("", ex.Message); }
            }
            return Json(new[] { item }.ToDataSourceResult(req, ModelState));
        }

        [HttpPost]
        public ActionResult CreateManual(ProgramGestionViewModel item)
        {
            try
            {
                if (item == null)                            return Json(new { success = false, message = "Datos inválidos" });
                if (string.IsNullOrEmpty(item.Id_Programa)) return Json(new { success = false, message = "El programa es requerido" });
                if (string.IsNullOrEmpty(item.WorkOrder))   return Json(new { success = false, message = "La orden de trabajo es requerida" });
                if (item.PiezasProgramadas <= 0)             return Json(new { success = false, message = "Las piezas deben ser mayor a 0" });

                item.Id_Proceso    = 1;
                item.Status        = "Creado";
                item.FechaCreacion = DateTime.Now;
                item.Trolleys      = "";

                // Inserta automáticamente todos los lados hermanos del ensamble
                var lados = _svc.GetLadosHermanos(item.Id_Programa);
                if (lados != null && lados.Count > 1)
                {
                    foreach (var lado in lados)
                    {
                        var copia = new ProgramGestionViewModel
                        {
                            Id_Proceso = item.Id_Proceso,
                            Id_Programa = lado,
                            WorkOrder = item.WorkOrder,
                            PiezasProgramadas = item.PiezasProgramadas,
                            Trolleys = "",
                            Status = item.Status,
                            FechaCreacion = item.FechaCreacion,
                            Id_Linea = item.Id_Linea,
                            Comentarios = item.Comentarios
                        };
                        _svc.Create(copia);
                    }
                }
                else
                {
                    _svc.Create(item);
                }
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult EditarDirecto(ProgramGestionViewModel item)
        {
            try
            {
                if (item == null || item.Id <= 0) return Json(new { success = false, message = "Datos inválidos" });
                _svc.Update(item);
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public ActionResult Update([DataSourceRequest] DataSourceRequest req, ProgramGestionViewModel item)
        {
            try   { if (item != null && ModelState.IsValid) _svc.Update(item); }
            catch (Exception ex) { ModelState.AddModelError("", ex.Message); }
            return Json(new[] { item }.ToDataSourceResult(req, ModelState));
        }

        [HttpPost]
        public ActionResult UpdateStatus(int id, string status)
        {
            try
            {
                _svc.Update(new ProgramGestionViewModel { Id = id, Status = status });
                return Json(new { success = true, message = string.Format("Status actualizado a '{0}'", status) });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public ActionResult Delete([DataSourceRequest] DataSourceRequest req, ProgramGestionViewModel item)
        {
            try   { _svc.Delete(item); }
            catch (Exception ex) { ModelState.AddModelError("", ex.Message); }
            return Json(new[] { item }.ToDataSourceResult(req, ModelState));
        }

        [HttpPost]
        public JsonResult EliminarDirecto(int? id)
        {
            try
            {
                if (id == null || id <= 0) return Json(new { success = false, message = "ID inválido: " + id });
                _svc.Delete(new ProgramGestionViewModel { Id = id.Value });
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetLineas()
        {
            try   { return Json(_svc.GetLineas(), JsonRequestBehavior.AllowGet); }
            catch (Exception ex) { return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        [HttpGet]
        public JsonResult GetEnsambles(int? lineaId)
        {
            if (lineaId == null) return Json(new List<SelectItemDto>(), JsonRequestBehavior.AllowGet);
            try   { return Json(_svc.GetEnsambles(lineaId.Value), JsonRequestBehavior.AllowGet); }
            catch (Exception ex) { return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        [HttpGet]
        public JsonResult GetProgramas(string ensamble)
        {
            if (string.IsNullOrEmpty(ensamble)) return Json(new List<SelectItemDto>(), JsonRequestBehavior.AllowGet);
            try   { return Json(_svc.GetProgramas(ensamble), JsonRequestBehavior.AllowGet); }
            catch (Exception ex) { return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        [HttpGet]
        public JsonResult GetLadosPorEnsamble(string ensamble)
        {
            try   { return Json(new { lados = _svc.GetLadosPorEnsamble(ensamble) }, JsonRequestBehavior.AllowGet); }
            catch (Exception ex) { return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        [HttpGet]
        public JsonResult GetSemanaFiscal(string fecha)
        {
            try
            {
                DateTime dt  = string.IsNullOrEmpty(fecha)
                    ? DateTime.Today
                    : DateTime.ParseExact(fecha, "yyyy/MM/dd", null);
                var raw = _svc.GetSemanaFiscal(dt).Split('|');
                return Json(new { semana = raw[0], anio = raw.Length > 1 ? raw[1] : dt.Year.ToString().Substring(2) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex) { return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        [HttpPost]
        public ActionResult CargaMasiva(HttpPostedFileBase archivoExcel, bool? preview)
        {
            // Preview mode
            if (preview == true)
            {
                if (archivoExcel == null || archivoExcel.ContentLength == 0)
                    return Json(new { success = false, message = "Selecciona un archivo Excel válido." });
                
                try
                {
                    var items = _svc.ParseExcelForPreview(archivoExcel.InputStream);
                    return Json(new { success = true, data = items, totalOriginal = items.Count });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
            
            // Direct save mode (legacy)
            if (archivoExcel == null || archivoExcel.ContentLength == 0)
            {
                TempData["Error"] = "Por favor selecciona un archivo de Excel válido.";
                return RedirectToAction("Index");
            }
            
            try
            {
                var items = _svc.ParseExcelForPreview(archivoExcel.InputStream);
                int count = _svc.GuardarDesdeLista(items);
                TempData[count == 0 ? "Error" : "Success"] = count == 0
                    ? "No se encontraron datos válidos en el Excel."
                    : string.Format("¡Éxito! Se cargaron {0} programas.", count);
            }
            catch (Exception ex) 
            { 
                TempData["Error"] = "Error al procesar el Excel: " + ex.Message; 
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult GuardarPreview()
        {
            try
            {
                Request.InputStream.Seek(0, System.IO.SeekOrigin.Begin);
                string body = new System.IO.StreamReader(Request.InputStream).ReadToEnd();
                if (string.IsNullOrWhiteSpace(body))
                    return Json(new { success = false, message = "No hay datos para guardar." });

                var items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ExcelPreviewItemDto>>(body);
                if (items == null || !items.Any())
                    return Json(new { success = false, message = "No hay datos para guardar." });

                int count = _svc.GuardarDesdeLista(items);
                return Json(new { success = true, count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetTrolleysPorLinea(int linea, int programaId)
        {
            try
            {
                var result = _setup.GetTrolleysSetup(linea, programaId);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex) { return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet); }
        }

        [HttpPost]
        public JsonResult GuardarSetupTrolleys(int programaId, int linea, int? maquinaId,
            int? maquinaId2,
            int? Z10, int? Z20, int? Z30, int? Z40, int? Z50,
            int? Z60, int? Z70, int? Z80, int? Z90,
            int? Z10_2, int? Z20_2, int? Z30_2, int? Z40_2, int? Z50_2,
            int? Z60_2, int? Z70_2, int? Z80_2, int? Z90_2)
        {
            try
            {
                var zonas1 = new Dictionary<int, int>();
                if (Z10.HasValue && Z10 > 0) zonas1[10] = Z10.Value;
                if (Z20.HasValue && Z20 > 0) zonas1[20] = Z20.Value;
                if (Z30.HasValue && Z30 > 0) zonas1[30] = Z30.Value;
                if (Z40.HasValue && Z40 > 0) zonas1[40] = Z40.Value;
                if (Z50.HasValue && Z50 > 0) zonas1[50] = Z50.Value;
                if (Z60.HasValue && Z60 > 0) zonas1[60] = Z60.Value;
                if (Z70.HasValue && Z70 > 0) zonas1[70] = Z70.Value;
                if (Z80.HasValue && Z80 > 0) zonas1[80] = Z80.Value;
                if (Z90.HasValue && Z90 > 0) zonas1[90] = Z90.Value;

                var zonas2 = new Dictionary<int, int>();
                if (Z10_2.HasValue && Z10_2 > 0) zonas2[10] = Z10_2.Value;
                if (Z20_2.HasValue && Z20_2 > 0) zonas2[20] = Z20_2.Value;
                if (Z30_2.HasValue && Z30_2 > 0) zonas2[30] = Z30_2.Value;
                if (Z40_2.HasValue && Z40_2 > 0) zonas2[40] = Z40_2.Value;
                if (Z50_2.HasValue && Z50_2 > 0) zonas2[50] = Z50_2.Value;
                if (Z60_2.HasValue && Z60_2 > 0) zonas2[60] = Z60_2.Value;
                if (Z70_2.HasValue && Z70_2 > 0) zonas2[70] = Z70_2.Value;
                if (Z80_2.HasValue && Z80_2 > 0) zonas2[80] = Z80_2.Value;
                if (Z90_2.HasValue && Z90_2 > 0) zonas2[90] = Z90_2.Value;

                _setup.GuardarSetupTrolleys(programaId, zonas1, maquinaId);
                if (zonas2.Count > 0 && maquinaId2.HasValue && maquinaId2.Value > 0)
                    _setup.GuardarSetupTrolleysCabezal2(programaId, zonas2, maquinaId2.Value);

                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
    }
}