# Validación de las correcciones

Fecha: 1 de noviembre de 2026, Buenos Aires. Rama de trabajo: `Facundo`, desde `e8f59bb`. Se mantuvo la persistencia SQLite/Dapper autorizada por el docente. No se incorporaron cambios de otras ramas ni se modificó `main`.

## Resultado

La solución `ECommerce.sln`, organizada bajo `src`, compila con SDK .NET 10.0.401 y runtime 10.0.12: **0 errores y 0 advertencias** en la verificación realizada.

| Suite incluida en el repositorio | Comprobaciones | Resultado |
|---|---:|---|
| `python tests/integration.py` | 207 | Todas aprobadas |
| `python tests/commerce_faults.py` | 28 | Todas aprobadas |
| `UsersCart.Regression` | 17 | Todas aprobadas |
| Total de comprobaciones de las tres suites | **252** | **Sin fallas** |

Son comprobaciones sobre distintos aspectos, no un porcentaje de cobertura de líneas ni una estimación de nota. Cada suite inicia procesos o crea bases descartables; no modifica las bases del repositorio. Los procesos temporales quedaron cerrados.

## Qué se verificó

- Arranque de las cinco APIs, Swagger UI, OpenAPI con esquemas/ejemplos de éxito y error, y health checks de proceso, almacenamiento y dependencias.
- Registro, unicidad de email normalizado, hash con salt, migración de contraseña Base64 heredada después de login correcto, contador de fallos, reseteo y bloqueo al tercer error. También se comprobaron registros e intentos de login simultáneos.
- Productos con campos válidos y faltantes, longitudes máximas, precio positivo incluso menor a 0,01, stock no negativo, duplicados y filtros combinados.
- Creación de órdenes, errores de usuario/producto/stock, precios capturados, ítems repetidos agregados y transiciones de estado.
- **Stock inicial 5 y diez compras simultáneas: cinco aceptadas, cinco rechazadas por stock, stock final 0.**
- Reintento con la misma clave idempotente sin otra orden ni otro descuento; conflicto al reutilizarla con otros datos.
- Fallo SQL forzado en el segundo ítem: rollback de cabecera e ítems, sin orden parcial.
- Products inaccesible durante la liberación: compensación persistida y aplicada una vez después de reiniciar.
- Cierre de Orders inmediatamente después de reservar: creación recuperada después del reinicio, sin doble descuento.
- Liberación anticipada que impide una reserva demorada; cancelación repetida sin doble reintegro; protección frente al límite entero al actualizar stock administrativo.
- Precisión de importes decimales de 27 dígitos y conservación del instante UTC de creación.
- Carrito con suma acumulada y agregados simultáneos, cantidad inválida, modificación de cantidad, eliminación, vaciado y fecha que no cambia al leer.
- Notificación válida persistida y consultada tras reiniciar, tipos inválidos, mensaje demasiado largo, usuario ausente e historial vacío con NTF-003.
- JSON y GUID inválidos con respuestas de error completas; Correlation ID en header y cuerpo; comportamiento consistente en Development/Production.
- Logs JSON, nivel Warning para errores de negocio y Error para fallas inesperadas, con código e identificador de seguimiento.
- Caída del proceso dependiente y ausencia deliberada de su tabla en una base de prueba: readiness deja de estar Healthy, mientras liveness permanece operativo.

## Migraciones y conservación de datos

Se ejecutaron dos migraciones consecutivas sobre copias de las bases antiguas y se verificó que conservaran sus registros. Las bases versionadas de la rama fueron comparadas mediante SHA-256 con su contenido original y no cambiaron. La reorganización de carpetas las trasladó a `src` conservando los mismos bytes.

La migración de emails/productos duplicados normalizados no elimina datos: si existen conflictos históricos, revierte y requiere resolverlos. Las contraseñas antiguas se actualizan al iniciar sesión con la clave correcta. Los importes antiguos conservan el valor disponible; una precisión que ya se hubiera perdido en REAL no se puede reconstruir.

## Reproducción

```powershell
dotnet restore ECommerce.sln
dotnet build ECommerce.sln
python tests/integration.py
python tests/commerce_faults.py
dotnet tests/UsersCart.Regression/bin/Debug/net10.0/UsersCart.Regression.dll
```

En el entorno de revisión se usó la caché local de NuGet y se desactivó su auditoría de vulnerabilidades durante la restauración. Las pruebas certifican los comportamientos registrados, no la ausencia de vulnerabilidades en dependencias. No se actualizaron paquetes sin necesidad para resolver los defectos del trabajo.

Las [capturas de Swagger](screenshots/README.md) muestran respuestas reales con errorCode, errorMessage y correlación. El mecanismo de reservas y sus límites están explicados en [consistencia de órdenes](commerce.md). La solución ofrece recuperación durable y compensación eventual, no una transacción distribuida instantánea durante una caída de dependencias.
