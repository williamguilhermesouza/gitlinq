using GitLinq;
using GitLinq.AST;

namespace Tests.QueryParserTests;

/// <summary>
/// Base tests for QueryParser - kept for backward compatibility.
/// See individual test files for comprehensive coverage.
/// </summary>
[TestClass]
public class BaseTest
{
    [TestMethod]
    public void ParseIdentifier()
    {
        const string identifier = "Commits";
        var root = QueryParser.ParseExpression(identifier);
        Assert.IsInstanceOfType(root, typeof(IdentifierNode), "Incorrect node type");
    }

    [TestMethod]
    public void ParseStringLiteral()
    {
        const string quotedText = "\"quoted\"";
        var root = QueryParser.ParseExpression(quotedText);
        Assert.IsInstanceOfType(root, typeof(StringLiteralNode), "Incorrect node type");

        var node = root as StringLiteralNode;
        Assert.AreEqual("quoted", node!.Value);
    }

    [TestMethod]
    public void ParseMemberAccess()
    {
        const string input = "Commit.Message";
        var root = QueryParser.ParseExpression(input);

        var node = root as MemberAccessNode;
        Assert.IsNotNull(node, "Wrong node type");
        Assert.AreEqual("Message", node.Member);
        
        var target = node.Target as IdentifierNode;
        Assert.IsNotNull(target);
        Assert.AreEqual("Commit", target.Name);
    }

    [TestMethod]
    public void ParseSimpleLambda()
    {
        const string input = "c => c.Message";
        var root = QueryParser.ParseExpression(input);

        var node = root as LambdaNode;
        Assert.IsNotNull(node);
        Assert.AreEqual("c", node.Parameter);
        Assert.IsInstanceOfType(node.Body, typeof(MemberAccessNode));
    }

    [TestMethod]
    public void ParseMethodCall()
    {
        const string expr = "Commits.Where(c => c.Message.Contains(\"test\"))";
        var root = QueryParser.ParseExpression(expr);

        var methodCallNode = root as MethodCallNode;
        Assert.IsNotNull(methodCallNode);
        Assert.AreEqual("Where", methodCallNode.Method);

        var target = methodCallNode.Target as IdentifierNode;
        Assert.IsNotNull(target);
        Assert.AreEqual("Commits", target.Name);

        var lambda = methodCallNode.Arguments.First() as LambdaNode;
        Assert.IsNotNull(lambda);
        Assert.AreEqual("c", lambda.Parameter);
    }
}

