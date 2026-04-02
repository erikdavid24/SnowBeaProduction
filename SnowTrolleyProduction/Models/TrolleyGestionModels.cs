using System.Collections.Generic;

namespace SnowTrolleyProduction.Models
{
    public class LineaGestion
    {
        public int Id_Linea { get; set; }
        public int Numero_Linea { get; set; }
    }

    public class EquipoGestion
    {
        public int IdEquipo { get; set; }
        public string EquipoDescripcion { get; set; }
        public int? NumeroLinea { get; set; }
    }

    public class MaquinaGestion
    {
        public int Id { get; set; }
        public LineaGestion Linea { get; set; }
        public EquipoGestion Equipo { get; set; }
    }

    public class EnsambleGestion
    {
        public int Id { get; set; }
        public string Numero { get; set; }
        public LineaGestion Linea { get; set; }
    }

    public class ProgramaGestion
    {
        public int Id { get; set; }
        public string Numero { get; set; }
        public int? EnsambleId { get; set; }
        public EnsambleGestion Ensamble { get; set; }
        public List<AcomodoGestion> Acomodos { get; set; }
    }

    public class AcomodoGestion
    {
        public int Id { get; set; }
        public int Locacion { get; set; }
        public int? ProgramaId { get; set; }
        public ProgramaGestion Programa { get; set; }
        public int? TrolleyId { get; set; }
        public EquipoGestion Trolley { get; set; }
        public MaquinaGestion Maquina { get; set; }
    }

    public class AcomodosGroupViewModel
    {
        public EnsambleGestion Ensamble { get; set; }
        public List<ProgramaGestion> Programas { get; set; }
    }

    public class AcomodoFormViewModel
    {
        public int ProgramaId { get; set; }
        public int MaquinaId { get; set; }
        public int? Z10 { get; set; }
        public int? Z20 { get; set; }
        public int? Z30 { get; set; }
        public int? Z40 { get; set; }
        public int? Z50 { get; set; }
        public int? Z60 { get; set; }
        public int? Z70 { get; set; }
        public int? Z80 { get; set; }
        public int? Z90 { get; set; }
    }
}
