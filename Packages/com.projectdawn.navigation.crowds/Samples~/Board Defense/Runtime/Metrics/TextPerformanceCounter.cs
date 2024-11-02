using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDawn.Navigation.Sample.Mass
{
    [RequireComponent(typeof(Text))]
    public class TextPerformanceCounter : MonoBehaviour
    {
        ProfilerRecorder m_SimulationSystemGroupRecorder;
        ProfilerRecorder m_PresentationSystemGroupRecorder;
        Text m_Text;

        void Awake()
        {
            m_SimulationSystemGroupRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "SimulationSystemGroup", 60);
            m_PresentationSystemGroupRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "PresentationSystemGroup", 60);
            m_Text = GetComponent<Text>();
        }

        void OnDestroy()
        {
            m_SimulationSystemGroupRecorder.Dispose();
            m_PresentationSystemGroupRecorder.Dispose();
        }

        void Update()
        {
            float simulationMs = GetRecorderAverageMs(m_SimulationSystemGroupRecorder);
            float presentationMs = GetRecorderAverageMs(m_PresentationSystemGroupRecorder);
            float ms = simulationMs + presentationMs;
            m_Text.text = $"{ms:0.00}ms";
        }

        static float GetRecorderAverageMs(ProfilerRecorder recorder)
        {
            if (recorder.Count == 0)
                return 0;

            long accumulation = 0;
            for (int i = 0; i < recorder.Count; ++i)
            {
                accumulation += recorder.GetSample(i).Value;
            }
            return (accumulation / recorder.Count) * (1e-6f);
        }
    }
}
