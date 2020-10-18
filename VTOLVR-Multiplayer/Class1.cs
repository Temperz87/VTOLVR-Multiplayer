using System.Collections;
using UnityEngine;
using UnityEngine.Profiling;
public class ProfilerDataSaverComponent : MonoBehaviour
{

    int _count = 0;

    void Start()
    {

        Profiler.logFile = "";
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.H))
        {
            StopAllCoroutines();
            _count = 0;
            StartCoroutine(SaveProfilerData());
        }
    }

    IEnumerator SaveProfilerData()
    {
        // keep calling this method until Play Mode stops
        while (true)
        {

            // generate the file path
            string filepath = "/profilerLog" + _count;

            // set the log file and enable the profiler
            Profiler.logFile = filepath;
            Profiler.enableBinaryLog = true;
            Profiler.enabled = true;

            // count 300 frames
            for (int i = 0; i < 300; ++i)
            {

                yield return new WaitForEndOfFrame();

                // workaround to keep the Profiler working
                if (!Profiler.enabled)
                    Profiler.enabled = true;
            }

            // start again using the next file name
            _count++;
        }
    }
}