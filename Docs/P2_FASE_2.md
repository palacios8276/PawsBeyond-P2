# Paws Beyond P2 — Fase 2 y base de Fase 3

Fecha: 24/10/2026. Base: 7782bb2; GDD v2.0 e informes Fases 0/1. Solo palacios8276/PawsBeyond-P2, main. Sin builds ni push.

## Puertas

DoorTrigger y BathroomDoorTrigger amplían el eje de aproximación X a 7,6 m (3,8 m por lado), conservando altura/anchura del acceso. DoorTriggerDetector reconoce OnTriggerStay y espera 0,6 s antes de cerrar; resorte/amortiguación de ambas puertas: 60/14. Parámetros en BoxCollider, HingeJoint y closingDelay.

Apertura hacia el mismo lado fijo y ángulo -95°. La orden de cierre del puzzle prevalece sobre presencia/demora y se conserva FreezeAll temporal de la puerta cerrada; no se altera Player ni la mecánica de las tres prendas. Ajustes de anticipación y respuesta pendientes de prueba manual: aproximación corriendo, bordes, reentrada, golpes y apertura posterior al solve.

Compilación comprobada sin errores; escena guardada. No se ejecutan smoke tests, pruebas de gameplay ni nuevos JSON de testing en esta fase.

## Ladrido normal y reforzado

B/cuadrado conservados. Intervalo mínimo: 0,25 s; ventana para segunda pulsación válida: 1,1 s. Las pulsaciones válidas forman pares: normal cian (aporte 0,2), reforzado ámbar (aporte 0,4); luego comienza otro par. Caducar la ventana inicia un normal. Una pulsación demasiado rápida no cuenta ni reinicia el par; mantener la tecla no genera repeticiones.

PlayerBark expone Origin, IsLastBarkReinforced, LastInterference y conserva onBarked. BarkWaveVfx conserva el listener y lee la intensidad del mismo ladrido; color ámbar/tamaño x1,3 configurables. DogBarkAudio usa el mismo emisor/clip con volumen x1,15 y pitch x0,9 en el reforzado; no acumula voces. Parámetros en los tres componentes. Compilación comprobada; combinación, cadencia, percepción de colores y sonido pendientes de prueba manual. Sin nuevos tests ni JSON.

## Rayo y audio

AbductionBeamController en OwnersAbductionBeam recibe onBarked una vez por ladrido. Filtra alcance 6 m hasta el volumen visible del haz y orientación horizontal máxima 70° respecto al hocico. Aportes 0,2/0,4; umbrales 0,2 / 0,55 / 0,85 para Afectado / Interferencia intensa / Descontrolado; debajo del primero, Estable. Interferencia limitada a 0–1; recupera 0,12/s después de 1,25 s sin un ladrido válido. Todo configurable.

Eventos onStateChanged y onUncontrolled sin receptores narrativos. MaterialPropertyBlock (sin modificar material compartido), luz BeamGlow y oscilación leve del mesh dan color, atenuación/titileo y pulso. No cambia posición/ruta de Owners ni levitación; sin IA ni redirecciones nuevas. Volumen y pitch evolucionan con la interferencia y vuelven gradualmente.

No había AudioSource de rayo. Se agrega AUDIO-04_AbductionBeam al grupo Ambience del PawsBeyondP2 existente, con loop mono de síntesis local provisional (2 s/22.050 Hz/PCM; generación reproducible en Tools/P2Phase2BeamSetup.cs). No usa Runway ni reemplaza audios existentes. Volumen base/máximo 0,65/0,8; pitch 1–1,35; espacialidad 3D y atenuación lineal 3–16 m. TV baja de 0,35 a 0,25 manteniendo clip y espacialidad. Mixer y NewAudioMixer ajeno sin modificaciones. El clip deja margen de amplitud; audibilidad y ausencia de clipping en la mezcla completa requieren escucha manual.

Pendiente probar manualmente: normal/ámbar dentro/fuera de ventana, mantener tecla, frente/espalda/fuera de rango, cuatro estados y recuperación, percepción de luz/mesh/audio, presencia del rayo frente a TV y convivencia con el recorrido/baño. Compilación revisada y configuración guardada; no se afirma funcionamiento probado en Play.

## Base local de episodios

EpisodeProgression en Systems: Dormitorio → Sala → Baño → Cocina → Regreso/cierre, separado de AbductionBeamState. Referencias a OwnersJourney y BathroomBasketDrop; onJourneyStarted registra el inicio y onSolved registra el baño resuelto, preservando ResumeAfterBathroom. Estos hechos no avanzan episodios. El inicio provisional P1 sigue igual.

CompleteBedroom / CompleteLivingRoom / CompleteBathroom / CompleteKitchen son puntos de conexión para futuras resoluciones; cada uno exige su episodio esperado y contenido de destino habilitado. Repetir una señal no avanza dos veces ni acepta un episodio fuera de orden. CompleteBathroom además exige solve real. RequestClosing exige regreso, contenido de cierre y petición no consumida. onEpisodeChanged y onClosingRequested quedan disponibles, sin inventar reglas de persianas/cocina.

Solo bathroomContentReady=true porque existe ese contenido; Sala, Cocina, Regreso y cierre deshabilitados. Por eso el recorrido físico provisional P1 puede continuar mientras el estado narrativo permanece en Dormitorio: todavía no se implementa el tutorial ni se declara completado ningún episodio futuro. No se conecta onUncontrolled a transiciones. Pendiente validar manualmente el registro de hechos; las transiciones completas se integrarán con sus contenidos reales.

## Relevamiento acotado de Level Design

Coordenadas mundiales aproximadas (X; Y; Z). Los huecos se deducen de las piezas existentes; falta validar paso visual/colliders y cámara. No se crearon huecos, mecanismos ni marcadores.

| Sector | Identificado / candidato |
|---|---|
| Dormitorio | Hueco entre BRWall_west (2)/(3), antepecho (1) y dintel: centro aprox. (-17,57; 1,58; 3,98). Posible soporte interior cerca de (-17,57; 1; 3,4); causalidad del juguete aún por concretar. |
| Sala | No se identificaron dos ventanas existentes: los muros consultados son cajas cerradas (24 vértices/12 triángulos). Candidatas, no construidas: funcional en pared x≈13,54, z≈7,5 (sector próximo al baño pero fuera de su recinto); bloqueada en pared z≈11,43, x≈5,4. Requieren aprobar huecos, muebles y visibilidad. |
| Cocina A/B | Huecos enfrentados en x≈10,85 y x≈-0,54, ambos z≈-15,7; centros verticales aprox. 1,49 y 1,26 respectivamente. Posibles mecanismos interiores: delantal cerca de x≈10,3 y caja cerca de x≈0; ubicaciones por validar. |
| Cocina–Sala | Abertura sin puerta en z≈-10,84, x≈3,0–7,3, dintel a y≈2,15. Isla actual (6; 0,55; -16) y mesada (6; 1,1; -19,7); no hay una mesa junto a la abertura identificada. Zona candidata para mesa/distracción cerca de (6,5; altura por definir; -12,5), sin bloquear acceso. |
| Circulación y regreso | Cocina → abertura (5,3; -10,84 en X/Z) → Sala (5,3; -0,2) → pasillo (-7,36; -0,09) → puerta dormitorio (-12; -0,07) → dormitorio (-17,4; -0,3). Referencia de trayecto, no ruta final creada ni verificada en Play. |

Techo pendiente para otra etapa: material URP, iluminación interior, sombras, espacio/colisión de cámara y estrategia de visibilidad/ocultación desde tercera persona antes de modelarlo. No se reconstruye la casa ni se agrega exterior transitable.

## Cierre y pendientes

Compilación final sin errores informados, conexiones de los componentes nuevos revisadas sin referencias rotas detectadas y escena guardada fuera de Play. No se ejecutó gameplay, no se generaron JSON de testing ni builds. La validación manual comprende anticipación/contactos de puertas, bloqueo del baño con tres prendas, pares de ladridos, rango/orientación del haz, recuperación y mezcla sonora. Los resultados de Fase 1 no certifican estos cambios nuevos.

Se preservan NewAudioMixer.mixer y su meta preexistentes sin registrar; quedan fuera de los commits. Sin cambios en movimiento/cámara/interacción de prendas, OwnersJourney, BathroomBasketDrop, prefabs, Input Actions, Mixer existente, paquetes ni ProjectSettings. Sin push.
