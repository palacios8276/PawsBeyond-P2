using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
public static class PolishGamepadInteractSmoke
{
    public static async Task<string> Main()
    {
        bool bg=Application.runInBackground;Application.runInBackground=true;
        Gamepad pad=null;GameObject cloth=null;
        try
        {
            var p=GameObject.Find("Player");
            var interaction=p.GetComponent<PlayerInteraction>();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LaundryCloth.prefab");
            cloth=UnityEngine.Object.Instantiate(prefab,p.transform.position+p.transform.forward*1.1f+Vector3.up*0.3f,Quaternion.identity);
            cloth.GetComponent<Rigidbody>().isKinematic=false;
            pad=InputSystem.AddDevice<Gamepad>("P1QA_InteractionPad");
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));
            await Task.Delay(200);
            if(!interaction.IsDragging || !cloth.GetComponent<BasketDrag>().IsHeld)throw new Exception("Virtual south button did not grab");
            InputSystem.QueueStateEvent(pad,new GamepadState());await Task.Delay(150);
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));await Task.Delay(200);
            if(interaction.IsDragging)throw new Exception("Virtual south button did not release");
            return "AUTOMATED PASS: virtual gamepad south button grabs/releases via existing mouth raycast; not physical/manual validation";
        }
        finally
        {
            if(pad!=null)InputSystem.RemoveDevice(pad);
            if(cloth)UnityEngine.Object.Destroy(cloth);
            Application.runInBackground=bg;
        }
    }
}
