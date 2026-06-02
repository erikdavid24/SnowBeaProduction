using Dapper;
using SnowTrolleyProduction.Models;
using SnowTrolleyProduction.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;

namespace SnowTrolleyProduction.Controllers.service
{
    /// <summary>
    /// Encapsula toda la l�gica de datos para la pantalla de Setup de Trolleys:
    /// carga del SVG, inicio de proceso, finalizaci�n y autorizaci�n.
    /// </summary>
    public class TrolleySetupService
    {
        private readonly string _connStr;

        public TrolleySetupService(BAESystemsGuaymasEntitiesSmtPlan ctx)
        {
            _connStr = ctx.Database.Connection.ConnectionString;
        }

        // ?? Datos del Setup (SVG) ?????????????????????????????????????????????

        /// <summary>
        /// Devuelve el programa activo ('En Proceso') para una l�nea y
        /// las posiciones de trolleys para pintar el SVG.
        /// Retorna null si no hay trabajo activo.
        /// </summary>
        public SetupDatosDto GetDatosPorLinea(int linea)
        {
            const string sqlWorks = @"
                SELECT
                    Id               AS IdProceso,
                    Id_Programa      AS ProgramaSeleccionado,
                    PiezasProgramadas,
                    Status
                FROM  [Process].[TrolleySetup]
                WHERE  Linea   = @linea
                  AND  Status IN ('Setup', 'Arranque')
                ORDER  BY
                    CASE WHEN Status = 'Arranque' THEN 1 ELSE 2 END ASC,
                    FechaCreacion ASC";

            const string sqlProgId = @"
                SELECT Id FROM [Process].[Programas]
                WHERE  Numero = @prog;";

            const string sqlAcomodo = @"
                SELECT
                    et.Equipo_descripcion  AS noTrolley,
                    a.Locacion             AS noPosicion,
                    em.Equipo_descripcion  AS nombreMaquina
                FROM  [Process].[Acomodo]  a
                INNER JOIN [Process].[Equipos]  et ON a.TrolleyId  = et.Id_Equipo
                INNER JOIN [Process].[Maquinas]  m ON a.MaquinaId  =  m.Id
                INNER JOIN [Process].[Equipos]  em ON m.EquipoId   = em.Id_Equipo
                WHERE  a.ProgramaId = @programaId;";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();

                var works = conn.Query<dynamic>(sqlWorks, new { linea }).ToList();
                if (works == null || works.Count == 0) return null;

                // Use the first record as the primary reference
                var primary = works[0];

                var positions = new List<TrolleyPosition>();
                var allIds = new List<int>();

                // Gather trolley positions from ALL active records (both sides)
                foreach (var work in works)
                {
                    allIds.Add((int)work.IdProceso);

                    int programaIdInt = conn.QueryFirstOrDefault<int>(
                        sqlProgId, new { prog = (string)work.ProgramaSeleccionado });

                    if (programaIdInt > 0)
                    {
                        var rows = conn.Query<dynamic>(sqlAcomodo, new { programaId = programaIdInt });
                        foreach (var row in rows)
                        {
                            string maquina = ((string)row.nombreMaquina ?? "").Trim();
                            int cabezal = maquina.Contains("-2 ") ? 2 : 1;

                            positions.Add(new TrolleyPosition
                            {
                                noTrolley     = (string)row.noTrolley,
                                noPosicion    = (int)row.noPosicion,
                                nombreMaquina = maquina,
                                noCabezal     = cabezal
                            });
                        }
                    }
                }

                // Determine overall status: if ANY is 'Arranque', report Arranque
                string overallStatus = works.Any(w => (string)w.Status == "Arranque")
                    ? "Arranque"
                    : (string)primary.Status;

                return new SetupDatosDto
                {
                    programaSeleccionado = (string)primary.ProgramaSeleccionado,
                    piezasProgramadas    = (int)primary.PiezasProgramadas,
                    idProceso            = (int)primary.IdProceso,
                    pzaTerminadas        = 0,
                    status               = overallStatus,
                    trolleyPositions     = positions
                };
            }
        }


        /// <summary>
        /// Devuelve los trolleys disponibles en la l�nea y
        /// el acomodo actual del programa dado.
        /// (Usado por el modal de Setup en ProgramGestion.)
        /// </summary>
        public TrolleySetupDataDto GetTrolleysSetup(int linea, int programaId)
        {
            const string sqlTrolleys = @"
                SELECT e.Id_Equipo AS Id, e.Equipo_descripcion AS Descripcion
                FROM   [Process].[Equipos] e
                INNER  JOIN [Process].[Lineas] l ON e.LineaId = l.Id_Linea
                WHERE  l.Numero_Linea = @linea
                  AND  (   (e.Equipo_descripcion LIKE 'A%'
                         OR e.Equipo_descripcion LIKE 'B%'
                         OR e.Equipo_descripcion LIKE 'C%')
                       AND LEN(e.Equipo_descripcion) = 3
                       OR  e.Equipo_descripcion = 'MANUAL'
                       OR  e.Equipo_descripcion = 'MATRIX')
                ORDER  BY e.Equipo_descripcion";

            const string sqlAcomodo = @"
                SELECT a.Locacion, a.TrolleyId, a.MaquinaId,
                       em.Equipo_descripcion AS MaquinaNombre
                FROM   [Process].[Acomodo] a
                INNER  JOIN [Process].[Programas]    p  ON a.ProgramaId = p.Id
                INNER  JOIN [Process].[TrolleySetup] ts ON ts.Id_Programa = p.Numero
                LEFT   JOIN [Process].[Maquinas]     m  ON a.MaquinaId = m.Id
                LEFT   JOIN [Process].[Equipos]      em ON m.EquipoId  = em.Id_Equipo
                WHERE  ts.Id = @programaId
                ORDER  BY a.MaquinaId, a.Locacion";

            const string sqlMaquinas = @"
                SELECT m.Id, e.Equipo_descripcion AS Descripcion
                FROM   [Process].[Maquinas] m
                INNER  JOIN [Process].[Equipos] e ON m.EquipoId = e.Id_Equipo
                WHERE  m.LineaId = (SELECT TOP 1 l.Id_Linea FROM [Process].[Lineas] l WHERE l.Numero_Linea = @linea)
                ORDER  BY e.Equipo_descripcion";

            var result = new TrolleySetupDataDto
            {
                Trolleys   = new List<TrolleyItemDto>(),
                Maquinas   = new List<TrolleyItemDto>(),
                Cabezales  = new List<CabezalAcomodoDto>(),
                Acomodo    = new Dictionary<string, int>(),
                MaquinaId  = 0
            };

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                result.Trolleys = conn.Query<TrolleyItemDto>(sqlTrolleys, new { linea }).ToList();
                result.Maquinas = conn.Query<TrolleyItemDto>(sqlMaquinas, new { linea }).ToList();

                var acomodoRows = conn.Query<dynamic>(sqlAcomodo, new { programaId }).ToList();

                // Group acomodos by machine (cabezal)
                var byMachine = new Dictionary<int, CabezalAcomodoDto>();
                foreach (var row in acomodoRows)
                {
                    int maqId = row.MaquinaId != null ? (int)row.MaquinaId : 0;
                    string maqName = row.MaquinaNombre != null ? ((string)row.MaquinaNombre).Trim() : "";
                    int cabezal = maqName.Contains("-2 ") ? 2 : 1;

                    if (!byMachine.ContainsKey(maqId))
                    {
                        byMachine[maqId] = new CabezalAcomodoDto
                        {
                            Cabezal       = cabezal,
                            MaquinaId     = maqId,
                            MaquinaNombre = maqName,
                            Acomodo       = new Dictionary<string, int>()
                        };
                    }

                    byMachine[maqId].Acomodo["Z" + row.Locacion] = (int)row.TrolleyId;
                }

                result.Cabezales = byMachine.Values.OrderBy(c => c.Cabezal).ToList();

                // Backwards compat: flat acomodo (merge all)
                foreach (var cab in result.Cabezales)
                {
                    foreach (var kv in cab.Acomodo)
                    {
                        if (!result.Acomodo.ContainsKey(kv.Key))
                            result.Acomodo[kv.Key] = kv.Value;
                    }
                    if (result.MaquinaId == 0 && cab.MaquinaId > 0)
                        result.MaquinaId = cab.MaquinaId;
                }
            }

            return result;
        }


        /// <summary>
        /// Reemplaza los registros de dbo.Acomodo para el programa indicado
        /// con las zonas/trolleys del diccionario { locacion ? trolleyId }.
        /// </summary>
        public void GuardarSetupTrolleys(int programaId, Dictionary<int, int> zonas, int? maquinaIdOverride = null)
        {
            const string sqlGetProg = @"
                SELECT TOP 1 p.Id
                FROM   [Process].[Programas]    p
                INNER  JOIN [Process].[TrolleySetup] ts ON ts.Id_Programa = p.Numero
                WHERE  ts.Id = @programaId";

            const string sqlGetMaquina = @"
                SELECT TOP 1 m.Id
                FROM   [Process].[Maquinas] m
                INNER  JOIN [Process].[Lineas]        l  ON m.LineaId    = l.Id_Linea
                INNER  JOIN [Process].[TrolleySetup]  ts ON l.Numero_Linea = ts.Linea
                WHERE  ts.Id = @programaId
                ORDER  BY m.Id";

            const string sqlDelete = "DELETE FROM [Process].[Acomodo] WHERE ProgramaId = @progId";

            const string sqlInsert = @"
                INSERT INTO [Process].[Acomodo] (ProgramaId, TrolleyId, Locacion, MaquinaId)
                VALUES (@progId, @trolleyId, @locacion, @maquinaId)";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();

                int progId = conn.QueryFirstOrDefault<int>(sqlGetProg, new { programaId });
                if (progId == 0) return;

                int maquinaId = maquinaIdOverride.HasValue && maquinaIdOverride.Value > 0
                    ? maquinaIdOverride.Value
                    : conn.QueryFirstOrDefault<int>(sqlGetMaquina, new { programaId });

                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        conn.Execute(sqlDelete, new { progId }, tx);

                        foreach (var kvp in zonas)
                        {
                            conn.Execute(sqlInsert, new
                            {
                                progId,
                                trolleyId = kvp.Value,
                                locacion  = kvp.Key,
                                maquinaId = maquinaId > 0 ? maquinaId : (int?)null
                            }, tx);
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }


        /// <summary>
        /// Appends acomodo records for the second machine (cabezal 2) for the same programa.
        /// Should be called after GuardarSetupTrolleys which deletes+inserts cabezal 1.
        /// </summary>
        public void GuardarSetupTrolleysCabezal2(int programaId, Dictionary<int, int> zonas, int maquinaId)
        {
            const string sqlGetProg = @"
                SELECT TOP 1 p.Id
                FROM   [Process].[Programas]    p
                INNER  JOIN [Process].[TrolleySetup] ts ON ts.Id_Programa = p.Numero
                WHERE  ts.Id = @programaId";

            const string sqlInsert = @"
                INSERT INTO [Process].[Acomodo] (ProgramaId, TrolleyId, Locacion, MaquinaId)
                VALUES (@progId, @trolleyId, @locacion, @maquinaId)";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();

                int progId = conn.QueryFirstOrDefault<int>(sqlGetProg, new { programaId });
                if (progId == 0) return;

                foreach (var kvp in zonas)
                {
                    conn.Execute(sqlInsert, new
                    {
                        progId,
                        trolleyId = kvp.Value,
                        locacion  = kvp.Key,
                        maquinaId
                    });
                }
            }
        }


        /// <summary>
        /// Finaliza el proceso:
        /// - Si piezasProducidas >= programadas ? Status = 'Completado'.
        /// - Si es parcial ? Status = 'Completado' + crea nuevo registro 'Creado'
        ///   con las piezas pendientes y una WorkOrder derivada.
        /// </summary>
        public void FinalizarProceso(int idProceso, int piezasProducidas, string comentarios)
        {
            const string sqlSelect = @"
                SELECT Id, WorkOrder, Linea, Id_Linea, Id_Programa, PiezasProgramadas
                FROM   [Process].[TrolleySetup]
                WHERE  Id = @id AND Status IN ('Setup', 'Arranque')";

            const string sqlCompletado = @"
                UPDATE [Process].[TrolleySetup]
                SET    Status = 'Completado', FechaFinalizacion = GETDATE()
                WHERE  Id = @id";

            const string sqlParcial = @"
                UPDATE [Process].[TrolleySetup]
                SET    Status = 'Finalizado Parcial', FechaFinalizacion = GETDATE(),
                       Comentarios = @coment, PiezasProgramadas = @pzasProducidas
                WHERE  Id = @id";

            const string sqlInsertPendiente = @"
                INSERT INTO [Process].[TrolleySetup]
                    (Id_Proceso, Id_Programa, WorkOrder, PiezasProgramadas,
                     Trolleys, Status, FechaCreacion, Id_Linea, Linea, Comentarios)
                VALUES
                    (1, @prog, @wo, @pzas,
                     '', 'Creado', GETDATE(), @idLinea, @linea, @comentario)";

            const string sqlSiblings = @"
                SELECT ts2.Id
                FROM   [Process].[TrolleySetup] ts1
                INNER JOIN [Process].[Programas] p1 ON p1.Numero = ts1.Id_Programa
                INNER JOIN [Process].[Programas] p2 ON p2.Ensamble = p1.Ensamble AND p2.Numero <> p1.Numero
                INNER JOIN [Process].[TrolleySetup] ts2 ON ts2.Id_Programa = p2.Numero
                WHERE  ts1.Id = @id
                  AND  ts2.Linea = ts1.Linea
                  AND  ts2.Status IN ('Setup', 'Arranque')";

            const string sqlSiblingSelect = @"
                SELECT Id, WorkOrder, Linea, Id_Linea, Id_Programa, PiezasProgramadas
                FROM   [Process].[TrolleySetup]
                WHERE  Id = @id";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                var tx = conn.BeginTransaction();
                try
                {
                    var current = conn.QueryFirstOrDefault<dynamic>(sqlSelect, new { id = idProceso }, tx);
                    if (current == null) throw new Exception("Proceso no encontrado.");

                    int pzasProg = (int)current.PiezasProgramadas;

                    if (piezasProducidas > pzasProg)
                        throw new Exception($"No se pueden producir {piezasProducidas} piezas. El máximo programado es {pzasProg}.");

                    if (piezasProducidas >= pzasProg)
                    {
                        conn.Execute(sqlCompletado, new { id = idProceso }, tx);
                    }
                    else
                    {
                        conn.Execute(sqlParcial, new { id = idProceso, coment = comentarios ?? string.Empty, pzasProducidas = piezasProducidas }, tx);

                        int    linea    = (int)current.Linea;
                        string programa = (string)current.Id_Programa;
                        string nuevaWO  = GenerarNuevaWorkOrder((string)current.WorkOrder);
                        int    pendientes = pzasProg - piezasProducidas;
                        int    idLineaOrigen = current.Id_Linea != null ? (int)current.Id_Linea : linea;

                        conn.Execute(sqlInsertPendiente, new
                        {
                            prog      = programa,
                            wo        = nuevaWO,
                            pzas      = pendientes,
                            idLinea   = idLineaOrigen,
                            linea,
                            comentario = $"Saldo de {(string)current.WorkOrder} | Producidas: {piezasProducidas} | Pendientes: {pendientes}"
                        }, tx);
                    }


                    var siblingIds = conn.Query<int>(sqlSiblings, new { id = idProceso }, tx).ToList();
                    foreach (var sibId in siblingIds)
                    {
                        var sibling = conn.QueryFirstOrDefault<dynamic>(sqlSiblingSelect, new { id = sibId }, tx);
                        if (sibling == null) continue;

                        if (piezasProducidas >= (int)sibling.PiezasProgramadas)
                        {
                            conn.Execute(sqlCompletado, new { id = sibId }, tx);
                        }
                        else
                        {
                            conn.Execute(sqlParcial, new { id = sibId, coment = comentarios ?? string.Empty, pzasProducidas = piezasProducidas }, tx);

                            int    sibLinea    = (int)sibling.Linea;
                            string sibPrograma = (string)sibling.Id_Programa;
                            string sibNuevaWO  = GenerarNuevaWorkOrder((string)sibling.WorkOrder);
                            int    sibPendientes = (int)sibling.PiezasProgramadas - piezasProducidas;
                            int    sibIdLineaOrigen = sibling.Id_Linea != null ? (int)sibling.Id_Linea : sibLinea;

                            conn.Execute(sqlInsertPendiente, new
                            {
                                prog      = sibPrograma,
                                wo        = sibNuevaWO,
                                pzas      = sibPendientes,
                                idLinea   = sibIdLineaOrigen,
                                linea     = sibLinea,
                                comentario = $"Saldo de {(string)sibling.WorkOrder} | Producidas: {piezasProducidas} | Pendientes: {sibPendientes}"
                            }, tx);
                        }
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }


        /// <summary>
        /// Cambia el status de 'En Proceso' a 'Arranque', marcando la fecha.
        /// Equivalente al bot�n "INICIAR PROCESO" de SnowBAEGym.
        /// </summary>
        public (bool Success, string Message) ActualizarArranque(int idProceso)
        {
            const string sql = @"
                UPDATE [Process].[TrolleySetup]
                SET    Status = 'Arranque'
                WHERE  Id = @id AND Status = 'Setup'";

           
            const string sqlSiblings = @"
                SELECT ts2.Id
                FROM   [Process].[TrolleySetup] ts1
                INNER JOIN [Process].[Programas] p1 ON p1.Numero = ts1.Id_Programa
                INNER JOIN [Process].[Programas] p2 ON p2.Ensamble = p1.Ensamble AND p2.Numero <> p1.Numero
                INNER JOIN [Process].[TrolleySetup] ts2 ON ts2.Id_Programa = p2.Numero
                WHERE  ts1.Id = @id
                  AND  ts2.Linea = ts1.Linea
                  AND  ts2.Status = 'Setup'";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                int rows = conn.Execute(sql, new { id = idProceso });

                // Also update sibling sides to Arranque
                var siblingIds = conn.Query<int>(sqlSiblings, new { id = idProceso }).ToList();
                foreach (var sibId in siblingIds)
                {
                    conn.Execute(sql, new { id = sibId });
                }

                return rows > 0
                    ? (true, "Arranque iniciado.")
                    : (false, "No se pudo iniciar el arranque.");
            }
        }

        /// <summary>Comprueba si existe alg�n proceso activo ('En Proceso'),
        /// opcionalmente filtrado por l�nea.</summary>
        public (bool Exists, int Linea) GetProcesoActivo(int? linea)
        {
            string sql = @"
                SELECT TOP 1 Id AS Id_Proceso, Linea
                FROM   [Process].[TrolleySetup]
                WHERE  Status IN ('Setup', 'Arranque')";

            if (linea.HasValue) sql += " AND Linea = @linea";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                var row = conn.QueryFirstOrDefault<dynamic>(sql, new { linea });
                if (row != null)
                    return (true, (int)row.Linea);

                return (false, 0);
            }
        }

        public bool VerificarSupervisor(string employeeNumber, string password)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM [Process].[SmtPlan_Supervisores]
                WHERE EmployeeNumber = @employeeNumber
                  AND Password       = @password";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                return conn.QueryFirstOrDefault<int>(sql, new { employeeNumber, password }) > 0;
            }
        }

        public List<dynamic> GetSupervisores()
        {
            const string sql = "SELECT Id, EmployeeNumber, FullName, FechaAlta FROM [Process].[SmtPlan_Supervisores] ORDER BY FullName";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                return conn.Query<dynamic>(sql).ToList();
            }
        }

        public List<dynamic> GetEmpleadosRH()
        {
            const string sql = @"
                SELECT EmployeeID, EmployeeNumber, FullName
                FROM [RH].[EmployeeFullInfo]
                WHERE EmployeeStatus = 1
                  AND EmployeeNumber IS NOT NULL
                ORDER BY FullName";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                return conn.Query<dynamic>(sql).ToList();
            }
        }

        public void AgregarSupervisor(int employeeId, string employeeNumber, string fullName, string password)
        {
            const string sql = @"
                IF NOT EXISTS (SELECT 1 FROM [Process].[SmtPlan_Supervisores] WHERE EmployeeNumber = @employeeNumber)
                INSERT INTO [Process].[SmtPlan_Supervisores] (EmployeeID, EmployeeNumber, FullName, Password)
                VALUES (@employeeId, @employeeNumber, @fullName, @password)";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                conn.Execute(sql, new { employeeId, employeeNumber, fullName, password });
            }
        }

        public string GetSupervisorPassword(int id)
        {
            const string sql = "SELECT Password FROM [Process].[SmtPlan_Supervisores] WHERE Id = @id";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                return conn.QueryFirstOrDefault<string>(sql, new { id });
            }
        }

        public void ActualizarSupervisorPassword(int id, string password)
        {
            const string sql = "UPDATE [Process].[SmtPlan_Supervisores] SET Password = @password WHERE Id = @id";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                conn.Execute(sql, new { id, password });
            }
        }

        public void EliminarSupervisor(int id)
        {
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                conn.Execute("DELETE FROM [Process].[SmtPlan_Supervisores] WHERE Id = @id", new { id });
            }
        }

        private static string GenerarNuevaWorkOrder(string woActual)
        {
            var match = Regex.Match(woActual ?? string.Empty, @"-(\d+)$");
            if (match.Success)
            {
                int numero   = int.Parse(match.Groups[1].Value) + 1;
                string baseW = woActual.Substring(0, match.Index);
                return $"{baseW}-{numero}";
            }
            return (woActual ?? string.Empty) + "-1";
        }
    }
}
