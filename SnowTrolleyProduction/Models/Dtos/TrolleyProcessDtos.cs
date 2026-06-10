using System;
using System.Collections.Generic;

namespace SnowTrolleyProduction.Models.Dtos
{
    public class ProcesoItem
    {
        public int          id_Proceso        { get; set; }
        public string       id_Programa       { get; set; }
        public string       ensamble          { get; set; }
        public string       workOrder         { get; set; }
        public int          piezasProgramadas { get; set; }
        public List<string> trolleys          { get; set; }

        public List<string> lados             { get; set; }

        public List<int>    idsProcesos       { get; set; }
    }

    public class ArranqueGridRow
    {
        public string id_Programa { get; set; }
        public string workOrder   { get; set; }
        public string badgesHtml  { get; set; }
        public string statusHtml  { get; set; }
    }

    public class ProcesoGridRow
    {
        public int    id_Proceso  { get; set; }
        public string id_Programa { get; set; }
        public string workOrder   { get; set; }
        public string badgesHtml  { get; set; }
        public string accionHtml  { get; set; }
    }

    public class ProcesoArranque
    {
        public int          id_Proceso    { get; set; }
        public int          linea         { get; set; }
        public string       id_Programa   { get; set; }
        public string       workOrder     { get; set; }
        public string       trolleys      { get; set; }
        public List<string> trolleysList  { get; set; }
        public DateTime     fechaCreacion { get; set; }
        public string       status        { get; set; }
    }
}
