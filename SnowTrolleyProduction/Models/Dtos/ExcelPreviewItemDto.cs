using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SnowTrolleyProduction.Models
{
    public class ExcelPreviewItemDto
    {
        public int Id { get; set; }
        public string Id_Programa { get; set; }
        public string WorkOrder { get; set; }
        public int PiezasProgramadas { get; set; }
        public string Status { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int? Id_Linea { get; set; }
        public string Comentarios { get; set; }
        public string Ensamble { get; set; }
        public bool EsDuplicado { get; set; }
        public string RazonRechazo { get; set; }
        public bool Pendiente { get; set; }
        public int ExistingId { get; set; }
    }
}