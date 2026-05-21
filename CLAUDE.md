# 🛠️ MEGA-REGLAS DE DESARROLLO Y CONTEXTO ABSOLUTO: ASP.NET MVC + Telerik Kendo UI

## 1. ARQUITECTURA Y STACK BASE (1-5)
1. Framework: ASP.NET MVC (C#).
2. UI Exclusiva: Telerik Kendo UI para ASP.NET MVC (HTML Helpers).
3. Patrón: MVC Tradicional (Views .cshtml, Controllers C#, ViewModels).
4. Motor de Vistas: Razor.
5. Inyección de dependencias: Vía constructores en Controladores.

## 2. DIRECTRICES FRONTEND Y RAZOR (6-15)
6. Helper Principal: SIEMPRE iniciar con `@(Html.Kendo().[Componente]())`.
7. Prohibición jQuery Inicial: NUNCA usar `$("#id").kendoWidget()` para instanciar en carga.
8. Nombres: Todo componente DEBE tener un `.Name("IdUnico")`.
9. Cadenas mágicas: Evitar. Usar expresiones lambda `columns.Bound(c => c.Propiedad)`.
10. Templates de Cliente: Usar `.ClientTemplate("#=Campo#")` para HTML en celdas.
11. Templates de Edición: Usar `EditorTemplateName("NombreTemplate")` vinculado a `Views/Shared/EditorTemplates`.
12. Accesibilidad DOM: Para llamar al API JS, usar `var obj = $("#IdUnico").data("kendo[Componente]");`.
13. Globalización: Incluir `kendo.culture("es-MX")` si se requieren formatos latinos.
14. Imports: Asegurar `@using Kendo.Mvc.UI` en `_ViewImports` o vistas.
15. Atributos HTML: Usar `.HtmlAttributes(new { @class = "mi-clase", style = "width:100%" })`.

## 3. DIRECTRICES BACKEND Y CONTROLADORES (16-25)
16. Namespaces: Obligatorio `using Kendo.Mvc.Extensions;` y `using Kendo.Mvc.UI;`.
17. Lectura (Read): Los métodos GET deben recibir `[DataSourceRequest] DataSourceRequest request`.
18. Parseo Kendo: Retornar SIEMPRE `Json(coleccion.ToDataSourceResult(request))`.
19. POST / Crear: Recibir `[DataSourceRequest]` y el ViewModel.
20. PUT / Actualizar: Recibir `[DataSourceRequest]` y el ViewModel.
21. DELETE / Destruir: Recibir `[DataSourceRequest]` y el ViewModel.
22. Validación: Si `!ModelState.IsValid`, devolver el objeto con errores vía `ToDataSourceResult()`.
23. Prevención JSON Hijacking: En .NET Framework usar `JsonRequestBehavior.AllowGet`.
24. Fechas: Serializar fechas correctamente en el backend para evitar desfases de TimeZone.
25. DataAnnotations: Decorar ViewModels con `[Required]`, `[UIHint("Template")]`, `[HiddenInput]`.

---

## 4. MEGACATÁLOGO DE COMPONENTES (26 - 100+)

### 📊 DATA MANAGEMENT (26-35)
26. Grid Básico: `@(Html.Kendo().Grid<Model>().Name("G1").Columns(c => c.Bound(p => p.Id)))`
27. Grid Paginación: `.Pageable(p => p.PageSizes(new[] {10, 20, 50}).Refresh(true))`
28. Grid Filtros: `.Filterable(f => f.Mode(GridFilterMode.Row))`
29. Grid Agrupación: `.Groupable()`
30. Grid Jerárquico (DetailTemplate): `.ClientDetailTemplateId("templateHijo")`
31. Grid Columnas Congeladas: `columns.Bound(p => p.Id).Locked(true).Width(150)`
32. Grid Edición InLine: `.Editable(e => e.Mode(GridEditMode.InLine))`
33. ListView: `@(Html.Kendo().ListView<Model>().Name("LV").ClientTemplateId("tpl").DataSource(ds => ds.Read("R","C")))`
34. TreeList: `@(Html.Kendo().TreeList<Model>().Name("TL").Columns(c => c.Add().Field(f => f.N)).DataSource(d => d.Model(m => { m.Id(i=>i.Id); m.ParentId(p=>p.PId); })))`
35. Spreadsheet (Excel Web): `@(Html.Kendo().Spreadsheet().Name("Sheet").Sheets(s => s.Add().Name("S1")) )`

### 📝 EDITORS - TEXTO Y SELECCIÓN (36-50)
36. TextBox: `@(Html.Kendo().TextBox().Name("Txt").Placeholder("Texto..."))`
37. TextArea: `@(Html.Kendo().TextArea().Name("TA").Rows(3))`
38. NumericTextBox: `@(Html.Kendo().NumericTextBox<int>().Name("Num").Min(0).Max(100))`
39. NumericTextBox Moneda: `.Format("c").Decimals(2)`
40. MaskedTextBox: `@(Html.Kendo().MaskedTextBox().Name("Tel").Mask("(000) 000-0000"))`
41. DropDownList: `@(Html.Kendo().DropDownList().Name("DDL").DataTextField("Txt").DataValueField("Val").DataSource(ds => ds.Read("Act","Ctrl")))`
42. ComboBox (Typeahead): `@(Html.Kendo().ComboBox().Name("CB").Filter(FilterType.Contains).MinLength(3))`
43. DropDownTree: `@(Html.Kendo().DropDownTree().Name("DDT").DataTextField("Name").DataValueField("Id"))`
44. MultiSelect: `@(Html.Kendo().MultiSelect().Name("MS").Placeholder("Elija varios..."))`
45. MultiColumnComboBox: `@(Html.Kendo().MultiColumnComboBox().Name("MCCB").Columns(c => { c.Add().Field("F1"); c.Add().Field("F2"); }))`
46. AutoComplete: `@(Html.Kendo().AutoComplete().Name("AC").Filter("startswith"))`
47. ColorPicker: `@(Html.Kendo().ColorPicker().Name("Color").Value("#ff0000"))`
48. Switch (Toggle): `@(Html.Kendo().Switch().Name("Activo").Messages(m => m.Checked("SI").Unchecked("NO")))`
49. Slider: `@(Html.Kendo().Slider().Name("Sl").Min(0).Max(10).SmallStep(1))`
50. Rating: `@(Html.Kendo().Rating().Name("Rat").Min(1).Max(5))`

### 📅 EDITORS - FECHAS Y TIEMPO (51-55)
51. DatePicker: `@(Html.Kendo().DatePicker().Name("DP").Format("dd/MM/yyyy"))`
52. TimePicker: `@(Html.Kendo().TimePicker().Name("TP").Format("HH:mm"))`
53. DateTimePicker: `@(Html.Kendo().DateTimePicker().Name("DTP"))`
54. DateRangePicker: `@(Html.Kendo().DateRangePicker().Name("DRP").Range(r => r.Start(DateTime.Now).End(DateTime.Now.AddDays(7))))`
55. DateInput: `@(Html.Kendo().DateInput().Name("DI"))`

### 📤 EDITORS - ARCHIVOS Y RICHTEXT (56-60)
56. Upload Simple: `@(Html.Kendo().Upload().Name("files").Async(a => a.Save("S", "C").AutoUpload(true)))`
57. Upload Restringido: `.Validation(v => v.AllowedExtensions(new[] {".pdf"}).MaxFileSize(1048576))`
58. DropZone: `@(Html.Kendo().DropZone().Name("DZ").DropZoneElement("#area"))`
59. Editor WYSIWYG: `@(Html.Kendo().Editor().Name("Ed").Tools(t => t.Clear().Bold().Italic()))`
60. Editor con Imagenes: `.ImageBrowser(ib => ib.Image("Img/{0}").Read("R","C"))`

### 🧭 NAVIGATION (61-70)
61. Menu: `@(Html.Kendo().Menu().Name("Menu").Items(i => { i.Add().Text("A"); i.Add().Text("B"); }))`
62. TabStrip: `@(Html.Kendo().TabStrip().Name("Tabs").Items(i => { i.Add().Text("T1").Content("C1"); }))`
63. TabStrip Ajax: `.LoadContentFrom("Accion", "Controlador")`
64. TreeView: `@(Html.Kendo().TreeView().Name("TV").DataTextField("Name").DataSource(d => d.Read("R","C")))`
65. TreeView DragDrop: `.DragAndDrop(true)`
66. PanelBar (Acordeón): `@(Html.Kendo().PanelBar().Name("PB").ExpandMode(PanelBarExpandMode.Single))`
67. Stepper: `@(Html.Kendo().Stepper().Name("Step").Steps(s => { s.Add().Label("1"); s.Add().Label("2"); }))`
68. Breadcrumb: `@(Html.Kendo().Breadcrumb().Name("BC").Items(i => { i.Add().Type(BreadcrumbItemType.RootNode); }))`
69. Drawer: `@(Html.Kendo().Drawer().Name("Drw").Template("<ul><li>Item</li></ul>").Mode("push"))`
70. BottomNavigation: `@(Html.Kendo().BottomNavigation().Name("BN").Items(i => i.Add().Text("Home").Icon("home")))`

### 🪟 LAYOUT & WINDOWS (71-80)
71. Window: `@(Html.Kendo().Window().Name("Win").Title("T").Visible(false).Modal(true))`
72. Dialog: `@(Html.Kendo().Dialog().Name("Dlg").Content("Seguro?").Actions(a => a.Add().Text("OK")))`
73. Splitter: `@(Html.Kendo().Splitter().Name("Spl").Panes(p => { p.Add().Size("30%"); p.Add(); }))`
74. Tooltip: `@(Html.Kendo().Tooltip().For("#btn").Content("Hola"))`
75. Popover: `@(Html.Kendo().Popover().For("#btn").Body("Detalles").ShowOn(PopoverShowOn.Click))`
76. Notification: `@(Html.Kendo().Notification().Name("Notif").Position(p => p.Top(20).Right(20)))`
77. Card: `@(Html.Kendo().Card().Name("Card").Header(h => h.Title("T")).Content("C"))`
78. Avatar: `@(Html.Kendo().Avatar().Name("Av").Type(AvatarType.Text).Text("JS").Rounded(Rounded.Circle))`
79. TileLayout: `@(Html.Kendo().TileLayout().Name("TL").Columns(3).Containers(c => c.Add().BodyTemplate("B")))`
80. Form: `@(Html.Kendo().Form<Model>().Name("Frm").Items(i => { i.Add().Field(f => f.Nombre); }))`

### 📆 SCHEDULING (81-85)
81. Calendar: `@(Html.Kendo().Calendar().Name("Cal"))`
82. Scheduler: `@(Html.Kendo().Scheduler<TaskModel>().Name("Sch").Date(DateTime.Now).Views(v => { v.DayView(); v.MonthView(); }))`
83. Gantt: `@(Html.Kendo().Gantt<Task, Dependency>().Name("Gt").DataSource(d => d.Read("R","C")).DependenciesDataSource(d => d.Read("D","C")))`
84. TaskBoard: `@(Html.Kendo().TaskBoard<CardModel, ColumnModel>().Name("TB").Columns(c => c.DataTextField("Name")))`
85. Timeline: `@(Html.Kendo().Timeline<EventModel>().Name("TimeL").DataDateField("Date"))`

### 📈 CHARTS & GAUGES (86-95)
86. Chart Column: `@(Html.Kendo().Chart<Model>().Name("Ch").Series(s => s.Column(m => m.Val).CategoryField("Cat")))`
87. Chart Pie: `@(Html.Kendo().Chart<Model>().Name("Pie").Series(s => s.Pie(m => m.Val, m => m.Cat)))`
88. Chart Line: `@(Html.Kendo().Chart<Model>().Name("Line").Series(s => s.Line(m => m.Val)))`
89. Sparkline: `@(Html.Kendo().Sparkline().Name("Sp").Data(new[] { 1, 5, 3, 4 }))`
90. LinearGauge: `@(Html.Kendo().LinearGauge().Name("LG").Pointer(p => p.Value(50)))`
91. RadialGauge: `@(Html.Kendo().RadialGauge().Name("RG").Pointer(p => p.Value(50)))`
92. ArcGauge: `@(Html.Kendo().ArcGauge().Name("AG").Value(50).Scale(s => s.Min(0).Max(100)))`
93. Barcode: `@(Html.Kendo().Barcode().Name("BC").Value("123456").Type(BarcodeSymbology.Code128))`
94. QRCode: `@(Html.Kendo().QRCode().Name("QR").Value("https://google.com"))`
95. Map: `@(Html.Kendo().Map().Name("Map").Center(30, -110).Zoom(5))`

### ⚙️ BUTTONS, INTERACTIVITY & MISC (96-105)
96. Button: `@(Html.Kendo().Button().Name("Btn").Content("Guardar").Icon("save"))`
97. ButtonGroup: `@(Html.Kendo().ButtonGroup().Name("BG").Items(i => { i.Add().Text("A"); i.Add().Text("B"); }))`
98. FloatingActionButton: `@(Html.Kendo().FloatingActionButton().Name("FAB").Icon("plus").PositionMode(FloatingActionButtonPositionMode.Fixed))`
99. ProgressBar: `@(Html.Kendo().ProgressBar().Name("PB").Type(ProgressBarType.Value).Max(100).Value(50))`
100. Badge: `@(Html.Kendo().Badge().Name("Bd").Text("Nuevo").ThemeColor(BadgeColor.Success))`
101. SkeletonContainer: `@(Html.Kendo().SkeletonContainer().Name("SC").Animation(SkeletonContainerAnimation.Pulse))`
102. Sortable: `@(Html.Kendo().Sortable().For("#lista").Cursor("move"))`
103. MediaPlayer: `@(Html.Kendo().MediaPlayer().Name("MP").AutoPlay(true).Media(m => m.Source("vid.mp4")))`
104. ScrollView (Carousel): `@(Html.Kendo().ScrollView().Name("SV").ContentHeight("300px").Items(i => i.Add().Template("<img>")) )`
105. Captcha: `@(Html.Kendo().Captcha().Name("Cap").Handler(Url.Action("Reset", "Auth")) )`

---

## 5. INSTRUCCIONES COGNITIVAS PARA CLAUDE
- Regla 106: Al resolver problemas, asume que la respuesta preferida SIEMPRE es usar el API Fluent de Kendo C# antes que escribir scripts JS manuales.
- Regla 107: En llamadas Ajax Kendo (.Read, .Create, etc.), prioriza pasar funciones JS en `.Data("miFuncionJS")` para enviar parámetros dinámicos, en lugar de regenerar el componente.
- Regla 108: Para recargar un componente tras una acción, deduce que el comando correcto es `$("#Id").data("kendoComponente").dataSource.read();`.
- Regla 109: Respeta los EditorTemplates. Si un modelo tiene llaves foráneas, asume que necesitará un UIHint y un archivo en la carpeta EditorTemplates.
- Regla 110: Utiliza siempre la sintaxis de Razor de bloque `@{ ... }` o en línea `@(...)` correctamente pareada para evitar errores de compilación en las vistas.