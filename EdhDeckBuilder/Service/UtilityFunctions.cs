using EdhDeckBuilder.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Service
{
    public static class UtilityFunctions
    {
        public static string CsvFormat(this string value)
        {
            return value.Contains(',') ? $"\"{value}\"" : value;
        }

        public static List<CardModel> ParseCardsFromText(string text)
        {
            var result = new List<CardModel>();

            using (var reader = new StreamReader(GenerateStreamFromString(text)))
            {
                var numberAndNamePattern = new Regex("(\\d+)\\s+(.+)");

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var match = numberAndNamePattern.Match(line);

                    if (!match.Success) continue;

                    if (int.TryParse(match.Groups[1].Value, out int numCopies))
                    {
                        var name = match.Groups[2].Value;

                        result.Add(new CardModel { Name = name, NumCopies = numCopies });
                    }
                }
            }

            return result;
        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static string CardsToClipboardFormat(List<CardModel> cards)
        {
            return string.Join(Environment.NewLine, cards.Where(c => c.NumCopies > 0).Select(c => c.ClipboardFormat()));
        }
    }
}
