/*
 * @author Valentin Simonov / http://va.lent.in/
 */

namespace TouchScript.Editor.InputSources
{
    public class InputSourceEditor : UnityEditor.Editor
    {
        protected virtual void OnEnable() {}

        public override void OnInspectorGUI() {}

        protected virtual void drawAdvanced() {}
    }
}