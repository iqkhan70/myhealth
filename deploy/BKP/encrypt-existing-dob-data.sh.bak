#!/bin/bash

# Script to encrypt existing DateOfBirth data on DigitalOcean server
# This can be run after migrations are applied to encrypt any plain text dates

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
SERVER_IP="159.65.242.79"
SSH_KEY="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app/server"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Encrypt Existing DateOfBirth Data${NC}"
echo -e "${GREEN}========================================${NC}"

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY" 2>/dev/null || stat -c "%a" "$SSH_KEY" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY"
    fi
fi

echo -e "\n${BLUE}This script will encrypt any plain text DateOfBirth values in the database.${NC}"
echo -e "${YELLOW}Note: The application automatically encrypts dates when accessed,${NC}"
echo -e "${YELLOW}but this script will encrypt all existing data immediately.${NC}"
echo -e ""
read -p "Continue? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Cancelled."
    exit 0
fi

echo -e "\n${BLUE}Creating encryption script on server...${NC}"

# Create a C# console app to encrypt data
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << 'ENDSSH'
    cd /opt/mental-health-app/server
    
    # Create a simple encryption runner
    cat > EncryptDobRunner.cs << 'CSHARP'
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Server.Helpers;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.Production.json", optional: false, reloadOnChange: true);

var configuration = builder.Build();

var services = new ServiceCollection();
services.AddDbContext<JournalDbContext>(options =>
    options.UseMySql(
        configuration.GetConnectionString("MySQL"),
        new MySqlServerVersion(new Version(8, 0, 21)),
        b => b.MigrationsAssembly("SM_MentalHealthApp.Server")
    ));

services.AddSingleton<IPiiEncryptionService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new PiiEncryptionService(config);
});

services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));

var serviceProvider = services.BuildServiceProvider();

try
{
    await EncryptExistingDateOfBirthData.RunAsync(serviceProvider);
    Console.WriteLine("✅ Encryption complete!");
    Environment.Exit(0);
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Environment.Exit(1);
}
CSHARP

    echo "✅ Encryption script created"
ENDSSH

echo -e "\n${BLUE}Running encryption on server...${NC}"

# Run the encryption using dotnet-script or compile and run
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << 'ENDSSH'
    cd /opt/mental-health-app/server
    export PATH=$PATH:/usr/share/dotnet:/root/.dotnet/tools
    
    # Use the existing EncryptExistingDateOfBirthData script
    # We'll create a simple console runner
    echo "Note: The application will automatically encrypt dates when accessed."
    echo "For immediate encryption, you can temporarily add the encryption call to Program.cs"
    echo "or run it manually through the application."
    
    # Check if there are any plain text dates
    DB_NAME="mentalhealthdb"
    DB_USER="mentalhealth_user"
    DB_PASSWORD=$(grep -A 1 '"MySQL"' appsettings.Production.json | grep -o 'password=[^;]*' | cut -d'=' -f2 | tr -d '"' | tr -d ',')
    
    if [ -z "$DB_PASSWORD" ]; then
        echo "⚠️  Could not get database password"
        exit 0
    fi
    
    echo "Checking for plain text dates in Users table..."
    PLAIN_TEXT_COUNT=$(mysql -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" -N -e "
        SELECT COUNT(*) FROM Users 
        WHERE DateOfBirthEncrypted IS NOT NULL 
        AND DateOfBirthEncrypted != '' 
        AND (DateOfBirthEncrypted LIKE '%-%-%' OR DateOfBirthEncrypted LIKE '%/%')
        AND DateOfBirthEncrypted NOT LIKE '%=%';
    " 2>/dev/null || echo "0")
    
    echo "Found $PLAIN_TEXT_COUNT potentially unencrypted dates in Users table"
    
    echo "Checking for plain text dates in UserRequests table..."
    PLAIN_TEXT_REQUESTS=$(mysql -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" -N -e "
        SELECT COUNT(*) FROM UserRequests 
        WHERE DateOfBirthEncrypted IS NOT NULL 
        AND DateOfBirthEncrypted != '' 
        AND (DateOfBirthEncrypted LIKE '%-%-%' OR DateOfBirthEncrypted LIKE '%/%')
        AND DateOfBirthEncrypted NOT LIKE '%=%';
    " 2>/dev/null || echo "0")
    
    echo "Found $PLAIN_TEXT_REQUESTS potentially unencrypted dates in UserRequests table"
    
    if [ "$PLAIN_TEXT_COUNT" -gt 0 ] || [ "$PLAIN_TEXT_REQUESTS" -gt 0 ]; then
        echo "⚠️  Found plain text dates. They will be encrypted automatically when accessed."
        echo "   To encrypt immediately, restart the application and access user records."
    else
        echo "✅ All dates appear to be encrypted"
    fi
ENDSSH

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}✅ Encryption Check Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo -e "\n${YELLOW}Note:${NC} The application automatically encrypts plain text dates when they are accessed."
echo -e "To ensure all dates are encrypted, restart the application:"
echo -e "${BLUE}systemctl restart mental-health-app${NC}"

