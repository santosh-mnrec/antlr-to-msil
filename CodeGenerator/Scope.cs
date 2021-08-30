using System.Collections.Generic;

namespace AntlrCodeGenerator
{
    public class Scope
    {

        private Scope _parent;
        public Dictionary<string, Value> Variables { get; set; }

        private bool _isFunction;
        public bool IsFunction
        {
            get
            {
                return _isFunction;
            }
        }

        public Scope() : this(null, false) { }

        public void assignParam(string var, Value value)
        {
            Variables.TryAdd(var, value);
        }
        public Scope(Scope p, bool function)
        {
            _parent = p;
            Variables = new Dictionary<string, Value>();
            _isFunction = function;
        }

        public void Assign(string var, Value @value)
        {
            if (Resolve(@var, !_isFunction) != null)
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
            if (Variables.ContainsKey(identifier))
            {
                // The variable is declared in this scope
                Variables.TryAdd(identifier, value);
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
            Variables.TryGetValue(@var, out var value);

            if (value != null)
            {
                // The variable resides in this scope
                return value;
            }
            else if (checkParent && !IsGlobalScope())
            {
                // Let the parent scope look for the variable
                return _parent.Resolve(var, !_parent._isFunction);
            }
            else
            {
                // Unknown variable
                return null;
            }
        }
    }


}