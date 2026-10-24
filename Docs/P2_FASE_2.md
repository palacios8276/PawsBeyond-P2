# Paws Beyond P2 — Fase 2 y base de Fase 3

Fecha: 24/10/2026. Base: 7782bb2; GDD v2.0 e informes Fases 0/1. Solo palacios8276/PawsBeyond-P2, main. Sin builds ni push.

## Puertas

DoorTrigger y BathroomDoorTrigger amplían el eje de aproximación X a 7,6 m (3,8 m por lado), conservando altura/anchura del acceso. DoorTriggerDetector reconoce OnTriggerStay y espera 0,6 s antes de cerrar; resorte/amortiguación de ambas puertas: 60/14. Parámetros en BoxCollider, HingeJoint y closingDelay.

Apertura hacia el mismo lado fijo y ángulo -95°. La orden de cierre del puzzle prevalece sobre presencia/demora y se conserva FreezeAll temporal de la puerta cerrada; no se altera Player ni la mecánica de las tres prendas. Ajustes de anticipación y respuesta pendientes de prueba manual: aproximación corriendo, bordes, reentrada, golpes y apertura posterior al solve.

Compilación comprobada sin errores; escena guardada. No se ejecutan smoke tests, pruebas de gameplay ni nuevos JSON de testing en esta fase.
