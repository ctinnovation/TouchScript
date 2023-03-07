#if TOUCHSCRIPT_DEBUG

using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using TouchScript.Debugging.Filters;
using TouchScript.Debugging.Loggers;
using TouchScript.InputSources.InputHandlers;
using TouchScript.Pointers;
using TouchScript.Utils;
using UnityEngine;

#endif

namespace TouchScript.Debugging
{
    [DefaultExecutionOrder(-50)]
    public class DebugLogger : MonoBehaviour, IPointerLogger
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private int numLines = 10;
        
        /// <inheritdoc />
        public int PointerCount
        {
            get { throw new NotImplementedException("DebugLogger doesn't support reading data."); }
        }

        private List<string> lines = new List<string>();
        private StringBuilder linesBuilder = new StringBuilder();

#if TOUCHSCRIPT_DEBUG
        private void Awake()
        {
            TouchScriptDebugger.Instance.PointerLogger = this;
        }

        private void LateUpdate()
        {
            linesBuilder.Clear();
            
            foreach (var line in lines)
            {
                linesBuilder.AppendLine(line);
            }

            text.text = linesBuilder.ToString();
        }
#endif
        
        public void Log(Pointer pointer, PointerEvent evt)
        {
#if TOUCHSCRIPT_DEBUG
            var path = TransformUtils.GetHierarchyPath(pointer.GetPressData().Target);

            var inputSource = pointer.InputSource;
            var targetDisplay = "1";
            
            if (inputSource is IMultiWindowInputHandler multiWindowInputHandler)
            {
                targetDisplay = multiWindowInputHandler.TargetDisplay.ToString();
            }

            var line =
                $"(Display {targetDisplay}): {pointer.Type}({pointer.Id}):({pointer.Position.x},{pointer.Flags}) {pointer.Position.y}) | ({pointer.PreviousPosition.x}, {pointer.PreviousPosition.y}) - ({path ?? ""})";

            lines.Add(line);
            if (lines.Count > numLines)
            {
                lines.RemoveAt(0);
            }
#endif
        }

        public List<PointerData> GetFilteredPointerData(IPointerDataFilter filter = null)
        {
            throw new NotImplementedException("DebugLogger doesn't support reading data.");
        }

        public List<PointerLog> GetFilteredLogsForPointer(int id, IPointerLogFilter filter = null)
        {
            throw new NotImplementedException("DebugLogger doesn't support reading data.");
        }

        public void Dispose() { }
    }
}