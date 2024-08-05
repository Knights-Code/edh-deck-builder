using EdhDeckBuilder.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Service
{
    public class Role
    {
        public string Name { get; private set; }
        public int Value { get; private set; }
        public bool Applies { get; private set; }

        public Role(RoleModel model)
        {
            Name = model.Name;
            Value = model.Value;
            Applies = model.Applies;
        }

        public RoleModel ToModel()
        {
            return new RoleModel(Name, Value, Applies);
        }
    }
}
