using NUnit.Framework;

namespace DeltaX.Modules.TagRuleEvaluator.UnitTest
{
    public class TagRuleEvaluatorUnitTest
    {
        TagRuleChangeEvaluator<int> tagRuleChangeEvaluator;

        [SetUp]
        public void Setup()
        {
             tagRuleChangeEvaluator = new TagRuleChangeEvaluator<int>();
            // tagRuleChangeEvaluator.AddRule(1, )
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}