function swalBase(a,b,c,d){Swal.fire($.extend({title:a,html:b,type:c},d||{}));}
function swalOk(a,b,c){swalBase(a,b,'success',$.extend({confirmButtonColor:'#1bc5bd'},c||{}));}
function swalErr(a,b){swalBase(a,b,'error',{confirmButtonColor:'#f64e60'});}
function swalWarn(a,b){swalBase(a,b,'warning',{confirmButtonColor:'#f64e60'});}
function formatFecha(a){if(!a)return'�';var b=new Date(parseInt(a.replace('/Date(','').replace(')/',''))); return isNaN(b)?'�':String(b.getDate()).padStart(2,'0')+'/'+String(b.getMonth()+1).padStart(2,'0')+'/'+b.getFullYear();}
function tgConfirm(t,x,fn){$('<div/>').appendTo('body').kendoDialog({width:420,title:t,closable:true,modal:true,content:'<p style="margin:0;color:#3d4465;">'+x+'</p>',actions:[{text:'Cancelar'},{text:'Confirmar',primary:true,action:function(){fn();}}],close:function(){this.destroy();}}).data('kendoDialog').open();}
function tgAlert(t,x){$('<div/>').appendTo('body').kendoDialog({width:380,title:t,closable:true,modal:true,content:'<p style="margin:0;color:#3d4465;">'+x+'</p>',actions:[{text:'OK',primary:true}],close:function(){this.destroy();}}).data('kendoDialog').open();}

function inicializarFiltroSemanaFiscal() {
    var datePicker = $('#fiscalWeekPicker').data('kendoDatePicker');
    if (!datePicker) return;

    datePicker.bind('change', function() {
        var selectedDate = this.value();
        if (selectedDate) {

            var fechaStr = kendo.toString(selectedDate, 'yyyy/MM/dd');
            $.get(PG.urls.semanaFiscal, { fecha: fechaStr }, function(r) {
                if (r && r.semana) {

                    $('#fiscalWeekPicker').data('fiscal-week', r.semana);
                    $('#fiscalWeekPicker').data('fiscal-year', r.anio);
                }
            });
        }
    });
}

function actualizarMesesFiscales(ano) {

}

function actualizarSemanasDelMes(ano, mes) {

}

function renderKanban(data) {
    var enProceso   = ['Setup', 'Arranque', 'En Proceso'];
    var completados = ['Completado', 'Finalizado', 'Finalizado Parcial'];
    var reSaldo     = /-\d+$/;

    var saldoMap = {};
    data.forEach(function (x) {
        var m = /^(.+?)-(\d+)$/.exec(x.WorkOrder || '');
        if (m && enProceso.indexOf(x.Status) < 0 && completados.indexOf(x.Status) < 0 && x.Status !== 'Pendiente')
            saldoMap[m[1]] = (saldoMap[m[1]] || 0) + (x.PiezasProgramadas || 0);
    });
    data.forEach(function (x) {
        x._isSaldo = enProceso.indexOf(x.Status) < 0 && completados.indexOf(x.Status) < 0
                     && x.Status !== 'Pendiente' && reSaldo.test(x.WorkOrder || '');
    });

    renderCol('bodyPendiente', 'cntPendiente', 'filterLineaPendiente',
        data.filter(function (x) { return x.Status === 'Pendiente'; }), saldoMap);
    renderCol('bodyCreado',    'cntCreado',    'filterLineaCreado',
        data.filter(function (x) { return enProceso.indexOf(x.Status) < 0 && completados.indexOf(x.Status) < 0 && x.Status !== 'Pendiente'; }), saldoMap);
    renderCol('bodyEnProceso', 'cntEnProceso', 'filterLineaEnProceso',
        data.filter(function (x) { return enProceso.indexOf(x.Status) >= 0; }), saldoMap);
    renderCol('bodyCompletado','cntCompletado','filterLineaCompletado',
        data.filter(function (x) { return completados.indexOf(x.Status) >= 0; }), saldoMap);

    agruparCreadoPorLinea();
    initKanbanSortables();
    poblarFiltroGlobal(data);
}

function agruparCreadoPorLinea() {
    var $body = $('#bodyCreado');
    var cards = $body.find('.ts-card').toArray();
    if (!cards.length) return;
    cards.sort(function (a, b) { return ($(a).data('linea') || 0) - ($(b).data('linea') || 0); });
    $body.empty();
    cards.forEach(function (card) {
        $body.append(card);
    });
}

function initKanbanSortables(lineaActiva) {
    _lineaFiltroCreado = lineaActiva || '';

    var ps = $('#bodyPendiente').data('kendoSortable'); if (ps) ps.destroy();
    var cs = $('#bodyCreado').data('kendoSortable');    if (cs) cs.destroy();

    $('#bodyPendiente').kendoSortable({
        filter: '.ts-card', cursor: 'grabbing',
        hint: kanbanHint, placeholder: kanbanPlaceholder,
        connectWith: '#bodyCreado',
        start: function (e) {
            _dragValid = false; _dragSource = 'pendiente';
            e.item.addClass('ts-card--dragging');
            $('#bodyCreado').addClass('kanban-drop-ready');
            $(document).off('mouseup.kanbanDrop touchend.kanbanDrop');
            $(document).one('mouseup.kanbanDrop touchend.kanbanDrop', function () {
                e.item.removeClass('ts-card--dragging');
                $('.ts-kanban-col-body').removeClass('kanban-drop-ready kanban-drag-over');
                setTimeout(function () { if (!_dragValid) cargarKanban(); }, 120);
            });
        },
        change: function (e) {
            _dragValid = true;
            $('.ts-kanban-col-body').removeClass('kanban-drop-ready kanban-drag-over');
            if (e.action === 'receive') {
                var id = parseInt(e.item.attr('data-id'), 10);
                if (!id) { tgAlert('Error', 'ID de carta inválido.'); cargarKanban(); return; }
                $.post(PG.urls.cambiarStatus, { id: id, status: 'Pendiente' }, function (r) {
                    if (!r.success) { tgAlert('Error', r.message || 'No se pudo mover la carta.'); }
                    cargarKanban();
                }).fail(function (xhr) { tgAlert('Error', 'Error de conexión (' + xhr.status + ').'); cargarKanban(); });
            }
        }
    });

    $('#bodyCreado').kendoSortable({
        filter: '.ts-card', cursor: 'grabbing',
        hint: kanbanHint,
        placeholder: function (element) {
            // En "Todas" no se muestra el recuadro azul de destino (sin animacion de reacomodo).
            if (!_lineaFiltroGlobal) return $('<div class="pg-sort-noop">').css({ display: 'none' });
            return kanbanPlaceholder(element);
        },
        connectWith: '#bodyPendiente',
        start: function (e) {
            _dragValid = false; _dragSource = 'creado';
            // En "Todas" se bloquea el reorden arriba/abajo: se guarda el orden para revertir.
            _creadoSnapshot = !_lineaFiltroGlobal ? $('#bodyCreado').children('.ts-card').toArray() : null;
            e.item.addClass('ts-card--dragging');
            $('#bodyPendiente').addClass('kanban-drop-ready');
            $(document).off('mouseup.kanbanDrop touchend.kanbanDrop');
            $(document).one('mouseup.kanbanDrop touchend.kanbanDrop', function () {
                e.item.removeClass('ts-card--dragging');
                $('.ts-kanban-col-body').removeClass('kanban-drop-ready kanban-drag-over');
                setTimeout(function () { if (!_dragValid) cargarKanban(); }, 120);
            });
        },
        change: function (e) {
            _dragValid = true;
            $('.ts-kanban-col-body').removeClass('kanban-drop-ready kanban-drag-over');
            // En "Todas" se bloquea el reorden interno (arriba/abajo); mover a Pendiente si se permite.
            if (e.action === 'sort' && !_lineaFiltroGlobal) {
                if (_creadoSnapshot) {
                    var $b = $('#bodyCreado');
                    _creadoSnapshot.forEach(function (el) { $b.append(el); });
                }
                return;
            }
            if (e.action === 'receive') {
                var id = parseInt(e.item.attr('data-id'), 10);
                if (!id) { tgAlert('Error', 'ID de carta inválido.'); cargarKanban(); return; }
                $.post(PG.urls.cambiarStatus, { id: id, status: 'Creado' }, function (r) {
                    if (!r.success) { tgAlert('Error', r.message || 'No se pudo mover la carta.'); }
                    cargarKanban();
                }).fail(function (xhr) { tgAlert('Error', 'Error de conexión (' + xhr.status + ').'); cargarKanban(); });
            }
        }
    });
}

var _lineaFiltroCreado    = '';
var _lineaFiltroPendiente = '';
var _kanbanScrollTimer    = null;
var _dragValid            = false;
var _dragSource           = '';
var _creadoSnapshot       = null;

function kanbanHint(element) {
    return element.clone().css({
        width: element.outerWidth(),
        opacity: 0.96,
        boxShadow: '0 10px 28px rgba(0,0,0,.22)',
        border: '2px solid #5d78ff',
        transform: 'rotate(1.5deg) scale(1.01)',
        zIndex: 10000,
        pointerEvents: 'none'
    });
}

function kanbanPlaceholder(element) {
    return $('<div class="pg-sort-placeholder">').css({ height: element.outerHeight(true) });
}

function onPendienteMove(e) {
    var $body = $('#bodyPendiente');
    var $empty = $body.find('.ts-kanban-empty');
    var $ph = $body.find('.pg-sort-placeholder');
    if ($empty.length && $ph.length && $body.children().first()[0] !== $ph[0]) {
        $body.prepend($ph);
    }
}

function onKanbanCardMoved(e) {
    if (e.action !== 'receive') return;
    var id     = e.item.data('id');
    var isPend = (_dragSource === 'group');
    var status = isPend ? 'Pendiente' : 'Creado';

    if (isPend) {
        var $it = e.item, $body = $('#bodyPendiente');
        setTimeout(function () {
            $body.find('.ts-kanban-empty').remove();
            $body.prepend($it);
        }, 0);
    }

    $.post(PG.urls.cambiarStatus, { id: id, status: status }, function (r) {
        if (!r.success) { tgAlert('Error', r.message || 'No se pudo mover la carta.'); cargarKanban(); }
        else if (!isPend) { cargarKanban(); }
    }).fail(function () { tgAlert('Error', 'Error de conexion.'); cargarKanban(); });
}

// Filtro GLOBAL de linea: filtra las 4 columnas a la vez.
var _lineaFiltroGlobal = '';

function poblarFiltroGlobal(data) {
    var ddl = $('#filterLineaGlobal').data('kendoDropDownList');
    if (!ddl) return;
    var prev = _lineaFiltroGlobal || '';
    var lineas = [];
    (data || []).forEach(function (x) {
        var l = x.Id_Linea;
        if (l && lineas.indexOf(l) < 0) lineas.push(l);
    });
    lineas.sort(function (a, b) { return a - b; });
    var ds = [{ text: 'Todas', value: '' }];
    lineas.forEach(function (l) { ds.push({ text: 'L' + l, value: String(l) }); });
    ddl.setDataSource(new kendo.data.DataSource({ data: ds }));
    var existe = ds.some(function (d) { return d.value === prev; });
    ddl.value(existe ? prev : '');
    _lineaFiltroGlobal = ddl.value();
    aplicarFiltroGlobal(_lineaFiltroGlobal);
}

function onFiltrarLineaGlobal() {
    _lineaFiltroGlobal = this.value();
    aplicarFiltroGlobal(_lineaFiltroGlobal);
}

function aplicarFiltroGlobal(linea) {
    $('#kanbanBoard .ts-card').each(function () {
        var l = String($(this).data('linea') || '');
        $(this).toggle(!linea || l === String(linea));
    });
}

function cargarKanban() {
    cargarPrecargas();
    var datePicker = $('#fiscalWeekPicker').data('kendoDatePicker');
    var selectedDate = datePicker && datePicker.value();
    if (!selectedDate) {
        $.post(PG.urls.readKanban, { semana: 0, anio: 0 }, function (resp) { renderKanban(resp.Data || []); });
        return;
    }
    var fechaStr = kendo.toString(selectedDate, 'yyyy/MM/dd');
    $.get(PG.urls.semanaFiscal, { fecha: fechaStr }, function(r) {
        var semana = (r && r.semana) ? parseInt(r.semana) : 0;
        var anio   = (r && r.anio)   ? parseInt('20' + r.anio) : new Date().getFullYear();
        $.post(PG.urls.readKanban, { semana: semana, anio: anio }, function (resp) { renderKanban(resp.Data || []); });
    });
}

function renderCol(bodyId, countId, filterId, items, saldoMap) {
    var $body = $('#' + bodyId).empty();
    $('#' + countId).text(items.length);

    if (!items.length) { $body.append('<div class="ts-kanban-empty"><div class="k-grid" style="border:none;background:transparent;overflow:visible;height:auto;box-shadow:none;"><img class="show-empty" /></div></div>'); return; }

    var procStatuses    = ['Setup', 'Arranque', 'En Proceso'];
    var doneStatuses    = ['Completado', 'Finalizado'];
    var partialStatuses = ['Finalizado Parcial'];
    items.forEach(function (item) {
        var isDone    = doneStatuses.indexOf(item.Status) >= 0;
        var isProc    = procStatuses.indexOf(item.Status) >= 0;
        var isPartial     = item._isSaldo || partialStatuses.indexOf(item.Status) >= 0;
        var displayStatus = item._isSaldo ? 'Por Terminar' : (item.Status || '');
        var badgeCls = isDone ? 'done' : (isProc ? 'proc' : (isPartial ? 'partial' : 'open'));
        var badgeIco = isDone ? 'fa-check-circle' : (isProc ? 'fa-cog' : (isPartial ? 'fa-hourglass-half' : 'fa-clock'));
        var ladosHtml = '';
        if (item.Lados) {
            ladosHtml = '<div class="ts-card-lados">';
            item.Lados.split(', ').forEach(function (l) { ladosHtml += '<span class="ts-lado ts-lado-active">' + l + '</span>'; });
            ladosHtml += '</div>';
        }
        var cnt = item.CantidadMateriales || 0;

        var fechaDisplay = (isDone && item.FechaFinalizacion) ? item.FechaFinalizacion : item.FechaCreacion;
        var piezasDisplay = item.PiezasProgramadas || 0;
        if (isDone && saldoMap && saldoMap[item.WorkOrder] !== undefined) {
            var real = piezasDisplay - saldoMap[item.WorkOrder];
            if (real >= 0) piezasDisplay = real;
        }

        $('<div class="ts-card"></div>').attr({
            'data-id': item.Id, 'data-wo': item.WorkOrder || '', 'data-prog': item.Id_Programa || '',
            'data-ensamble': item.Ensamble || '', 'data-piezas': item.PiezasProgramadas || 0,
            'data-linea': item.Id_Linea || '', 'data-trolleys': item.Trolleys || '',
            'data-coment': item.Comentarios || '', 'data-lados': item.Lados || '',
            'data-materiales': cnt
        }).html(
            '<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:5px;">' +
            '<span class="ts-badge ' + badgeCls + '"><i class="fas ' + badgeIco + '"></i>&nbsp;' + displayStatus + '</span>' +
            '<span class="ts-card-num">#' + item.Id + '</span>' +
            '</div>' +
            '<div class="ts-card-title">' + (item.Ensamble || item.Id_Programa || '&mdash;') + '</div>' +
            '<div class="ts-card-sub">WO: ' + (item.WorkOrder || '') + '</div>' + ladosHtml +
            (item.Trolleys ? '<div class="ts-card-trolleys"><i class="fas fa-grip-lines"></i>&nbsp;' + item.Trolleys + '</div>' : '') +
            '<div class="ts-card-meta"><span><i class="fas fa-calendar-alt"></i>&nbsp;' + formatFecha(fechaDisplay) + '</span>' +
            '<span><i class="fas fa-boxes"></i>&nbsp;' + piezasDisplay + ' pzas</span>' +
            '<span><i class="fas fa-industry"></i>&nbsp;L' + (item.Id_Linea || '?') + '</span></div>' +
            '<div class="ts-card-actions">' +
                '<a role="button" class="btn-card-edit" href="#"><i class="fas fa-edit"></i> Editar</a>' +
                '<a role="button" class="btn-card-del del" href="#"><i class="fas fa-trash-alt"></i> Eliminar</a>' +
            '</div>'
        ).appendTo($body);
    });
}

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
    var ensamble = $('#cbEnsamble').data('kendoComboBox').value();
    var programa = $('#hdnProgramaSeleccionado').val();
    var base = $('#txtWorkOrderBase').val();
    if (!linea)    { swalWarn('Campo requerido', 'Selecciona una L�nea.');    return false; }
    if (!ensamble) { swalWarn('Campo requerido', 'Selecciona un Ensamble.');  return false; }
    if (!programa) { swalWarn('Campo requerido', 'No se encontraron programas para este ensamble.');  return false; }
    if (!base)     { swalWarn('Campo requerido', 'El WorkOrder es inv�lido.'); return false; }
    $('#hdnWorkOrderFinal').val(base + $('#txtWorkOrderSufijo').val());
    return true;
}

function getLineaData()    { var d = $('#cbLinea').data('kendoDropDownList');    return { lineaId: d && d.value() ? parseInt(d.value()) : 0 }; }
function getEnsambleData() { var d = $('#cbEnsamble').data('kendoComboBox'); return { ensamble: d ? d.value() : '' }; }

function onWndNuevoCerrar() {
    var dLineaNvo = $('#cbLinea').data('kendoDropDownList');   if (dLineaNvo) dLineaNvo.value('');
    var dEnsNvo   = $('#cbEnsamble').data('kendoComboBox');    if (dEnsNvo)   { dEnsNvo.value(''); dEnsNvo.enable(false); }
    $('#txtWorkOrderBase,#hdnWorkOrderFinal,#txtPiezas,#txtComentarios,#hdnProgramaSeleccionado').val(''); $('#txtWorkOrderSufijo').val('000');
    $('#ladosAutoContainer').html('<span style="color:#b0b7d0;font-size:.82rem;">Selecciona un ensamble para ver los lados...</span>');
}
function onWndEditarCerrar() {
    $('#editId,#editEnsamble,#editEnsambleVal,#editWorkOrder,#editWorkOrderBase,#editWorkOrderSufijo,#editTrolleys,#editComentarios').val('');
    var npClose = $('#editPiezas').data('kendoNumericTextBox'); if (npClose) npClose.value(null);
    var dlClose = $('#editLinea').data('kendoDropDownList');     if (dlClose) dlClose.value('');
    $('#editLadosContainer').html('<span style="color:#b0b7d0;font-size:.82rem;">Cargando lados...</span>');
}
function onWndSetupCerrar() { $('#setupPrograma,#setupProgramaId,#setupLinea').val(''); $('#setupZonasContainer').empty(); }

function onLineaChange() {
    var id = $('#cbLinea').data('kendoDropDownList').value();
    var ddlE = $('#cbEnsamble').data('kendoComboBox');
    ddlE.value('');
    $('#hdnProgramaSeleccionado').val('');
    $('#ladosAutoContainer').html('<span style="color:#b0b7d0;font-size:.82rem;">Selecciona un ensamble para ver los lados...</span>');
    if (id) { ddlE.enable(true); ddlE.dataSource.read(); generarWorkOrderBase(); } else { ddlE.enable(false); $('#txtWorkOrderBase').val(''); }
}
function onEnsambleChange() {
    var id = $('#cbEnsamble').data('kendoComboBox').value();
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

function abrirPreview(data, soloVer) {
    var existing = $('#gridPreviewContainer').data('kendoGrid');
    if (existing) { existing.destroy(); }
    $('#gridPreviewContainer').empty();
    var $div = $('<div id="gridPreview"></div>').appendTo('#gridPreviewContainer');
    $div.kendoGrid({
        dataSource: {
            data: data,
            schema: { model: { fields: {
                Id_Programa:       { type: 'string',  editable: false },
                Ensamble:          { type: 'string',  editable: false },
                WorkOrder:         { type: 'string' },
                PiezasProgramadas: { type: 'number' },
                Id_Linea:          { type: 'number' },
                FechaCreacion:     { type: 'date' },
                Comentarios:       { type: 'string' },
                Status:            { type: 'string' },
                EsDuplicado:       { type: 'boolean', editable: false },
                RazonRechazo:      { type: 'string',  editable: false },
                EsActualizacion:   { type: 'boolean', editable: false },
                ExistingId:        { type: 'number',  editable: false },
                PiezasAnteriores:  { type: 'number',  editable: false },
                Pendiente:         { type: 'boolean', defaultValue: false, editable: false },
                Seleccionado:      { type: 'boolean', defaultValue: true,  editable: false }
            } } },
            pageSize: 100
        },
        height: 500, scrollable: true,
        pageable: { refresh: true, pageSizes: [50, 100, 200, 'all'], buttonCount: 5 },
        sortable: true,
        filterable: { mode: 'row' },
        editable: 'incell',
        noRecords: { template: "<img class='show-empty' />" },
        columns: [
            { field: 'Seleccionado', width: 46, sortable: false, filterable: false,
              headerTemplate: '<input type="checkbox" class="preview-sel-all" checked title="Seleccionar / deseleccionar todos" style="width:16px;height:16px;cursor:pointer;" />',
              headerAttributes: { style: 'text-align:center;' },
              attributes: { style: 'text-align:center;' },
              template: function(d){ if(d.EsDuplicado) return '<input type="checkbox" class="preview-sel-chk" disabled style="width:16px;height:16px;cursor:not-allowed;" title="Duplicado: no se puede seleccionar" />'; return '<input type="checkbox" class="preview-sel-chk" ' + (d.Seleccionado !== false ? 'checked' : '') + ' style="width:16px;height:16px;cursor:pointer;" />'; } },
            { field: 'EsDuplicado', title: '', width: 50, sortable: false, filterable: false,
              template: function(d){ if(d.EsDuplicado) return '<i class="fas fa-exclamation-triangle" style="color:#ef4444;font-size:16px;" title="'+(d.RazonRechazo||'')+'"></i>'; if(d.EsActualizacion) return '<i class="fas fa-sync-alt" style="color:#3b82f6;font-size:16px;" title="'+(d.RazonRechazo||'')+'"></i>'; if(d.RazonRechazo) return '<i class="fas fa-exclamation-triangle" style="color:#f59e0b;font-size:16px;" title="'+d.RazonRechazo+'"></i>'; return '<i class="fas fa-check-circle" style="color:#22c55e;font-size:16px;"></i>'; } },
            { field: 'Pendiente', title: 'Restricción', width: 100, sortable: false, filterable: false,
              headerAttributes: { style: 'text-align:center;font-size:.78rem;' },
              attributes: { style: 'text-align:center;' },
              template: function(d){ return '<input type="checkbox" class="preview-pend-chk" ' + (d.Pendiente ? 'checked' : '') + ' style="width:16px;height:16px;cursor:pointer;" title="Marcar como Requerido no programado por restricción" />'; } },
            { field: 'Ensamble',          title: 'Ensamble', width: 200,
              filterable: { cell: { operator: 'contains', showOperators: false } } },
            { field: 'Id_Programa',       title: 'Programa',  width: 180,
              filterable: { cell: { operator: 'contains', showOperators: false } } },
            { field: 'WorkOrder',         title: 'WO',        width: 140,
              filterable: { cell: { operator: 'contains', showOperators: false } } },
            { field: 'PiezasProgramadas', title: 'Piezas',    width: 90, format: '{0:n0}',
              filterable: { cell: { operator: 'gte', showOperators: false } } },
            { field: 'Id_Linea',          title: 'L#',        width: 70,
              filterable: { cell: { operator: 'eq', showOperators: false } } },
            { field: 'FechaCreacion',     title: 'Fecha',     width: 120, format: '{0:dd/MM/yyyy}',
              filterable: { cell: { operator: 'gte', showOperators: false } } },
            { field: 'Comentarios',       title: 'Notas',     width: 180,
              filterable: { cell: { operator: 'contains', showOperators: false } } },
            { title: '', width: 60, sortable: false, filterable: false,
              template: '<a class="k-button k-button-sm pg-preview-del" href="\\#" style="color:\\#ef4444;min-width:0;padding:4px 8px;"><i class="fas fa-trash-alt"></i></a>' }
        ]
    });

    $(document).off('change.pgpend').on('change.pgpend', '.preview-pend-chk', function () {
        var grid = $('#gridPreview').data('kendoGrid');
        var item = grid.dataItem($(this).closest('tr'));
        if (item) item.set('Pendiente', this.checked);
    });
    $(document).off('change.pgsel').on('change.pgsel', '.preview-sel-chk', function () {
        var grid = $('#gridPreview').data('kendoGrid');
        var item = grid.dataItem($(this).closest('tr'));
        if (item) item.set('Seleccionado', this.checked);
        sincronizarSelAll();
        actualizarConteoPreview();
    });
    $(document).off('change.pgselall').on('change.pgselall', '.preview-sel-all', function () {
        var chk = this.checked;
        var grid = $('#gridPreview').data('kendoGrid');
        var all = grid.dataSource.data();
        for (var i = 0; i < all.length; i++) { if (!all[i].EsDuplicado) all[i].set('Seleccionado', chk); }
        $('#gridPreview .preview-sel-chk:not(:disabled)').prop('checked', chk);
        actualizarConteoPreview();
    });
    actualizarConteoPreview();
    $(document).off('click.pgpreview').on('click.pgpreview', '.pg-preview-del', function(e) {
        e.preventDefault();
        var grid = $('#gridPreview').data('kendoGrid');
        var item = grid.dataItem($(this).closest('tr'));
        if (item) { grid.dataSource.remove(item); actualizarConteoPreview(); }
    });
    if (soloVer) {
        $('#btnConfirmarCarga').hide();
        $('#btnEliminarDuplicados').hide();
        var gSolo = $('#gridPreview').data('kendoGrid');
        if (gSolo) gSolo.hideColumn('Seleccionado');
    } else {
        $('#btnConfirmarCarga').show();
    }
    $('#wndPreview').data('kendoWindow').center().open();
}

function verPrecargas() {
    $.get(PG.urls.getPrecargas, function (data) {
        if (!data || !data.length) {
            tgAlert('Sin pendientes', 'No hay registros pendientes de autorizacion.');
            return;
        }
        var items = $.map(data, function (d) {
            return $.extend({}, d, { EsDuplicado: false, RazonRechazo: '' });
        });
        abrirPreview(items, true);
    }).fail(function () { tgAlert('Error', 'No se pudieron cargar los registros pendientes.'); });
}

function sincronizarSelAll() {
    var g = $('#gridPreview').data('kendoGrid');
    if (!g) return;
    var all = g.dataSource.data(), totalSel = 0, sel = 0;
    for (var i = 0; i < all.length; i++) {
        if (all[i].EsDuplicado) continue;
        totalSel++;
        if (all[i].Seleccionado !== false) sel++;
    }
    var $chk = $('#gridPreview .preview-sel-all');
    if (!$chk.length) return;
    $chk.prop('checked', totalSel > 0 && sel === totalSel);
    $chk.prop('indeterminate', sel > 0 && sel < totalSel);
}

function actualizarConteoPreview() {
    var g = $('#gridPreview').data('kendoGrid');
    if (!g) return;
    var all = g.dataSource.data(), n = all.length, dupl = 0, sel = 0;
    for (var i = 0; i < n; i++) {
        if (all[i].EsDuplicado) { dupl++; continue; }
        if (all[i].Seleccionado !== false) sel++;
    }
    var ok = n - dupl;
    $('#previewCountLabel').html('<i class="fas fa-list-ol"></i> <b>'+n+'</b> registros &nbsp;|&nbsp; <i class="fas fa-check-circle" style="color:#22c55e;"></i> <b>'+ok+'</b> validos &nbsp;|&nbsp; <i class="fas fa-check-square" style="color:#1bc5bd;"></i> <b>'+sel+'</b> seleccionados');
    if (dupl > 0) {
        $('#previewDuplicateLabel').html('<i class="fas fa-exclamation-triangle"></i> <b>'+dupl+'</b> duplicados (se omitiran al guardar)').show();
        $('#btnEliminarDuplicados').show();
    } else {
        $('#previewDuplicateLabel').hide();
        $('#btnEliminarDuplicados').hide();
    }
}

function cargarPrecargas() {
    $.get(PG.urls.getPrecargas, function (data) {
        var count = (data && data.length) ? data.length : 0;
        $('#badgePrecargas').text(count);
        if (count > 0) {
            $('#secPrecargas').slideDown(200);
            $('#btnSubirExcel').addClass('disabled').attr('title', 'Autoriza o rechaza los pendientes antes de subir otro Excel.');
        } else {
            $('#secPrecargas').slideUp(200);
            $('#btnSubirExcel').removeClass('disabled').removeAttr('title');
        }
    });
}

$(function () {

    if (PG.msj.exito)      { swalOk('Listo!', PG.msj.exito); cargarKanban(); }
    else if (PG.msj.error) { swalErr('Error', PG.msj.error);   cargarKanban(); }
    else                   { cargarKanban(); }

    inicializarFiltroSemanaFiscal();

    $('#btnNuevoPrograma').on('click', function (e) { e.preventDefault(); $('#wndNuevo').data('kendoWindow').center().open(); });
    $('#btnSubirExcel').on('click', function (e) {
        e.preventDefault();
        if ($(this).hasClass('k-state-disabled')) {
            tgAlert('Pendientes de autorizacion', 'Debes autorizar o rechazar los registros pendientes antes de subir otro Excel.');
            return;
        }
        $('#archivoExcelInput').val(''); $('#excelUploadProgress').hide(); $('#wndExcel').data('kendoWindow').center().open();
    });
    $('#btnBuscar').on('click',  function (e) { e.preventDefault(); cargarKanban(); });
    $('#btnLimpiar').on('click', function (e) {
        e.preventDefault();
        var dp = $('#fiscalWeekPicker').data('kendoDatePicker');
        if (dp) { dp.value(null); }
        cargarPrecargas();
        $.post(PG.urls.readKanban, { semana: 0, anio: 0 }, function (resp) { renderKanban(resp.Data || []); });
    });
    $('#btnCancelarNuevo').on('click',  function () { $('#wndNuevo').data('kendoWindow').close(); });
    $('#btnCancelarEditar').on('click', function () { $('#wndEditar').data('kendoWindow').close(); });
    // Si se borra el sufijo del WorkOrder y se deja vacio, volver a poner "000"
    $(document).on('blur', '#editWorkOrderSufijo', function () {
        if (!($(this).val() || '').trim()) $(this).val('000');
    });
    $('#btnCancelarSetup').on('click',  function () { $('#wndSetup').data('kendoWindow').close(); });

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
                abrirPreview(r.data, false);
            },
            error: function() { $('#excelUploadProgress').hide(); $btn.prop('disabled', false).html('<i class="fas fa-search"></i> Vista Previa'); swalErr('Error', 'No se pudo procesar el archivo.'); }
        });
    });

    $('#btnDescartarPreview').on('click', function () {
        $('#btnConfirmarCarga').show();
        $('#wndPreview').data('kendoWindow').close();
    });

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
        var raw = grid.dataSource.data(), items = [], dups = 0, noSel = 0;
        for (var i = 0; i < raw.length; i++) {
            var obj = raw[i].toJSON ? raw[i].toJSON() : raw[i];
            if (obj.EsDuplicado) { dups++; continue; }
            if (obj.Seleccionado === false) { noSel++; continue; }
            items.push(obj);
        }
        if (!items.length) { swalWarn('Sin seleccion', 'Marca al menos un registro para pre-guardar.'); return; }
        var msj = 'Se guardaran <b>'+items.length+'</b> registros seleccionados.';
        if (noSel > 0) msj += '<br/><small style="color:#7d8499;">Se omitiran '+noSel+' sin seleccionar.</small>';
        if (dups > 0) msj += '<br/><small style="color:#ef4444;">Se omitiran '+dups+' duplicados.</small>';
        Swal.fire({ title: 'Pre-Guardar Excel?', html: msj, type: 'question', showCancelButton: true,
            confirmButtonColor: '#1bc5bd', cancelButtonColor: '#9aa0b8',
            confirmButtonText: '<i class="fas fa-check"></i> Pre-Guardar', cancelButtonText: 'Cancelar' })
        .then(function(result) {
            if (!result.value) return;
            var $btnK = $('#btnConfirmarCarga').data('kendoButton'); $btnK.enable(false);
            $('#btnConfirmarCarga').find('.k-button-text').html('<i class="fas fa-spinner fa-spin"></i> Guardando...');
            var $btn = $('#btnConfirmarCarga');
            $.ajax({
                url: PG.urls.preGuardar, type: 'POST', contentType: 'application/json', data: JSON.stringify(items),
                success: function(r) {
                    if (r && r.success) { swalOk('Pre-Guardado!', '<b>'+r.count+'</b> registros en espera de autorizacion.', { timer: 2500, showConfirmButton: false }); $('#wndPreview').data('kendoWindow').close(); cargarPrecargas(); }
                    else { swalErr('Error', r && r.message ? r.message : 'Error desconocido'); }
                },
                error: function() { swalErr('Error', 'Error de conexion.'); }
            }).always(function() { $('#btnConfirmarCarga').data('kendoButton').enable(true); $('#btnConfirmarCarga').find('.k-button-text').html('<i class="fas fa-check"></i> Pre-Guardar'); });
        });
    });

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

    $('#btnGuardarEditar').on('click', function () {
        var nPiezasW = $('#editPiezas').data('kendoNumericTextBox');
        var piezas = nPiezasW ? nPiezasW.value() : parseInt($('#editPiezas').val());
        if (!piezas || piezas <= 0) { swalWarn('Campo requerido', 'Ingresa las piezas.'); return; }
        var ddlLineaW = $('#editLinea').data('kendoDropDownList');
        var lineaSel = ddlLineaW ? ddlLineaW.value() : $('#editLinea').val();
        if (!lineaSel) { swalWarn('Campo requerido', 'Selecciona una Linea.'); return; }
        var woSuf = ($('#editWorkOrderSufijo').val() || '').trim();
        if (!woSuf) { woSuf = '000'; $('#editWorkOrderSufijo').val('000'); }
        var woFinal = ($('#editWorkOrderBase').val() || '') + woSuf;
        $('#editWorkOrder').val(woFinal);
        var $btn = $(this).prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Guardando...');
        $.post(PG.urls.editarDirecto, {
            Id: parseInt($('#editId').val()), Id_Proceso: 1,
            WorkOrder: woFinal, PiezasProgramadas: piezas,
            Trolleys: $('#editTrolleys').val(), Id_Linea: lineaSel || null,
            Comentarios: $('#editComentarios').val()
        }, function (r) {
            if (r.success) { swalOk('Listo!', '', { timer: 1200, showConfirmButton: false }); $('#wndEditar').data('kendoWindow').close(); cargarKanban(); }
            else { swalErr('Error', r.message); }
        }).fail(function () { swalErr('Error', 'Error de conexion.'); })
          .always(function () { $btn.prop('disabled', false).html('<i class="fas fa-save"></i> Guardar'); });
    });

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
            if (r.success) { swalOk('Listo!', 'Setup guardado.', { timer: 1500, showConfirmButton: false }); $('#wndSetup').data('kendoWindow').close(); cargarKanban(); }
            else { swalErr('Error', r.message); }
        }).fail(function () { swalErr('Error', 'Error de conexion.'); })
          .always(function () { $btn.prop('disabled', false).html('<i class="fas fa-tools"></i> Guardar Setup'); });
    });

    $(document).on('click', '.btn-card-edit', function (e) {
        e.preventDefault();
        var $c = $(this).closest('.ts-card'), ensamble = $c.data('ensamble') || '', progActual = $c.data('prog') || '';
        $('#editId').val($c.data('id'));
        var woFull = String($c.data('wo') || '');
        var woBase = woFull.length > 3 ? woFull.slice(0, -3) : woFull;
        var woSuf  = woFull.length > 3 ? woFull.slice(-3)    : '';
        $('#editWorkOrder').val(woFull);
        $('#editWorkOrderBase').val(woBase);
        $('#editWorkOrderSufijo').val(woSuf);
        $('#editEnsamble').val(ensamble); $('#editEnsambleVal').val(ensamble);
        var nPiezas = $('#editPiezas').data('kendoNumericTextBox');
        if (nPiezas) nPiezas.value($c.data('piezas') || null);
        var ddlLinea = $('#editLinea').data('kendoDropDownList');
        if (ddlLinea) ddlLinea.value(String($c.data('linea') || ''));
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

    $('#btnVerPrecargas').on('click', function () { verPrecargas(); });

    $('#btnAutorizarTodo').on('click', function () {
        Swal.fire({ title: 'Autorizar carga?', html: 'Los registros se moveran al kanban como <b>Creado</b>.',
            type: 'question', showCancelButton: true,
            confirmButtonColor: '#22c55e', cancelButtonColor: '#9aa0b8',
            confirmButtonText: '<i class="fas fa-check"></i> Autorizar', cancelButtonText: 'Cancelar' })
        .then(function (r) {
            if (!r.value) return;
            $.post(PG.urls.autorizarPrecargas, function (res) {
                if (res && res.success) {
                    swalOk('Autorizado!', res.count + ' programas agregados al plan.', { timer: 2500, showConfirmButton: false });
                    cargarPrecargas();
                    cargarKanban();
                } else { swalErr('Error', res && res.message ? res.message : 'Error desconocido'); }
            }).fail(function () { swalErr('Error', 'Error de conexion.'); });
        });
    });

    $('#btnRechazarTodo').on('click', function () {
        Swal.fire({ title: 'Rechazar carga?', html: 'Se eliminaran todos los registros pendientes.',
            type: 'warning', showCancelButton: true,
            confirmButtonColor: '#ef4444', cancelButtonColor: '#9aa0b8',
            confirmButtonText: '<i class="fas fa-times"></i> Rechazar', cancelButtonText: 'Cancelar' })
        .then(function (r) {
            if (!r.value) return;
            $.post(PG.urls.rechazarPrecargas, function (res) {
                if (res && res.success) {
                    swalOk('Rechazado', 'Los registros pendientes fueron eliminados.', { timer: 2000, showConfirmButton: false });
                    cargarPrecargas();
                } else { swalErr('Error', res && res.message ? res.message : 'Error desconocido'); }
            }).fail(function () { swalErr('Error', 'Error de conexion.'); });
        });
    });

    $(document).on('mousemove.kanbanscroll', function (e) {
        clearInterval(_kanbanScrollTimer);
        _kanbanScrollTimer = null;
        if (!$('.pg-sort-placeholder').length) return;
        var ZONE = 100, SPEED = 15;
        $('.ts-kanban-col-body').each(function () {
            var r = this.getBoundingClientRect();
            if (e.clientX < r.left - 20 || e.clientX > r.right + 20) return;
            var el = this, delta = 0;
            if (e.clientY > r.top && e.clientY < r.top + ZONE)
                delta = -Math.ceil(SPEED * (1 - (e.clientY - r.top) / ZONE));
            else if (e.clientY > r.bottom - ZONE && e.clientY < r.bottom)
                delta = Math.ceil(SPEED * (1 - (r.bottom - e.clientY) / ZONE));
            if (delta) {
                var d = delta, target = el;
                _kanbanScrollTimer = setInterval(function () { target.scrollTop += d; }, 16);
            }
        });
    });
});
