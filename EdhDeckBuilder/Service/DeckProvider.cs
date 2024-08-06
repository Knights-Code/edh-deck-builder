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
        const decimal DECK_FILE_FORMAT_VERSION = 1.0m;

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
                    // Version information.
                    writer.WriteLine($"Deck File Format Version:,{DECK_FILE_FORMAT_VERSION}");

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

                    // Write tags associated with each role.
                    writer.Write(","); // Card quantity and card name columns aren't needed.
                    var groupings = deckModel.RoleAndTagGroupings;
                    foreach (var role in allRoles)
                    {
                        writer.Write(",");
                        var roleAndTagGrouping = groupings.FirstOrDefault((grouping) => grouping.RoleName == role);

                        if (roleAndTagGrouping == null)
                        {
                            // No tags for this role.
                            continue;
                        }

                        writer.Write($"{string.Join(";", roleAndTagGrouping.Tags)}");
                    }

                    writer.WriteLine();

                    // And now for the cards.
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

        public DeckModel LoadDeck(string deckFilePath)
        {
            DeckModel result = new DeckModel("Unloaded Deck");
            // Check version of file.
            using (var csvParser = new TextFieldParser(deckFilePath))
            {
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                var versionLine = csvParser.ReadFields();

                if (versionLine[0] != "Deck File Format Version:")
                {
                    result.FileVersion = 0m;
                }
                else
                {
                    if (decimal.TryParse(versionLine[1], out decimal version))
                    {
                        result.FileVersion = version;
                    }
                    else
                    {
                        result.FileVersion = 0m;
                    }
                }
            }

            while (result.FileVersion < DECK_FILE_FORMAT_VERSION)
            {
                result = UpgradeVersion(deckFilePath, result);

                if (result.FileVersion == DECK_FILE_FORMAT_VERSION)
                {
                    // Finished upgrading. Save deck.
                    SaveDeck(result, deckFilePath);
                }
            }

            result = LoadDeck_Version_1_0(deckFilePath);

            return result;
        }

        public DeckModel UpgradeVersion(string deckFilePath, DeckModel result)
        {
            if (result.FileVersion == 0m)
            {
                result = LoadDeck_Unversioned(deckFilePath);
                result = Migrate_Version_0_To_Version_1_0(result);
            }

            return result;
        }

        public DeckModel LoadDeck_Version_1_0(string deckFilePath)
        {
            DeckModel result = null;
            var defaultRoleSet = TemplatesAndDefaults.DefaultRoleSet();
            using (var csvParser = new TextFieldParser(deckFilePath))
            {
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Version doesn't matter at this point, just need
                // to progress the parser.
                var versionLine = csvParser.ReadFields();

                var deckLine = csvParser.ReadFields();
                var deckName = deckLine[0];
                result = new DeckModel(deckName);

                // Load custom roles, if any.
                foreach (var item in deckLine)
                {
                    if (item == deckName || string.IsNullOrEmpty(item)) continue;
                    if (defaultRoleSet.Contains(item)) continue;

                    result.CustomRoles.Add(item);
                }

                // Read role rankings, if any.
                var allRoles = defaultRoleSet;
                allRoles.AddRange(result.CustomRoles);

                // Now read tag groupings.
                var tagGroupings = csvParser.ReadFields();

                for (int i = 2; i < allRoles.Count + 2; i++)
                {
                    if (string.IsNullOrEmpty(tagGroupings[i])) continue;

                    var tagsForRole = tagGroupings[i].Split(';');
                    var newRoleAndTagGrouping = new DeckRoleTagModel
                    {
                        RoleName = allRoles[i],
                        Tags = tagsForRole.ToList()
                    };

                    result.RoleAndTagGroupings.Add(newRoleAndTagGrouping);
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

                    for (int i = 2; i < allRoles.Count + 2; i++)
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(fields[i])) continue;

                            if (!int.TryParse(fields[i], out int rankingValue))
                            {
                                throw new InvalidDataException($"Invalid role ranking value. Value needs to be a number, but was {fields[i]}.");
                            }

                            newCard.Roles.Add(new RoleModel(allRoles[i - 2], rankingValue, true));
                        }
                        catch (IndexOutOfRangeException)
                        {
                            // When a card row has no role data, the CSV parser treats it as having
                            // fewer columns than the number of roles to examine.
                            // It's safe to simply treat this as the card not having the role.
                            continue;
                        }
                    }

                    cards.Add(newCard);
                }

                result.AddCards(cards);
            }

            return result;
        }

        public DeckModel Migrate_Version_0_To_Version_1_0(DeckModel deckModel)
        {
            // Main differences with this version are:
            // - Version is now tracked
            // - Each Role has associated Scryfall Tags and Type Tags
            // - The Standalone, Enhancer, and Enabler Roles have been retired
            deckModel.FileVersion = 1.0m;
            return deckModel;
        }

        /// <summary>
        /// Loads a list of card names from deck file.
        /// The file structure expected by this function was used before
        /// the versioning system was introduced.
        /// </summary>
        /// <param name="deckFilePath">The path of the file that contains the decklist.</param>
        /// <returns>A model of the decklist.</returns>
        public DeckModel LoadDeck_Unversioned(string deckFilePath)
        {
            DeckModel result = null;
            var defaultRoleSet = TemplatesAndDefaults.DefaultRoleSet_Unversioned();
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
                    if (defaultRoleSet.Contains(item)) continue;

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
                    var allRoles = defaultRoleSet;
                    allRoles.AddRange(result.CustomRoles);

                    for (int i = 2; i < allRoles.Count + 2; i++)
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(fields[i])) continue;

                            if (!int.TryParse(fields[i], out int rankingValue))
                            {
                                throw new InvalidDataException($"Invalid role ranking value. Value needs to be a number, but was {fields[i]}.");
                            }

                            newCard.Roles.Add(new RoleModel(allRoles[i - 2], rankingValue, true));
                        }
                        catch (IndexOutOfRangeException)
                        {
                            // When a card row has no role data, the CSV parser treats it as having
                            // fewer columns than the number of roles to examine.
                            // It's safe to simply treat this as the card not having the role.
                            continue;
                        }
                    }

                    cards.Add(newCard);
                }

                result.AddCards(cards);
            }

            return result;
        }
    }
}
