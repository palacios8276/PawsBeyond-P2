# PAWS BEYOND P2 — INFORME DE AUDIO INTERACTIVO

Fecha: 21/10/2026. Proyecto: PawsBeyond-P2, Unity 6000.0.81f1. Repositorio: palacios8276/PawsBeyond-P2, branch main. Carpeta: `C:/Users/JUNIN/Unity Projects/PawsBeyond-P2/PawsBeyond-P2`. Escena: `Assets/Scenes/Playground.unity`.

## A. Resumen de intervención

**Objetivo:** mejorar la presencia de la heladera y sumar identidad al baño y al perro sin cambiar la jugabilidad.

**Inspección inicial real:** cuatro AudioSources activos: ambiente nocturno 2D, TV 3D, heladera 3D y brisa 3D. Un único AudioListener, en Main Camera. Mixer con Master/Music/SFX/Ambience/Voice. Git limpio al comenzar, commit de referencia bb887c8. Todos los clips previos son temporales generados con Runway.

Se inspeccionaron el Player con CharacterController, PlayerMovement, PlayerInteraction, OwnersJourney, las dos cápsulas de dueños, TV, Fridge, SinkBasin, ToiletBase/ToiletTank y TubBase. Hay una bañera; no se encontró una ducha activa que justifique agua corriendo.

**Mejorado:** heladera con aumento moderado de volumen y región cercana más amplia para el Listener de tercera persona. **Incorporado:** goteo 3D y pasos independientes con tres variantes. **Preparado:** ladrido con clip y método público, sin evento; dos fuentes de ronquido deshabilitadas, sin clips ni estado de sueño. **Conservado:** TV, brisa, ambiente general, Mixer y todos los sistemas jugables.

**Aclaración importante:** no existe AudioSource ni AudioClip de maquinaria/rayo en esta copia. El usuario indicó que le pareció escucharlo; se verificaron las cuatro fuentes existentes y no se atribuyó esa percepción a una de ellas sin evidencia. Los rayos siguen siendo visuales. No se inventó un emisor ni se subió la brisa como sustitución.

## B. Inventario sonoro actualizado

Los IDs de esta tabla corresponden al segundo pedido; los nombres de GameObject anteriores se conservan aunque su numeración difiera.

| ID | Sonido | Objeto/ubicación | AudioClip | 2D/3D | Grupo | Reproducción | Estado | Origen |
|---|---|---|---|---|---|---|---|---|
| AMB-01 | Casa nocturna | AUDIO-01_NightAmbience, raíz | NightHouse_Loop.wav | 2D | Ambience | Loop | Implementado previo, conservado | Runway, temporal |
| AUDIO-01 | Heladera | AUDIO-03_Fridge, hijo de Fridge | FridgeMotor_Loop.wav | 3D | Ambience | Loop | Implementado y ajustado | Runway, temporal |
| AUDIO-02 | TV | AUDIO-02_TV, hijo de TV | TV_Nonverbal_Loop.wav | 3D | Ambience | Loop | Implementado previo, intacto | Runway, temporal |
| AUDIO-03 | Maquinaria/rayo | Ninguna fuente sonora identificada | Ninguno | — | — | — | Pendiente de definición | No disponible |
| AUDIO-04 | Brisa | AUDIO-05_WindowBreeze, abertura de Bedroom | WindowBreeze_Loop.wav | 3D | Ambience | Loop | Implementado previo; atenuación medida | Runway, temporal |
| AUDIO-05 | Goteo | AUDIO-05_SinkDrip, hijo de SinkBasin | SinkDrip_Loop.wav | 3D | Ambience | Loop con intervalos dentro del clip | Implementado | Runway, temporal |
| AUDIO-06 | Inodoro | ToiletBase/ToiletTank existentes | Ninguno | — | — | Ninguna | Descartado en esta etapa: no hay uso ni evento justificante | — |
| AUDIO-07 | Ducha | Bañera TubBase; sin ducha activa observada | Ninguno | — | — | Ninguna | Sin agua continua; opcional futuro | — |
| AUDIO-08 | Patitas | AUDIO-08_DogPaws, hijo del Player | DogPaw_01/02/03.wav | 3D | SFX | Desplazamiento real | Implementado | Runway, tres variantes temporales |
| AUDIO-09 | Ladrido | AUDIO-09_DogBark, hijo del Player | DogBark_01.wav | 3D | SFX | PlayBark(), sin evento conectado | Preparado, no funcional narrativamente | Runway, temporal |
| AUDIO-10 | Ronquidos A/B | AUDIO-10_Sleep_Owner_A_Capsule y AUDIO-10_Sleep_Owner_B_Capsule | Ninguno | 3D | Ambience | Ninguna; fuentes deshabilitadas | Preparado, sin recurso ni estado | — |

**Recursos y reemplazo:** ambientes activos en `Assets/Audio/EnUso/Ambientes/`; pasos en `Assets/Audio/EnUso/Efectos/`; ladrido pendiente de evento en `Assets/Audio/Pruebas/Efectos/`. Todos los nuevos WAV son mono, 44100 Hz, PCM16 de origen. Goteo importado Vorbis 0,70/Compressed In Memory; pasos y ladrido PCM/Decompress On Load. Duraciones observadas: goteo 10,038 s; pasos 0,300/0,214/0,300 s; ladrido 1,407 s.

Originales MP3 nuevos, prompts, IDs y huellas SHA-256: `AudioSources/Originales/Runway/SegundaIntegracion/`. Son IA generada a pedido del usuario, no grabaciones propias. [Condiciones de uso de Runway consultadas](https://help.runwayml.com/hc/en-us/articles/18927776141715-Usage-rights). Los prompts solicitaron sonidos apropiados sin música ni voces humanas; confirmar el resultado semántico requiere escucha humana. La conversión a WAV no recupera calidad perdida en MP3. Los clips son reemplazables por exportaciones de REAPER conservando sus .meta.

## C. Configuración técnica

Todas las fuentes están en Playground, no en prefabs. Las 3D usan Linear Rolloff y Doppler 0. Sus distancias se miden al Listener de Main Camera, no al hocico. La atenuación no produce oclusión de paredes.

| GameObject / AudioSource | Volumen | Spatial Blend | Min/Max | Loop | Play On Awake | Enabled | Salida | Script |
|---|---:|---:|---|---|---|---|---|---|
| AUDIO-01_NightAmbience | 0,20 | 0 | No aplican en 2D | Sí | Sí | Sí | Ambience | Ninguno |
| AUDIO-02_TV | 0,35 | 1 | 1,5/10 | Sí | Sí | Sí | Ambience | Ninguno |
| AUDIO-03_Fridge | 0,45 | 1 | 2,5/7 | Sí | Sí | Sí | Ambience | Ninguno |
| AUDIO-05_WindowBreeze | 0,25 | 1 | 1/6 | Sí | Sí | Sí | Ambience | Ninguno |
| AUDIO-05_SinkDrip | 0,22 | 1 | 1,2/5,5 | Sí | Sí | Sí | Ambience | Ninguno |
| AUDIO-08_DogPaws | 0,45 base ±6% | 1 | 2,5/8 | No | No | Sí | SFX | DogFootstepAudio |
| AUDIO-09_DogBark | 0,45 | 1 | 3/12 | No | No | Sí | SFX | DogBarkAudio, sin señal conectada |
| AUDIO-10_Sleep_Owner_A_Capsule | 0,12 | 1 | 1/5 | Sí configurado | No | No | Ambience | Ninguno |
| AUDIO-10_Sleep_Owner_B_Capsule | 0,12 | 1 | 1/5 | Sí configurado | No | No | Ambience | Ninguno |

Heladera: antes volumen 0,30, Min 1 y Max 6; después 0,45, Min 2,5 y Max 7. El aumento de ganancia de fuente equivale a aproximadamente +3,52 dB; además la zona sin atenuación se amplía. No es una medición auditiva antes/después. Cocina medida previamente: 11,26 × 9,70 unidades. TV y brisa no cambiaron de posición, nivel, curva ni clip. El ambiente existente ya aporta continuidad; no se duplicó con otra capa de sala.

Goteo: posición (21,5; 1,18; 3,8), sobre la cubeta del lavabo existente. Pasos: hijo del Player a 0,1 unidades de altura. Ladrido: hijo a 0,4. Ronquidos preparados: hijos de las cápsulas existentes, siguiendo sus transformaciones sin alterar su movimiento.

Mixer reutilizado: `Assets/Audio/Mezcladores/PawsBeyondP2.mixer`. Master con Music, SFX, Ambience y Voice. Sin Mixer nuevo, faders alterados, filtros, snapshots ni gestión personalizada de voces. Voice permanece sin grabaciones.

## D. Scripts de audio

### DogFootstepAudio

- Ruta: `Assets/Scripts/Audio/DogFootstepAudio.cs`, componente en AUDIO-08_DogPaws.
- Observa el desplazamiento horizontal real del Transform del CharacterController en LateUpdate y lee isGrounded. No lee Input, no llama a Move y no escribe velocidad, posición ni estados jugables.
- Inspector: controller, audioSource, variants (tres clips), stepDistance 0,8; minimumInterval 0,16 s; minimumSpeed 0,12; pitchRange 0,95–1,05.
- Acumula distancia y reproduce un contacto; espera a que termine la fuente antes de otro. No dispara audio nuevo cada frame ni acumula PlayOneShot.
- En reposo, caída o traslado brusco de más de 2 unidades/frame descarta distancia. No repite inmediatamente la misma variante si hay más de una.
- Usa System.Random propio, evitando alterar el estado aleatorio global de Unity. Variación de pitch ±5% y volumen ±6%. PlayedSteps permite diagnosticar disparos.

### DogBarkAudio

- Ruta: `Assets/Scripts/Audio/DogBarkAudio.cs`, componente en AUDIO-09_DogBark.
- Inspector: audioSource y variants (un clip temporal).
- Método público PlayBark(): reproduce una variante si no está sonando otra. No tiene Update, Input ni activación automática.
- No existe acción de ladrar ni interacción para despertar dueños en PlayerInteraction/Input. OwnersJourney.onJourneyStarted corresponde a observación/recorrido, no a un intento de ladrar: no se conectó artificialmente.
- Dependencia pendiente: definir/autorizar un evento real de ladrido o interacción. Este pedido no modifica scripts jugables para agregarlo.

### Herramientas fuera del juego

`Tools/P2PrepareSecondAudio.cs` decodifica originales, genera WAV e importa por uso. Recorta patitas a un máximo de 0,30 s, aplica fades breves; goteo con crossfade circular de 150 ms. No sobrescribe recursos existentes. Los picos PCM se limitan mediante ganancia a 0,45, sin afirmar balance perceptual. `Tools/P2SecondAudioSetup.cs` aplica la configuración y compara serialización de scripts previos y fuentes conservadas. `Tools/P2SecondAudioSmoke.cs` ejecuta pruebas de audio transitorias en Play. Estas herramientas no están en Assets ni se compilan en el juego.

## E. Fundamentación

1. **Presencia cercana de heladera →** aumento moderado de fuente y Min Distance 2,5 → subir todo Ambience → la cámara está separada del perro y un radio cercano pequeño atenúa demasiado pronto → parámetros guardados y señal medida decreciente cerca/intermedio/lejos. Escucha perceptual final pendiente.
2. **Reconocer lavabo →** una fuente 3D con un loop de goteo espaciado → simulación de gotas o varias fuentes → el componente nativo resuelve ubicación y repetición sin lógica extra → clip asignado, mono y prueba de atenuación real. Naturalidad al oído pendiente.
3. **Feedback del movimiento →** observar distancia real y contacto con suelo → usar teclas o modificar PlayerMovement → evita pasos por mera intención de avanzar, reposo o giro y mantiene el controlador intacto → cinco pasos en la prueba WASD, sin nuevos disparos al detenerse o girar quieto.
4. **Ladrido narrativo →** componente preparado sin activación → tecla nueva o ladrido en onJourneyStarted → esas alternativas inventarían una acción o una causalidad → inspección de Input/PlayerInteraction/OwnersJourney y prueba sin ladrido automático.
5. **Dueños dormidos →** fuentes preparadas y deshabilitadas → interpretar HasStarted=false como sueño → no existe estado de sueño verificable; las cápsulas ya flotan y el flag controla el recorrido → no se asume sueño ni se agregan ronquidos automáticos.
6. **Continuidad doméstica →** conservar ambiente 2D existente y TV localizada → superponer más ambientes en Living → evita duplicar textura y ganancia sin evidencia de necesidad → fuentes originales sin cambios, ninguna capa general nueva.

## F. Pruebas

Pruebas automatizadas de Editor; no equivalen a escucha física ni validación en WebGL. Durante la medición de atenuación se aisló temporalmente cada fuente con mute y se movió el Listener de la cámara en Play, deshabilitando transitoriamente el Brain para impedir seguimiento. El finally restaura mute y cámara; esos cambios de prueba no se guardan.

| Escenario | Esperado | Observado | Estado | Corrección |
|---|---|---|---|---|
| Reposo del perro | Sin nuevos pasos | Conteo estable | PASS automático | — |
| WASD real | Pasos acompañan desplazamiento | Cinco disparos y desplazamiento real | PASS automático | — |
| Detenerse / girar quieto | No disparar nuevos pasos | Conteo estable en ambos casos | PASS automático | — |
| Variantes | Tres clips intercambiables, sin repetición inmediata | Tres referencias asignadas; exclusión lógica de variante previa | Estructura verificada; timbre pendiente de oído | — |
| Heladera 1/3,5/9 unidades | Menor señal con distancia | RMS 0,02537 / 0,01697 / 0 | PASS técnico | Volumen y radios ajustados |
| Brisa 1/3,5/7 unidades | Menor señal con distancia | RMS 0,01432 / 0,00759 / 0 | PASS técnico | No se alteró |
| Lavabo 1/3,5/7 unidades | Goteo cerca y silencio lejos | RMS 0,000799 / 0,000312 / 0; pico cercano 0,0577 | PASS técnico | Fuente nueva |
| TV | Conservar configuración | Serialización de AudioSource idéntica durante aplicación | PASS estructura; escucha posterior pendiente | No se alteró |
| Ladrido | No sonar sin evento real | Sin reproducción automática | PASS preparado; integración narrativa pendiente | No se agregó tecla |
| Ronquidos | No asumir sueño | Dos fuentes sin clip y deshabilitadas | Preparado, no audible | No se agregó estado |
| Maquinaria | Ajustar solo emisor identificado | No existe fuente en esta copia | NO EJECUTADA / dependencia pendiente | No se creó sustituto |
| Inodoro/ducha | No sonar como uso inexistente | Sin descarga ni agua permanente | Descarte de diseño | — |
| Clipping | Picos menores al límite en muestra | Máximo aislado medido 0,0638 | PASS técnico aislado; mezcla completa pendiente | Sin ganancia global |
| Gameplay P1 | Baño y recorrido intactos | P1Smoke superó tres prendas, E, abducción, puerta libre, salida/reentrada y recorrido completo con teclado virtual | PASS automático | Ningún cambio jugable |
| Console final | Sin errores relevantes | 1 error y 1 advertencia agrupados: CLI/Pipeline y cuenta Unity; sin fallo de compilación | INCIDENCIAS DE INFRAESTRUCTURA | No se tocaron paquetes |
| Escucha completa y WebGL | Naturalidad, balance, rendimiento | No ejecutados | PENDIENTE MANUAL | — |

Evidencia numérica: `Docs/P2_AUDIO_SEGUNDA_PRUEBAS.json` y `Docs/P2_AUDIO_SEGUNDA_RECURSOS.json`. Son amplitudes lineales de buffers de salida, no mediciones LUFS ni del Audio Profiler. No se midió CPU o memoria del Audio Profiler. Tampoco se afirma ausencia de clicks por oído ni de saturación en todo el recorrido.

## G. Requisitos del Parcial 2

| Bloque | Estado real | Evidencia | Falta |
|---|---|---|---|
| A — AudioMixer | Cumplido técnicamente en estructura/routing | Un Mixer, cinco grupos y fuentes ruteadas | Revisión perceptual de mezcla final |
| B — Música adaptativa | Pendiente | No se implementó música | Dos estados musicales y transición autorizada |
| C — Cinco acciones SFX, dos con variaciones | Parcial | Una acción operativa: pasos, con tres variantes. Ladrido solo preparado | Cuatro acciones operativas más; segunda acción con variantes. No contar las tres patitas como tres acciones |
| D — Ambiente loop + tres fuentes 3D | Cumplido técnicamente | Ambiente 2D y TV/heladera/brisa/goteo 3D con clips | Escucha humana y recursos definitivos |
| E — Voz humana propia editada | Pendiente | No hay grabaciones propias | Grabación y edición del estudiante. Ladridos y ronquidos no cuentan |
| F — Script propio reactivo al gameplay | Implementado técnicamente | DogFootstepAudio observa movimiento y modifica reproducción | Comprensión y fundamentación del estudiante, validación docente |

Los estados técnicos no certifican una nota ni reemplazan la consigna final de Audio Interactivo.

## H. Control de cambios

**Agregados:** dos scripts runtime exclusivamente sonoros con sus metas; goteo, tres patitas y un ladrido WAV con metas; originales MP3 nuevos con procedencia/huellas; carpetas EnUso/Efectos y Pruebas/Efectos; herramientas de preparación, configuración y pruebas; evidencia JSON y este informe.

**Modificados:** Playground (solo audio nuevo y parámetros de heladera); Tools/P2AmbientSetup.cs (nuevos valores de heladera para no revertirlos al reejecutar); README y guía de organización para referenciar este informe.

**Componentes:** un AudioSource de goteo, uno de pasos y DogFootstepAudio, uno de ladrido y DogBarkAudio, dos AudioSources de ronquido deshabilitados. La heladera existente se ajustó; no se duplicó. No se alteraron TV, brisa, ambiente general ni Mixer.

**Git:** raíz y remoto P2 verificados; sin cambios preexistentes al comenzar. Se registrará esta intervención en un commit separado con título y descripción en español, sin push. El listado exacto de cambios queda en el commit. Informes históricos de pruebas preservados.

**Límites/dependencias:** falta identificar o autorizar audio del rayo; no existen evento de ladrar/despertar ni estado de sueño. No se añadieron música, voces humanas, filtros complejos, snapshots, sistemas de superficies ni cambios narrativos. P1 y el proyecto original no fueron intervenidos.

La configuración comparó los MonoBehaviours jugables preexistentes antes y después, sin diferencias. Verificación final: ningún archivo de las carpetas jugables previas, Input, prefabs, Packages, ProjectSettings o Mixer presenta diferencias frente a bb887c8. La auditoría PolishAudit pasó: sin scripts faltantes, referencias correctas, 16 waypoints, evento onSolved y Player sin Rigidbody. No se modificó movimiento, cámara, Input System, colliders, Rigidbody, interacción, puzzle ni eventos narrativos. Las modificaciones temporales de cámara usadas para medir audio se limitaron a Play y se restauraron.

## I. Próxima iteración recomendada — no implementada

1. Escucha con el usuario del recorrido completo; ajustar heladera, goteo y nivel relativo de patitas sin subir todo el Mixer.
2. Mejorar los WAV en REAPER y grabar patitas propias; comparar las tres variantes y la naturalidad del goteo.
3. Definir primero la acción narrativa de ladrar/despertar y el estado de sueño, antes de conectar ladridos/ronquidos.
4. Definir el sonido y el momento del rayo si se desea incorporarlo como una intervención nueva.
5. Planificar las acciones SFX restantes, una segunda con variantes, la voz humana propia y los dos estados musicales.

### Cierre de pruebas y limitaciones de infraestructura

- El primer P1Smoke y la repetición QAControlsSmoke fallaron al inyectar W sobre el teclado nativo con el Editor sin foco. No se modificó el controlador para hacerlo pasar.
- En una sesión nueva, con un teclado virtual temporal identificado P2QA_VirtualKeyboard, P1Smoke completó el flujo en 92,33 s. Ese dispositivo fue retirado y se salió de Play. Esto comprueba entrada simulada y regresión automatizada; no una prueba física de controles.
- Console al cierre mostró 1 error y 1 advertencia agrupados. El buffer retenía 10 eventos «No command named editor status», 2 timeouts de 4000 ms de Pipeline y 5 avisos «Account API did not become accessible within 30 seconds». Corresponden a infraestructura CLI/Pipeline/Unity Account; no se detectó una excepción de los componentes de audio ni fallo de compilación. No se borró Console para ocultarlos.
- Se recargó desde disco la escena de audio guardada antes de Play, descartando únicamente estado transitorio de las pruebas; quedó fuera de Play y sin modificaciones pendientes en la escena del Editor.
- No se certifican escucha física, naturalidad del material generado, balance integral, controles físicos ni audio WebGL. No se generó una build.