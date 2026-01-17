using GitLinq;
using GitLinq.AST;

namespace Tests.QueryParserTests;

[TestClass]
public class StringLiteralTests
{
    [TestMethod]
    public void ParseSimpleString()
    {
        var root = QueryParser.ParseExpression("\"hello\"");
        
        Assert.IsInstanceOfType(root, typeof(StringLiteralNode));
        var node = (StringLiteralNode)root;
        Assert.AreEqual("hello", node.Value);
    }

    [TestMethod]
    public void ParseEmptyString()
    {
        var root = QueryParser.ParseExpression("\"\"");
        
        Assert.IsInstanceOfType(root, typeof(StringLiteralNode));
        var node = (StringLiteralNode)root;
        Assert.AreEqual("", node.Value);
    }

    [TestMethod]
    public void ParseStringWithSpaces()
    {
        var root = QueryParser.ParseExpression("\"hello world\"");
        
        Assert.IsInstanceOfType(root, typeof(StringLiteralNode));
        var node = (StringLiteralNode)root;
        Assert.AreEqual("hello world", node.Value);
    }

    [TestMethod]
    public void ParseStringWithNumbers()
    {
        var root = QueryParser.ParseExpression("\"test123\"");
        
        Assert.IsInstanceOfType(root, typeof(StringLiteralNode));
        var node = (StringLiteralNode)root;
        Assert.AreEqual("test123", node.Value);
    }

    [TestMethod]
    public void ParseStringWithSpecialChars()
    {
        var root = QueryParser.ParseExpression("\"fix: bug #123\"");
        
        Assert.IsInstanceOfType(root, typeof(StringLiteralNode));
        var node = (StringLiteralNode)root;
        Assert.AreEqual("fix: bug #123", node.Value);
    }
}
