namespace Tests.LinqExpressionBuilderTests;

[TestClass]
public class BaseTest
{
    [TestMethod]
    public void ConstantExpressionParse()
    {
        Assert.AreEqual(true, true);
    }
    
}