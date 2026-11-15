# Source-run lifecycle cleanup

The inspected game closes a Window by removing it and destroying its object.
CodeWindow.OnDisable only removes a theme subscription. MainSim.SetupSim replaces
the simulation; MainSim.OnDestroy sets its disposed flag. Those methods do not
themselves pass through the Execution.StopExecution hook that owns our worker
cleanup. Calling code can stop execution first, but the adapter should not rely
on every caller doing so.

Source mode now prefixes Window.Close, MainSim.SetupSim and MainSim.OnDestroy
inside the existing Core.dll checksum gate. It stops only the tracked TypeScript
run belonging to that simulation owner; a window close additionally requires
the run's source window to match. Closing documentation/unrelated code windows
does not stop the active run. Minimizing is not closing and is unchanged.

Ownership is checked again under the native simulation lock. Native stop releases
the mailbox and cancels the worker through the existing execution hook; no worker
join occurs on Unity or the simulation thread. A stale adapter is detached
without stopping a newer native execution. Already-applied farm effects cannot
be undone. Pending compiler/runtime UI messages for the old source window are
invalidated before teardown.

## Validation

Locked plugin builds and desktop runtime/mailbox/cancellation/loop regression
checks pass. These do not execute native Unity lifecycle hooks. No simulation
teardown race is claimed proven by those tests.

Manual checks pending on CachyOS/Proton:
1. Run a continuous farm program, close its code window with X: actions stop,
   no popup appears, and the log reports worker released.
2. Open a new code window and run doAFlip(): one flip, no stale-run rejection.
3. While a program runs, close an unrelated code/docs window: it keeps running.
4. While running, switch saves or return to the main menu and reload: the old
   run releases, no actions leak into the loaded farm, and a new run succeeds.

Close-during-compilation, application exit, and repeated transition races need
separate verification. This change does not add pause/step or timing parity.
