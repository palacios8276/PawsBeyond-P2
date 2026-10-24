using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;

public static class P2Phase2BeamSetup
{
    public static string Main()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying||scene.path!="Assets/Scenes/Playground.unity")throw new Exception("Usar Playground P2 fuera de Play");
        const string path="Assets/Audio/EnUso/Ambientes/AbductionBeam_Loop.wav";
        if(!File.Exists(path))
        {
            const int rate=22050,count=rate*2;
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                for(int i=0;i<count;i++)
                {
                    double t=i/(double)rate;
                    double sample=(0.12*Math.Sin(2*Math.PI*110*t)+0.065*Math.Sin(2*Math.PI*165*t)+0.035*Math.Sin(2*Math.PI*330*t))*(0.75+0.25*Math.Sin(2*Math.PI*2*t));
                    sample+=0.012*Math.Sin(2*Math.PI*880*t)*(0.5+0.5*Math.Sin(2*Math.PI*17*t));
                    writer.Write((short)(sample*short.MaxValue));
                }
            }
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);importer.forceToMono=true;
            var sampleSettings=importer.defaultSampleSettings;sampleSettings.preloadAudioData=true;sampleSettings.loadType=AudioClipLoadType.DecompressOnLoad;sampleSettings.compressionFormat=AudioCompressionFormat.PCM;
            importer.defaultSampleSettings=sampleSettings;importer.SaveAndReimport();
        }
        var mixer=AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/Mezcladores/PawsBeyondP2.mixer");
        var ambience=mixer.FindMatchingGroups("").Single(g=>g.name=="Ambience");
        var owners=GameObject.Find("Owners").transform;var beam=GameObject.Find("OwnersAbductionBeam");
        var sound=owners.Find("AUDIO-04_AbductionBeam");
        if(!sound){var g=new GameObject("AUDIO-04_AbductionBeam");Undo.RegisterCreatedObjectUndo(g,"Agregar audio del rayo");g.transform.SetParent(owners,false);g.transform.localPosition=Vector3.up*0.45f;sound=g.transform;}
        var source=sound.GetComponent<AudioSource>();if(!source)source=Undo.AddComponent<AudioSource>(sound.gameObject);
        source.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);source.loop=true;source.playOnAwake=true;source.spatialBlend=1f;source.rolloffMode=AudioRolloffMode.Linear;
        source.minDistance=3f;source.maxDistance=16f;source.dopplerLevel=0f;source.volume=0.65f;source.outputAudioMixerGroup=ambience;
        var lightTransform=owners.Find("BeamGlow");
        if(!lightTransform){var g=new GameObject("BeamGlow");Undo.RegisterCreatedObjectUndo(g,"Agregar luz del rayo");g.transform.SetParent(owners,false);g.transform.localPosition=Vector3.up*0.6f;lightTransform=g.transform;}
        var light=lightTransform.GetComponent<Light>();if(!light)light=Undo.AddComponent<Light>(lightTransform.gameObject);
        light.type=LightType.Point;light.color=new Color(0.2f,0.85f,1f);light.intensity=2.5f;light.range=6f;light.shadows=LightShadows.None;
        var controller=beam.GetComponent<AbductionBeamController>();if(!controller)controller=Undo.AddComponent<AbductionBeamController>(beam);
        var so=new SerializedObject(controller);so.FindProperty("barkSource").objectReferenceValue=GameObject.Find("Player").GetComponent<PlayerBark>();
        so.FindProperty("beamRenderer").objectReferenceValue=beam.GetComponent<Renderer>();so.FindProperty("beamLight").objectReferenceValue=light;so.FindProperty("beamAudio").objectReferenceValue=source;so.ApplyModifiedProperties();
        var bark=GameObject.Find("Player").GetComponent<PlayerBark>();var callback=(UnityEvent)typeof(PlayerBark).GetField("onBarked",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(bark);
        Undo.RecordObject(bark,"Vincular ladridos con rayo");
        for(int i=callback.GetPersistentEventCount()-1;i>=0;i--)if(callback.GetPersistentTarget(i)==controller)UnityEventTools.RemovePersistentListener(callback,i);
        UnityEventTools.AddPersistentListener(callback,controller.OnBark);EditorUtility.SetDirty(bark);
        var tv=GameObject.Find("AUDIO-02_TV").GetComponent<AudioSource>();Undo.RecordObject(tv,"Reducir TV");tv.volume=0.25f;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Rayo configurado con audio mono sintetizado de 2 s, Ambience existente, 3D 3–16 m y TV 0,25. Sin Play ni pruebas automáticas.";
    }
}
