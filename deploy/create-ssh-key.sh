#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Script to create ~/.ssh/id_rsa file on DigitalOcean server and append "hello" to it

SSH_KEY="$HOME/.ssh/id_rsa"

echo "🔧 Creating SSH key file on DigitalOcean server..."

ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << 'ENDSSH'
    SSH_DIR="$HOME/.ssh"
    SSH_KEY_FILE="$SSH_DIR/id_rsa"

    echo "Creating SSH key file on server..."

    # Create .ssh directory if it doesn't exist
    if [ ! -d "$SSH_DIR" ]; then
        echo "Creating .ssh directory..."
        mkdir -p "$SSH_DIR"
        chmod 700 "$SSH_DIR"
    fi

    # Create the file if it doesn't exist, or append to it if it does
    if [ ! -f "$SSH_KEY_FILE" ]; then
        echo "Creating $SSH_KEY_FILE..."
        touch "$SSH_KEY_FILE"
        chmod 600 "$SSH_KEY_FILE"
    fi

    # Append "token" to the file
    echo "Appending 'ssh-rsa token' to $SSH_KEY_FILE..."
    echo "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAACAQC2qjC4fE69JC0z6y48pqUZCw04UunQ/yQMrCUzWyEFaEtyXQXXQU/gvDSxWexpB+1ixS4CFMBG6cKC1HChyg4xCDGZoZKGMDe8u6e7GzOMtxqqvTpazx63F+selTycFhokqGRGixvXyU7DSgYUrBbAhqDRoD7UnJvBqAlXY/6Y9yhKgDr3z2jeAST1zS35quiZRDWqOxM7rgReVFYg2NrqA1LykUxV6idY8dGUGjQN/UvdEv6f4VMRMafuquKK/ndgy4n5hxUpUMeKPqFE7utQ70xqWxZkw3hND5iVG2MbTV5ayr8Xv4Bho/UH/g0RrkR0TA43eVPsVrs/G79sCFRCAo0wY5jDefbnLqMRId8jQNl/4lO1X6b0cvK9/jqjr/tW1Q/BI28d4ssV+RoOKDvwjGD3tT8NzkXJX9/o1Hv5vWR5dQ1Fv5seAxXwvXMibHeLEcdYxC8ut5E9YA2v9cproY8q/lOTvfmlKitMMQHJNFaflVrC961fiUNgZpbnknapzdpU0fuYYor8ac/cuRTvoaEu4w8ssflXWgYWgwA3BzdK1DC4rXPTwlDMai1zah0L/2CmZexYx9Tm02vGUA6C3JyoFNE0vBs6LDM1Qr3eoTo/59d4nNp8KtlnCJormwjsNwpaxK+hEVxjFncRYpT7zIdnQz2WgFYVImrQ5QIhbQ== iqkhan@yahoo.com" >> "$SSH_KEY_FILE"

    echo "✅ Done! File created/updated at: $SSH_KEY_FILE"
    echo ""
    echo "File contents:"
    cat "$SSH_KEY_FILE"
    echo ""
    echo "File permissions:"
    ls -la "$SSH_KEY_FILE"
ENDSSH

echo ""
echo "✅ Script complete!"

