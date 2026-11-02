# Notificaciones

`POST /api/notifications/send` valida que el destinatario exista en Users.API y acepta únicamente los canales `Email`, `Push` y `SMS`, escritos con esos nombres. El mensaje debe contener texto y tener como máximo 500 caracteres. El ID del destinatario no puede ser un GUID vacío.

La operación simula un envío exitoso, registra `estado: "Enviada"` y asigna `fechaEnvio` en UTC. Devuelve 201 solamente después de persistir el resultado. No se contacta ningún proveedor ni se envían emails, mensajes Push o SMS reales. El log de simulación incluye `EventCode: "NTF-SENT"`, ID de notificación, canal, destinatario y estado; no incluye el texto del mensaje.

Ejemplo de solicitud:

```json
{
  "usuarioId": "a1b2c3d4-0000-0000-0000-111122223333",
  "mensaje": "Su orden fue confirmada.",
  "tipo": "Email"
}
```

Ejemplo de respuesta:

```json
{
  "id": "11112222-3333-4444-5555-666677778888",
  "usuarioId": "a1b2c3d4-0000-0000-0000-111122223333",
  "tipo": "Email",
  "mensaje": "Su orden fue confirmada.",
  "estado": "Enviada",
  "fechaEnvio": "2026-11-01T12:00:00Z"
}
```

`GET /api/notifications/{userId}` consulta el historial persistido localmente. Si no hay registros devuelve 404 con `NTF-003`. Las notificaciones existentes pueden consultarse aunque el destinatario haya sido bloqueado posteriormente o Users.API esté temporalmente fuera de servicio.

| Código | HTTP | Condición |
|---|---|---|
| NTF-001 | 404 | El destinatario del POST no existe en Users.API. |
| NTF-002 | 400 | Datos incompletos, mensaje inválido, tipo no permitido o identificador vacío. |
| NTF-003 | 404 | El usuario no tiene notificaciones registradas. |
| NTF-004 | 500 | Fallo inesperado de persistencia o no se puede verificar el destinatario por un fallo de integración. |

Una caída de Users.API no se representa con NTF-003: ese código conserva el significado de historial vacío definido por la consigna.

## Migración de bases anteriores

Al iniciar, una transacción adapta el esquema existente sin borrar registros:

- La columna antigua `FechaCreacion` pasa a llamarse `FechaEnvio`, conservando sus valores.
- Se agrega `Estado` si falta. Los registros anteriores quedan `Pendiente`, porque la versión anterior solo los guardaba y no registraba el resultado del envío.
- Se agrega un índice por destinatario y fecha.

Las bases nuevas incluyen restricciones de canal, longitud de mensaje y estado. La migración no reescribe los mensajes ni los canales de registros históricos, aunque los haya aceptado la versión anterior con valores que hoy se rechazan. Las escrituras nuevas se validan en el servicio de negocio. La conversión de los GUID guardados como TEXT queda registrada mediante el type handler de Dapper.

La conexión se configura con `ConnectionStrings:DefaultConnection`; el valor predeterminado es `Data Source=notifications.db;Foreign Keys=True;`. La dependencia se configura con `Services:UsersApiUrl`.
