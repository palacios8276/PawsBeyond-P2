using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.Events;
using UnityEditor.SceneManagement;

public static class P2Phase1WaveSetup
{
    public static string Main()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying||scene.isDirty||scene.path!="Assets/Scenes/Playground.unity")throw new Exception("Usar Playground guardada fuera de Play");
        const string texturePath="Assets/Materials/BarkWaveDot.png";
        if(!File.Exists(texturePath))
        {
            var texture=new Texture2D(32,32,TextureFormat.RGBA32,false);
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {
                float radius=new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).magnitude;
                texture.SetPixel(x,y,new Color(1f,1f,1f,Mathf.SmoothStep(1f,0f,(radius-0.55f)/0.45f)));
            }
            texture.Apply();File.WriteAllBytes(texturePath,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
        }
        const string materialPath="Assets/Materials/BarkWave.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(!material)
        {
            var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");if(!shader)throw new Exception("Falta shader URP de partículas");
            material=new Material(shader);material.name="BarkWave";material.SetFloat("_Surface",1f);material.SetFloat("_Blend",0f);
            material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite",0f);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            AssetDatabase.CreateAsset(material,materialPath);
        }
        var origin=GameObject.Find("InteractionOrigin").transform;
        var child=origin.Find("BarkWave");GameObject effect;
        if(!child){effect=new GameObject("BarkWave");Undo.RegisterCreatedObjectUndo(effect,"Agregar onda de ladrido");effect.transform.SetParent(origin,false);}
        else effect=child.gameObject;
        var particles=effect.GetComponent<ParticleSystem>();if(!particles)particles=Undo.AddComponent<ParticleSystem>(effect);
        particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=particles.main;main.loop=false;main.playOnAwake=false;main.duration=0.4f;main.startLifetime=0.4f;main.startSpeed=0f;main.maxParticles=12;
        main.simulationSpace=ParticleSystemSimulationSpace.World;main.startSize=0.08f;
        var emission=particles.emission;emission.enabled=false;var shape=particles.shape;shape.enabled=false;
        var color=particles.colorOverLifetime;color.enabled=true;
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0f),new GradientColorKey(Color.white,1f)},new[]{new GradientAlphaKey(1f,0f),new GradientAlphaKey(0f,1f)});
        color.color=gradient;
        var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Billboard;
        var wave=effect.GetComponent<BarkWaveVfx>();if(!wave)wave=Undo.AddComponent<BarkWaveVfx>(effect);
        var so=new SerializedObject(wave);so.FindProperty("wave").objectReferenceValue=particles;so.FindProperty("mouth").objectReferenceValue=origin;so.ApplyModifiedProperties();
        var bark=GameObject.Find("Player").GetComponent<PlayerBark>();
        var field=typeof(PlayerBark).GetField("onBarked",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        var callback=(UnityEvent)field.GetValue(bark);Undo.RecordObject(bark,"Conectar onda al ladrido");
        for(int i=callback.GetPersistentEventCount()-1;i>=0;i--)if(callback.GetPersistentTarget(i)==wave)UnityEventTools.RemovePersistentListener(callback,i);
        UnityEventTools.AddPersistentListener(callback,wave.EmitWave);EditorUtility.SetDirty(bark);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Onda nativa configurada: 12 partículas, 0,4 s, origen InteractionOrigin, un único listener persistente";
    }
}
