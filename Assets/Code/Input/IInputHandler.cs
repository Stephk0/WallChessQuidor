namespace WallChess.Input
{
    /// <summary>
    /// Interface for components that handle specific input modes.
    /// </summary>
    public interface IInputHandler
    {
        /// <summary>Check if this handler can process the given input.</summary>
        bool CanHandle(InputData input);

        /// <summary>Called when input begins (touch down / mouse down).</summary>
        void OnInputBegin(InputData input);

        /// <summary>Called when input moves (drag).</summary>
        void OnInputMove(InputData input);

        /// <summary>Called when input ends (touch up / mouse up).</summary>
        void OnInputEnd(InputData input);

        /// <summary>Called when input is cancelled.</summary>
        void OnCancel();
    }
}
