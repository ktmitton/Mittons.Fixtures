using System;
using System.Collections.Generic;
using System.Linq;
using Mittons.Fixtures.Core;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit.Core;

public class DependencyGraphTests
{
    private record Node(string Name, string[] Depndencies);

    [Fact]
    public void Ctor_WhenCalledWithNoDependencies_ExpectAllNodesToBeCreated()
    {
        // Arrange
        var nodes = new List<Node>
        {
            new Node("Node1", new string[0]),
            new Node("Node2", new string[0]),
            new Node("Node3", new string[0]),
            new Node("Node4", new string[0])
        };

        // Act
        var dependencyGraph = new DependencyGraph<Node>(nodes, x => x.Name, x => x.Depndencies);

        // Assert
        Assert.Equal(nodes.Count(), dependencyGraph.Nodes.Count());

        foreach (var node in nodes)
        {
            Assert.Equal(node, dependencyGraph.Nodes[node.Name].Value);
            Assert.Equal(node.Name, dependencyGraph.Nodes[node.Name].Name);
            Assert.Empty(dependencyGraph.Nodes[node.Name].Dependencies);
        }
    }

    [Fact]
    public void Ctor_WhenCalledWithEarlyNodesThatDependOnLaterNodes_ExpectAllNodesToBeCreated()
    {
        // Arrange
        var nodes = new List<Node>
        {
            new Node("Node1", new string[] { "Node2" }),
            new Node("Node2", new string[] { "Node3", "Node4" }),
            new Node("Node3", new string[] { "Node4"}),
            new Node("Node4", new string[0])
        };

        // Act
        var dependencyGraph = new DependencyGraph<Node>(nodes, x => x.Name, x => x.Depndencies);

        // Assert
        Assert.Equal(nodes.Count(), dependencyGraph.Nodes.Count());

        foreach (var node in nodes)
        {
            Assert.Equal(node, dependencyGraph.Nodes[node.Name].Value);
            Assert.Equal(node.Name, dependencyGraph.Nodes[node.Name].Name);
            Assert.Equal(node.Depndencies, dependencyGraph.Nodes[node.Name].Dependencies.Select(x => x.Name));
        }
    }

    [Fact]
    public void Ctor_WhenCalledWithLaterNodesThatDependOnEarlyNodes_ExpectAllNodesToBeCreated()
    {
        // Arrange
        var nodes = new List<Node>
        {
            new Node("Node1", new string[0]),
            new Node("Node2", new string[] { "Node1" }),
            new Node("Node3", new string[] { "Node2", "Node1" }),
            new Node("Node4", new string[] { "Node3"})
        };

        // Act
        var dependencyGraph = new DependencyGraph<Node>(nodes, x => x.Name, x => x.Depndencies);

        // Assert
        Assert.Equal(nodes.Count(), dependencyGraph.Nodes.Count());

        foreach (var node in nodes)
        {
            Assert.Equal(node, dependencyGraph.Nodes[node.Name].Value);
            Assert.Equal(node.Name, dependencyGraph.Nodes[node.Name].Name);
            Assert.Equal(node.Depndencies, dependencyGraph.Nodes[node.Name].Dependencies.Select(x => x.Name));
        }
    }

    [Fact]
    public void Ctor_WhenANodeIsAddedTwice_ExpectExceptionToBeThrown()
    {
        // Arrange
        var nodes = new List<Node>
        {
            new Node("Node1", new string[0]),
            new Node("Node2", new string[] { "Node1" }),
            new Node("Node3", new string[] { "Node1" }),
            new Node("Node3", new string[] { "Node2" }),
            new Node("Node4", new string[] { "Node3"})
        };

        // Act
        // Assert
        Assert.Throws<ArgumentException>(() => new DependencyGraph<Node>(nodes, x => x.Name, x => x.Depndencies));
    }

    [Fact]
    public void CreateBuildOrder_WhenADirectCircularDependencyIsDetected_ExpectExceptionToBeThrown()
    {
        // Arrange
        var nodes = new List<Node>
        {
            new Node("Node1", new string[] { "Node4" }),
            new Node("Node2", new string[] { "Node3" }),
            new Node("Node3", new string[] { "Node4" }),
            new Node("Node4", new string[] { "Node1" })
        };

        var graph = new DependencyGraph<Node>(nodes, x => x.Name, x => x.Depndencies);

        // Act
        // Assert
        Assert.Throws<InvalidOperationException>(() => graph.CreateBuildOrder());
    }

    [Fact]
    public void Ctor_WhenAnIndirectCircularDependencyIsDetected_ExpectExceptionToBeThrown()
    {
        // Arrange
        var nodes = new List<Node>
        {
            new Node("Node1", new string[] { "Node2" }),
            new Node("Node2", new string[] { "Node3" }),
            new Node("Node3", new string[] { "Node4" }),
            new Node("Node4", new string[] { "Node2" })
        };

        var graph = new DependencyGraph<Node>(nodes, x => x.Name, x => x.Depndencies);

        // Act
        // Assert
        Assert.Throws<InvalidOperationException>(() => graph.CreateBuildOrder());
    }

    [Fact]
    public void CreateBuildOrder_WhenCalled_Expe()
    {
        // Arrange
        var node1 = new Node("Node1", new string[] { "Node2" });
        var node2 = new Node("Node2", new string[] { "Node3", "Node4" });
        var node3 = new Node("Node3", new string[] { "Node4" });
        var node4 = new Node("Node4", new string[0]);
        var node5 = new Node("Node5", new string[0]);
        var node6 = new Node("Node6", new string[] { "Node3", "Node5" });

        var nodes = new List<Node>
        {
            node1,
            node2,
            node5,
            node3,
            node4,
            node6
        };

        var expectedBuildOrder = new Node[]
        {
            node4,
            node5,
            node3,
            node2,
            node6,
            node1
        };

        var dependencyGraph = new DependencyGraph<Node>(nodes, x => x.Name, x => x.Depndencies);

        // Act
        var actualBuildOrder = dependencyGraph.CreateBuildOrder();

        // Assert
        Assert.Equal(expectedBuildOrder, actualBuildOrder);
    }
}
