namespace ViteKlub.Core.Data;

public sealed record DatasetValidationError(string Code, string Path, string Message);
