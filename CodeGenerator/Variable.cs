using System;

namespace AntlrCodeGenerator
{
    public class Value : IComparable<Value>

    {
        private const string @void = "VOID";
        public static readonly Value VOID = new Value();


        public  object value;
        public string Type { get; set; }

        public Value()
        {

            value = new object();
            Type = "int32";
        }
        public Value(object input)
        {
            value = input ?? throw new Exception("v == null");

            // only accept bool, list, number or string types
            if (!(Isbool() || IsNumber() || IsString() || IsNull()))
            {
                var exception = new Exception("invalid data type: " + input + " (" + input.GetType() + ")");
                throw exception;
            }
        }
        public Value(object input, string type)
        {
            value = input ?? throw new Exception("v == null");
            Type = type;
            // only accept bool, list, number or string types
            if (!(Isbool() || IsNumber() || IsString() || IsNull()))
            {
                var exception = new Exception("invalid data type: " + input + " (" + input.GetType() + ")");
                throw exception;
            }
        }

        public bool Asbool()
        {
            var result = false;
            if (value is bool boolean)
            {
                result = boolean;
                //Do something
            }
            else
            {
                //It's not a bool
            }
            return result;
        }

        public bool ToFloat()
        {
          return float.TryParse(value.ToString(), out float result);
        }
        public float IsFloat(){
            return float.Parse(value.ToString());
            
        }
        public int ToInteger()
        {
            return int.Parse(value.ToString());
        }



        public string ToStr() => value.ToString();



        public int CompareTo(Value that)
        {
            if (that is null)
            {
                throw new ArgumentNullException(nameof(that));
            }

            if (IsNumber() && that.IsNumber())
            {
                if (Equals(that))
                {
                    return 0;
                }
                else
                {
                    return ToFloat().CompareTo(that.ToFloat());
                }
            }
            else if (IsString() && that.IsString())
            {
                return ToStr().CompareTo(that.ToStr());
            }
            else
            {
                Exception exception = new Exception("illegal expression: can't compare `" + this + "` to `" + that + "`");
                throw exception;
            }
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
            var that = (Value)o;
            if (IsNumber() && that.IsNumber())
            {
                var result = ToFloat() == that.ToFloat();
                return result;

            }
            else
            {
                return value.Equals(that.value);
            }
        }



        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public bool Isbool()
        {
            return bool.TryParse(value.ToString(), out bool result);
        }

        public bool IsNumber()
        {
            //can convert to int32
            return int.TryParse(value.ToString(), out int result);


        }



        public bool IsNull()
        {
            return this == VOID;
        }

        public bool IsVoid()
        {
            return this == VOID;
        }

        public bool IsString()
        {
            return value.ToString() != null;
        }



        public override string ToString()
        {
            bool isVoid = IsVoid();
            return IsNull() ? "NULL" : isVoid ? @void : value.ToString();
        }
    }
}