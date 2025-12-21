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

sudo systemctl restart ollama

#restart from server -app
systemctl restart mental-health-app

#restart from server -nginx
nginx -t && systemctl reload nginx

#tail from mac
cd /Users/mohammedkhan/iq/health && sleep 3 && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@45.55.248.45 journalctl -u mental-health-app -f -n 20

#tail from the server - this works
journalctl -u mental-health-app -f -n 20

=======================secret github instructions======================
Create a CI-only SSH key on your Mac

On your local Mac terminal, run:

ssh-keygen -t ed25519 -C "github-actions-ci" -f ~/.ssh/do_github_ci

You’ll see prompts:

Enter passphrase (empty for no passphrase):

For CI, you usually leave this empty (just press Enter twice), otherwise GitHub can’t unlock it.

This creates two files:

~/.ssh/do_github_ci → private key

~/.ssh/do_github_ci.pub → public key

You can confirm:

ls ~/.ssh/do_github_ci\*

2️⃣ Add the public key to your DigitalOcean droplet

Now we give your droplet the public key, so it accepts SSH from CI.

First, show the public key on your Mac:

cat ~/.ssh/do_github_ci.pub

Copy the entire line (starts with ssh-ed25519 ...).

Now SSH into your droplet (the way you normally do it now):

ssh root@146.190.166.198

Once logged in, run:

mkdir -p ~/.ssh
chmod 700 ~/.ssh
echo "PASTE_PUBLIC_KEY_HERE" >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys

⚠️ Replace PASTE_PUBLIC_KEY_HERE with the exact ssh-ed25519 ... line you copied (put it in quotes).

Example:

echo "ssh-ed25519 AAAAC3Nz... github-actions-ci" >> ~/.ssh/authorized_keys

Exit the server:

exit

3️⃣ Test the new key from your Mac

Before involving GitHub, test that this new key works:

ssh -i ~/.ssh/do_github_ci root@146.190.166.198

If you get in without issues → the key is correctly installed.
Type exit to leave.

4️⃣ Add the private key to GitHub Secrets as SSH_PRIVATE_KEY

Now we give GitHub Actions the private half of this key.

On your Mac:

cat ~/.ssh/do_github_ci

Copy everything, including:

-----BEGIN OPENSSH PRIVATE KEY-----
...
-----END OPENSSH PRIVATE KEY-----

Now in your GitHub repo:

Go to Settings (of the repo, not your account)

Left side → Secrets and variables → Actions

Click New repository secret

Create these secrets:

a) SSH_PRIVATE_KEY

Name: SSH_PRIVATE_KEY

Value: paste the entire private key you just copied.

b) SSH_HOST

Name: SSH_HOST

Value: 146.190.166.198

c) SSH_USER

Name: SSH_USER

Value: root
(later you can create a deploy user, but root is fine for now)

You now have 3 secrets:

SSH_PRIVATE_KEY

SSH_HOST

SSH_USER

5️⃣ Use the key in GitHub Actions (example workflow)

In your repo, create (or edit):

.github/workflows/deploy-dev.yml

Here’s a minimal example that:

Loads your SSH key

Connects to your droplet

(You can later add build/publish & deploy steps.)

name: Deploy to Dev Droplet

on:
push:
branches: [ "dev", "develop" ] # adjust to your dev branch name

jobs:
deploy-dev:
runs-on: ubuntu-latest

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Start SSH agent and add key
        uses: webfactory/ssh-agent@v0.9.0
        with:
          ssh-private-key: ${{ secrets.SSH_PRIVATE_KEY }}

      - name: Test SSH connection
        run: |
          ssh -o StrictHostKeyChecking=no ${{ secrets.SSH_USER }}@${{ secrets.SSH_HOST }} "echo 'Connected from GitHub Actions to $(hostname)'"

Push this file to your repo, then:

Go to Actions tab in GitHub

Click the workflow run

You should see Connected from GitHub Actions to ... in the logs

Once that works, you can extend this job to:

dotnet publish your app

rsync the publish folder to /var/www/yourapp on the droplet

ssh in and systemctl restart yourapp.service

6️⃣ (Optional, but good later) Separate Dev / Test / Prod

Later, when you add a Test or Prod droplet, you can:

Generate separate keys for each (e.g., do_github_test, do_github_prod)

Add secrets:

DEV_SSH_PRIVATE_KEY, DEV_SSH_HOST, DEV_SSH_USER

PROD_SSH_PRIVATE_KEY, PROD_SSH_HOST, PROD_SSH_USER

Select which to use based on branch or environment.

============very important---./deploy/setup-staging-connectionstring.sh
========this file is used in case we jack up the db password for what ever
==reason

#Please note at the end after running
consolidated-deploy.sh
run
ollama tinyllama:latest (or what ever llm you want to use)
after that run
fix-ollama-not-found.sh
