using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;

public static class P2Phase3ClosureSetup
{
    static void Ref(UnityEngine.Object target,string field,UnityEngine.Object value)
    {var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static void Int(UnityEngine.Object target,string field,int value)
    {var so=new SerializedObject(target);so.FindProperty(field).intValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static T Event<T>(UnityEngine.Object target,string field) where T:class => target.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target) as T;
    static void Box(GameObject g,string name,Vector3 center,Vector3 size)
    {Undo.RecordObject(g.transform,"Reubicar ventana de sala");g.name=name;g.SetActive(true);g.transform.SetPositionAndRotation(center,Quaternion.identity);g.transform.localScale=size;}
    static GameObject Cube(string name,Transform parent,Vector3 pos,Vector3 size,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;Undo.RegisterCreatedObjectUndo(g,"Completar ambientación gris");
        g.transform.position=pos;g.transform.localScale=size;g.transform.SetParent(parent,true);g.GetComponent<Renderer>().sharedMaterial=material;return g;
    }
    static Transform Point(string name,Transform parent,Vector3 position)
    {var g=new GameObject(name);Undo.RegisterCreatedObjectUndo(g,"Agregar punto de episodio");g.transform.SetParent(parent,false);g.transform.position=position;return g.transform;}
    static GameObject Cylinder(string name,Transform parent,Vector3 pos,Vector3 scale,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;Undo.RegisterCreatedObjectUndo(g,"Agregar ambiente doméstico");
        g.transform.position=pos;g.transform.localScale=scale;g.transform.SetParent(parent,true);g.GetComponent<Renderer>().sharedMaterial=material;return g;
    }
    static void Bone(string name,Transform parent,Vector3 center,Material material)
    {
        var root=Point(name,parent,center);
        var g=Cube("Decorative_NoInteraction",root,center,new Vector3(0.45f,0.1f,0.10f),material);UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        for(int side=-1;side<=1;side+=2)
        {
            var end=GameObject.CreatePrimitive(PrimitiveType.Sphere);end.name="DecorativeEnd";end.transform.position=center+Vector3.right*(side*0.22f);
            end.transform.localScale=new Vector3(0.16f,0.12f,0.22f);end.transform.SetParent(root,true);end.GetComponent<Renderer>().sharedMaterial=material;
            UnityEngine.Object.DestroyImmediate(end.GetComponent<Collider>());
        }
    }
    public static string Main()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying||scene.path!="Assets/Scenes/Playground.unity")throw new Exception("Usar Playground P2 fuera de Play");
        var living=GameObject.Find("LivingRoomEpisode");
        if(living.GetComponent<WindowAmbienceFocus>())throw new Exception("El cierre ya fue configurado; ajustar desde Inspector");
        var episode=living.GetComponent<LivingRoomEpisode>();var journey=GameObject.Find("Owners").GetComponent<OwnersJourney>();
        var routeSo=new SerializedObject(journey);var route=routeSo.FindProperty("waypoints");
        if(route.arraySize!=16)throw new Exception("El recorrido previo no tiene los 16 puntos esperados; no se modifica");
        var old=new Transform[16];for(int i=0;i<16;i++)old[i]=(Transform)route.GetArrayElementAtIndex(i).objectReferenceValue;
        var functional=GameObject.Find("LivingWindow_Functional").transform;var blocked=GameObject.Find("LivingWindow_PreviouslyBlocked").transform;
        var originalBack=GameObject.Find("LRWallWest");var wallBounds=originalBack.GetComponent<Renderer>().bounds;
        var frames=new List<GameObject>();
        foreach(var window in new[]{functional,blocked})foreach(string part in new[]{"FrameLeft","FrameRight","Sill","Lintel"})
        {var child=window.Find(part);if(!child)throw new Exception("Falta un segmento previo de ventana");frames.Add(child.gameObject);}
        var frameRoot=Point("LivingWindows_SharedWallFrame",living.transform,Vector3.zero);
        foreach(var frame in frames)Undo.SetTransformParent(frame.transform,frameRoot,"Conservar segmentos de muro");
        float y0=wallBounds.min.y,y1=wallBounds.max.y,z=wallBounds.center.z,depth=wallBounds.size.z;
        var regions=new[]{new Vector4(wallBounds.min.x,2.2f,y0,y1),new Vector4(4.6f,9.3f,y0,y1),new Vector4(11.7f,wallBounds.max.x,y0,y1),
            new Vector4(2.2f,4.6f,y0,0.9f),new Vector4(2.2f,4.6f,2.2f,y1),new Vector4(9.3f,11.7f,y0,0.9f),new Vector4(9.3f,11.7f,2.2f,y1)};
        for(int i=0;i<regions.Length;i++)
        {var a=regions[i];Box(frames[i],"SharedWallSection"+i,new Vector3((a.x+a.y)*0.5f,(a.z+a.w)*0.5f,z),new Vector3(a.y-a.x,a.w-a.z,depth));}
        frames[7].name="RetiredOriginalSection";frames[7].SetActive(false);
        functional.position=new Vector3(10.5f,1.55f,z);blocked.position=new Vector3(3.4f,1.55f,z);
        Box(functional.Find("WindowGlass_Greybox").gameObject,"WindowGlass_Greybox",functional.position,new Vector3(2.4f,1.3f,depth));
        Box(blocked.Find("PreviouslyBlockedPanel").gameObject,"PreviouslyBlockedPanel",blocked.position,new Vector3(2.4f,1.3f,depth));
        // Se restaura el paño lateral anterior: no quedan huecos ni eventos allí.
        var formerWall=GameObject.Find("LRWallNorth (1)");formerWall.GetComponent<Renderer>().enabled=true;
        foreach(var collider in formerWall.GetComponents<Collider>())collider.enabled=true;
        var epSo=new SerializedObject(episode);
        var functionalLight=(Light)epSo.FindProperty("functionalWindowLight").objectReferenceValue;
        var blockedLight=(Light)epSo.FindProperty("blockedWindowLight").objectReferenceValue;
        functionalLight.transform.position=new Vector3(10.5f,2f,10.4f);blockedLight.transform.position=new Vector3(3.4f,2f,10.4f);
        var silhouette=(GameObject)epSo.FindProperty("alienSilhouette").objectReferenceValue;
        silhouette.transform.SetPositionAndRotation(new Vector3(10.5f,0f,z-0.18f),Quaternion.identity);
        var body=silhouette.transform.Find("SilhouetteBody");body.localPosition=new Vector3(0f,1.48f,0f);body.localScale=new Vector3(0.34f,0.4f,0.06f);
        var head=silhouette.transform.Find("SilhouetteHead");head.localPosition=new Vector3(0f,1.96f,-0.01f);head.localScale=new Vector3(0.5f,0.42f,0.07f);
        var leftApproach=Point("04_LivingLeftClearance",old[3].parent,new Vector3(2.5f,1.6f,5.4f));
        var rightApproach=Point("05_LivingRightClearance",old[3].parent,new Vector3(11.8f,1.6f,5.4f));
        old[4].name="06_FunctionalWindowAttempt";old[4].position=new Vector3(11.8f,1.6f,9.6f);
        old[5].name="07_BlockedWindowAttempt";old[5].position=new Vector3(3.4f,1.6f,9.6f);
        var points=new List<Transform>{old[0],old[1],old[2],old[3],leftApproach,rightApproach,old[4],old[5],old[4],rightApproach};
        for(int i=6;i<old.Length;i++)points.Add(old[i]);
        route.arraySize=points.Count;for(int i=0;i<points.Count;i++)route.GetArrayElementAtIndex(i).objectReferenceValue=points[i];routeSo.ApplyModifiedPropertiesWithoutUndo();
        Int(journey,"bathroomCrossing",12);Int(journey,"bathroomStop",13);Int(journey,"bathroomExit",15);
        Int(episode,"functionalWindowWaypoint",6);Int(episode,"blockedWindowWaypoint",7);
        GameObject.Find("KitchenIsland").SetActive(false);GameObject.Find("IslandTop").SetActive(false);
        var bedroom=GameObject.Find("BedroomFurniture").transform;var kitchen=GameObject.Find("KitchenFurniture").transform;
        var soft=GameObject.Find("Mattress").GetComponent<Renderer>().sharedMaterial;var stone=GameObject.Find("ToiletBase").GetComponent<Renderer>().sharedMaterial;
        Cylinder("DogBed_Pouf_Greybox",bedroom,new Vector3(-21.6f,0.18f,2.7f),new Vector3(1.4f,0.08f,1.1f),soft);
        foreach(var p in new[]{new Vector3(1.1f,0.16f,-12.2f),new Vector3(2f,0.16f,-12.2f)})
        {
            bool water=p.x>1.5f;var bowl=Cylinder(water?"WaterBowl_Greybox":"FoodBowl_Greybox",kitchen,p,new Vector3(0.5f,0.06f,0.5f),stone);
            Cylinder(water?"WaterSurface_Greybox":"FoodSurface_Greybox",bowl.transform,p+Vector3.up*0.04f,new Vector3(0.38f,0.015f,0.38f),soft);
        }
        Bone("AmbientToy_Bone_Bedroom",bedroom,new Vector3(-22.6f,0.18f,2.6f),stone);
        Bone("AmbientToy_Bone_Living",GameObject.Find("LivingFurniture").transform,new Vector3(8.9f,0.18f,2.5f),stone);
        var outsideFunctional=Point("FunctionalWindow_ExteriorAudio",functional,new Vector3(10.5f,1.5f,z+0.5f));
        var outsideBlocked=Point("BlockedWindow_ExteriorAudio",blocked,new Vector3(3.4f,1.5f,z+0.5f));
        var outsideNeutral=Point("LivingWindows_ExteriorAudio",living.transform,new Vector3(6.95f,1.5f,z+0.5f));
        var night=GameObject.Find("AUDIO-01_NightAmbience").GetComponent<AudioSource>();night.transform.position=new Vector3(-17.57f,1.5f,4.4f);
        night.spatialBlend=1f;night.rolloffMode=AudioRolloffMode.Linear;night.minDistance=3f;night.maxDistance=20f;night.dopplerLevel=0f;
        var wind=GameObject.Find("AUDIO-05_WindowBreeze").GetComponent<AudioSource>();wind.transform.position=outsideNeutral.position;
        wind.spatialBlend=1f;wind.rolloffMode=AudioRolloffMode.Linear;wind.minDistance=2f;wind.maxDistance=18f;wind.dopplerLevel=0f;
        var beam=UnityEngine.Object.FindFirstObjectByType<AbductionBeamController>();
        var beamSo=new SerializedObject(beam);var beamAudio=(AudioSource)beamSo.FindProperty("beamAudio").objectReferenceValue;
        var shipObject=new GameObject("AUDIO-11_ShipAmbience_Provisional");shipObject.transform.SetParent(living.transform,false);shipObject.transform.position=outsideNeutral.position+new Vector3(0f,3f,5f);
        var ship=shipObject.AddComponent<AudioSource>();ship.clip=beamAudio.clip;ship.outputAudioMixerGroup=beamAudio.outputAudioMixerGroup;
        ship.loop=true;ship.playOnAwake=true;ship.volume=0.09f;ship.pitch=0.65f;ship.spatialBlend=1f;ship.rolloffMode=AudioRolloffMode.Linear;
        ship.minDistance=4f;ship.maxDistance=32f;ship.dopplerLevel=0f;shipObject.AddComponent<AudioLowPassFilter>().cutoffFrequency=900f;
        var focus=living.AddComponent<WindowAmbienceFocus>();Ref(focus,"nightAmbience",night);Ref(focus,"windowWind",wind);Ref(focus,"shipAmbience",ship);
        Ref(focus,"beam",beam);Ref(focus,"functionalExterior",outsideFunctional);Ref(focus,"blockedExterior",outsideBlocked);Ref(focus,"neutralExterior",outsideNeutral);
        UnityEventTools.AddPersistentListener(Event<UnityEngine.Events.UnityEvent>(episode,"onWindowAttempt"),focus.BeginWindowAttempt);
        UnityEventTools.AddPersistentListener(Event<UnityEngine.Events.UnityEvent>(episode,"onWindowFocusShifted"),focus.ShiftToBlockedWindow);
        UnityEventTools.AddPersistentListener(Event<UnityEngine.Events.UnityEvent>(episode,"onRedirected"),focus.EndWindowAttempt);
        EditorUtility.SetDirty(episode);EditorUtility.SetDirty(journey);EditorUtility.SetDirty(night);EditorUtility.SetDirty(wind);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Dos ventanas detrás del sofá, recorrido/cues reubicados, isla retirada, ambiente doméstico gris y mezcla exterior/nave enlazada a los tres eventos reales. Sin Play.";
    }
}

