namespace PlayerProgression.Interfaces
{
    public interface IProgressionCommand
    {
        void Execute();
        void Undo();
    }
}