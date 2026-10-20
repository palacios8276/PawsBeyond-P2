using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using System.Reflection;
public static class PolishAudit
{
    public static string Main()
    {
        if(Application.isPlaying)throw new System.Exception("Audit saved edit scene");
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)
                throw new System.Exception("Missing script: "+t.name);
        var player=GameObject.Find("Player");
        if(!player.GetComponent<CharacterController>() || player.GetComponent<Rigidbody>())throw new System.Exception("Player invariant");
        var journey=GameObject.Find("Owners").GetComponent<OwnersJourney>();
        var s=new SerializedObject(journey);
        foreach(string field in new[]{"viewingCamera","bedroomTrigger","bedroomDoor","bathroomTrigger","bathroomDoor","bathroomPuzzle"})
            if(!s.FindProperty(field).objectReferenceValue)throw new System.Exception("Missing journey ref: "+field);
        var points=s.FindProperty("waypoints");
        if(points.arraySize!=16)throw new System.Exception("Route length");
        for(int i=0;i<points.arraySize;i++)if(!points.GetArrayElementAtIndex(i).objectReferenceValue)throw new System.Exception("Missing waypoint");
        var puzzle=GameObject.Find("BathroomEntryTrigger").GetComponent<BathroomBasketDrop>();
        var onSolved=(UnityEvent)typeof(BathroomBasketDrop).GetField("onSolved",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(puzzle);
        bool connected=false;
        for(int i=0;i<onSolved.GetPersistentEventCount();i++)
            if(onSolved.GetPersistentTarget(i)==journey && onSolved.GetPersistentMethodName(i)=="ResumeAfterBathroom")connected=true;
        if(!connected)throw new System.Exception("Completion event not connected");
        var camera=GameObject.Find("CinemachineCamera");
        if(!camera.GetComponent<ThirdPersonCameraInput>().enabled)throw new System.Exception("Existing camera input disabled");
        var target=new SerializedObject(GameObject.Find("CameraTarget").GetComponent<CameraTargetFollow>());
        if(!target.FindProperty("player").objectReferenceValue || !target.FindProperty("orbit").objectReferenceValue)
            throw new System.Exception("Camera target reference");
        return "PASS: saved scene, required refs, 16 waypoints, existing onSolved connection, original camera input, no Player Rigidbody, no missing scripts";
    }
}
