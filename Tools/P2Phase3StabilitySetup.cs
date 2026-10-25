using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class P2Phase3StabilitySetup
{
    public static string Main()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying||scene.path!="Assets/Scenes/Playground.unity")throw new Exception("Usar Playground P2 fuera de Play");
        var player=GameObject.Find("Player").GetComponent<CharacterController>();
        foreach(var door in UnityEngine.Object.FindObjectsByType<DoorTriggerDetector>(FindObjectsSortMode.None))
        {
            var so=new SerializedObject(door);so.FindProperty("allowedPlayer").objectReferenceValue=player;so.ApplyModifiedPropertiesWithoutUndo();
        }
        const string materialPath="Assets/Materials/BedroomToyFriction.physicMaterial";
        var friction=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(materialPath);
        if(!friction){friction=new PhysicsMaterial("BedroomToyFriction");AssetDatabase.CreateAsset(friction,materialPath);}
        friction.dynamicFriction=0.6f;friction.staticFriction=0.8f;friction.bounciness=0f;
        friction.frictionCombine=PhysicsMaterialCombine.Maximum;friction.bounceCombine=PhysicsMaterialCombine.Minimum;EditorUtility.SetDirty(friction);
        const string prefabPath="Assets/Prefabs/Tutorial/BedroomToy.prefab";
        var root=PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var body=root.GetComponent<Rigidbody>();body.mass=0.8f;body.linearDamping=3f;body.angularDamping=3f;
            body.maxLinearVelocity=6.5f;body.maxAngularVelocity=5f;
            root.GetComponent<Collider>().sharedMaterial=friction;
            if(!root.GetComponent<BedroomToyPhysics>())root.AddComponent<BedroomToyPhysics>();
            var so=new SerializedObject(root.GetComponent<BasketDrag>());so.FindProperty("followStrength").floatValue=8f;so.FindProperty("maxSpeed").floatValue=6f;so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var third=GameObject.Find("Toy03_ChairLatch").GetComponent<BedroomToyPhysics>();
        if(!third)throw new Exception("No se propagó el componente al tercer juguete");
        var thirdSo=new SerializedObject(third);thirdSo.FindProperty("keepInBedroom").boolValue=false;thirdSo.ApplyModifiedPropertiesWithoutUndo();
        // El tercer juguete conserva su estado kinematic y la traba del amarre.
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Puertas vinculadas solo al CharacterController del Player y ajustes específicos del prefab de juguetes. Sin Play.";
    }
}

