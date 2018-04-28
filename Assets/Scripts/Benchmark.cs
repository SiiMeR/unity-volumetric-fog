using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

public class Benchmark : MonoBehaviour
{

    [SerializeField] private Text _text;
    private struct Pair
    {
        public float ms;
        public float fps;

        public Pair(float ms, float fps)
        {
            this.ms = ms;
            this.fps = fps;
        }


    }
	
    private Animator _animator;
    private float _timeSpent;

    private List<Pair> _pairs;
    // Use this for initialization
    void Awake ()
    {
        _pairs = new List<Pair>();
		
        _animator = GetComponent<Animator>();
		
        Profiler.enabled = true;
        //	Profiler.logFile = Application.persistentDataPath + "/volumetricfog_" + DateTime.Now.ToFileTimeUtc() + ".txt";
        //	Profiler.enableBinaryLog = true;

        StartCoroutine(StartBench());

    }


    private IEnumerator StartBench()
    {
        //Profiler.BeginSample("Volumetric Fog Benchmark");

        	yield return new WaitUntil(() =>
            {
                _timeSpent += (Time.unscaledDeltaTime - _timeSpent) * 0.1f;	
           
    
                float ms = 1000.0f * _timeSpent;
                float fps = 1.0f / _timeSpent;
                
                _pairs.Add(new Pair(ms, fps));
                
                _text.text = $"{ms:0.0} ms ({fps:0.} fps)";
                
                return !_animator.GetCurrentAnimatorStateInfo(0).IsName("Benchmark");
            });
		
		
        //Profiler.EndSample();


        WriteToCSV();
	
        Application.Quit();

    }

    private void WriteToCSV()
    {
        string fileName = Application.persistentDataPath + "/volumetricfog_" + DateTime.Now.ToFileTimeUtc() + ".csv";

        if (!File.Exists(fileName))
        {
            File.WriteAllText(fileName, "Frame time(ms)" + "," + "FPS" + Environment.NewLine);
        }
		
       // _pairs.RemoveRange(0,4);
		
        _pairs.ForEach(pair =>
        {	
	
            string ms = pair.ms.ToString().Replace(",", ".");
            string fps = pair.fps.ToString().Replace(",", ".");
			
            File.AppendAllText(fileName, $"{ms},{fps}" + Environment.NewLine);
        });
    }

    // Update is called once per frame
    void Update () {

    }
}