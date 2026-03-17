namespace MoriiCoffee.Domain.Shared.Enums.Notification;

/// <summary>Visual severity and category of an in-app notification.</summary>
public enum ENotificationType
{
    /// <summary>Informational message — no action required.</summary>
    Info = 0,

    /// <summary>Warning that may require user attention.</summary>
    Warning = 1,

    /// <summary>Error or failure notification.</summary>
    Error = 2,

    /// <summary>Success confirmation (e.g., order placed, payment received).</summary>
    Success = 3
}
