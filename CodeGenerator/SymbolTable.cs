using System;
using System.Collections.Generic;

namespace AntlrCodeGenerator.CodeGenerator
{
    public class SymbolTable
    {

        //symbol table

        public Dictionary<string, string> symbolTable = new Dictionary<string, string>();

        public void AddSymbol(string name, string type)
        {
            symbolTable.Add(name, type);
        }
        public void AddSymbol(string name)
        {
            symbolTable.Add(name, "int");

        }

        public string GetSymbolType(string name)
        {
            return symbolTable[name];
        }

        public bool IsSymbol(string name)
        {
            return symbolTable.ContainsKey(name);
        }

        public void RemoveSymbol(string name)
        {
            symbolTable.Remove(name);
        }

        public  void ClearSymbolTable()
        {
            symbolTable.Clear();
        }

        public  void PrintSymbolTable()
        {
            foreach (KeyValuePair<string, string> entry in symbolTable)
            {
                Console.WriteLine("{0} : {1}", entry.Key, entry.Value);
            }
        }



    }

}