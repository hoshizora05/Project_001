using System;
namespace DialogSystem
{
    /// <summary>
    /// Data for a simple "OK" dialog (single button).
    /// </summary>
    public class OkDialogData : BaseDialogData
    {
        public Action OnOk { get; private set; }

        public OkDialogData(string title, string message, Action onOk = null)
            : base(title, message)
        {
            OnOk = onOk;
        }
    }
}