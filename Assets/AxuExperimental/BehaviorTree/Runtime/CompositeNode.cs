using System.Collections.Generic;
using UnityEngine;

namespace AxuExperimental.BehaviorTree.Runtime
{
    public abstract class CompositeNode : Node
    {
        [HideInInspector] public List<Node> children = new List<Node>();

        public override Node Clone()
        {
            CompositeNode node = Instantiate(this);
            node.children = new List<Node>();
            foreach (var child in children)
            {
                node.children.Add(child.Clone());
            }
            return node;
        }
    }
}
