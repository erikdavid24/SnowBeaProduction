using ClosedXML.Excel;
using SnowTrolleyProduction.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace SnowTrolleyProduction.Controllers.service
{
    public class ProgramGestionService
    {
        BAESystemsGuaymasEntities BD = new BAESystemsGuaymasEntities();

        public ProgramGestionService(BAESystemsGuaymasEntities BDContext)
        {
            BD = BDContext;
        }

        public List<ProgramGestionViewModel> Read(string startDate, string endDate)
        {
            string sql = @"
                SELECT 
                    Id, Id_Proceso, Id_Programa, WorkOrder, PiezasProgramadas, 
                    Trolleys, Status, FechaCreacion, FechaFinalizacion, Id_Linea, Comentarios
                FROM [Proccess].[TrolleySetup]";

            if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
            {
                return BD.Database.SqlQuery<ProgramGestionViewModel>(sql).ToList();
            }
            else
            {
                sql += " WHERE CAST(FechaCreacion AS DATE) >= @start AND CAST(FechaCreacion AS DATE) <= @end";
                DateTime fechaInicio = DateTime.ParseExact(startDate, "yyyy/MM/dd", null);
                DateTime fechaFin = DateTime.ParseExact(endDate, "yyyy/MM/dd", null);

                var paramStart = new SqlParameter("@start", fechaInicio.Date);
                var paramEnd = new SqlParameter("@end", fechaFin.Date);

                return BD.Database.SqlQuery<ProgramGestionViewModel>(sql, paramStart, paramEnd).ToList();
            }
        }

        public void Create(ProgramGestionViewModel model)
        {
            string sql = @"INSERT INTO [Proccess].[TrolleySetup] 
                   (Id_Proceso, Id_Programa, WorkOrder, PiezasProgramadas, Trolleys, Status, FechaCreacion, Id_Linea, Comentarios, Linea) 
                   VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9)";

            BD.Database.ExecuteSqlCommand(sql,
                model.Id_Proceso,
                model.Id_Programa ?? (object)DBNull.Value,
                model.WorkOrder ?? (object)DBNull.Value,
                model.PiezasProgramadas,
                model.Trolleys ?? (object)DBNull.Value,
                model.Status ?? "Pendiente", // <-- CORRECCIÓN: Estado compatible
                model.FechaCreacion != DateTime.MinValue ? model.FechaCreacion : DateTime.Now,
                model.Id_Linea ?? (object)DBNull.Value,
                model.Comentarios ?? (object)DBNull.Value,
                model.Id_Linea ?? 0 // <-- CORRECCIÓN: Evitamos que la línea quede en 0
            );
        }

        public int CargarDesdeExcel(Stream streamArchivo, string nombreArchivo)
        {
            int conteoInsertados = 0;

            string digitosArchivo = new string(nombreArchivo.Where(char.IsDigit).ToArray());
            string semanaPlan = digitosArchivo.Length >= 2 ? digitosArchivo.Substring(0, 2) : "01";
            string anioActual = DateTime.Now.Year.ToString();

            using (var workBook = new XLWorkbook(streamArchivo))
            {
                var sheet = workBook.Worksheets.FirstOrDefault(w => w.Name.IndexOf("SMT Run Plan", StringComparison.OrdinalIgnoreCase) >= 0);
                if (sheet == null) sheet = workBook.Worksheet(3);

                var rows = sheet.RangeUsed().RowsUsed().Skip(1);

                foreach (var row in rows)
                {
                    var filaReal = row.WorksheetRow();

                    // 1. EL FILTRO DEFINITIVO DE FECHA
                    DateTime? fechaPlanOpt = ObtenerFechaDeExcel(filaReal.Cell("A"));
                    if (!fechaPlanOpt.HasValue) continue;

                    DateTime fechaPlan = fechaPlanOpt.Value;

                    // 2. Col B: Programa (corresponde a EnsambleBase)
                    string programaBaseExcel = filaReal.Cell("B").GetString()?.Trim();
                    if (string.IsNullOrWhiteSpace(programaBaseExcel)) continue;

                    // 3. Col H: Linea
                    string lineaExcelStr = filaReal.Cell("H").GetString()?.Trim() ?? "";
                    int? lineaFinal = null;
                    if (int.TryParse(lineaExcelStr, out int parsedLine))
                    {
                        lineaFinal = parsedLine;
                    }

                    // 4. Col Y: Piezas
                    int piezas = ObtenerEntero(filaReal.Cell("Y"));

                    // 5. Buscar el ensamble padre en la BD que coincida con EnsambleBase y Linea
                    var infoEnsamble = BD.Ensambles.FirstOrDefault(e => e.EnsambleBase == programaBaseExcel && e.Linea1 == lineaFinal);

                    // Si no se encontró con línea, intentar solo por EnsambleBase
                    if (infoEnsamble == null)
                    {
                        infoEnsamble = BD.Ensambles.FirstOrDefault(e => e.EnsambleBase == programaBaseExcel);
                    }

                    // Tomar la línea de la BD si existe
                    if (infoEnsamble != null && infoEnsamble.Linea1 != null)
                    {
                        lineaFinal = infoEnsamble.Linea1;
                    }

                    // 6. Obtener la lista REAL de programas hijos desde [Proccess].[Programas]
                    List<string> programasReales = new List<string>();
                    if (infoEnsamble != null)
                    {
                        programasReales = BD.Programas
                            .Where(p => p.Ensamble == infoEnsamble.Id && p.Numero != null && p.Numero != "")
                            .Select(p => p.Numero)
                            .ToList();
                    }

                    // 7. Fallback: si no hay hijos en catálogo, insertar el nombre tal cual del Excel
                    if (!programasReales.Any())
                    {
                        programasReales.Add(programaBaseExcel);
                    }

                    // 8. Iterar sobre los programas REALES para insertar las tarjetas
                    foreach (var programaReal in programasReales)
                    {
                        string woLinea = lineaFinal.HasValue ? lineaFinal.Value.ToString() : "0";
                        string workOrderGenerado = $"L{woLinea}{semanaPlan}{anioActual}000";

                        InsertarTarjeta(programaReal, fechaPlan, lineaFinal, piezas, workOrderGenerado);
                        conteoInsertados++;
                    }
                }
            }

            return conteoInsertados;
        }

        private void InsertarTarjeta(string programa, DateTime fecha, int? linea, int piezas, string workOrder)
        {
            Create(new ProgramGestionViewModel
            {
                Id_Proceso = 1,
                WorkOrder = workOrder,
                Id_Programa = programa,
                PiezasProgramadas = piezas,
                Trolleys = "",
                Id_Linea = linea,
                Comentarios = "Carga Excel",
                Status = "Pendiente", // <-- CORRECCIÓN
                FechaCreacion = DateTime.Now
            });
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

                if (double.TryParse(texto, out double numero))
                    return (int)Math.Round(numero);
            }
            catch { }

            return 0;
        }

        // ==============================================================================
        // MÉTODOS PARA LLENAR LOS COMBOBOX (Extraídos DE LA BD)
        // ==============================================================================

        public List<SelectItemDto> GetLineas()
        {
            try
            {
                string query = @"
                    SELECT DISTINCT l.Numero_Linea
                    FROM [Proccess].[Lineas] l
                    INNER JOIN [Proccess].[Ensambles] e ON e.Linea1 = l.Id_Linea
                    WHERE e.EnsambleBase IS NOT NULL AND e.EnsambleBase <> ''
                    ORDER BY l.Numero_Linea ASC";

                return BD.Database.SqlQuery<int>(query)
                    .ToList()
                    .Select(x => new SelectItemDto { Text = "Línea " + x, Value = x.ToString() })
                    .ToList();
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
                    FROM [Proccess].[Ensambles] e
                    INNER JOIN [Proccess].[Lineas] l ON e.Linea1 = l.Id_Linea
                    WHERE l.Numero_Linea = @p0
                    AND e.EnsambleBase IS NOT NULL AND e.EnsambleBase <> ''
                    ORDER BY e.EnsambleBase ASC";

                return BD.Database.SqlQuery<string>(query, lineaId)
                    .ToList()
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Select(x => new SelectItemDto { Text = x, Value = x })
                    .ToList();
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
                if (string.IsNullOrEmpty(ensamble))
                    return new List<SelectItemDto>();

                string query = @"
                    SELECT DISTINCT p.Numero
                    FROM [Proccess].[Programas] p
                    INNER JOIN [Proccess].[Ensambles] e ON p.Ensamble = e.Id
                    WHERE e.EnsambleBase = @p0
                    AND p.Numero IS NOT NULL AND p.Numero <> ''
                    ORDER BY p.Numero ASC";

                return BD.Database.SqlQuery<string>(query, ensamble)
                    .ToList()
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Select(x => new SelectItemDto { Text = x, Value = x })
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error en GetProgramas: " + ex.Message);
                return new List<SelectItemDto>();
            }
        }

        public void Update(ProgramGestionViewModel model)
        {
            string sql = @"
                UPDATE [Proccess].[TrolleySetup]
                SET Id_Proceso = @p0, Id_Programa = @p1, WorkOrder = @p2, PiezasProgramadas = @p3,
                    Trolleys = @p4, Status = @p5, Id_Linea = @p6, Comentarios = @p7,
                    FechaFinalizacion = CASE WHEN @p5 = 'Completado' AND FechaFinalizacion IS NULL THEN GETDATE() ELSE FechaFinalizacion END,
                    Linea = @p6 -- <-- CORRECCIÓN: Para que la línea principal se mantenga sincronizada
                WHERE Id = @p8";

            BD.Database.ExecuteSqlCommand(sql,
                new SqlParameter("@p0", model.Id_Proceso),
                new SqlParameter("@p1", (object)model.Id_Programa ?? DBNull.Value),
                new SqlParameter("@p2", (object)model.WorkOrder ?? DBNull.Value),
                new SqlParameter("@p3", model.PiezasProgramadas),
                new SqlParameter("@p4", (object)model.Trolleys ?? DBNull.Value),
                new SqlParameter("@p5", (object)model.Status ?? DBNull.Value),
                new SqlParameter("@p6", (object)model.Id_Linea ?? DBNull.Value),
                new SqlParameter("@p7", (object)model.Comentarios ?? DBNull.Value),
                new SqlParameter("@p8", model.Id)
            );
        }

        public void Delete(ProgramGestionViewModel model)
        {
            string sql = "DELETE FROM [Proccess].[TrolleySetup] WHERE Id = @p0";
            BD.Database.ExecuteSqlCommand(sql, new SqlParameter("@p0", model.Id));
        }
    }
}