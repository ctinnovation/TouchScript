namespace TouchScript.InputSources.InputHandlers
{
    public interface IMultiDisplayInputHandler : IInputSource
    {
        int TargetDisplay { get; set; }
    }
}