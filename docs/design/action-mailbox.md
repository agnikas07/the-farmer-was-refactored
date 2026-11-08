# JavaScript action transport

ActionMailbox is a per-run, single-command transport. It has no game references.
Construct it on the simulation owner thread; only that thread may claim or finish
a ticket. Bind DoAFlip to the JavaScript name doAFlip on the worker.

The worker waits for completion. TryClaim and Complete never wait for worker
execution, and continuations run asynchronously. A short internal lock protects
the slot; no game calls or callbacks run while holding it.

A claim reserves the command but does not acknowledge completion. The slot remains
occupied until Complete or Fail. The future native adapter must call Complete only
when its iterator resumes past the native action cost gate, as demonstrated by the
fixed native action probe. The mailbox itself cannot establish that timing.

Dispose permanently stops that run and releases a waiting worker. Before claim,
it prevents delivery; after claim, it cannot undo an already authorized effect.
Native stop/cleanup must still be coordinated by the adapter. A new execution or
world must get a new mailbox. Tickets from another run and duplicate completions
are rejected by object identity.

## Language naming

The public API follows TypeScript conventions: camelCase functions, variables, and
properties; PascalCase types. For example, native can_harvest maps to canHarvest,
and native do_a_flip maps to doAFlip. Do not expose snake_case compatibility
aliases by default. Native names remain internal to adapter/dependency lookups.

Only doAFlip is exercised here. canHarvest is an agreed naming example, not an
implemented binding. No TypeScript compiler or player-facing declarations ship in
this PR. Native diagnostic trigger programs still use the game's existing syntax.

## Validation and next integration

The desktop suite runs real Jint code with two sequential doAFlip calls and checks
that claiming does not release the script, while completing does. It covers
capacity through completion, duplicate/stale/cross-run responses, owner-thread
guards, cancellation before/after claim, stopped-run rejection, and host failures.
The tests reference the staged netstandard runtime DLL used by the plugin.

Run bash scripts/test-runtime.sh. No game launch is required for this isolated
transport change. Unity/Mono transport validation remains pending. The opt-in
[JavaScript action probe](javascript-action-probe.md) now wires a diagnostic adapter.

Before connecting the worker, define a scheduler yield when no command is ready:
repeated zero-cost native iterator yields can spin without advancing simulated
time. Do not fix that by blocking the simulation thread, adding arbitrary charged
operations, or issuing the next command before the prior action finishes. The
adapter also needs native stop/reset hooks that dispose this mailbox even when a
native iterator is abandoned without disposal. Jint cancellation alone does not
release a host call waiting in this mailbox; stopping must dispose the mailbox too.
