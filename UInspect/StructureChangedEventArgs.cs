using System;
using UIAutomationClient;

namespace UInspect
{
    public class StructureChangedEventArgs : EventArgs
    {
        public StructureChangedEventArgs(AutomationElement element, StructureChangeType changeType, Array runtimeId)
        {
            Element = element;
            ChangeType = changeType;
            RuntimeId = runtimeId;
        }

        public AutomationElement Element { get; }
        public StructureChangeType ChangeType { get; }
        public Array RuntimeId { get; }
        public string Id => AutomationUtilities.GetId(RuntimeId);

        public override string ToString() => Id + " " + ChangeType + " " + Element;
    }
}
