namespace StackBraid.Shared.Persistence;

public interface ISoftDeleteService
{
    void Delete(object entity, DateTime nowUtc);

    void Restore(object entity, DateTime nowUtc);
}

public sealed class SoftDeleteService : ISoftDeleteService
{
    public void Delete(object entity, DateTime nowUtc)
    {
        SetProperty(entity, "DeletedAtUtc", nowUtc);
        SetStatus(entity, "Inactive");
    }

    public void Restore(object entity, DateTime nowUtc)
    {
        SetProperty(entity, "DeletedAtUtc", null);
        SetStatus(entity, "Active");
    }

    private static void SetProperty(object entity, string name, object? value)
    {
        var property = entity.GetType().GetProperty(name);
        if (property is null || !property.CanWrite)
        {
            throw new InvalidOperationException($"The entity does not expose a writable {name} property.");
        }

        property.SetValue(entity, value);
    }

    private static void SetStatus(object entity, string value)
    {
        var property = entity.GetType().GetProperty("Status");
        if (property is null || !property.CanWrite || !Enum.IsDefined(property.PropertyType, value))
        {
            throw new InvalidOperationException("The entity does not expose a writable Status property.");
        }

        property.SetValue(entity, Enum.Parse(property.PropertyType, value));
    }
}
