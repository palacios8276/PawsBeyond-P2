using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Unity.Cinemachine;
public static class QAControlsSmoke
{
    public static async Task<string> Main()
    {
        bool bg=Application.runInBackground;
        Application.runInBackground=true;
        try
        {
            var player=GameObject.Find("Player");
            if(!player.GetComponent<CharacterController>() || player.GetComponent<Rigidbody>()) throw new Exception("Controller changed");
            var pan=GameObject.Find("CinemachineCamera").GetComponent<CinemachinePanTilt>();
            foreach(var key in new[]{Key.W,Key.S,Key.A,Key.D})
            {
                Vector3 before=player.transform.position;
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));
                await Task.Delay(350);
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                await Task.Delay(100);
                if(Vector3.Distance(before,player.transform.position)<0.2f) throw new Exception("Key failed: "+key);
                Debug.Log("PASS P1QA movement: "+key);
            }
            float x=pan.PanAxis.Value,y=pan.TiltAxis.Value;
            Quaternion cameraBefore=Camera.main.transform.rotation;
            for(int i=0;i<12;i++)
            {
                InputSystem.QueueDeltaStateEvent(Mouse.current.delta,new Vector2(4f,2f));
                await Task.Delay(30);
            }
            await Task.Delay(300);
            if(Mathf.Abs(Mathf.DeltaAngle(x,pan.PanAxis.Value))<1f || Mathf.Abs(y-pan.TiltAxis.Value)<0.5f
                || Quaternion.Angle(cameraBefore,Camera.main.transform.rotation)<1f) throw new Exception("Mouse Look failed");
            Debug.Log("PASS P1QA mouse delta changes Pan, Tilt and actual camera");
            return "PASS WASD, mouse horizontal/vertical Look, CharacterController and Cinemachine";
        }
        finally
        {
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            Application.runInBackground=bg;
        }
    }
}
