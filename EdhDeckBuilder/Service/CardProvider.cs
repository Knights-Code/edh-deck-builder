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
        public CardProvider()
        {
            _cards = new Dictionary<string, CardModel>();
        }

        public void Initialise()
        {
            // Initialise database.
            var multiverseIdsByUuid = new Dictionary<string, string>();
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

                    if (string.IsNullOrEmpty(multiverseId)) continue; // Ignore cards without multiverse IDs.

                    var uuid = fields[18];

                    if (string.IsNullOrEmpty(uuid)) continue; // Ignore cards without a UUID (don't think this is possible).

                    if (multiverseIdsByUuid.ContainsKey(uuid)) continue; // Only record first multiverse ID for each UUID.

                    multiverseIdsByUuid[uuid] = multiverseId;
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

                    if (!multiverseIdsByUuid.ContainsKey(uuid)) continue; // Ignore cards without multiverse IDs.

                    var name = fields[50];

                    if (string.IsNullOrEmpty(name)) continue; // Ignore cards with no name.

                    var nameAsKey = name.ToLower();

                    if (_cards.ContainsKey(nameAsKey)) continue; // Only store first occurrence.

                    _cards[nameAsKey] = new CardModel { Name = name, CardImage = null, MultiverseId = multiverseIdsByUuid[uuid] };
                }
            }

            _initialised = true;
        }

        public CardModel TryGetCard(string name)
        {
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
        public Image DownloadImageForCard(CardModel cardModel)
        {
            if (cardModel.CardImage != null) return cardModel.CardImage;

            var client = new WebClient();
            var url = cardModel.BuildGathererUrl();
            var imageData = client.DownloadData(url);

            Image image;

            using (var memoryStream = new MemoryStream(imageData))
            {
                image = Image.FromStream(memoryStream);
            }

            cardModel.CardImage = image;

            return image;
        }
    }
}
