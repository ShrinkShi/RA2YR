# ADR-0042 - M5-C3 production and technology foundation

Production definitions, prerequisite tokens, availability blockers, and queue
entries remain distinct immutable/candidate layers. Only the explicit
`ExplicitCapabilitiesAndLimits` and `FifoPerFactory` synthetic profiles are
implemented. Payment, refunds, modifiers, factory capture/destruction,
placement/exits, and completion actor creation remain deferred to later
simulation work. No ProjectBaseline data or original-runtime compatibility
claim is added.
