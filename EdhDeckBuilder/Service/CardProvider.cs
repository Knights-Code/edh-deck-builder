using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using Microsoft.VisualBasic.FileIO;
using EdhDeckBuilder.Model;
using System.Net;

namespace EdhDeckBuilder.Service
{
    /// <summary>
    /// Retrieves card data from CSV files as well as MTG Gatherer (for images).
    /// </summary>
    public class CardProvider
    {
        private bool _initialised = false;
        private readonly Dictionary<string, CardModel> _cards;
        private Image _cardBack = null;
        public CardProvider()
        {
            _cards = new Dictionary<string, CardModel>();
        }

        public void Initialise()
        {
            // Download generic card back.
            _cardBack = DownloadImage("https://s3.amazonaws.com/ccg-corporate-production/news-images/Back0_Sheet%20(F)20201203163456929.jpg");

            // Initialise database.
            var multiverseAndScryfallIdsByUuid = new Dictionary<string, Tuple<string, string>>();
            var cardIdentifiersPath = @"C:\Users\pugtu\Documents\Games\Magic The Gathering\CSVCardDatabase\cardIdentifiers.csv";

            // Construct map of multiverse IDs by card UUID.
            using (var csvParser = new TextFieldParser(cardIdentifiersPath))
            {
                csvParser.SetDelimiters(new string[] { "," });

                // Skip column headers.
                csvParser.ReadLine();

                while (!csvParser.EndOfData)
                {
                    var fields = csvParser.ReadFields();
                    var multiverseId = fields[12];
                    var scryfallId = fields[13];

                    var uuid = fields[18];

                    if (string.IsNullOrEmpty(uuid)) continue; // Ignore cards without a UUID (don't think this is possible).

                    if (multiverseAndScryfallIdsByUuid.ContainsKey(uuid)) continue; // Only record IDs for first occurrence of the UUID.

                    multiverseAndScryfallIdsByUuid[uuid] = new Tuple<string, string>(multiverseId, scryfallId);
                }
            }

            var cardDataPath = @"C:\Users\pugtu\Documents\Games\Magic The Gathering\CSVCardDatabase\cards.csv";
            using (var csvParser = new TextFieldParser(cardDataPath))
            {
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Skip column headers.
                csvParser.ReadLine();

                while (!csvParser.EndOfData)
                {
                    var fields = csvParser.ReadFields();
                    var uuid = fields[75];

                    var name = fields[50];

                    if (string.IsNullOrEmpty(name)) continue; // Ignore cards with no name.

                    var nameAsKey = name.ToLower();

                    if (_cards.ContainsKey(nameAsKey)) continue; // Only store first occurrence.

                    var multiverseId = string.Empty;
                    var scryfallId = string.Empty;

                    if (multiverseAndScryfallIdsByUuid.ContainsKey(uuid))
                    {
                        multiverseId = multiverseAndScryfallIdsByUuid[uuid].Item1;
                        scryfallId = multiverseAndScryfallIdsByUuid[uuid].Item2;
                    }

                    _cards[nameAsKey] = new CardModel { Name = name, CardImage = null, MultiverseId = multiverseId, ScryfallId = scryfallId };
                }
            }

            _initialised = true;
        }

        public CardModel TryGetCard(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
                
            if (!_initialised) Initialise();

            var nameAsKey = name.ToLower();

            if (!_cards.ContainsKey(nameAsKey)) return null;

            return _cards[nameAsKey];
        }

        /// <summary>
        /// Retrieves a card image from MTG Gatherer.
        /// </summary>
        /// <param name="cardModel">The card to retrieve the image for.</param>
        /// <returns></returns>
        public Image DownloadImageForCard(string cardName)
        {
            CardModel cardModel;
            cardModel = TryGetCard(cardName);
            if (cardModel == null) return null;

            if (cardModel.CardImage != null) return cardModel.CardImage;

            if (!cardModel.HasDownloadableImage) return null;

            var scryfallFrontUrl = cardModel.BuildScryfallUrl();
            var scryfallBackUrl = cardModel.BuildScryfallUrl(true);
            var gathererUrl = cardModel.BuildGathererUrl();
            var hasScryfallFrontUrl = !string.IsNullOrEmpty(scryfallFrontUrl);
            var hasScryfallBackUrl = !string.IsNullOrEmpty(scryfallBackUrl);

            var image = DownloadImage(hasScryfallFrontUrl ? scryfallFrontUrl : gathererUrl);
            var backImage = hasScryfallBackUrl ? DownloadImage(scryfallBackUrl) : null;

            cardModel.CardImage = image;
            cardModel.BackImage = backImage ?? _cardBack;

            return image;
        }

        private Image DownloadImage(string url)
        {
            var client = new WebClient();

            Image image = null;
            try
            {
                var imageData = client.DownloadData(url);

                using (var memoryStream = new MemoryStream(imageData))
                {
                    image = Image.FromStream(memoryStream);
                }
            }
            catch (WebException)
            {
                // Unable to connect but it might just be a bad URL.
                // Do nothing.
            }

            return image;
        }
    }
}
