using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Benchmark : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI _fpsText;
    [SerializeField] private TextMeshProUGUI _frametimeText;

    public bool benchmarkModeEnabled;
    
    public struct BenchmarkData
    {
        public readonly float Ms;
        public readonly float Fps;
        public float Time;

        public BenchmarkData(float ms, float fps, float timeSinceStart)
        {
            Ms = ms;
            Fps = fps;
            Time = timeSinceStart;
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
        
        StartCoroutine(benchmarkModeEnabled ? RunBenchmarks() : DisplayFps());
    }

    private IEnumerator DisplayFps()
    {
        while (true)
        {
            TimeSpent += (Time.unscaledDeltaTime - TimeSpent) * 0.1f;	
            
            var ms = 1000.0f * TimeSpent;
            var fps = 1.0f / TimeSpent;

            _fpsText.SetText($"FPS: {fps:0.0}");
            _frametimeText.SetText($"Frametime: {ms:0.00}ms");

            yield return null;
        }
    }
    
    private IEnumerator RunBenchmarks()
    {
        Screen.SetResolution(1920, 1080, true);
        yield return StartCoroutine(StartBench("1080p"));
        Screen.SetResolution(1280, 720, true);
        yield return StartCoroutine(StartBench("720p"));
        Screen.SetResolution(640, 480, true);
        yield return StartCoroutine(StartBench("480p"));
        
        Application.Quit();
    }

    
    private IEnumerator StartBench(string runName, bool writeToCsv = true)
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
           
    
            var ms = 1000.0f * TimeSpent;
            var fps = 1.0f / TimeSpent;
            var timeSinceStart = Mathf.Round(Time.time - time);

            List<CSVData> list;
            Data.TryGetValue(timeSinceStart, out list);
            
            if (list == null)
            {
                Data[timeSinceStart] = new List<CSVData>();
            }
            var benchMarkD = new BenchmarkData(ms, fps, timeSinceStart);
            var frameD = _currentFrameData;
            
            Data[timeSinceStart].Add(new CSVData(frameD, benchMarkD));
            
            _fpsText.SetText($"{fps:0.0} FPS");
            _frametimeText.SetText($"{ms:0.} ms, time elapsed: {timeSinceStart} - {runName}");
            
            return !_animator.GetCurrentAnimatorStateInfo(0).IsName("Benchmark");
        });
		
		
        if (writeToCsv)
        {
            WriteToCsv(runName);
        }
        
    }

    private void WriteToCsv(string runName)
    {
        var fileName = Application.persistentDataPath + "/volumetricfog_" + runName + "_" +
                       DateTime.Now.ToFileTimeUtc() + ".csv";

        if (!File.Exists(fileName))
        {
            File.WriteAllText(fileName,
                "Time since start(s)" + "." + "FPS" + "." + "MS" + Environment.NewLine);
        }


        foreach (var data in Data)
        {
            var fps = data.Value.Average(val => val.BenchmarkData.Fps).ToString(CultureInfo.InvariantCulture);
            var ms = data.Value.Average(val => val.BenchmarkData.Ms).ToString(CultureInfo.InvariantCulture);
            var time = data.Key.ToString(CultureInfo.InvariantCulture);
			
            File.AppendAllText(fileName, $"{time}.{fps}.{ms}" + Environment.NewLine);
        }

    }
}