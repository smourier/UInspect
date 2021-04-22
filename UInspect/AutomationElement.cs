using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using UIAutomationClient;
using UInspect.Utilities;

namespace UInspect
{
    public class AutomationElement : IEquatable<AutomationElement>
    {
        public const string CategoryAria = "Aria";
        public static AutomationElement Root = AutomationElement.From(AutomationUtilities.Root);

        private readonly Lazy<Process> _process;
        private readonly Lazy<string> _id;
        private readonly ConcurrentDictionary<EventHandler<StructureChangedEventArgs>, AutomationStructureChangedEventHandler> _structureChangedHandlers = new ConcurrentDictionary<EventHandler<StructureChangedEventArgs>, AutomationStructureChangedEventHandler>();

        public event EventHandler<StructureChangedEventArgs> StructureChanged
        {
            add
            {
                // we use subtree otherwise we don't get all events (like child removed)
                _ = AutomationUtilities.RunAutomationTaskAsync(() =>
                {
                    var state = Thread.CurrentThread.GetApartmentState();
                    var handler = new AutomationStructureChangedEventHandler(this, value);
                    AutomationUtilities.Automation.AddStructureChangedEventHandler(Element, TreeScope.TreeScope_Subtree, null, handler);
                    _structureChangedHandlers[value] = handler;
                });
            }
            remove
            {
                if (_structureChangedHandlers.TryRemove(value, out var handler))
                {
                    AutomationUtilities.RunAutomationTask(() => AutomationUtilities.Automation.RemoveStructureChangedEventHandler(Element, handler));
                }
            }
        }

        private class AutomationStructureChangedEventHandler : IUIAutomationStructureChangedEventHandler
        {
            private readonly EventHandler<StructureChangedEventArgs> _handler;
            private readonly AutomationElement _element;

            public AutomationStructureChangedEventHandler(AutomationElement element, EventHandler<StructureChangedEventArgs> handler)
            {
                _element = element;
                _handler = handler;
            }

            public void HandleStructureChangedEvent(IUIAutomationElement sender, StructureChangeType changeType, Array runtimeId)
            {
                var element = From(sender);
                if (element == null)
                    return;

                if (element.Process?.ProcessName == "WpfTraceSpy")
                    return;

                Extensions.Log(changeType + " => " + element + " h:" + element.WindowHandle);
                _handler(_element, new StructureChangedEventArgs(element, changeType, runtimeId));
            }
        }

        public static AutomationElement From(IUIAutomationElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var id = element.GetId();
            if (id == null)
                return null;

            return new AutomationElement(element, id);
        }

        private AutomationElement(IUIAutomationElement element, string id)
        {
            Id = id;
            Element = element;
            Element2 = Element as IUIAutomationElement2;
            Element3 = Element as IUIAutomationElement3;
            Element4 = Element as IUIAutomationElement4;
            Element5 = Element as IUIAutomationElement5;
            Element6 = Element as IUIAutomationElement6;
            Element7 = Element as IUIAutomationElement7;
            Element8 = Element as IUIAutomationElement8;
            Element9 = Element as IUIAutomationElement9;

            _process = new Lazy<Process>(() =>
            {
                try
                {
                    return Process.GetProcessById(ProcessId);
                }
                catch
                {
                    return null;
                }
            });
        }

        [Browsable(false)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IUIAutomationElement Element { get; }

        [Browsable(false)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IUIAutomationElement2 Element2 { get; }

        [Browsable(false)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IUIAutomationElement3 Element3 { get; }

        [Browsable(false)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IUIAutomationElement4 Element4 { get; }

        [Browsable(false)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IUIAutomationElement5 Element5 { get; }

        [Browsable(false)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IUIAutomationElement6 Element6 { get; }

        [Browsable(false)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IUIAutomationElement7 Element7 { get; }

        [Browsable(false)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IUIAutomationElement8 Element8 { get; }

        [Browsable(false)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IUIAutomationElement9 Element9 { get; }

        public string Id { get; }
        public string Name => Element.CurrentName;
        public string ClassName => Element.CurrentClassName;
        public string LocalizedControlType => Element.CurrentLocalizedControlType;
        public string AutomationId => Element.CurrentAutomationId;
        public string FrameworkId => Element.CurrentFrameworkId;
        public int ProcessId => Element.CurrentProcessId;
        public IntPtr WindowHandle => Element.CurrentNativeWindowHandle;

        [Category(CategoryAria)]
        public string AriaRole => Element.CurrentAriaRole;

        [Category(CategoryAria)]
        public string AriaProperties => Element.CurrentAriaProperties;

        [Browsable(false)]
        public Process Process => _process.Value;
        public string ProcessName
        {
            get
            {
                try
                {
                    return Process?.MainModule.FileName;
                }
                catch
                {
                    return Process.ProcessName;
                }
            }
        }

        [Browsable(false)]
        public bool IsRoot => Equals(Root);

        public bool? IsDialog
        {
            get
            {
                if (Element9 == null)
                    return null;

                try
                {
                    return Element9.CurrentIsDialog != 0;
                }
                catch
                {
                    return null;
                }
            }
        }

        [Browsable(false)]
        public bool HasChild => AutomationUtilities.RunAutomationTask(() => Element.FindFirst(TreeScope.TreeScope_Children, AutomationUtilities.True)) != null;

        [Browsable(false)]
        public IReadOnlyList<AutomationElement> Children => FindAll();

        public override string ToString()
        {
            var s = LocalizedControlType + ":" + Id;
            var name = Name;
            if (!string.IsNullOrEmpty(name))
            {
                s += ": " + name;
            }
            return s;
        }

        public IReadOnlyList<AutomationElement> FindAll(TreeScope scope = TreeScope.TreeScope_Children) => AutomationUtilities.RunAutomationTask(() =>
         {
             var all = Element.FindAll(scope, AutomationUtilities.True);
             var list = new List<AutomationElement>();
             for (var i = 0; i < all.Length; i++)
             {
                 var element = From(all.GetElement(i));
                 if (element != null)
                 {
                     list.Add(element);
                 }
             }
             return list.AsReadOnly();
         });

        public override int GetHashCode() => Id.GetHashCode();
        public override bool Equals(object obj) => Equals(obj as AutomationElement);
        public bool Equals(AutomationElement other) => other?.Id == Id;
    }
}
