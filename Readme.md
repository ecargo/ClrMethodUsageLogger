Why
===
The concept is to detect dead code by logging the inverse, i.e. live code, as it is JITted. The CLR JITs each method once the first time it is used.

How
===
The CLR uses ETW to output useful logging information; this app hooks in to ETW and subscribes to the appropriate event(s).

Limitations
===========
This logs JITting of each method; each method is only JITted one. Obviously if this app is started after JITting has already taken place then the outputted information will not be complete.

The assembly filtering is the provided by hooking in to a different ETW event (`ModuleLoad` as opposed to `JittingStarted`). If the module (assembly) has already been loaded prior to this app starting, even if no JITting has taken place, then the assembly output and filtering won't function.
