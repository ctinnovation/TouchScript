using TouchScript.Editor.EditorUI;
using UnityEditor;
using UnityEngine;

namespace TouchScript.Editor
{
    [CustomEditor(typeof(MultiWindowManager))]
    public class MultiWindowManagerEditor : UnityEditor.Editor
    {
        public static readonly GUIContent TEXT_HELP = new GUIContent("This component holds TouchScript multi window configuration options for a scene.");
        public static readonly GUIContent TEXT_DEFAULTS_HEADER = new GUIContent("Defaults", "Default actions when some of TouchScript components are not present in the scene.");
        public static readonly GUIContent TEXT_INPUT_HANDLERS_HEADER = new GUIContent("Touch", "How to update Windows touch handlers upon activation.");
        
        public static readonly GUIContent TEXT_DEBUG_MODE = new GUIContent("Debug", "Turns on debug mode.");
        public static readonly GUIContent TEXT_ACTIVATE_DISPLAYS = new GUIContent("Activate Displays", "");
        public static readonly GUIContent TEXT_UPDATE_INPUT_HANDLERS = new GUIContent("Update Input Handlers", "When set to true, the input handlers for the specific windows will be created when the touch system is ready; after 2 frames.");
        
        private MultiWindowManager instance;
        private SerializedProperty basicEditor;
        private SerializedProperty debugMode;

        private SerializedProperty shouldActivateDisplays;
        private SerializedProperty shouldUpdateInputHandlers;

        private void OnEnable()
        {
            instance = target as MultiWindowManager;

            basicEditor = serializedObject.FindProperty("basicEditor");
            debugMode = serializedObject.FindProperty("debugMode");

            shouldActivateDisplays = serializedObject.FindProperty("shouldActivateDisplays");
            shouldUpdateInputHandlers = serializedObject.FindProperty("shouldUpdateInputHandlers");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            
            GUILayout.Space(5);

            if (basicEditor.boolValue)
            {
                DoDrawDefaults();
                
                if (GUIElements.BasicHelpBox(TEXT_HELP))
                {
                    basicEditor.boolValue = false;
                    Repaint();
                }
            }
            else
            {
                DrawDefaults();
                DrawInputHandlers();
                DrawDebug();
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        private void DoDrawDefaults()
        {
            using (new EditorGUI.DisabledGroupScope(Application.isPlaying))
            {
                EditorGUILayout.PropertyField(shouldActivateDisplays, TEXT_ACTIVATE_DISPLAYS);
            }
        }

        private void DrawDefaults()
        {
            var display = GUIElements.Header(TEXT_DEFAULTS_HEADER, shouldActivateDisplays);
            if (display)
            {
                EditorGUI.indentLevel++;
                DoDrawDefaults();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawInputHandlers()
        {
            var display = GUIElements.Header(TEXT_INPUT_HANDLERS_HEADER, shouldUpdateInputHandlers);
            if (display)
            {
                EditorGUI.indentLevel++;
                using (new EditorGUI.DisabledGroupScope(Application.isPlaying))
                {
                    EditorGUILayout.PropertyField(shouldUpdateInputHandlers, TEXT_UPDATE_INPUT_HANDLERS);
                }
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawDebug()
        {
            if (debugMode == null) return;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(debugMode, TEXT_DEBUG_MODE);
            if (EditorGUI.EndChangeCheck()) instance.DebugMode = debugMode.boolValue;
        }
    }
}