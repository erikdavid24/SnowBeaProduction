namespace SnowTrolleyProduction.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Works_Backup
    {
        public int Id_Proceso { get; set; }
        public Nullable<System.DateTime> Fecha_inicio { get; set; }
        public string Id_Ensamble { get; set; }
        public string Id_Programa { get; set; }
        public string ID_WorkOrder { get; set; }
        public string Status { get; set; }
        public Nullable<int> Linea { get; set; }
        public Nullable<int> Orden { get; set; }
        public Nullable<System.DateTime> Fecha_setup { get; set; }
        public Nullable<System.DateTime> Fecha_arranque { get; set; }
        public Nullable<System.DateTime> fecha_cierre { get; set; }
        public Nullable<int> Pzas_programadas { get; set; }
        public Nullable<int> Pza_terminadas { get; set; }
        public string Comentarios { get; set; }
        public Nullable<System.DateTime> Fecha_Proyectada { get; set; }
        public Nullable<int> Id_Linea { get; set; }
    }
}
