/* This is a small sample program to test/learn DLR's binding features. */
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq.Expressions;
using Microsoft.Scripting;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Providers;
using Microsoft.Scripting.Runtime;

namespace Sample
{
    public class TestLanguageBindings
    {
        public static void Main()
        {
            var engine = MyLanguage.CreateEngine(); 
            
            var r = engine.Execute("return 'abc' < 'xyz'");
            Console.WriteLine("Result: {0}", r); // Expect to see: Result: True

            // But instead get an InvalidOperationException exception:
            // "The result type 'System.Object' of the binder 
            //  'Sample.MyBinaryOperationBinder' is not compatible with the 
            //  result type 'System.Boolean' expected by the call site."
        }
    }

    public static class MyLanguage
    {
        internal static LanguageSetup CreateLanguageSetup()
        {
            var setup = new LanguageSetup(
                typeof(MyContext).AssemblyQualifiedName,
                "test",
                new[] { "test" },
                new[] { ".tst" });

            return setup;
        }

        internal static ScriptRuntimeSetup CreateRuntimeSetup()
        {
            var setup = new ScriptRuntimeSetup();
            setup.LanguageSetups.Add(CreateLanguageSetup());
            return setup;
        }

        public static ScriptRuntime CreateRuntime()
        {
            return new ScriptRuntime(CreateRuntimeSetup());
        }

        public static ScriptEngine CreateEngine()
        {
            var runtime = CreateRuntime();
            return runtime.GetEngineByTypeName(
                typeof(MyContext).AssemblyQualifiedName);
        }

        public static MyContext GetMyContext(this ScriptEngine engine)
        {
            return HostingHelpers.GetLanguageContext(engine) as MyContext;
        }
    }

    public class MyContext : LanguageContext
    {
        public DefaultBinder Binder;

        public MyContext(ScriptDomainManager domainManager, IDictionary<string, object> options = null)
            : base(domainManager)
        {
            Binder = new DefaultBinder();
        }

        public override ScriptCode CompileSourceCode(SourceUnit sourceUnit, CompilerOptions options, ErrorSink errorSink)
        {
            var context = this;
            // Here we parse the code and then eventually create Expressions. 
            // For testing we will skip the parsing step and just do the generate step here.            

            var statements = new List<Expression>();

            // Assume our code is: return 'abc' < 'xyz'

            var left = Expression.Constant("abc");
            var right = Expression.Constant("xyz");
            var opr = ExpressionType.LessThan;

            var binExpr = Expression.Dynamic(
                context.CreateBinaryOperationBinder(opr),
                typeof(bool), left, right);

            var binExprObj = Expression.Convert(binExpr, typeof(object));

            var blockReturnLabel = Expression.Label(typeof(object));
            statements.Add(Expression.Return(blockReturnLabel, binExprObj, typeof(object)));

            statements.Add(Expression.Label(blockReturnLabel, Expression.Constant(null)));

            var block = Expression.Block(typeof(object), statements);
            var code = Expression.Lambda<Func<dynamic>>(block);

            Func<dynamic> compiledCode = code.Compile();
            return new MyScriptCode(sourceUnit, compiledCode);
        }

        private readonly Lazy<Dictionary<ExpressionType, BinaryOperationBinder>> _binOprBinders =
            new Lazy<Dictionary<ExpressionType, BinaryOperationBinder>>(() =>
                new Dictionary<ExpressionType, BinaryOperationBinder>());

        public override BinaryOperationBinder CreateBinaryOperationBinder(ExpressionType operation)
        {
            var lookup = _binOprBinders.Value;

            BinaryOperationBinder binder;
            if (!lookup.TryGetValue(operation, out binder))
                lookup[operation] = binder = new MyBinaryOperationBinder(Binder, operation);

            return binder;
        }
    }

    public class MyBinaryOperationBinder : BinaryOperationBinder
    {
        private readonly DefaultBinder _binder;

        public MyBinaryOperationBinder(DefaultBinder binder, ExpressionType operation)
            : base(operation)
        {
            Contract.Requires(binder != null);
            _binder = binder;
        }

        public override DynamicMetaObject FallbackBinaryOperation(
            DynamicMetaObject target,
            DynamicMetaObject arg,
            DynamicMetaObject errorSuggestion)
        {
            DynamicMetaObject left = target;
            DynamicMetaObject right = arg;

            if (Operation != ExpressionType.LessThan)
                throw new NotImplementedException();

            if (left.LimitType != right.LimitType)
            {
                throw new Exception(String.Format(
                    "attempt to compare {0} with {1}",
                    left.LimitType.Name, right.LimitType.Name));
            }

            if (left.LimitType != typeof(string) &&
                left.LimitType != typeof(double))
            {
                throw new Exception(String.Format(
                    "attempt to compare two {0} values",
                    left.LimitType.Name));
            }

            return _binder.DoOperation(Operation, left, right);
        }
    }

    public class MyScriptCode : ScriptCode
    {
        private readonly Func<dynamic> _compiledCode;

        public MyScriptCode(SourceUnit sourceUnit, Func<dynamic> compiledCode)
            : base(sourceUnit)
        {
            Contract.Requires(compiledCode != null);
            _compiledCode = compiledCode;
        }

        public override object Run(Scope scope)
        {
            return _compiledCode();
        }
    }

} // namespace
/* end of file */
