using GitLinq;
using GitLinq.AST;

namespace Tests.QueryParserTests;

[TestClass]
public class NumberLiteralTests
{
    [TestMethod]
    public void ParseSingleDigitNumber()
    {
        var root = QueryParser.ParseExpression("5");
        
        Assert.IsInstanceOfType(root, typeof(NumberLiteralNode));
        var node = (NumberLiteralNode)root;
        Assert.AreEqual(5, node.Value);
    }

    [TestMethod]
    public void ParseMultiDigitNumber()
    {
        var root = QueryParser.ParseExpression("123");
        
        Assert.IsInstanceOfType(root, typeof(NumberLiteralNode));
        var node = (NumberLiteralNode)root;
        Assert.AreEqual(123, node.Value);
    }

    [TestMethod]
    public void ParseZero()
    {
        var root = QueryParser.ParseExpression("0");
        
        Assert.IsInstanceOfType(root, typeof(NumberLiteralNode));
        var node = (NumberLiteralNode)root;
        Assert.AreEqual(0, node.Value);
    }

    [TestMethod]
    public void ParseNumberInMethodCall()
    {
        var root = QueryParser.ParseExpression("Commits.Take(10)");
        
        Assert.IsInstanceOfType(root, typeof(MethodCallNode));
        var node = (MethodCallNode)root;
        
        Assert.AreEqual("Take", node.Method);
        Assert.AreEqual(1, node.Arguments.Count());
        
        var arg = node.Arguments.First() as NumberLiteralNode;
        Assert.IsNotNull(arg);
        Assert.AreEqual(10, arg.Value);
    }

    [TestMethod]
    public void ParseNumberWithWhitespace()
    {
        var root = QueryParser.ParseExpression("  42  ");
        
        Assert.IsInstanceOfType(root, typeof(NumberLiteralNode));
        var node = (NumberLiteralNode)root;
        Assert.AreEqual(42, node.Value);
    }
}
