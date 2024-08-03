using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scryscraper
{
    public class ScryfallTagProvider
    {
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
                    result.Add("Scryfall Tagger website reported URL not found.");
                    return result;
                }

                var cardTags = await page.XPathAsync(cardTagSelector);
                var ancestorTags = await page.XPathAsync(ancestorTagSelector);

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
                    result.Add($"Unable to retrieve tags. Card name was null or blank." +
                        $"URL was {(!string.IsNullOrEmpty(url) ? url : "null")}.");
                }
                else
                {
                    result.Add($"Unable to retrieve tags for {cardName}. URL was {(!string.IsNullOrEmpty(url) ? url : "null")}.");
                }
            }

            return result;
        }

        public async Task<Dictionary<string, List<string>>> GetScryfallTagsAsync(
            Dictionary<string, string> cards)
        {
            var result = new Dictionary<string, List<string>>();

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

                foreach (var cardName in cards.Keys)
                {
                    var tagsForCard = await RetrieveTagsFromUrlAsync(
                        cards[cardName],
                        page,
                        navigationOptions,
                        cardName);
                    result[cardName] = tagsForCard;
                }
            }

            return result;
        }
    }
}
