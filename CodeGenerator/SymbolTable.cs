using System;
using System.Collections.Generic;

namespace AntlrCodeGenerator.CodeGenerator
{
    public class SymbolTable
    {

        //symbol table

        public static Dictionary<string, string> symbolTable = new Dictionary<string, string>();

        public static void addSymbol(string name, string type)
        {
            symbolTable.Add(name, type);
        }

        public static string getSymbolType(string name)
        {
            return symbolTable[name];
        }

        public static bool isSymbol(string name)
        {
            return symbolTable.ContainsKey(name);
        }

        public static void removeSymbol(string name)
        {
            symbolTable.Remove(name);
        }

        public static void clearSymbolTable()
        {
            symbolTable.Clear();
        }

        public static void printSymbolTable()
        {
            foreach (KeyValuePair<string, string> entry in symbolTable)
            {
                Console.WriteLine("{0} : {1}", entry.Key, entry.Value);
            }
        }



    }

}