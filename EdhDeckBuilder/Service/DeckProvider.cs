using EdhDeckBuilder.Model;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualBasic.FileIO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
                try
                {
                    File.Delete(deckFilePath);
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Unable to overwrite existing file.\n\n{e.Message}");
                }
            }

            try
            {
                using (var writer = new StreamWriter(new FileStream(deckFilePath, FileMode.OpenOrCreate)))
                {
                    writer.Write($"{deckModel.Name},,"); // Need extra comma for card name column.

                    var defaultRoles = TemplatesAndDefaults.DefaultRoleSet();
                    writer.Write(string.Join(",", defaultRoles.Select(role => role.CsvFormat())));

                    if (deckModel.CustomRoles.Any())
                    {
                        writer.Write(",");
                        writer.Write(string.Join(",", deckModel.CustomRoles.Select(role => role.CsvFormat())));
                    }

                    writer.WriteLine();

                    var allRoles = new List<string>();
                    allRoles.AddRange(defaultRoles);
                    allRoles.AddRange(deckModel.CustomRoles);

                    foreach (var cardModel in deckModel.Cards)
                    {
                        if (cardModel.NumCopies == 0) continue;

                        writer.Write($"{cardModel.NumCopies},{cardModel.Name.CsvFormat()}");

                        foreach (var role in allRoles)
                        {
                            writer.Write(",");
                            var roleModel = cardModel.Roles.FirstOrDefault(r => r.Name == role);

                            if (roleModel == null || !roleModel.Applies) continue;

                            writer.Write($"{roleModel.Value}");
                        }

                        writer.WriteLine();
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Unable to save deck.\n\n{e.Message}");
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
                    if (item == deckName || string.IsNullOrEmpty(item)) continue;
                    if (TemplatesAndDefaults.DefaultRoleSet().Contains(item)) continue;

                    result.CustomRoles.Add(item);
                }

                // Now read cards.
                var cards = new List<CardModel>();

                while (!csvParser.EndOfData)
                {
                    var fields = csvParser.ReadFields();
                    var numCopies = fields[0];
                    var name = fields[1];

                    if (!int.TryParse(numCopies, out int intNumCopies)) throw new InvalidDataException($"Invalid value for number of copies for card named {name}. Value needs to be a number, but was {fields[0]}.");

                    var newCard = new CardModel { Name = name, NumCopies = intNumCopies };

                    // Read role rankings, if any.
                    var allRoles = TemplatesAndDefaults.DefaultRoleSet();
                    allRoles.AddRange(result.CustomRoles);

                    for (int i = 2; i < allRoles.Count + 2; i++)
                    {
                        if (string.IsNullOrEmpty(fields[i])) continue;

                        if (!int.TryParse(fields[i], out int rankingValue))
                        {
                            throw new InvalidDataException($"Invalid role ranking value. Value needs to be a number, but was {fields[i]}.");
                        }

                        newCard.Roles.Add(new RoleModel(allRoles[i - 2], rankingValue, true));
                    }

                    cards.Add(newCard);
                }

                result.AddCards(cards);
            }

            return result;
        }
    }
}
