using EdhDeckBuilder.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Tests.Model
{
    [TestFixture]
    public class CardModelTests
    {
        [Test]
        public void BuildScryfallUrl_WhenScryfallIdIsNull_ReturnsEmptyString()
        {
            var cardModel = new CardModel { Name = "Undesigned Card" };

            var url = cardModel.BuildScryfallUrl();

            Assert.AreEqual(string.Empty, url);
        }

        [Test]
        public void BuildScryfallUrl_WhenScryfallIdIsEmptyString_ReturnsEmptyString()
        {
            var cardModel = new CardModel { Name = "Missing Data Card", ScryfallId = string.Empty };

            var url = cardModel.BuildScryfallUrl();

            Assert.AreEqual(string.Empty, url);
        }

        [Test]
        public void BuildScryfallUrl_WhenScryfallIdIsValid_ReturnsCorrectUrl()
        {
            var cardModel = new CardModel { Name = "Bronzebeak Foragers", ScryfallId = "dadfcd91-3000-448e-a304-be425ff68644" };

            var url = cardModel.BuildScryfallUrl();

            Assert.AreEqual("https://cards.scryfall.io/large/front/d/a/dadfcd91-3000-448e-a304-be425ff68644.jpg", url);
        }

        [Test]
        public void BuildGathererUrl_WhenMultiverseIdIsNull_ReturnsEmptyString()
        {
            var cardModel = new CardModel { Name = "Undesigned Card" };

            var url = cardModel.BuildGathererUrl();

            Assert.AreEqual(string.Empty, url);
        }

        [Test]
        public void BuildGathererUrl_WhenMultiverseIdIsEmptyString_ReturnsEmptyString()
        {
            var cardModel = new CardModel { Name = "Missing Data Card", MultiverseId = string.Empty };

            var url = cardModel.BuildGathererUrl();

            Assert.AreEqual(string.Empty, url);
        }

        [Test]
        public void BuildGathererUrl_WhenMultiverseIdIsValid_ReturnsCorrectUrl()
        {
            var cardModel = new CardModel { Name = "Ancestor's Chosen", MultiverseId = "130550" };

            var url = cardModel.BuildGathererUrl();

            Assert.AreEqual("https://gatherer.wizards.com/Handlers/Image.ashx?multiverseid=130550&type=card", url);
        }
    }
}
