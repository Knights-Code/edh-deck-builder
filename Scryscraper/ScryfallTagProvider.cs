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
        private IScryfallService _scryfallService;

        private async Task<List<string>> RetrieveTagsFromUrlAsync(
            string url,
            IPage page,
            NavigationOptions navigationOptions)
        {
            var result = new List<string>();

            try
            {
                await page.GoToAsync(url, navigationOptions);
                
                var cardTagSelector = "//h2[text() = ' Card']/..//div[@class='tag-row']//div" +
                    "[contains(concat(\" \", normalize-space(@class), \" \"), \" value-card \")]/..//a";
                var ancestorTagSelector = "//h2[text() = ' Card']/..//div[@class='tagging-ancestors']//a";
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
            catch (Exception e)
            {
                throw e;
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
                    var tagsForCard = await RetrieveTagsFromUrlAsync(cards[cardName], page, navigationOptions);
                    result[cardName] = tagsForCard;
                }
            }

            return result;
        }
    }
}
