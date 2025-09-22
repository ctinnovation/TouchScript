using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-10)]
public class DebugLogView : MonoBehaviour
    {
        [SerializeField] private int maxLines = 100;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Scrollbar scrollbar;
        [SerializeField] private TextMeshProUGUI content;

        private readonly StringBuilder linesBuilder = new();
        private Queue<string> lines = new();

        private Thread unityMainThread;
        private volatile bool updateText;
        private object linesLock = new();

        private int setScrollPositionAtBottomIn;

        private void Awake()
        {
            unityMainThread = Thread.CurrentThread;

            Application.logMessageReceived += OnLogMessageReceived;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private void LateUpdate()
        {
            if (setScrollPositionAtBottomIn > 0)
            {
                setScrollPositionAtBottomIn--;
                if (setScrollPositionAtBottomIn < 1)
                {
                    scrollRect.verticalNormalizedPosition = 0;
                    scrollbar.value = 0;
                }
            }

            TryUpdateText();
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        }

        public void Write(string message, LogType logType, string stackTrace = null)
        {
            switch (logType)
            {
                case LogType.Log:
                    Write(message);
                    break;
                case LogType.Warning:
                    Write($"<color=#ffd443ff>{message}</color>");
                    break;
                default:
                    Write($"<color=#ff3434ff>{message}</color>");
                    break;
            }
        }

        public void Write(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            lock (linesLock)
            {
                if (lines.Count == maxLines)
                {
                    var line = lines.Dequeue();
                    linesBuilder.Remove(0, line.Length);
                }

                lines.Enqueue(message);
                linesBuilder.AppendLine(message);
            }

            // This can be called from multiple threads
            updateText = true;
            if (!Thread.CurrentThread.Equals(unityMainThread))
            {
                return;
            }

            TryUpdateText();
        }

        public void Clear()
        {
            lock (linesLock)
            {
                lines.Clear();
                linesBuilder.Clear();
            }

            DoUpdateText();
        }

        private void TryUpdateText()
        {
            if (!updateText)
            {
                return;
            }

            DoUpdateText();
        }

        private void DoUpdateText()
        {
            var go = gameObject;
            if (!go || !go.activeInHierarchy)
            {
                return;
            }

            updateText = false;

            string textToDisplay;

            lock (linesLock)
            {
                textToDisplay = linesBuilder.ToString();
            }

            // Ensure we don't exceed Unity's limit
            if (textToDisplay.Length > 16384)
            {
                content.text = textToDisplay.Substring(textToDisplay.Length - 16384);
            }
            else
            {
                content.text = textToDisplay;
            }

            if (setScrollPositionAtBottomIn == 0)
            {
                setScrollPositionAtBottomIn = 2;
            }
        }

        private void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            Write(message, type, stackTrace);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            if (args?.ExceptionObject != null &&
                args.ExceptionObject.GetType() == typeof(Exception))
            {
                var e = (Exception)args.ExceptionObject;
                Write($"Unhandled exception at {e.Source} - {e.Message}", LogType.Exception, e.StackTrace);
            }
            else
            {
                Write("Unknown unhandled exception", LogType.Exception);
            }
        }
    }
