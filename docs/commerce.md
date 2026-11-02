# Consistencia de órdenes e inventario

Products es el dueño del stock. Orders no vuelve a enviar un PUT con el producto completo para descontarlo. Ambos servicios conservan SQLite y Dapper, autorizados por el docente.

## Creación de una orden

1. Orders valida los identificadores y cantidades. Agrupa productos repetidos y suma sus cantidades con control de overflow.
2. Guarda una intención durable en `OrderCreationIntent` antes de llamar a Products. Su identificador será el de la orden. El request normalizado y su huella permiten reconocer reintentos.
3. Products reserva todos los ítems en una única transacción SQLite de escritura inmediata. Cada descuento usa `UPDATE Product SET Stock = Stock - cantidad WHERE Stock >= cantidad`. Si un producto no existe o falta stock, se revierte el lote completo.
4. La reserva guarda los nombres, cantidades y precios exactos capturados en esa transacción. Repetir la reserva para el mismo identificador y contenido devuelve la misma captura, sin descontar otra vez.
5. Orders inserta cabecera, todos los ítems, intención completada y acción de confirmación en una misma transacción local. No puede quedar una cabecera o un subconjunto de ítems después de un rollback.
6. Aplica la confirmación a Products. La acción queda en `OrderStockAction` hasta recibir una respuesta satisfactoria.

Los precios nuevos se conservan como texto decimal exacto. Las columnas auxiliares `PrecioExacto`, `TotalExacto` y `PrecioUnitarioExacto` permiten conservar compatibilidad con las bases previas, cuyas columnas REAL ya podían haber perdido precisión. La migración no puede reconstruir decimales que no estaban almacenados originalmente.

## Reintentos del cliente

El cliente puede enviar `Idempotency-Key` con un GUID no vacío en POST `/api/orders`. La primera llamada y todos sus reintentos con el mismo usuario y cantidades normalizadas devuelven la misma orden y el mismo Location. Repetir la clave con otro contenido devuelve 409 `ORD-008`.

Si no se envía clave, el servidor genera una y la devuelve en la respuesta exitosa. Para recuperarse de un corte sin respuesta, el cliente debe haber elegido y conservado la clave **antes** del primer envío. Repetir sin clave representa una compra nueva. Una intención fallida conserva su resultado de error; para una nueva compra corregida se usa otra clave.

## Fallas y compensación

Si la creación falla antes de una confirmación local, Orders registra en una misma transacción la intención fallida y una acción durable `release`. Antes de decidir compensar comprueba, bajo el mismo bloqueo de escritura que la creación, que no exista una orden ya confirmada localmente. No devuelve stock a ciegas después de un resultado ambiguo de commit.

Products libera una reserva una sola vez. Si recibe la liberación antes de que llegue una petición de reserva demorada, guarda una marca `Released`; esa reserva tardía será rechazada. Una confirmación demorada tampoco puede reabrir una reserva liberada o completada.

`OrderRecoveryWorker` revisa intenciones incompletas y acciones pendientes cada tres segundos. Puede reanudar una creación después de un cierre de Orders ocurrido entre la reserva y la escritura de la orden. También reintenta la devolución de stock después de que Products vuelva a estar disponible. Las acciones son idempotentes y su eliminación de la cola incluye la acción esperada, para no borrar una actualización más nueva del mismo pedido.

Esto ofrece recuperación durable y consistencia eventual; no es una transacción distribuida instantánea. Mientras Products esté caído, una compensación puede permanecer pendiente y el stock reservado sigue indisponible. Si la base de Orders también queda inaccesible y no permite registrar la decisión, la intención anterior puede reanudarse al recuperar el servicio; consultar/reintentar con la misma clave permite conocer el resultado. No se vencen reservas automáticamente por tiempo, porque liberarlas sin conocer el estado durable de Orders podría habilitar sobreventa. Las bases de ambos servicios deben conservarse y respaldarse juntas; restaurar solo una a un punto anterior requiere reconciliación.

## Estados de la orden

| Estado actual | Cambios permitidos |
|---|---|
| Pendiente | Confirmada, Cancelada |
| Confirmada | Enviada, Cancelada |
| Enviada | Entregada |
| Entregada | Ninguno |
| Cancelada | Ninguno |

Repetir el estado actual es idempotente. Una transición diferente de las permitidas devuelve 409 `ORD-006`. El UPDATE también verifica el estado anterior, de modo que dos solicitudes concurrentes no salteen una validación realizada sobre un estado viejo.

Al cancelar una orden nueva se registra la liberación y eventual reposición del stock. Al pasar a Enviada se completa la reserva sin reponerlo: desde allí el producto ya no tiene una orden activa según la definición del TP. Un producto con reservas `Reserved` o `Committed` no puede borrarse, además de la verificación HTTP de órdenes Pendiente/Confirmada. El chequeo de reservas y el borrado se hacen bajo la misma transacción, evitando la carrera con una reserva nueva. Un PUT del catálogo también verifica bajo transacción que el stock propuesto más las cantidades reservadas que podrían devolverse no excedan el máximo de un entero; así una actualización administrativa no vuelve imposible una compensación posterior.

Las órdenes que ya existían antes de esta corrección no tienen una reserva durable vinculada. Se conservan y pueden consultarse, pero su cancelación no inventa una devolución de inventario cuyo origen no se puede verificar. Para una demostración reproducible conviene crear datos nuevos en bases de prueba.

## Contratos internos de inventario

Son operaciones internas entre servicios, no reemplazan los endpoints públicos del TP.

| Método y ruta | Efecto |
|---|---|
| POST `/internal/stock/reservations/{orderId}` | Reserva un lote `{items:[{productoId,cantidad}]}` y devuelve la captura de precios |
| PUT `/internal/stock/reservations/{orderId}/confirm` | Marca la reserva como confirmada sin volver a descontar |
| PUT `/internal/stock/reservations/{orderId}/complete` | Finaliza la protección de la reserva sin reponer stock |
| DELETE `/internal/stock/reservations/{orderId}` | Libera/restaura una vez o guarda la marca que impide una reserva tardía |

Se agregan al catálogo original `PRD-006` (422, falta de stock en una reserva interna), `PRD-007` (409, reserva liberada o incompatible) y `ORD-008` (409, clave idempotente incompatible). Orders traduce PRD-001 y PRD-006 de Products a ORD-004 y ORD-005 respectivamente. Estas rutas siguen el supuesto del TP de servicios internos confiables; un despliegue expuesto a Internet requiere autenticación y autorización de las comunicaciones internas.

## Verificación de fallas

La suite `tests/commerce_faults.py` utiliza bases temporales y un proxy de fallas local. Comprueba compras simultáneas, reintentos con la misma clave, rollback del segundo ítem, liberación pendiente con Products inaccesible y recuperación después de cerrar Orders justo después de reservar. No opera sobre las bases del repositorio.
