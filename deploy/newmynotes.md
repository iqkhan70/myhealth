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

==command to find env:
sudo systemctl show mental-health-app.service | grep ASPNETCORE_ENVIRONMENT

Please note that when we run git dev/staging, it will expect
appsettings.Staging.json

but when we build initially using consolidated script file since that cotains env as Production, it will create appsettings.Production.json and used that, in case in the initial setup if you need Staging change the script to make it Staging for Production make it Production and so on

==Also make sure mynotes in BKP folder has very valuable information
========necessaryfor GIT===============secret github instructions======================
--this is my backup file in my localmachine in case you want to add this
--and avoid below steps, please do that only for lower env
--for prod it is necessary we generate new keys etc.
/Users/mohammedkhan/iq/certs/authorized_keybackupfile.txt.pdf

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
