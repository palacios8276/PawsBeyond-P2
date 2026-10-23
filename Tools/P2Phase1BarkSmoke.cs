using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class P2Phase1BarkSmoke
{
    static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL: "+label);Debug.Log("P2 Bark PASS: "+label);}
    public static async Task<string> Main()
    {
        bool bg=Application.runInBackground;Application.runInBackground=true;
        var keyboard=InputSystem.AddDevice<Keyboard>("P2Bark_VirtualKeyboard");keyboard.MakeCurrent();
        var pad=InputSystem.AddDevice<Gamepad>("P2Bark_VirtualPad");pad.MakeCurrent();
        var bark=GameObject.Find("Player").GetComponent<PlayerBark>();
        var source=GameObject.Find("AUDIO-09_DogBark").GetComponent<AudioSource>();
        var callback=(UnityEvent)typeof(PlayerBark).GetField("onBarked",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(bark);
        var audioFrames=new List<int>();UnityAction listener=()=>{Check(source.isPlaying,"audio iniciado antes del evento");audioFrames.Add(Time.frameCount);};
        callback.AddListener(listener);
        try
        {
            int frame=Time.frameCount;await Task.Delay(250);Check(Time.frameCount>frame,"frames avanzan");
            int initial=bark.BarkCount;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.B));await Task.Delay(180);
            Check(bark.BarkCount==initial+1,"B emite un ladrido");
            float energy=0;var samples=new float[512];
            for(int i=0;i<8;i++){source.GetOutputData(samples,0);foreach(float v in samples)energy+=v*v;await Task.Delay(30);}
            Check(energy>0.000001f,"salida PCM del ladrido no silenciosa");
            await Task.Delay(900);Check(bark.BarkCount==initial+1,"mantener B no repite tras cooldown");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(100);
            Check(bark.BarkCount==initial+1,"soltar B no ladra");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.B));await Task.Delay(70);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(70);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.B));await Task.Delay(70);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(70);
            Check(bark.BarkCount==initial+2,"enfriamiento rechaza pulsación demasiado rápida");
            await Task.Delay(500);
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.West));await Task.Delay(1100);
            Check(bark.BarkCount==initial+3,"cuadrado/oeste emite una vez aunque quede presionado");
            InputSystem.QueueStateEvent(pad,new GamepadState());await Task.Delay(120);
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));await Task.Delay(120);
            InputSystem.QueueStateEvent(pad,new GamepadState());await Task.Delay(120);
            Check(bark.BarkCount==initial+3,"botón sur conserva Interact y no ladra");
            bark.enabled=false;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.B));await Task.Delay(550);
            Check(bark.BarkCount==initial+3,"componente deshabilitado no ladra");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(80);bark.enabled=true;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.B));await Task.Delay(120);
            Check(bark.BarkCount==initial+4 && audioFrames.Count==4,"rehabilitar no duplica suscripción/eventos");
            Check(!source.loop&&!source.playOnAwake,"emisor sin loop ni autoarranque");
            return "PASS automático: B y botón oeste virtual, pulsación/soltado/mantener, cooldown, habilitación y evento único con audio PCM. Cuatro ladridos aceptados; no prueba física ni escucha perceptual.";
        }
        finally{callback.RemoveListener(listener);InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(pad);Application.runInBackground=bg;}
    }
}
