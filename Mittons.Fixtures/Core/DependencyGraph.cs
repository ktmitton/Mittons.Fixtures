using System;
using System.Collections.Generic;
using System.Linq;

namespace Mittons.Fixtures.Core
{
    internal class DependencyGraph<T> where T : class
    {
        public Dictionary<string, Node> Nodes { get; }

        public DependencyGraph(IEnumerable<T> nodeValues, Func<T, string> nameGenerator, Func<T, IEnumerable<string>> dependencyNameGenerator)
        {
            Nodes = new Dictionary<string, Node>();

            foreach (var nodeValue in nodeValues)
            {
                var name = nameGenerator(nodeValue);
                var dependencyNames = dependencyNameGenerator(nodeValue);

                foreach (var dependencyName in dependencyNames)
                {
                    if (!Nodes.ContainsKey(dependencyName))
                    {
                        Nodes[dependencyName] = new Node
                        {
                            Name = dependencyName,
                            Value = default(T),
                            Dependencies = new List<Node>()
                        };
                    }
                }

                CheckForCircularDependency(name, dependencyNames);

                if (!Nodes.ContainsKey(name))
                {
                    Nodes[name] = new Node
                    {
                        Name = name,
                        Value = nodeValue,
                        Dependencies = new List<Node>(dependencyNames.Select(x => Nodes[x]))
                    };
                }
                else if (Nodes[name].Value is null)
                {
                    Nodes[name].Value = nodeValue;
                    Nodes[name].Dependencies = new List<Node>(dependencyNames.Select(x => Nodes[x]));
                }
                else
                {
                    throw new ArgumentException("Duplicate node names are not supported", $"nodeValues{{{name}}}");
                }
            }
        }

        public IEnumerable<T> CreateBuildOrder()
        {
            var nodes = Nodes.Select(x => x.Value).ToList();

            var buildOrder = new Queue<T>();

            while (nodes.Any(x => !x.Dependencies.Any(y => nodes.Any(z => z.Name == y.Name))))
            {
                var newNodes = nodes.Where(x => !x.Dependencies.Any(y => nodes.Any(z => z.Name == y.Name))).OrderBy(x => x.Name).ToArray();

                foreach (var node in newNodes)
                {
                    buildOrder.Enqueue(node.Value);
                    nodes.Remove(node);
                }
            }

            return buildOrder;
        }

        private void CheckForCircularDependency(string nodeName, IEnumerable<string> dependencyNames)
        {
            if (!dependencyNames.Any())
            {
                return;
            }

            var grandchildren = Nodes.Where(x => dependencyNames.Contains(x.Key)).SelectMany(x => x.Value.Dependencies.Select(y => y.Name));//.SelectMany(x => x.Value.Dependencies.Select(y => y.Name));

            var circularDependencies = grandchildren.Where(x => x.Contains(nodeName));

            if (circularDependencies.Any())
            {
                throw new ArgumentException($"Circular dependency detected for nodeValues{{{nodeName}}} and {string.Join(",", circularDependencies.Select(x => $"nodeValues{{{x}}}"))}", $"nodeValues{{{nodeName}}}");
            }

            CheckForCircularDependency(nodeName, grandchildren);
        }

        public class Node
        {
            public string Name;

            public T Value;

            public List<Node> Dependencies { get; set; }
        }
    }
}
