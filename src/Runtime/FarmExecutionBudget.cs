using System;
using Jint;

namespace TheFarmerWasRefactored.Runtime
{
    // Owned by the JavaScript worker. Native completion itself never resets it.
    public sealed class FarmExecutionBudget : Constraint
    {
        public const int StatementLimit = 10_000;
        private int statements;

        public override void Check()
        {
            if (++statements > StatementLimit)
                throw new FarmComputationLimitException();
        }

        public override void Reset() => statements = 0;
    }

    public sealed class FarmComputationLimitException : Exception
    {
        public FarmComputationLimitException()
            : base("Computation limit reached: complete a farm call within 10000 statements. Stop and simplify loops that do not call the farm.") { }
    }
}
