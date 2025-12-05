#to open the cert from digital ocean machine
#got to /opt/mentalhealth\* - there will be a certs folder from there
openssl x509 -in server.crt -text -noout

#query digital occean machine
ssh root@146.190.166.198 "mysql -u mentalhealth*user -p\$(grep -A 1 '\"MySQL\"' /opt/mental-health-app/server/appsettings.Production.json | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '\"' | tr -d '}') mentalhealthdb -e \"select \_ from Users ;\""

#query digital occean machine
ssh root@146.190.166.198 "mysql -u mentalhealth*user -p\$(grep -A 1 '\"MySQL\"' /opt/mental-health-app/server/appsettings.Production.json | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '\"' | tr -d '}') mentalhealthdb -e \"select \_ from UserRequests ;\""

#curl reference
curl -v -X POST "https://45.55.71.169/api/clinicalnotes" \
 -H "Content-Type: application/json" \
 -d '{
"encounterData": "test encounter from curl",
"patientId": 3,
"context": "ClinicalNote"
}'

#restart from mac
ssh -i ~/.ssh/id_rsa root@143.198.148.50 "systemctl restart mental-health-app" # Restart app

#restart from server -app
systemctl restart mental-health-app

#restart from server -nginx
nginx -t && systemctl reload nginx 

#tail from mac
cd /Users/mohammedkhan/iq/health && sleep 3 && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@45.55.248.45 "journalctl -u mental-health-app --no-pager -n 20 | tail -20" 2>/dev/null

#tail from the server
journalctl -u mental-health-app --no-pager -n 20 | tail -20" 2>/dev/null
