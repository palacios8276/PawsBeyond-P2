# P2 Audio — Matriz de trazabilidad A–F

| Bloque | Requisito | Implementación | Evidencia | Estado |
|---|---|---|---|---|
| A | Mixer, grupos y mezcla | `PawsBeyondP2.mixer`; Master/Music/SFX/Ambience/Voice | `A_Mixer/A01_Mixer_grupos_niveles.png` | Técnico PASS; escucha pendiente |
| B | Música adaptativa | `AdaptiveMusicController`; eventos contextuales; crossfade 2 s | `B_Musica/B01_AdaptiveMusic_crossfade_niveles.png` | Técnico PASS |
| C | 5 SFX, 2 variaciones | ladrido, pasos, persiana, rayo e impacto | `C_SFX/C01_Ladrido_variacion_reforzada_SFX.png` | Parcial alto |
| D | ambiente + 3 fuentes 3D | noche 2D; brisa/TV/heladera/pileta/nave/rayo 3D | `D_Audio3D/D01_TVNews_spatial_blend_distancias.png` | Técnico PASS; falta segunda captura |
| E | voces propias procesadas | cuatro noticias, playlist y Voice | `E_Voz/E01_TVNews_playlist_estados.png`, `E02_TVNews_ruteo_Voice.png` | **PARCIAL**: falta autoría/REAPER/eventos individuales |
| F | script reactivo | `TVNewsController` cambia pitch y pausa según música | `F_Codigo/F01_TVNews_codigo_reactivo.png` | Técnico PASS |

## Pies de foto

- **A01.** Bloque A. Escena Playground. Mixer `PawsBeyondP2`: jerarquía Master, SFX, Ambience, Voice y Music, con niveles de reposo. Demuestra organización y balance por categorías.
- **B01.** Bloque B. GameObject `AdaptiveMusic`. Componente `AdaptiveMusicController`: fuentes Regular/Stress, crossfade 2 s y niveles 0,18/0,22. Demuestra música adaptativa.
- **C01.** Bloque C. GameObject `AUDIO-09_DogBark`. Componente `DogBarkAudio`: ladrido normal y reforzado, multiplicadores y reproducción SFX. Demuestra variación contextual.
- **D01.** Bloque D. GameObject `AUDIO-TV_News`. `AudioSource` 3D: Spatial Blend 1, Linear Rolloff, Min 1,5 y Max 12. Demuestra espacialización y atenuación.
- **E01.** Bloque E. GameObject `AUDIO-TV_News`. `TVNewsController`: playlist de cuatro clips, Music State, pitch 1/1,15 y pausa 1,5/0. Demuestra reproducción reactiva.
- **E02.** Bloque E. `AudioSource` de noticias ruteado a Voice. Demuestra organización de voces.
- **F01.** Bloque F. Código real de `TVNewsController.cs`, líneas 39–74. Demuestra modificación suave de pitch y pausa a partir de `AdaptiveMusicState`.

