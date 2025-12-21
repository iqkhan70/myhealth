#!/bin/bash

# Check which provider is configured (HuggingFace vs Ollama)

DROPLET_IP="64.225.12.121"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "🔍 Checking Provider Configuration..."
echo ""

echo "=== LOCAL MAC ==="
echo "1. Environment Variables:"
printenv | grep -iE "hugging|ollama|hf_" || echo "   No HuggingFace or Ollama env vars found"
echo ""

echo "2. Local appsettings.json:"
echo "   HuggingFace section: $(grep -A 3 '"HuggingFace"' SM_MentalHealthApp.Server/appsettings.json | head -1 || echo 'Not found')"
echo "   Ollama section: $(grep -A 3 '"Ollama"' SM_MentalHealthApp.Server/appsettings.json | head -1 || echo 'NOT FOUND (this is expected)')"
echo ""

echo "=== SERVER ==="
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    echo "1. Environment Variables:"
    env | grep -iE "hugging|ollama|hf_" || echo "   No HuggingFace or Ollama env vars found"
    echo ""
    
    echo "2. Server appsettings.Production.json:"
    APPSETTINGS="/opt/mental-health-app/server/appsettings.Production.json"
    
    echo "   HuggingFace section:"
    python3 << 'PYTHON'
import json
try:
    with open("/opt/mental-health-app/server/appsettings.Production.json", "r") as f:
        config = json.load(f)
    hf = config.get("HuggingFace", {})
    if hf:
        print(f"      ✅ Found: ApiKey={'SET' if hf.get('ApiKey') else 'NOT SET'}")
        print(f"      BioMistralModelUrl: {hf.get('BioMistralModelUrl', 'NOT SET')}")
    else:
        print("      ❌ NOT FOUND")
except Exception as e:
    print(f"      ❌ Error: {e}")
PYTHON
    
    echo ""
    echo "   Ollama section:"
    python3 << 'PYTHON'
import json
try:
    with open("/opt/mental-health-app/server/appsettings.Production.json", "r") as f:
        config = json.load(f)
    ollama = config.get("Ollama", {})
    if ollama:
        print(f"      ✅ Found: BaseUrl={ollama.get('BaseUrl', 'NOT SET')}")
    else:
        print("      ❌ NOT FOUND")
except Exception as e:
    print(f"      ❌ Error: {e}")
PYTHON
    
    echo ""
    echo "3. Database: Which Provider is Active?"
    echo "   (Checking AIModelConfigs table...)"
    
    # Extract DB password
    DB_PASS=$(python3 << 'PYTHON'
import json
import re
try:
    with open("/opt/mental-health-app/server/appsettings.Production.json", "r") as f:
        config = json.load(f)
    conn = config.get("ConnectionStrings", {}).get("MySQL", "")
    m = re.search(r'password=([^;]+)', conn)
    print(m.group(1) if m else "")
except:
    print("")
PYTHON
)
    
    if [ -n "$DB_PASS" ]; then
        mysql -u mentalhealth_user -p"$DB_PASS" mentalhealthdb -e "
        SELECT 
            amc.Id,
            amc.ModelName,
            amc.Provider,
            amc.ApiEndpoint,
            amc.IsActive,
            CASE 
                WHEN amc.Provider = 'Ollama' THEN '⚠️  USING OLLAMA (SLOW)'
                WHEN amc.Provider = 'HuggingFace' THEN '✅ USING HUGGINGFACE (FAST)'
                ELSE amc.Provider
            END as Status
        FROM AIModelConfigs amc
        WHERE amc.IsActive = 1
        ORDER BY amc.Id;
        " 2>/dev/null || echo "   ❌ Could not query database"
    else
        echo "   ❌ Could not extract database password"
    fi
    
    echo ""
    echo "4. Recent Logs - Which Provider Was Used?"
    echo "   (Last 5 AI calls...)"
    journalctl -u mental-health-app --since "1 hour ago" | grep -iE "Calling.*model|Ollama|HuggingFace" | tail -5 || echo "   No recent AI calls found"
ENDSSH

echo ""
echo "✅ Check complete!"

