using LabApi.Features.Wrappers;

namespace AutoEvent.Events.EventArgs;

public class UsingStaminaArgs(ReferenceHub ply, float amount, bool isAllowed = true)
{
    public Player Player { get; } = Player.Get(ply);
    public float Amount { get; } = amount;
    public bool IsAllowed { get; set; } = isAllowed;
}