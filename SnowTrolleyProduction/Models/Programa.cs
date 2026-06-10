namespace SnowTrolleyProduction.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Programa
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Programa()
        {
            this.Acomodoes = new HashSet<Acomodo>();
        }

        public int Id { get; set; }
        public string Numero { get; set; }
        public Nullable<int> Ensamble { get; set; }
        public Nullable<int> CantidadTotalMateriales { get; set; }
        public int CantidadComponentesDiferentes { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Acomodo> Acomodoes { get; set; }
    }
}
