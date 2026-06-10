namespace SnowTrolleyProduction.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Ensamble
    {
        public int Id { get; set; }
        public string EnsambleBase { get; set; }
        public string EnsambleEspecifico { get; set; }
        public Nullable<int> Linea1 { get; set; }
        public Nullable<int> Linea2 { get; set; }

        public virtual Linea Linea { get; set; }
        public virtual Linea Linea3 { get; set; }
    }
}
