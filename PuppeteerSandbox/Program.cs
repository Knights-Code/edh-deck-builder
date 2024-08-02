using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace PuppeteerSandbox
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Fetching browser...");
            await new BrowserFetcher().DownloadAsync();
            using (var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true
                }))
            {
                Console.WriteLine("Creating page...");
                var page = await browser.NewPageAsync();

                Console.WriteLine("Navigating to website...");
                var navigationOptions = new NavigationOptions
                {
                    Timeout = 0,
                    WaitUntil = new[]
                    {
                        WaitUntilNavigation.Networkidle2
                    }
                };

                Console.WriteLine("Navigating to page...");
                await page.GoToAsync("https://tagger.scryfall.com/card/otc/267", navigationOptions);

                Console.WriteLine("Retrieving tags...");
                var cardTagSelector = "//h2[text() = ' Card']/..//div[@class='tag-row']//div" +
                    "[contains(concat(\" \", normalize-space(@class), \" \"), \" value-card \")]/..//a";
                var ancestorTagSelector = "//h2[text() = ' Card']/..//div[@class='tagging-ancestors']//a";
                var cardTags = await page.XPathAsync(cardTagSelector);
                var ancestorTags = await page.XPathAsync(ancestorTagSelector);

                Console.WriteLine($"{cardTags.Count()} card tag(s) found.");
                
                foreach (var cardTag in cardTags)
                {
                    var innerText = await (await cardTag.GetPropertyAsync("innerText")).JsonValueAsync<string>();
                    Console.WriteLine(innerText);
                }

                Console.WriteLine($"{ancestorTags.Count()} ancestor tag(s) found.");
                foreach (var ancestorTag in ancestorTags)
                {
                    var innerText = await (await ancestorTag.GetPropertyAsync("innerText")).JsonValueAsync<string>();
                    Console.WriteLine(innerText);
                }
            }

            Console.ReadKey();
        }
    }
}
