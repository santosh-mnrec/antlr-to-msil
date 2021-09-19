using System;

namespace AntlrCodeGenerator
{
    public class SymbolTable : IComparable<SymbolTable>

    {

        public static readonly SymbolTable Void = new SymbolTable();
        public object Value;
        public string Type { get; set; }
        private SymbolTable()
        {

            Value = new object();
            Type = "int32";
        }

       

        public SymbolTable(object input)
        {
            Value = input ?? throw new Exception($"v == null");

            // only accept bool, list, number or string types
            if (Isbool() || IsNumber() || IsString() || IsNull()) return;
            var exception = new Exception("invalid data type: " + input + " (" + input.GetType() + ")");
            throw exception;
        }
        public SymbolTable(object input, string type)
        {
            Value = input ?? throw new Exception("v == null");
            Type = type;
            // only accept bool, list, number or string types
            if (Isbool() || IsNumber() || IsString() || IsNull()) return;
            var exception = new Exception("invalid data type: " + input + " (" + input.GetType() + ")");
            throw exception;
        }

        public bool Isboolean()
        {
            if (!(Value is bool boolean)) return false;
            return boolean;
        }

        public bool ToFloat()
        {
          return float.TryParse(Value.ToString(), out float result);
        }
        public float IsFloat(){
            return float.Parse(Value.ToString() ?? string.Empty);
            
        }
        public int ToInteger()
        {
            return int.Parse(Value.ToString() ?? string.Empty);
        }
        public string ToStr() => Value.ToString();
        public int CompareTo(SymbolTable that)
        {
            if (that is null)
            {
                throw new ArgumentNullException(nameof(that));
            }

            if (IsNumber() && that.IsNumber())
            {
                return Equals(that) ? 0 : ToFloat().CompareTo(that.ToFloat());
            }

            var exception = new Exception("illegal expression: can't compare `" + this + "` to `" + that + "`");
            if (IsString() && that.IsString())
            {
                return string.Compare(ToStr(), that.ToStr(), StringComparison.Ordinal);
            }

            throw exception;
        }
        public override bool Equals(object o)
        {
            if (this == Void || o == Void)
            {
                _ = new Exception("can't use VOID: " + this + " ==/!= " + o);
            }
            if (this == o)
            {
                return true;
            }
            if (o == null || GetType() != o.GetType())
            {
                return false;
            }
            var that = (SymbolTable)o;
            if (!IsNumber() || !that.IsNumber()) return Value.Equals(that.Value);
            var result = ToFloat() == that.ToFloat();
            return result;

        }



        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        private bool Isbool()
        {
            return bool.TryParse(Value.ToString(), out bool result);
        }

        public bool IsNumber()
        {
            //can convert to int32
            return int.TryParse(Value.ToString(), out var result);


        }


        private bool IsNull()
        {
            return Equals(this, Void);
        }

        private bool IsVoid()
        {
            return Equals(this, Void);
        }

        public bool IsString()
        {
            return Value.ToString() != null;
        }



        public override string ToString()
        {
            var isVoid = IsVoid();
            return IsNull() ? "NULL" : isVoid ? "Void" : Value.ToString();
        }
    }
}