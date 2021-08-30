using System.Collections.Generic;

namespace AntlrCodeGenerator
{
    public class Scope
    {

        private Scope _parent;
        public Dictionary<string, Value> _variables { get; set; }
        public string ScopeName { get; set; }

        public bool IsScope(string name)
        {
            return name.Equals(ScopeName);
        }


        public Scope() : this(null, "global") { }

        public void assignParam(string var, Value value)
        {
            _variables.TryAdd(var, value);
        }
        public Scope(Scope p, string scopeName)
        {
            _parent = p;
            _variables = new Dictionary<string, Value>();
            ScopeName = scopeName;

        }

        public void Assign(string var, Value @value)
        {
            if (Resolve(@var, !IsScope(ScopeName)) != null)
            {
                // There is already such a variable, re-assign it
                this.ReAssign(@var, @value);
            }
            else
            {
                // A newly declared variable
                _variables.TryAdd(var, value);
            }
        }

        private bool IsGlobalScope()
        {
            return _parent == null;
        }

        public Scope Parent
        {
            get
            {
                return _parent;
            }
        }

        private void ReAssign(string identifier, Value value)
        {
            if (_variables.ContainsKey(identifier))
            {
                // The variable is declared in this scope
                _variables.TryAdd(identifier, value);
            }
            else if (_parent != null)
            {
                // The variable was not declared in this scope, so let
                // the parent scope re-assign it
                _parent.ReAssign(identifier, value);
            }
        }

        public Value Resolve(string var)
        {
            return Resolve(var, true);
        }

        private Value Resolve(string var, bool checkParent)
        {
            _variables.TryGetValue(@var, out var value);

            if (value != null)
            {
                // The variable resides in this scope
                return value;
            }
            else if (checkParent && !IsGlobalScope())
            {
                // Let the parent scope look for the variable
                return _parent.Resolve(var, !_parent.IsScope(ScopeName));
            }
            else
            {
                // Unknown variable
                return null;
            }
        }
    }


}