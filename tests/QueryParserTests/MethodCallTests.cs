using GitLinq;
using GitLinq.AST;

namespace Tests.QueryParserTests;

[TestClass]
public class MethodCallTests
{
    [TestMethod]
    public void ParseMethodCallNoArgs()
    {
        var root = QueryParser.ParseExpression("Commits.First()");
        
        Assert.IsInstanceOfType(root, typeof(MethodCallNode));
        var node = (MethodCallNode)root;
        
        Assert.AreEqual("First", node.Method);
        Assert.IsFalse(node.Arguments.Any());
        Assert.IsInstanceOfType(node.Target, typeof(IdentifierNode));
        Assert.AreEqual("Commits", ((IdentifierNode)node.Target).Name);
    }

    [TestMethod]
    public void ParseMethodCallWithStringArg()
    {
        var root = QueryParser.ParseExpression("Message.Contains(\"test\")");
        
        Assert.IsInstanceOfType(root, typeof(MethodCallNode));
        var node = (MethodCallNode)root;
        
        Assert.AreEqual("Contains", node.Method);
        Assert.AreEqual(1, node.Arguments.Count());
        
        var arg = node.Arguments.First() as StringLiteralNode;
        Assert.IsNotNull(arg);
        Assert.AreEqual("test", arg.Value);
    }

    [TestMethod]
    public void ParseMethodCallWithLambdaArg()
    {
        var root = QueryParser.ParseExpression("Commits.Where(c => c.Message)");
        
        Assert.IsInstanceOfType(root, typeof(MethodCallNode));
        var node = (MethodCallNode)root;
        
        Assert.AreEqual("Where", node.Method);
        Assert.AreEqual(1, node.Arguments.Count());
        
        var lambda = node.Arguments.First() as LambdaNode;
        Assert.IsNotNull(lambda);
        Assert.AreEqual("c", lambda.Parameter);
    }

    [TestMethod]
    public void ParseChainedMethodCalls()
    {
        var root = QueryParser.ParseExpression("Commits.Where(c => c.Message).First()");
        
        Assert.IsInstanceOfType(root, typeof(MethodCallNode));
        var outerCall = (MethodCallNode)root;
        
        Assert.AreEqual("First", outerCall.Method);
        Assert.IsFalse(outerCall.Arguments.Any());
        
        Assert.IsInstanceOfType(outerCall.Target, typeof(MethodCallNode));
        var innerCall = (MethodCallNode)outerCall.Target;
        
        Assert.AreEqual("Where", innerCall.Method);
        Assert.IsInstanceOfType(innerCall.Target, typeof(IdentifierNode));
    }

    [TestMethod]
    public void ParseMethodCallOnMemberAccess()
    {
        var root = QueryParser.ParseExpression("c.Message.Contains(\"fix\")");
        
        Assert.IsInstanceOfType(root, typeof(MethodCallNode));
        var node = (MethodCallNode)root;
        
        Assert.AreEqual("Contains", node.Method);
        Assert.IsInstanceOfType(node.Target, typeof(MemberAccessNode));
        
        var memberAccess = (MemberAccessNode)node.Target;
        Assert.AreEqual("Message", memberAccess.Member);
    }

    [TestMethod]
    public void ParseComplexQuery()
    {
        var root = QueryParser.ParseExpression("Commits.Where(c => c.Message.Contains(\"fix\")).Take(10)");
        
        // Outer: Take(10)
        Assert.IsInstanceOfType(root, typeof(MethodCallNode));
        var takeCall = (MethodCallNode)root;
        Assert.AreEqual("Take", takeCall.Method);
        
        // Middle: Where(...)
        Assert.IsInstanceOfType(takeCall.Target, typeof(MethodCallNode));
        var whereCall = (MethodCallNode)takeCall.Target;
        Assert.AreEqual("Where", whereCall.Method);
        
        // Inner: Commits
        Assert.IsInstanceOfType(whereCall.Target, typeof(IdentifierNode));
        Assert.AreEqual("Commits", ((IdentifierNode)whereCall.Target).Name);
    }
}
