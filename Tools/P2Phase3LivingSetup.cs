using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;

// Ajuste local de dos muros y vínculos del episodio; no modifica el resto de la vivienda.
public static class P2Phase3LivingSetup
{
    static void Ref(UnityEngine.Object target,string name,UnityEngine.Object value)
    {var so=new SerializedObject(target);so.FindProperty(name).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static T Event<T>(UnityEngine.Object target,string name) where T:class =>
        target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target) as T;
    static GameObject Cube(string name,Transform parent,Vector3 position,Vector3 size,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;
        Undo.RegisterCreatedObjectUndo(g,"Agregar ventana Fase 3");g.transform.position=position;g.transform.localScale=size;
        g.transform.SetParent(parent,true);g.GetComponent<Renderer>().sharedMaterial=material;return g;
    }
    static Material Material(string name,Color color)
    {
        string path="Assets/Materials/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material)return material;
        material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",color);
        AssetDatabase.CreateAsset(material,path);return material;
    }
    static void Window(GameObject wall,Transform parent,string name,bool normalX,float center,float width,float bottom,float top,Material glass,bool blocked)
    {
        var renderer=wall.GetComponent<Renderer>();var bounds=renderer.bounds;var material=renderer.sharedMaterial;
        float min=normalX?bounds.min.z:bounds.min.x,max=normalX?bounds.max.z:bounds.max.x;
        float low=center-width*0.5f,high=center+width*0.5f;
        if(low<=min||high>=max||bottom<=bounds.min.y||top>=bounds.max.y)throw new Exception("Ventana fuera del muro "+wall.name);
        var root=new GameObject(name);root.transform.SetParent(parent,false);
        Action<string,float,float,float,float> segment=(part,a,b,y0,y1)=>
        {
            var p=bounds.center;var size=bounds.size;
            if(normalX){p.z=(a+b)*0.5f;size.z=b-a;}else{p.x=(a+b)*0.5f;size.x=b-a;}
            p.y=(y0+y1)*0.5f;size.y=y1-y0;Cube(part,root.transform,p,size,material);
        };
        segment("FrameLeft",min,low,bounds.min.y,bounds.max.y);
        segment("FrameRight",high,max,bounds.min.y,bounds.max.y);
        segment("Sill",low,high,bounds.min.y,bottom);
        segment("Lintel",low,high,top,bounds.max.y);
        var position=bounds.center;var panelSize=bounds.size;
        if(normalX){position.z=center;panelSize.z=width;}else{position.x=center;panelSize.x=width;}
        position.y=(top+bottom)*0.5f;panelSize.y=top-bottom;
        Cube(blocked?"PreviouslyBlockedPanel":"WindowGlass_Greybox",root.transform,position,panelSize,blocked?Material("P2BlockedShutterGreybox",new Color(0.23f,0.24f,0.27f)):glass);
        Undo.RecordObject(renderer,"Abrir hueco local de ventana");renderer.enabled=false;
        foreach(var collider in wall.GetComponents<Collider>()){Undo.RecordObject(collider,"Reemplazar colisión del paño por marco");collider.enabled=false;}
    }
    static Light Cue(Transform parent,string name,Vector3 position,Color color)
    {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.position=position;
        var light=g.AddComponent<Light>();light.type=LightType.Point;light.color=color;light.intensity=2.5f;light.range=5f;light.shadows=LightShadows.None;light.enabled=false;return light;
    }
    public static string Main()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying||scene.path!="Assets/Scenes/Playground.unity")throw new Exception("Usar Playground P2 fuera de Play");
        if(GameObject.Find("LivingRoomEpisode"))throw new Exception("La sala Fase 3 ya existe; ajustar desde Inspector");
        var journey=GameObject.Find("Owners").GetComponent<OwnersJourney>();
        var beam=UnityEngine.Object.FindFirstObjectByType<AbductionBeamController>();
        var progression=UnityEngine.Object.FindFirstObjectByType<EpisodeProgression>();
        var root=new GameObject("LivingRoomEpisode");Undo.RegisterCreatedObjectUndo(root,"Integrar sala Fase 3");
        var episode=root.AddComponent<LivingRoomEpisode>();
        var glass=Material("P2WindowGreybox",new Color(0.10f,0.24f,0.30f));
        Window(GameObject.Find("LRWallNorth (1)"),root.transform,"LivingWindow_Functional",true,7.5f,2.4f,0.9f,2.2f,glass,false);
        Window(GameObject.Find("LRWallWest"),root.transform,"LivingWindow_PreviouslyBlocked",false,5.4f,2.4f,0.9f,2.2f,glass,true);
        var dark=Material("P2AlienSilhouetteGreybox",new Color(0.015f,0.025f,0.035f));
        var silhouette=new GameObject("AlienSilhouette_ScriptedGreybox");silhouette.transform.SetParent(root.transform,false);
        // Señal interior pegada a la ventana: visible desde la cámara, sin colisión ni IA.
        var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.name="SilhouetteBody";body.transform.SetParent(silhouette.transform,false);
        body.transform.position=new Vector3(13.30f,1.48f,7.5f);body.transform.localScale=new Vector3(0.06f,0.40f,0.34f);
        body.GetComponent<Renderer>().sharedMaterial=dark;UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
        var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.name="SilhouetteHead";head.transform.SetParent(silhouette.transform,false);
        head.transform.position=new Vector3(13.29f,1.96f,7.5f);head.transform.localScale=new Vector3(0.07f,0.42f,0.50f);
        head.GetComponent<Renderer>().sharedMaterial=dark;UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
        silhouette.SetActive(false);
        var functionalLight=Cue(root.transform,"FunctionalWindowCue",new Vector3(12.5f,2f,7.5f),new Color(0.2f,0.85f,1f));
        var blockedLight=Cue(root.transform,"BlockedWindowCue",new Vector3(5.4f,2f,10.4f),new Color(0.8f,0.25f,1f));
        Ref(episode,"progression",progression);Ref(episode,"journey",journey);Ref(episode,"beam",beam);
        Ref(episode,"alienSilhouette",silhouette);Ref(episode,"functionalWindowLight",functionalLight);Ref(episode,"blockedWindowLight",blockedLight);
        var tv=GameObject.Find("AUDIO-02_TV").GetComponent<AudioSource>();
        if(tv.GetComponent<TVBeamInterference>())throw new Exception("El televisor ya tiene respuesta al rayo");
        var tvResponse=Undo.AddComponent<TVBeamInterference>(tv.gameObject);Ref(tvResponse,"beam",beam);Ref(tvResponse,"beamVolume",beam.GetComponent<Renderer>());
        // El valor 0,25 y el grupo Ambience existentes se conservan.
        Undo.RecordObject(tv,"Condicionar reproducción del televisor");tv.playOnAwake=false;
        var so=new SerializedObject(journey);var waypoints=so.FindProperty("waypoints");
        var functionalPoint=(Transform)waypoints.GetArrayElementAtIndex(4).objectReferenceValue;
        var blockedPoint=(Transform)waypoints.GetArrayElementAtIndex(5).objectReferenceValue;
        Undo.RecordObjects(new UnityEngine.Object[]{functionalPoint,blockedPoint},"Ajustar dos puntos del recorrido de sala");
        functionalPoint.position=new Vector3(11.5f,1.6f,7.5f);blockedPoint.position=new Vector3(5.4f,1.6f,9.6f);
        UnityEventTools.AddPersistentListener(Event<JourneyWaypointEvent>(journey,"onWaypointReached"),episode.OnWaypointReached);
        UnityEventTools.AddPersistentListener(Event<UnityEvent>(beam,"onUncontrolled"),episode.OnBeamUncontrolled);
        so=new SerializedObject(progression);so.FindProperty("livingRoomContentReady").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(journey);EditorUtility.SetDirty(beam);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Sala integrada con dos ventanas locales, silueta, luces, TV existente y una redirección hacia baño. Sin Play.";
    }
}

