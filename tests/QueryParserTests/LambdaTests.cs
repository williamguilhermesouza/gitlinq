using GitLinq;
using GitLinq.AST;

namespace Tests.QueryParserTests;

[TestClass]
public class LambdaTests
{
    [TestMethod]
    public void ParseSimpleLambda()
    {
        var root = QueryParser.ParseExpression("c => c.Message");
        
        Assert.IsInstanceOfType(root, typeof(LambdaNode));
        var node = (LambdaNode)root;
        
        Assert.AreEqual("c", node.Parameter);
        Assert.IsInstanceOfType(node.Body, typeof(MemberAccessNode));
    }

    [TestMethod]
    public void ParseLambdaWithParentheses()
    {
        var root = QueryParser.ParseExpression("(c) => c.Message");
        
        Assert.IsInstanceOfType(root, typeof(LambdaNode));
        var node = (LambdaNode)root;
        
        Assert.AreEqual("c", node.Parameter);
    }

    [TestMethod]
    public void ParseLambdaWithMethodCallBody()
    {
        var root = QueryParser.ParseExpression("c => c.Message.Contains(\"test\")");
        
        Assert.IsInstanceOfType(root, typeof(LambdaNode));
        var node = (LambdaNode)root;
        
        Assert.AreEqual("c", node.Parameter);
        Assert.IsInstanceOfType(node.Body, typeof(MethodCallNode));
        
        var methodCall = (MethodCallNode)node.Body;
        Assert.AreEqual("Contains", methodCall.Method);
    }

    [TestMethod]
    public void ParseLambdaWithDifferentParameterNames()
    {
        var root = QueryParser.ParseExpression("commit => commit.Sha");
        
        Assert.IsInstanceOfType(root, typeof(LambdaNode));
        var node = (LambdaNode)root;
        
        Assert.AreEqual("commit", node.Parameter);
        
        var body = (MemberAccessNode)node.Body;
        var target = (IdentifierNode)body.Target;
        Assert.AreEqual("commit", target.Name);
    }

    [TestMethod]
    public void ParseLambdaWithChainedMemberAccess()
    {
        var root = QueryParser.ParseExpression("x => x.Author.Name");
        
        Assert.IsInstanceOfType(root, typeof(LambdaNode));
        var node = (LambdaNode)root;
        
        Assert.AreEqual("x", node.Parameter);
        Assert.IsInstanceOfType(node.Body, typeof(MemberAccessNode));
        
        var outerAccess = (MemberAccessNode)node.Body;
        Assert.AreEqual("Name", outerAccess.Member);
        
        var innerAccess = (MemberAccessNode)outerAccess.Target;
        Assert.AreEqual("Author", innerAccess.Member);
    }
}
