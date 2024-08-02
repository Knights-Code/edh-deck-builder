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

        private Dictionary<string, int> IdentifyColumnNumbers(IList<string> columnHeaderNames, string[] columnHeaders)
        {
            if (!columnHeaderNames.Any())
            {
                throw new ArgumentException("Need at least one column header name to retrieve column number(s).", nameof(columnHeaderNames));
            }

            if (!columnHeaders.Any())
            {
                throw new ArgumentException("No column headers were provided.", nameof(columnHeaders));
            }

            var result = new Dictionary<string, int>();

            for (var i = 0; i < columnHeaders.Length; i++)
            {
                foreach (var columnHeaderName in columnHeaderNames)
                {
                    if (columnHeaders[i] == columnHeaderName)
                    {
                        result[columnHeaderName] = i;
                        break;
                    }
                }
            }

            return result;
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

                // Identify key columns and their number.
                var columnHeaderNumbersByColumnName = IdentifyColumnNumbers(new List<string> { 
                    "multiverseId",
                    "scryfallId", 
                    "uuid" }, csvParser.ReadFields());

                if (columnHeaderNumbersByColumnName.Keys.Count < 3)
                {
                    // Couldn't locate all columns.
                    throw new InvalidDataException("Card identifiers CSV is missing one or more key columns. Key columns are multiverseId," +
                        " scryfallId, and uuid. Please exit the application, check your data files, and try again.\n\n" +
                        $"File path: {cardIdentifiersPath}");
                }

                while (!csvParser.EndOfData)
                {
                    var fields = csvParser.ReadFields();
                    var multiverseId = fields[columnHeaderNumbersByColumnName["multiverseId"]];
                    var scryfallId = fields[columnHeaderNumbersByColumnName["scryfallId"]];

                    var uuid = fields[columnHeaderNumbersByColumnName["uuid"]];

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

                // Identify key columns and their number.
                var columnHeaderNumbersByColumnName = IdentifyColumnNumbers(new List<string> {
                    "uuid",
                    "name" }, csvParser.ReadFields());

                if (columnHeaderNumbersByColumnName.Keys.Count < 2)
                {
                    // Couldn't locate all columns.
                    throw new InvalidDataException("Cards CSV is missing one or more key columns. Key columns are multiverseId," +
                        " scryfallId, and uuid. Please exit the application, check your data files, and try again.\n\n" +
                        $"File path: {cardIdentifiersPath}");
                }

                while (!csvParser.EndOfData)
                {
                    var fields = csvParser.ReadFields();
                    var uuid = fields[columnHeaderNumbersByColumnName["uuid"]];
                    var name = fields[columnHeaderNumbersByColumnName["name"]];

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
        /// Retrieves a card image from memory if cached, or from the web otherwise.
        /// </summary>
        /// <param name="cardModel">The card to retrieve the image for.</param>
        /// <param name="back">If true, retrieves image for back of card, otherwise retrieves the front of the card.</param>
        /// <returns>Image of the card front or card back.</returns>
        public Image GetCardImage(string cardName, bool back = false)
        {
            CardModel cardModel;
            cardModel = TryGetCard(cardName);
            if (cardModel == null) return null;

            if (!back && cardModel.CardImage != null) return cardModel.CardImage;
            else if (back && cardModel.BackImage != null) return cardModel.BackImage;

            if (!cardModel.HasDownloadableImage) return null;

            var gathererUrl = cardModel.BuildGathererUrl();

            if (cardModel.CardImage == null)
            {
                var scryfallFrontUrl = cardModel.BuildScryfallUrl();
                var hasScryfallFrontUrl = !string.IsNullOrEmpty(scryfallFrontUrl);
                var image = DownloadImage(hasScryfallFrontUrl ? scryfallFrontUrl : gathererUrl);
                cardModel.CardImage = image;
            }

            if (cardModel.BackImage == null)
            {
                var scryfallBackUrl = cardModel.BuildScryfallUrl(true);
                var hasScryfallBackUrl = !string.IsNullOrEmpty(scryfallBackUrl);
                var backImage = hasScryfallBackUrl ? DownloadImage(scryfallBackUrl) : null;
                cardModel.BackImage = backImage ?? _cardBack;
            }

            // Should have non-null values for both front and back now.
            return back ? cardModel.BackImage : cardModel.CardImage;
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
