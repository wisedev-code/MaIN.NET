## Example: Problematic Code

```csharp
public string GetUser(int id) {
    var conn = new SqlConnection("Server=prod;Password=admin123");
    conn.Open();
    var cmd = new SqlCommand("SELECT * FROM users WHERE id = " + id, conn);
    return cmd.ExecuteScalar().ToString();
}
```

Expected issues: SQL injection, hardcoded connection string with password, no using/dispose, no null check on ExecuteScalar result, synchronous I/O.
