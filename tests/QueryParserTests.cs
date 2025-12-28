using GitLinq;
using GitLinq.AST;

namespace QueryParserTest
{
    [TestClass]
    public sealed class BaseTests
    {
        [TestMethod]
        public void ParseIdentifier()
        {
            var identifier = "Commits";

            var root = QueryParser.ParseExpression(identifier);

            Assert.IsNotNull(root);
            Assert.IsInstanceOfType(root, typeof(IdentifierNode));
        }
    }
}
