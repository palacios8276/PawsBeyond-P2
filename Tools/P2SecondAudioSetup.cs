using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class P2SecondAudioSetup
{
    public static string Main()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying || scene.isDirty || scene.path!="Assets/Scenes/Playground.unity" || !Application.dataPath.Replace('\\','/').EndsWith("/PawsBeyond-P2/Assets"))throw new Exception("Abrí P2 guardado fuera de Play.");
        var originals=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(x=>x && !(x is DogFootstepAudio) && !(x is DogBarkAudio)).ToDictionary(x=>x,x=>EditorJsonUtility.ToJson(x));
        var untouched=new[]{"AUDIO-02_TV","AUDIO-05_WindowBreeze","AUDIO-01_NightAmbience"}
            .Select(n=>GameObject.Find(n).GetComponent<AudioSource>()).ToDictionary(x=>x,x=>EditorJsonUtility.ToJson(x));
        var mixer=AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/Mezcladores/PawsBeyondP2.mixer");
        var ambience=mixer.FindMatchingGroups("").Single(g=>g.name=="Ambience");var sfx=mixer.FindMatchingGroups("").Single(g=>g.name=="SFX");
        Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Segunda integración exclusivamente sonora");
        var fridge=GameObject.Find("AUDIO-03_Fridge").GetComponent<AudioSource>();Undo.RecordObject(fridge,"Presencia cercana de la heladera");
        fridge.volume=0.45f;fridge.minDistance=2.5f;fridge.maxDistance=7f;
        var sink=GameObject.Find("SinkBasin");
        var drip=Source("AUDIO-05_SinkDrip",sink.transform,new Vector3(21.5f,1.18f,3.8f),ambience,0.22f,1.2f,5.5f);
        drip.clip=Clip("Assets/Audio/EnUso/Ambientes/SinkDrip_Loop.wav");drip.loop=true;drip.playOnAwake=true;
        var player=GameObject.Find("Player");
        var paws=Source("AUDIO-08_DogPaws",player.transform,player.transform.position+Vector3.up*0.1f,sfx,0.45f,2.5f,8f);
        paws.loop=false;paws.playOnAwake=false;
        var steps=paws.GetComponent<DogFootstepAudio>();if(!steps)steps=Undo.AddComponent<DogFootstepAudio>(paws.gameObject);
        var so=new SerializedObject(steps);so.FindProperty("controller").objectReferenceValue=player.GetComponent<CharacterController>();
        so.FindProperty("audioSource").objectReferenceValue=paws;var variants=so.FindProperty("variants");variants.arraySize=3;
        for(int i=0;i<3;i++)variants.GetArrayElementAtIndex(i).objectReferenceValue=Clip("Assets/Audio/EnUso/Efectos/DogPaw_0"+(i+1)+".wav");so.ApplyModifiedProperties();
        var bark=Source("AUDIO-09_DogBark",player.transform,player.transform.position+Vector3.up*0.4f,sfx,0.45f,3f,12f);
        bark.loop=false;bark.playOnAwake=false;bark.clip=Clip("Assets/Audio/Pruebas/Efectos/DogBark_01.wav");
        var barking=bark.GetComponent<DogBarkAudio>();if(!barking)barking=Undo.AddComponent<DogBarkAudio>(bark.gameObject);
        var bs=new SerializedObject(barking);bs.FindProperty("audioSource").objectReferenceValue=bark;
        bs.FindProperty("variants").arraySize=1;bs.FindProperty("variants").GetArrayElementAtIndex(0).objectReferenceValue=bark.clip;bs.ApplyModifiedProperties();
        foreach(string name in new[]{"Owner_A_Capsule","Owner_B_Capsule"})
        {
            var owner=GameObject.Find(name);var snore=Source("AUDIO-10_Sleep_"+name,owner.transform,owner.transform.position,ambience,0.12f,1f,5f);
            snore.loop=true;snore.playOnAwake=false;snore.enabled=false;
        }
        foreach(var row in originals)if(EditorJsonUtility.ToJson(row.Key)!=row.Value)throw new Exception("Cambio jugable inesperado: "+row.Key.name);
        foreach(var row in untouched)if(EditorJsonUtility.ToJson(row.Key)!=row.Value)throw new Exception("Cambio de fuente no autorizado: "+row.Key.name);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "PASS: heladera ajustada; TV, brisa y ambiente intactos; goteo y pasos conectados; ladrido sin evento; dos ronquidos sin clips/estado, deshabilitados. Scripts jugables preexistentes intactos.";
    }
    static AudioClip Clip(string path){var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(!clip)throw new Exception("Falta "+path);return clip;}
    static AudioSource Source(string name,Transform parent,Vector3 world,AudioMixerGroup group,float volume,float min,float max)
    {
        var go=GameObject.Find(name);if(!go){go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Emisor sonoro");go.transform.SetParent(parent,true);go.transform.position=world;}
        var source=go.GetComponent<AudioSource>();if(!source)source=Undo.AddComponent<AudioSource>(go);Undo.RecordObject(source,"Parámetros de audio");
        source.spatialBlend=1;source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=min;source.maxDistance=max;source.volume=volume;
        source.dopplerLevel=0;source.outputAudioMixerGroup=group;return source;
    }
}
