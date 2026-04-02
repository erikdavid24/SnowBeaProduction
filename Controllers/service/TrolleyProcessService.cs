using Dapper;
using SnowTrolleyProduction.Models;
using SnowTrolleyProduction.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace SnowTrolleyProduction.Controllers.service
{
    /// <summary>
    /// Encapsula toda la lógica de consulta y transición de estado
    /// para la cola de procesos (TrolleyProcess).
    /// </summary>
    public class TrolleyProcessService
    {
        private readonly string _connStr;

        public TrolleyProcessService(BAESystemsGuaymasEntities ctx)
        {
            _connStr = ctx.Database.Connection.ConnectionString;
        }

        // ?? Consultas ?????????????????????????????????????????????????????????

        /// <summary>Devuelve los registros con Status = 'Creado' o 'Pendiente' para una línea,
        /// agrupados por ensamble (un item por ensamble con todos sus lados).</summary>
        public List<ProcesoItem> GetProcesosDisponibles(int linea)
        {
            const string sql = @"
                SELECT
                    ts.Id               AS id_Proceso,
                    ts.Id_Programa      AS id_Programa,
                    ts.WorkOrder        AS workOrder,
                    ts.PiezasProgramadas AS piezasProgramadas,
                    ISNULL(e.EnsambleBase, ts.Id_Programa) AS ensamble
                FROM   [Proccess].[TrolleySetup]  ts
                LEFT JOIN [Proccess].[Programas]  p  ON p.Numero  = ts.Id_Programa
                LEFT JOIN [Proccess].[Ensambles]  e  ON e.Id      = p.Ensamble
                WHERE  ts.Linea   = @linea
                  AND  ts.Status IN ('Creado', 'Pendiente')
                ORDER  BY e.EnsambleBase ASC, ts.FechaCreacion ASC";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                var rows = conn.Query<dynamic>(sql, new { linea }).ToList();

                // Agrupar por ensamble: el primero del grupo es el representante
                var grupos = new Dictionary<string, ProcesoItem>();
                foreach (var r in rows)
                {
                    string ens = (string)r.ensamble ?? (string)r.id_Programa;
                    string prog = (string)r.id_Programa;
                    if (!grupos.ContainsKey(ens))
                    {
                        grupos[ens] = new ProcesoItem
                        {
                            id_Proceso        = (int)r.id_Proceso,
                            id_Programa       = prog,
                            ensamble          = ens,
                            workOrder         = (string)r.workOrder,
                            piezasProgramadas = (int)r.piezasProgramadas,
                            trolleys          = new List<string>(),
                            lados             = new List<string>(),
                            idsProcesos       = new List<int>()
                        };
                    }
                    grupos[ens].lados.Add(prog);
                    grupos[ens].idsProcesos.Add((int)r.id_Proceso);
                }

                return grupos.Values.ToList();
            }
        }

        /// <summary>Devuelve los registros con Status = 'Setup' o 'Arranque' para una línea,
        /// incluyendo la lista de trolleys asignados en dbo.Acomodo.</summary>
        public List<ProcesoArranque> GetTrabajosEnProceso(int linea)
        {
            const string sqlWorks = @"
                SELECT Id AS id_Proceso, Linea AS linea, Id_Programa AS id_Programa,
                       WorkOrder AS workOrder, Status AS status, FechaCreacion AS fechaCreacion
                FROM   [Proccess].[TrolleySetup]
                WHERE  Linea   = @linea
                  AND  Status IN ('Setup', 'Arranque')
                ORDER  BY FechaCreacion ASC";

            const string sqlTrolleys = @"
                SELECT e.Equipo_descripcion
                FROM   [Proccess].[Acomodo] a
                INNER JOIN [Proccess].[Programas] p ON a.ProgramaId = p.Id
                INNER JOIN [Proccess].[Equipos]   e ON a.TrolleyId  = e.Id_Equipo
                WHERE  p.Numero = @prog
                ORDER  BY a.Locacion";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                var rows = conn.Query<dynamic>(sqlWorks, new { linea }).ToList();
                var trabajos = new List<ProcesoArranque>();

                foreach (var r in rows)
                {
                    var trolleyList = conn.Query<string>(sqlTrolleys, new { prog = (string)r.id_Programa }).ToList();
                    trabajos.Add(new ProcesoArranque
                    {
                        id_Proceso    = (int)r.id_Proceso,
                        linea         = (int)r.linea,
                        id_Programa   = (string)r.id_Programa,
                        workOrder     = (string)r.workOrder,
                        status        = (string)r.status,
                        fechaCreacion = r.fechaCreacion != null ? (DateTime)r.fechaCreacion : DateTime.Now,
                        trolleysList  = trolleyList,
                        trolleys      = string.Join(",", trolleyList)
                    });
                }

                return trabajos;
            }
        }

        /// <summary>Enriquece cada ProcesoItem con su lista de trolleys desde [Proccess].[Acomodo].</summary>
        public List<ProcesoItem> EnriquecerConTrolleys(List<ProcesoItem> procesos)
        {
            if (procesos == null || procesos.Count == 0)
                return procesos ?? new List<ProcesoItem>();

            const string sql = @"
                SELECT e.Equipo_descripcion
                FROM   [Proccess].[Acomodo] a
                INNER JOIN [Proccess].[Programas] p ON a.ProgramaId = p.Id
                INNER JOIN [Proccess].[Equipos]   e ON a.TrolleyId  = e.Id_Equipo
                WHERE  p.Numero = @prog
                ORDER  BY a.Locacion";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                foreach (var p in procesos)
                {
                    p.trolleys = conn.Query<string>(sql, new { prog = p.id_Programa }).ToList();
                }
            }

            return procesos;
        }

        // ?? Transición de estado ??????????????????????????????????????????????

        /// <summary>
        /// Carga procesos por IDs y los enriquece con sus trolleys de Acomodo.
        /// </summary>
        public List<ProcesoItem> GetProcesosConTrolleys(List<int> idProcesos)
        {
            const string sqlItems = @"
                SELECT
                    ts.Id               AS id_Proceso,
                    ts.Id_Programa      AS id_Programa,
                    ts.WorkOrder        AS workOrder,
                    ts.PiezasProgramadas AS piezasProgramadas,
                    ISNULL(e.EnsambleBase, ts.Id_Programa) AS ensamble
                FROM   [Proccess].[TrolleySetup] ts
                LEFT JOIN [Proccess].[Programas] p  ON p.Numero = ts.Id_Programa
                LEFT JOIN [Proccess].[Ensambles] e  ON e.Id     = p.Ensamble
                WHERE  ts.Id IN @ids
                ORDER  BY e.EnsambleBase ASC, ts.FechaCreacion ASC";

            const string sqlTrolleys = @"
                SELECT e.Equipo_descripcion
                FROM   [Proccess].[Acomodo] a
                INNER JOIN [Proccess].[Programas] p ON a.ProgramaId = p.Id
                INNER JOIN [Proccess].[Equipos]   e ON a.TrolleyId  = e.Id_Equipo
                WHERE  p.Numero = @prog
                ORDER  BY a.Locacion";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                var rows = conn.Query<dynamic>(sqlItems, new { ids = idProcesos }).ToList();

                // Agrupar por ensamble, combinando trolleys de todos los lados
                var grupos = new Dictionary<string, ProcesoItem>();
                foreach (var r in rows)
                {
                    string ens = (string)r.ensamble ?? (string)r.id_Programa;
                    string prog = (string)r.id_Programa;
                    if (!grupos.ContainsKey(ens))
                    {
                        grupos[ens] = new ProcesoItem
                        {
                            id_Proceso        = (int)r.id_Proceso,
                            id_Programa       = prog,
                            ensamble          = ens,
                            workOrder         = (string)r.workOrder,
                            piezasProgramadas = (int)r.piezasProgramadas,
                            trolleys          = new List<string>(),
                            lados             = new List<string>(),
                            idsProcesos       = new List<int>()
                        };
                    }
                    grupos[ens].lados.Add(prog);
                    grupos[ens].idsProcesos.Add((int)r.id_Proceso);

                    // Agregar trolleys de este lado al grupo
                    var tList = conn.Query<string>(sqlTrolleys, new { prog }).ToList();
                    grupos[ens].trolleys.AddRange(tList);
                }

                return grupos.Values.ToList();
            }
        }

        /// <summary>
        /// Cambia el registro a 'Setup' si no hay otro activo en la misma línea.
        /// Devuelve (success, mensaje).
        /// </summary>
        public (bool Success, string Message) IniciarSetup(int idProceso, int linea)
        {
            const string sqlCheck = @"
                SELECT COUNT(*)
                FROM   [Proccess].[TrolleySetup]
                WHERE  Linea  = @linea
                  AND  Status IN ('Setup', 'Arranque')";

            const string sqlUpdate = @"
                UPDATE [Proccess].[TrolleySetup]
                SET    Status = 'Setup'
                WHERE  Id = @id";

            // Find sibling sides: same ensamble, same line, status 'Creado' or 'Pendiente'
            const string sqlSiblings = @"
                SELECT ts2.Id
                FROM   [Proccess].[TrolleySetup] ts1
                INNER JOIN [Proccess].[Programas] p1 ON p1.Numero = ts1.Id_Programa
                INNER JOIN [Proccess].[Programas] p2 ON p2.Ensamble = p1.Ensamble AND p2.Numero <> p1.Numero
                INNER JOIN [Proccess].[TrolleySetup] ts2 ON ts2.Id_Programa = p2.Numero
                WHERE  ts1.Id = @id
                  AND  ts2.Linea = @linea
                  AND  ts2.Status IN ('Creado', 'Pendiente')";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();

                int activos = conn.QueryFirstOrDefault<int>(sqlCheck, new { linea });
                if (activos > 0)
                    return (false, "Ya existe un trabajo en Setup o Arranque en esta línea. Finalícelo primero.");

                conn.Execute(sqlUpdate, new { id = idProceso });

                // Also set sibling sides to Setup
                var siblingIds = conn.Query<int>(sqlSiblings, new { id = idProceso, linea }).ToList();
                foreach (var sibId in siblingIds)
                {
                    conn.Execute(sqlUpdate, new { id = sibId });
                }

                return (true, string.Empty);
            }
        }
    }
}
