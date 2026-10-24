# Paws Beyond P2 — Fase 3

Fecha: 24/10/2026. Proyecto: `palacios8276/PawsBeyond-P2`, rama `feature/p2-fase-3`. Escena: `Assets/Scenes/Playground.unity`. Base: `6b91855`.

## 1. Resumen de cambios

Tutorial local del dormitorio: desplazamiento, observación, tomar/soltar dos juguetes en orden, ladrido y par reforzado cerca de los dueños. Las ayudas uGUI/TMP duran 8 s por paso y se ocultan al salir del dormitorio o completar el tutorial; volver permite recordar el paso pendiente. Solo el juguete correspondiente recibe resaltado. Los controles existentes se conservan: WASD/stick izquierdo, mouse/stick derecho, E/cruz para interacción y B/cuadrado para ladrido.

Tras el par válido, los dueños reaccionan brevemente, se activa el rayo y `BeginJourney` registra el comienzo. El inicio automático provisional queda deshabilitado. El recorrido se mantiene pausado mientras el tutorial mueve suavemente a los dueños hacia la ventana. Cápsulas horizontales sobre la cama, identificadas como placeholders; oscilación leve y orientación fija, sin Animator.

El tercer juguete permanece visible pero trabado hasta la aproximación del rayo. La aclaración del usuario permite usar una silla y un amarre: el juguete actúa como traba; retirarlo 0,4 m libera el sostén. El cordón es una representación greybox, sin simulación de cuerda. La silla permanece fija. El panel baja por gravedad, guiado por restricciones de Rigidbody, y se traba al llegar al antepecho. Solo el cierre físico registrado completa Dormitorio y libera el recorrido. Ladrar o recuperar estabilidad del rayo no cierra ni reabre la persiana.

Sala: dos ventanas locales en muros existentes; funcional cerca del baño y alternativa previamente bloqueada según el GDD v2.0. Se conservan objetos/IDs originales de los muros, desactivando únicamente su renderer y collider y reemplazando esos paños por segmentos con Collider. No se reconstruye el ambiente. La ventana funcional conserva un panel de vidrio greybox opaco con Collider para impedir escapes del perro; no es arte definitivo.

Los dueños esperan ante la ventana funcional; aparecen una silueta guionada sin Collider/IA y una luz. Una llegada real a Descontrolado libera el intento hacia la alternativa bloqueada. Después de 1,5 s ante ella, el episodio pasa a Baño y continúa el recorrido previo. El evento solo ocurre una vez. No se exige otro puzzle de objetos en sala.

TV: usa el mismo AudioSource, clip, grupo Ambience y volumen máximo 0,25. Su reproducción y volumen dependen del rayo activo y de la distancia al volumen visible del haz. Sin fuentes ni clips nuevos. El clip de TV sigue siendo provisional: comprobar por escucha si comunica adecuadamente la estática prevista.

El baño conserva la condición conjunta dueños/perro, la puerta física y las tres prendas. No se modifican sus scripts, prefab de prendas ni lógica de resolución. Cocina/cierre no se implementan; se conserva el recorrido P1 posterior ya existente, sin habilitar su progresión narrativa futura.

## 2. Componentes y archivos

Nuevos:
- `Assets/Scripts/Systems/BedroomTutorial.cs`: pasos, ayudas, resaltado y comienzo de abducción.
- `Assets/Scripts/Systems/BedroomShutter.cs`: traba por desplazamiento de juguete, caída física y cierre único.
- `Assets/Scripts/Systems/LivingRoomEpisode.cs`: intentos de ventana y transición hacia baño.
- `Assets/Scripts/Audio/TVBeamInterference.cs`: proximidad real, actividad y fundido de la fuente existente.
- `Assets/Prefabs/Tutorial/BedroomToy.prefab`: Rigidbody, SphereCollider y BasketDrag, usado por los tres juguetes.
- Tres materiales URP provisionales para ventanas, bloqueo y silueta.
- Recursos esenciales oficiales de TextMeshPro, importados desde el paquete uGUI instalado para las ayudas y caracteres en español. No se importan ejemplos.
- `Tools/P2Phase3BedroomSetup.cs` y `Tools/P2Phase3LivingSetup.cs`: configuración de autoría mediante APIs de Unity. Se niegan a repetir la creación sobre objetos ya configurados. No son pruebas ni se ejecutan en runtime.

Ajustados:
- `PlayerInteraction`: eventos de agarre y liberación después de la acción existente; mismo Raycast/Interact.
- `OwnersJourney`: pausa narrativa y evento tipado al alcanzar un waypoint; conserva puertas y espera del baño.
- `AbductionBeamController`: actividad explícita para mantenerlo apagado antes del evento; interferencia independiente de episodios.
- `OwnerFloat`: reacción vertical breve, conservando orientación.
- `Playground.unity`: raíces BedroomEpisode/LivingRoomEpisode, ayudas, silla/persiana, tres juguetes, dos ventanas, cues y referencias serializadas. EpisodeProgression habilita contenido de Sala.

No se alteran Input Actions, Cinemachine, CharacterController, puertas, paquetes, ProjectSettings ni Mixer existente. Player continúa sin Rigidbody. Solo P2.

## 3. Inspector y ubicación

- BedroomTutorial: referencias de Player, cámara, interacción, ladrido, juguetes/renderers, persiana, recorrido, rayo, progresión, dueños, texto/panel; límites del dormitorio, duración de ayudas, umbral de movimiento/giro, velocidad de abducción, alcance de reacción, color y eventos únicos.
- BedroomShutter: juguete/Rigidbody de panel/cordón, desplazamiento de liberación, altura/tolerancia de cierre y onClosed.
- OwnerFloat: distancias/frecuencia de oscilación y altura/duración de reacción.
- LivingRoomEpisode: referencias, índices de waypoint 4/5, demora del intento bloqueado, luces/silueta y eventos.
- TVBeamInterference: rayo, Renderer, distancias 1–5 m y fundido 0,5 s. El AudioSource existente conserva el techo 0,25 y Ambience.
- Audio/luz/partículas: se reutilizan el loop, feedback y ondas de Fases 1/2. Nuevas luces de ventana son componentes Light configurables. Sin corte de Cinemachine: no se justifica quitar control de cámara en esta iteración.

Posiciones aproximadas:
- Persiana dormitorio: x=-17,57, z=3,8; panel abierto y=3,12, centro cerrado y≈1,58. Guía inferior interior sostiene el panel sin atravesar el dintel existente.
- Silla: x=-17,57, z=2,8; juguete-traba delante, z=2,1.
- Ventana funcional sala: x≈13,54, z=7,5; alternativa bloqueada: x=5,4, z≈11,43. Ambas: ancho 2,4 m, antepecho 0,9 m, dintel 2,2 m.
- Waypoints 4/5: (11,5; 1,6; 7,5) y (5,4; 1,6; 9,6). Los otros puntos y accesos no se cambian.

Se inspeccionaron bounds de paredes y sofá antes de cortar los dos paños: no coinciden con puertas ni con el recinto del baño. Circulación efectiva, lectura y cámara requieren prueba manual.

## 4. Verificaciones técnicas realizadas

- Compilación del código: completada, sin errores informados.
- Configuración de ambos bloques mediante run_script: completada sin errores de compilación/ejecución de autoría.
- Referencias serializadas principales de los cuatro componentes nuevos: presentes; listeners enlazados a métodos existentes.
- Recorrido completo de jerarquía para scripts faltantes: ninguno detectado.
- 10 AudioSources antes/después, una sola Canvas; no se duplica la fuente del televisor.
- Player con CharacterController y sin Rigidbody; volumen TV 0,25 y grupo Ambience comprobados.
- Escena guardada mediante save_scene; no se pierde su GUID. La copia de comparación posterior coincide con lo guardado.
- Console consultada sin limpiarla: 0 errores. Advertencia existente/recurrente de Unity AI: Account API inaccesible por conexión/foco; no corresponde a los scripts de gameplay.

No se ejecutó Play, smoke tests, pruebas físicas/virtuales, builds ni informes JSON de testing. Estas verificaciones no certifican gameplay.

Se preservan fuera de los commits NewAudioMixer.mixer/.meta y dos ajustes de audio que ya estaban sin guardar (volumen 0,42 y prioridad 118 en un ambiente). Por eso la escena puede seguir figurando modificada en Git solo por esos valores. Los commits mantienen sus valores previos. No se hace merge ni push; main no se modifica.

## 5. Commits

- `47d453e` — Incorpora el tutorial de juguetes y la persiana del dormitorio.
- `af1f8b9` — Conecta las ventanas de la sala con el recorrido hacia el baño.
- Commit de este informe — Documenta la integración y las pruebas manuales de Fase 3.

## 6. Lista breve de pruebas manuales pendientes

1. Desde inicio: WASD/mando y mouse/cámara; observar que ayudas avanzan y desaparecen, sin inicio automático de abducción.
2. Juguete 1 y luego 2: detectar con Raycast, E/cruz, transportar/soltar; recoger el segundo antes de tiempo no debe saltear el primero. Probar soltar por distancia y salir/volver al dormitorio.
3. B/cuadrado: aislado no inicia la abducción; un par válido cerca de los dueños sí. Mantener botón no repite; sonido/ondas normales y reforzadas conservados. Dueños horizontales y reacción breve sin giro.
4. Tercero: inaccesible antes del evento, luego tomar/tirar del amarre. La persiana baja sin engancharse, queda cerrada y permite la transición; soltar/reagarrar y ladrar no deben reiniciar ni sustituir el cierre.
5. Seguir a los dueños: paso por puerta y cámara, TV solo audible cerca del rayo, silueta/luces legibles, espera frente a ventana funcional; lograr Descontrolado una vez y observar intento bloqueado y salida hacia baño.
6. Llegar antes/después al baño: no caer cesto sin ambas presencias. Entrar, transportar las tres prendas y verificar que 1/2 no resuelven; tercera libera puerta y se retira el cesto como antes.
7. Recorrer fuera de orden y volver a sala/dormitorio: no repetir tutorial, intentos ni disparadores consumidos; no salir por ventanas o laterales de puertas. La TV puede volver a responder a proximidad real sin reiniciar el episodio.
8. Revisar Console durante toda la pasada y ajustar desde Inspector tiempos, alcance, ayudas, mezcla y cámara. No integrar a main hasta la aprobación manual.

## 7. Provisional y riesgos para Fase 4

- Confirmar en Play que el tirón, gravedad, guía y cierre se lean bien y no se traben; el cordón visual no es una simulación física ni una animación final.
- Cápsulas, silla, silueta, ventanas/persiana y audio del rayo/TV siguen siendo recursos greybox/provisionales. No hay IA ni animaciones definitivas.
- Validar claridad de ventanas, colliders y cámara, especialmente el panel de vidrio opaco y el corredor detrás del sofá. Posiciones configuradas según geometría real, pendientes de aceptación jugable.
- Afinar ritmo y mezcla con escucha manual; una sola secuencia de sala, sin dependencia de que la interferencia permanezca alta después del evento.
- Recién después de esta regresión: planificar la siguiente fase con reglas del baño evolucionado/cocina, sin implementarlas aquí. Mantener el cierre del dormitorio persistente y la futura progresión separada del rayo.

## Ajuste posterior de mobiliario gris

A pedido del usuario: inodoro girado 90° hacia el centro del baño, con mochila próxima a pared este y asiento greybox; se agrupan sus piezas conservando objetos y referencias existentes. Lavamanos desplazado 0,5 m hacia su pared y bañera 0,65 m hacia el muro sur. Cocina: mesa gris y dos sillas cerca del acceso, fuera del eje de circulación x=5,3 y sin tapar las ventanas. Se reutilizan materiales grises existentes, sin texturas, mecánicas ni puzzles nuevos.

Archivos: Playground.unity y Tools/P2GreyboxFurnitureSetup.cs (autoría, con guardia contra duplicaciones). La configuración compiló y se ejecutó correctamente en el Editor; comprobación de bounds: ningún solape de los sanitarios ajustados con muros del baño. Console mantiene 0 errores. Sin Play: paso con Player/cámara, lectura de muebles y comodidad del espacio pendientes de la pasada manual. Se preservan fuera del commit los ajustes previos de audio y NewAudioMixer.

Commit: Ajusta el baño y completa el mobiliario gris de cocina.
