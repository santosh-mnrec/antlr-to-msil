using System;

namespace AntlrCodeGenerator
{
    public class Variable : IComparable<Variable>

    {

        public static readonly Variable VOID = new Variable();
        public object value;
        public string Type { get; set; }
        private Variable( )
        {

            value = new object();
            Type = "int32";
        }



        public Variable(object input)
        {
            value = input ?? throw new Exception($"v == null");

            // only accept bool, list, number or string types
            if (Isbool() || IsNumber() || IsString() || IsNull()) return;
            var exception = new Exception("invalid data type: " + input + " (" + input.GetType() + ")");
            throw exception;
        }
        public Variable(object input, string type)
        {
            value = input ?? throw new Exception("v == null");
            Type = type;
            // only accept bool, list, number or string types
            if (Isbool() || IsNumber() || IsString() || IsNull()) return;
            var exception = new Exception("invalid data type: " + input + " (" + input.GetType() + ")");
            throw exception;
        }

        public bool Isboolean( )
        {
            if (!(value is bool boolean)) return false;
            return boolean;
        }

        public bool ToFloat( )
        {
            return float.TryParse(value.ToString(), out float result);
        }
        public float IsFloat( )
        {
            return float.Parse(value.ToString() ?? string.Empty);

        }
        public int ToInteger( )
        {
            return int.Parse(value.ToString() ?? string.Empty);
        }
        public string ToStr( ) => value.ToString();
        public int CompareTo(Variable that)
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
            if (this == VOID || o == VOID)
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
            var that = (Variable)o;
            if (!IsNumber() || !that.IsNumber()) return value.Equals(that.value);
            var result = ToFloat() == that.ToFloat();
            return result;

        }



        public override int GetHashCode( )
        {
            return value.GetHashCode();
        }

        private bool Isbool( )
        {
            return bool.TryParse(value.ToString(), out bool result);
        }

        public bool IsNumber( )
        {
            //can convert to int32
            return int.TryParse(value.ToString(), out var result);


        }


        private bool IsNull( )
        {
            return Equals(this, VOID);
        }

        private bool IsVoid( )
        {
            return Equals(this, VOID);
        }

        public bool IsString( )
        {
            return value.ToString() != null;
        }



        //Override ToString() to return the value of the variable
        public override string ToString( )
        {
            return value.ToString();
        }
        
    }
}