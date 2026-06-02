// ============================================================
//  trolley-gestion.js  -  CRUD Maquinas, Ensambles, Programas, Acomodos
// ============================================================

function tgConfirm(titulo, texto, onConfirm) {
    $('<div/>').appendTo('body').kendoDialog({
        width: 420, title: titulo, closable: true, modal: true,
        content: '<p style="margin:0;color:#3d4465;">' + texto + '</p>',
        actions: [
            { text: 'Cancelar' },
            { text: 'Confirmar', primary: true, action: function () { onConfirm(); } }
        ],
        close: function () { this.destroy(); }
    }).data('kendoDialog').open();
}

function tgAlert(titulo, texto) {
    $('<div/>').appendTo('body').kendoDialog({
        width: 380, title: titulo, closable: true, modal: true,
        content: '<p style="margin:0;color:#3d4465;">' + texto + '</p>',
        actions: [{ text: 'OK', primary: true }],
        close: function () { this.destroy(); }
    }).data('kendoDialog').open();
}

function onWndGestionCerrar() {
    $('#wndGestionBody').empty();
}

function abrirModal(url, params) {
    $.get(url, params || {}, function (html) {
        $('#wndGestionBody').html(html);
        $('#wndGestion').data('kendoWindow').center().open();
    });
}

function cargarMaquinas()  { location.reload(); }
function cargarEnsambles() { location.reload(); }
function cargarProgramas() { location.reload(); }
function cargarAcomodos()  { location.reload(); }

// ── Maquinas ──────────────────────────────────────────────────────────────────
$(document).on('click', '#btnAgregarMaquina', function () { abrirModal(TG.urls.agregarMaquinaModal); });

$(document).on('click', '.btn-edit-maquina', function () {
    abrirModal(TG.urls.editarMaquinaModal, { maquinaId: $(this).data('id') });
});

$(document).on('click', '.btn-del-maquina', function () {
    var id = $(this).data('id');
    tgConfirm('Eliminar maquina', 'Esta accion no se puede deshacer.', function () {
        $.post(TG.urls.eliminarMaquina, { maquinaId: id }, function (r) {
            if (r.success) location.reload();
            else tgAlert('Error', r.message);
        });
    });
});

// ── Ensambles ─────────────────────────────────────────────────────────────────
$(document).on('click', '#btnAgregarEnsamble', function () { abrirModal(TG.urls.agregarEnsambleModal); });

$(document).on('click', '.btn-edit-ensamble', function () {
    abrirModal(TG.urls.editarEnsambleModal, { ensambleId: $(this).data('id') });
});

$(document).on('click', '.btn-del-ensamble', function () {
    var id = $(this).data('id');
    tgConfirm('Eliminar ensamble', 'Esta accion no se puede deshacer.', function () {
        $.post(TG.urls.eliminarEnsamble, { ensambleId: id }, function (r) {
            if (r.success) location.reload();
            else tgAlert('Error', r.message);
        });
    });
});

// ── Programas ─────────────────────────────────────────────────────────────────
$(document).on('click', '#btnAgregarPrograma', function () { abrirModal(TG.urls.agregarProgramaModal); });

$(document).on('click', '.btn-edit-programa', function () {
    abrirModal(TG.urls.editarProgramaModal, { programaId: $(this).data('id') });
});

$(document).on('click', '.btn-del-programa', function () {
    var id = $(this).data('id');
    tgConfirm('Eliminar programa', 'Esta accion no se puede deshacer.', function () {
        $.post(TG.urls.eliminarPrograma, { programaId: id }, function (r) {
            if (r.success) location.reload();
            else tgAlert('Error', r.message);
        });
    });
});

// ── Acomodos ──────────────────────────────────────────────────────────────────
$(document).on('click', '#btnAgregarAcomodo', function () {
    abrirModal(TG.urls.agregarAcomodoModal);
});

$(document).on('click', '.btn-edit-acomodo', function () {
    abrirModal(TG.urls.editarAcomodoModal, { ensambleId: $(this).data('id') });
});

$(document).on('click', '.acomodo-toggle', function () {
    var targetId = $(this).data('target');
    $('#' + targetId).toggle();
    $(this).find('.acomodo-arrow').toggleClass('fa-chevron-right fa-chevron-down');
});
