using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Model
{
    public class RoleModel
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public bool Applies { get; set; }

        public RoleModel(string name, int value = 1, bool applies = true)
        {
            if (value <= 0) throw new ArgumentException("Value must be greater than 0");
            Name = name;
            Value = value;
            Applies = applies;
        }

        public RoleModel() { }
    }
}
