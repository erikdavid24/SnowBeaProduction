using SHE.SafetyTours.Models;
using SnowTrolleyProduction.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient; // Necesario para SqlParameter
using System.Linq;

namespace SHE.SafetyTours.Models.Service
{
    public class TrolleySetupServices
    {
        BAESystemsGuaymasEntities1 BD;

        public TrolleySetupServices(BAESystemsGuaymasEntities1 BDContext)
        {
            BD = BDContext;
        }

        public List<TrolleySetupViewModel> Read(string startDate, string endDate)
        {
            // OJO: Asegúrate de que el esquema sea [Proccess] o [Process] según como lo creaste en BD
            string sql = @"
                SELECT 
                    Id, Id_Proceso, Id_Programa, WorkOrder, PiezasProgramadas, 
                    Trolleys, Status, FechaCreacion, FechaFinalizacion, Id_Linea, Comentarios
                FROM [Proccess].[TrolleySetup]";

            if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
            {
                // Leer todo sin filtros
                return BD.Database.SqlQuery<TrolleySetupViewModel>(sql).ToList();
            }
            else
            {
                // Leer con filtros de fecha (usamos CAST para ignorar las horas en SQL)
                sql += " WHERE CAST(FechaCreacion AS DATE) >= @start AND CAST(FechaCreacion AS DATE) <= @end";

                DateTime fechaInicio = DateTime.ParseExact(startDate, "yyyy/MM/dd", null);
                DateTime fechaFin = DateTime.ParseExact(endDate, "yyyy/MM/dd", null);

                var paramStart = new SqlParameter("@start", fechaInicio.Date);
                var paramEnd = new SqlParameter("@end", fechaFin.Date);

                return BD.Database.SqlQuery<TrolleySetupViewModel>(sql, paramStart, paramEnd).ToList();
            }
        }

        public void Create(TrolleySetupViewModel model)
        {
            try
            {
                string sql = @"
                    INSERT INTO [Proccess].[TrolleySetup] 
                    (Id_Proceso, Id_Programa, WorkOrder, PiezasProgramadas, Trolleys, Status, FechaCreacion, Id_Linea, Comentarios, Linea)
                    VALUES 
                    (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9)";

                BD.Database.ExecuteSqlCommand(sql,
                    new SqlParameter("@p0", model.Id_Proceso),
                    new SqlParameter("@p1", (object)model.Id_Programa ?? DBNull.Value),
                    new SqlParameter("@p2", (object)model.WorkOrder ?? DBNull.Value),
                    new SqlParameter("@p3", model.PiezasProgramadas),
                    new SqlParameter("@p4", (object)model.Trolleys ?? DBNull.Value),
                    new SqlParameter("@p5", "Abierto"), // Status por default
                    new SqlParameter("@p6", DateTime.Now),
                    new SqlParameter("@p7", (object)model.Id_Linea ?? DBNull.Value),
                    new SqlParameter("@p8", (object)model.Comentarios ?? DBNull.Value),
                    new SqlParameter("@p9", 0) // Campo Linea
                );
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Update(TrolleySetupViewModel model)
        {
            // Nota: En SQL podemos hacer la condición para la FechaFinalizacion usando un CASE
            string sql = @"
                UPDATE [Proccess].[TrolleySetup]
                SET 
                    Id_Proceso = @p0,
                    Id_Programa = @p1,
                    WorkOrder = @p2,
                    PiezasProgramadas = @p3,
                    Trolleys = @p4,
                    Status = @p5,
                    Id_Linea = @p6,
                    Comentarios = @p7,
                    FechaFinalizacion = CASE 
                                            WHEN @p5 = 'Completado' AND FechaFinalizacion IS NULL THEN GETDATE() 
                                            ELSE FechaFinalizacion 
                                        END
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

             public void Delete(TrolleySetupViewModel model)
        {
            string sql = "DELETE FROM [Proccess].[TrolleySetup] WHERE Id = @p0";
            BD.Database.ExecuteSqlCommand(sql, new SqlParameter("@p0", model.Id));
        }
    }
}