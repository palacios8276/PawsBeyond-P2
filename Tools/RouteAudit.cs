using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public static class RouteAudit
{
    public static string Main()
    {
        var root=GameObject.Find("Owners");
        var route=GameObject.Find("OwnersWaypoints").transform;
        var hits=new HashSet<string>();
        var owners=new[]{GameObject.Find("Owner_A_Capsule").transform,GameObject.Find("Owner_B_Capsule").transform};
        Vector3 from=root.transform.position;
        for(int i=0;i<route.childCount;i++)
        {
            Vector3 to=route.GetChild(i).position;
            int steps=Mathf.CeilToInt(Vector3.Distance(from,to)/0.15f);
            for(int step=0;step<=steps;step++)
            {
                Vector3 anchor=Vector3.Lerp(from,to,step/(float)Mathf.Max(1,steps));
                foreach(var owner in owners)
                foreach(float sway in new[]{-0.3f,0f,0.3f})
                {
                    Vector3 c=anchor+owner.localPosition+new Vector3(sway,0.06f,0f);
                    foreach(var collider in Physics.OverlapCapsule(c-Vector3.up*0.425f,c+Vector3.up*0.425f,0.275f,~0,QueryTriggerInteraction.Ignore))
                    {
                        if(collider.transform.IsChildOf(root.transform)||collider.CompareTag("Player")||collider.GetComponent<HingeJoint>())continue;
                        hits.Add("segment "+i+" "+route.GetChild(i).name+" intersects "+collider.name);
                    }
                }
            }
            from=to;
        }
        return hits.Count==0?"PASS: sampled capsule route including maximum lateral sway clears static walls/furniture":string.Join("\n",hits);
    }
}
