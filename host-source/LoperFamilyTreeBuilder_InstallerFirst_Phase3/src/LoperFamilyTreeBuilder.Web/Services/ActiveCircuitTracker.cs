using Microsoft.AspNetCore.Components.Server.Circuits;

namespace LoperFamilyTreeBuilder.Web.Services;

public sealed class ActiveCircuitTracker : CircuitHandler
{
    private readonly object _sync = new();
    private int _activeCircuits;
    private bool _hasSeenCircuit;
    private DateTimeOffset _lastCircuitClosedUtc = DateTimeOffset.UtcNow;

    public int ActiveCircuits
    {
        get
        {
            lock (_sync)
            {
                return _activeCircuits;
            }
        }
    }

    public bool HasSeenCircuit
    {
        get
        {
            lock (_sync)
            {
                return _hasSeenCircuit;
            }
        }
    }

    public DateTimeOffset LastCircuitClosedUtc
    {
        get
        {
            lock (_sync)
            {
                return _lastCircuitClosedUtc;
            }
        }
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _activeCircuits++;
            _hasSeenCircuit = true;
        }

        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _activeCircuits = Math.Max(0, _activeCircuits - 1);
            _lastCircuitClosedUtc = DateTimeOffset.UtcNow;
        }

        return Task.CompletedTask;
    }
}
