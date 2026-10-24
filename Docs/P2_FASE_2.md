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
