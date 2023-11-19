using EdhDeckBuilder.Model;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Service
{
    public class RoleProvider
    {
        private Dictionary<string, List<RoleModel>> _rolesByCardName;
        private List<string> _roleHeaders;
        private bool _initialised = false;

        public RoleProvider()
        {
            _rolesByCardName = new Dictionary<string, List<RoleModel>>();
            _roleHeaders = new List<string>();
        }

        public void Initialise(string rolesPath = "")
        {
            if (string.IsNullOrEmpty(rolesPath))
            {
                rolesPath = SettingsProvider.RolesFilePath();

                if (string.IsNullOrEmpty(rolesPath))
                {
                    return;
                }
            }

            using (var csvParser = new TextFieldParser(rolesPath))
            {
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Get role names.
                var headers = csvParser.ReadFields();

                foreach (var roleHeader in headers)
                {
                    _roleHeaders.Add(roleHeader);
                }

                while (!csvParser.EndOfData)
                {
                    var fields = csvParser.ReadFields();
                    var cardName = fields[0];

                    if (_rolesByCardName.ContainsKey(cardName))
                    {
                        continue; // Ignore dupes. But also, need way to signal that file needs tidying.
                    }

                    var roles = new List<RoleModel>();

                    for (var i = 1; i < fields.Length; i++)
                    {
                        // If there's any string value in the cell, the role applies
                        // to the card.
                        if (string.IsNullOrEmpty(fields[i]))
                        {
                            continue;
                        }

                        roles.Add(new RoleModel(_roleHeaders[i]));
                    }

                    if (roles.Any()) _rolesByCardName[cardName] = roles;
                }
            }

            _initialised = true;
        }


        public List<RoleModel> GetRolesForCard(string cardName)
        {
            if (!_initialised) Initialise();

            if (!_rolesByCardName.ContainsKey(cardName)) return null;

            if (!_rolesByCardName[cardName].Any()) return null;

            return _rolesByCardName[cardName];
        }

        public void SaveRoles(DeckModel deckModel, string rolesFilePath)
        {
            // Update roles map with deck's.
            foreach (var card in deckModel.Cards)
            {
                _rolesByCardName[card.Name] = card.Roles;
            }

            // Write the roles to the file.
            using (var writer = new StreamWriter(new FileStream(rolesFilePath, FileMode.OpenOrCreate)))
            {
                writer.WriteLine(string.Join(",", _roleHeaders));

                var cards = _rolesByCardName.Keys;

                foreach (var cardName in cards)
                {
                    writer.Write(cardName.CsvFormat());
                    var roles = new List<RoleModel>(_rolesByCardName[cardName]);

                    if (!roles.Any())
                    {
                        // Move to next line.
                        writer.WriteLine();
                        continue;
                    }

                    // NOTE: Custom roles are not supported.
                    foreach (var roleHeader in _roleHeaders)
                    {
                        if (string.IsNullOrEmpty(roleHeader)) continue;

                        if (!roles.Any()) continue;

                        var roleModel = roles.FirstOrDefault(rm => rm.Name == roleHeader && rm.Applies);

                        if (roleModel != null)
                        {
                            writer.Write(",1");
                            roles.Remove(roleModel);
                        }
                        else
                        {
                            writer.Write(",");
                        }
                    }

                    writer.WriteLine();
                }
            }
        }
    }
}
