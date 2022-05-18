using System;
using System.Collections.Generic;
using System.Text;

namespace Toolbox.Core.ViewModels
{
    public class NodeFolder : NodeBase
    {
        public Dictionary<string, NodeBase> FolderChildren
        {
            get
            {
                Dictionary<string, NodeBase> result = new Dictionary<string, NodeBase>(Children.Count);

                foreach (var childNode in Children)
                {
                    result.Add(childNode.Header, childNode);
                }

                return result;
            }
            set
            {
                Children.Clear();

                foreach (var childNode in value.Values)
                {
                    Children.Add(childNode);
                    childNode.Parent = this;
                }
            }
        }

        public new void AddChild(NodeBase child)
        {
            if (!FolderChildren.ContainsKey(child.Header))
            {
                Children.Add(child);
                child.Parent = this;
            }
            else
                throw new ArgumentException("A node with the same header has already been added to this folder!");
        }

        public void TryAddChild(NodeBase child)
        {
            if (!FolderChildren.ContainsKey(child.Header))
            {
                Children.Add(child);
                child.Parent = this;
            }
        }

        public NodeFolder() : base() { }

        public NodeFolder(string name) : base(name) { }
    }
}
