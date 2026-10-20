# Sonidos provisorios de Runway — P2

Fecha: 19/10/2026, America/Buenos_Aires. Esta integración continúa la preparación de audio y no modifica sus resultados históricos.

## Estado aplicado

Se generaron cuatro loops básicos mediante Runway, autorizados por el usuario. Son **recursos provisorios generados con IA**, para reemplazar o mejorar con REAPER y grabaciones propias. No se incorporaron voces, música, pasos ni sonidos alienígenas. Los prompts solicitaron ausencia de esos elementos; la revisión perceptual del contenido queda pendiente.

| Clip aplicado | Emisor | Canales / frecuencia | Carga / compresión |
|---|---|---|---|
| NightHouse_Loop.wav | AUDIO-01_NightAmbience, 2D | Estéreo, 44100 Hz | Compressed In Memory / Vorbis 0,70 |
| TV_Nonverbal_Loop.wav | AUDIO-02_TV, 3D | Mono, 44100 Hz | Compressed In Memory / Vorbis 0,70 |
| FridgeMotor_Loop.wav | AUDIO-03_Fridge, 3D | Mono, 44100 Hz | Decompress On Load / PCM |
| WindowBreeze_Loop.wav | AUDIO-05_WindowBreeze, 3D | Mono, 44100 Hz | Compressed In Memory / Vorbis 0,65 |

Las cuatro fuentes están habilitadas, con Loop, Play On Awake y salida Ambience. Distancias, posiciones y volúmenes conservan la preparación anterior. No se alteraron los faders del Mixer, la cámara ni las mecánicas. Se verificaron las propiedades después de reimportar.

No se usó Streaming: son loops cortos. Se precargan y la carga en segundo plano está desactivada. No se hicieron cambios globales de AudioSettings ni ProjectSettings. No se generó build WebGL.

## Procedencia y preparación

- Originales MP3: `AudioSources/Originales/Runway/`. Se conservan sin modificación, con sus huellas SHA-256.
- `AudioSources/Originales/Runway/procedencia.json`: prompts, identificadores de generación y correspondencia con cada WAV. No contiene enlaces privados temporales.
- Copias para Unity: `Assets/Audio/EnUso/Ambientes/`, WAV PCM16. Convertir a WAV **no recupera** información perdida por el MP3 generado.
- Herramienta fuera de Assets: `Tools/P2PrepareRunwayLoops.cs`. Decodifica con Unity, pasa a mono las tres fuentes 3D, aplica un crossfade circular lineal de 150 ms y ajusta RMS con techo de pico de -6 dBFS. No agrega scripts runtime. No sobrescribe WAV existentes.
- La duración importada observada es aproximadamente 10,038 s por WAV; difiere de los 10 s solicitados por la decodificación del MP3 y el procesamiento. No se declara un loop perfecto de oído.
- Pico/RMS y diferencia entre muestras de la unión: `Docs/P2_AUDIO_RUNWAY_MEDICIONES.json`. Son mediciones de los WAV preparados, no del Audio Profiler.
- Condiciones de uso consultadas: [derechos de uso de Runway](https://help.runwayml.com/hc/en-us/articles/18927776141715-Usage-rights). No se presentan como grabaciones del estudiante ni como material con una licencia Creative Commons.

## Prueba real ejecutada

`Tools/P2AudioSmoke.cs`, ejecutado en Play, mantuvo las cuatro fuentes reproduciendo y observó dos vueltas de cada loop. Un único listener activo. El reloj DSP y los frames avanzaron.

La muestra de salida se tomó al inicio en Bedroom durante unos 23,68 s: pico 0,06123 y RMS 0,01315 (amplitud lineal). Hubo señal no nula y el pico quedó por debajo del umbral técnico 0,99 durante esa muestra. Esto **no certifica** ausencia de saturación en todo el mapa, mezcla perceptual ni ausencia de artefactos audibles. Resultado detallado: `Docs/P2_AUDIO_RUNWAY_PRUEBA.json`.

| Prueba | Resultado |
|---|---|
| Cuatro clips asignados, emisores activos, routing Ambience | PASS automático |
| Tres clips 3D mono y ambiente 2D estéreo | PASS importación real |
| Cuatro reproducciones y dos vueltas por fuente | PASS automático |
| Señal de salida al inicio, sin pico cercano al límite medido | PASS automático, alcance limitado a Bedroom |
| Balance por habitación, sonidos apropiados y unión del loop al oído | PENDIENTE escucha humana |
| Audio Profiler: memoria y CPU | NO MEDIDO |
| WebGL / itch.io | NO EJECUTADA |

Los scripts jugables, Input, prefabs, Packages y ProjectSettings permanecen sin diferencias frente al commit anterior. Las pruebas previas de controles y puzzle no se reescribieron ni se presentan como una nueva pasada manual.

## Cómo escucharlo y continuar

Abrí Playground y entrá en Play con el audio del Editor habilitado. Al comienzo se reproduce el ambiente nocturno; cerca de la abertura del dormitorio se suma la brisa. Acercate a TV en Living y Fridge en Kitchen para escuchar sus fuentes localizadas. El Listener está en Main Camera y la atenuación no simula paredes.

Primero revisar volúmenes y percepción de repetición; después editar o reemplazar en REAPER. Para conservar referencias, reemplazar el WAV y mantener su archivo .meta. Las grabaciones propias y las voces corresponden a etapas posteriores: todavía no se agregó ninguna voz al juego.

## Control de cambios

Nuevos: cuatro MP3 originales y documentación de procedencia, cuatro WAV con sus metas, herramientas de preparación/prueba y este registro con mediciones. Modificado Playground únicamente para asignar clips y habilitar los emisores existentes. README actualizado al estado actual. No se tocaron P1 ni el proyecto original.
