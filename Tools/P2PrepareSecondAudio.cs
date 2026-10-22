using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
public static class P2PrepareSecondAudio
{
    public static async Task<string> Main()
    {
        if(Application.isPlaying || !Application.dataPath.Replace('\\','/').EndsWith("/PawsBeyond-P2/Assets"))throw new Exception("Abrí P2 fuera de Play.");
        foreach(string path in new[]{"Assets/Audio/EnUso/Efectos","Assets/Audio/Pruebas/Efectos"})
            if(!AssetDatabase.IsValidFolder(path))AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));
        var rows=new List<object>();string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        foreach(string name in new[]{"SinkDrip_Loop","DogPaw_01","DogPaw_02","DogPaw_03","DogBark_01"})
        {
            bool drip=name.StartsWith("Sink");bool paw=name.StartsWith("DogPaw");
            string output="Assets/Audio/"+(drip?"EnUso/Ambientes/":paw?"EnUso/Efectos/":"Pruebas/Efectos/")+name+".wav";
            if(File.Exists(Path.Combine(root,output)))throw new Exception("No sobrescribir "+output);
            using(var request=UnityWebRequestMultimedia.GetAudioClip(new Uri(Path.Combine(root,"AudioSources/Originales/Runway/SegundaIntegracion",name+".mp3")).AbsoluteUri,AudioType.MPEG))
            {
                _=request.SendWebRequest();var until=DateTime.UtcNow.AddSeconds(25);
                while(!request.isDone){if(DateTime.UtcNow>until)throw new Exception("Tiempo agotado");await Task.Delay(25);}
                if(request.result!=UnityWebRequest.Result.Success)throw new Exception(request.error);
                var clip=DownloadHandlerAudioClip.GetContent(request);var raw=new float[clip.samples*clip.channels];
                if(!clip.GetData(raw,0))throw new Exception("PCM no disponible");
                var mono=new float[clip.samples];for(int i=0;i<mono.Length;i++){for(int c=0;c<clip.channels;c++)mono[i]+=raw[i*clip.channels+c]/clip.channels;}
                float[] data;
                if(drip)
                {
                    int fade=(int)(clip.frequency*0.15f);data=new float[mono.Length-fade];
                    Array.Copy(mono,fade,data,0,mono.Length-2*fade);
                    for(int i=0;i<fade;i++){float t=(float)i/(fade-1);data[mono.Length-2*fade+i]=mono[mono.Length-fade+i]*(1-t)+mono[i]*t;}
                }
                else
                {
                    float peak0=0;foreach(float x in mono)peak0=Math.Max(peak0,Math.Abs(x));
                    int first=0;while(first<mono.Length-1 && Math.Abs(mono[first])<peak0*0.02f)first++;
                    first=Math.Max(0,first-(int)(clip.frequency*0.01f));
                    int last=mono.Length-1;while(last>first && Math.Abs(mono[last])<peak0*0.015f)last--;
                    int count=Math.Min(mono.Length-first,last-first+1+(int)(clip.frequency*0.03f));
                    if(paw)count=Math.Min(count,(int)(clip.frequency*0.30f));
                    data=new float[count];Array.Copy(mono,first,data,0,count);
                    int fadeIn=Math.Min(count/4,(int)(clip.frequency*0.005f)),fadeOut=Math.Min(count/4,(int)(clip.frequency*0.02f));
                    for(int i=0;i<fadeIn;i++)data[i]*=(float)i/fadeIn;
                    for(int i=0;i<fadeOut;i++)data[count-1-i]*=(float)i/fadeOut;
                }
                double e=0;float peak=0;foreach(float x in data){e+=x*x;peak=Math.Max(peak,Math.Abs(x));}
                float rms=(float)Math.Sqrt(e/data.Length);float gain=Math.Min((drip?0.07f:0.12f)/Math.Max(rms,0.000001f),0.45f/Math.Max(peak,0.000001f));
                for(int i=0;i<data.Length;i++)data[i]*=gain;
                using(var w=new BinaryWriter(File.Create(Path.Combine(root,output))))
                {
                    w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+data.Length*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                    w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(clip.frequency);w.Write(clip.frequency*2);w.Write((short)2);w.Write((short)16);
                    w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(data.Length*2);foreach(float x in data)w.Write((short)Math.Round(x*32767));
                }
                AssetDatabase.ImportAsset(output,ImportAssetOptions.ForceSynchronousImport);
                var importer=(AudioImporter)AssetImporter.GetAtPath(output);importer.forceToMono=true;importer.loadInBackground=false;
                var settings=importer.defaultSampleSettings;settings.preloadAudioData=true;
                settings.loadType=drip?AudioClipLoadType.CompressedInMemory:AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=drip?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM;
                settings.quality=0.7f;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings=settings;importer.SaveAndReimport();
                rows.Add(new{name,path=output,seconds=(double)data.Length/clip.frequency,peak=peak*gain,rms=rms*gain,frequency=clip.frequency,channels=1});
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }
        var json=Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(Path.Combine(root,"Docs/P2_AUDIO_SEGUNDA_RECURSOS.json"),json);return json;
    }
}
