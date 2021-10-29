namespace TouchScript.InputSources.InputHandlers
{
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