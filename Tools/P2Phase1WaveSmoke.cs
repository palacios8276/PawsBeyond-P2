using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class P2Phase1WaveSmoke
{
    static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL: "+label);Debug.Log("P2 Onda PASS: "+label);}
    public static async Task<string> Main()
    {
        bool bg=Application.runInBackground;Application.runInBackground=true;
        var keyboard=InputSystem.AddDevice<Keyboard>("P2Wave_VirtualKeyboard");keyboard.MakeCurrent();
        var bark=GameObject.Find("Player").GetComponent<PlayerBark>();
        var wave=UnityEngine.Object.FindFirstObjectByType<BarkWaveVfx>();var particles=wave.GetComponent<ParticleSystem>();
        var audio=GameObject.Find("AUDIO-09_DogBark").GetComponent<AudioSource>();
        var callback=(UnityEvent)typeof(PlayerBark).GetField("onBarked",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(bark);
        int synced=0;UnityAction listener=()=>{Check(wave.LastEmissionFrame==Time.frameCount&&audio.isPlaying,"audio y onda en mismo evento/frame");synced++;};
        callback.AddListener(listener);
        try
        {
            await Task.Delay(200);int start=bark.BarkCount;
            Check(particles.particleCount==0&&!particles.main.loop&&!particles.main.playOnAwake&&!particles.emission.enabled,"sin emisión automática/continua");
            Check(callback.GetPersistentEventCount()==1&&callback.GetPersistentTarget(0)==wave,"una sola conexión persistente");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.B));await Task.Delay(100);
            Check(bark.BarkCount==start+1&&wave.EmissionCount==1&&particles.particleCount==12,"una pulsación genera una onda de 12 partículas");
            var data=new ParticleSystem.Particle[32];int count=particles.GetParticles(data);float spread=0f;
            for(int i=1;i<count;i++)spread+=Vector3.Distance(data[0].position,data[i].position);
            Check(spread>0.3f&&particles.main.simulationSpace==ParticleSystemSimulationSpace.World,"onda distribuida y simulada en mundo");
            ScreenCapture.CaptureScreenshot("Temp/P2Phase1_BarkWave.png");
            await Task.Delay(750);Check(particles.particleCount==0&&wave.EmissionCount==1,"expira sin acumular y mantener tecla no repite");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(100);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.B));await Task.Delay(90);
            Check(wave.EmissionCount==2&&particles.particleCount<=12,"segunda pulsación sincronizada y limitada");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(60);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.B));await Task.Delay(60);
            Check(wave.EmissionCount==2&&bark.BarkCount==start+2,"pulsación rechazada no crea onda");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(600);
            audio.enabled=false;callback.RemoveListener(listener);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.B));await Task.Delay(100);
            Check(bark.BarkCount==start+2&&wave.EmissionCount==2,"sin audio disponible no confirma ni emite onda");audio.enabled=true;
            Check(synced==2,"dos emisiones sincronizadas, sin duplicación");
            return "PASS automático: 2 ladridos/ondas sincronizados en el mismo frame, 12 partículas máximas, expiración, sin repetición mantenida, sin onda para pulsación rechazada o audio ausente. Captura Temp/P2Phase1_BarkWave.png; pendiente apreciación manual.";
        }
        finally{audio.enabled=true;callback.RemoveListener(listener);InputSystem.RemoveDevice(keyboard);Application.runInBackground=bg;}
    }
}
