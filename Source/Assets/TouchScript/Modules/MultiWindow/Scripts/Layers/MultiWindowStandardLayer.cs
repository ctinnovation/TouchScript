using TouchScript.Hit;
using TouchScript.InputSources;
using TouchScript.InputSources.InputHandlers;
using TouchScript.Pointers;

namespace TouchScript.Layers
{
    /// <summary>
    /// A display specific touch layer, to be used with <see cref="MultiWindowStandardInput"/>. It inherits from StandardLayer,
    /// but checks if the <see cref="IInputSource"/> is the input source for this 
    /// </summary>
    public class MultiWindowStandardLayer : StandardLayer
    {
        public override HitResult Hit(IPointer pointer, out HitData hit)
        {
            var inputSource = pointer.InputSource;
            if (inputSource is IMultiWindowInputHandler multiWindowInputHandler)
            {
                if (multiWindowInputHandler.TargetDisplay != _camera.targetDisplay)
                {
                    hit = default;
                    return HitResult.Miss;
                }
            }
            
            return base.Hit(pointer, out hit);
        }
    }
}