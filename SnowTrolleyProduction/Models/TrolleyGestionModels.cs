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

    public class TrolleyGestionIndexViewModel
    {
        public List<MaquinaGestion>         Maquinas  { get; set; }
        public List<EnsambleGestion>        Ensambles { get; set; }
        public List<ProgramaGestion>        Programas { get; set; }
        public List<AcomodosGroupViewModel> Acomodos  { get; set; }
    }

    public class AcomodoFormViewModel
    {
        public int ProgramaId { get; set; }
        // Maquina 1
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
        // Maquina 2
        public int MaquinaId2 { get; set; }
        public int? Z10_2 { get; set; }
        public int? Z20_2 { get; set; }
        public int? Z30_2 { get; set; }
        public int? Z40_2 { get; set; }
        public int? Z50_2 { get; set; }
        public int? Z60_2 { get; set; }
        public int? Z70_2 { get; set; }
        public int? Z80_2 { get; set; }
        public int? Z90_2 { get; set; }
    }

    public class MaquinaGridRow {
        public int Id { get; set; }
        public string Maquina { get; set; }
        public int? Linea { get; set; }
    }

    public class EnsambleGridRow {
        public int Id { get; set; }
        public string Numero { get; set; }
        public int? Linea { get; set; }
    }

    public class ProgramaGridRow {
        public int Id { get; set; }
        public string Numero { get; set; }
        public string Ensamble { get; set; }
        public int? Linea { get; set; }
    }

    public class AcomodoGridRow {
        public int EnsambleId { get; set; }
        public string Ensamble { get; set; }
        public int? Linea { get; set; }
    }
}
