namespace SnowTrolleyProduction.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Acomodo
    {
        public int Id { get; set; }
        public Nullable<int> ProgramaId { get; set; }
        public Nullable<int> TrolleyId { get; set; }
        public Nullable<int> Locacion { get; set; }
        public Nullable<int> MaquinaId { get; set; }

        public virtual Programa Programa { get; set; }
        public virtual Equipos Equipos { get; set; }
    }
}
