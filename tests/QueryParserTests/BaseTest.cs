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
        Assert.IsTrue(nodeTarget.Name == "c", "Wrong target name");

        var nodeMember = memberTarget.Member;
        Assert.IsTrue(nodeMember != null, "Error parsing member");
        Assert.IsTrue(nodeMember == "Message", "Wrong member name");

        Assert.AreEqual(body.Method, "Contains", $"Wrong method name: {body.Method}");

        var methodArgument = body.Arguments.FirstOrDefault() as StringLiteralNode;
        Assert.IsTrue(methodArgument != null, $"Error parsing method arg: {methodArgument}");
        Assert.AreEqual(methodArgument.Value, "test", $"Error parsing method arg: {methodArgument.Value}");
    }
    [TestMethod]
    public void ParseMethodCall()
    {
        const string expr = """Commits.Where(c => c.Message.Contains("test"))""";
        var root = QueryParser.ParseExpression(expr);

        var methodCallNode = root as MethodCallNode;
        Assert.IsTrue(methodCallNode != null, $"Error parsing method call node: {root.GetType()}");
        Assert.AreEqual(methodCallNode.Method, "Where", $"Wrong method call name: {methodCallNode.Method}");

        var target = methodCallNode.Target as IdentifierNode;
        Assert.IsTrue(target != null, $"Error parsing target node: {methodCallNode.Target.GetType()}");
        Assert.AreEqual(target.Name, "Commits", $"Wrong target node value: {target.Name}");

        var argument = methodCallNode.Arguments.FirstOrDefault() as LambdaNode;
        Assert.IsTrue(argument != null, $"Error parsing lambda node: {methodCallNode.Arguments.FirstOrDefault()?.GetType()}");
        Assert.AreEqual(argument.Parameter, "c", $"Wrong lambda parameter: {argument.Parameter}");

        var body = argument.Body as MethodCallNode;
        Assert.IsTrue(body != null, $"Error parsing lambda body node: {body.GetType()}");
        Assert.AreEqual(body.Method, "Contains", $"Wrong lambda method call method: {body.Method}");

        var containsArg = body.Arguments.FirstOrDefault() as StringLiteralNode;
        Assert.IsTrue(containsArg != null, $"Error parsing lambda method call arg node: {body.Arguments.FirstOrDefault()?.GetType()}");
        Assert.AreEqual(containsArg.Value, "test", $"Wrong lambda method call arg value: {containsArg.Value}");

        var lambdaTarget = body.Target as MemberAccessNode;
        Assert.IsTrue(lambdaTarget != null, $"Error parsing lambda member access target node: {body.Target.GetType()}");
        Assert.AreEqual(lambdaTarget.Member, "Message", $"Wrong lambda member access method value: {lambdaTarget.Member}");

        var memberAccessTarget = lambdaTarget.Target as IdentifierNode;
        Assert.IsTrue(memberAccessTarget != null, $"Error lambda member access arg node: {lambdaTarget.Target.GetType()}");
        Assert.AreEqual(memberAccessTarget.Name, "c", $"Wrong lambda member access arg node value: {memberAccessTarget.Name}");
    }
}
