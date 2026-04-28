$path = "C:\SnowTrolley\SnowTrolleyProduction\SnowTrolleyProduction\Models\ExcelPreview ItemDto.cs"
$content = @"
using System;
namespace SnowTrolleyProduction.Models
{
    public class ExcelPreviewItemDto
    {
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
    }
}
"@
[System.IO.File]::WriteAllText($path, $content, [System.Text.Encoding]::UTF8)
Write-Output "OK: $([System.IO.File]::Exists($path))"
