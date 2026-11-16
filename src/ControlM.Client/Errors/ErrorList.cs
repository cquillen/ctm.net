namespace ControlM.Client.Errors;

internal sealed record ErrorList(IReadOnlyList<ControlMError>? Errors);
