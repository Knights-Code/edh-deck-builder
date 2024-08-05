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
using System.Threading;
using System.Net.Http;

namespace EdhDeckBuilder.Service
{
    /// <summary>
    /// Retrieves card data from CSV files as well as MTG Gatherer (for images).
    /// </summary>
    public class CardProvider
    {
        private bool _initialised = false;
        private readonly Dictionary<string, Card> _cards;
        private Image _cardBack = null;
        private Dictionary<string, int> _cardIdentifierColumnNumbers;
        private Dictionary<string, int> _cardDataColumnNumbers;
        private string _cardDataPath;
        private string _cardIdentifiersPath;

        public CardProvider()
        {
            _cards = new Dictionary<string, Card>();
            _cardIdentifierColumnNumbers = new Dictionary<string, int>();
            _cardDataColumnNumbers = new Dictionary<string, int>();
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

        /// <summary>
        /// Establishes connections to database files and verifies they contain all key
        /// columns. The CardProvider is considered "initialised" when it is ready to load card data
        /// from the database files.
        /// </summary>
        public async Task Initialise()
        {
            // Download generic card back.
            _cardBack = await DownloadImageAsync("https://s3.amazonaws.com/ccg-corporate-production/news-images/Back0_Sheet%20(F)20201203163456929.jpg",
                new CancellationTokenSource()).ConfigureAwait(false);

            // Initialise database.
            _cardIdentifiersPath = @"C:\Users\pugtu\Documents\Games\Magic The Gathering\CSVCardDatabase\cardIdentifiers.csv";

            // Construct map of multiverse IDs by card UUID.
            using (var csvParser = new TextFieldParser(_cardIdentifiersPath))
            {
                csvParser.SetDelimiters(new string[] { "," });

                // Identify key columns and their number.
                var keyColumnNames = new List<string> {
                        "multiverseId",
                        "scryfallId",
                        "uuid" };
                lock (_cardIdentifierColumnNumbers)
                {
                    _cardIdentifierColumnNumbers = IdentifyColumnNumbers(keyColumnNames, csvParser.ReadFields());

                    if (_cardIdentifierColumnNumbers.Keys.Count < keyColumnNames.Count)
                    {
                        // Couldn't locate all columns.
                        throw new InvalidDataException("Card identifiers CSV is missing one or more key columns. Key columns are multiverseId," +
                            " scryfallId, and uuid. Please exit the application, check your data files, and try again.\n\n" +
                            $"File path: {_cardIdentifiersPath}");
                    }
                }
            }

            _cardDataPath = @"C:\Users\pugtu\Documents\Games\Magic The Gathering\CSVCardDatabase\cards.csv";

            using (var csvParser = new TextFieldParser(_cardDataPath))
            {
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Identify key columns and their number.
                var keyColumnNames = new List<string> {
                    "uuid",
                    "name",
                    "number",       //  T
                    "setCode",      //  |   Needed for building Scryfall Tagger URL.
                    "subtypes",     //  T
                    "supertypes",   //  |   Needed for type tags.
                    "types"};       //  |

                lock (_cardDataColumnNumbers)
                {
                    _cardDataColumnNumbers = IdentifyColumnNumbers(keyColumnNames, csvParser.ReadFields());

                    if (_cardDataColumnNumbers.Keys.Count < keyColumnNames.Count)
                    {
                        // Couldn't locate all columns.
                        throw new InvalidDataException("Cards CSV is missing one or more key columns. Key columns are" +
                            $" {string.Join(", ", keyColumnNames)}. Please exit the application, check your data files, and try again.\n\n" +
                            $"File path: {_cardDataPath}");
                    }
                }
            }

            _initialised = true;
        }

        private IList<string> ExtractStringsFromStringList(string list)
        {
            var result = list.Split(',').Select((item) => item.Trim()).ToList();
            return result;
        }

        public async Task<CardModel> TryGetCard(string name,
            CancellationTokenSource cancellationTokenSource = null)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (!_initialised) Initialise();

            var lowerCaseName = name.ToLower();

            if (!_cards.ContainsKey(lowerCaseName))
            {
                // Attempt to locate card data in files.
                if (cancellationTokenSource == null)
                {
                    cancellationTokenSource = new CancellationTokenSource();
                }

                await TryGetCardModelAsync(lowerCaseName, cancellationTokenSource);
            }

            lock (_cards)
            {
                return _cards[lowerCaseName].ToModel();
            }
        }

        public async Task<List<CardModel>> TryGetCardsAsync(List<string> names,
            CancellationTokenSource cancellationTokenSource)
        {
            var result = new List<CardModel>();
            if (!names.Any()) return new List<CardModel>();

            if (!_initialised) await Initialise().ConfigureAwait(false);

            // Remove duplicates and convert names to lower case.
            var manifest = names.Distinct().Select((name) => name.ToLower()).ToList();

            // First, try to retrieve cards from memory.
            var cardsFoundInMemory = new Stack<string>();

            lock (_cards)
            {
                foreach (var lowerCaseName in manifest)
                {
                    if (_cards.ContainsKey(lowerCaseName))
                    {
                        // Add to result array, and report card as found.
                        result.Add(_cards[lowerCaseName].ToModel());
                        cardsFoundInMemory.Push(lowerCaseName);
                    }
                }
            }

            // Remove cards already found from manifest.
            while (cardsFoundInMemory.Any())
            {
                manifest.Remove(cardsFoundInMemory.Pop());
            }

            if (!manifest.Any())
            {
                // Found all cards on the manifest in memory.
                return result;
            }
            // One or more cards could not be found in memory, so now
            // attempt to locate them in data files.
            var cardsFromFiles = await TryLoadCardsFromFilesAsync(manifest,
                cancellationTokenSource);

            if (cardsFromFiles.Any())
            {
                result.AddRange(cardsFromFiles.Select((fileCard) => fileCard.ToModel()));
            }

            return result;
        }

        private async Task<Card> TryGetCardAsync(string name, CancellationTokenSource cancellationTokenSource)
        {
            var lowerCaseName = name.ToLower();
            lock (_cards)
            {
                if (_cards.ContainsKey(lowerCaseName))
                {
                    return _cards[lowerCaseName];
                }
            }

            // Attempt to locate card data in files.
            var dataCard = await TryLoadCardFromFilesAsync(lowerCaseName, cancellationTokenSource);

            if (dataCard == null) return null;

            // Add loaded card to memory.
            lock (_cards)
            {
                _cards[lowerCaseName] = dataCard;
            }

            return dataCard;
        }

        public async Task<CardModel> TryGetCardModelAsync(string name,
            CancellationTokenSource cancellationTokenSource)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (!_initialised) Initialise();

            var dataCard = await TryGetCardAsync(name, cancellationTokenSource);
            return dataCard.ToModel();
        }

        private async Task<Card> TryLoadCardFromFilesAsync(string lowerCaseName,
            CancellationTokenSource cancellationTokenSource)
        {
            Card cardResult = null;

            var loadResult = await TryLoadCardsFromFilesAsync(new List<string> { lowerCaseName },
                cancellationTokenSource);

            if (loadResult.Count == 1)
            {
                cardResult = loadResult.First();
            }

            return cardResult;
        }

        private async Task<List<Card>> TryLoadCardsFromFilesAsync(List<string> manifest,
            CancellationTokenSource cancellationTokenSource)
        {
            List<Card> cardResults = new List<Card>();

            await Task.Run(() =>
            {
                using (var csvParser = new TextFieldParser(_cardDataPath))
                {
                    csvParser.SetDelimiters(new string[] { "," });
                    csvParser.HasFieldsEnclosedInQuotes = true;

                    while (!csvParser.EndOfData)
                    {
                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        var fields = csvParser.ReadFields();
                        var uuid = fields[_cardDataColumnNumbers["uuid"]];
                        var name = fields[_cardDataColumnNumbers["name"]];
                        var number = fields[_cardDataColumnNumbers["number"]];
                        var setCode = fields[_cardDataColumnNumbers["setCode"]];
                        var subtypes = fields[_cardDataColumnNumbers["subtypes"]];
                        var supertypes = fields[_cardDataColumnNumbers["supertypes"]];
                        var types = fields[_cardDataColumnNumbers["types"]];

                        if (string.IsNullOrEmpty(name) // Ignore cards with no name.
                            || !manifest.Contains(name.ToLower()) // Skip unless this is one of the
                                                                  // cards we want.
                        ) continue;

                        // If we got this far, we found a match.
                        var nameAsKey = name.ToLower();

                        var allTypes = ExtractStringsFromStringList(subtypes)
                            .Union(ExtractStringsFromStringList(supertypes))
                            .Union(ExtractStringsFromStringList(types))
                            .ToList();

                        cardResults.Add(new Card
                        {
                            Name = name,
                            Uuid = uuid,
                            CardImage = null,
                            CollectorNumber = number,
                            SetCode = setCode.ToLower(),
                            AllTypes = allTypes,
                        });

                        // Remove card name from manifest.
                        manifest.Remove(nameAsKey);

                        if (!manifest.Any())
                        {
                            break; // No need to keep searching.
                        }
                    }
                }
            });

            // If we didn't find the cards in the card data, there's no point
            // trying to get the multiverse and Scryfall IDs for them.
            if (!cardResults.Any() || cancellationTokenSource.IsCancellationRequested)
            {
                return cardResults;
            }

            // Now we need to retrieve the multiverse and Scryfall IDs from
            // the card identifiers file (needed for image retrieval).
            var identifierEntriesFound = 0;
            await Task.Run(() =>
            {
                using (var csvParser = new TextFieldParser(_cardIdentifiersPath))
                {
                    csvParser.SetDelimiters(new string[] { "," });

                    while (!csvParser.EndOfData)
                    {
                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        var fields = csvParser.ReadFields();
                        var uuid = fields[_cardIdentifierColumnNumbers["uuid"]];
                        var cardMatch = cardResults.FirstOrDefault((cardResult) => uuid == cardResult.Uuid);

                        if (string.IsNullOrEmpty(uuid) || cardMatch == null)
                        {
                            continue;
                        }
                        else if (cardMatch != null)
                        {
                            identifierEntriesFound++;
                        }

                        // Found identifiers entry for a card.
                        var multiverseId = fields[_cardIdentifierColumnNumbers["multiverseId"]];
                        var scryfallId = fields[_cardIdentifierColumnNumbers["scryfallId"]];

                        cardMatch.MultiverseId = multiverseId;
                        cardMatch.ScryfallId = scryfallId;

                        if (identifierEntriesFound == cardResults.Count)
                        {
                            // Found an entry for all card results, even if some of them
                            // don't have both a multiverse ID and a Scryfall ID.
                            break;
                        }
                    }
                }
            });

            if (cardResults.Any())
            {
                lock (_cards)
                {
                    foreach (var card in cardResults)
                    {
                        _cards[card.Name.ToLower()] = card;
                    }
                }
            }

            return cardResults;
        }

        public Image GetCardBack()
        {
            if (!_initialised) return null;

            return _cardBack;
        }

        /// <summary>
        /// Retrieves a card image from memory if cached, or from the web otherwise.
        /// </summary>
        /// <param name="cardName">The name of the card to retrieve the image for.</param>
        /// <param name="back">If true, retrieves image for back of card, otherwise retrieves the front of the card.</param>
        public async Task<Image> GetCardImageAsync(
            string cardName,
            CancellationTokenSource cancellationTokenSource,
            bool back = false)
        {
            var images = await GetCardImagesAsync(
                cardName,
                cancellationTokenSource);

            if (images == null) return null;

            return back ? images.Item2 : images.Item1;
        }

        /// <summary>
        /// Retrieves card images from memory if cached, or from the web otherwise.
        /// </summary>
        /// <param name="cardName">The name of the card to retrieve images for.</param>
        /// <param name="back">If true, retrieves image for back of card, otherwise retrieves the front of the card.</param>
        /// <returns>Tuple of front and back images for the card.</returns>
        public async Task<Tuple<Image, Image>> GetCardImagesAsync(
            string cardName,
            CancellationTokenSource cancellationTokenSource)
        {
            Card card;
            card = await TryGetCardAsync(cardName, cancellationTokenSource);
            if (card == null) return null;

            if (card.CardImage != null && card.BackImage != null)
            {
                return new Tuple<Image, Image>(card.CardImage, card.BackImage);
            }

            if (!card.HasDownloadableImage) return null;

            var gathererUrl = card.BuildGathererUrl();

            if (card.CardImage == null)
            {
                var scryfallFrontUrl = card.BuildScryfallUrl();
                var hasScryfallFrontUrl = !string.IsNullOrEmpty(scryfallFrontUrl);
                var image = await DownloadImageAsync(hasScryfallFrontUrl ? scryfallFrontUrl : gathererUrl,
                    cancellationTokenSource);
                card.CardImage = image;
            }

            if (card.BackImage == null)
            {
                var scryfallBackUrl = card.BuildScryfallUrl(true);
                var hasScryfallBackUrl = !string.IsNullOrEmpty(scryfallBackUrl);
                var backImage = hasScryfallBackUrl ? await DownloadImageAsync(scryfallBackUrl,
                    cancellationTokenSource) : null;
                card.BackImage = backImage ?? _cardBack;
            }

            // Should have non-null values for both front and back now.
            return new Tuple<Image, Image>(card.CardImage, card.BackImage);
        }

        private async Task<Image> DownloadImageAsync(string url, CancellationTokenSource cts)
        {
            var client = new HttpClient();

            Image image = null;
            try
            {
                var bytes = await client.GetByteArrayAsync(url).ConfigureAwait(false);

                using (var memoryStream = new MemoryStream(bytes))
                {
                    image = Image.FromStream(memoryStream);
                }
            }
            catch (HttpRequestException)
            {
                // Unable to connect but it might just be a bad URL.
                // Do nothing.
            }

            return image;
        }
    }
}
