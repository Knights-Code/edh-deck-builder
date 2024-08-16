using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Service
{
    /// <summary>
    /// A verbose report containing all changes made during an update
    /// to card's tags and roles by the Tag Manager.
    /// </summary>
    public class RolesAndTagsReport
    {
        private List<string> _renamedRoleEvents;
        private List<string> _roleUpdateEvents;
        private List<string> _scryfallTagUpdateEvents;

        public RolesAndTagsReport()
        {
            _renamedRoleEvents = new List<string>();
            _roleUpdateEvents = new List<string>();
            _scryfallTagUpdateEvents = new List<string>();
        }

        public void AddRenamedRoleEvent(string renamedRoleEvent)
        {
            _renamedRoleEvents.Add(renamedRoleEvent);
        }

        public void AddRoleUpdateEvent(string roleUpdateEvent)
        {
            _roleUpdateEvents.Add(roleUpdateEvent);
        }

        public void AddScryfallTagsUpdateEvent(string scryfallTagUpdateEvent)
        {
            _scryfallTagUpdateEvents.Add(scryfallTagUpdateEvent);
        }

        public string Summary()
        {
            var summary = string.Empty;

            if (!_renamedRoleEvents.Any() &&
                !_scryfallTagUpdateEvents.Any() &&
                !_roleUpdateEvents.Any())
            {
                return "All cards already up-to-date. No actions taken.";
            }

            if (_renamedRoleEvents.Count == 1)
            {
                summary += $"{_renamedRoleEvents.First()}";
            }
            else if (_renamedRoleEvents.Any())
            {
                summary += $"Renamed {_renamedRoleEvents.Count} roles.";
            }

            if (_scryfallTagUpdateEvents.Count == 1)
            {
                summary += $" {_scryfallTagUpdateEvents.First()}";
            }
            else if (_scryfallTagUpdateEvents.Any())
            {
                summary += $" Updated Scryfall tags for {_scryfallTagUpdateEvents.Count} cards.";
            }

            if (_roleUpdateEvents.Count == 1)
            {
                summary += $" {_roleUpdateEvents.First()}";
            }
            else if (_roleUpdateEvents.Any())
            {
                summary += $" Updated {_roleUpdateEvents.Count} role values across all cards.";
            }

            return summary;
        }

        public override string ToString()
        {
            var result = "\n\n// Start of Report //\n\n";

            result += string.Join("\n", _renamedRoleEvents);
            result += "\n";
            result += string.Join("\n", _scryfallTagUpdateEvents);
            result += "\n";
            result += string.Join("\n", _roleUpdateEvents);

            return result;
        }
    }
}
