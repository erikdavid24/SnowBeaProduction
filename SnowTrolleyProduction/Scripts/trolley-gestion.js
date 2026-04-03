// ============================================================
//  trolley-gestion.js  –  CRUD Máquinas, Ensambles, Programas, Acomodos
// ============================================================

// Pestañas
$(function () {
    $('.gestion-tabs .nav-link').on('click', function (e) {
        e.preventDefault();
        var tab = $(this).data('tab');
        $('.gestion-tabs .nav-link').removeClass('active');
        $(this).addClass('active');
        $('.gestion-panel').hide();
        $('#' + tab + 'Container').show();

        if (tab === 'maquinas')  cargarMaquinas();
        if (tab === 'ensambles') cargarEnsambles();
        if (tab === 'programas') cargarProgramas();
        if (tab === 'acomodos')  cargarAcomodos();
    });

    // Initial load
    var activeTab = $('.gestion-tabs .nav-link.active').data('tab') || 'maquinas';
    if (activeTab === 'maquinas')  cargarMaquinas();
    if (activeTab === 'ensambles') cargarEnsambles();
    if (activeTab === 'programas') cargarProgramas();
    if (activeTab === 'acomodos')  cargarAcomodos();

    // Search with debounce
    var ensambleTimer, programaTimer;
    $('#txtBuscarEnsamble').on('input', function () {
        clearTimeout(ensambleTimer);
        var val = $(this).val();
        ensambleTimer = setTimeout(function () { cargarEnsambles(val); }, 500);
    });
    $('#txtBuscarPrograma').on('input', function () {
        clearTimeout(programaTimer);
        var val = $(this).val();
        programaTimer = setTimeout(function () { cargarProgramas(val); }, 500);
    });

    // Agregar buttons
    $('#btnAgregarEnsamble').on('click', function () { abrirModal(TG.urls.agregarEnsambleModal); });
    $('#btnAgregarPrograma').on('click', function () { abrirModal(TG.urls.agregarProgramaModal); });
});

function onWndGestionCerrar() {
    $('#wndGestionBody').empty();
}

// Helper Modal
function abrirModal(url, params) {
    $.get(url, params || {}, function (html) {
        $('#wndGestionBody').html(html);
        var wnd = $('#wndGestion').data('kendoWindow');
        wnd.center().open();
    });
}

// Máquinas
function cargarMaquinas() {
    $.get(TG.urls.maquinasTable, function (html) {
        $('#maquinasContainer').html(html);
    });
}

$(document).on('click', '#btnAgregarMaquina', function () { abrirModal(TG.urls.agregarMaquinaModal); });

$(document).on('click', '.btn-edit-maquina', function () {
    abrirModal(TG.urls.editarMaquinaModal, { maquinaId: $(this).data('id') });
});

$(document).on('click', '.btn-del-maquina', function () {
    var id = $(this).data('id');
    if (!confirm('¿Estás seguro de eliminar esta máquina?')) return;
    $.post(TG.urls.eliminarMaquina, { maquinaId: id }, function (r) {
        if (r.success) cargarMaquinas();
        else alert(r.message);
    });
});

// Ensambles
function cargarEnsambles(numero) {
    $.get(TG.urls.ensamblesTable, { numero: numero || '' }, function (html) {
        $('#ensamblesTablePlace').html(html);
    });
}

$(document).on('click', '.btn-edit-ensamble', function () {
    abrirModal(TG.urls.editarEnsambleModal, { ensambleId: $(this).data('id') });
});

$(document).on('click', '.btn-del-ensamble', function () {
    var id = $(this).data('id');
    if (!confirm('¿Estás seguro de eliminar este ensamble?')) return;
    $.post(TG.urls.eliminarEnsamble, { ensambleId: id }, function (r) {
        if (r.success) cargarEnsambles();
        else alert(r.message);
    });
});

// Programas
function cargarProgramas(numero) {
    $.get(TG.urls.programasTable, { numero: numero || '' }, function (html) {
        $('#programasTablePlace').html(html);
    });
}

$(document).on('click', '.btn-edit-programa', function () {
    abrirModal(TG.urls.editarProgramaModal, { programaId: $(this).data('id') });
});

$(document).on('click', '.btn-del-programa', function () {
    var id = $(this).data('id');
    if (!confirm('¿Estás seguro de eliminar este programa?')) return;
    $.post(TG.urls.eliminarPrograma, { programaId: id }, function (r) {
        if (r.success) cargarProgramas();
        else alert(r.message);
    });
});

// Acomodos
function cargarAcomodos() {
    $.get(TG.urls.acomodoTable, function (html) {
        $('#acomodosContainer').html(html);
    });
}

$(document).on('click', '#btnAgregarAcomodo', function () {
    abrirModal(TG.urls.agregarAcomodoModal);
});

$(document).on('click', '.acomodo-toggle', function () {
    var targetId = $(this).data('target');
    var $detail = $('#' + targetId);
    var $arrow = $(this).find('.acomodo-arrow');
    $detail.toggle();
    $arrow.toggleClass('fa-chevron-right fa-chevron-down');
});
