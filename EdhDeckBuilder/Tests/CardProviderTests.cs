using EdhDeckBuilder.Model;
using EdhDeckBuilder.Service;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Tests
{
    [TestFixture]
    public class CardProviderTests
    {
        private CardProvider _cardProvider;

        [SetUp]
        public void SetUp()
        {
            _cardProvider = new CardProvider();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.WriteLine("Initialising database...");
            _cardProvider.Initialise();
            stopwatch.Stop();
            Console.WriteLine($"Database initialisation complete ({stopwatch.ElapsedMilliseconds} ms)");
        }

        [Test]
        public void TryGetCardTest()
        {
            var card = _cardProvider.TryGetCard("Ancestor's Chosen");
            var cardImageUrl = card.BuildGathererUrl();

            Assert.NotNull(card);
            Assert.AreEqual("https://gatherer.wizards.com/Handlers/Image.ashx?multiverseid=130550&type=card", cardImageUrl);
        }

        [Test]
        public void DownloadImageForCardTest()
        {
            var cardModel = new CardModel { MultiverseId = "130550" };
            var image = _cardProvider.DownloadImageForCard(cardModel);

            Assert.NotNull(image);
            Assert.NotNull(cardModel.CardImage);
        }
    }
}
