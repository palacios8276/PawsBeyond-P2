using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Unity.Cinemachine;
public static class PolishGameplaySmoke
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message); Debug.Log("PASS: "+message);}
    public static async Task<string> Main()
    {
        bool bg=Application.runInBackground;
        int physicalPads=Gamepad.all.Count;
        Gamepad testPad=null;
        Application.runInBackground=true;
        Cursor.lockState=CursorLockMode.Locked;
        try
        {
            var p=GameObject.Find("Player");
            var cam=Camera.main;
            var pan=GameObject.Find("CinemachineCamera").GetComponent<CinemachinePanTilt>();
            await Task.Delay(500);
            Quaternion cameraBefore=cam.transform.rotation;
            float travelledYaw=0f,lastYaw=p.transform.eulerAngles.y;
            foreach(var key in new[]{Key.W,Key.S,Key.W})
            {
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));
                for(int i=0;i<18;i++)
                {
                    await Task.Delay(35);
                    float yaw=p.transform.eulerAngles.y;
                    travelledYaw+=Mathf.Abs(Mathf.DeltaAngle(lastYaw,yaw)); lastYaw=yaw;
                }
            }
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            await Task.Delay(150);
            Check(travelledYaw<390f,"W/S/W uses two half-turns, no full extra spin");
            Check(Quaternion.Angle(cameraBefore,cam.transform.rotation)<0.5f,"dog reversal does not rotate camera");
            var dog=GameObject.Find("PlaceholderDog").GetComponent<Renderer>();
            Vector3 frame=cam.WorldToViewportPoint(dog.bounds.center);
            Check(frame.z>0f && frame.x>0.3f && frame.x<0.7f && frame.y>0.05f && frame.y<0.6f,
                "dog framed lower-centre: "+frame);
            Vector3 cameraOffset=cam.transform.position-p.transform.position;
            float x=pan.PanAxis.Value;
            for(int i=0;i<15;i++){InputSystem.QueueDeltaStateEvent(Mouse.current.delta,new Vector2(6f,2f));await Task.Delay(35);}
            await Task.Delay(400);
            Check(Mathf.Abs(Mathf.DeltaAngle(x,pan.PanAxis.Value))>2f,"mouse drives orbit input");
            Vector3 offsetAfter=cam.transform.position-p.transform.position;
            Check(Vector3.Angle(cameraOffset,offsetAfter)>2f,"camera position actually orbits player");
            testPad=InputSystem.AddDevice<Gamepad>("P1QA_VirtualPad");
            Vector3 before=p.transform.position;
            InputSystem.QueueStateEvent(testPad,new GamepadState{leftStick=Vector2.up});
            await Task.Delay(300);
            InputSystem.QueueStateEvent(testPad,new GamepadState());
            Check(Vector3.Distance(before,p.transform.position)>0.2f,"virtual gamepad left stick moves player");
            x=pan.PanAxis.Value;
            InputSystem.QueueStateEvent(testPad,new GamepadState{rightStick=new Vector2(0.5f,0f)});
            await Task.Delay(300);
            InputSystem.QueueStateEvent(testPad,new GamepadState());
            Check(Mathf.Abs(Mathf.DeltaAngle(x,pan.PanAxis.Value))>3f,"virtual right stick orbits camera");
            Check(!p.GetComponent<Rigidbody>(),"no Rigidbody on Player");
            return "AUTOMATED PASS; physical gamepads detected: "+physicalPads+". Real keyboard/mouse/controller validation remains manual.";
        }
        finally
        {
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            if(testPad!=null)InputSystem.RemoveDevice(testPad);
            Application.runInBackground=bg;
        }
    }
}
