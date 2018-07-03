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

    public bool BenchMark;
    
    public struct BenchmarkData
    {
        public readonly float Ms;
        public readonly float Fps;
        public float Time;

        public BenchmarkData(float ms, float fps, float timeSinceStart)
        {
            this.Ms = ms;
            this.Fps = fps;
            this.Time = timeSinceStart;
        }
    }

    public struct CSVData
    {
        public FrameData FrameData;
        public BenchmarkData BenchmarkData;

        public CSVData(FrameData frameData, BenchmarkData benchmarkData)
        {
            FrameData = frameData;
            BenchmarkData = benchmarkData;
        }
    }

    public struct FrameData
    {
        public readonly double fogDensityShaderTime;
        public readonly double applyBlurTime;
        public readonly double applySceneTime;
        public readonly double totalFrameTime;

        public FrameData(double fogDensityShaderTime, double applyBlurTime, double applySceneTime, double totalFrameTime)
        {
            this.fogDensityShaderTime = fogDensityShaderTime;
            this.applyBlurTime = applyBlurTime;
            this.applySceneTime = applySceneTime;
            this.totalFrameTime = totalFrameTime;
        }
    }
    
    public Dictionary<float, List<CSVData>> Data;

    private FrameData _currentFrameData;
    private Animator _animator;
    public float TimeSpent;
    
    // Use this for initialization
    void Awake ()
    {
        Data = new Dictionary<float, List<CSVData>>();
        
        _animator = GetComponent<Animator>();

        VolumetricFog fog = Camera.main.gameObject.GetComponent<VolumetricFog>();

        StartCoroutine(!BenchMark ? StartBenchMarks() : Fps());
    }

    private IEnumerator Fps()
    {
        while (true)
        {
            var time = Time.time;
            TimeSpent += (Time.unscaledDeltaTime - TimeSpent) * 0.1f;	
                   
            
            float ms = 1000.0f * TimeSpent;
            float fps = 1.0f / TimeSpent;
            float timeSinceStart = Mathf.Round(Time.time - time);

            Text.text = $"{fps:0.0} fps ({ms:0.} ms)";

            yield return null;

        }
    }

    public void SetFrameInfo(double fogDensityTime, double applyBlurTime, double applySceneTime, double totalTime)
    {
        _currentFrameData = new FrameData(fogDensityTime, applyBlurTime, applySceneTime, totalTime);
        
        var densityProc = fogDensityTime / totalTime * 100f;
        var blurProc = applyBlurTime / totalTime * 100f;
        var applyToSceneProc = applySceneTime / totalTime * 100f;
        
        Text2.text = $"Total frame time: {totalTime} ms\n" +
                     $"Calculate Density: {fogDensityTime:0.000} ms ({densityProc:0.0}%)\n" +
                     $"Apply Blur: {applyBlurTime:0.000} ms ({blurProc:0.0}%)\n" +
                     $"Apply To Scene: {applySceneTime:0.000} ms ({applyToSceneProc:0.0}%)";
    }
    
    private IEnumerator StartBenchMarks()
    {
        Screen.SetResolution(1920, 1080, true);
        yield return StartCoroutine(StartBench("1080p"));
    //    Screen.SetResolution(1280, 720, true);
        yield return StartCoroutine(StartBench("720p"));
     //   Screen.SetResolution(640, 480, true);
        yield return StartCoroutine(StartBench("480p"));
        
        Application.Quit();
    }

    
    private IEnumerator StartBench(string runName, bool writeToCSV = true)
    {



        TimeSpent = 0;
        Data = new Dictionary<float, List<CSVData>>();

        float timer = 0;
        
        // warmup to let the fps stabilize
        while ((timer += Time.deltaTime) < 4.0f)
        {
            TimeSpent += (Time.unscaledDeltaTime - TimeSpent) * 0.1f;
            
            yield return new WaitForSeconds(Time.deltaTime);
            
        }
        
        var time = Time.time;
        
        _animator.SetTrigger("StartBench");

        // wait for the animation to start
        yield return new WaitUntil(() => _animator.GetCurrentAnimatorStateInfo(0).IsName("Benchmark"));

        yield return new WaitUntil(() =>
        {
            TimeSpent += (Time.unscaledDeltaTime - TimeSpent) * 0.1f;	
           
    
            float ms = 1000.0f * TimeSpent;
            float fps = 1.0f / TimeSpent;
            float timeSinceStart = Mathf.Round(Time.time - time);

            List<CSVData> list;
            Data.TryGetValue(timeSinceStart, out list);
            
            if (list == null)
            {
                Data[timeSinceStart] = new List<CSVData>();
            }
            var benchMarkD = new BenchmarkData(ms, fps, timeSinceStart);
            var frameD = _currentFrameData;
            
            Data[timeSinceStart].Add(new CSVData(frameD, benchMarkD));

            Text.text = $"{fps:0.0} fps ({ms:0.} ms), time elapsed: {timeSinceStart} - {runName} ";
                
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
                "Time since start(s)" + "." + "FPS" + "." + "MS" + Environment.NewLine);
        }


        foreach (var data in Data)
        {
        
            
            string fps = data.Value.Average(val => val.BenchmarkData.Fps).ToString();
            string ms = data.Value.Average(val => val.BenchmarkData.Ms).ToString();
            string time = data.Key.ToString();
			
            File.AppendAllText(fileName, $"{time}.{fps}.{ms}" + Environment.NewLine);
        }

    }

    // Update is called once per frame
    void Update () {

    }
}