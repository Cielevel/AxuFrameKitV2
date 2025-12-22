using TMPro;
using UnityEngine;

namespace AxuFrameKit.Modules.Analysis.Script
{
    public class UIPerformanceMonitor : MonoBehaviour
    {
        [Header("帧率")]
        [SerializeField] private TMP_Text _textFps;
        [SerializeField] private TMP_Text _textFrameTimeMs;
        
        [Header("内存")]
        [SerializeField] private TMP_Text _textTotalReserveMem;
        [SerializeField] private TMP_Text _textAllocatedMem;
        [SerializeField] private TMP_Text _textMonoUseMem;
        [SerializeField] private TMP_Text _textMonoHeapMem;
        
        [Header("GC")]
        [SerializeField] private TMP_Text _textGcCount;
        [SerializeField] private TMP_Text _textGcAllocPerFrame;

        public void UpdateData(PerfData data)
        {
            _textFps.text             = data.fps.ToString("F0") + " fps";
            _textFrameTimeMs.text     = data.frameTimeMs.ToString("F2") + " ms";
            _textTotalReserveMem.text = data.totalReservedMem.ToString("F2") + " MB TRAM";
            _textAllocatedMem.text    = data.totalAllocatedMem.ToString("F2") + " MB RAM";
            // _textMonoUseMem.text      = data.monoUsedMem.ToString("F2") + "MB";
            // _textMonoHeapMem.text     = data.monoHeapMem.ToString("F2") + "MB";
            _textGcCount.text         = data.gcCount + " GC";
            // _textGcAllocPerFrame.text = data.gcAllocPerFrame.ToString("F2") + "B";
        }
    }
}