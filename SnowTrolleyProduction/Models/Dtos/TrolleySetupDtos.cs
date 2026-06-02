using System;
using System.Collections.Generic;

namespace SnowTrolleyProduction.Models.Dtos
{
    public class SupervisorDto
    {
        public int      Id             { get; set; }
        public string   EmployeeNumber { get; set; }
        public string   FullName       { get; set; }
        public DateTime FechaAlta      { get; set; }
    }

    public class TrolleyPosition
    {
        public string noTrolley     { get; set; }
        public int    noCabezal     { get; set; }
        public int    noPosicion    { get; set; }
        public string nombreMaquina { get; set; }
    }

    public class TrolleyItemDto
    {
        public int    Id          { get; set; }
        public string Descripcion { get; set; }
    }

    public class CabezalAcomodoDto
    {
        public int                     Cabezal   { get; set; }
        public int                     MaquinaId { get; set; }
        public string                  MaquinaNombre { get; set; }
        public Dictionary<string, int> Acomodo   { get; set; }
    }

    public class TrolleySetupDataDto
    {
        public List<TrolleyItemDto>      Trolleys   { get; set; }
        public List<TrolleyItemDto>      Maquinas   { get; set; }
        public List<CabezalAcomodoDto>   Cabezales  { get; set; }

        // Backwards compat: flat acomodo for single-machine case
        public Dictionary<string, int>   Acomodo    { get; set; }
        public int                       MaquinaId  { get; set; }
    }

    public class SetupDatosDto
    {
        public string                programaSeleccionado { get; set; }
        public int                   piezasProgramadas    { get; set; }
        public int                   idProceso            { get; set; }
        public int                   pzaTerminadas        { get; set; }
        public string                status               { get; set; }
        public List<TrolleyPosition> trolleyPositions     { get; set; }
    }
}
