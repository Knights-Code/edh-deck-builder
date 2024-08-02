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
                await page.GoToAsync("https://tagger.scryfall.com/card/otc/267", navigationOptions);

                Console.WriteLine("Getting content...");
                var content = await page.GetContentAsync();

                Console.WriteLine(content);
                Console.WriteLine($"\n{(content.Contains("ramp") ? "Ramp detected" : "Nothing found")}");
            }

            Console.ReadKey();
        }
    }
}
