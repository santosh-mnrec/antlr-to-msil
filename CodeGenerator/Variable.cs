using System;
using System.Collections;
using System.Collections.Generic;

namespace AntlrCodeGenerator
{
    public class Value : IComparable<Value>

    {
       
        public static readonly Value VOID = new Value();
       

        private readonly object value;

        public Value()
        {
           
            value = new object();
        }

        public Value(object input)
        {
            if (input == null)
            {
                throw new Exception("v == null");
            }
            value = input;
            // only accept bool, list, number or string types
            if (!(Isbool()  || IsNumber() || isString() || isNull()))
            {
                throw new Exception("invalid data type: " + input + " (" + input.GetType() + ")");
            }
        }

        public bool Asbool()
        {
            var result = false;
            if (value is bool)
            {
                result = (bool)value;
                //Do something
            }
            else
            {
                //It's not a bool
            }
            return result;
        }

        public int AsDouble()
        {
            return int.Parse(value.ToString());
        }

        public int AsInt()
        {
            return (int)value;
        }



        public string AsString()
        {
            return value.ToString();
        }



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
                    return AsDouble().CompareTo(that.AsDouble());
                }
            }
            else if (isString() && that.isString())
            {
                return AsString().CompareTo(that.AsString());
            }
            else
            {
                throw new Exception("illegal expression: can't compare `" + this + "` to `" + that + "`");
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
                var result = AsDouble() == that.AsDouble();
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

      

        public bool isNull()
        {
            return this == VOID;
        }

        public bool isVoid()
        {
            return this == VOID;
        }

        public bool isString()
        {
            return value is string;
        }



        public override string ToString()
        {
            return isNull() ? "NULL" : isVoid() ? "VOID" : value.ToString();
        }
    }
}