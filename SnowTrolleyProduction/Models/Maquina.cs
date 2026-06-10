namespace SnowTrolleyProduction.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Maquina
    {
        public int Id { get; set; }
        public Nullable<int> EquipoId { get; set; }
        public Nullable<int> LineaId { get; set; }
        public Nullable<int> MaquinaId { get; set; }
        public Nullable<int> Cabezales { get; set; }
        public Nullable<int> Zonas { get; set; }

        public virtual Equipos Equipos { get; set; }
        public virtual Linea Linea { get; set; }
    }
}
