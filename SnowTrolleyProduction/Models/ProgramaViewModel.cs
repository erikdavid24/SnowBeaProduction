using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SnowTrolleyProduction.Models
{
    public class ProgramaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El número de programa es obligatorio")]
        [DisplayName("Número de Programa")]
        public string Numero { get; set; }

        [Required(ErrorMessage = "El ensamble es obligatorio")]
        [DisplayName("ID del Ensamble")]
        public int Ensamble { get; set; }
    }
}