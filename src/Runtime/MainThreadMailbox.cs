using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheFarmerWasRefactored.Runtime
{
    // Single outstanding request for the diagnostic. No Jint or Unity objects cross threads.
    public sealed class MainThreadMailbox
    {
        private sealed class Request
        {
            public readonly int Input;
            public readonly TaskCompletionSource<int> Result =
                new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            public int State; // 0 queued, 1 claimed by owner, 2 cancelled before claim
            public Request(int input) { Input = input; }
            public void Cancel()
            {
                Interlocked.CompareExchange(ref State, 2, 0);
                Result.TrySetCanceled();
            }
        }

        private readonly int owner = Thread.CurrentThread.ManagedThreadId;
        private readonly Func<int, int> handler;
        private Request pending;
        public bool HasPending => Volatile.Read(ref pending) != null;

        public MainThreadMailbox(Func<int, int> handler)
        {
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public int Invoke(int input, CancellationToken token)
        {
            if (Thread.CurrentThread.ManagedThreadId == owner)
                throw new InvalidOperationException("The owner thread must never wait on its own mailbox.");
            token.ThrowIfCancellationRequested();
            var request = new Request(input);
            if (Interlocked.CompareExchange(ref pending, request, null) != null)
                throw new InvalidOperationException("Mailbox already has an outstanding request.");
            using (token.Register(() =>
            {
                request.Cancel();
                Interlocked.CompareExchange(ref pending, null, request);
            }))
                return request.Result.Task.GetAwaiter().GetResult();
        }

        // Called only by the main thread; at most one short host action per call.
        public void Pump()
        {
            if (Thread.CurrentThread.ManagedThreadId != owner)
                throw new InvalidOperationException("Only the owner may pump the mailbox.");
            var request = Interlocked.Exchange(ref pending, null);
            if (request == null || Interlocked.CompareExchange(ref request.State, 1, 0) != 0) return;
            // Cancellation after claim can release the worker, but cannot undo this action.
            try { request.Result.TrySetResult(handler(request.Input)); }
            catch (Exception error) { request.Result.TrySetException(error); }
        }
    }
}
