namespace SnowTrolleyProduction.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Trolley_Program
    {
        public int Id { get; set; }
        public string Id_Programa { get; set; }
        public Nullable<int> No_linea { get; set; }
        public Nullable<int> No_Cabezal { get; set; }
        public Nullable<int> No_Posicion { get; set; }
        public string No_Trolley { get; set; }
        public Nullable<int> Id_Linea { get; set; }

        public virtual Linea Linea { get; set; }
    }
}
