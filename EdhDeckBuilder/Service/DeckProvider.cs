using EdhDeckBuilder.Model;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualBasic.FileIO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Service
{
    /// <summary>
    /// Saves and loads decklists to and from disk.
    /// </summary>
    public class DeckProvider
    {
        /// <summary>
        /// Saves decklist to disk.
        /// </summary>
        /// <param name="deckModel">The Deck to save to disk.</param>
        /// <param name="deckFilePath">The path of the file to which to write the decklist.</param>
        public void SaveDeck(DeckModel deckModel, string deckFilePath)
        {
            if (File.Exists(deckFilePath))
            {
                File.Delete(deckFilePath);
            }

            using (var writer = new StreamWriter(new FileStream(deckFilePath, FileMode.OpenOrCreate)))
            {
                writer.Write($"{deckModel.Name},");
                writer.Write(string.Join(",", TemplatesAndDefaults.DefaultRoleSet().Select(role => role.CsvFormat())));
                writer.WriteLine(string.Join(",", deckModel.CustomRoles.Select(role => role.CsvFormat())));

                foreach (var cardModel in deckModel.Cards)
                {
                    writer.WriteLine($"{cardModel.NumCopies},{cardModel.Name.CsvFormat()}");
                }
            }
        }

        /// <summary>
        /// Loads a list of card names from deck file.
        /// </summary>
        /// <param name="deckFilePath">The path of the file that contains the decklist.</param>
        /// <returns>A model of the decklist.</returns>
        public DeckModel LoadDeck(string deckFilePath)
        {
            DeckModel result = null;
            using (var csvParser = new TextFieldParser(deckFilePath))
            {
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                var deckLine = csvParser.ReadFields();
                var deckName = deckLine[0];
                result = new DeckModel(deckName);

                // Load custom roles, if any.
                foreach (var item in deckLine)
                {
                    if (item == deckName) continue;
                    if (TemplatesAndDefaults.DefaultRoleSet().Contains(item)) continue;

                    result.CustomRoles.Add(item);
                }

                var cards = new List<CardModel>();

                while (!csvParser.EndOfData)
                {
                    var fields = csvParser.ReadFields();
                    var numCopies = fields[0];
                    var name = fields[1];

                    if (!int.TryParse(numCopies, out int intNumCopies)) throw new InvalidDataException();

                    cards.Add(new CardModel { Name = name, NumCopies = intNumCopies });
                }

                result.AddCards(cards);
            }

            return result;
        }
    }
}
