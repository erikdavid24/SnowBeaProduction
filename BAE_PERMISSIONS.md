# Sistema de Permisos BAEClassLibrary

## Cómo funciona

Los permisos se configuran desde el **panel de administración de BAE** (externo al código).
En el código solo se *consultan* — nunca se definen aquí.

El objeto central es `BAEClassLibrary.UserContext` que expone dos cosas:

### 1. Flag de administrador global
```csharp
UserContext.current.IsAdmin  // true/false
```
Si es `true`, el usuario tiene acceso total. No necesita tener ningún permiso de módulo asignado en BAE.

### 2. Permisos por módulo
```csharp
BAEClassLibrary.UserContext.ModulePermissions.Access("Nombre del Módulo")  // Ver / entrar al módulo
BAEClassLibrary.UserContext.ModulePermissions.Create("Nombre del Módulo")  // Crear registros
BAEClassLibrary.UserContext.ModulePermissions.Update("Nombre del Módulo")  // Editar registros
BAEClassLibrary.UserContext.ModulePermissions.Delete("Nombre del Módulo")  // Eliminar registros
BAEClassLibrary.UserContext.ModulePermissions.Print("Nombre del Módulo")   // Exportar / imprimir
```
Cada método devuelve `bool`. El nombre del módulo debe coincidir **exactamente** (mayúsculas, espacios, acentos) con el registrado en el panel de BAE.

---

## Patrón recomendado

### En el Controller — filtrar datos
```csharp
bool hasPermission = BAEClassLibrary.UserContext.ModulePermissions.Access("Mi Modulo");
bool isAdmin       = UserContext.current != null && UserContext.current.IsAdmin;

// Si el usuario NO tiene permiso y NO es admin → solo ve sus propios registros
if (!hasPermission && !isAdmin)
{
    data = data.Where(item => item.EmployeeID == UserContext.current.EmployeeID).ToList();
}
```

### En la Vista (.cshtml) — mostrar/ocultar botones
Declarar `isAdmin` una vez al inicio del archivo:
```csharp
@{
    bool isAdmin = BAEClassLibrary.UserContext.current != null && BAEClassLibrary.UserContext.current.IsAdmin;
}
```

Luego usarlo en cada validación:
```cshtml
@* Botón crear *@
@if (isAdmin || BAEClassLibrary.UserContext.ModulePermissions.Create("Mi Modulo")) {
    <a class="k-button" href="#">Crear</a>
}

@* Botón editar *@
@if (isAdmin || BAEClassLibrary.UserContext.ModulePermissions.Update("Mi Modulo")) {
    <a class="k-button" href="#">Editar</a>
}

@* Filtros de búsqueda *@
@if (isAdmin || BAEClassLibrary.UserContext.ModulePermissions.Access("Mi Modulo")) {
    @* campos de filtro *@
}
```

En Kendo Grid (columna filterable / comando):
```csharp
columns.ForeignKey(...).Filterable(isAdmin || BAEClassLibrary.UserContext.ModulePermissions.Access("Mi Modulo"));

columns.Command(command => {
    if (isAdmin || BAEClassLibrary.UserContext.ModulePermissions.Update("Mi Modulo")) command.Edit();
    if (isAdmin || BAEClassLibrary.UserContext.ModulePermissions.Delete("Mi Modulo")) command.Destroy();
});
```

---

## Cómo agregar un nuevo módulo

1. **En el panel de BAE** — crear el módulo con el nombre exacto, por ejemplo `"Gestion de Auditorias"`, y asignar los permisos (Access, Create, Update, Delete, Print) a los roles o usuarios que corresponda.

2. **En el código** — usar el mismo nombre literal:
```csharp
// Controller
bool puedeVer   = BAEClassLibrary.UserContext.ModulePermissions.Access("Gestion de Auditorias");
bool puedeCrear = BAEClassLibrary.UserContext.ModulePermissions.Create("Gestion de Auditorias");

// Vista
@if (isAdmin || BAEClassLibrary.UserContext.ModulePermissions.Create("Gestion de Auditorias")) { ... }
```

3. No se requiere ningún cambio en la base de datos del proyecto ni en archivos de configuración locales.

---

## Errores comunes

| Problema | Causa | Solución |
|----------|-------|----------|
| Permiso siempre devuelve `false` | El nombre del módulo no coincide exactamente con BAE | Verificar mayúsculas, espacios y acentos |
| Admin no ve opciones / datos filtrados | El código no incluye `\|\| isAdmin` en la validación | Agregar `isAdmin \|\|` antes de cada `ModulePermissions.*` |
| NullReferenceException en la vista | `UserContext.current` es null (sesión no iniciada) | Siempre verificar `UserContext.current != null` antes de usarlo |
| Botón de acción no aparece en registros completados | Lógica del ClientTemplate no contempla registros con `Registration_Status == false` | Usar condición que incluya al asignado y al que tiene permiso, no solo al estado abierto |
