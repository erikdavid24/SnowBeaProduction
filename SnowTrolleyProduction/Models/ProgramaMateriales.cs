using System.Collections.Generic;

namespace SnowTrolleyProduction.Models
{
    public partial class Programa
    {
        public virtual ICollection<Material> Materiales { get; set; }
    }
}
