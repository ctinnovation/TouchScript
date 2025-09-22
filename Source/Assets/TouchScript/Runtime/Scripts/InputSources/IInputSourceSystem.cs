namespace TouchScript.InputSources
{
    /// <summary>
    /// An object which represents a system backing one or more input sources.
    /// </summary>
    public interface IInputSourceSystem
    {
        /// <summary>
        /// This method is called by <see cref="ITouchManager"/> to synchronously process the system. It is called right
        /// before the update of the input sources.
        /// </summary>
        void PrepareInputs();
    }
}
