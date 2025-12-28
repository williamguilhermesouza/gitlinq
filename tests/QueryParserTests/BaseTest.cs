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
        Assert.IsInstanceOfType(root, typeof(IdentifierNode));
    }
}