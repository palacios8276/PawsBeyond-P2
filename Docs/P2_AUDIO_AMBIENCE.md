# PawsBeyond-P2 — Ambientación doméstica, etapa 1

Registro: 19/10/2026 (America/Buenos_Aires). Estado: **estructura de audio preparada; sonido pendiente de recursos**.

## 1. Inspección inicial

- Proyecto exclusivo: `C:/Users/JUNIN/Unity Projects/PawsBeyond-P2/PawsBeyond-P2`, Unity 6000.0.81f1. Contiene Assets, Packages y ProjectSettings; Pipeline conectado a esta ruta.
- Inspección inicial: no había Git en la raíz Unity. Después se localizó el clon existente anidado y se trasladó únicamente .git a esta raíz, preservando historial. Remoto: https://github.com/palacios8276/PawsBeyond-P2.git; branch main. El registro inicial de audio precede a esa configuración.
- Escena inspeccionada y modificada: `Assets/Scenes/Playground.unity`, inicialmente guardada y fuera de Play. `SampleScene` no se modificó.
- Inicialmente: 0 AudioSources, 0 AudioClips bajo Assets, 0 AudioMixers. Listener único activo: Main Camera; salida consultada: 48000 Hz; plataforma activa WebGL.
- Emisores visuales existentes: TV y Fridge. No se encontró ventilador. La abertura de Bedroom queda entre los tramos BRWall_west (2)/(3), debajo de BRWall_west y encima de BRWall_west (1).
- Dimensiones medidas por bounds: Bedroom 12 × 8; Living 16,16 × 22; Kitchen 11,26 × 9,70 unidades de planta. No se modificó geometría.

## 2. GameObjects y parámetros aplicados

Se agregaron exclusivamente GameObjects vacíos con Transform y AudioSource. Sin colliders, triggers, scripts runtime ni elementos visuales.

| GameObject y ubicación | Spatial Blend | Rolloff | Min/Max | Volumen inicial |
|---|---:|---|---|---:|
| AUDIO-01_NightAmbience, raíz, (0; 0; 0) | 0 | No afecta a 2D | No aplican | 0,20 |
| AUDIO-02_TV, hijo de TV, (7; 1,3; 2,4) | 1 | Linear | 1,5 / 10 | 0,35 |
| AUDIO-03_Fridge, hijo de Fridge, (1; 0,45; -19) | 1 | Linear | 1 / 6 | 0,30 |
| AUDIO-05_WindowBreeze, hijo de BedroomFurniture, (-17,6; 1,5; 3,85) | 1 | Linear | 1 / 6 | 0,25 |

Todos: Loop y Play On Awake configurados, Doppler 0, salida Ambience. **AudioSource.enabled=false mientras no haya clip y grupo asignados.** No equivalen a audio audible/verificado.

TV alcanza su zona de estar sin cubrir toda la casa; Fridge queda concentrado hacia el fondo de Kitchen; brisa cubre el sector de la abertura del dormitorio. Los radios se eligieron respecto de estas posiciones y dimensiones, y requieren ajuste perceptual con los clips reales. El Listener sigue en la cámara: la distancia se mide a ella, no al hocico. No se cambia la cámara para compensarlo.

## 3. Mixer

El usuario creó `Assets/Audio/PawsBeyondP2 .mixer` (hay un espacio antes de la extensión; se conservó) y confirmó los hijos de Master. Inventario posterior: Master, Music, SFX, Ambience y Voice.

- Los cuatro emisores nuevos están conectados a **Ambience**; Music/SFX/Voice quedan reservados, sin sonidos añadidos.
- No había fuentes previas que reencaminar.
- No se modificaron niveles ni efectos internos del Mixer. Sus faders requieren revisión manual: no se declara un nivel que no fue medido.
- Punto de partida sugerido, **no aplicado/verificado**: Master 0 dB, Ambience -12 dB; ajustar tras importar los clips. Los volúmenes de fuente son balances iniciales, no sustituyen el bus Ambience.
- La atenuación lineal reduce el nivel hacia Max Distance. Las paredes no producen oclusión automática; no se agregó un sistema acústico.

## 4. Recursos y procedencia

No había recursos sonoros reutilizables. No se descargaron, generaron ni asignaron sonidos ficticios. **Clips conectados: 0; clips temporales: 0.**

Colocar recursos propios o con licencia documentada en `Assets/Audio/Clips/Ambience/`:

| Nombre esperado (sin extensión) | Contenido y origen a documentar | Importación a evaluar al recibirlo |
|---|---|---|
| NightHouse_Loop | Ambiente nocturno doméstico sin música ni voz | 2D: conservar estéreo si aporta; Compressed In Memory o Streaming solo si duración/memoria lo justifican |
| TV_Nonverbal_Loop | Textura de TV sin diálogo inteligible | Force To Mono; loop moderado, evaluar Vorbis/Compressed In Memory |
| FridgeMotor_Loop | Motor/zumbido discreto del refrigerador | Force To Mono; clip corto puede usar Decompress On Load; comparar calidad y memoria |
| WindowBreeze_Loop | Brisa suave, sin nave/tormenta | Force To Mono; evaluar Compressed In Memory según duración |

Preferir masters WAV/AIFF editados en Audio Interactivo. Registrar autor, grabación/síntesis, fecha, edición y licencia cuando corresponda. No hay import settings modificados porque no existen clips. Frecuencia, codec, calidad, normalización y carga deben ajustarse por archivo, sin imponer Streaming a todos. WebGL necesita prueba posterior real: no se certificó audio en navegador ni se generó build.

Ventilador: descartado en esta etapa por ausencia del objeto. Crujidos: pendientes por falta de recursos; no se agregó lógica aleatoria.

## 5. Activar y validar al recibir los clips

1. Importar los cuatro recursos y documentar procedencia; comprobar loops sin clicks en la edición de audio.
2. Revisar individualmente Import Settings; fuentes 3D preferentemente mono. Aplicar/importar y confirmar canales/carga reales.
3. Asignar cada clip al AudioSource correspondiente y habilitar ese componente. Alternativa: con esos nombres exactos y escena guardada, ejecutar el helper `Tools/P2AmbientSetup.cs` mediante Pipeline `run_script`; busca un único clip por nombre y activa solo si también existe Ambience. Es herramienta de Editor fuera de Assets, no lógica del juego.
4. Revisar faders en Audio Mixer y comenzar a nivel bajo. Entrar a Play, asegurar foco y audio del Editor habilitado.
5. Recorrer Bedroom → Living → Kitchen → Bathroom; aproximarse y alejarse de TV, Fridge y ventana, escuchando la mezcla y revisando medidores. El ambiente 2D debe permanecer discreto y continuo.
6. Completar tres prendas y salir del baño sin cambiar eventos. Registrar cualquier fallo heredado sin corregir gameplay dentro de esta tarea.
7. Verificar cambios de vuelta de loop, picos/clipping, número de voces y Audio Profiler. Medir CPU/memoria cuando existan clips; no hay mediciones declaradas aquí.
8. Repetir manualmente en una build WebGL servida por HTTP y posteriormente en el hosting. Inicio de audio puede requerir interacción del usuario.

## 6. Matriz de pruebas observadas

| Prueba | Esperado | Observado | Resultado |
|---|---|---|---|
| Referencias y componentes | Sin faltantes; Player CharacterController sin Rigidbody | PolishAudit superó referencias, 16 waypoints, evento onSolved, cámara y scripts | PASS automático |
| WASD y cámara | Movimiento y Look conservados | QAControlsSmoke superó W/A/S/D y mouse horizontal/vertical | PASS automático |
| Emisores y routing | 1 fuente 2D + 3 fuentes 3D a Ambience | Cuatro fuentes configuradas, clips nulos, deshabilitadas | PASS estructura; audio pendiente |
| Listener | Un único activo | Main Camera | PASS estructura |
| Inicio de escena | Ambiente continuo | No hay clip; fuente deshabilitada | PENDIENTE recurso/escucha |
| Aproximarse al televisor | Aumento perceptible | Solo parámetros 3D verificados | PENDIENTE recurso/escucha |
| Alejarse del refrigerador | Disminución perceptible | Solo parámetros 3D verificados | PENDIENTE recurso/escucha |
| Acercarse a ventana | Brisa más perceptible | Solo posición y parámetros verificados | PENDIENTE recurso/escucha |
| Recorrido completo de audio | Sin cortes ni saturación | Sin clips; no se puede evaluar perceptualmente | NO EJECUTADA |
| Puzzle y recorrido jugable | Tres prendas, abducción, puerta y salida | P1Smoke superó tres prendas, E, abducción, puerta, salida/reentrada y recorrido completo | PASS automático |
| Audio Profiler | Voces/memoria/CPU medidos | No se midieron, no hay material sonoro | NO EJECUTADA |
| WebGL/audio físico | Reproducción correcta en navegador | No se compiló ni escuchó audio | NO EJECUTADA |

Las pruebas de teclado/mouse fueron automatizadas; no sustituyen controles físicos ni audición humana. No se alteraron informes históricos de P1.

## 7. Fundamentación: intención → sistema → alternativa → motivo → evidencia

- Continuidad nocturna → una fuente 2D en loop → repetir ambiente en cada habitación → evita multiplicar fuentes y permite continuidad → configuración aplicada; escucha pendiente.
- Reconocer TV y Fridge → AudioSources 3D en hijos sonoros de objetos existentes → audio 2D constante → aporta ubicación y distancia → posiciones, bounds y parámetros verificados; percepción pendiente.
- Relación interior/exterior → una brisa 3D junto a abertura real → emisores en todas las ventanas → un punto basta para comenzar sin sobrecargar → hueco medido en Bedroom; escucha pendiente.
- Separar categorías → Mixer Master con Music/SFX/Ambience/Voice → salida directa de cada fuente → permite mezcla global y ampliación posterior → grupos inventariados y cuatro conexiones comprobadas; faders pendientes de audición.
- Evitar audio engañoso → fuentes deshabilitadas sin clips → placeholders arbitrarios o silencio presentado como sonido → distingue preparación de implementación audible → 0 clips y 4 fuentes deshabilitadas comprobados.
- Mantener alcance → componentes nativos y helper de Editor → manager/reactividad/pooling → los loops continuos no necesitan nuevo código de juego → ningún script runtime añadido ni alterado.

## 8. Bloques A–F y próximos pasos

- A: grupos creados por el usuario y routing aplicado; balance perceptual pendiente de clips.
- D: ambiente 2D y tres emisores 3D preparados; faltan los cuatro recursos y validación auditiva.
- B, C, E y F: música adaptativa, cinco acciones SFX, voz académica y script de audio reactivo quedan para después. Voice permanece vacío.
- No se añadieron pasos, interacciones sonoras, música, voces, audio alienígena, triggers de audio, FMOD/Wwise ni dependencias.

## 9. Control de cambios

Codex: `Assets/Scenes/Playground.unity` (solo cuatro hijos sonoros), carpetas nuevas `Assets/Audio/Clips/`, `Assets/Audio/Clips/Ambience/`, `Assets/Audio/Mixers/` con sus metas, `Tools/P2AmbientSetup.cs` y este documento.

Usuario: `Assets/Audio/PawsBeyondP2 .mixer` y su meta; creación de Master/Music/SFX/Ambience/Voice. `Assets/Audio` ya existía y estaba vacío.

Durante la aplicación se comparó cada componente preexistente (excepto Transform/AudioSource) por serialización y cada Transform por posición, rotación, escala y parent: **ninguno cambió**. Se registraron hashes de Assets/Scripts, Assets/Input, Assets/Prefabs, Packages y ProjectSettings: control posterior: **0 archivos protegidos modificados**. No se modificaron los proyectos P1/original. El usuario autorizó después registrar los fuentes de P2 y la preparación de audio en Git.

## Referencias técnicas

[Unity 6: Audio Source](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AudioSource.html) y [Audio Clip](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AudioClip.html). Los valores de esta escena son decisiones iniciales del proyecto, no mediciones de escucha.

Cierre verificado: Play avanzó hasta frame 8267; 1 listener activo y 4 AudioSources, ninguna reproduciendo por falta de clips. Console final: 0 errores, 0 advertencias y sin fallo de compilación. Escena guardada, fuera de Play. El conflicto de compilación del helper por Tuple fue corregido en Tools; no afectó scripts del juego.
