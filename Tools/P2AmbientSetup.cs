using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;

// Editor-only helper outside Assets. Adds/configures audio children; never edits gameplay code.
public static class P2AmbientSetup
{
    public static string Main()
    {
        if (!Application.dataPath.Replace('\\','/').EndsWith("/PawsBeyond-P2/Assets"))
            throw new Exception("P2 project required.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/Playground.unity" || scene.isDirty)
            throw new Exception("Open saved Playground in Edit mode first.");
        var listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (listeners.Count(x=>x.isActiveAndEnabled)!=1) throw new Exception("Expected exactly one active AudioListener.");
        var original = UnityEngine.Object.FindObjectsByType<Component>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(x=>x && !(x is Transform) && !(x is AudioSource)).ToDictionary(x=>x, x=>EditorJsonUtility.ToJson(x));
        var transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .ToDictionary(x=>x,x=>new TransformSnapshot { position=x.localPosition, rotation=x.localRotation, scale=x.localScale, parent=x.parent });
        foreach (var path in new[]{"Assets/Audio","Assets/Audio/EnUso","Assets/Audio/EnUso/Ambientes","Assets/Audio/Mezcladores"})
            if(!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(path).Replace('\\','/'),System.IO.Path.GetFileName(path));
        var mixPaths = AssetDatabase.FindAssets("t:AudioMixer",new[]{"Assets"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        var groups = mixPaths.Select(AssetDatabase.LoadAssetAtPath<AudioMixer>).SelectMany(m=>m.FindMatchingGroups(""))
            .Where(g=>g.name=="Ambience").ToArray();
        if(groups.Length>1) throw new Exception("Several Ambience groups; choose the intended mixer manually.");
        AudioMixerGroup group = groups.SingleOrDefault();
        Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("P2 domestic ambience preparation");
        var rows = new List<string>();
        Make("AUDIO-01_NightAmbience",null,new Vector3(0,0,0),0f,0.20f,1f,1f,"NightHouse_Loop",group,rows);
        var tv=GameObject.Find("TV"); var fridge=GameObject.Find("Fridge"); var bedroom=GameObject.Find("BedroomFurniture");
        if(tv) Make("AUDIO-02_TV",tv.transform,tv.transform.position,1f,0.35f,1.5f,10f,"TV_Nonverbal_Loop",group,rows);
        if(fridge) Make("AUDIO-03_Fridge",fridge.transform,new Vector3(1f,0.45f,-19f),1f,0.45f,2.5f,7f,"FridgeMotor_Loop",group,rows);
        if(bedroom && GameObject.Find("BRWall_west (2)") && GameObject.Find("BRWall_west (3)"))
            Make("AUDIO-05_WindowBreeze",bedroom.transform,new Vector3(-17.6f,1.5f,3.85f),1f,0.25f,1f,6f,"WindowBreeze_Loop",group,rows);
        var changed=original.Where(x=>EditorJsonUtility.ToJson(x.Key)!=x.Value).Select(x=>x.Key.name+":"+x.Key.GetType().Name).ToArray();
        var moved=transforms.Where(x=>x.Key.localPosition!=x.Value.position || x.Key.localRotation!=x.Value.rotation
            || x.Key.localScale!=x.Value.scale || x.Key.parent!=x.Value.parent).Select(x=>x.Key.name).ToArray();
        if(changed.Length>0 || moved.Length>0){Undo.RevertAllDownToGroup(undo); throw new Exception("Unexpected existing-component/transform change: "+string.Join(",",changed.Concat(moved)));}
        Undo.CollapseUndoOperations(undo);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        return "PREPARED; no existing gameplay component/transform changed; active listeners=1\n"+string.Join("\n",rows)
            +"\nMixer routing: "+(group?"Ambience assigned":"PENDING: create mixer/groups in Audio Mixer UI")
            +"\nNo fan present. Creaks deferred. No runtime scripts added.";
    }
    sealed class TransformSnapshot { public Vector3 position, scale; public Quaternion rotation; public Transform parent; }
    static void Make(string name,Transform parent,Vector3 position,float spatial,float volume,float min,float max,
        string clipName,AudioMixerGroup group,List<string> rows)
    {
        var go=GameObject.Find(name);
        if(!go){go=new GameObject(name); Undo.RegisterCreatedObjectUndo(go,"Create audio emitter"); go.transform.SetParent(parent,true); go.transform.position=position;}
        var source=go.GetComponent<AudioSource>(); if(!source) source=Undo.AddComponent<AudioSource>(go);
        Undo.RecordObject(source,"Configure ambience");
        var clips=AssetDatabase.FindAssets(clipName+" t:AudioClip",new[]{"Assets/Audio/EnUso/Ambientes"});
        if(!source.clip && clips.Length==1) source.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(clips[0]));
        source.playOnAwake=true; source.loop=true; source.spatialBlend=spatial; source.volume=volume;
        source.dopplerLevel=0f; source.rolloffMode=AudioRolloffMode.Linear; source.minDistance=min; source.maxDistance=max;
        source.outputAudioMixerGroup=group;
        // Prepared sources remain silent/disabled until BOTH a real clip and routing exist.
        source.enabled=source.clip && group;
        rows.Add(name+" pos="+go.transform.position+" spatial="+spatial+" vol="+volume+" min="+min+" max="+max
            +" clip="+(source.clip?source.clip.name:"PENDING "+clipName)+" enabled="+source.enabled);
    }
}
