namespace AutoEvent.API.Enums;

public enum EventRegistrationResult
{
    Success,
    EventIsNull,
    AlreadyRegistered,
    NotFound,
    CannotUnregisterInternal,
    MissingProjectMer
}