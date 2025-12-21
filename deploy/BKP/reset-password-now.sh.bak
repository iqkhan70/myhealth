#!/bin/bash

# Reset password for a specific user (non-interactive version)

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

# Get parameters
USER_EMAIL="${1:-john@doe.com}"
NEW_PASSWORD="${2:-Password123!}"

if [ "$1" = "--help" ] || [ "$1" = "-h" ]; then
    echo "Usage: $0 [email] [password]"
    echo "Example: $0 john@doe.com Password123!"
    echo "Default: $0 john@doe.com Password123!"
    exit 0
fi

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Password Reset Tool${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Resetting password for: $USER_EMAIL"
echo "New password: $NEW_PASSWORD"
echo ""

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY_PATH" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY_PATH" 2>/dev/null || stat -c "%a" "$SSH_KEY_PATH" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY_PATH"
    fi
fi

# Create a simple C# program to hash the password using the exact same method as AuthService
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    # Create temp directory
    mkdir -p /tmp/password-reset
    cd /tmp/password-reset
    
    # Create C# program that matches AuthService.HashPassword exactly
    cat > Program.cs << 'CSPROG'
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
            Console.Error.WriteLine("Usage: dotnet run -- <password>");
            Environment.Exit(1);
        }
        Console.WriteLine(HashPassword(args[0]));
    }
}
CSPROG

    # Create project file
    cat > password-reset.csproj << 'CSPROJ'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
CSPROJ

    # Build and run
    dotnet build -q 2>/dev/null
    PASSWORD_HASH=\$(dotnet run -- "$NEW_PASSWORD" 2>/dev/null | tail -1)
    
    if [ -z "\$PASSWORD_HASH" ]; then
        echo "❌ Failed to generate password hash"
        exit 1
    fi
    
    echo "✅ Generated password hash (length: \${#PASSWORD_HASH} chars)"
    
    # Update password in database
    DB_PASSWORD=\$(grep -A 1 '\"MySQL\"' $APP_DIR/server/appsettings.Production.json | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '\"' | tr -d '}')
    
    ROWS_UPDATED=\$(mysql -u mentalhealth_user -p"\$DB_PASSWORD" mentalhealthdb -sN << MYSQL
UPDATE Users 
SET PasswordHash = '\$PASSWORD_HASH', 
    MustChangePassword = 0
WHERE Email = '$USER_EMAIL';
SELECT ROW_COUNT();
MYSQL
)
    
    if [ "\$ROWS_UPDATED" = "1" ]; then
        echo "✅ Password reset successfully for $USER_EMAIL"
        echo ""
        echo "Login credentials:"
        echo "  Email: $USER_EMAIL"
        echo "  Password: $NEW_PASSWORD"
    elif [ "\$ROWS_UPDATED" = "0" ]; then
        echo "⚠️  No user found with email: $USER_EMAIL"
        echo ""
        echo "Available users:"
        mysql -u mentalhealth_user -p"\$DB_PASSWORD" mentalhealthdb -e "SELECT Id, Email, FirstName, LastName FROM Users ORDER BY Id LIMIT 10;" 2>/dev/null
        exit 1
    else
        echo "❌ Failed to reset password (updated \$ROWS_UPDATED rows)"
        exit 1
    fi
    
    # Cleanup
    cd /
    rm -rf /tmp/password-reset
ENDSSH

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Password Reset Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "You can now login with:"
echo "  Email: $USER_EMAIL"
echo "  Password: $NEW_PASSWORD"

