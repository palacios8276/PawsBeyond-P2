# Paws Beyond P2 — Fase 1: estabilización y ladridos

Fecha: 23/10/2026. Repositorio exclusivo: palacios8276/PawsBeyond-P2, main. Base: 30f2b1a y auditoría P2_FASE_0_AUDITORIA_Y_PLAN.md/GDD v2.0. No se modifican P1 ni el informe histórico de testing.

## Bloque 1 — Progresión y baño

- OwnersJourney conserva el recorrido P1. Acumula observación y dispone de un inicio de respaldo tras visitar el dormitorio (maximumStartWait=3 s). Para el tutorial futuro puede desactivarse automaticStart y llamarse BeginJourney; no se implementan todavía los cinco episodios.
- El baño requiere llegada efectiva de los dueños a su waypoint de espera y presencia del perro. OnTriggerStay y el registro de presencia permiten ambos órdenes de llegada sin salir y reingresar.
- La puerta conserva un único lado de apertura: openTarget=-95°, sin detección de lado. HingeJoint.spring (Spring/Damper) permite ajustar respuesta desde Unity; no se cambió el valor existente ni se agregó un sistema de velocidades.
- Mientras el puzzle está pendiente, la puerta primero cierra con su resorte y luego se inmovilizan temporalmente los grados de libertad de su Rigidbody. Al resolver se restauran las restricciones originales, y vuelve a abrir físicamente con HingeJoint. El Player conserva CharacterController sin Rigidbody. No se agregaron barreras invisibles ni geometría nueva.
- Se conservan transporte con fuerzas, E/Raycast, tres prendas y abducción posterior del cesto. No se implementan sacudida/lanzamiento.
- Se evita reescribir el objetivo del resorte cuando no cambia.

### Pruebas y límites

P2Phase1BathroomSmoke: PASS automático. Inicio sin completar observación, perro primero, activación al llegar dueños, 28 intentos de escape (4 alturas y 7 laterales), puerta estable bajo empuje, tres prendas con E, apertura posterior, reentrada sin reset y recorrido hasta cocina. Evidencia: P2_FASE1_BANO_PRUEBA.json.

Los casos de altura usan prendas colocadas/apiladas y posiciones iniciales controladas; los intentos de cruce usan CharacterController.Move. El teclado es virtual y la velocidad de dueños se aumenta a 8 m/s solo en Play para acotar duración. La escena guardada conserva 1,1 m/s y espera máxima de inicio de 3 s. Esto demuestra los escenarios automatizados, no sustituye repetir manualmente QA-04/05/06 con teclado y PS5.

Durante desarrollo se descartaron dos soluciones: límites de bisagra iguales devolvían NaN; un rango estrecho todavía cedía ante empuje repetido del CharacterController. La solución validada inmoviliza únicamente el Rigidbody de la puerta ya cerrada y restaura sus restricciones al resolver.

Primer intento del harness con eval_file no fue ejecutado por formato incompatible; se usa run_script para compilar y ejecutar sus clases C# fuera de Assets. Los scripts del juego compilaron sin errores.

### Archivos del bloque 1

Assets/Scripts/Systems/OwnersJourney.cs; Assets/Scripts/Puzzles/BathroomBasketDrop.cs; Assets/Scripts/Interaction/DoorTriggerDetector.cs; Assets/Scenes/Playground.unity; Tools/P2Phase1BathroomSmoke.cs; Tools/P2Phase1LateEntrySmoke.cs; evidencia JSON de ambas pruebas y este registro.

## Reproducción

Usar Unity CLI con el proyecto explícito y run_script, no ejecutar las clases de Tools mediante eval_file. Ejemplo:

```powershell
unity command run_script --caller plugin --skill unity-cli --project-path 'C:/Users/JUNIN/Unity Projects/PawsBeyond-P2/PawsBeyond-P2' --timeout 240 --format json -- --file 'Tools/P2Phase1BathroomSmoke.cs' --entry P2Phase1BathroomSmoke.Main --timeout_ms 220000
```

Ejecutar cada prueba en Play nuevo. Para el caso temprano, antes de Play preparar maximumStartWait=30 sin guardar; el harness restablece 3 al comenzar y verifica que aún no inició. Al terminar salir de Play y recargar Playground guardada; no guardar cambios de preparación. Los helpers no forman parte del juego.

P2Phase1LateEntrySmoke: PASS automático adicional. Dueños primero esperan sin activar; entrada posterior del perro activa tres prendas. Evidencia: P2_FASE1_BANO_LLEGADA_TARDIA.json. Ambos casos de llegada quedaron ejecutados; pendiente retesting manual exploratorio.

## Bloque 2 — Ladridos por pulsación

- Acción Bark agregada al mapa Player existente: B y <Gamepad>/buttonWest (cuadrado PS5 / X de Xbox), interacción Press Only. E/botón sur conservan Interact. Asignación provisional para revisión.
- PlayerBark en Player habilita solo su acción de entrada, con minimumInterval=0,45 s ajustable y UnityEvent onBarked para conexiones futuras. No modifica movimiento/cámara ni implementa rayo/IA.
- Reutiliza DogBarkAudio y AUDIO-09_DogBark. Asignar clips en variants de DogBarkAudio; volumen, pitch, mezcla y alcance se configuran en su AudioSource. Una pulsación válida reinicia el único emisor; no apila voces del clip provisional largo. Si falta audio válido, no confirma el ladrido ni emite evento.
- El importador nativo regeneró PlayerInputActions.cs desde el asset de acciones; no se editó manualmente el wrapper. GUID/meta existentes preservados.

P2Phase1BarkSmoke: PASS automático en Play, cuatro ladridos aceptados. B y botón oeste virtual funcionan; mantener no repite, soltar no dispara, el cooldown filtra pulsaciones demasiado próximas y deshabilitar/rehabilitar no duplica suscripciones. El evento ocurre con el AudioSource iniciado y se midió salida PCM no silenciosa. Botón sur no genera ladrido. Evidencia: P2_FASE1_LADRIDOS_PRUEBA.json. Falta teclado/PS5 físicos y escucha perceptual.

Archivos: Assets/Input/PlayerInputActions.inputactions y wrapper .cs; Assets/Scripts/Player/PlayerBark.cs y meta; Assets/Scripts/Audio/DogBarkAudio.cs; Playground; Tools/P2Phase1BarkSmoke.cs; evidencia JSON y este registro.
