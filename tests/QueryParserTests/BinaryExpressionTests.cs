using GitLinq;
using GitLinq.AST;

namespace Tests.QueryParserTests;

[TestClass]
public class BinaryExpressionTests
{
    [TestMethod]
    public void ParseGreaterThan_ReturnsCorrectBinaryNode()
    {
        var result = QueryParser.ParseExpression("x > 5");
        
        Assert.IsInstanceOfType(result, typeof(BinaryNode));
        var binary = (BinaryNode)result;
        Assert.AreEqual(">", binary.Operator);
        Assert.IsInstanceOfType(binary.Left, typeof(IdentifierNode));
        Assert.IsInstanceOfType(binary.Right, typeof(NumberLiteralNode));
    }

    [TestMethod]
    public void ParseLessThan_ReturnsCorrectBinaryNode()
    {
        var result = QueryParser.ParseExpression("x < 10");
        
        Assert.IsInstanceOfType(result, typeof(BinaryNode));
        var binary = (BinaryNode)result;
        Assert.AreEqual("<", binary.Operator);
    }

    [TestMethod]
    public void ParseGreaterThanOrEqual_ReturnsCorrectBinaryNode()
    {
        var result = QueryParser.ParseExpression("x >= 5");
        
        Assert.IsInstanceOfType(result, typeof(BinaryNode));
        var binary = (BinaryNode)result;
        Assert.AreEqual(">=", binary.Operator);
    }

    [TestMethod]
    public void ParseLessThanOrEqual_ReturnsCorrectBinaryNode()
    {
        var result = QueryParser.ParseExpression("x <= 10");
        
        Assert.IsInstanceOfType(result, typeof(BinaryNode));
        var binary = (BinaryNode)result;
        Assert.AreEqual("<=", binary.Operator);
    }

    [TestMethod]
    public void ParseEqual_ReturnsCorrectBinaryNode()
    {
        var result = QueryParser.ParseExpression("x == 5");
        
        Assert.IsInstanceOfType(result, typeof(BinaryNode));
        var binary = (BinaryNode)result;
        Assert.AreEqual("==", binary.Operator);
    }

    [TestMethod]
    public void ParseNotEqual_ReturnsCorrectBinaryNode()
    {
        var result = QueryParser.ParseExpression("x != 0");
        
        Assert.IsInstanceOfType(result, typeof(BinaryNode));
        var binary = (BinaryNode)result;
        Assert.AreEqual("!=", binary.Operator);
    }

    [TestMethod]
    public void ParseMemberAccessComparison_ReturnsCorrectStructure()
    {
        var result = QueryParser.ParseExpression("d.FilesChanged > 5");
        
        Assert.IsInstanceOfType(result, typeof(BinaryNode));
        var binary = (BinaryNode)result;
        Assert.AreEqual(">", binary.Operator);
        Assert.IsInstanceOfType(binary.Left, typeof(MemberAccessNode));
        Assert.IsInstanceOfType(binary.Right, typeof(NumberLiteralNode));
        
        var memberAccess = (MemberAccessNode)binary.Left;
        Assert.AreEqual("FilesChanged", memberAccess.Member);
    }

    [TestMethod]
    public void ParseLambdaWithComparison_ReturnsCorrectStructure()
    {
        var result = QueryParser.ParseExpression("d => d.TotalLinesAdded > 100");
        
        Assert.IsInstanceOfType(result, typeof(LambdaNode));
        var lambda = (LambdaNode)result;
        Assert.AreEqual("d", lambda.Parameter);
        Assert.IsInstanceOfType(lambda.Body, typeof(BinaryNode));
        
        var binary = (BinaryNode)lambda.Body;
        Assert.AreEqual(">", binary.Operator);
    }

    [TestMethod]
    public void ParseMethodCallWithLambdaComparison_ReturnsCorrectStructure()
    {
        var result = QueryParser.ParseExpression("Diffs.Where(d => d.FilesChanged >= 3)");
        
        Assert.IsInstanceOfType(result, typeof(MethodCallNode));
        var methodCall = (MethodCallNode)result;
        Assert.AreEqual("Where", methodCall.Method);
        
        var args = methodCall.Arguments.ToList();
        Assert.AreEqual(1, args.Count);
        Assert.IsInstanceOfType(args[0], typeof(LambdaNode));
        
        var lambda = (LambdaNode)args[0];
        Assert.IsInstanceOfType(lambda.Body, typeof(BinaryNode));
        
        var binary = (BinaryNode)lambda.Body;
        Assert.AreEqual(">=", binary.Operator);
    }

    [TestMethod]
    public void ParseComparisonWithZero_ReturnsCorrectValue()
    {
        var result = QueryParser.ParseExpression("x != 0");
        
        Assert.IsInstanceOfType(result, typeof(BinaryNode));
        var binary = (BinaryNode)result;
        Assert.IsInstanceOfType(binary.Right, typeof(NumberLiteralNode));
        
        var number = (NumberLiteralNode)binary.Right;
        Assert.AreEqual(0, number.Value);
    }

    [TestMethod]
    public void ParseExpressionWithoutComparison_ReturnsNonBinaryNode()
    {
        var result = QueryParser.ParseExpression("Commits.Take(10)");
        
        // Should not be a BinaryNode - it's a MethodCallNode
        Assert.IsInstanceOfType(result, typeof(MethodCallNode));
    }

    [TestMethod]
    public void ParseChainedMethodWithComparison_ReturnsCorrectStructure()
    {
        var result = QueryParser.ParseExpression("Diffs.Where(d => d.TotalLinesAdded > 50).Take(10)");
        
        Assert.IsInstanceOfType(result, typeof(MethodCallNode));
        var takeCall = (MethodCallNode)result;
        Assert.AreEqual("Take", takeCall.Method);
        
        Assert.IsInstanceOfType(takeCall.Target, typeof(MethodCallNode));
        var whereCall = (MethodCallNode)takeCall.Target;
        Assert.AreEqual("Where", whereCall.Method);
    }
}
