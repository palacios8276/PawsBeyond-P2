# Paws Beyond P2

Vertical slice académico 3D desarrollado en **Unity 6 con URP** para el Segundo Parcial de Motores de Desarrollo 1. Es una evolución independiente de PawsBeyond-P1.

**Versión:** `0.7.3-playtest`  
**Unity:** `6000.0.81f1`  
**Rama de entrega:** `main`

## Contenido principal

- Recorrido narrativo dentro de la vivienda con cámara en tercera persona.
- Tutorial, interacción física mediante Raycast y puzle del baño con tres prendas.
- Secuencia de abducción, ladridos reactivos y presentación audiovisual.
- Encuentros con alienígenas, navegación, animaciones y cierre jugable.
- Audio espacial, música contextual, noticias de televisión y mezcla por categorías.

## Abrir el proyecto

1. Clonar este repositorio.
2. Agregar la carpeta raíz en Unity Hub.
3. Abrirla con Unity `6000.0.81f1`.
4. Usar la configuración de escenas versionada en `ProjectSettings/EditorBuildSettings.asset`.

La compilación comienza en `Assets/Scenes/Intro.unity` y continúa en `Assets/Scenes/Playground.unity`.

## Controles

- `WASD` / stick izquierdo: movimiento.
- Mouse / stick derecho: cámara.
- `E` / botón inferior (`X` en PlayStation, `A` en Xbox): interactuar, agarrar y soltar.
- `B` / botón izquierdo (`Cuadrado` en PlayStation, `X` en Xbox): ladrar.

## Organización del repositorio

- `Assets/`: escenas, scripts, prefabs, modelos, materiales, audio y UI.
- `Packages/`: dependencias reproducibles del proyecto.
- `ProjectSettings/`: configuración de Unity y orden de escenas.
- `Docs/`: documentación técnica y evidencias académicas.
- `Tools/`: utilidades de Editor y pruebas internas; no forman parte del ejecutable.

`Library`, `Temp`, `Logs`, `UserSettings` y las builds generadas están excluidas porque Unity las reconstruye localmente.

## Estado de validación

- Estructura Unity verificada sin metadatos faltantes, GUID duplicados ni conflictos.
- Build Windows 64 bits generada correctamente desde `main`.
- Arranque técnico del ejecutable comprobado sin excepciones relevantes en `Player.log`.

La aprobación jugable, visual y auditiva completa depende de la pasada manual indicada en la documentación de QA.
