namespace TouchScript.InputSources.InputHandlers
{
    public interface IMultiWindowInputHandler : IInputSource
    {
        int TargetDisplay { get; set; }
    }
}