function swalBase(a,b,c,d){Swal.fire($.extend({title:a,text:b,type:c},d||{}));}
function swalOk(a,b,c){swalBase(a,b,'success',$.extend({confirmButtonColor:'#1bc5bd'},c||{}));}
function swalErr(a,b){swalBase(a,b,'error',{confirmButtonColor:'#f64e60'});}
function swalWarn(a,b){swalBase(a,b,'warning',{confirmButtonColor:'#f64e60'});}
function formatFecha(a){if(!a)return'�';var b=new Date(parseInt(a.replace('/Date(','').replace(')/',''))); return isNaN(b)?'�':String(b.getDate()).padStart(2,'0')+'/'+String(b.getMonth()+1).padStart(2,'0')+'/'+b.getFullYear();}
function tgConfirm(t,x,fn){$('<div/>').appendTo('body').kendoDialog({width:420,title:t,closable:true,modal:true,content:'<p style="margin:0;color:#3d4465;">'+x+'</p>',actions:[{text:'Cancelar'},{text:'Confirmar',primary:true,action:function(){fn();}}],close:function(){this.destroy();}}).data('kendoDialog').open();}
function tgAlert(t,x){$('<div/>').appendTo('body').kendoDialog({width:380,title:t,closable:true,modal:true,content:'<p style="margin:0;color:#3d4465;">'+x+'</p>',actions:[{text:'OK',primary:true}],close:function(){this.destroy();}}).data('kendoDialog').open();}

// Kanban
function cargarKanban() {
    var s = $('#startDate').data('kendoDatePicker'), e = $('#endDate').data('kendoDatePicker');
    $.post(PG.urls.readKanban, {
        startDate: s ? kendo.toString(s.value(), 'yyyy/MM/dd') : '',
        endDate:   e ? kendo.toString(e.value(), 'yyyy/MM/dd') : ''
    }, function (resp) {
        var data = resp.Data || [];
        var enProceso = ['Setup', 'Arranque', 'En Proceso'];
        var completados = ['Completado', 'Finalizado', 'Finalizado Parcial'];
        renderCol('bodyPendiente',  'cntPendiente',  data.filter(function (x) { return enProceso.indexOf(x.Status) < 0 && completados.indexOf(x.Status) < 0; }));
        renderCol('bodyEnProceso',  'cntEnProceso',  data.filter(function (x) { return enProceso.indexOf(x.Status) >= 0; }));
        renderCol('bodyCompletado', 'cntCompletado', data.filter(function (x) { return completados.indexOf(x.Status) >= 0; }));
    });
}

function renderCol(bodyId, countId, items) {
    var $body = $('#' + bodyId).empty();
    $('#' + countId).text(items.length);
    if (!items.length) { $body.append('<div class="ts-kanban-empty"><i class="fas fa-inbox"></i><br/>Sin registros</div>'); return; }

    var procStatuses = ['Setup', 'Arranque', 'En Proceso'];
    var doneStatuses = ['Completado', 'Finalizado', 'Finalizado Parcial'];
    items.forEach(function (item) {
        var isDone = doneStatuses.indexOf(item.Status) >= 0;
        var isProc = procStatuses.indexOf(item.Status) >= 0;
        var badgeCls = isDone ? 'done' : (isProc ? 'proc' : 'open');
        var badgeIco = isDone ? 'fa-check-circle' : (isProc ? 'fa-cog' : 'fa-clock');
        var ladosHtml = '';
        if (item.Lados) {
            ladosHtml = '<div class="ts-card-lados">';
            item.Lados.split(', ').forEach(function (l) { ladosHtml += '<span class="ts-lado ts-lado-active">' + l + '</span>'; });
            ladosHtml += '</div>';
        }
        var cnt = item.CantidadMateriales || 0;
        var materialBadge = cnt > 0
            ? '<span class="ts-material-badge mat-ok"><i class="fas fa-microchip"></i>&nbsp;' + cnt + ' mat.</span>'
            : '<span class="ts-material-badge mat-sin"><i class="fas fa-exclamation-triangle"></i>&nbsp;Sin materiales</span>';
        $('<div class="ts-card"></div>').attr({
            'data-id': item.Id, 'data-wo': item.WorkOrder || '', 'data-prog': item.Id_Programa || '',
            'data-ensamble': item.Ensamble || '', 'data-piezas': item.PiezasProgramadas || 0,
            'data-linea': item.Id_Linea || '', 'data-trolleys': item.Trolleys || '',
            'data-coment': item.Comentarios || '', 'data-lados': item.Lados || '',
            'data-materiales': cnt
        }).html(
            '<span class="ts-card-num">#' + item.Id + '</span>' +
            '<div style="margin-top:6px;"><span class="ts-badge ' + badgeCls + '"><i class="fas ' + badgeIco + '"></i>&nbsp;' + (item.Status || '') + '</span></div>' +
            '<div class="ts-card-title">' + (item.Ensamble || item.Id_Programa || '&mdash;') + '</div>' +
            '<div class="ts-card-sub">WO: ' + (item.WorkOrder || '') + '</div>' + ladosHtml +
            materialBadge +
            (item.Trolleys ? '<div class="ts-card-trolleys"><i class="fas fa-grip-lines"></i>&nbsp;' + item.Trolleys + '</div>' : '') +
            '<div class="ts-card-meta"><span><i class="fas fa-calendar-alt"></i>&nbsp;' + formatFecha(item.FechaCreacion) + '</span>' +
            '<span><i class="fas fa-boxes"></i>&nbsp;' + (item.PiezasProgramadas || 0) + ' pzas</span>' +
            '<span><i class="fas fa-industry"></i>&nbsp;L' + (item.Id_Linea || '?') + '</span></div>' +
            '<div class="ts-card-actions">' +
                '<a role="button" class="btn-card-edit" href="#"><i class="fas fa-edit"></i> Editar</a>' +
                '<a role="button" class="btn-card-del del" href="#"><i class="fas fa-trash-alt"></i> Eliminar</a>' +
            '</div>'
        ).appendTo($body);
    });
}

// Work Order
function generarWorkOrderBase() {
    var ddl = $('#cbLinea').data('kendoDropDownList');
    if (!ddl || !ddl.value()) return;
    var fecha = $('#FechaCreacion').val() || kendo.toString(new Date(), 'yyyy/MM/dd');
    $.get(PG.urls.semanaFiscal, { fecha: fecha }, function (r) {
        if (r.semana) { $('#txtWorkOrderBase').val('L' + ddl.value() + r.semana + r.anio); $('#txtWorkOrderSufijo').val('000'); }
    });
}

function prepararWorkOrderFinal() {
    var linea = $('#cbLinea').data('kendoDropDownList').value();
    var ensamble = $('#cbEnsamble').data('kendoDropDownList').value();
    var programa = $('#hdnProgramaSeleccionado').val();
    var base = $('#txtWorkOrderBase').val();
    if (!linea)    { swalWarn('Campo requerido', 'Selecciona una L�nea.');    return false; }
    if (!ensamble) { swalWarn('Campo requerido', 'Selecciona un Ensamble.');  return false; }
    if (!programa) { swalWarn('Campo requerido', 'No se encontraron programas para este ensamble.');  return false; }
    if (!base)     { swalWarn('Campo requerido', 'El WorkOrder es inv�lido.'); return false; }
    $('#hdnWorkOrderFinal').val(base + $('#txtWorkOrderSufijo').val());
    return true;
}

// Helpers de dropdowns
function getLineaData()    { var d = $('#cbLinea').data('kendoDropDownList');    return { lineaId: d && d.value() ? parseInt(d.value()) : 0 }; }
function getEnsambleData() { var d = $('#cbEnsamble').data('kendoDropDownList'); return { ensamble: d ? d.value() : '' }; }


function onWndNuevoCerrar() {
    ['#cbLinea', '#cbEnsamble'].forEach(function (s) { var d = $(s).data('kendoDropDownList'); if (d) { d.value(''); if (s !== '#cbLinea') d.enable(false); } });
    $('#txtWorkOrderBase,#hdnWorkOrderFinal,#txtPiezas,#txtComentarios,#hdnProgramaSeleccionado').val(''); $('#txtWorkOrderSufijo').val('000');
    $('#ladosAutoContainer').html('<span style="color:#b0b7d0;font-size:.82rem;">Selecciona un ensamble para ver los lados...</span>');
}
function onWndEditarCerrar() {
    $('#editId,#editEnsamble,#editEnsambleVal,#editWorkOrder,#editPiezas,#editTrolleys,#editLinea,#editComentarios').val('');
    $('#editLadosContainer').html('<span style="color:#b0b7d0;font-size:.82rem;">Cargando lados...</span>');
}
function onWndSetupCerrar() { $('#setupPrograma,#setupProgramaId,#setupLinea').val(''); $('#setupZonasContainer').empty(); }

function onLineaChange() {
    var id = $('#cbLinea').data('kendoDropDownList').value();
    var ddlE = $('#cbEnsamble').data('kendoDropDownList');
    ddlE.value('');
    $('#hdnProgramaSeleccionado').val('');
    $('#ladosAutoContainer').html('<span style="color:#b0b7d0;font-size:.82rem;">Selecciona un ensamble para ver los lados...</span>');
    if (id) { ddlE.enable(true); ddlE.dataSource.read(); generarWorkOrderBase(); } else { ddlE.enable(false); $('#txtWorkOrderBase').val(''); }
}
function onEnsambleChange() {
    var id = $('#cbEnsamble').data('kendoDropDownList').value();
    var $cont = $('#ladosAutoContainer');
    $('#hdnProgramaSeleccionado').val('');
    if (!id) {
        $cont.html('<span style="color:#b0b7d0;font-size:.82rem;">Selecciona un ensamble para ver los lados...</span>');
        return;
    }
    $cont.html('<span style="color:#b0b7d0;font-size:.82rem;"><i class="fas fa-spinner fa-spin"></i> Cargando...</span>');
    $.get(PG.urls.getLadosPorEnsamble, { ensamble: id }, function (r) {
        $cont.empty();
        if (!r.lados || !r.lados.length) {
            $cont.html('<span style="color:#ef4444;font-size:.82rem;">No se encontraron programas para este ensamble.</span>');
            return;
        }
        $('#hdnProgramaSeleccionado').val(r.lados[0]);
        r.lados.forEach(function (l) {
            var $lbl = $('<label style="display:inline-flex;align-items:center;gap:6px;background:#eff6ff;color:#3b82f6;border:1px solid #bfdbfe;border-radius:12px;padding:4px 12px;font-size:.82rem;font-weight:700;cursor:pointer;user-select:none;"></label>');
            $lbl.append('<input type="checkbox" class="lado-check" value="' + l + '" checked style="accent-color:#3b82f6;cursor:pointer;" />');
            $lbl.append(document.createTextNode(' ' + l));
            $cont.append($lbl);
        });
    });
}
function abrirSetup(programa, programaId, linea) {
    $('#setupPrograma').val(programa); $('#setupProgramaId').val(programaId); $('#setupLinea').val(linea);
    $('#setupAlertaDuplicados').hide();
    var $c = $('#setupZonasContainer').html('<div style="padding:12px;color:#9aa0b8;text-align:center;"><i class="fas fa-spinner fa-spin"></i> Cargando...</div>');
    $('#setupMaquina').closest('.form-group').hide();
    $.get(PG.urls.getTrolleysPorLinea, { linea: linea, programaId: programaId }, function (r) {
        $c.empty();
        if (r.error) { $c.html('<span style="color:#ef4444;">' + r.error + '</span>'); return; }
        var trolleys  = r.Trolleys  || r.trolleys  || [];
        var maquinas  = r.Maquinas  || r.maquinas  || [];
        var cabezales = r.Cabezales || r.cabezales || [];

        var html = '<div class="setup-dual-layout">';

        if (cabezales.length >= 2) {
            html += buildMachineLayout(1, '',  trolleys, cabezales[0].Acomodo || {}, cabezales[0].MaquinaId || 0, cabezales[0].MaquinaNombre || '', maquinas);
            html += buildMachineLayout(2, '2', trolleys, cabezales[1].Acomodo || {}, cabezales[1].MaquinaId || 0, cabezales[1].MaquinaNombre || '', maquinas);
        } else if (cabezales.length === 1) {
            html += buildMachineLayout(1, '', trolleys, cabezales[0].Acomodo || {}, cabezales[0].MaquinaId || 0, cabezales[0].MaquinaNombre || '', maquinas);
        } else {
            var acomodo = r.Acomodo || r.acomodo || {};
            var maqId   = r.MaquinaId || r.maquinaId || 0;
            html += buildMachineLayout(1, '', trolleys, acomodo, maqId, '', maquinas);
        }

        html += '</div>';
        $c.html(html);

        $('.setup-trolley-slot').each(function () { colorSlot($(this)); });
    });
    $('#wndSetup').data('kendoWindow').center().open();
}

function colorSlot($sel) {
    $sel.css('background-color', $sel.val() !== '-1' ? '#22c55e' : '#ef4444');
}

$(document).on('change', '.setup-trolley-slot', function () {
    colorSlot($(this));
    $('#setupAlertaDuplicados').hide();
});

// Preview Excel
function abrirPreview(data) {
    var existing = $('#gridPreviewContainer').data('kendoGrid');
    if (existing) { existing.destroy(); }
    $('#gridPreviewContainer').empty();
    var $div = $('<div id="gridPreview"></div>').appendTo('#gridPreviewContainer');
    $div.kendoGrid({
        dataSource: {
            data: data,
            schema: { model: { fields: {
                Id_Programa: { type: 'string', editable: false },
                Ensamble: { type: 'string', editable: false },
                WorkOrder: { type: 'string' },
                PiezasProgramadas: { type: 'number' },
                Id_Linea: { type: 'number' },
                FechaCreacion: { type: 'date' },
                Comentarios: { type: 'string' },
                Status: { type: 'string' },
                EsDuplicado: { type: 'boolean', editable: false },
                RazonRechazo: { type: 'string', editable: false }
            } } },
            pageSize: 100
        },
        height: 500, scrollable: true,
        pageable: { refresh: true, pageSizes: [50, 100, 200, 'all'], buttonCount: 5 },
        sortable: true, filterable: { mode: 'row' }, editable: 'incell',
        columns: [
            { field: 'EsDuplicado', title: '', width: 50, sortable: false, filterable: false, locked: true,
              template: function(d){ return d.EsDuplicado ? '<i class="fas fa-exclamation-triangle" style="color:#ef4444;font-size:16px;" title="'+(d.RazonRechazo||'')+'"></i>' : '<i class="fas fa-check-circle" style="color:#22c55e;font-size:16px;"></i>'; } },
            { field: 'Ensamble', title: 'Ensamble', width: 180 },
            { field: 'Id_Programa', title: 'Programa', width: 170 },
            { field: 'WorkOrder', title: 'WO', width: 130 },
            { field: 'PiezasProgramadas', title: 'Piezas', width: 90, format: '{0:n0}' },
            { field: 'Id_Linea', title: 'L#', width: 60 },
            { field: 'FechaCreacion', title: 'Fecha', width: 110, format: '{0:dd/MM/yyyy}' },
            { field: 'Comentarios', title: 'Notas', width: 180 },
            { title: '', width: 70, sortable: false, filterable: false,
              template: '<a class="k-button k-button-sm pg-preview-del" href="\\#" style="color:\\#ef4444;min-width:0;padding:4px 8px;"><i class="fas fa-trash-alt"></i></a>' }
        ]
    });
    actualizarConteoPreview();
    $(document).off('click.pgpreview').on('click.pgpreview', '.pg-preview-del', function(e) {
        e.preventDefault();
        var grid = $('#gridPreview').data('kendoGrid');
        var item = grid.dataItem($(this).closest('tr'));
        if (item) { grid.dataSource.remove(item); actualizarConteoPreview(); }
    });
    $('#wndPreview').data('kendoWindow').center().open();
}

function actualizarConteoPreview() {
    var g = $('#gridPreview').data('kendoGrid');
    if (!g) return;
    var all = g.dataSource.data(), n = all.length, dupl = 0;
    for (var i = 0; i < n; i++) { if (all[i].EsDuplicado) dupl++; }
    var ok = n - dupl;
    $('#previewCountLabel').html('<i class="fas fa-list-ol"></i> <b>'+n+'</b> registros &nbsp;|&nbsp; <i class="fas fa-check-circle" style="color:#22c55e;"></i> <b>'+ok+'</b> validos');
    if (dupl > 0) {
        $('#previewDuplicateLabel').html('<i class="fas fa-exclamation-triangle"></i> <b>'+dupl+'</b> duplicados (se omitiran al guardar)').show();
        $('#btnEliminarDuplicados').show();
    } else {
        $('#previewDuplicateLabel').hide();
        $('#btnEliminarDuplicados').hide();
    }
}

// Inicio
$(function () {
    var now = new Date();
    $('#startDate').kendoDatePicker({ value: new Date(now.getFullYear(), 0, 1), format: 'yyyy/MM/dd' });
    $('#endDate').kendoDatePicker({   value: new Date(now.getFullYear(), 11, 31), format: 'yyyy/MM/dd' });

    if (PG.msj.exito)      { swalOk('�Listo!', PG.msj.exito); cargarKanban(); }
    else if (PG.msj.error) { swalErr('Error', PG.msj.error);   cargarKanban(); }
    else                   { cargarKanban(); }

    $('#btnNuevoPrograma').on('click', function (e) { e.preventDefault(); $('#wndNuevo').data('kendoWindow').center().open(); });
    $('#btnSubirExcel').on('click', function (e) { e.preventDefault(); $('#archivoExcelInput').val(''); $('#excelUploadProgress').hide(); $('#wndExcel').data('kendoWindow').center().open(); });
    $('#btnBuscar').on('click',        function (e) { e.preventDefault(); cargarKanban(); });
    $('#btnCancelarNuevo').on('click',  function () { $('#wndNuevo').data('kendoWindow').close(); });
    $('#btnCancelarEditar').on('click', function () { $('#wndEditar').data('kendoWindow').close(); });
    $('#btnCancelarSetup').on('click',  function () { $('#wndSetup').data('kendoWindow').close(); });

    // Vista previa Excel
    $('#btnPreviewExcel').on('click', function () {
        var file = $('#archivoExcelInput')[0] && $('#archivoExcelInput')[0].files[0];
        if (!file) { swalWarn('Archivo requerido', 'Selecciona un archivo .xlsx'); return; }
        var fd = new FormData();
        fd.append('archivoExcel', file);
        fd.append('preview', 'true');
        var $btn = $(this).prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Procesando...');
        $('#excelUploadProgress').show();
        $.ajax({
            url: PG.urls.cargaMasiva, type: 'POST', data: fd, processData: false, contentType: false,
            success: function(r) {
                $('#excelUploadProgress').hide();
                $btn.prop('disabled', false).html('<i class="fas fa-search"></i> Vista Previa');
                if (!r.success) { swalErr('Error al leer Excel', r.message); return; }
                if (!r.data || !r.data.length) { swalWarn('Sin datos', 'No se encontraron registros en el archivo.'); return; }
                $('#wndExcel').data('kendoWindow').close();
                abrirPreview(r.data);
            },
            error: function() { $('#excelUploadProgress').hide(); $btn.prop('disabled', false).html('<i class="fas fa-search"></i> Vista Previa'); swalErr('Error', 'No se pudo procesar el archivo.'); }
        });
    });

    $('#btnDescartarPreview').on('click', function () { $('#wndPreview').data('kendoWindow').close(); });

    $('#btnEliminarDuplicados').on('click', function () {
        var grid = $('#gridPreview').data('kendoGrid');
        if (!grid) return;
        var toRemove = grid.dataSource.data().filter(function(x) { return x.EsDuplicado; });
        toRemove.forEach(function(item) { grid.dataSource.remove(item); });
        actualizarConteoPreview();
    });

    $('#btnConfirmarCarga').on('click', function () {
        var grid = $('#gridPreview').data('kendoGrid');
        if (!grid) return;
        var raw = grid.dataSource.data(), items = [], dups = 0;
        for (var i = 0; i < raw.length; i++) {
            var obj = raw[i].toJSON ? raw[i].toJSON() : raw[i];
            if (!obj.EsDuplicado) { items.push(obj); } else { dups++; }
        }
        if (!items.length) { swalWarn('Sin datos validos', 'No quedan registros validos para guardar.'); return; }
        var msj = 'Se guardaran <b>'+items.length+'</b> registros nuevos.';
        if (dups > 0) msj += '<br/><small style="color:#ef4444;">Se omitiran '+dups+' duplicados.</small>';
        Swal.fire({ title: 'Confirmar carga?', html: msj, type: 'question', showCancelButton: true,
            confirmButtonColor: '#1bc5bd', cancelButtonColor: '#9aa0b8',
            confirmButtonText: '<i class="fas fa-check"></i> Guardar', cancelButtonText: 'Cancelar' })
        .then(function(result) {
            if (!result.value) return;
            var $btn = $('#btnConfirmarCarga').prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Guardando...');
            $.ajax({
                url: PG.urls.guardarPreview, type: 'POST', contentType: 'application/json', data: JSON.stringify(items),
                success: function(r) {
                    if (r && r.success) { swalOk('Exito!', 'Se guardaron '+r.count+' programas.', { timer: 2500, showConfirmButton: false }); $('#wndPreview').data('kendoWindow').close(); cargarKanban(); }
                    else { swalErr('Error', r && r.message ? r.message : 'Error desconocido'); }
                },
                error: function() { swalErr('Error', 'Error de conexion.'); }
            }).always(function() { $btn.prop('disabled', false).html('<i class="fas fa-check"></i> Confirmar y Guardar'); });
        });
    });

    // Guardar nuevo
    $('#btnGuardarNuevo').on('click', function () {
        if (!prepararWorkOrderFinal()) return;
        var piezas = parseInt($('#txtPiezas').val());
        if (!piezas || piezas <= 0) { swalWarn('Campo requerido', 'Ingresa las piezas.'); return; }
        var lados = [];
        $('.lado-check:checked').each(function () { lados.push($(this).val()); });
        if (!lados.length) { swalWarn('Campo requerido', 'Selecciona al menos un lado.'); return; }
        var $btn = $(this).prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Guardando...');
        $.ajax({
            url: PG.urls.createManual,
            type: 'POST',
            traditional: true,
            data: {
                Id_Linea: $('#cbLinea').data('kendoDropDownList').value(),
                Id_Programa: lados[0],
                WorkOrder: $('#hdnWorkOrderFinal').val(),
                PiezasProgramadas: piezas,
                FechaCreacion: $('#FechaCreacion').val(),
                Comentarios: $('#txtComentarios').val(),
                LadosSeleccionados: lados
            },
            success: function (r) {
                if (r.success) { swalOk('\xA1Listo!', 'Programa guardado.'); $('#wndNuevo').data('kendoWindow').close(); cargarKanban(); }
                else { swalErr('Error', r.message); }
            }
        }).fail(function () { swalErr('Error', 'Error de conexi\xF3n.'); })
          .always(function () { $btn.prop('disabled', false).html('<i class="fas fa-save"></i> Guardar'); });
    });

    // Guardar edici�n
    $('#btnGuardarEditar').on('click', function () {
        var piezas = parseInt($('#editPiezas').val());
        if (!piezas || piezas <= 0) { swalWarn('Campo requerido', 'Ingresa las piezas.'); return; }
        var $btn = $(this).prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Guardando...');
        $.post(PG.urls.editarDirecto, {
            Id: parseInt($('#editId').val()), Id_Proceso: 1,
            WorkOrder: $('#editWorkOrder').val(), PiezasProgramadas: piezas,
            Trolleys: $('#editTrolleys').val(), Id_Linea: $('#editLinea').val() || null,
            Comentarios: $('#editComentarios').val()
        }, function (r) {
            if (r.success) { swalOk('�Listo!', '', { timer: 1200, showConfirmButton: false }); $('#wndEditar').data('kendoWindow').close(); cargarKanban(); }
            else { swalErr('Error', r.message); }
        }).fail(function () { swalErr('Error', 'Error de conexi�n.'); })
          .always(function () { $btn.prop('disabled', false).html('<i class="fas fa-save"></i> Guardar'); });
    });

    // Guardar setup trolleys
    $('#btnGuardarSetup').on('click', function () {
        var programaId = parseInt($('#setupProgramaId').val()), linea = parseInt($('#setupLinea').val());
        var zonas = {};
        var elegidos = [];
        var hayDuplicado = false;

        var maquinaId  = parseInt($('.setup-maquina-select[data-cabezal="1"]').val()) || 0;
        var maquinaId2 = parseInt($('.setup-maquina-select[data-cabezal="2"]').val()) || 0;

        $('.setup-trolley-slot').each(function () {
            var name = $(this).data('name'), v = parseInt($(this).val());
            zonas[name] = v;
            if (v > 0) {
                if (elegidos.indexOf(v) >= 0) hayDuplicado = true;
                elegidos.push(v);
            }
        });

        if (hayDuplicado) {
            $('#setupAlertaDuplicados').show();
            return;
        }

        var payload = $.extend({ programaId: programaId, linea: linea, maquinaId: maquinaId, maquinaId2: maquinaId2 }, zonas);
        var $btn = $(this).prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Guardando...');
        $.post(PG.urls.guardarSetupTrolleys, payload, function (r) {
            if (r.success) { swalOk('�Listo!', 'Setup guardado.', { timer: 1500, showConfirmButton: false }); $('#wndSetup').data('kendoWindow').close(); cargarKanban(); }
            else { swalErr('Error', r.message); }
        }).fail(function () { swalErr('Error', 'Error de conexi�n.'); })
          .always(function () { $btn.prop('disabled', false).html('<i class="fas fa-tools"></i> Guardar Setup'); });
    });

    // Card events � Edit
    $(document).on('click', '.btn-card-edit', function (e) {
        e.preventDefault();
        var $c = $(this).closest('.ts-card'), ensamble = $c.data('ensamble') || '', progActual = $c.data('prog') || '';
        $('#editId').val($c.data('id')); $('#editWorkOrder').val($c.data('wo'));
        $('#editEnsamble').val(ensamble); $('#editEnsambleVal').val(ensamble);
        $('#editPiezas').val($c.data('piezas')); $('#editLinea').val($c.data('linea'));
        $('#editTrolleys').val($c.data('trolleys')); $('#editComentarios').val($c.data('coment'));
        var ladosStr = $c.data('lados') || '';
        var $cont = $('#editLadosContainer').empty();
        if (ladosStr) {
            ladosStr.split(', ').forEach(function (l) {
                if (l.trim()) $cont.append('<span class="ts-lado ts-lado-active" style="padding:5px 12px;font-size:.82rem;">' + l.trim() + '</span>');
            });
        } else {
            $cont.html('<span style="color:#b0b7d0;font-size:.82rem;">Sin lados</span>');
        }
        $('#wndEditar').data('kendoWindow').center().open();
    });

    // Card events � Delete
    $(document).on('click', '.btn-card-del', function (e) {
        e.preventDefault();
        var $c = $(this).closest('.ts-card');
        var workOrder = $c.data('wo') || '';
        var ensamble  = $c.data('ensamble') || '';
        if (!workOrder) { tgAlert('Error', 'No se pudo obtener el WorkOrder.'); return; }
        tgConfirm('Eliminar carta', '\xbfEliminar toda la carta de "' + (ensamble || workOrder) + '"? Esta acci\xf3n no se puede deshacer.', function () {
            $.post(PG.urls.eliminarCarta, { workOrder: workOrder, ensamble: ensamble }, function (r) {
                if (r && r.success) { cargarKanban(); }
                else { tgAlert('Error', (r && r.message) || 'No se pudo eliminar.'); }
            }).fail(function (xhr) { tgAlert('Error', 'Error de conexi\xf3n: ' + xhr.status); });
        });
    });
});
