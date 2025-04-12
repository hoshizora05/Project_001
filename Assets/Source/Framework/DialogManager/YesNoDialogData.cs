using System;
namespace DialogSystem
{
    /// <summary>
    /// Data for a "Yes/No" choice dialog.
    /// </summary>
    public class YesNoDialogData : BaseDialogData
    {
        public Action OnYes { get; private set; }
        public Action OnNo { get; private set; }

        public YesNoDialogData(string title, string message, Action onYes, Action onNo)
            : base(title, message)
        {
            OnYes = onYes;
            OnNo = onNo;
        }
    }
}