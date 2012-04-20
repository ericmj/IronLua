using System;
using IronLua.Runtime;

namespace IronLua.Library
{
    class MathLibrary : Library
    {
        public MathLibrary(Context context)
            : base(context)
        {

        }

        public override void Setup(LuaTable table)
        {
            const double Math_Tau = 2.0 * Math.PI; // http://tauday.com

            table.SetValue("huge", Double.MaxValue);

            // Basic operations
            table.SetValue("abs", (Func<double, double>)Math.Abs);
            table.SetValue("mod", (Func<double, double, double>) ((a, b) => a%b));
            table.SetValue("modf", (Func<double, double, Varargs>)((a, b) =>
            {
                long r;
                long q = Math.DivRem((long) a, (long) b, out r);
                return new Varargs(q, r);
            }));
            table.SetValue("floor", (Func<double, double>) Math.Floor);
            table.SetValue("ceil", (Func<double, double>) Math.Ceiling);
            table.SetValue("min", (Func<double, double, double>) Math.Min);
            table.SetValue("max", (Func<double, double, double>) Math.Max);

            // Exponetial and logarithmic
            table.SetValue("sqrt", (Func<double, double>) Math.Sqrt);
            table.SetValue("pow", (Func<double, double, double>) Math.Pow);
            table.SetValue("exp", (Func<double, double>) Math.Exp);
            table.SetValue("log", (Func<double, double>) Math.Log);
            table.SetValue("log10", (Func<double, double>) Math.Log10);

            // Trigonometrical
            table.SetValue("pi", Math.PI);
            table.SetValue("tau", Math_Tau);
            table.SetValue("deg", (Func<double, double>)(r => r * 360.0 / Math_Tau));
            table.SetValue("rad", (Func<double, double>)(d => d / 360.0 * Math_Tau));
            table.SetValue("cos", (Func<double, double>) Math.Cos);
            table.SetValue("sin", (Func<double, double>) Math.Sin);
            table.SetValue("tan", (Func<double, double>)Math.Tan);
            table.SetValue("acos", (Func<double, double>)Math.Acos);
            table.SetValue("asin", (Func<double, double>)Math.Asin);
            table.SetValue("atan", (Func<double, double>)Math.Atan);
            table.SetValue("atan2", (Func<double, double, double>)Math.Atan2);
            
            // Splitting on powers of 2
            //table.SetValue("frexp", (Func<double, double>) Math.??);
            //table.SetValue("ldexp", (Func<double, double, double>) Math.??);

            // Pseudo-random numbers
            //table.SetValue("randomseed", (Func<double, double>) Math.??);
            //table.SetValue("random", (Func<double, double, double>) Math.??); // overloaded
        }
    }
}