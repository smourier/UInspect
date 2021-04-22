using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using UInspect.Utilities;

namespace UInspect
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
            Icon = Program.AppIcon;

            treeViewElements.BeforeExpand += OnTreeViewVisualsBeforeExpand;
            treeViewElements.AfterCollapse += OnTreeViewVisualsAfterCollapse;
            treeViewElements.AfterSelect += OnTreeViewVisualsAfterSelect;
            treeViewElements.NodeMouseClick += (s, e) => treeViewElements.SelectedNode = e.Node; // right click selects
            Extensions.Log("Start");
            Reload();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                AutomationUtilities.RemoveAllEventHandlers();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        public void Reload()
        {
            treeViewElements.Nodes.Clear();
            AddElement(AutomationElement.Root, treeViewElements.Nodes);
        }

        private void SelectElement()
        {
            var element = treeViewElements.GetSelectedTag<AutomationElement>();
            propertyGridElement.SelectedObject = element;
        }

        private void OnTreeViewVisualsBeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            var element = (AutomationElement)e.Node.Tag;
            if (IsLazy(e.Node))
            {
                e.Node.Nodes.Clear();
                foreach (var child in element.FindAll())
                {
                    AddElement(child, e.Node.Nodes);
                }
            }
        }

        private TreeNode AddElement(AutomationElement element, TreeNodeCollection nodes)
        {
            var id = element.Id;

            var node = nodes.Find(id, false).FirstOrDefault();
            if (node != null)
                return node;

            var name = element.LocalizedControlType;
            if (!string.IsNullOrWhiteSpace(element.Name))
            {
                name += " '" + element.Name + "'";
            }

            node = new TreeNode(name);
            nodes.Add(node);
            node.Name = id;
            node.Tag = element;
            Lazyfy(node);

            element.StructureChanged += OnElementStructureChanged;

            if (element.IsRoot)
            {
                node.Expand();
            }
            return node;
        }

        private void OnElementStructureChanged(object sender, StructureChangedEventArgs e)
        {
            Extensions.Log(sender + " => " + e + " h:" + e.Element.WindowHandle);
            this.BeginInvoke(() =>
            {
                var element = (AutomationElement)sender;
                var node = treeViewElements.Nodes.Find(element.Id, true).FirstOrDefault();
                if (node != null && !IsLazy(node))
                {
                    switch (e.ChangeType)
                    {
                        case UIAutomationClient.StructureChangeType.StructureChangeType_ChildRemoved:
                            if (e.Element.Name?.IndexOf("notepad", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                            }
                            break;
                    }
                    var children = element.Children.ToList();
                    var nodes = node.Nodes.OfType<TreeNode>().ToList();
                    foreach (var child in node.Nodes.OfType<TreeNode>())
                    {
                        if (!(child.Tag is AutomationElement childElement))
                            continue;

                        if (children.Contains(childElement))
                        {
                            Unsubscribe(child, true);
                            nodes.Remove(child);
                            children.Remove(childElement);
                        }
                    }

                    foreach (var remove in nodes)
                    {
                        Unsubscribe(remove, true);
                        node.Nodes.Remove(remove);
                    }

                    foreach (var childElement in children)
                    {
                        AddElement(childElement, node.Nodes);
                    }
                }
            });
        }

        private void OnTreeViewVisualsAfterCollapse(object sender, TreeViewEventArgs e)
        {
            // this allow a refresh on re-expand
            Lazyfy(e.Node);
            SelectElement();
        }

        private void OnTreeViewVisualsAfterSelect(object sender, TreeViewEventArgs e) => SelectElement();
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e) => Close();

        private static bool IsLazy(TreeNode node) => node.Nodes.Count == 1 && string.IsNullOrEmpty(node.Nodes[0].Text);
        private void Lazyfy(TreeNode node)
        {
            Unsubscribe(node, false);
            node.Nodes.Clear();

            var element = (AutomationElement)node.Tag;
            if (element.HasChild)
            {
                node.Nodes.Add(string.Empty);
            }
        }

        private void Unsubscribe(TreeNode node, bool me)
        {
            if (me && node.Tag is AutomationElement element)
            {
                element.StructureChanged -= OnElementStructureChanged;
            }

            foreach (var child in node.Nodes.OfType<TreeNode>())
            {
                Unsubscribe(child, true);
            }
        }
    }
}
