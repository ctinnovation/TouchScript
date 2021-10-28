using TouchScript.InputSources;
using UnityEngine;

namespace TouchScript.Examples.Multiwindow
{
    public class DisplayActivator : MonoBehaviour
    {
        [SerializeField] private int[] displayIndicesToActivate;
        
        private void Start()
        {
            foreach (var i in displayIndicesToActivate)
            {
                if (i < 0 || i >= Display.displays.Length)
                {
                    continue;
                }
                
                var display = Display.displays[i];
                if (display.active)
                {
                    continue;
                }
                
                display.Activate();
            }

            var inputs = TouchManager.Instance.Inputs;
            foreach (var input in inputs)
            {
                if (input is MultiDisplayInput multiWindowInput)
                {
                    multiWindowInput.RefreshInputHandlers();
                }
            }
        }
    }
}
