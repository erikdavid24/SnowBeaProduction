using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SHE.SafetyTours.Models
{
    public class TrolleySetupViewModel
    {
        public int Id { get; set; }

        [Required]
        [DisplayName("ID Proceso")]
        public int Id_Proceso { get; set; }

        [DisplayName("Programa")]
        public string Id_Programa { get; set; }

        [Required]
        [DisplayName("Orden de Trabajo (WO)")]
        public string WorkOrder { get; set; }

        [Required]
        [DisplayName("Pzas Programadas")]
        public int PiezasProgramadas { get; set; }

        [Required]
        [DisplayName("Trolleys")]
        public string Trolleys { get; set; }

        [Required]
        [DisplayName("Estatus")]
        public string Status { get; set; }

        [DisplayName("Fecha de Creación")]
        public System.DateTime FechaCreacion { get; set; }

        [DisplayName("Fecha Finalización")]
        public Nullable<System.DateTime> FechaFinalizacion { get; set; }

        [DisplayName("Línea")]
        public Nullable<int> Id_Linea { get; set; }

        [DisplayName("Comentarios")]
        public string Comentarios { get; set; }

    }
}