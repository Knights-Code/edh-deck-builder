using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Model
{
    public class TemplateModel
    {
        public string Role { get; private set; }
        public int Minimum { get; private set; }
        public int Maximum { get; private set; }

        public TemplateModel(string role, int minimum, int maximum)
        {
            if (maximum < minimum) throw new ArgumentException("Maximum cannot be smaller than minimum");

            Role = role;
            Minimum = minimum;
            Maximum = maximum;
        }
    }
}
