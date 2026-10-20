using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;

// Herramienta de Editor: prepara copias de prueba; no agrega lógica al juego.
public static class P2PrepareRunwayLoops
{
    public static async Task<string> Main()
    {
        if(Application.isPlaying || !Application.dataPath.Replace('\\','/').EndsWith("/PawsBeyond-P2/Assets"))
            throw new Exception("Abrí PawsBeyond-P2 fuera de Play.");
        string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        var rows=new List<object>();
        foreach(string name in new[]{"NightHouse_Loop","TV_Nonverbal_Loop","FridgeMotor_Loop","WindowBreeze_Loop"})
        {
            string path=Path.Combine(root,"AudioSources/Originales/Runway",name+".mp3");
            string output="Assets/Audio/EnUso/Ambientes/"+name+".wav";
            if(File.Exists(Path.Combine(root,output))) throw new Exception("No sobrescribir el clip existente: "+output);
            using(var request=UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri,AudioType.MPEG))
            {
                _ = request.SendWebRequest(); var until=DateTime.UtcNow.AddSeconds(25);
                while(!request.isDone){if(DateTime.UtcNow>until)throw new Exception("Tiempo agotado al decodificar "+name);await Task.Delay(25);}
                if(request.result!=UnityWebRequest.Result.Success)throw new Exception(request.error);
                var clip=DownloadHandlerAudioClip.GetContent(request);
                if(!clip || clip.samples<1000)throw new Exception("Audio vacío: "+name);
                float[] raw=new float[clip.samples*clip.channels];
                if(!clip.GetData(raw,0))throw new Exception("No se pudo leer PCM: "+name);
                int channels=name=="NightHouse_Loop"?clip.channels:1;
                float[] input=new float[clip.samples*channels];
                for(int frame=0;frame<clip.samples;frame++) for(int c=0;c<channels;c++)
                {
                    if(channels==clip.channels) input[frame*channels+c]=raw[frame*clip.channels+c];
                    else {float sum=0f;for(int k=0;k<clip.channels;k++)sum+=raw[frame*clip.channels+k];input[frame]=sum/clip.channels;}
                }
                int fade=Math.Min((int)(clip.frequency*0.15f),clip.samples/4), frames=clip.samples-fade;
                float[] data=new float[frames*channels];
                Array.Copy(input,fade*channels,data,0,(clip.samples-2*fade)*channels);
                for(int i=0;i<fade;i++)for(int c=0;c<channels;c++)
                {
                    float t=(float)i/(fade-1);
                    data[(clip.samples-2*fade+i)*channels+c]=input[(clip.samples-fade+i)*channels+c]*(1-t)+input[i*channels+c]*t;
                }
                double energy=0;float peak=0;
                foreach(float x in data){energy+=x*x;peak=Math.Max(peak,Math.Abs(x));}
                float rms=(float)Math.Sqrt(energy/data.Length);
                float target=name=="NightHouse_Loop"?0.063f:name=="TV_Nonverbal_Loop"?0.10f:name=="FridgeMotor_Loop"?0.07f:0.08f;
                float gain=Math.Min(target/Math.Max(rms,0.000001f),0.5f/Math.Max(peak,0.000001f));
                for(int i=0;i<data.Length;i++)data[i]*=gain;
                WriteWav(Path.Combine(root,output),data,channels,clip.frequency);
                AssetDatabase.ImportAsset(output,ImportAssetOptions.ForceSynchronousImport);
                var importer=(AudioImporter)AssetImporter.GetAtPath(output);
                importer.forceToMono=channels==1; importer.loadInBackground=false;
                var settings=importer.defaultSampleSettings; settings.preloadAudioData=true;
                settings.loadType=name=="FridgeMotor_Loop"?AudioClipLoadType.DecompressOnLoad:AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat=name=="FridgeMotor_Loop"?AudioCompressionFormat.PCM:AudioCompressionFormat.Vorbis;
                settings.quality=name=="WindowBreeze_Loop"?0.65f:0.7f;
                settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings=settings; importer.SaveAndReimport();
                float seam=0;for(int c=0;c<channels;c++)seam=Math.Max(seam,Math.Abs(data[c]-data[data.Length-channels+c]));
                rows.Add(new{name,seconds=(double)frames/clip.frequency,frequency=clip.frequency,channels,
                    rmsDbFS=20*Math.Log10(rms*gain),peakDbFS=20*Math.Log10(peak*gain),seamDelta=seam,
                    loadType=settings.loadType.ToString(),compression=settings.compressionFormat.ToString(),quality=settings.quality,
                    pcmBytes=data.Length*2});
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }
        string json=Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(Path.Combine(root,"Docs/P2_AUDIO_RUNWAY_MEDICIONES.json"),json);
        AssetDatabase.SaveAssets(); return json;
    }
    static void WriteWav(string path,float[] data,int channels,int rate)
    {
        using(var w=new BinaryWriter(File.Create(path)))
        {
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+data.Length*2);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)channels);
            w.Write(rate);w.Write(rate*channels*2);w.Write((short)(channels*2));w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(data.Length*2);
            foreach(float sample in data)w.Write((short)Math.Round(Math.Max(-1f,Math.Min(1f,sample))*32767));
        }
    }
}
