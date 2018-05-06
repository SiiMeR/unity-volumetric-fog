using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

public class Benchmark : MonoBehaviour
{

    public Text Text;
    public Text Text2;
    
    public struct BenchmarkData
    {
        public readonly float Ms;
        public readonly float Fps;
        public float Time;
        public float fogdensitycalcTime;
        public float applyblurTime;
        public float applysceneTime;

        public BenchmarkData(float ms, float fps, float time, float fogdensitycalcTime, float applyblurTime, float applysceneTime)
        {
            Ms = ms;
            Fps = fps;
            Time = time;
            this.fogdensitycalcTime = fogdensitycalcTime;
            this.applyblurTime = applyblurTime;
            this.applysceneTime = applysceneTime;
        }

        public BenchmarkData(float ms, float fps, float timeSinceStart)
        {
            this.Ms = ms;
            this.Fps = fps;
            this.Time = timeSinceStart;

            fogdensitycalcTime = 0;
            applyblurTime = 0;
            applysceneTime = 0;
        }
    }
    public Dictionary<float, List<BenchmarkData>> Data;
    
    private Animator _animator;
    public float TimeSpent;
    
    // Use this for initialization
    void Awake ()
    {
        Data = new Dictionary<float, List<BenchmarkData>>();
        
        _animator = GetComponent<Animator>();

        VolumetricFog fog = Camera.main.gameObject.GetComponent<VolumetricFog>();
        
        StartCoroutine(StartBenchMarks());

    }

    public void SetFrameInfo(double fogDensityTime, double applyBlurTime, double applySceneTime, double totalTime)
    {
        var densityProc = fogDensityTime / totalTime * 100f;
        var blurProc = applyBlurTime / totalTime * 100f;
        var applyToSceneProc = applySceneTime / totalTime * 100f;
        
        Text2.text = $"Total frame time: {totalTime} ms\n" +
                     $"Calculate Density {fogDensityTime:0.000} ms ({densityProc:0.0}%)\n" +
                     $"Apply Blur: {applyBlurTime:0.000} ms ({blurProc:0.0}%)\n" +
                     $"Apply To Scene: {applySceneTime:0.000} ms ({applyToSceneProc:0.0}%)";
    }
    
    private IEnumerator StartBenchMarks()
    {
        Screen.SetResolution(1920, 1080, true);
        yield return StartCoroutine(StartBench("1080p"));
        Screen.SetResolution(1280, 720, true);
        yield return StartCoroutine(StartBench("720p"));
        Screen.SetResolution(640, 480, true);
        yield return StartCoroutine(StartBench("480p"));
        
        Application.Quit();
    }

    
    private IEnumerator StartBench(string runName, bool writeToCSV = true)
    {



        TimeSpent = 0;
        Data = new Dictionary<float, List<BenchmarkData>>();

        float timer = 0;
        while ((timer += Time.deltaTime) < 2.0f)
        {
            TimeSpent += (Time.unscaledDeltaTime - TimeSpent) * 0.1f;
            
            yield return new WaitForSeconds(Time.deltaTime);
            
        }
        
        var time = Time.time;
        
        _animator.SetTrigger("StartBench");

        yield return new WaitUntil(() => _animator.GetCurrentAnimatorStateInfo(0).IsName("Benchmark"));

        yield return new WaitUntil(() =>
        {
            TimeSpent += (Time.unscaledDeltaTime - TimeSpent) * 0.1f;	
           
    
            float ms = 1000.0f * TimeSpent;
            float fps = 1.0f / TimeSpent;
            float timeSinceStart = Mathf.Round(Time.time - time);

            List<BenchmarkData> list;
            Data.TryGetValue(timeSinceStart, out list);
            
            if (list == null)
            {
                Data[timeSinceStart] = new List<BenchmarkData>();
            }
            
            Data[timeSinceStart].Add(new BenchmarkData(ms, fps,timeSinceStart));

            Text.text = $"{ms:0.0} ms ({fps:0.} fps), time elapsed: {timeSinceStart} - {runName} ";
                
            return !_animator.GetCurrentAnimatorStateInfo(0).IsName("Benchmark");
        });
		
		
        if (writeToCSV)
        {
            WriteToCSV(runName);
        }
       
        
       
        
    }

    private void WriteToCSV(string runName)
    {
        string fileName = Application.persistentDataPath + "/volumetricfog_" + runName + "_" +
                          DateTime.Now.ToFileTimeUtc() + ".csv";

        if (!File.Exists(fileName))
        {
            File.WriteAllText(fileName,
                "Time since start(s)" + "." + "FPS" + "." +  "Frame time(ms)" + Environment.NewLine);
        }


        foreach (var data in Data)
        {
            
            string ms = data.Value.Average(val => val.Ms).ToString();
            string fps = data.Value.Average(val => val.Fps).ToString();
            string time = data.Key.ToString();
			
            File.AppendAllText(fileName, $"{time}.{fps}.{ms}" + Environment.NewLine);
        }

    }

    // Update is called once per frame
    void Update () {

    }
}