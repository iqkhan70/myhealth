make sure this happens before running git for build
root@ubuntu-s-2vcpu-4gb-amd-sfo3-01:~/.ssh# cp authorized_keys id_rsa

#run this from the server after first deployment
ollama pull tinyllama

#make sure appsetting_env(prod, stg,dev).json
has correct entries of huggingface api key
email with correct emailid and pwd for sending emails etc
vonage for sms etc

#restart from mac
ssh -i ~/.ssh/id_rsa root@143.198.148.50 "systemctl restart mental-health-app" # Restart app

sudo systemctl restart ollama

#restart from server -app
systemctl restart mental-health-app

#restart from server -nginx
nginx -t && systemctl reload nginx
