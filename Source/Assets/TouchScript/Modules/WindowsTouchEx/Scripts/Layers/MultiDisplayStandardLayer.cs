using TouchScript.Hit;
using TouchScript.InputSources;
using TouchScript.InputSources.InputHandlers;
using TouchScript.Pointers;

namespace TouchScript.Layers
{
    /// <summary>
    /// A display specific touch layer, to be used with <see cref="MultiDisplayInput"/>. It inherits from StandardLayer,
    /// but checks if the <see cref="IInputSource"/> is the input source for this 
    /// </summary>
    public class MultiDisplayStandardLayer : StandardLayer
    {
        public override HitResult Hit(IPointer pointer, out HitData hit)
        {
            var inputSource = pointer.InputSource;
            if (inputSource is MultiDisplayInputHandler multiDisplayPointerHandler)
            {
                
            }
            
            return base.Hit(pointer, out hit);
        }
    }
}