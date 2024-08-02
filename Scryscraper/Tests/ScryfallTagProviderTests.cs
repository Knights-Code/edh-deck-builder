using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scryscraper.Tests
{
    [TestFixture]
    public class ScryfallTagProviderTests
    {
        private ScryfallTagProvider _scryfallTagProvider;

        [SetUp]
        public void SetUp()
        {
            _scryfallTagProvider = new ScryfallTagProvider();
        }

        [Test]
        public async Task ScryfallTagProvider_WhenGivenCardDictionary_ReturnsDictionaryOfTagsByCardName()
        {
            var inputDictionary = new Dictionary<string, string>();
            inputDictionary["Junk Jet"] = "https://tagger.scryfall.com/card/pip/60";
            inputDictionary["Sol Ring"] = "https://tagger.scryfall.com/card/otc/267";

            var tagDictionary = await _scryfallTagProvider.GetScryfallTagsAsync(inputDictionary);

            Assert.IsNotEmpty(tagDictionary);
            var card1Tags = tagDictionary["Junk Jet"];
            var card2Tags = tagDictionary["Sol Ring"];

            foreach(var cardName in tagDictionary.Keys)
            {
                Console.WriteLine($"Tags for {cardName}:");

                foreach (var tag in tagDictionary[cardName])
                {
                    Console.WriteLine(tag);
                }
            }

            Assert.Contains("alliteration", card1Tags);
            Assert.Contains("exponential", card1Tags);
            Assert.Contains("impulsive draw", card1Tags);
            Assert.Contains("power doubler", card1Tags);
            Assert.Contains("sacrifice outlet-artifact", card1Tags);
            Assert.Contains("synergy-artifact", card1Tags);
            Assert.Contains("card names", card1Tags);
            Assert.Contains("power matters", card1Tags);
            Assert.Contains("red effect", card1Tags);
            Assert.Contains("sacrifice outlet", card1Tags);

            Assert.Contains("adds multiple mana", card2Tags);
            Assert.Contains("mana rock", card2Tags);
            Assert.Contains("mana producer", card2Tags);
            Assert.Contains("ramp", card2Tags);
        }
    }
}
