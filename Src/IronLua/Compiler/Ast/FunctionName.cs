using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace IronLua.Compiler.Ast
{
    class FunctionName : Node
    {
        private readonly List<string> _identifiers;
        private readonly bool _hasTableMethod;

        public List<string> Identifiers
        {
            get { return _identifiers; }
        }

        public bool HasTableMethod
        {
            get { return _hasTableMethod; }
        }

        public FunctionName(List<string> identifiers, bool lastIsTableMethod)
        {
            Contract.Requires(identifiers != null);
            Contract.Requires(identifiers.Count > 0);
            Contract.Requires(identifiers.Count > 1 || !lastIsTableMethod);
            _identifiers = identifiers;
            _hasTableMethod = lastIsTableMethod;
        }

        public FunctionName(string identifier)
            : this(new List<string> { identifier }, false)
        {
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Identifiers != null);
            Contract.Invariant(Identifiers.Count > 0);
            // Must have at least two identifiers if we have a table method
            Contract.Invariant(Identifiers.Count > 1 || !HasTableMethod); 
        }
    }
}