using System;
using System.Linq;
using ClosedXML.Excel;
using SnowTrolleyProduction.Models;

namespace SnowTrolleyProduction.Models.Service
{
    public class ProgramaServices
    {

        BAESystemsGuaymasEntitiesSmtPlan BD = new BAESystemsGuaymasEntitiesSmtPlan();

        public ProgramaServices(BAESystemsGuaymasEntitiesSmtPlan BDContext)
        {
            BD = BDContext;
        }

        public void Create(ProgramaViewModel model)
        {

            BD.Database.ExecuteSqlCommand(
                "INSERT INTO [Process].[Programas] (Numero, Ensamble) VALUES (@p0, @p1)",
                new System.Data.SqlClient.SqlParameter("@p0", model.Numero),
                new System.Data.SqlClient.SqlParameter("@p1", model.Ensamble)
            );
        }

    }
}
