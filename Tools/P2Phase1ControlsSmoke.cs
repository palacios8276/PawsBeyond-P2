using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Unity.Cinemachine;

public static class P2Phase1ControlsSmoke
{
    static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL: "+label);Debug.Log("P2 Controles PASS: "+label);}
    public static async Task<string> Main()
    {
        bool bg=Application.runInBackground;Application.runInBackground=true;
        var keyboard=InputSystem.AddDevice<Keyboard>("P2Controls_VirtualKeyboard");keyboard.MakeCurrent();
        var mouse=InputSystem.AddDevice<Mouse>("P2Controls_VirtualMouse");mouse.MakeCurrent();
        var pad=InputSystem.AddDevice<Gamepad>("P2Controls_VirtualPad");pad.MakeCurrent();
        try
        {
            Cursor.lockState=CursorLockMode.Locked;await Task.Delay(250);
            var player=GameObject.Find("Player");var camera=Camera.main;
            var orbit=GameObject.Find("CinemachineCamera").GetComponent<CinemachinePanTilt>();
            Vector3 initial=player.transform.position;Quaternion view=camera.transform.rotation;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));await Task.Delay(400);
            Vector3 forward=player.transform.position-initial;forward.y=0;Check(forward.magnitude>0.4f,"W desplaza al perro");
            Vector3 before=player.transform.position;float yaw=player.transform.eulerAngles.y;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S));await Task.Delay(400);
            Vector3 backward=player.transform.position-before;backward.y=0;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(80);
            Check(backward.magnitude>0.4f&&Vector3.Dot(forward.normalized,backward.normalized)<-0.8f,"S invierte desplazamiento");
            Check(Mathf.Abs(Mathf.DeltaAngle(yaw,player.transform.eulerAngles.y))>140f,"W/S gira hacia movimiento");
            Check(Quaternion.Angle(view,camera.transform.rotation)<1f,"giro del perro no arrastra órbita");
            float pan=orbit.PanAxis.Value;Vector3 offset=camera.transform.position-player.transform.position;
            for(int i=0;i<12;i++){InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(6f,1f));await Task.Delay(35);}
            await Task.Delay(200);
            Check(Mathf.Abs(Mathf.DeltaAngle(pan,orbit.PanAxis.Value))>2f&&Vector3.Angle(offset,camera.transform.position-player.transform.position)>2f,"mouse mueve órbita y cámara");
            before=player.transform.position;InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=Vector2.up});await Task.Delay(300);
            InputSystem.QueueStateEvent(pad,new GamepadState());await Task.Delay(60);
            Check(Vector3.Distance(before,player.transform.position)>0.3f,"stick izquierdo mueve");
            pan=orbit.PanAxis.Value;InputSystem.QueueStateEvent(pad,new GamepadState{rightStick=new Vector2(0.5f,0f)});await Task.Delay(300);
            InputSystem.QueueStateEvent(pad,new GamepadState());await Task.Delay(60);
            Check(Mathf.Abs(Mathf.DeltaAngle(pan,orbit.PanAxis.Value))>3f,"stick derecho mueve cámara");
            var bark=player.GetComponent<PlayerBark>();int count=bark.BarkCount;before=player.transform.position;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.B));await Task.Delay(300);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(60);
            Check(bark.BarkCount==count+1&&Vector3.Distance(before,player.transform.position)>0.3f,"ladrar no bloquea movimiento");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));await Task.Delay(100);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(60);
            Check(!player.GetComponent<PlayerInteraction>().IsDragging&&bark.BarkCount==count+1,"E fuera de alcance no agarra ni ladra");
            Check(player.GetComponent<CharacterController>()&&!player.GetComponent<Rigidbody>(),"CharacterController conservado sin Rigidbody");
            Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,"un solo AudioListener");
            return "PASS automático con teclado/mouse/mando virtuales: W/S y giro, órbita independiente, mouse y dos sticks, W+B simultáneo, E fuera de rango, CharacterController y Listener único. No valida dispositivos físicos ni sensación de control.";
        }
        finally{InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(pad);Application.runInBackground=bg;}
    }
}
