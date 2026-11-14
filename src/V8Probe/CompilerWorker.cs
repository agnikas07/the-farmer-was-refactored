using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace TheFarmerWasRefactored.V8Probe
{
    // One owner thread for V8 creation, compilation, and disposal. Never called on Unity.
    internal sealed class CompilerWorker : IDisposable
    {
        private readonly object gate = new object();
        private readonly BlockingCollection<Request> queue = new BlockingCollection<Request>(1);
        private readonly CancellationTokenSource shutdown = new CancellationTokenSource();
        private readonly TaskCompletionSource<bool> finished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool disposed;
        private Exception failure;
        internal Task Completion => finished.Task;

        internal CompilerWorker(string bundlePath, Action<string> report)
        {
            new Thread(() => Work(bundlePath, report)) { IsBackground = true, Name = "Refactored TypeScript compiler" }.Start();
        }

        internal string Compile(string source, Action<string> mark, CancellationToken token)
        {
            if (source == null || source.Length > 32_768)
                throw new ArgumentException("TypeScript source must be at most 32768 characters.");
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, shutdown.Token))
            {
                var request = new Request(source, mark, linked.Token);
                using (linked.Token.Register(() => request.Result.TrySetCanceled()))
                {
                    lock (gate)
                    {
                        if (failure != null) throw new InvalidOperationException("Compiler worker failed to initialize.", failure);
                        if (disposed) throw new ObjectDisposedException(nameof(CompilerWorker));
                        linked.Token.ThrowIfCancellationRequested();
                        if (!queue.TryAdd(request)) throw new InvalidOperationException("Compiler queue is busy; retry Run.");
                    }
                    // Only the action worker waits. Stop cancels this wait without joining V8.
                    return request.Result.Task.GetAwaiter().GetResult();
                }
            }
        }

        private void Work(string bundlePath, Action<string> report)
        {
            try
            {
                using (var session = new CompilerSession(bundlePath, null))
                {
                    report($"TypeScript compiler worker: ready (thread={Thread.CurrentThread.ManagedThreadId})");
                    foreach (var request in queue.GetConsumingEnumerable())
                    {
                        if (request.Token.IsCancellationRequested) continue;
                        try
                        {
                            request.Mark?.Invoke("compiler_request_started");
                            var javascript = session.Compile(request.Source, request.Mark);
                            if (!request.Token.IsCancellationRequested) request.Result.TrySetResult(javascript);
                        }
                        catch (Exception error) { request.Result.TrySetException(error); }
                    }
                }
            }
            catch (Exception error)
            {
                lock (gate)
                {
                    failure = error;
                    queue.CompleteAdding();
                    while (queue.TryTake(out var pending)) pending.Result.TrySetException(error);
                }
                report($"TypeScript compiler worker FAILED: {error}");
            }
            finally
            {
                lock (gate) { disposed = true; queue.Dispose(); }
                shutdown.Cancel();
                report("TypeScript compiler worker: released");
                finished.TrySetResult(true);
            }
        }

        public void Dispose()
        {
            lock (gate)
            {
                if (disposed) return;
                disposed = true;
                queue.CompleteAdding();
            }
            shutdown.Cancel();
            // The owner exits after any active compile, then disposes V8 itself. No join.
        }

        private sealed class Request
        {
            internal readonly string Source;
            internal readonly Action<string> Mark;
            internal readonly CancellationToken Token;
            internal readonly TaskCompletionSource<string> Result = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            internal Request(string source, Action<string> mark, CancellationToken token)
            { Source = source; Mark = mark; Token = token; }
        }
    }
}
