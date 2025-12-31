using GitLinq;
using GitLinq.AST;

namespace Tests.QueryParserTests;

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
        const string quotedText = """
                                  "quoted"
                                  """;
        var root = QueryParser.ParseExpression(quotedText);
        Assert.IsInstanceOfType(root, typeof(StringLiteralNode), "Incorrect node type");
        
        var node = root as StringLiteralNode; 
        Assert.IsTrue(node!.Value == "quoted", $"Incorrect node value: {node.Value}");
    }
    
    [TestMethod]
    public void ParseMemberAccess()
    {
        const string input = "Commit.Message";
        var root = QueryParser.ParseExpression(input);
        
        var node = root as MemberAccessNode; 
        Assert.IsTrue(node != null, "Wrong node type");

        var nodeTarget = node.Target as IdentifierNode;
        Assert.IsTrue(nodeTarget != null, "Error parsing member target");
        Assert.IsTrue(nodeTarget.Name == "Commit", "Wrong target name");
        
        var nodeMember = node.Member;
        Assert.IsTrue(nodeMember != null, "Error parsing member");
        Assert.IsTrue(nodeMember == "Message", "Wrong member name");
    }
    
    [TestMethod]
    public void ParseLambda()
    {
        const string identifier = """c => c.Message.Contains("test")""";
        var root = QueryParser.ParseExpression(identifier);

        var node = root as LambdaNode;
        Assert.IsTrue(node != null, $"Wrong node type: {root.GetType()}");

        var parameter = node.Parameter;
        Assert.AreEqual(parameter, "c", "Wrong parameter");
        
        var body = node.Body as MethodCallNode;
        Assert.IsTrue(body != null, $"Wrong body type: {body.GetType()}");

        var memberTarget = body.Target as MemberAccessNode;
        Assert.IsTrue(memberTarget != null, $"Wrong member target type: {memberTarget.GetType()}");
        
        var nodeTarget = memberTarget.Target as IdentifierNode;
        Assert.IsTrue(nodeTarget != null, "Error parsing member target");
        Assert.IsTrue(nodeTarget.Name == "Commit", "Wrong target name");
        
        var nodeMember = memberTarget.Member;
        Assert.IsTrue(nodeMember != null, "Error parsing member");
        Assert.IsTrue(nodeMember == "Message", "Wrong member name");
        
        Assert.AreEqual(body.Method, "Contains", $"Wrong method name: {body.Method}");

        var methodArgument = body.Arguments.FirstOrDefault() as StringLiteralNode;
        Assert.IsTrue(methodArgument != null, $"Error parsing method arg: {methodArgument}");
        Assert.AreEqual(methodArgument.Value, "test", $"Error parsing method arg: {methodArgument.Value}");
    }
    [TestMethod]
    public void ParseMethodCall() // TODO
    {
        const string identifier = "Commits";
        var root = QueryParser.ParseExpression(identifier);
        Assert.IsInstanceOfType(root, typeof(IdentifierNode));
    }
}