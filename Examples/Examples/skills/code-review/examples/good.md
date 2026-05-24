## Example: Well-Written Code

```csharp
public async Task<User?> GetUserAsync(int id, CancellationToken ct = default)
{
    await using var conn = new SqlConnection(_connectionString);
    return await conn.QueryFirstOrDefaultAsync<User>(
        "SELECT * FROM users WHERE id = @id",
        new { id },
        cancellationToken: ct);
}
```

Why this is good: async, parameterized query prevents SQL injection, connection string from config, proper disposal via await using, nullable return type communicates that user may not exist.
