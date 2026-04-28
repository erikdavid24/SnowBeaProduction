@echo off
set TARGET=C:\SnowTrolley\SnowTrolleyProduction\SnowTrolleyProduction\Models\ExcelPreview ItemDto.cs
(
echo using System;
echo namespace SnowTrolleyProduction.Models
echo {
echo     public class ExcelPreviewItemDto
echo     {
echo         public string Id_Programa { get; set; }
echo         public string WorkOrder { get; set; }
echo         public int PiezasProgramadas { get; set; }
echo         public string Status { get; set; }
echo         public DateTime FechaCreacion { get; set; }
echo         public int? Id_Linea { get; set; }
echo         public string Comentarios { get; set; }
echo         public string Ensamble { get; set; }
echo         public bool EsDuplicado { get; set; }
echo         public string RazonRechazo { get; set; }
echo     }
echo }
) > "%TARGET%"
echo done
