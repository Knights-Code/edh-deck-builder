using EdhDeckBuilder.Model;
using EdhDeckBuilder.Service;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Tests.Model
{
    [TestFixture]
    public class DeckProviderTests
    {
        private string _testDeckFilename = "test_deck.csv";

        [SetUp]
        public void SetUp()
        {
            if (File.Exists(_testDeckFilename)) File.Delete(_testDeckFilename);
        }

        [Test]
        public void SaveDeck_WhenGivenDeckModelWithCards_SavesCorrectly()
        {
            var deckProvider = new DeckProvider();
            var deckModel = new DeckModel("Test Deck");
            var card1 = new CardModel
            {
                Name = "Skullclamp",
                NumCopies = 1
            };
            var card2 = new CardModel
            {
                Name = "Llanowar Elves",
                NumCopies = 1
            };
            var card3 = new CardModel
            {
                Name = "Forest",
                NumCopies = 10
            };

            deckModel.AddCards(new List<CardModel> { card1, card2, card3 });

            deckProvider.SaveDeck(deckModel, _testDeckFilename);

            using (var reader = new StreamReader(_testDeckFilename))
            {
                var fileText = reader.ReadToEnd();
                Assert.AreEqual("Test Deck,Ramp,Draw,Removal,Wipe,Land,Standalone,Enhancer,Enabler,Tapland\r\n1,Skullclamp\r\n1,Llanowar Elves\r\n10,Forest\r\n", fileText);
            }
        }

        [Test]
        public void SaveDeck_WhenCardHasCommaInName_PutsNameInQuotes()
        {
            var deckProvider = new DeckProvider();
            var deckModel = new DeckModel("Test Deck");
            var card1 = new CardModel
            {
                Name = "Skullclamp",
                NumCopies = 1
            };
            var card2 = new CardModel
            {
                Name = "Ghalta, Primal Hunger",
                NumCopies = 1
            };
            var card3 = new CardModel
            {
                Name = "Forest",
                NumCopies = 10
            };

            deckModel.AddCards(new List<CardModel> { card1, card2, card3 });

            deckProvider.SaveDeck(deckModel, _testDeckFilename);

            using (var reader = new StreamReader(_testDeckFilename))
            {
                var fileText = reader.ReadToEnd();
                Assert.AreEqual("Test Deck,Ramp,Draw,Removal,Wipe,Land,Standalone,Enhancer,Enabler,Tapland\r\n1,Skullclamp\r\n1,\"Ghalta, Primal Hunger\"\r\n10,Forest\r\n", fileText);
            }
        }

        [Test]
        public void SaveDeck_WhenCardHasCommaInNameAndIsDoubleFaced_PutsNameInQuotes()
        {
            var deckProvider = new DeckProvider();
            var deckModel = new DeckModel("Test Deck");
            var card1 = new CardModel
            {
                Name = "Ojer Axonil, Deepest Might // Temple of Power",
                NumCopies = 1
            };
            var card2 = new CardModel
            {
                Name = "Ghalta, Primal Hunger",
                NumCopies = 1
            };
            var card3 = new CardModel
            {
                Name = "Forest",
                NumCopies = 10
            };

            deckModel.AddCards(new List<CardModel> { card1, card2, card3 });

            deckProvider.SaveDeck(deckModel, _testDeckFilename);

            using (var reader = new StreamReader(_testDeckFilename))
            {
                var fileText = reader.ReadToEnd();
                Assert.AreEqual("Test Deck,Ramp,Draw,Removal,Wipe,Land,Standalone,Enhancer,Enabler,Tapland\r\n1,\"Ojer Axonil, Deepest Might // Temple of Power\"\r\n1,\"Ghalta, Primal Hunger\"\r\n10,Forest\r\n", fileText);
            }
        }

        [Test]
        public void LoadDeck_WhenGivenFilePathWithDeckWithCustomRoles_LoadsCorrectly()
        {
            using (var writer = new StreamWriter(new FileStream(_testDeckFilename, FileMode.OpenOrCreate)))
            {
                writer.WriteLine("Test Deck,Ramp,Draw,Removal,Wipe,Land,Standalone,Enhancer,Enabler,Tapland,Unplayables");
                writer.WriteLine("10,Forest");
                writer.WriteLine("1,Skullclamp");
                writer.WriteLine("1,\"Ghalta, Primal Hunger\"");
            }

            var deckProvider = new DeckProvider();

            var deck = deckProvider.LoadDeck(_testDeckFilename);

            Assert.NotNull(deck.CustomRoles);
            Assert.AreEqual(1, deck.CustomRoles.Count);
            var role = deck.CustomRoles.FirstOrDefault(r => r == "Unplayables");
            Assert.NotNull(role);
        }

        [Test]
        public void LoadDeck_WhenGivenFilePathWithValidDeck_LoadsCorrectly()
        {
            using (var writer = new StreamWriter(new FileStream(_testDeckFilename, FileMode.OpenOrCreate)))
            {
                writer.WriteLine("Test Deck");
                writer.WriteLine("10,Forest");
                writer.WriteLine("1,Skullclamp");
                writer.WriteLine("1,\"Ghalta, Primal Hunger\"");
            }

            var deckProvider = new DeckProvider();

            var deck = deckProvider.LoadDeck(_testDeckFilename);

            var skullclamp = deck.Cards.FirstOrDefault(c => c.Name == "Skullclamp");
            var ghalta = deck.Cards.FirstOrDefault(c => c.Name == "Ghalta, Primal Hunger");
            var forest = deck.Cards.FirstOrDefault(c => c.Name == "Forest");
            Assert.AreEqual("Test Deck", deck.Name);
            Assert.NotNull(skullclamp);
            Assert.NotNull(ghalta);
            Assert.NotNull(forest);
            Assert.AreEqual(1, skullclamp.NumCopies);
            Assert.AreEqual(1, ghalta.NumCopies);
            Assert.AreEqual(10, forest.NumCopies);
        }

        [Test]
        public void SavesDeckThenLoadsDeckCorrectly()
        {
            var deckProvider = new DeckProvider();
            var deckModel = new DeckModel("Test Deck");
            var card1 = new CardModel
            {
                Name = "Skullclamp",
                NumCopies = 1
            };
            var card2 = new CardModel
            {
                Name = "Ghalta, Primal Hunger",
                NumCopies = 1
            };
            var card3 = new CardModel
            {
                Name = "Forest",
                NumCopies = 10
            };

            deckModel.AddCards(new List<CardModel> { card1, card2, card3 });

            deckProvider.SaveDeck(deckModel, _testDeckFilename);
            var loadedDeck = deckProvider.LoadDeck(_testDeckFilename);

            Assert.NotNull(loadedDeck);
            Assert.AreEqual(3, loadedDeck.Cards.Count);
            var skullclamp = loadedDeck.Cards.FirstOrDefault(c => c.Name == "Skullclamp");
            var ghalta = loadedDeck.Cards.FirstOrDefault(c => c.Name == "Ghalta, Primal Hunger");
            var forest = loadedDeck.Cards.FirstOrDefault(c => c.Name == "Forest");
            Assert.AreEqual("Test Deck", loadedDeck.Name);
            Assert.NotNull(skullclamp);
            Assert.NotNull(ghalta);
            Assert.NotNull(forest);
            Assert.AreEqual(1, skullclamp.NumCopies);
            Assert.AreEqual(1, ghalta.NumCopies);
            Assert.AreEqual(10, forest.NumCopies);
        }
    }
}
