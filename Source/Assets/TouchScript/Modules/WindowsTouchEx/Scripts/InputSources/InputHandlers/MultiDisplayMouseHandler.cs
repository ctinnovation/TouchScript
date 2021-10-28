namespace TouchScript.InputSources.InputHandlers
{
    public class MultiDisplayMouseHandler : MouseHandler, IMultiDisplayInputHandler
    {
        public int TargetDisplay { get; set; }

        public MultiDisplayMouseHandler(PointerDelegate addPointer, PointerDelegate updatePointer,
            PointerDelegate pressPointer, PointerDelegate releasePointer, PointerDelegate removePointer,
            PointerDelegate cancelPointer)
            : base(addPointer, updatePointer, pressPointer, releasePointer, removePointer, cancelPointer)
        {
            
        }
    }
}