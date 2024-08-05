using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Service
{
    public static class SettingsProvider
    {
        public static string DeckFilePath()
        {
            return Properties.Settings.Default.DeckFilePath;
        }

        public static void UpdateDeckFilePath(string newPath)
        {
            Properties.Settings.Default.DeckFilePath = newPath;
            Properties.Settings.Default.Save();
        }

        public static string RolesFilePath()
        {
            return Properties.Settings.Default.RolesFilePath;
        }

        public static void UpdateRolesFilePath(string newPath)
        {
            Properties.Settings.Default.RolesFilePath = newPath;
            Properties.Settings.Default.Save();
        }

        public static string ScryfallTagsFilePath()
        {
            return Properties.Settings.Default.ScryfallTagsFilePath;
        }

        public static void UpdateScryfallTagsFilePath(string newPath)
        {
            Properties.Settings.Default.ScryfallTagsFilePath = newPath;
            Properties.Settings.Default.Save();
        }
    }
}
