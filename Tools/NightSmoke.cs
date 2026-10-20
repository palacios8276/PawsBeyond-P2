using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class NightSmoke
{
    public static async Task<string> Main()
    {
        bool bg=Application.runInBackground;Application.runInBackground=true;
        try
        {
            await Task.Delay(600);
            var cam=Camera.main;
            if(cam.clearFlags!=CameraClearFlags.SolidColor || cam.backgroundColor.maxColorComponent>0.1f
                || RenderSettings.ambientMode!=AmbientMode.Flat)throw new Exception("Night background invalid");
            if(cam.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing)
                throw new Exception("Post effects enabled");
            var moon=GameObject.Find("Directional Light").GetComponent<Light>();
            if(moon.intensity>0.3f || moon.color.b<=moon.color.r)throw new Exception("Moonlight not dim/cool");
            var lamps=GameObject.Find("NightInteriorLights").GetComponentsInChildren<Light>();
            if(lamps.Length!=5)throw new Exception("Unexpected interior lighting count");
            foreach(var lamp in lamps)
                if(lamp.type!=LightType.Point || lamp.lightmapBakeType!=LightmapBakeType.Realtime || lamp.color.r<=lamp.color.b)
                    throw new Exception("Interior lamp configuration invalid");
            Vector3 dog=cam.WorldToViewportPoint(GameObject.Find("PlaceholderDog").GetComponent<Renderer>().bounds.center);
            if(dog.z<=0f || dog.x<0.25f || dog.x>0.75f || dog.y<0.05f || dog.y>0.65f)
                throw new Exception("Dog not in playable night frame: "+dog);
            return "AUTOMATED PASS: simple night, cool moon, five warm realtime points, no post, dog framed. Brightness/readability requires human review.";
        }
        finally{Application.runInBackground=bg;}
    }
}
