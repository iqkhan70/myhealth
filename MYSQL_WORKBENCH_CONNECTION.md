# MySQL Workbench Connection to DigitalOcean Database

## Connection Method: SSH Tunnel

Since the MySQL database is not directly exposed (for security), you need to connect via **SSH tunnel**.

## Step-by-Step Setup

### 1. Get Connection Details

Run this command to extract the password:

```bash
ssh root@159.65.242.79 "grep -A 1 '\"MySQL\"' /opt/mental-health-app/server/appsettings.Production.json | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '\"' | tr -d '}'"
```

### 2. MySQL Workbench Setup

1. **Open MySQL Workbench**
2. Click **"+"** next to "MySQL Connections" to create a new connection
3. Fill in the connection details:

#### Connection Tab:

- **Connection Name:** `DigitalOcean - Mental Health DB`
- **Connection Method:** `Standard (TCP/IP)`
- **Hostname:** `127.0.0.1` (or `localhost`)
- **Port:** `3306`
- **Username:** `mentalhealth_user`
- **Password:** (paste the password from step 1)
- **Default Schema:** `mentalhealthdb`

#### SSH Tab (IMPORTANT):

- **Use SSH Tunnel:** ✅ **Check this box**
- **SSH Hostname:** `159.65.242.79`
- **SSH Port:** `22`
- **SSH Username:** `root`
- **SSH Password:** (your root password) OR
- **SSH Key File:** (if you use SSH keys, browse to your private key file)

#### Advanced Tab (Optional):

- **Default Schema:** `mentalhealthdb`

### 3. Test Connection

1. Click **"Test Connection"** button
2. If successful, you'll see "Successfully made the MySQL connection"
3. Click **"OK"** to save

### 4. Connect

1. Double-click the connection in the list
2. Enter your SSH password if prompted
3. You should now be connected!

## Alternative: Direct Connection (If MySQL is Exposed)

If MySQL port (3306) is exposed on the server, you can connect directly:

- **Hostname:** `159.65.242.79`
- **Port:** `3306`
- **Username:** `mentalhealth_user`
- **Password:** (from appsettings)
- **Default Schema:** `mentalhealthdb`

**Note:** This is less secure and requires firewall rules. SSH tunnel is recommended.

## Troubleshooting

### "Can't connect to MySQL server"

- Check if SSH tunnel is enabled
- Verify SSH credentials
- Check if MySQL is running: `ssh root@159.65.242.79 "systemctl status mysql"`

### "Access denied"

- Verify username: `mentalhealth_user`
- Verify password (extract again if needed)
- Check if user has remote access: `ssh root@159.65.242.79 "mysql -u root -e \"SELECT user, host FROM mysql.user WHERE user='mentalhealth_user';\""`

### "SSH tunnel failed"

- Verify SSH key or password
- Check SSH access: `ssh root@159.65.242.79 "echo 'SSH works'"`

## Quick Connection String Reference

From your working command:

- **Server:** `159.65.242.79`
- **SSH User:** `root`
- **MySQL User:** `mentalhealth_user`
- **Database:** `mentalhealthdb`
- **Password:** (extract from appsettings.Production.json)
