# Orden de audio — P2

- `Assets/Audio/Pruebas/Ambientes`: candidatos para escuchar y comparar. No se asignan al juego hasta elegirlos.
- `Assets/Audio/EnUso/Ambientes`: clips que actualmente usa la escena. Los cuatro loops de Runway siguen siendo provisorios; estar aquí no significa que sean definitivos.
- `Assets/Audio/Mezcladores`: Audio Mixer y grupos.
- `AudioSources/Originales/Runway`: MP3 originales generados, procedencia y huellas. Se conservan sin editar y fuera de Assets para no importar duplicados.
- `AudioSources/Edicion/REAPER`: proyectos de edición y grabaciones de trabajo. Exportar candidatos a Pruebas y promover los elegidos a EnUso.

Para sustituir un audio ya asignado, conservar el nombre del WAV y su .meta. Para mover assets, usar el panel Project de Unity: así se mantienen sus GUID y referencias. No cambiar los originales ni presentar recursos generados con IA como grabaciones propias.

Las rutas del informe inicial P2_AUDIO_AMBIENCE.md describen el estado histórico anterior a esta organización. El informe actual P2_AUDIO_RUNWAY.md y las herramientas ya usan las rutas nuevas.
