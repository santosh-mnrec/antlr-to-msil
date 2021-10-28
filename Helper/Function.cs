using System.Collections.Generic;

namespace BLanguageMSILGenerator
{
    public class Function
    {
        public string Name { get; set; }

        public List<Variable> Arguments { get; set; } = new List<Variable>();

    }
}