namespace SnowTrolleyProduction.Models
{
    using System;
    using System.Collections.Generic;

    public partial class TrolleySetup
    {
        public int Id { get; set; }
        public int Id_Proceso { get; set; }
        public string Id_Programa { get; set; }
        public string WorkOrder { get; set; }
        public int PiezasProgramadas { get; set; }
        public string Trolleys { get; set; }
        public string Status { get; set; }
        public System.DateTime FechaCreacion { get; set; }
        public Nullable<System.DateTime> FechaFinalizacion { get; set; }
        public int Linea { get; set; }
        public Nullable<int> Id_Linea { get; set; }
        public string Comentarios { get; set; }
        public Nullable<int> Orden { get; set; }

        public virtual Linea Linea1 { get; set; }
    }
}
