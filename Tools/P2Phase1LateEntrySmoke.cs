using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
public static class P2Phase1LateEntrySmoke
{
    public static async Task<string> Main()
    {
        bool bg=Application.runInBackground; Application.runInBackground=true;
        try
        {
            var journey=GameObject.Find("Owners").GetComponent<OwnersJourney>();
            var puzzle=GameObject.Find("BathroomEntryTrigger").GetComponent<BathroomBasketDrop>();
            var s=new SerializedObject(journey);s.FindProperty("moveSpeed").floatValue=8f;s.ApplyModifiedPropertiesWithoutUndo();
            for(int i=0;i<1000 && !journey.WaitingForBathroom;i++)await Task.Delay(50);
            if(!journey.WaitingForBathroom || puzzle.IsActivated)throw new Exception("Dueños primero: espera inválida");
            var cc=GameObject.Find("Player").GetComponent<CharacterController>();
            cc.enabled=false;cc.transform.position=new Vector3(18.3f,0.15f,-0.21f);cc.enabled=true;Physics.SyncTransforms();
            for(int i=0;i<300 && !puzzle.IsReady;i++)await Task.Delay(50);
            if(!puzzle.IsReady || puzzle.Clothes.Length!=3)throw new Exception("Entrada posterior no activa");
            return "PASS automático: dueños primero no disparan caída; ingreso posterior del perro activa una vez. Velocidad 8 m/s y posición controlada solo en Play.";
        }
        finally{Application.runInBackground=bg;}
    }
}
