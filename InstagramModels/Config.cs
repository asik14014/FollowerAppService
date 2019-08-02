using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramModels
{
    public class Config
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public int IntValue
        {
            get
            {
                var value = 0;
                int.TryParse(Value, out value);
                return value;
            }
        }

        public Config()
        {
        }

        public Config(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
