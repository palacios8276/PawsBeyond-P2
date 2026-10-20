using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Cinemachine;
public static class NarrativeWaitSmoke
{
    public static async Task<string> Main()
    {
        bool bg=Application.runInBackground;Application.runInBackground=true;
        try
        {
            var journey=GameObject.Find("Owners").GetComponent<OwnersJourney>();
            var puzzle=GameObject.Find("BathroomEntryTrigger").GetComponent<BathroomBasketDrop>();
            var pan=GameObject.Find("CinemachineCamera").GetComponent<CinemachinePanTilt>();
            await Task.Delay(1800);
            if(journey.HasStarted)throw new Exception("Owners departed before observation");
            pan.PanAxis.Value=-22f;pan.TiltAxis.Value=-12f;
            for(int i=0;i<120 && !journey.HasStarted;i++)await Task.Delay(100);
            if(!journey.HasStarted)throw new Exception("Observation/presence did not start journey");
            Quaternion orientation=GameObject.Find("Owner_A_Capsule").transform.rotation;
            for(int i=0;i<1100 && !journey.WaitingForBathroom;i++)await Task.Delay(100);
            if(!journey.WaitingForBathroom || journey.CurrentWaypoint!=9)
                throw new Exception("Owners did not reach Bathroom wait; stage "+journey.CurrentWaypoint);
            Vector3 anchor=journey.transform.position;
            await Task.Delay(1500);
            if(Vector3.Distance(anchor,journey.transform.position)>0.01f || puzzle.IsSolved || puzzle.IsActivated)
                throw new Exception("Bathroom wait not stable before player entry");
            if(Quaternion.Angle(orientation,GameObject.Find("Owner_A_Capsule").transform.rotation)>0.01f)
                throw new Exception("Owners rotated during transport");
            var beam=GameObject.Find("OwnersAbductionBeam");
            if(!beam || beam.transform.parent!=journey.transform || !beam.activeInHierarchy)
                throw new Exception("Shared beam is not travelling with owners");
            return "AUTOMATED PASS: observation gate; Bedroom -> behind TV -> near Kitchen -> Bathroom; physical-door passage; stable wait and shared beam";
        }
        finally{Application.runInBackground=bg;}
    }
}
