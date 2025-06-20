#!/bin/bash

if [ "$1" = "personal" ]; then
  git remote set-url origin https://github.com/timothytenzin20/S25-USRA-FDMDigitalTwin-Unity.git
  echo "Switched 'origin' remote to PERSONAL repository."
elif [ "$1" = "work" ]; then
  git remote set-url origin https://github.com/DIIM-Lab/DualDT4Quality.git
  echo "Switched 'origin' remote to WORK repository."
else
  echo "Usage: $0 [personal|work]"
fi
