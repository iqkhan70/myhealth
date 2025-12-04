#!/bin/bash

# Go to health folder and run it from there that is fine
./deploy/create-ssh-key.sh

./deploy/deploy-all.sh

./deploy/install-ef-tool.sh

./deploy/fix-jwt-now.sh

./deploy/update-ai-model-endpoints.sh

./deploy/check-ollama-models.sh

./deploy/generate-schema-sync.sh

./deploy/add-isactive-column.sh

./deploy/add-chatmessages-isactive.sh

./deploy/add-clinicalnotes-columns.sh

./deploy/check-and-fix-all-schema.sh

./deploy/install-ollama.sh

./deploy/fix-ollama-config-correct.sh

./deploy/create-appsettingprod.sh




