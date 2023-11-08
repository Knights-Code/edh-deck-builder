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
        [Test]
        public void EndToEnd()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var cardProvider = new CardProvider();
            cardProvider.Initialise();
            Console.WriteLine("Initialising database...");
            stopwatch.Stop();
            Console.WriteLine($"Database initialisation complete ({stopwatch.ElapsedMilliseconds} ms)");
            var card = cardProvider.TryGetCard("Ancestor's Chosen");
            var cardImageUrl = card.BuildGathererUrl();
            Console.WriteLine($"Gatherer URL: {cardImageUrl}");

            Assert.That(card != null);
            Assert.AreEqual("https://gatherer.wizards.com/Handlers/Image.ashx?multiverseid=130550&type=card", cardImageUrl);
        }
    }
}
