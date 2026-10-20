using System;
using System.Threading.Tasks;
using UnityEngine;
public static class VisualSmoke
{
    public static async Task<string> Main()
    {
        bool background=Application.runInBackground;
        Application.runInBackground=true;
        try
        {
            var a=GameObject.Find("Owner_A_Capsule").transform;
            var b=GameObject.Find("Owner_B_Capsule").transform;
            Quaternion a0=a.rotation,b0=b.rotation;
            Vector3 ap=a.position,bp=b.position;
            await Task.Delay(2200);
            if(Quaternion.Angle(a0,a.rotation)>0.01f || Quaternion.Angle(b0,b.rotation)>0.01f
                || Vector3.Angle(a.up,Vector3.up)>0.01f || Vector3.Angle(b.up,Vector3.up)>0.01f)
                throw new Exception("Owners rotated or tilted");
            if(Mathf.Abs(ap.x-a.position.x)<0.02f || Mathf.Abs(bp.x-b.position.x)<0.02f)
                throw new Exception("Owners did not float laterally");
            var p=GameObject.Find("Player");
            if(p.GetComponent<MeshRenderer>().enabled || !p.GetComponentInChildren<MeshRenderer>().enabled)
            {
                bool dog=false;
                foreach(var r in p.GetComponentsInChildren<MeshRenderer>()) if(r.name=="PlaceholderDog" && r.enabled) dog=true;
                if(p.GetComponent<MeshRenderer>().enabled || !dog) throw new Exception("Dog visibility invalid");
            }
            return "PASS Owners move laterally without rotation or tilt; visible dog placeholder";
        }
        finally {Application.runInBackground=background;}
    }
}
