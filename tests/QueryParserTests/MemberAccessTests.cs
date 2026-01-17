using GitLinq;
using GitLinq.AST;

namespace Tests.QueryParserTests;

[TestClass]
public class MemberAccessTests
{
    [TestMethod]
    public void ParseSimpleMemberAccess()
    {
        var root = QueryParser.ParseExpression("Commit.Message");
        
        Assert.IsInstanceOfType(root, typeof(MemberAccessNode));
        var node = (MemberAccessNode)root;
        
        Assert.AreEqual("Message", node.Member);
        Assert.IsInstanceOfType(node.Target, typeof(IdentifierNode));
        Assert.AreEqual("Commit", ((IdentifierNode)node.Target).Name);
    }

    [TestMethod]
    public void ParseChainedMemberAccess()
    {
        var root = QueryParser.ParseExpression("Commit.Author.Name");
        
        Assert.IsInstanceOfType(root, typeof(MemberAccessNode));
        var outerNode = (MemberAccessNode)root;
        
        Assert.AreEqual("Name", outerNode.Member);
        Assert.IsInstanceOfType(outerNode.Target, typeof(MemberAccessNode));
        
        var innerNode = (MemberAccessNode)outerNode.Target;
        Assert.AreEqual("Author", innerNode.Member);
        Assert.IsInstanceOfType(innerNode.Target, typeof(IdentifierNode));
        Assert.AreEqual("Commit", ((IdentifierNode)innerNode.Target).Name);
    }

    [TestMethod]
    public void ParseTripleMemberAccessChain()
    {
        var root = QueryParser.ParseExpression("a.b.c.d");
        
        Assert.IsInstanceOfType(root, typeof(MemberAccessNode));
        var node1 = (MemberAccessNode)root;
        Assert.AreEqual("d", node1.Member);
        
        var node2 = (MemberAccessNode)node1.Target;
        Assert.AreEqual("c", node2.Member);
        
        var node3 = (MemberAccessNode)node2.Target;
        Assert.AreEqual("b", node3.Member);
        
        var node4 = (IdentifierNode)node3.Target;
        Assert.AreEqual("a", node4.Name);
    }
}
