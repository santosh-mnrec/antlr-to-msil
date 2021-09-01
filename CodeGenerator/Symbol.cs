namespace AntlrCodeGenerator.CodeGenerator
{
    public class Symbol
    {
       public string Type { get; set; }
       public string Name { get; set; }
       public string Value { get; set; }
       //constructor
       public Symbol(string type, string name, string value)
       {
           Type = type;
           Name = name;
           Value = value;
       }

    }
}