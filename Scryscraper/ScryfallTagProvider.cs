using Microsoft.VisualBasic.FileIO;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scryscraper
{
    public class ScryfallTagProvider
    {
        private readonly Dictionary<string, List<string>> _tagsByCardName = new Dictionary<string, List<string>>();
        private bool _initialised = false;
        private string _scryfallTagsFilePath;

        public ScryfallTagProvider(string scryfallTagsFilePath)
        {
            _scryfallTagsFilePath = scryfallTagsFilePath;
        }

        private async Task Initialise()
        {
            if (_initialised) return;

            if (string.IsNullOrEmpty(_scryfallTagsFilePath)) return;

            await Task.Run(() =>
            {
                using (var csvParser = new TextFieldParser(_scryfallTagsFilePath))
                {
                    csvParser.SetDelimiters(new string[] { "," });

                    while (!csvParser.EndOfData)
                    {
                        var fields = csvParser.ReadFields();
                        var cardName = fields[0];

                        lock (_tagsByCardName)
                        {
                            if (_tagsByCardName.ContainsKey(cardName))
                            {
                                continue;
                            }
                        }

                        var tags = new List<string>();

                        for (var i = 1; i < fields.Length; i++)
                        {
                            if (string.IsNullOrEmpty(fields[i]))
                            {
                                continue;
                            }

                            tags.Add(fields[i]);
                        }

                        if (tags.Any())
                        {
                            lock (_tagsByCardName) _tagsByCardName[cardName] = tags;
                        }
                    }
                }
            });

            _initialised = true;
        }

        private async Task<List<string>> RetrieveTagsFromUrlAsync(
            string url,
            IPage page,
            NavigationOptions navigationOptions,
            string cardName = "")
        {
            var result = new List<string>();

            try
            {
                await page.GoToAsync(url, navigationOptions);

                var notFoundSelector = "//h1[text() ='Lost in the wild']";
                var cardTagSelector = "//h2[text() = ' Card']/..//div[@class='tag-row']//div" +
                    "[contains(concat(\" \", normalize-space(@class), \" \"), \" value-card \")]/..//a";
                var ancestorTagSelector = "//h2[text() = ' Card']/..//div[@class='tagging-ancestors']//a";
                var notFoundTags = await page.XPathAsync(notFoundSelector);

                if (notFoundTags.Count() > 0)
                {
                    result.Add($"ScryfallTagProviderError: Unable to retrieve tags for {cardName}. " +
                        $"Scryfall Tagger website reported URL not found.");
                    return result;
                }

                var cardTags = await page.XPathAsync(cardTagSelector);
                var ancestorTags = await page.XPathAsync(ancestorTagSelector);

                if (!cardTags.Any() && !ancestorTags.Any())
                {
                    result.Add($"ScryfallTagProviderError: Unable to retrieve tags for { cardName}. " +
                        $"Page loaded but XPath was unable to locate tags. " +
                        $"URL was {url}");
                }

                foreach (var cardTag in cardTags)
                {
                    var innerText = await (await cardTag.GetPropertyAsync("innerText")).JsonValueAsync<string>();
                    result.Add(innerText);
                }

                foreach (var ancestorTag in ancestorTags)
                {
                    var innerText = await (await ancestorTag.GetPropertyAsync("innerText")).JsonValueAsync<string>();
                    result.Add(innerText);
                }
            }
            catch
            {
                if (string.IsNullOrEmpty(cardName))
                {
                    result.Add($"ScryfallTagProviderError: Unable to retrieve tags. Card name was null or blank." +
                        $"URL was {(!string.IsNullOrEmpty(url) ? url : "null")}.");
                }
                else
                {
                    result.Add($"ScryfallTagProviderError: Unable to retrieve tags for {cardName}. URL was {(!string.IsNullOrEmpty(url) ? url : "null")}.");
                }
            }

            return result;
        }

        public async Task<Tuple<Dictionary<string, List<string>>, List<string>>> GetScryfallTagsAsync(
            Dictionary<string, string> cards)
        {
            if (!_initialised) await Initialise();

            var result = new Dictionary<string, List<string>>();
            var errors = new List<string>();
            var manifest = cards.Keys.ToList();

            // Check tags dictionary first.
            var cardsWithTagsInMemory = new Stack<string>();
            foreach (var manifestCardName in manifest)
            {
                lock (_tagsByCardName)
                {
                    if (_tagsByCardName.ContainsKey(manifestCardName))
                    {
                        result[manifestCardName] = _tagsByCardName[manifestCardName];
                        cardsWithTagsInMemory.Push(manifestCardName);
                    }
                }
            }

            // Update manifest.
            while (cardsWithTagsInMemory.Any())
            {
                manifest.Remove(cardsWithTagsInMemory.Pop());
            }

            if (!manifest.Any())
            {
                return new Tuple<Dictionary<string, List<string>>, List<string>>(
                    result, errors);
            }

            await new BrowserFetcher().DownloadAsync();
            using (var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true
            }))
            {
                var page = await browser.NewPageAsync();

                var navigationOptions = new NavigationOptions
                {
                    Timeout = 0,
                    WaitUntil = new[]
                    {
                        WaitUntilNavigation.Networkidle2
                    }
                };

                foreach (var cardName in manifest)
                {
                    var tagsForCard = await RetrieveTagsFromUrlAsync(
                        cards[cardName],
                        page,
                        navigationOptions,
                        cardName);

                    if (tagsForCard.Count == 1 && tagsForCard.First().StartsWith("ScryfallTagProviderError: "))
                    {
                        // Couldn't scrape card tags for some reason.
                        errors.Add(tagsForCard.First());
                        continue;
                    }

                    result[cardName] = tagsForCard;

                    lock (_tagsByCardName)
                    {
                        _tagsByCardName[cardName] = tagsForCard;
                    }
                }
            }

            return new Tuple<Dictionary<string, List<string>>, List<string>>(
                result, errors);
        }

        public async Task SaveScryfallTagsAsync()
        {
            if (File.Exists(_scryfallTagsFilePath))
            {
                File.Delete(_scryfallTagsFilePath);
            }

            using (var writer = new StreamWriter(
                new FileStream(_scryfallTagsFilePath, FileMode.OpenOrCreate)))
            {
                await Task.Run(() =>
                {
                    lock (_tagsByCardName)
                    {
                        var alphabetisedList = _tagsByCardName.Keys.OrderBy((key) => key).ToList();
                        foreach (var cardName in alphabetisedList)
                        {
                            var tags = _tagsByCardName[cardName];

                            if (!tags.Any()) continue;

                            writer.WriteLine($"{CsvFormat(cardName)},{string.Join(",", tags)}");
                        }
                    }
                });
            }
        }

        private string CsvFormat(string originalString)
        {
            if (!originalString.Contains(',')) return originalString;

            return $"\"{originalString}\"";
        }
    }
}
