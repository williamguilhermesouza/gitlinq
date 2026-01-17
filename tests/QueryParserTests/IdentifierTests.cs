using GitLinq;
using GitLinq.AST;

namespace Tests.QueryParserTests;

[TestClass]
public class IdentifierTests
{
    [TestMethod]
    public void ParseSimpleIdentifier()
    {
        var root = QueryParser.ParseExpression("Commits");
        
        Assert.IsInstanceOfType(root, typeof(IdentifierNode));
        var node = (IdentifierNode)root;
        Assert.AreEqual("Commits", node.Name);
    }

    [TestMethod]
    public void ParseIdentifierWithUnderscore()
    {
        var root = QueryParser.ParseExpression("my_variable");
        
        Assert.IsInstanceOfType(root, typeof(IdentifierNode));
        var node = (IdentifierNode)root;
        Assert.AreEqual("my_variable", node.Name);
    }

    [TestMethod]
    public void ParseIdentifierWithLeadingUnderscore()
    {
        var root = QueryParser.ParseExpression("_private");
        
        Assert.IsInstanceOfType(root, typeof(IdentifierNode));
        var node = (IdentifierNode)root;
        Assert.AreEqual("_private", node.Name);
    }

    [TestMethod]
    public void ParseIdentifierTrimsWhitespace()
    {
        var root = QueryParser.ParseExpression("  Commits  ");
        
        Assert.IsInstanceOfType(root, typeof(IdentifierNode));
        var node = (IdentifierNode)root;
        Assert.AreEqual("Commits", node.Name);
    }
}
