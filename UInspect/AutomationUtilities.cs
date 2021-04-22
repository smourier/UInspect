using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UIAutomationClient;
using UInspect.Utilities;

namespace UInspect
{
    public static class AutomationUtilities
    {
        // https://docs.microsoft.com/en-us/windows/win32/winauto/uiauto-threading
        private static readonly SingleThreadTaskScheduler _scheduler = new SingleThreadTaskScheduler(t =>
        {
            t.SetApartmentState(ApartmentState.MTA);
            return true;
        });

        private static readonly Lazy<CUIAutomation> _automation = new Lazy<CUIAutomation>(() => RunAutomationTask(() => new CUIAutomation()));
        private static readonly Lazy<IUIAutomationCondition> _true = new Lazy<IUIAutomationCondition>(() => RunAutomationTask(() => Automation.CreateTrueCondition()));
        private static readonly Lazy<IUIAutomationCondition> _false = new Lazy<IUIAutomationCondition>(() => RunAutomationTask(() => Automation.CreateFalseCondition()));
        private static readonly Lazy<IUIAutomationElement> _root = new Lazy<IUIAutomationElement>(() => RunAutomationTask(() => Automation.GetRootElement()));
        public static CUIAutomation Automation
        {
            get
            {
                if (!_scheduler.IsRunningAsThread)
                    throw new AutomationException("AUT0016: Cannot access to Automation from this thread.");

                return _automation.Value;
            }
        }

        public static IUIAutomationCondition True => _true.Value;
        public static IUIAutomationCondition False => _false.Value;
        public static IUIAutomationElement Root => _root.Value;

        public static void RemoveAllEventHandlers()
        {
            if (!_automation.IsValueCreated)
                return;

            RunAutomationTask(() => _automation.Value.RemoveAllEventHandlers());
        }

        public static void RunAutomationTask(Action action, bool startNew = false) => _scheduler.RunAutomationTask(action, startNew);
        public static Task RunAutomationTaskAsync(Action action, bool startNew = false) => _scheduler.RunAutomationTaskAsync(action, startNew);
        public static T RunAutomationTask<T>(Func<T> action, bool startNew = false) => _scheduler.RunAutomationTask(action, startNew);
        public static Task<T> RunAutomationTaskAsync<T>(Func<T> action, bool startNew = false) => _scheduler.RunAutomationTaskAsync(action, startNew);

        public static string GetId(Array runtimeId)
        {
            if (runtimeId == null || runtimeId.Length == 0)
                return null;

            if (runtimeId is int[] ints)
                return string.Join(".", ints.Select(i => i.ToString("X4")));

            return string.Join(".", runtimeId.OfType<object>());
        }

        public static string GetId(this IUIAutomationElement element)
        {
            if (element == null)
                return null;

            return GetId(RunAutomationTask(() =>
            {
                try
                {
                    return element.GetRuntimeId();
                }
                catch
                {
                    return null;
                }
            }));
        }
    }
}
