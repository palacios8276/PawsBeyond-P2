using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Cinemachine;
public static class NightBathroomView
{
    public static async Task<string> Main()
    {
        bool bg=Application.runInBackground;Application.runInBackground=true;
        try
        {
            var player=GameObject.Find("Player");var cc=player.GetComponent<CharacterController>();
            cc.enabled=false;player.transform.position=new Vector3(17.8f,0.15f,-0.1f);cc.enabled=true;
            var puzzle=GameObject.Find("BathroomEntryTrigger").GetComponent<BathroomBasketDrop>();
            for(int i=0;i<100 && !puzzle.IsReady;i++)await Task.Delay(50);
            if(!puzzle.IsReady)throw new Exception("Night bathroom first-entry event not activated");
            cc.enabled=false;player.transform.position=new Vector3(16.1f,0.15f,-0.1f);cc.enabled=true;
            player.transform.rotation=Quaternion.LookRotation(Vector3.left);
            var pan=GameObject.Find("CinemachineCamera").GetComponent<CinemachinePanTilt>();
            pan.PanAxis.Value=-90f;pan.TiltAxis.Value=20f;
            await Task.Delay(1200);
            return "Night bathroom view prepared through actual first-entry trigger; this setup uses test repositioning, not manual input";
        }
        finally{Application.runInBackground=bg;}
    }
}
