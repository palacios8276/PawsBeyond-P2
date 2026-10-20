# PawsBeyond-P1 — WebGL / itch.io

Materia: **Motores de Desarrollo 1 — Unity**. Entrega Parcial 1, **Work in Progress (WIP)**.

## Requisitos y alcance

Usar exclusivamente este repositorio (`palacios8276/PawsBeyond-P1`), Unity **6000.0.81f1**, módulo **WebGL Build Support** de la misma versión, Unity CLI/Pipeline instalado y Python 3 para empaquetar/servir. Abrir este proyecto en el Editor, fuera de Play y sin errores de compilación. No ejecutar `Tools/BuildP1.cs`: ese procedimiento genera Windows.

No se cambiaron mecánicas ni resultados históricos de testing. Los perfiles `New Web Profile` y `New Windows Profile` ya existían como archivos no versionados y se conservan. El perfil Web hereda la lista global de escenas.

## Configuración aplicada por el helper

- Escena única e inicial: `Assets/Scenes/Playground.unity`.
- WebGL, IL2CPP, .NET Standard 2.1; build sin Development, Script Debugging ni Profiler.
- Gzip + Decompression Fallback, caché de datos y template Default; 1280 × 720.
- Memoria inicial 256 MB, crecimiento geométrico hasta 2048 MB; threads desactivados.
- Excepciones explícitas conservadas y stripping Low por compatibilidad; strip engine code activado.
- Optimización DiskSizeLTO aplicada en cada ejecución: es una preferencia local de Unity, no confiar en que viaje por Git.
- Sin cambios de calidad global, paquetes ni scripts de gameplay. Cinemachine, física y New Input System se conservan.

Gzip con fallback permite servir los `.unityweb` sin configurar Content-Encoding; agrega descompresión JavaScript. Se eligió compatibilidad de hosting/local antes que exigir cabeceras Brotli. No habilitar WebGPU, threads ni ajustes experimentales para esta entrega.

## Generar y empaquetar (PowerShell)

Desde la raíz Git del proyecto nuevo:

```powershell
$projectPath = (Get-Location).Path
# Verificar que origin sea palacios8276/PawsBeyond-P1 y la carpeta sea PawsBeyond-P1.
git remote get-url origin
unity status --project-path $projectPath --format json
unity command run_script --caller plugin --skill optimize-web --project-path $projectPath --file "$projectPath/Tools/BuildWebGLP1.cs" --timeout_ms 1800000 --timeout 1805 --format json
```

Continuar únicamente si el resultado es `Succeeded`. El helper devuelve la carpeta exacta `Builds/WebGL-P1-<fechaUTC>`. Reemplazar el ejemplo siguiente por esa salida:

```powershell
python Tools/package_webgl.py Builds/WebGL-P1-<fechaUTC>
```

Genera un ZIP hermano `WebGL-P1-<fechaUTC>-itch.zip`, con `index.html` en la raíz, `Build/` y `TemplateData/`; incluye StreamingAssets si Unity lo genera. El reporte queda fuera del ZIP. Las salidas y ZIP están ignorados por Git. No comprimir la carpeta contenedora ni subir Assets/Library/código fuente. El helper usa carpetas nuevas y no borra builds previas.

## Probar por HTTP

```powershell
python -m http.server 8000 --bind 127.0.0.1 --directory Builds/WebGL-P1-<fechaUTC>
```

Abrir `http://127.0.0.1:8000/` en navegador de escritorio con WebGL 2; no abrir index.html con `file://`. Para terminar el servidor: Ctrl+C.

Comprobar carga sin errores de Wasm/compresión, hacer clic dentro del juego para foco/captura del mouse, WASD, órbita, E, entrada al baño, tres prendas, puerta libre, abducción y Owners hacia Kitchen. Probar Esc y volver a hacer clic; recargar reinicia el prototipo. La validación del Editor o una carga HTTP correcta no certifican por sí solas el gameplay en navegador. Registrar fallos WebGL en un informe separado; no editar el informe de testing original.

## Subir a itch.io

Crear/editar la página como **HTML Game**, subir el ZIP y marcar **This file will be played in the browser**. Usar viewport 1280 × 720, Click to Play y botón de fullscreen. Mantener Mobile Friendly desactivado: no hay controles táctiles. Primero revisar la vista previa/borrador; publicar solo después de probar allí la secuencia completa. Este trabajo no publica la página.

El empaquetador valida: hasta 1000 archivos, 500 MB extraídos, 200 MB por archivo y rutas de hasta 240 caracteres. Verificar también mayúsculas/minúsculas y rutas relativas.

Fuentes: [itch.io HTML5](https://itch.io/docs/creators/html5), [Unity 6: desplegar Web](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-deploying.html).

## Texto para la página

### PawsBeyond — Parcial 1 (WIP)

**Work in Progress — Motores de Desarrollo 1 — Unity**

Un perro recorre su casa mientras una interferencia alienígena hace flotar a sus dueños. Observá su recorrido y resolvé el obstáculo del baño: retirá las tres prendas con la boca para liberar la puerta. El rayo retira el cesto solo después de resolver el puzzle.

Prototipo académico 3D en greybox, desarrollado en Unity 6. Incluye cámara en tercera persona, interacción por Raycast, objetos físicos, triggers y prendas instanciadas desde un prefab. Esta entrega tiene un único puzzle completo: Bathroom.

**Controles:** WASD para moverse; mouse para rotar cámara; E para agarrar/soltar. Hacer clic dentro del juego para activar el foco; Esc libera el cursor. Mando: stick izquierdo para moverse, derecho para cámara y botón inferior (X en PS5/A en Xbox) para agarrar/soltar. La compatibilidad del mando en navegador depende del navegador/dispositivo y debe comprobarse; la prueba física previa fue en el Editor.

**Bugs conocidos y límites:** no se declara una lista de bugs WebGL antes de probar esa plataforma. Consultar el registro WebGL separado incluido en el proyecto para el resultado real. Sin controles táctiles ni compatibilidad móvil validada. La cámara y física en espacios estrechos requieren revisión manual en el navegador elegido.

**Funcionalidades pendientes:** arte, animaciones y audio finales; puzzles de Kitchen/Living Room; cierre narrativo completo; menú y ajustes. Son posibilidades futuras, no funcionalidades incluidas ni requisitos certificados de P1.
