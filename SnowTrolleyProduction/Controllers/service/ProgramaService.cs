using System;
using System.Linq;
using ClosedXML.Excel;
using SnowTrolleyProduction.Models; 

namespace SnowTrolleyProduction.Models.Service
{
    public class ProgramaServices
    {
        // Usa el nombre correcto de tu contexto de BD
        BAESystemsGuaymasEntities BD = new BAESystemsGuaymasEntities();

        public ProgramaServices(BAESystemsGuaymasEntities BDContext)
        {
            BD = BDContext;
        }

       

        public void Create(ProgramaViewModel model)
        {
            // Insertamos en la tabla usando Raw SQL para mayor velocidad
            BD.Database.ExecuteSqlCommand(
                "INSERT INTO [Proccess].[Programas] (Numero, Ensamble) VALUES (@p0, @p1)",
                new System.Data.SqlClient.SqlParameter("@p0", model.Numero),
                new System.Data.SqlClient.SqlParameter("@p1", model.Ensamble)
            );
        }

       
    }
}