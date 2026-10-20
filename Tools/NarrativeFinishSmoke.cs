using System;
using System.Threading.Tasks;
using UnityEngine;
public static class NarrativeFinishSmoke
{
    public static async Task<string> Main()
    {
        bool bg=Application.runInBackground;Application.runInBackground=true;
        try
        {
            var journey=GameObject.Find("Owners").GetComponent<OwnersJourney>();
            var puzzle=GameObject.Find("BathroomEntryTrigger").GetComponent<BathroomBasketDrop>();
            if(!puzzle.IsSolved)throw new Exception("Finish requires actual solved bathroom");
            for(int i=0;i<900 && !journey.IsComplete;i++)await Task.Delay(100);
            if(!journey.IsComplete)throw new Exception("Journey did not resume to Kitchen; stage "+journey.CurrentWaypoint);
            var point=GameObject.Find("15_KitchenSuspend").transform;
            if(Vector3.Distance(journey.transform.position,point.position)>0.02f)
                throw new Exception("Owners not suspended at Kitchen endpoint");
            return "AUTOMATED PASS: existing onSolved resumes owners; return through door; Kitchen suspension is final P1 state";
        }
        finally{Application.runInBackground=bg;}
    }
}
