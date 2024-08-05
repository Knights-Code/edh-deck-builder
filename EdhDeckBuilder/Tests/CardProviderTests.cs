using EdhDeckBuilder.Model;
using EdhDeckBuilder.Service;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
        }

        [Test]
        public void TryGetCardTest()
        {
            var card = _cardProvider.TryGetCardModelAsync("Ancestor's Chosen",
                new CancellationTokenSource()).Result;
            var cardImageUrl = card.BuildGathererUrl();

            Assert.NotNull(card);
            Assert.AreEqual("https://gatherer.wizards.com/Handlers/Image.ashx?multiverseid=130550&type=card", cardImageUrl);
        }

        [Test]
        public void DownloadImageForCardTest()
        {
            var image = _cardProvider.GetCardImageAsync("Ancestor's Chosen",
                new CancellationTokenSource()).Result;

            Assert.NotNull(image);
        }

        [Test]
        public void CardTypesAndTaggerPropertiesTest()
        {
            var card = _cardProvider.TryGetCardModelAsync("Junk Jet",
                new CancellationTokenSource()).Result;

            Assert.Contains("Artifact", card.AllTypes);
            Assert.Contains("Equipment", card.AllTypes);
            Assert.AreEqual("60", card.CollectorNumber);
            Assert.AreEqual("PIP", card.SetCode);
        }

        [Test]
        public void CardProviderLoadsCardInATimelyFashion()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var card = _cardProvider.TryGetCardModelAsync("Junk Jet",
                new CancellationTokenSource());

            stopwatch.Stop();

            Assert.NotNull(card.Result);
            Assert.Less(stopwatch.ElapsedMilliseconds, 5000);

            Console.WriteLine($"Retrieved card in {stopwatch.ElapsedMilliseconds}ms.");
        }

        [Test]
        public void CardProviderLoadsMultipleCardsInATimelyFashion()
        {

            var deckProvider = new DeckProvider();
            var deckModel = deckProvider.LoadDeck(
                @"C:\Users\pugtu\Documents\Games\Magic The Gathering\Commander\Billy and the Cloneasaurus\billy_1-96.csv");
            var manifest = deckModel.Cards.Select((c) => c.Name).ToList();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var cards = _cardProvider.TryGetCardModelsAsync(
                manifest,
                new CancellationTokenSource());

            stopwatch.Stop();

            Assert.IsNotEmpty(cards.Result);
            Assert.AreEqual(manifest.Count, cards.Result.Count);
            Assert.Less(stopwatch.ElapsedMilliseconds, 5000);

            Console.WriteLine($"Retrieved card in {stopwatch.ElapsedMilliseconds}ms.");
        }
    }
}
