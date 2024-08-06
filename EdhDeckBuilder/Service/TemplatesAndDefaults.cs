using EdhDeckBuilder.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Service
{
    public static class TemplatesAndDefaults
    {
        public static List<string> DefaultRoleSet_Unversioned()
        {
            var result = new List<string>
            {
                "Ramp",
                "Draw",
                "Removal",
                "Wipe",
                "Land",
                "Standalone",
                "Enhancer",
                "Enabler",
                "Tapland"
            };

            return result;
        }

        public static List<string> DefaultRoleSet()
        {
            var result = new List<string>
            {
                "Ramp",
                "Draw",
                "Removal",
                "Wipe",
                "Land",
                "Tapland"
            };

            return result;
        }

        public static List<TemplateModel> DefaultTemplates()
        {
            var result = new List<TemplateModel>();

            result.Add(new TemplateModel("Ramp", 10, 12));
            result.Add(new TemplateModel("Draw", 10, 100));
            result.Add(new TemplateModel("Removal", 10, 12));
            result.Add(new TemplateModel("Wipe", 2, 2));
            result.Add(new TemplateModel("Land", 35, 38));
            result.Add(new TemplateModel("Tapland", 0, 4));

            return result;
        }

        public static List<string> DefaultTagsForRole(string role)
        {
            var result = new List<string>();

            switch (role)
            {
                case "Ramp":
                    result.Add("ramp");
                    break;
                case "Draw":
                    result.Add("card advantage");
                    break;
                case "Removal":
                    result.Add("removal");
                    break;
                case "Wipe":
                    result.Add("sweeper");
                    break;
                case "Land":
                    result.Add("type:Land");
                    break;
                case "Tapland":
                    result.Add("tapland");
                    break;
            }

            return result;
        }
    }
}
