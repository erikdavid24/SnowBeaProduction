using Dapper;
using SnowTrolleyProduction.Models;
using SnowTrolleyProduction.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace SnowTrolleyProduction.Controllers.service
{

    public class TrolleyProcessService
    {
        private readonly string _connStr;

        public TrolleyProcessService(BAESystemsGuaymasEntitiesSmtPlan ctx)
        {
            _connStr = ctx.Database.Connection.ConnectionString;
        }

        public List<SelectItemDto> GetLineas()
        {
            const string sql = @"
                SELECT DISTINCT l.Numero_Linea
                FROM [Process].[Lineas] l
                INNER JOIN [Process].[Ensambles] e ON e.Linea1 = l.Id_Linea
                WHERE e.EnsambleBase IS NOT NULL AND e.EnsambleBase <> ''
                ORDER BY l.Numero_Linea ASC";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                return conn.Query<int>(sql)
                           .Select(n => new SelectItemDto { Text = "Linea " + n, Value = n.ToString() })
                           .ToList();
            }
        }

        public List<ProcesoItem> GetProcesosDisponibles(int linea)
        {
            const string sql = @"
                SELECT
                    ts.Id               AS id_Proceso,
                    ts.Id_Programa      AS id_Programa,
                    ts.WorkOrder        AS workOrder,
                    ts.PiezasProgramadas AS piezasProgramadas,
                    ISNULL(e.EnsambleBase, ts.Id_Programa) AS ensamble
                FROM   [Process].[TrolleySetup]  ts
                LEFT JOIN [Process].[Programas]  p  ON p.Numero  = ts.Id_Programa
                LEFT JOIN [Process].[Ensambles]  e  ON e.Id      = p.Ensamble
                WHERE  ts.Linea   = @linea
                  AND  ts.Status IN ('Creado', 'Pendiente')
                ORDER  BY ISNULL(ts.Orden, 999999) ASC, ts.FechaCreacion ASC";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                var rows = conn.Query<dynamic>(sql, new { linea }).ToList();

                return rows.Select(r => new ProcesoItem
                {
                    id_Proceso        = (int)r.id_Proceso,
                    id_Programa       = (string)r.id_Programa,
                    ensamble          = (string)r.ensamble ?? (string)r.id_Programa,
                    workOrder         = (string)r.workOrder,
                    piezasProgramadas = (int)r.piezasProgramadas,
                    trolleys          = new List<string>(),
                    lados             = new List<string>(),
                    idsProcesos       = new List<int>()
                }).ToList();
            }
        }

        public List<ProcesoArranque> GetTrabajosEnProceso(int linea)
        {
            const string sqlWorks = @"
                SELECT ts.Id AS id_Proceso, ts.Linea AS linea, ts.Id_Programa AS id_Programa,
                       ts.WorkOrder AS workOrder, ts.Status AS status, ts.FechaCreacion AS fechaCreacion
                FROM   [Process].[TrolleySetup] ts
                WHERE  ts.Linea   = @linea
                  AND  ts.Status IN ('Setup', 'Arranque')
                ORDER  BY ts.FechaCreacion ASC";

            const string sqlTrolleys = @"
                SELECT e.Equipo_descripcion
                FROM   [Process].[Acomodo] a
                INNER JOIN [Process].[Programas] p ON a.ProgramaId = p.Id
                INNER JOIN [Process].[Equipos]   e ON a.TrolleyId  = e.Id_Equipo
                WHERE  p.Numero = @prog
                ORDER  BY a.Locacion";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                var rows = conn.Query<dynamic>(sqlWorks, new { linea }).ToList();
                var trabajos = new List<ProcesoArranque>();

                foreach (var r in rows)
                {
                    var prog = (string)r.id_Programa;
                    var trolleyList = conn.Query<string>(sqlTrolleys, new { prog }).ToList();
                    trabajos.Add(new ProcesoArranque
                    {
                        id_Proceso    = (int)r.id_Proceso,
                        linea         = (int)r.linea,
                        id_Programa   = prog,
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

        public List<ProcesoItem> EnriquecerConTrolleys(List<ProcesoItem> procesos)
        {
            if (procesos == null || procesos.Count == 0)
                return procesos ?? new List<ProcesoItem>();

            const string sql = @"
                SELECT e.Equipo_descripcion
                FROM   [Process].[Acomodo] a
                INNER JOIN [Process].[Programas] p ON a.ProgramaId = p.Id
                INNER JOIN [Process].[Equipos]   e ON a.TrolleyId  = e.Id_Equipo
                WHERE  p.Numero = @prog
                ORDER  BY a.Locacion";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                foreach (var p in procesos)
                {
                    p.trolleys = new List<string>();
                    var programas = (p.lados != null && p.lados.Count > 0) ? p.lados : new List<string> { p.id_Programa };
                    foreach (var prog in programas)
                    {
                        var tList = conn.Query<string>(sql, new { prog }).ToList();
                        p.trolleys.AddRange(tList);
                    }
                }
            }

            return procesos;
        }

        public List<ProcesoItem> GetProcesosConTrolleys(List<int> idProcesos)
        {
            const string sqlItems = @"
                SELECT
                    ts.Id               AS id_Proceso,
                    ts.Id_Programa      AS id_Programa,
                    ts.WorkOrder        AS workOrder,
                    ts.PiezasProgramadas AS piezasProgramadas,
                    ISNULL(e.EnsambleBase, ts.Id_Programa) AS ensamble
                FROM   [Process].[TrolleySetup] ts
                LEFT JOIN [Process].[Programas] p  ON p.Numero = ts.Id_Programa
                LEFT JOIN [Process].[Ensambles] e  ON e.Id     = p.Ensamble
                WHERE  ts.Id IN @ids
                ORDER  BY ISNULL(ts.Orden, 999999) ASC, ts.FechaCreacion ASC";

            const string sqlTrolleys = @"
                SELECT e.Equipo_descripcion
                FROM   [Process].[Acomodo] a
                INNER JOIN [Process].[Programas] p ON a.ProgramaId = p.Id
                INNER JOIN [Process].[Equipos]   e ON a.TrolleyId  = e.Id_Equipo
                WHERE  p.Numero = @prog
                ORDER  BY a.Locacion";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                var rows = conn.Query<dynamic>(sqlItems, new { ids = idProcesos }).ToList();

                var result = new List<ProcesoItem>();
                foreach (var r in rows)
                {
                    string prog = (string)r.id_Programa;
                    var tList = conn.Query<string>(sqlTrolleys, new { prog }).ToList();
                    result.Add(new ProcesoItem
                    {
                        id_Proceso        = (int)r.id_Proceso,
                        id_Programa       = prog,
                        ensamble          = (string)r.ensamble ?? prog,
                        workOrder         = (string)r.workOrder,
                        piezasProgramadas = (int)r.piezasProgramadas,
                        trolleys          = tList,
                        lados             = new List<string>(),
                        idsProcesos       = new List<int>()
                    });
                }

                return result;
            }
        }

        public void GuardarOrden(List<int> idProcesos)
        {
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                for (int i = 0; i < idProcesos.Count; i++)
                {
                    conn.Execute(
                        "UPDATE [Process].[TrolleySetup] SET Orden = @orden WHERE Id = @id",
                        new { orden = i + 1, id = idProcesos[i] });
                }
            }
        }

        public (bool Success, string Message) IniciarSetup(int idProceso, int linea)
        {
            const string sqlCheck = @"
                SELECT COUNT(*)
                FROM   [Process].[TrolleySetup]
                WHERE  Linea  = @linea
                  AND  Status IN ('Setup', 'Arranque')";

            const string sqlUpdate = @"
                UPDATE [Process].[TrolleySetup]
                SET    Status = 'Setup'
                WHERE  Id = @id";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();

                int activos = conn.QueryFirstOrDefault<int>(sqlCheck, new { linea });
                if (activos > 0)
                    return (false, "Ya existe un trabajo en Setup o Arranque en esta linea. Finalacielo primero.");

                conn.Execute(sqlUpdate, new { id = idProceso });

                return (true, string.Empty);
            }
        }
    }
}
