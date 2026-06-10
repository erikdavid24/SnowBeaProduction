using ClosedXML.Excel;
using SnowTrolleyProduction.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Dapper;

namespace SnowTrolleyProduction.Controllers.service
{
    public class ProgramGestionService
    {
        BAESystemsGuaymasEntitiesSmtPlan BD = new BAESystemsGuaymasEntitiesSmtPlan();

        public ProgramGestionService(BAESystemsGuaymasEntitiesSmtPlan BDContext)
        {
            BD = BDContext;
        }

        private string GetConnectionString() => BD.Database.Connection.ConnectionString;

        public List<ProgramGestionViewModel> Read(string startDate, string endDate, int? semana = null, int? anio = null)
        {
            if (semana.HasValue && anio.HasValue && semana.Value > 0 && anio.Value > 0)
            {
                var rango = GetRangoSemanaFiscal(semana.Value, anio.Value);
                startDate = rango.Item1.ToString("yyyy/MM/dd");
                endDate   = rango.Item2.ToString("yyyy/MM/dd");
            }

            string sql = @"
                SELECT
                    ts.Id, ts.Id_Proceso, ts.Id_Programa, ts.WorkOrder, ts.PiezasProgramadas,
                    ts.Trolleys, ts.Status, ts.FechaCreacion, ts.FechaFinalizacion, ts.Id_Linea, ts.Comentarios,
                    ISNULL(e.EnsambleBase, ts.Id_Programa) AS Ensamble,
                    STUFF((
                        SELECT DISTINCT ', ' + ts2.Id_Programa
                        FROM [Process].[TrolleySetup] ts2
                        INNER JOIN [Process].[Programas] p2 ON p2.Numero = ts2.Id_Programa
                        WHERE p2.Ensamble = e.Id
                          AND ts2.Status    = ts.Status
                          AND ts2.WorkOrder = ts.WorkOrder
                          AND ts2.Linea     = ts.Linea
                        FOR XML PATH(''), TYPE
                    ).value('.','NVARCHAR(MAX)'), 1, 2, '') AS Lados,
                    ISNULL(p.CantidadTotalMateriales, 0) AS CantidadMateriales
                FROM [Process].[TrolleySetup] ts
                LEFT JOIN [Process].[Programas] p  ON p.Numero    = ts.Id_Programa
                LEFT JOIN [Process].[Ensambles] e  ON e.Id        = p.Ensamble";

            using (var conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                List<ProgramGestionViewModel> rows;
                if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
                {
                    sql += " WHERE ts.Status != 'PreCarga'";
                    rows = conn.Query<ProgramGestionViewModel>(sql).ToList();
                }
                else
                {
                    sql += " WHERE ts.Status != 'PreCarga' AND CAST(ts.FechaCreacion AS DATE) >= @start AND CAST(ts.FechaCreacion AS DATE) <= @end";
                    DateTime fechaInicio = DateTime.ParseExact(startDate, "yyyy/MM/dd", null);
                    DateTime fechaFin    = DateTime.ParseExact(endDate,   "yyyy/MM/dd", null);
                    rows = conn.Query<ProgramGestionViewModel>(sql, new { start = fechaInicio.Date, end = fechaFin.Date }).ToList();
                }
                var grouped = new List<ProgramGestionViewModel>();
                var seen = new Dictionary<string, ProgramGestionViewModel>();
                foreach (var r in rows)
                {
                    string key = (r.Ensamble ?? r.Id_Programa ?? "") + "|" + (r.WorkOrder ?? "") + "|" + (r.Status ?? "");
                    if (!seen.ContainsKey(key))
                    {
                        seen[key] = r;
                        grouped.Add(r);
                    }
                }
                return grouped;
            }
        }

        public void Create(ProgramGestionViewModel model)
        {
            string sql = @"INSERT INTO [Process].[TrolleySetup]
                   (Id_Proceso, Id_Programa, WorkOrder, PiezasProgramadas, Trolleys, Status, FechaCreacion, Id_Linea, Comentarios, Linea)
                   VALUES (@IdProceso, @IdPrograma, @WorkOrder, @PiezasProgramadas, @Trolleys, @Status, @FechaCreacion, @IdLinea, @Comentarios, @Linea)";

            using (var conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                conn.Execute(sql, new
                {
                    IdProceso = model.Id_Proceso,
                    IdPrograma = (object)model.Id_Programa ?? DBNull.Value,
                    WorkOrder = (object)model.WorkOrder ?? DBNull.Value,
                    PiezasProgramadas = model.PiezasProgramadas,
                    Trolleys = (object)model.Trolleys ?? DBNull.Value,
                    Status = model.Status ?? "Pendiente",
                    FechaCreacion = model.FechaCreacion != DateTime.MinValue ? model.FechaCreacion : DateTime.Now,
                    IdLinea = (object)model.Id_Linea ?? DBNull.Value,
                    Comentarios = (object)model.Comentarios ?? DBNull.Value,
                    Linea = model.Id_Linea ?? 0
                });
            }
        }

        public List<ExcelPreviewItemDto> ParseExcelForPreview(Stream streamArchivo)
        {
            var lista = new List<ExcelPreviewItemDto>();
            string anioActual = DateTime.Now.Year.ToString().Substring(2);

            using (var workBook = new XLWorkbook(streamArchivo))
            using (var conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                var sheet = workBook.Worksheets.FirstOrDefault(w => w.Name.IndexOf("SMT Run Plan", StringComparison.OrdinalIgnoreCase) >= 0);
                if (sheet == null) sheet = workBook.Worksheet(3);

                string nombreHoja = sheet.Name ?? "";
                string digitosHoja = new string(nombreHoja.Where(char.IsDigit).ToArray());
                string semanaPlan = digitosHoja.Length >= 2 ? digitosHoja.Substring(0, 2)
                                  : digitosHoja.Length == 1 ? digitosHoja.PadLeft(2, '0')
                                  : "01";

                var rows = sheet.RangeUsed().RowsUsed().Skip(1).ToList();

                foreach (var row in rows)
                {
                    var filaReal = row.WorksheetRow();

                    DateTime? fechaPlanOpt = ObtenerFechaDeExcel(filaReal.Cell("A"));
                    if (!fechaPlanOpt.HasValue) continue;
                    DateTime fechaPlan = fechaPlanOpt.Value;

                    string programaBaseExcel = filaReal.Cell("B").GetString()?.Trim();
                    if (string.IsNullOrWhiteSpace(programaBaseExcel)) continue;

                    string lineaExcelStr = filaReal.Cell("H").GetString()?.Trim() ?? "";
                    int? lineaFinal = null;
                    if (int.TryParse(lineaExcelStr, out int parsedLine)) lineaFinal = parsedLine;

                    int piezas = ObtenerEntero(filaReal.Cell("Y"));

                    var infoEnsamble = conn.QueryFirstOrDefault<EnsambleDto>(
                        @"SELECT TOP 1 e.Id, e.Linea1 FROM [Process].[Ensambles] e
                          INNER JOIN [Process].[Lineas] l ON l.Id_Linea = e.Linea1
                          WHERE e.EnsambleBase = @eb AND l.Numero_Linea = @ln",
                        new { eb = programaBaseExcel, ln = lineaFinal });

                    bool useFallback = (infoEnsamble == null);
                    if (useFallback)
                        infoEnsamble = conn.QueryFirstOrDefault<EnsambleDto>(
                            "SELECT TOP 1 Id, Linea1 FROM [Process].[Ensambles] WHERE EnsambleBase = @eb",
                            new { eb = programaBaseExcel });

                    string advertenciaLinea = null;
                    if (!lineaFinal.HasValue && infoEnsamble != null && infoEnsamble.Linea1.HasValue)
                    {
                        var numLinea = conn.QueryFirstOrDefault<int?>(
                            "SELECT TOP 1 Numero_Linea FROM [Process].[Lineas] WHERE Id_Linea = @id",
                            new { id = infoEnsamble.Linea1.Value });
                        if (numLinea.HasValue && numLinea.Value >= 7 && numLinea.Value <= 9) lineaFinal = numLinea.Value;
                    }
                    else if (lineaFinal.HasValue && useFallback && infoEnsamble != null && infoEnsamble.Linea1.HasValue)
                    {
                        var numLineaDB = conn.QueryFirstOrDefault<int?>(
                            "SELECT TOP 1 Numero_Linea FROM [Process].[Lineas] WHERE Id_Linea = @id",
                            new { id = infoEnsamble.Linea1.Value });
                        if (numLineaDB.HasValue && numLineaDB.Value >= 7 && numLineaDB.Value <= 9 && numLineaDB.Value != lineaFinal.Value)
                            advertenciaLinea = $"BD línea {numLineaDB.Value}, Excel línea {lineaFinal.Value}";
                    }

                    List<string> programasReales = new List<string>();
                    if (infoEnsamble != null)
                    {
                        programasReales = conn.Query<string>(
                            "SELECT Numero FROM [Process].[Programas] WHERE Ensamble = @eid AND Numero IS NOT NULL AND Numero <> '' ORDER BY Numero ASC",
                            new { eid = infoEnsamble.Id }).ToList();
                    }

                    if (!programasReales.Any()) continue;

                    foreach (var programaReal in programasReales)
                    {
                        string woLinea = lineaFinal.HasValue ? lineaFinal.Value.ToString() : "0";
                        string workOrderGenerado = $"L{woLinea}{semanaPlan}{anioActual}000";

                        string woFinal    = workOrderGenerado;
                        int    pzasFinales = piezas;
                        bool   esDup       = false;
                        string razon       = advertenciaLinea ?? "";
                        int    existingId  = 0;

                        bool originalActivo = conn.QueryFirstOrDefault<int>(@"
                            SELECT COUNT(1) FROM [Process].[TrolleySetup]
                            WHERE Id_Programa = @prog AND WorkOrder = @wo
                              AND Status NOT IN ('Completado', 'Finalizado Parcial')",
                            new { prog = programaReal, wo = workOrderGenerado }) > 0;

                        if (originalActivo)
                        {
                            esDup = true;
                            razon = $"WorkOrder '{workOrderGenerado}' ya existe";
                        }
                        else
                        {

                            var pendienteInfo = conn.QueryFirstOrDefault<dynamic>(@"
                                SELECT TOP 1 Id, WorkOrder, PiezasProgramadas FROM [Process].[TrolleySetup]
                                WHERE Id_Programa = @prog
                                  AND WorkOrder LIKE @pat
                                  AND Status NOT IN ('Completado')
                                ORDER BY Id DESC",
                                new { prog = programaReal, pat = workOrderGenerado + "-%" });

                            if (pendienteInfo != null)
                            {
                                int pzasExistentes = (int)pendienteInfo.PiezasProgramadas;
                                int pzasFaltantes  = piezas - pzasExistentes;

                                if (pzasFaltantes <= 0)
                                {

                                    existingId  = (int)pendienteInfo.Id;
                                    woFinal     = (string)pendienteInfo.WorkOrder;
                                    pzasFinales = pzasExistentes;
                                    razon       = $"Carta pendiente cubre la demanda ({pzasExistentes} pzas existentes vs {piezas} del Excel)";
                                }
                                else
                                {

                                    string woExistente = (string)pendienteInfo.WorkOrder;
                                    var wm = System.Text.RegularExpressions.Regex.Match(woExistente ?? "", @"-(\d+)$");
                                    woFinal     = wm.Success
                                        ? woExistente.Substring(0, wm.Index) + "-" + (int.Parse(wm.Groups[1].Value) + 1)
                                        : woExistente + "-1";
                                    pzasFinales = pzasFaltantes;
                                    razon       = $"Saldo adicional de {woFinal}: {pzasExistentes} existentes + {pzasFaltantes} nuevas = {piezas} total";
                                }
                            }
                            else
                            {
                                bool originalTerminado = conn.QueryFirstOrDefault<int>(@"
                                    SELECT COUNT(1) FROM [Process].[TrolleySetup]
                                    WHERE Id_Programa = @prog AND WorkOrder = @wo
                                      AND Status IN ('Completado', 'Finalizado Parcial')",
                                    new { prog = programaReal, wo = workOrderGenerado }) > 0;

                                if (originalTerminado)
                                {
                                    woFinal = workOrderGenerado + "-1";
                                    razon   = $"Saldo de '{workOrderGenerado}'";
                                }
                            }
                        }

                        lista.Add(new ExcelPreviewItemDto
                        {
                            Id_Programa       = programaReal,
                            WorkOrder         = woFinal,
                            PiezasProgramadas = pzasFinales,
                            Status            = "Creado",
                            FechaCreacion     = fechaPlan,
                            Id_Linea          = lineaFinal,
                            Comentarios       = woFinal == workOrderGenerado
                                                    ? "Carga Excel"
                                                    : $"Carga Excel | Saldo de {workOrderGenerado}",
                            Ensamble          = programaBaseExcel,
                            EsDuplicado       = esDup,
                            RazonRechazo      = razon,
                            ExistingId        = existingId
                        });
                    }
                }
            }
            return lista;
        }

        public int GuardarDesdeLista(List<ExcelPreviewItemDto> items)
        {
            int conteo = 0;
            string sqlInsert = @"INSERT INTO [Process].[TrolleySetup]
                   (Id_Proceso, Id_Programa, WorkOrder, PiezasProgramadas, Trolleys, Status, FechaCreacion, Id_Linea, Comentarios, Linea)
                   VALUES (@IdProceso, @IdPrograma, @WorkOrder, @PiezasProgramadas, @Trolleys, @Status, @FechaCreacion, @IdLinea, @Comentarios, @Linea)";

            using (var conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in items)
                        {
                            if (item.EsDuplicado || item.ExistingId > 0) continue;

                            conn.Execute(sqlInsert, new
                            {
                                IdProceso         = 1,
                                IdPrograma        = (object)item.Id_Programa,
                                WorkOrder         = (object)item.WorkOrder,
                                PiezasProgramadas = item.PiezasProgramadas,
                                Trolleys          = "",
                                Status            = item.Status ?? "Creado",
                                FechaCreacion     = item.FechaCreacion,
                                IdLinea           = (object)item.Id_Linea ?? DBNull.Value,
                                Comentarios       = (object)item.Comentarios ?? DBNull.Value,
                                Linea             = item.Id_Linea ?? 0
                            }, transaction);
                            conteo++;
                        }
                        transaction.Commit();
                    }
                    catch { transaction.Rollback(); throw; }
                }
            }
            return conteo;
        }

        public int CargarDesdeExcel(Stream streamArchivo, string nombreArchivo)
        {
            int conteoInsertados = 0;
            string anioActual = DateTime.Now.Year.ToString().Substring(2);

            string sqlInsert = @"INSERT INTO [Process].[TrolleySetup]
                   (Id_Proceso, Id_Programa, WorkOrder, PiezasProgramadas, Trolleys, Status, FechaCreacion, Id_Linea, Comentarios, Linea)
                   VALUES (@IdProceso, @IdPrograma, @WorkOrder, @PiezasProgramadas, @Trolleys, @Status, @FechaCreacion, @IdLinea, @Comentarios, @Linea)";

            using (var workBook = new XLWorkbook(streamArchivo))
            using (var conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();

                var sheet = workBook.Worksheets.FirstOrDefault(w => w.Name.IndexOf("SMT Run Plan", StringComparison.OrdinalIgnoreCase) >= 0);
                if (sheet == null) sheet = workBook.Worksheet(3);

                string nombreHoja = sheet.Name ?? "";
                string digitosHoja = new string(nombreHoja.Where(char.IsDigit).ToArray());
                string semanaPlan = digitosHoja.Length >= 2 ? digitosHoja.Substring(0, 2)
                                  : digitosHoja.Length == 1 ? digitosHoja.PadLeft(2, '0')
                                  : "01";

                var rows = sheet.RangeUsed().RowsUsed().Skip(1).ToList();
                var registros = new List<object>();

                foreach (var row in rows)
                {
                    var filaReal = row.WorksheetRow();

                    DateTime? fechaPlanOpt = ObtenerFechaDeExcel(filaReal.Cell("A"));
                    if (!fechaPlanOpt.HasValue) continue;
                    DateTime fechaPlan = fechaPlanOpt.Value;

                    string programaBaseExcel = filaReal.Cell("B").GetString()?.Trim();
                    if (string.IsNullOrWhiteSpace(programaBaseExcel)) continue;

                    string lineaExcelStr = filaReal.Cell("H").GetString()?.Trim() ?? "";
                    int? lineaFinal = null;
                    if (int.TryParse(lineaExcelStr, out int parsedLine)) lineaFinal = parsedLine;

                    int piezas = ObtenerEntero(filaReal.Cell("Y"));

                    var infoEnsamble = conn.QueryFirstOrDefault<EnsambleDto>(
                        @"SELECT TOP 1 e.Id, e.Linea1 FROM [Process].[Ensambles] e
                          INNER JOIN [Process].[Lineas] l ON l.Id_Linea = e.Linea1
                          WHERE e.EnsambleBase = @eb AND l.Numero_Linea = @ln",
                        new { eb = programaBaseExcel, ln = lineaFinal });

                    bool useFallback = (infoEnsamble == null);
                    if (useFallback)
                        infoEnsamble = conn.QueryFirstOrDefault<EnsambleDto>(
                            "SELECT TOP 1 Id, Linea1 FROM [Process].[Ensambles] WHERE EnsambleBase = @eb",
                            new { eb = programaBaseExcel });

                    string comentariosExtra = null;
                    if (!lineaFinal.HasValue && infoEnsamble != null && infoEnsamble.Linea1.HasValue)
                    {
                        var numLinea = conn.QueryFirstOrDefault<int?>(
                            "SELECT TOP 1 Numero_Linea FROM [Process].[Lineas] WHERE Id_Linea = @id",
                            new { id = infoEnsamble.Linea1.Value });
                        if (numLinea.HasValue && numLinea.Value >= 7 && numLinea.Value <= 9) lineaFinal = numLinea.Value;
                    }
                    else if (lineaFinal.HasValue && useFallback && infoEnsamble != null && infoEnsamble.Linea1.HasValue)
                    {
                        var numLineaDB = conn.QueryFirstOrDefault<int?>(
                            "SELECT TOP 1 Numero_Linea FROM [Process].[Lineas] WHERE Id_Linea = @id",
                            new { id = infoEnsamble.Linea1.Value });
                        if (numLineaDB.HasValue && numLineaDB.Value >= 7 && numLineaDB.Value <= 9 && numLineaDB.Value != lineaFinal.Value)
                            comentariosExtra = $"BD línea {numLineaDB.Value}, Excel línea {lineaFinal.Value}";
                    }

                    List<string> programasReales = new List<string>();
                    if (infoEnsamble != null)
                    {
                        programasReales = conn.Query<string>(
                            "SELECT Numero FROM [Process].[Programas] WHERE Ensamble = @eid AND Numero IS NOT NULL AND Numero <> '' ORDER BY Numero ASC",
                            new { eid = infoEnsamble.Id }).ToList();
                    }

                    if (!programasReales.Any()) continue;

                    foreach (var programaReal in programasReales)
                    {
                        string woLinea = lineaFinal.HasValue ? lineaFinal.Value.ToString() : "0";
                        string workOrderGenerado = $"L{woLinea}{semanaPlan}{anioActual}000";

                        string woFinal = workOrderGenerado;

                        bool originalActivo = conn.QueryFirstOrDefault<int>(@"
                            SELECT COUNT(1) FROM [Process].[TrolleySetup]
                            WHERE Id_Programa = @prog AND WorkOrder = @wo
                              AND Status NOT IN ('Completado', 'Finalizado Parcial')",
                            new { prog = programaReal, wo = workOrderGenerado }) > 0;

                        if (originalActivo) continue;

                        bool pendienteActiva = conn.QueryFirstOrDefault<int>(@"
                            SELECT COUNT(1) FROM [Process].[TrolleySetup]
                            WHERE Id_Programa = @prog AND WorkOrder LIKE @pat
                              AND Status NOT IN ('Completado')",
                            new { prog = programaReal, pat = workOrderGenerado + "-%" }) > 0;

                        if (pendienteActiva) continue;

                        bool originalTerminado = conn.QueryFirstOrDefault<int>(@"
                            SELECT COUNT(1) FROM [Process].[TrolleySetup]
                            WHERE Id_Programa = @prog AND WorkOrder = @wo
                              AND Status IN ('Completado', 'Finalizado Parcial')",
                            new { prog = programaReal, wo = workOrderGenerado }) > 0;

                        if (originalTerminado) woFinal = workOrderGenerado + "-1";

                        registros.Add(new
                        {
                            IdProceso         = 1,
                            IdPrograma        = (object)programaReal,
                            WorkOrder         = (object)woFinal,
                            PiezasProgramadas = piezas,
                            Trolleys          = "",
                            Status            = "Creado",
                            FechaCreacion     = fechaPlan,
                            IdLinea           = (object)lineaFinal ?? DBNull.Value,
                            Comentarios       = comentariosExtra != null ? $"Carga Excel | {comentariosExtra}" : "Carga Excel",
                            Linea             = lineaFinal ?? 0
                        });
                    }
                }

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var reg in registros) { conn.Execute(sqlInsert, reg, transaction); conteoInsertados++; }
                        transaction.Commit();
                    }
                    catch { transaction.Rollback(); throw; }
                }
            }

            return conteoInsertados;
        }

        public void Update(ProgramGestionViewModel model)
        {
            string sql = @"
                UPDATE [Process].[TrolleySetup]
                SET Id_Proceso = @p0,
                    Id_Programa = ISNULL(@p1, Id_Programa),
                    WorkOrder = @p2,
                    PiezasProgramadas = @p3,
                    Trolleys = ISNULL(@p4, ''),
                    Id_Linea = @p6, Comentarios = @p7,
                    Linea = @p6
                WHERE Id = @p8";

            using (var conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                conn.Execute(sql, new
                {
                    p0 = model.Id_Proceso > 0 ? model.Id_Proceso : 1,
                    p1 = (object)model.Id_Programa ?? DBNull.Value,
                    p2 = (object)model.WorkOrder   ?? DBNull.Value,
                    p3 = model.PiezasProgramadas,
                    p4 = string.IsNullOrEmpty(model.Trolleys) ? "" : model.Trolleys,
                    p6 = (object)model.Id_Linea    ?? DBNull.Value,
                    p7 = (object)model.Comentarios ?? DBNull.Value,
                    p8 = model.Id
                });
            }
        }

        public void Delete(ProgramGestionViewModel model)
        {
            string sql = "DELETE FROM [Process].[TrolleySetup] WHERE Id = @p0";
            using (var conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                conn.Execute(sql, new { p0 = model.Id });
            }
        }

        public void DeleteCarta(string workOrder, string ensamble)
        {
            string sql = string.IsNullOrEmpty(ensamble)
                ? "DELETE FROM [Process].[TrolleySetup] WHERE WorkOrder = @wo"
                : @"DELETE FROM [Process].[TrolleySetup]
                    WHERE WorkOrder = @wo
                      AND Id_Programa IN (
                          SELECT p.Numero FROM [Process].[Programas] p
                          INNER JOIN [Process].[Ensambles] e ON e.Id = p.Ensamble
                          WHERE e.EnsambleBase = @ensamble
                      )";
            using (var conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                conn.Execute(sql, new { wo = workOrder, ensamble });
            }
        }

        public List<SelectItemDto> GetLineas()
        {
            try
            {
                string query = @"
                    SELECT DISTINCT l.Numero_Linea
                    FROM [Process].[Lineas] l
                    INNER JOIN [Process].[Ensambles] e ON e.Linea1 = l.Id_Linea
                    WHERE e.EnsambleBase IS NOT NULL AND e.EnsambleBase <> ''
                    ORDER BY l.Numero_Linea ASC";

                using (var conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    var result = conn.Query<int>(query).ToList();
                    return result.Select(x => new SelectItemDto { Text = "Linea " + x, Value = x.ToString() }).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error en GetLineas: " + ex.Message);
                return new List<SelectItemDto>();
            }
        }

        public List<SelectItemDto> GetEnsambles(int lineaId)
        {
            try
            {
                string query = @"
                    SELECT DISTINCT e.EnsambleBase
                    FROM [Process].[Ensambles] e
                    INNER JOIN [Process].[Lineas] l ON e.Linea1 = l.Id_Linea
                    WHERE l.Numero_Linea = @p0
                    AND e.EnsambleBase IS NOT NULL AND e.EnsambleBase <> ''
                    ORDER BY e.EnsambleBase ASC";

                using (var conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    var items = conn.Query<string>(query, new { p0 = lineaId }).ToList();
                    return items.Where(x => !string.IsNullOrEmpty(x)).Select(x => new SelectItemDto { Text = x, Value = x }).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error en GetEnsambles: " + ex.Message);
                return new List<SelectItemDto>();
            }
        }

        public List<SelectItemDto> GetProgramas(string ensamble)
        {
            try
            {
                if (string.IsNullOrEmpty(ensamble)) return new List<SelectItemDto>();

                string query = @"
                    SELECT DISTINCT p.Numero
                    FROM [Process].[Programas] p
                    INNER JOIN [Process].[Ensambles] e ON p.Ensamble = e.Id
                    WHERE e.EnsambleBase = @p0
                    AND p.Numero IS NOT NULL AND p.Numero <> ''
                    ORDER BY p.Numero ASC";

                using (var conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    var items = conn.Query<string>(query, new { p0 = ensamble }).ToList();
                    return items.Where(x => !string.IsNullOrEmpty(x)).Select(x => new SelectItemDto { Text = x, Value = x }).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error en GetProgramas: " + ex.Message);
                return new List<SelectItemDto>();
            }
        }

        public List<string> GetLadosPorEnsamble(string ensamble)
        {
            try
            {
                if (string.IsNullOrEmpty(ensamble)) return new List<string>();
                string query = @"
                    SELECT p.Numero
                    FROM [Process].[Programas] p
                    INNER JOIN [Process].[Ensambles] e ON p.Ensamble = e.Id
                    WHERE e.EnsambleBase = @p0
                    AND p.Numero IS NOT NULL AND p.Numero <> ''
                    ORDER BY p.Numero ASC";
                using (var conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    return conn.Query<string>(query, new { p0 = ensamble }).ToList();
                }
            }
            catch { return new List<string>(); }
        }

        public List<string> GetLadosHermanos(string programaNumero)
        {
            try
            {
                if (string.IsNullOrEmpty(programaNumero)) return new List<string>();
                string query = @"
                    SELECT p2.Numero
                    FROM [Process].[Programas] p1
                    INNER JOIN [Process].[Programas] p2 ON p2.Ensamble = p1.Ensamble
                    WHERE p1.Numero = @prog
                      AND p2.Numero IS NOT NULL AND p2.Numero <> ''
                    ORDER BY p2.Numero ASC";
                using (var conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    return conn.Query<string>(query, new { prog = programaNumero }).ToList();
                }
            }
            catch { return new List<string>(); }
        }

        public int PreGuardar(List<ExcelPreviewItemDto> items)
        {
            int conteo = 0;
            const string sqlInsert = @"INSERT INTO [Process].[TrolleySetup]
                   (Id_Proceso, Id_Programa, WorkOrder, PiezasProgramadas, Trolleys, Status, FechaCreacion, Id_Linea, Comentarios, Linea)
                   VALUES (@IdProceso, @IdPrograma, @WorkOrder, @PiezasProgramadas, @Trolleys, @Status, @FechaCreacion, @IdLinea, @Comentarios, @Linea)";
            const string sqlUpdate = @"UPDATE [Process].[TrolleySetup]
                   SET PiezasProgramadas = @PiezasProgramadas
                   WHERE Id = @Id";

            using (var conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in items)
                        {
                            if (item.EsDuplicado || item.ExistingId > 0) continue;
                            conn.Execute(sqlInsert, new
                            {
                                IdProceso         = 1,
                                IdPrograma        = (object)item.Id_Programa,
                                WorkOrder         = (object)item.WorkOrder,
                                PiezasProgramadas = item.PiezasProgramadas,
                                Trolleys          = "",
                                Status            = item.Pendiente ? "Pendiente" : "PreCarga",
                                FechaCreacion     = item.FechaCreacion,
                                IdLinea           = (object)item.Id_Linea ?? DBNull.Value,
                                Comentarios       = (object)item.Comentarios ?? DBNull.Value,
                                Linea             = item.Id_Linea ?? 0
                            }, transaction);
                            conteo++;
                        }
                        transaction.Commit();
                    }
                    catch { transaction.Rollback(); throw; }
                }
            }
            return conteo;
        }

        public List<ExcelPreviewItemDto> GetPrecargas()
        {
            const string sql = @"
                SELECT ts.Id, ts.Id_Programa, ts.WorkOrder, ts.PiezasProgramadas,
                       ts.FechaCreacion, ts.Id_Linea, ts.Comentarios,
                       ISNULL(e.EnsambleBase, ts.Id_Programa) AS Ensamble
                FROM   [Process].[TrolleySetup] ts
                LEFT JOIN [Process].[Programas] p ON p.Numero = ts.Id_Programa
                LEFT JOIN [Process].[Ensambles] e ON e.Id     = p.Ensamble
                WHERE  ts.Status = 'PreCarga'
                ORDER  BY ts.FechaCreacion ASC";

            using (var conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                return conn.Query<ExcelPreviewItemDto>(sql).ToList();
            }
        }

        public int AutorizarPrecargas()
        {
            const string sql = "UPDATE [Process].[TrolleySetup] SET Status = 'Creado' WHERE Status = 'PreCarga'";
            using (var conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                return conn.Execute(sql);
            }
        }

        public void RechazarPrecargas()
        {
            const string sql = "DELETE FROM [Process].[TrolleySetup] WHERE Status = 'PreCarga'";
            using (var conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                conn.Execute(sql);
            }
        }

        private DateTime? ObtenerFechaDeExcel(IXLCell celda)
        {
            try
            {
                if (celda.IsEmpty()) return null;
                if (celda.TryGetValue(out DateTime dt)) return dt.Date;

                string texto = celda.GetString() ?? "";
                if (string.IsNullOrWhiteSpace(texto)) return null;

                if (DateTime.TryParse(texto, out DateTime fechaConvertida)) return fechaConvertida.Date;
                if (double.TryParse(texto, out double oaDate)) return DateTime.FromOADate(oaDate).Date;
                if (DateTime.TryParse(texto, new System.Globalization.CultureInfo("en-US"), System.Globalization.DateTimeStyles.None, out DateTime fechaUS))
                    return fechaUS.Date;
            }
            catch { }

            return null;
        }

        private int ObtenerEntero(IXLCell celda)
        {
            try
            {
                string texto = celda.GetString() ?? "";
                if (string.IsNullOrWhiteSpace(texto)) return 0;
                if (double.TryParse(texto, out double numero)) return (int)Math.Round(numero);
            }
            catch { }
            return 0;
        }

        private class EnsambleDto
        {
            public int Id { get; set; }
            public int? Linea1 { get; set; }
        }

        private class FiscalWeekDto
        {
            public int FiscalWeek { get; set; }
            public int FiscalYear { get; set; }
        }

        public class SemanaDelMesDto
        {
            public int Semana { get; set; }
            public string Rango { get; set; }
        }

        public List<SemanaDelMesDto> GetSemanasDelMesFiscal(int ano, int mes)
        {
            var semanas = new List<SemanaDelMesDto>();

            try
            {

                DateTime primerDiaDelMes = new DateTime(ano, mes, 1);
                DateTime ultimoDiaDelMes = primerDiaDelMes.AddMonths(1).AddDays(-1);

                using (var conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();

                    var semanasDelMes = conn.Query<dynamic>(
                        @"SELECT DISTINCT fc.FiscalWeek, fc.FiscalYear,
                                 MIN(fc.[Date]) as FechaInicio, MAX(fc.[Date]) as FechaFin
                          FROM dbo.FiscalCalendar fc
                          WHERE CAST(fc.[Date] AS DATE) >= @inicio
                            AND CAST(fc.[Date] AS DATE) <= @fin
                          GROUP BY fc.FiscalWeek, fc.FiscalYear
                          ORDER BY fc.FiscalYear, fc.FiscalWeek",
                        new { inicio = primerDiaDelMes.Date, fin = ultimoDiaDelMes.Date }).ToList();

                    foreach (var row in semanasDelMes)
                    {
                        DateTime fechaInicio = ((DateTime)row.FechaInicio).Date;
                        DateTime fechaFin = ((DateTime)row.FechaFin).Date;
                        string rango = fechaInicio.ToString("dd/MM") + " - " + fechaFin.ToString("dd/MM");

                        semanas.Add(new SemanaDelMesDto
                        {
                            Semana = (int)row.FiscalWeek,
                            Rango = rango
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error GetSemanasDelMesFiscal: " + ex.Message);
            }

            return semanas;
        }

        public string GetSemanaFiscal(DateTime fecha)
        {
            try
            {
                using (var conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    var row = conn.QueryFirstOrDefault<FiscalWeekDto>(
                        "SELECT TOP 1 FiscalWeek, FiscalYear FROM dbo.FiscalCalendar WHERE CAST([Date] AS DATE) = CAST(@fecha AS DATE)",
                        new { fecha = fecha.Date });

                    if (row != null)
                        return row.FiscalWeek.ToString().PadLeft(2, '0') + "|" + row.FiscalYear.ToString().Substring(2);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error GetSemanaFiscal BD: " + ex.Message);
            }

            return CalcularSemanaFiscal(fecha);
        }

        private string CalcularSemanaFiscal(DateTime fecha)
        {

            int anioFiscal = fecha.Year;

            DateTime inicioFiscal = UltimoSabadoDiciembre(anioFiscal - 1);

            if (fecha.Date < inicioFiscal.Date)
            {
                anioFiscal--;
                inicioFiscal = UltimoSabadoDiciembre(anioFiscal - 1);
            }

            int diasDesdeInicio = (fecha.Date - inicioFiscal.Date).Days;
            int semana = (diasDesdeInicio / 7) + 1;

            return semana.ToString().PadLeft(2, '0') + "|" + anioFiscal.ToString().Substring(2);
        }

        public Tuple<DateTime, DateTime> GetRangoSemanaFiscal(int semana, int anio)
        {
            DateTime inicioFiscal = UltimoSabadoDiciembre(anio - 1);
            DateTime start = inicioFiscal.AddDays((semana - 1) * 7);
            return Tuple.Create(start, start.AddDays(6));
        }

        public void CambiarStatusKanban(int id, string status)
        {
            if (status != "Pendiente" && status != "Creado")
                throw new Exception("Estado no permitido.");
            const string sql = @"
                UPDATE [Process].[TrolleySetup]
                SET    Status = @status
                WHERE  Id     = @id
                  AND  Status IN ('Pendiente','Creado')";
            using (var conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                conn.Execute(sql, new { id, status });
            }
        }

        private DateTime UltimoSabadoDiciembre(int anio)
        {
            DateTime ultimoDic = new DateTime(anio, 12, 31);
            int diasHastaSabado = ((int)ultimoDic.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
            return ultimoDic.AddDays(-diasHastaSabado);
        }
    }
}
