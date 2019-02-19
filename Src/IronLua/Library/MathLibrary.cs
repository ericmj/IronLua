using System;
using IronLua.Runtime;

namespace IronLua.Library
{
    class MathLibrary : Library
    {
        public MathLibrary(LuaContext context)
            : base(context)
        {

        }

        private Random rand = new Random();

        public override void Setup(LuaTable table)
        {
            const double Math_Tau = 2.0 * Math.PI; // http://tauday.com

            table.SetConstant("huge", Double.MaxValue);

            // Basic operations
            table.SetConstant("abs", (Func<double, double>)Math.Abs);
            table.SetConstant("mod", (Func<double, double, double>) ((a, b) => a%b));
            table.SetConstant("modf", (Func<double, double, Varargs>)((a, b) =>
            {
                long r;
                long q = Math.DivRem((long) a, (long) b, out r);
                return new Varargs(q, r);
            }));
            table.SetConstant("floor", (Func<double, double>) Math.Floor);
            table.SetConstant("ceil", (Func<double, double>) Math.Ceiling);
            table.SetConstant("min", (Func<double, double, double>) Math.Min);
            table.SetConstant("max", (Func<double, double, double>) Math.Max);

            // Exponetial and logarithmic
            table.SetConstant("sqrt", (Func<double, double>) Math.Sqrt);
            table.SetConstant("pow", (Func<double, double, double>) Math.Pow);
            table.SetConstant("exp", (Func<double, double>) Math.Exp);
            table.SetConstant("log", (Func<double, double>) Math.Log);
            table.SetConstant("log10", (Func<double, double>) Math.Log10);

            // Trigonometrical
            table.SetConstant("pi", Math.PI);
            table.SetConstant("tau", Math_Tau);
            table.SetConstant("deg", (Func<double, double>)(r => r * 360.0 / Math_Tau));
            table.SetConstant("rad", (Func<double, double>)(d => d / 360.0 * Math_Tau));
            table.SetConstant("cos", (Func<double, double>) Math.Cos);
            table.SetConstant("sin", (Func<double, double>) Math.Sin);
            table.SetConstant("tan", (Func<double, double>)Math.Tan);
            table.SetConstant("acos", (Func<double, double>)Math.Acos);
            table.SetConstant("asin", (Func<double, double>)Math.Asin);
            table.SetConstant("atan", (Func<double, double>)Math.Atan);
            table.SetConstant("atan2", (Func<double, double, double>)Math.Atan2);
            
            // Splitting on powers of 2
            //table.SetConstant("frexp", (Func<double, double>) Math.??);
            //table.SetConstant("ldexp", (Func<double, double, double>) Math.??);

            // Pseudo-random numbers
            table.SetConstant("randomseed", (Func<double, double>)(x => { rand = new Random((int)x); return rand.NextDouble(); }));
            table.SetConstant("random", (Func<double, double, double>)((min,max) => { return (double)rand.Next((int)min,(int)max); })); // overloaded
        }
    }
}