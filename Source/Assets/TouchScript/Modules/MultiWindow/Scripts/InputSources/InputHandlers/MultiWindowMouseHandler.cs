namespace TouchScript.InputSources.InputHandlers
{
    // TODO Correct mouse relative position with in order to know if the position is in the correct screen.
    // To do so we might need to override getMousePosition in MouseHandler (?)
    // See https://forum.unity.com/threads/multi-display-canvases-not-working-in-5-4-2.439429/#post-5988683 for multiple
    // canvases
    public class MultiWindowMouseHandler : MouseHandler, IMultiWindowInputHandler
    {
        public int TargetDisplay { get; set; }

        public MultiWindowMouseHandler(PointerDelegate addPointer, PointerDelegate updatePointer,
            PointerDelegate pressPointer, PointerDelegate releasePointer, PointerDelegate removePointer,
            PointerDelegate cancelPointer)
            : base(addPointer, updatePointer, pressPointer, releasePointer, removePointer, cancelPointer)
        {
            
        }
    }
}