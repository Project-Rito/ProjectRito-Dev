using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Toolbox.Core.ViewModels
{
    public class NodeFolder : NodeBase
    {
        public Dictionary<string, NodeBase> NamedChildren
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

        public Dictionary<string, NodeFolder> FolderChildren
        {
            get
            {
                Dictionary<string, NodeFolder> result = new Dictionary<string, NodeFolder>(Children.Count);

                foreach (var childNode in Children)
                {
                    if (childNode is NodeFolder)
                        result.Add(childNode.Header, (NodeFolder)childNode);
                }

                return result;
            }
        }

        public new void AddChild(NodeBase child)
        {
            if (!NamedChildren.ContainsKey(child.Header))
            {
                Children.Add(child);
                child.Parent = this;
            }
            else
                throw new ArgumentException("A node with the same header has already been added to this folder!");
        }

        public void TryAddChild(NodeBase child)
        {
            if (!NamedChildren.ContainsKey(child.Header))
            {
                Children.Add(child);
                child.Parent = this;
            }
        }

        public void RemoveChild(string key)
        {
            Children.Remove(Children.First(x => x.Header == key));
        }

        public NodeFolder() : base() { }

        public NodeFolder(string name) : base(name) { }
    }
}
