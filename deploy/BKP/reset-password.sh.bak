#!/bin/bash

# Reset a user's password on the DigitalOcean server
# This uses the correct password hashing method

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Configuration
DROPLET_IP="159.65.242.79"
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Password Reset Tool${NC}"
echo -e "${GREEN}========================================${NC}"

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY_PATH" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY_PATH" 2>/dev/null || stat -c "%a" "$SSH_KEY_PATH" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY_PATH"
    fi
fi

# Get user email and new password
echo ""
read -p "Enter user email: " USER_EMAIL
read -sp "Enter new password: " NEW_PASSWORD
echo ""

if [ -z "$USER_EMAIL" ] || [ -z "$NEW_PASSWORD" ]; then
    echo -e "${RED}ERROR: Email and password are required${NC}"
    exit 1
fi

echo -e "\n${YELLOW}Resetting password for: $USER_EMAIL${NC}"

# Create a C# script to hash the password using the correct method
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    cat > /tmp/reset-password.cs << 'CSPROG'
using System;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[32];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);

        var hashBytes = new byte[64];
        Array.Copy(salt, 0, hashBytes, 0, 32);
        Array.Copy(hash, 0, hashBytes, 32, 32);

        return Convert.ToBase64String(hashBytes);
    }

    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: reset-password <password>");
            Environment.Exit(1);
        }
        Console.WriteLine(HashPassword(args[0]));
    }
}
CSPROG

    # Compile and run the C# script
    cd /tmp
    dotnet new console -n ResetPassword -f net8.0 --force 2>/dev/null || true
    cat > /tmp/ResetPassword/Program.cs << 'CSPROG'
using System;
using System.Security.Cryptography;

class Program
{
    static string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[32];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);

        var hashBytes = new byte[64];
        Array.Copy(salt, 0, hashBytes, 0, 32);
        Array.Copy(hash, 0, hashBytes, 32, 32);

        return Convert.ToBase64String(hashBytes);
    }

    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: dotnet run -- <password>");
            Environment.Exit(1);
        }
        Console.WriteLine(HashPassword(args[0]));
    }
}
CSPROG
    cd /tmp/ResetPassword
    dotnet build -q 2>/dev/null
    PASSWORD_HASH=\$(dotnet run -- "$NEW_PASSWORD" 2>/dev/null | tail -1)
    
    # Update password in database
    DB_PASSWORD=\$(grep -A 1 '\"MySQL\"' $APP_DIR/server/appsettings.Production.json | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '\"' | tr -d '}')
    
    mysql -u mentalhealth_user -p"\$DB_PASSWORD" mentalhealthdb << MYSQL
UPDATE Users 
SET PasswordHash = '\$PASSWORD_HASH', 
    MustChangePassword = 0
WHERE Email = '$USER_EMAIL';
SELECT ROW_COUNT() as rows_updated;
MYSQL

    if [ \$? -eq 0 ]; then
        echo "✅ Password reset successfully for $USER_EMAIL"
    else
        echo "❌ Failed to reset password"
        exit 1
    fi
    
    # Cleanup
    rm -rf /tmp/ResetPassword /tmp/reset-password.cs
ENDSSH

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Password Reset Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "You can now login with:"
echo "  Email: $USER_EMAIL"
echo "  Password: (the password you just entered)"

