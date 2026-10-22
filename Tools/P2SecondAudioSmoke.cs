using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Unity.Cinemachine;
public static class P2SecondAudioSmoke
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static async Task<string> Main()
    {
        bool background=Application.runInBackground;Application.runInBackground=true;
        var camera=Camera.main;var brain=camera.GetComponent<CinemachineBrain>();
        bool brainEnabled=brain.enabled;Vector3 cameraPosition=camera.transform.position;Quaternion cameraRotation=camera.transform.rotation;
        var all=UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var mutes=all.ToDictionary(x=>x,x=>x.mute);var rows=new List<object>();
        try
        {
            Check(Application.isPlaying,"Entrar en Play");
            Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include,FindObjectsSortMode.None).Count(x=>x.isActiveAndEnabled)==1,"Listener único");
            var player=GameObject.Find("Player");var steps=UnityEngine.Object.FindFirstObjectByType<DogFootstepAudio>();
            var bark=GameObject.Find("AUDIO-09_DogBark").GetComponent<AudioSource>();
            Check(!bark.playOnAwake && !bark.isPlaying,"Ladrido sin activación automática");
            Check(all.Where(x=>x.name.StartsWith("AUDIO-10_")).All(x=>!x.enabled && !x.clip),"Ronquidos preparados sin asumir sueño");
            await Task.Delay(500);int idle=steps.PlayedSteps;await Task.Delay(500);Check(steps.PlayedSteps==idle,"Reposo sin pasos");
            var before=player.transform.position;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.W));await Task.Delay(1500);
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            Check(Vector3.Distance(before,player.transform.position)>0.5f && steps.PlayedSteps>idle,"Pasos por movimiento WASD real");
            await Task.Delay(500);int stopped=steps.PlayedSteps;await Task.Delay(600);Check(steps.PlayedSteps==stopped,"Detención sin nuevos pasos");
            int walkingCount=steps.PlayedSteps;
            player.transform.rotation=Quaternion.Euler(0,player.transform.eulerAngles.y+90,0);await Task.Delay(300);
            Check(steps.PlayedSteps==walkingCount,"Giro inmóvil sin pasos");
            rows.Add(new{test="Pasos",result="PASS automático",played=walkingCount,quietStops=true,rotationSilent=true});
            brain.enabled=false;
            foreach(var source in all)source.mute=true;
            foreach(var entry in new[]{new{name="AUDIO-03_Fridge",distances=new[]{1f,3.5f,9f},samples=10},new{name="AUDIO-05_WindowBreeze",distances=new[]{1f,3.5f,7f},samples=10},new{name="AUDIO-05_SinkDrip",distances=new[]{1f,3.5f,7f},samples=105}})
            {
                var source=all.Single(x=>x.name==entry.name);source.mute=false;var measurements=new List<object>();var levels=new List<double>();
                foreach(float distance in entry.distances)
                {
                    camera.transform.position=source.transform.position+Vector3.right*distance;await Task.Delay(300);
                    var buffer=new float[1024];double sum=0;long count=0;float peak=0;
                    for(int i=0;i<entry.samples;i++)
                    {
                        await Task.Delay(100);
                        for(int channel=0;channel<2;channel++){AudioListener.GetOutputData(buffer,channel);foreach(float x in buffer){sum+=x*x;count++;peak=Math.Max(peak,Math.Abs(x));}}
                    }
                    double rms=Math.Sqrt(sum/count);levels.Add(rms);Check(peak<0.99f,"Pico de salida alto: "+source.name);
                    measurements.Add(new{distance,rms,peak});
                }
                Check(levels[0]>0.00001 && levels[2]<levels[0]*0.1,"Atenuación cerca/lejos no comprobada: "+source.name);
                rows.Add(new{test=source.name,result="PASS técnico de atenuación",measurements});source.mute=true;
            }
            Check(!bark.isPlaying,"No se inventó un disparador de ladrido");
            string json=Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(Path.Combine(Application.dataPath,"../Docs/P2_AUDIO_SEGUNDA_PRUEBAS.json"),json);return json;
        }
        finally
        {
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());foreach(var row in mutes)if(row.Key)row.Key.mute=row.Value;
            camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);brain.enabled=brainEnabled;Application.runInBackground=background;
        }
    }
}
