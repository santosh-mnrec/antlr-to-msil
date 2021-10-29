using System.Collections.Generic;
using System.Text;
using BLanguageMSILGenerator.CodeGenerator;

namespace BLanguageMSILGenerator
{
    public class Scope
    {

        public Scope Parent { get; set; }
        public Dictionary<string, Variable> Variables { get; set; }
        public Dictionary<string, Variable> LocalVariables { get; set; } = new Dictionary<string, Variable>();
        public int ArgCount { get; set; }
        public string ScopeName { get; set; }
       
        public string ReturnType { get; set; }

        public bool IsScope(string name)
        {
            return name.Equals(ScopeName);
        }


        public Scope() : this(null, "global") { }

        public void AssignParameter(string var, Variable value)
        {
            if (Variables.TryGetValue(var, out Variable v))
            {
                var oldValue = Variables[var];
                //update the value of the variable
                Variables[var].value = value.value;
            }
            else
            {

                Variables.TryAdd(var, value);
            }

        }

        public Scope(Scope p, string scopeName)
        {
            Parent = p;
            Variables = new Dictionary<string, Variable>();
            ScopeName = scopeName;

        }
        //is variable locally defined
        public bool IsScopeDefined(string name)
        {
            return Variables.ContainsKey(name);
        }
        public void Assign(string var, Variable @value)
        {
            if (Resolve(@var, !IsScope(ScopeName)) != null)
            {
                // There is already such a variable, re-assign it
                this.ReAssign(@var, @value);
            }
            else
            {
                // A newly declared variable
                Variables.TryAdd(var, value);
            }
        }

        public bool IsGlobalScope()
        {
            return Parent == null;
        }



        private void ReAssign(string identifier, Variable value)
        {
            if (Variables.ContainsKey(identifier))
            {
                // The variable is declared in this scope
                Variables.TryAddOrUpdate(identifier, value);
            }
            else if (Parent != null)
            {
                // The variable was not declared in this scope, so let
                // the parent scope re-assign it
                Parent.ReAssign(identifier, value);
            }
        }

        public Variable Resolve(string var)
        {
            return Resolve(var, true);
        }

        private Variable Resolve(string var, bool checkParent)
        {
            Variables.TryGetValue(@var, out var value);

            if (value != null)
            {
                // The variable resides in this scope
                return value;
            }
            else if (checkParent && !IsGlobalScope())
            {
                // Let the parent scope look for the variable
                return Parent.Resolve(var, !Parent.IsScope(ScopeName));
            }
            else
            {
                // Unknown variable
                return null;
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in Variables)
            {
                sb.Append(item.Key).Append("->").Append(item.Value).Append(",");
            }
            return sb.ToString();
        }
    }


}