using System;
namespace DialogSystem
{
    /// <summary>
    /// Abstract base for any dialog's data.
    /// You can derive from this to create specialized dialogs.
    /// </summary>
    public abstract class BaseDialogData
    {
        // Common fields (optional). e.g. many dialogs have a title & message:
        public string Title { get; protected set; }
        public string Message { get; protected set; }

        // Constructor
        protected BaseDialogData(string title, string message)
        {
            Title = title;
            Message = message;
        }

        // Optional: you could add abstract or virtual methods if needed.
    }
}