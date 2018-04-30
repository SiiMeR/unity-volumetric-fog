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

    [SerializeField] private Text _text;
    private struct Triplet
    {
        public float ms;
        public float fps;
        public float time;

        public Triplet(float ms, float fps, float time)
        {
            this.ms = ms;
            this.fps = fps;
            this.time = time;
        }


    }
	
    private Animator _animator;
    private float _timeSpent;
    private bool _isRunning;
    private Dictionary<float, List<Triplet>> _triplets;
    // Use this for initialization
    void Awake ()
    {
        _triplets = new Dictionary<float, List<Triplet>>();
        
        _animator = GetComponent<Animator>();

        VolumetricFog fog = Camera.main.gameObject.GetComponent<VolumetricFog>();
        StartCoroutine(StartBench("Base"));

    }
    

    private IEnumerator StartBench(string runName, bool writeToCSV = true)
    {

        float timer = 0;

        while ((timer += Time.deltaTime) < 2.0f)
        {
            _timeSpent += (Time.unscaledDeltaTime - _timeSpent) * 0.1f;
            
            yield return new WaitForSeconds(Time.deltaTime);
            
        }
        

        float time = Time.time;
        
        _animator.SetTrigger("StartBench");
        
        while(!_animator.GetCurrentAnimatorStateInfo(0).IsName("Benchmark"))
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        

        yield return new WaitUntil(() =>
        {
            _timeSpent += (Time.unscaledDeltaTime - _timeSpent) * 0.1f;	
           
    
            float ms = 1000.0f * _timeSpent;
            float fps = 1.0f / _timeSpent;
            float timeSinceStart = Mathf.Round(Time.time - time);

            List<Triplet> list;
            _triplets.TryGetValue(timeSinceStart, out list);
            
            if (list == null)
            {
                _triplets[timeSinceStart] = new List<Triplet>();
            }
            
            _triplets[timeSinceStart].Add(new Triplet(ms, fps,timeSinceStart));

            _text.text = $"{ms:0.0} ms ({fps:0.} fps), time elapsed: {timeSinceStart} - {runName}";
                
            return !_animator.GetCurrentAnimatorStateInfo(0).IsName("Benchmark");
        });
		
		
        if (writeToCSV)
        {
            WriteToCSV(runName);
        }
       
        
        Application.Quit();
        
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


        foreach (var triplet in _triplets)
        {
            
            string ms = triplet.Value.Average(val => val.ms).ToString();
            string fps = triplet.Value.Average(val => val.fps).ToString();
            string time = triplet.Key.ToString();
			
            File.AppendAllText(fileName, $"{time}.{fps}.{ms}" + Environment.NewLine);
        }

    }

    // Update is called once per frame
    void Update () {

    }
}