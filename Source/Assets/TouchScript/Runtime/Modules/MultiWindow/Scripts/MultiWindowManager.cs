using TouchScript.Core;
using TouchScript.Utils.Attributes;
using UnityEngine;

namespace TouchScript
{
    [AddComponentMenu("TouchScript/Multi Window Manager")]
    public class MultiWindowManager : DebuggableMonoBehaviour
    {
        /// <summary>
        /// Gets the instance of <see cref="IMultiWindowManager"/> implementation used in the application.
        /// </summary>
        /// <value>An instance of <see cref="IMultiWindowManager"/> which is in charge of global pointer input control in the application.</value>
        public static IMultiWindowManager Instance
        {
            get { return MultiWindowManagerInstance.Instance; }
        }

#if TOUCHSCRIPT_DEBUG

        /// <inheritdoc />
        public override bool DebugMode
        {
            get { return base.DebugMode; }
            set
            {
                base.DebugMode = value;
                if (Application.isPlaying) (Instance as MultiWindowManagerInstance).DebugMode = value;
            }
        }
#endif
        
        public bool ShouldActivateDisplays
        {
            get => shouldActivateDisplays;
            set => shouldActivateDisplays = value;
        }

        public bool ShouldUpdateInputHandlers
        {
            get => shouldUpdateInputHandlers;
            set => shouldUpdateInputHandlers = value;
        }
        
        [ToggleLeft, SerializeField] private bool shouldActivateDisplays = true;
        [ToggleLeft, SerializeField] private bool shouldUpdateInputHandlers = true;
        
#pragma warning disable CS0414

        [SerializeField, HideInInspector] private bool basicEditor = true;

#pragma warning restore CS0414
        
        private void Awake()
        {
            if (Instance == null) return;

#if TOUCHSCRIPT_DEBUG
            if (DebugMode) (Instance as MultiWindowManagerInstance).DebugMode = true;
#endif

            Instance.ShouldActivateDisplays = shouldActivateDisplays;
            Instance.ShouldUpdateInputHandlersOnStart = shouldUpdateInputHandlers;
        }
        
        [ContextMenu("Basic Editor")]
        private void SwitchToBasicEditor()
        {
            basicEditor = true;
        }
    }
}