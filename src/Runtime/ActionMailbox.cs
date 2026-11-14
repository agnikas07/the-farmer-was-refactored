using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheFarmerWasRefactored.Runtime
{
    // One instance per run, constructed on its simulation owner thread.
    // Only the JavaScript worker waits; claim and completion never wait on it.
    public sealed class ActionMailbox : IDisposable
    {
        public enum Command { DoAFlip, CanHarvest, Harvest, Move }

        public sealed class Ticket
        {
            public Command Action { get; }
            public string Direction { get; }
            internal readonly TaskCompletionSource<bool> Result =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            internal bool Claimed;
            internal Ticket(Command action, string direction) { Action = action; Direction = direction; }
        }

        private readonly object gate = new object();
        private readonly int owner = Thread.CurrentThread.ManagedThreadId;
        private Ticket current;
        private bool stopped;

        public bool HasPending
        {
            get { lock (gate) return current != null && !current.Claimed; }
        }

        // Bind as doAFlip in JavaScript. Native names belong only in the adapter.
        public void DoAFlip() => Request(Command.DoAFlip);

        public bool CanHarvest() => Request(Command.CanHarvest);

        public bool Harvest() => Request(Command.Harvest);

        public bool Move(string direction)
        {
            if (direction != "north" && direction != "east" && direction != "south" && direction != "west")
                throw new ArgumentException("Direction must be north, east, south, or west.", nameof(direction));
            return Request(Command.Move, direction);
        }

        private bool Request(Command command, string direction = null)
        {
            if (Thread.CurrentThread.ManagedThreadId == owner)
                throw new InvalidOperationException("The simulation owner must not wait on itself.");
            Ticket ticket;
            lock (gate)
            {
                if (stopped) throw new OperationCanceledException("Action run stopped.");
                if (current != null) throw new InvalidOperationException("An action is already outstanding.");
                current = ticket = new Ticket(command, direction);
            }
            return ticket.Result.Task.GetAwaiter().GetResult();
        }

        public bool TryClaim(out Ticket ticket)
        {
            AssertOwner();
            lock (gate)
            {
                ticket = null;
                if (stopped || current == null || current.Claimed) return false;
                current.Claimed = true;
                ticket = current;
                return true;
            }
        }

        // Call only when native scheduling resumes AFTER the action cost gate.
        // A claim alone must never release the worker.
        public bool Complete(Ticket ticket, bool result = true)
        {
            return Finish(ticket, null, result);
        }

        public bool Fail(Ticket ticket, Exception error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));
            return Finish(ticket, error);
        }

        private bool Finish(Ticket ticket, Exception error, bool result = false)
        {
            AssertOwner();
            lock (gate)
            {
                if (stopped || ticket == null || ticket != current || !ticket.Claimed) return false;
                current = null;
                if (error == null) ticket.Result.TrySetResult(result);
                else ticket.Result.TrySetException(error);
                return true;
            }
        }

        private void AssertOwner()
        {
            if (Thread.CurrentThread.ManagedThreadId != owner)
                throw new InvalidOperationException("Only the simulation owner may claim or finish actions.");
        }

        // Permanent for this run. Never reuse an old mailbox for a new world/run.
        // Before claim: no effect permitted. After claim: cannot undo an effect;
        // the adapter must also preserve native stop/cleanup.
        public void Dispose()
        {
            lock (gate)
            {
                if (stopped) return;
                stopped = true;
                current?.Result.TrySetCanceled();
                current = null;
            }
        }
    }
}
