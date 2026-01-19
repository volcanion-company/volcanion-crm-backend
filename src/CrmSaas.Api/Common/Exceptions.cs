namespace CrmSaas.Api.Common;

public class ApiException : Exception
{
    public int StatusCode { get; }
    public List<string>? Errors { get; }

    public ApiException(string message, int statusCode = 500) : base(message)
    {
        StatusCode = statusCode;
    }

    public ApiException(string message, int statusCode, List<string> errors) : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}

public class NotFoundException : ApiException
{
    public NotFoundException(string message) : base(message, 404) { }
    public NotFoundException(string entity, object key) : base($"{entity} with id '{key}' was not found.", 404) { }
}

public class BadRequestException : ApiException
{
    public BadRequestException(string message) : base(message, 400) { }
    public BadRequestException(string message, List<string> errors) : base(message, 400, errors) { }
}

public class UnauthorizedException : ApiException
{
    public UnauthorizedException(string message = "Unauthorized") : base(message, 401) { }
}

public class ForbiddenException : ApiException
{
    public ForbiddenException(string message = "Forbidden") : base(message, 403) { }
}

public class ConflictException : ApiException
{
    public ConflictException(string message) : base(message, 409) { }
}

public class ValidationException : ApiException
{
    public ValidationException(string message) : base(message, 422) { }
    public ValidationException(List<string> errors) : base("Validation failed", 422, errors) { }
}
