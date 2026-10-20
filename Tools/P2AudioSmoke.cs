using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// Prueba de Editor: verifica reproducción y señal, no sustituye la escucha humana.
public static class P2AudioSmoke
{
    public static async Task<string> Main()
    {
        if(!Application.isPlaying)throw new Exception("Entrá en Play para probar audio.");
        bool background=Application.runInBackground; Application.runInBackground=true;
        try
        {
            var sources=UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            if(sources.Length!=4)throw new Exception("Se esperaban cuatro emisores.");
            if(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include,FindObjectsSortMode.None).Count(x=>x.isActiveAndEnabled)!=1)
                throw new Exception("Listener duplicado o ausente.");
            var last=sources.ToDictionary(x=>x,x=>x.timeSamples);var wraps=sources.ToDictionary(x=>x,x=>0);
            int startFrame=Time.frameCount;double startDsp=AudioSettings.dspTime;
            float peak=0;double energy=0;long samples=0;var buffer=new float[1024];
            for(int step=0;step<220;step++)
            {
                await Task.Delay(100);
                foreach(var source in sources)
                {
                    if(!source.enabled || !source.clip || !source.isPlaying || !source.loop || !source.outputAudioMixerGroup
                        || source.outputAudioMixerGroup.name!="Ambience")throw new Exception("Emisor sin reproducción correcta: "+source.name);
                    int now=source.timeSamples;if(now<last[source])wraps[source]++;last[source]=now;
                }
                for(int channel=0;channel<2;channel++)
                {
                    AudioListener.GetOutputData(buffer,channel);
                    foreach(float x in buffer){peak=Math.Max(peak,Math.Abs(x));energy+=x*x;samples++;}
                }
            }
            if(Time.frameCount<=startFrame || AudioSettings.dspTime<=startDsp)throw new Exception("Play o DSP no avanzó.");
            if(wraps.Any(x=>x.Value<2))throw new Exception("No se observaron dos vueltas en todos los loops.");
            if(peak<0.00001f)throw new Exception("No se detectó señal de salida; revisar mute del Editor/Mixer.");
            if(peak>=0.99f)throw new Exception("Pico de salida demasiado alto.");
            var report=new {result="PASS técnico; escucha perceptual pendiente",frames=Time.frameCount-startFrame,
                dspSeconds=AudioSettings.dspTime-startDsp,listeners=1,sources=sources.Select(x=>new{name=x.name,playing=x.isPlaying,wraps=wraps[x]}).ToArray(),
                sampledOutputPeak=peak,sampledOutputRms=Math.Sqrt(energy/samples),scope="Salida de inicio en Bedroom durante 22 segundos; no es medición del Audio Profiler ni validación de todo el recorrido."};
            string json=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(Path.Combine(Application.dataPath,"../Docs/P2_AUDIO_RUNWAY_PRUEBA.json"),json);
            return json;
        }
        finally{Application.runInBackground=background;}
    }
}
