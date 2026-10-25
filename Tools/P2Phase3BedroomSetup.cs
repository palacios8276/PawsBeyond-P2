using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;

// Configuración puntual de autoría. Ejecutar una vez sobre Playground P2 fuera de Play.
public static class P2Phase3BedroomSetup
{
    static void Ref(UnityEngine.Object target, string name, UnityEngine.Object value)
    {
        var so = new SerializedObject(target); var property = so.FindProperty(name);
        if (property == null) throw new Exception("Campo inexistente: " + name);
        property.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Number(UnityEngine.Object target, string name, float value)
    {
        var so=new SerializedObject(target); so.FindProperty(name).floatValue=value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    static T Event<T>(UnityEngine.Object target,string name) where T:class =>
        target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target) as T;
    static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name;
        Undo.RegisterCreatedObjectUndo(g,"Agregar dormitorio Fase 3");
        g.transform.position=position; g.transform.localScale=size; g.transform.SetParent(parent,true);
        g.GetComponent<Renderer>().sharedMaterial=material; return g;
    }
    static void Cord(Transform parent,string name,Vector3 a,Vector3 b,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;
        g.transform.position=(a+b)*0.5f;g.transform.up=(b-a).normalized;
        g.transform.localScale=new Vector3(0.025f,(b-a).magnitude*0.5f,0.025f);g.transform.SetParent(parent,true);
        g.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
    }
    public static string Main()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying||scene.path!="Assets/Scenes/Playground.unity")throw new Exception("Abrir Playground P2 fuera de Play");
        if(GameObject.Find("BedroomEpisode"))throw new Exception("El dormitorio Fase 3 ya existe; ajustar desde Inspector");
        var player=GameObject.Find("Player");var owners=GameObject.Find("Owners");
        var journey=owners.GetComponent<OwnersJourney>();
        var beam=GameObject.Find("OwnersAbductionBeam").GetComponent<AbductionBeamController>();
        var progression=UnityEngine.Object.FindFirstObjectByType<EpisodeProgression>();
        var stone=GameObject.Find("BedFrame").GetComponent<Renderer>().sharedMaterial;
        var root=new GameObject("BedroomEpisode");Undo.RegisterCreatedObjectUndo(root,"Agregar tutorial Fase 3");
        var tutorial=root.AddComponent<BedroomTutorial>();
        if(!AssetDatabase.IsValidFolder("Assets/Prefabs/Tutorial"))AssetDatabase.CreateFolder("Assets/Prefabs","Tutorial");
        var toy=GameObject.CreatePrimitive(PrimitiveType.Sphere);toy.name="BedroomToy";toy.transform.localScale=Vector3.one*0.44f;
        toy.GetComponent<Renderer>().sharedMaterial=stone;
        var rb=toy.AddComponent<Rigidbody>();rb.mass=0.15f;rb.linearDamping=1f;rb.angularDamping=1f;
        rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.interpolation=RigidbodyInterpolation.Interpolate;
        toy.AddComponent<BasketDrag>();
        const string toyPath="Assets/Prefabs/Tutorial/BedroomToy.prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(toyPath))throw new Exception("El prefab de juguete ya existe");
        var prefab=PrefabUtility.SaveAsPrefabAsset(toy,toyPath);UnityEngine.Object.DestroyImmediate(toy);
        Vector3[] positions={new Vector3(-16.3f,0.34f,-0.3f),new Vector3(-15.0f,0.34f,0.9f),new Vector3(-17.57f,0.34f,2.1f)};
        string[] names={"Toy01_Practice","Toy02_Practice","Toy03_ChairLatch"};
        var toys=new BasketDrag[3];
        for(int i=0;i<3;i++)
        {
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform);
            Undo.RegisterCreatedObjectUndo(instance,"Agregar juguete Fase 3");instance.name=names[i];instance.transform.position=positions[i];
            toys[i]=instance.GetComponent<BasketDrag>();
        }
        toys[2].GetComponent<Rigidbody>().isKinematic=true;
        var chair=new GameObject("Chair_ShutterSupport_Greybox");chair.transform.SetParent(root.transform,false);
        Cube("Seat",chair.transform,new Vector3(-17.57f,0.65f,2.8f),new Vector3(0.9f,0.12f,0.9f),stone);
        Cube("Back",chair.transform,new Vector3(-17.57f,1.1f,3.2f),new Vector3(0.9f,0.85f,0.1f),stone);
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
            Cube("Leg",chair.transform,new Vector3(-17.57f+x*0.34f,0.37f,2.8f+z*0.34f),new Vector3(0.1f,0.54f,0.1f),stone);
        var cord=new GameObject("Cord_VisualLatch_Greybox");cord.transform.SetParent(root.transform,false);
        Cord(cord.transform,"ToyToChair",positions[2],new Vector3(-17.57f,0.65f,2.45f),stone);
        Cord(cord.transform,"ChairToShutter",new Vector3(-17.57f,1.45f,3.2f),new Vector3(-17.57f,2.8f,3.8f),stone);
        Cube("ShutterBottomStop",root.transform,new Vector3(-17.57f,0.80f,3.8f),new Vector3(3.30f,0.12f,0.20f),stone);
        var panel=Cube("BedroomShutter_Physical",root.transform,new Vector3(-17.57f,3.12f,3.8f),new Vector3(3.26f,1.44f,0.12f),stone);
        var panelBody=panel.AddComponent<Rigidbody>();panelBody.mass=2f;panelBody.isKinematic=true;
        panelBody.constraints=RigidbodyConstraints.FreezePositionX|RigidbodyConstraints.FreezePositionZ|RigidbodyConstraints.FreezeRotation;
        panelBody.interpolation=RigidbodyInterpolation.Interpolate;panelBody.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
        var shutter=root.AddComponent<BedroomShutter>();Ref(shutter,"attachedToy",toys[2]);Ref(shutter,"shutter",panelBody);Ref(shutter,"cord",cord);
        var target=new GameObject("BedroomWindow_AbductionHold");target.transform.SetParent(root.transform,false);target.transform.position=new Vector3(-17.57f,1.7f,2.6f);
        owners.transform.position=new Vector3(-21f,1.25f,-1.7f);
        var floats=owners.GetComponentsInChildren<OwnerFloat>();
        if(floats.Length!=2)throw new Exception("Se esperaban dos placeholders de dueños");
        for(int i=0;i<floats.Length;i++)
        {
            Undo.RecordObject(floats[i].transform,"Acostar placeholders de dueños");
            floats[i].transform.localPosition=new Vector3(i==0?-0.55f:0.55f,0f,0f);
            floats[i].transform.localRotation=Quaternion.Euler(90f,0f,0f);
            Number(floats[i],"lateralDistance",0.06f);Number(floats[i],"floatHeight",0.025f);
        }
        var canvas=new GameObject("BedroomTutorialCanvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));
        Undo.RegisterCreatedObjectUndo(canvas,"Agregar ayudas uGUI");canvas.transform.SetParent(root.transform,false);
        canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920f,1080f);scaler.matchWidthOrHeight=0.5f;
        var hintPanel=new GameObject("ContextHint",typeof(RectTransform),typeof(UnityEngine.UI.Image));hintPanel.transform.SetParent(canvas.transform,false);
        var rect=hintPanel.GetComponent<RectTransform>();rect.anchorMin=new Vector2(0.18f,0.06f);rect.anchorMax=new Vector2(0.82f,0.16f);rect.offsetMin=rect.offsetMax=Vector2.zero;
        var image=hintPanel.GetComponent<UnityEngine.UI.Image>();image.color=new Color(0.025f,0.035f,0.05f,0.86f);image.raycastTarget=false;
        var label=new GameObject("HintText",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));label.transform.SetParent(hintPanel.transform,false);
        rect=label.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(22f,10f);rect.offsetMax=new Vector2(-22f,-10f);
        var text=label.GetComponent<TMPro.TextMeshProUGUI>();text.font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if(!text.font)throw new Exception("Faltan recursos esenciales de TMP");
        text.fontSize=28f;text.alignment=TMPro.TextAlignmentOptions.Center;text.color=Color.white;text.raycastTarget=false;
        text.text="Movete con WASD / stick izquierdo";
        Ref(tutorial,"player",player.transform);Ref(tutorial,"viewingCamera",Camera.main.transform);
        Ref(tutorial,"interaction",player.GetComponent<PlayerInteraction>());Ref(tutorial,"bark",player.GetComponent<PlayerBark>());
        Ref(tutorial,"firstToy",toys[0]);Ref(tutorial,"secondToy",toys[1]);Ref(tutorial,"thirdToy",toys[2]);
        Ref(tutorial,"firstHighlight",toys[0].GetComponent<Renderer>());Ref(tutorial,"secondHighlight",toys[1].GetComponent<Renderer>());Ref(tutorial,"thirdHighlight",toys[2].GetComponent<Renderer>());
        Ref(tutorial,"shutter",shutter);Ref(tutorial,"journey",journey);Ref(tutorial,"abductionTarget",target.transform);
        Ref(tutorial,"beam",beam);Ref(tutorial,"progression",progression);Ref(tutorial,"hint",text);Ref(tutorial,"hintPanel",hintPanel);
        var so=new SerializedObject(tutorial);var array=so.FindProperty("owners");array.arraySize=floats.Length;for(int i=0;i<floats.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=floats[i];so.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(Event<UnityEvent>(shutter,"onClosed"),tutorial.OnShutterClosed);
        UnityEventTools.AddPersistentListener(Event<UnityEvent>(player.GetComponent<PlayerBark>(),"onBarked"),tutorial.OnBark);
        so=new SerializedObject(journey);so.FindProperty("automaticStart").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
        so=new SerializedObject(beam);so.FindProperty("beamActive").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
        beam.GetComponent<Renderer>().enabled=false;
        var beamSo=new SerializedObject(beam);var light=beamSo.FindProperty("beamLight").objectReferenceValue as Light;if(light)light.enabled=false;
        var audio=beamSo.FindProperty("beamAudio").objectReferenceValue as AudioSource;if(audio)audio.playOnAwake=false;
        EditorUtility.SetDirty(player.GetComponent<PlayerBark>());EditorUtility.SetDirty(shutter);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Dormitorio configurado: tres juguetes/prefab, silla y persiana física, ayudas TMP, dueños horizontales y comienzo por tutorial. Sin Play.";
    }
}

