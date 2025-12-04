# Database Migration Options

Since the server only has .NET Runtime (not SDK), you have 3 options to run database migrations:

## Option 1: Install .NET SDK on Server (Recommended - Simplest)

This installs the SDK and EF tools on the server so you can run migrations directly:

```bash
./install-ef-tool.sh
```

Then run migrations:

```bash
ssh root@68.183.61.49 'cd /opt/mental-health-app/server && dotnet ef database update'
```

**Pros:**

- Simplest long-term solution
- Can run migrations anytime on server
- No need to generate SQL files

**Cons:**

- Installs SDK (~200MB) on server (but you can remove it later)

---

## Option 2: Generate SQL Locally and Apply on Server

This generates a SQL script from your local migrations and applies it on the server:

```bash
./generate-migration-sql.sh
```

**Pros:**

- No SDK needed on server
- Can review SQL before applying
- Works with any database tool

**Cons:**

- Requires local .NET SDK
- Two-step process

---

## Option 3: Apply Migrations Directly from Local Machine

This connects your local machine directly to the remote database:

```bash
./apply-migration-direct.sh
```

**Pros:**

- No SDK needed on server
- Direct connection

**Cons:**

- Requires MySQL to allow remote connections (might need to configure firewall)
- Requires local .NET SDK

---

## Quick Recommendation

**For first-time setup:** Use Option 1 (`./install-ef-tool.sh`) - it's the simplest and you can always remove the SDK later if needed.

**For ongoing updates:** Once SDK is installed, just use:

```bash
ssh root@68.183.61.49 'cd /opt/mental-health-app/server && dotnet ef database update'
```

---

## Troubleshooting

### "No .NET SDKs were found"

- Run `./install-ef-tool.sh` to install SDK on server

### "The application 'ef' does not exist"

- Run `dotnet tool install --global dotnet-ef` on server
- Or run `./install-ef-tool.sh`

### "Cannot connect to MySQL server"

- Check if MySQL is running: `systemctl status mysql`
- Check if firewall allows MySQL port (3306)
- For Option 3, ensure MySQL allows remote connections
