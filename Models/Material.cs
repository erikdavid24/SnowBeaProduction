using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnowTrolleyProduction.Models
{
    [Table("Materiales", Schema = "Proccess")]
    public class Material
    {
        [Key]
        public int Id { get; set; }

        public string NumeroMaterial { get; set; }

        public int ProgramaId { get; set; }

        [ForeignKey("ProgramaId")]
        public virtual Programa Programa { get; set; }
    }
}
