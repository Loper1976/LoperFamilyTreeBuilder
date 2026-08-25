# Host Application Integration Plan

## Current constraint
The repository does not expose the complete Windows application as a normal searchable source tree. The primary source is carried inside `bootstrap/source.part*.b64` with additional override assets. GitHub code search therefore cannot safely identify all host forms, database classes, migrations, or installer project files directly.

## Rule
Do not guess host class names, database technology, or installer wiring. The research-agent core remains independently compilable until the host source is reconstructed in a normal working tree.

## Alpha integration sequence
1. Reconstruct the bootstrap source package on a Windows development machine.
2. Run the existing host application and existing tests before modification.
3. Record database provider, schema/migration mechanism, startup form/navigation architecture, backup mechanism, installer project, and stable person-ID type.
4. Add `ResearchAgent.Core` as a project/reference without changing existing person IDs or Legacy Numbers.
5. Implement a host adapter for `IResearchCenterQueryStore` and `IApprovalStore`.
6. Add Research Center navigation and person context.
7. Wire `ResearchCommandService.ResearchThisPersonAsync` to the UI button.
8. Add configuration UI for local Ollama first. Cloud providers remain opt-in.
9. Validate accepted-tree promotion through the host's existing backup/audit path.
10. Run regression tests, backup/restore test, clean-machine installer test, then evaluate `AlphaReadiness`.

## Installation checkpoint
Do not ask the user to install Alpha until every `AlphaReadinessInput` flag can be demonstrated true on a reconstructed host build.
