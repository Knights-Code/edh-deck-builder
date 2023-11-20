using EdhDeckBuilder.Model;
using EdhDeckBuilder.Service;
using NUnit.Framework;
using System.Collections.Generic;

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

        [Test]
        public void CardsToClipboardFormat_WhenGivenValidCardModels_ReturnsCardListAsString()
        {
            var cards = new List<CardModel>
            {
                new CardModel { Name = "Forest", NumCopies = 10 },
                new CardModel { Name = "Ghalta, Primal Hunger", NumCopies = 1 }
            };

            var clipboardFormat = UtilityFunctions.CardsToClipboardFormat(cards);

            Assert.AreEqual("10 Forest\r\n1 Ghalta, Primal Hunger", clipboardFormat);
        }
    }
}
