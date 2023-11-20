using EdhDeckBuilder.Service;
using NUnit.Framework;

namespace EdhDeckBuilder.Tests
{
    [TestFixture]
    public class UtilityFunctionsTests
    {
        [Test]
        public void ParseCardsFromText_WhenTextContainsValidCards_ReturnsListOfCardModels()
        {
            var cards = UtilityFunctions.ParseCardsFromText("10 Forest\n1 Foulmire Knight // Profane Insight\n1 Ghalta, Primal Hunger");

            Assert.AreEqual(3, cards.Count);
            var forest = cards[0];
            var foulmireKnight = cards[1];
            var ghalta = cards[2];
            Assert.NotNull(forest);
            Assert.NotNull(foulmireKnight);
            Assert.NotNull(ghalta);
            Assert.AreEqual(10, forest.NumCopies);
            Assert.AreEqual(1, foulmireKnight.NumCopies);
            Assert.AreEqual(1, ghalta.NumCopies);
            Assert.AreEqual("Forest", forest.Name);
            Assert.AreEqual("Foulmire Knight // Profane Insight", foulmireKnight.Name);
            Assert.AreEqual("Ghalta, Primal Hunger", ghalta.Name);
        }
    }
}
