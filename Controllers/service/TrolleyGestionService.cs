using Dapper;
using SnowTrolleyProduction.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace SnowTrolleyProduction.Controllers.service
{
    public class TrolleyGestionService
    {
        private readonly string _connStr;

        public TrolleyGestionService(BAESystemsGuaymasEntities ctx)
        {
            _connStr = ctx.Database.Connection.ConnectionString;
        }

        // ?? L�neas SMT 
        public List<LineaGestion> GetLineasSMT()
        {
            const string sql = @"SELECT Id_Linea, Numero_Linea FROM [Proccess].[Lineas] WHERE AreaId = 48 ORDER BY Numero_Linea";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                return conn.Query<LineaGestion>(sql).ToList();
            }
        }

        // ?? Maquinas 
        public List<MaquinaGestion> GetMaquinas()
        {
            const string sql = @"
                SELECT m.Id, e.Id_Equipo AS IdEquipo, e.Equipo_descripcion AS EquipoDescripcion, l.Numero_Linea
                FROM [Proccess].[Maquinas] m
                JOIN [Proccess].[Equipos] e ON m.EquipoId = e.Id_Equipo
                JOIN [Proccess].[Lineas] l ON e.LineaId = l.Id_Linea
                ORDER BY e.Equipo_descripcion ASC";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                return conn.Query<dynamic>(sql).Select(r => new MaquinaGestion
                {
                    Id = r.Id,
                    Equipo = new EquipoGestion
                    {
                        IdEquipo = r.IdEquipo,
                        EquipoDescripcion = r.EquipoDescripcion,
                        NumeroLinea = r.Numero_Linea
                    }
                }).ToList();
            }
        }

        public MaquinaGestion GetMaquina(int id)
        {
            const string sql = @"
                SELECT m.Id, l.Id_Linea, l.Numero_Linea, e.Id_Equipo AS IdEquipo, e.Equipo_descripcion AS EquipoDescripcion
                FROM [Proccess].[Maquinas] m
                JOIN [Proccess].[Lineas] l ON m.LineaId = l.Id_Linea
                JOIN [Proccess].[Equipos] e ON m.EquipoId = e.Id_Equipo
                WHERE m.Id = @id";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                var r = conn.QueryFirstOrDefault<dynamic>(sql, new { id });
                if (r == null) return null;
                return new MaquinaGestion
                {
                    Id = r.Id,
                    Linea = new LineaGestion { Id_Linea = r.Id_Linea, Numero_Linea = r.Numero_Linea },
                    Equipo = new EquipoGestion { IdEquipo = r.IdEquipo }
                };
            }
        }

        public void AgregarMaquina(int equipoId, int lineaId)
        {
            const string sql = "INSERT INTO [Proccess].[Maquinas] (EquipoId, LineaId) VALUES (@equipoId, @lineaId)";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                conn.Execute(sql, new { equipoId, lineaId });
            }
        }

        public void EditarMaquina(int maquinaId, int equipoId)
        {
            const string sql = "UPDATE [Proccess].[Maquinas] SET EquipoId = @equipoId WHERE Id = @maquinaId";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                conn.Execute(sql, new { maquinaId, equipoId });
            }
        }

        public string EliminarMaquina(int maquinaId)
        {
            try
            {
                const string sql = "DELETE FROM [Proccess].[Maquinas] WHERE Id = @maquinaId";
                using (var conn = new SqlConnection(_connStr))
                {
                    conn.Open();
                    conn.Execute(sql, new { maquinaId });
                }
                return null;
            }
            catch (SqlException ex)
            {
                return ex.Number == 547
                    ? "No se puede eliminar esta m�quina porque tiene acomodos vinculados."
                    : "Ocurri� un error al intentar eliminar esta m�quina.";
            }
        }

        // ?? Equipo
        public List<EquipoGestion> GetEquiposPorLinea(int lineaId)
        {
            const string sql = @"
                SELECT Id_Equipo AS IdEquipo, Equipo_descripcion AS EquipoDescripcion
                FROM [Proccess].[Equipos]
                WHERE AreaId = 48 AND LineaId = @lineaId";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                return conn.Query<EquipoGestion>(sql, new { lineaId }).ToList();
            }
        }

        // ?? M�quinas
        public List<MaquinaGestion> GetMaquinasPorLinea(int lineaId)
        {
            const string sql = @"
                SELECT m.Id, e.Id_Equipo AS IdEquipo, e.Equipo_descripcion AS EquipoDescripcion
                FROM [Proccess].[Maquinas] m
                JOIN [Proccess].[Equipos] e ON m.EquipoId = e.Id_Equipo
                WHERE e.LineaId = @lineaId
                ORDER BY e.Equipo_descripcion";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                return conn.Query<dynamic>(sql, new { lineaId }).Select(r => new MaquinaGestion
                {
                    Id = r.Id,
                    Equipo = new EquipoGestion { IdEquipo = r.IdEquipo, EquipoDescripcion = r.EquipoDescripcion }
                }).ToList();
            }
        }

        // ?? Ensambles 
        public List<EnsambleGestion> GetEnsambles(string numero = "")
        {
            const string sql = @"
                SELECT e.Id, e.EnsambleBase AS Numero, l.Id_Linea, l.Numero_Linea
                FROM [Proccess].[Ensambles] e
                JOIN [Proccess].[Lineas] l ON e.Linea1 = l.Id_Linea
                WHERE e.EnsambleBase IS NOT NULL AND e.EnsambleBase <> ''
                  AND e.EnsambleBase LIKE @filtro";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                return conn.Query<dynamic>(sql, new { filtro = "%" + (numero ?? "") + "%" }).Select(r => new EnsambleGestion
                {
                    Id = r.Id,
                    Numero = r.Numero,
                    Linea = new LineaGestion { Id_Linea = r.Id_Linea, Numero_Linea = r.Numero_Linea }
                }).ToList();
            }
        }

        public List<EnsambleGestion> GetAllEnsambles()
        {
            const string sql = "SELECT Id, EnsambleBase AS Numero FROM [Proccess].[Ensambles] WHERE EnsambleBase IS NOT NULL AND EnsambleBase <> ''";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                return conn.Query<EnsambleGestion>(sql).ToList();
            }
        }

        public EnsambleGestion GetEnsamble(int id)
        {
            const string sql = @"
                SELECT e.Id, e.EnsambleBase AS Numero, l.Id_Linea, l.Numero_Linea
                FROM [Proccess].[Ensambles] e
                JOIN [Proccess].[Lineas] l ON e.Linea1 = l.Id_Linea
                WHERE e.Id = @id";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                var r = conn.QueryFirstOrDefault<dynamic>(sql, new { id });
                if (r == null) return null;
                return new EnsambleGestion
                {
                    Id = r.Id,
                    Numero = r.Numero,
                    Linea = new LineaGestion { Id_Linea = r.Id_Linea, Numero_Linea = r.Numero_Linea }
                };
            }
        }

        public void AgregarEnsamble(string numero, int lineaId)
        {
            const string sql = "INSERT INTO [Proccess].[Ensambles] (EnsambleBase, Linea1) VALUES (@numero, @lineaId)";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                conn.Execute(sql, new { numero, lineaId });
            }
        }

        public void EditarEnsamble(int ensambleId, string numero, int lineaId)
        {
            const string sql = "UPDATE [Proccess].[Ensambles] SET EnsambleBase = @numero, Linea1 = @lineaId WHERE Id = @ensambleId";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                conn.Execute(sql, new { ensambleId, numero, lineaId });
            }
        }

        public void EliminarEnsamble(int ensambleId)
        {
            const string sql = "DELETE FROM [Proccess].[Ensambles] WHERE Id = @ensambleId";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                conn.Execute(sql, new { ensambleId });
            }
        }

        public List<ProgramaGestion> GetProgramasTable(string numero = "")
        {
            const string sql = @"
                SELECT p.Id, p.Numero, e.Id AS EnsambleId, e.EnsambleBase AS EnsambleNumero, l.Id_Linea, l.Numero_Linea
                FROM [Proccess].[Programas] p
                JOIN [Proccess].[Ensambles] e ON p.Ensamble = e.Id
                LEFT JOIN [Proccess].[Lineas] l ON e.Linea1 = l.Id_Linea
                WHERE p.Numero IS NOT NULL AND p.Numero <> ''
                  AND p.Numero LIKE @filtro
                ORDER BY l.Numero_Linea ASC";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                return conn.Query<dynamic>(sql, new { filtro = "%" + (numero ?? "") + "%" }).Select(r => new ProgramaGestion
                {
                    Id = r.Id,
                    Numero = r.Numero,
                    Ensamble = new EnsambleGestion
                    {
                        Id = r.EnsambleId,
                        Numero = r.EnsambleNumero,
                        Linea = new LineaGestion { Id_Linea = r.Id_Linea, Numero_Linea = r.Numero_Linea }
                    }
                }).ToList();
            }
        }

        public ProgramaGestion GetPrograma(int id)
        {
            const string sql = "SELECT Id, Numero FROM [Proccess].[Programas] WHERE Id = @id";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                return conn.QueryFirstOrDefault<ProgramaGestion>(sql, new { id });
            }
        }

        public void AgregarPrograma(string numero, int ensambleId)
        {
            const string sql = "INSERT INTO [Proccess].[Programas] (Numero, Ensamble) VALUES (@numero, @ensambleId)";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                conn.Execute(sql, new { numero, ensambleId });
            }
        }

        public void EditarPrograma(int programaId, int ensambleId, string numero)
        {
            const string sql = "UPDATE [Proccess].[Programas] SET Ensamble = @ensambleId, Numero = @numero WHERE Id = @programaId";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                conn.Execute(sql, new { programaId, ensambleId, numero });
            }
        }

        public void EliminarPrograma(int programaId)
        {
            const string sql = "DELETE FROM [Proccess].[Programas] WHERE Id = @programaId";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                conn.Execute(sql, new { programaId });
            }
        }

        // ?? Acomodos 
        public List<AcomodosGroupViewModel> GetAcomodos()
        {
            const string sql = @"
                SELECT
                    a.Id AS AcomodoId, et.EnsambleBase AS EnsambleNumero, a.ProgramaId, a.TrolleyId, a.Locacion, a.MaquinaId,
                    p.Id AS PId, p.Numero, p.Ensamble,
                    e.Equipo_descripcion AS TrolleyNombre,
                    e2.Equipo_descripcion AS MaquinaNombre,
                    l.Numero_Linea
                FROM [Proccess].[Acomodo] a
                LEFT JOIN [Proccess].[Equipos] e ON a.TrolleyId = e.Id_Equipo
                LEFT JOIN [Proccess].[Maquinas] m ON a.MaquinaId = m.Id
                LEFT JOIN [Proccess].[Equipos] e2 ON m.EquipoId = e2.Id_Equipo
                LEFT JOIN [Proccess].[Programas] p ON a.ProgramaId = p.Id
                LEFT JOIN [Proccess].[Ensambles] et ON p.Ensamble = et.Id
                JOIN [Proccess].[Lineas] l ON et.Linea1 = l.Id_Linea
                ORDER BY p.Numero, MaquinaNombre, Locacion ASC";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                var rows = conn.Query<dynamic>(sql).ToList();

                var result = new List<AcomodosGroupViewModel>();
                int ensambleAnterior = -1;
                int programaAnterior = -1;

                foreach (var row in rows)
                {
                    int ensambleActual = (int)row.Ensamble;
                    int programaActual = (int)row.ProgramaId;

                    var acomodo = new AcomodoGestion
                    {
                        Id = (int)row.AcomodoId,
                        Trolley = new EquipoGestion { IdEquipo = (int)row.TrolleyId, EquipoDescripcion = (string)row.TrolleyNombre },
                        Locacion = (int)row.Locacion,
                        Maquina = new MaquinaGestion
                        {
                            Id = (int)row.MaquinaId,
                            Equipo = new EquipoGestion { EquipoDescripcion = (string)row.MaquinaNombre }
                        }
                    };

                    if (ensambleAnterior != ensambleActual)
                    {
                        result.Add(new AcomodosGroupViewModel
                        {
                            Ensamble = new EnsambleGestion
                            {
                                Id = ensambleActual,
                                Numero = (string)row.EnsambleNumero,
                                Linea = new LineaGestion { Numero_Linea = (int)row.Numero_Linea }
                            },
                            Programas = new List<ProgramaGestion>
                            {
                                new ProgramaGestion
                                {
                                    Id = programaActual,
                                    Numero = (string)row.Numero,
                                    Acomodos = new List<AcomodoGestion> { acomodo }
                                }
                            }
                        });
                    }
                    else if (programaAnterior != programaActual)
                    {
                        result[result.Count - 1].Programas.Add(new ProgramaGestion
                        {
                            Id = programaActual,
                            Numero = (string)row.Numero,
                            Acomodos = new List<AcomodoGestion> { acomodo }
                        });
                    }
                    else
                    {
                        var progs = result[result.Count - 1].Programas;
                        progs[progs.Count - 1].Acomodos.Add(acomodo);
                    }

                    ensambleAnterior = ensambleActual;
                    programaAnterior = programaActual;
                }

                return result;
            }
        }

        public AcomodoGestion GetAcomodo(int acomodoId)
        {
            const string sql = @"
                SELECT
                    l.Id_Linea, l.Numero_Linea,
                    a.Id AS AcomodoId, p.Id AS ProgramaId, p.Numero,
                    a.MaquinaId, e2.Equipo_descripcion AS MaquinaNombre,
                    a.TrolleyId, e.Equipo_descripcion AS TrolleyNombre,
                    a.Locacion
                FROM [Proccess].[Acomodo] a
                JOIN [Proccess].[Programas] p ON a.ProgramaId = p.Id
                JOIN [Proccess].[Maquinas] m ON a.MaquinaId = m.Id
                JOIN [Proccess].[Lineas] l ON m.LineaId = l.Id_Linea
                JOIN [Proccess].[Equipos] e2 ON m.EquipoId = e2.Id_Equipo
                JOIN [Proccess].[Equipos] e ON a.TrolleyId = e.Id_Equipo
                WHERE a.Id = @acomodoId";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                var r = conn.QueryFirstOrDefault<dynamic>(sql, new { acomodoId });
                if (r == null) return null;
                return new AcomodoGestion
                {
                    Id = (int)r.AcomodoId,
                    Programa = new ProgramaGestion { Id = (int)r.ProgramaId, Numero = (string)r.Numero },
                    Maquina = new MaquinaGestion
                    {
                        Id = (int)r.MaquinaId,
                        Equipo = new EquipoGestion { EquipoDescripcion = (string)r.MaquinaNombre },
                        Linea = new LineaGestion { Id_Linea = (int)r.Id_Linea, Numero_Linea = (int)r.Numero_Linea }
                    },
                    Trolley = new EquipoGestion { IdEquipo = (int)r.TrolleyId, EquipoDescripcion = (string)r.TrolleyNombre },
                    Locacion = (int)r.Locacion
                };
            }
        }

        public void GuardarAcomodo(AcomodoFormViewModel vm)
        {
            const string sqlIns = "INSERT INTO [Proccess].[Acomodo] (ProgramaId, TrolleyId, Locacion, MaquinaId) VALUES (@prog, @trolley, @loc, @maq)";

            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                conn.Execute("DELETE FROM [Proccess].[Acomodo] WHERE ProgramaId = @ProgramaId", new { vm.ProgramaId });

                // Maquina 1
                if (vm.MaquinaId > 0)
                {
                    var z1 = new Dictionary<int, int?> { {10,vm.Z10},{20,vm.Z20},{30,vm.Z30},{40,vm.Z40},{50,vm.Z50},{60,vm.Z60},{70,vm.Z70},{80,vm.Z80},{90,vm.Z90} };
                    foreach (var kvp in z1)
                        if (kvp.Value.HasValue && kvp.Value > 0)
                            conn.Execute(sqlIns, new { prog = vm.ProgramaId, trolley = kvp.Value.Value, loc = kvp.Key, maq = vm.MaquinaId });
                }

                // Maquina 2
                if (vm.MaquinaId2 > 0)
                {
                    var z2 = new Dictionary<int, int?> { {10,vm.Z10_2},{20,vm.Z20_2},{30,vm.Z30_2},{40,vm.Z40_2},{50,vm.Z50_2},{60,vm.Z60_2},{70,vm.Z70_2},{80,vm.Z80_2},{90,vm.Z90_2} };
                    foreach (var kvp in z2)
                        if (kvp.Value.HasValue && kvp.Value > 0)
                            conn.Execute(sqlIns, new { prog = vm.ProgramaId, trolley = kvp.Value.Value, loc = kvp.Key, maq = vm.MaquinaId2 });
                }
            }
        }

        public void EditarAcomodo(int acomodoId, int programaId, int trolleyId, int locacion, int maquinaId)
        {
            const string sql = "UPDATE [Proccess].[Acomodo] SET ProgramaId = @programaId, TrolleyId = @trolleyId, MaquinaId = @maquinaId, Locacion = @locacion WHERE Id = @acomodoId";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                conn.Execute(sql, new { acomodoId, programaId, trolleyId, maquinaId, locacion });
            }
        }

        public void EliminarAcomodo(int acomodoId)
        {
            const string sql = "DELETE FROM [Proccess].[Acomodo] WHERE Id = @acomodoId";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                conn.Execute(sql, new { acomodoId });
            }
        }

        // ?? Trolleys por l�nea (para acomodo modal) 
        public List<EquipoGestion> GetTrolleysPorLinea(int lineaId)
        {
            const string sql = @"
                SELECT Id_Equipo AS IdEquipo, Equipo_descripcion AS EquipoDescripcion
                FROM [Proccess].[Equipos]
                WHERE (Equipo_descripcion LIKE 'A%' OR Equipo_descripcion LIKE 'B%' OR Equipo_descripcion LIKE 'C%')
                  AND LEN(Equipo_descripcion) = 3
                  AND LineaId = @lineaId
                   OR Equipo_descripcion = 'MANUAL'
                   OR Equipo_descripcion = 'MATRIX'";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                return conn.Query<EquipoGestion>(sql, new { lineaId }).ToList();
            }
        }

        // ?? Programas por l�nea (para acomodo modal) 
        public List<ProgramaGestion> GetProgramasPorLinea(int lineaId)
        {
            const string sql = @"
                SELECT p.Id, p.Numero
                FROM [Proccess].[Programas] p
                JOIN [Proccess].[Ensambles] e ON p.Ensamble = e.Id
                WHERE e.Linea1 = @lineaId
                  AND p.Numero IS NOT NULL AND p.Numero <> ''
                ORDER BY p.Numero";
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Open();
                return conn.Query<ProgramaGestion>(sql, new { lineaId }).ToList();
            }
        }
    }
}
