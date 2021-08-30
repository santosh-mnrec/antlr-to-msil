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
            //update if exits   
            if (symbolTable.ContainsKey(name))
            {
                symbolTable[name] = "int";
            }
            else
            {
                symbolTable.Add(name, "int");
            }



        }
        //get value
        public string GetValue(string name)
        {

            //if exits
            if (symbolTable.ContainsKey(name))
            {
                return symbolTable[name];

            }
            else
            {
                return "";
            }
        }

        public string GetSymbolType(string name)
        {
            if (symbolTable.ContainsKey(name))
                return symbolTable[name];
            return "";

        }

        public bool IsSymbol(string name)
        {
            return symbolTable.ContainsKey(name);
        }

        public void RemoveSymbol(string name)
        {
            symbolTable.Remove(name);
        }

        public void ClearSymbolTable()
        {
            symbolTable.Clear();
        }

        //update symbol type
        public void UpdateSymbolType(string name, string type)
        {
            symbolTable[name] = type;
        }

        public void PrintSymbolTable()
        {
            foreach (KeyValuePair<string, string> entry in symbolTable)
            {
                Console.WriteLine("{0} : {1}", entry.Key, entry.Value);
            }
        }



    }

}