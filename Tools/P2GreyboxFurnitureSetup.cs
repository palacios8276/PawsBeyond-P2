using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class P2GreyboxFurnitureSetup
{
    static GameObject Piece(string name,Transform parent,Vector3 localPosition,Vector3 size,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;
        Undo.RegisterCreatedObjectUndo(g,"Completar mobiliario gris");
        g.transform.SetParent(parent,false);g.transform.localPosition=localPosition;g.transform.localScale=size;
        g.GetComponent<Renderer>().sharedMaterial=material;return g;
    }
    static void Chair(Transform parent,string name,Vector3 position,float yaw,Material material)
    {
        var g=new GameObject(name);Undo.RegisterCreatedObjectUndo(g,"Agregar silla gris");
        g.transform.SetParent(parent,false);g.transform.position=position;
        Piece("Seat",g.transform,new Vector3(0f,0.48f,0f),new Vector3(0.7f,0.1f,0.7f),material);
        Piece("Back",g.transform,new Vector3(0f,0.9f,0.3f),new Vector3(0.7f,0.75f,0.1f),material);
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
            Piece("Leg",g.transform,new Vector3(x*0.27f,0.27f,z*0.27f),new Vector3(0.08f,0.34f,0.08f),material);
        g.transform.rotation=Quaternion.Euler(0f,yaw,0f);
    }
    public static string Main()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying||scene.path!="Assets/Scenes/Playground.unity")throw new Exception("Abrir Playground P2 fuera de Play");
        if(GameObject.Find("ToiletFixture_Greybox"))throw new Exception("El ajuste ya está aplicado; usar Inspector para cambios posteriores");
        var bathroom=GameObject.Find("BathroomFurniture").transform;
        var toilet=GameObject.Find("ToiletBase");var tank=GameObject.Find("ToiletTank");
        var material=toilet.GetComponent<Renderer>().sharedMaterial;
        var fixture=new GameObject("ToiletFixture_Greybox");Undo.RegisterCreatedObjectUndo(fixture,"Orientar inodoro hacia el baño");
        fixture.transform.SetParent(bathroom,false);fixture.transform.position=toilet.transform.position;
        Undo.SetTransformParent(toilet.transform,fixture.transform,"Agrupar inodoro");
        Undo.SetTransformParent(tank.transform,fixture.transform,"Agrupar mochila");
        Piece("SeatLeft",fixture.transform,new Vector3(-0.23f,0.33f,-0.1f),new Vector3(0.12f,0.07f,0.58f),material);
        Piece("SeatRight",fixture.transform,new Vector3(0.23f,0.33f,-0.1f),new Vector3(0.12f,0.07f,0.58f),material);
        Piece("SeatFront",fixture.transform,new Vector3(0f,0.33f,-0.36f),new Vector3(0.46f,0.07f,0.1f),material);
        Piece("SeatBack",fixture.transform,new Vector3(0f,0.33f,0.16f),new Vector3(0.46f,0.07f,0.1f),material);
        fixture.transform.rotation=Quaternion.Euler(0f,90f,0f);fixture.transform.position+=Vector3.right*0.4f;
        foreach(string name in new[]{"SinkCabinet","SinkBasin"})
        {
            var g=GameObject.Find(name);Undo.RecordObject(g.transform,"Acercar lavamanos a la pared");g.transform.position+=Vector3.forward*0.5f;
        }
        foreach(string name in new[]{"TubBase","TubBack","TubFront","TubEndA","TubEndB"})
        {
            var g=GameObject.Find(name);Undo.RecordObject(g.transform,"Ubicar bañera junto a pared");g.transform.position-=Vector3.forward*0.65f;
        }
        var kitchen=GameObject.Find("KitchenFurniture").transform;
        var table=new GameObject("KitchenDiningTable_Greybox");Undo.RegisterCreatedObjectUndo(table,"Completar mesa de cocina");
        table.transform.SetParent(kitchen,false);table.transform.position=new Vector3(8f,0f,-12.65f);
        var tableMaterial=GameObject.Find("Countertop").GetComponent<Renderer>().sharedMaterial;
        Piece("Tabletop",table.transform,new Vector3(0f,0.85f,0f),new Vector3(1.6f,0.12f,1.1f),tableMaterial);
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
            Piece("Leg",table.transform,new Vector3(x*0.65f,0.445f,z*0.4f),new Vector3(0.12f,0.69f,0.12f),material);
        Chair(kitchen,"KitchenChairEast_Greybox",new Vector3(9.3f,0f,-12.65f),90f,material);
        Chair(kitchen,"KitchenChairSouth_Greybox",new Vector3(8f,0f,-13.65f),180f,material);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Inodoro girado 90 grados hacia el centro, lavamanos y bañera junto a pared, mesa y dos sillas grises. Sin cambios en mecánicas/materiales ni Play.";
    }
}
