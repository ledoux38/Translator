#!/usr/bin/env bash

# répertoire de base du script
BASEDIR=$(dirname "$0")

# Fonction appelée lors de l'interruption du script (CTRL+C)
function ctrl_c() {
    echo "Interruption détectée. Arrêt..."
    pkill -P $$
}

# Définit la fonction à appeler lors de la réception d'un signal INT (CTRL+C)
trap ctrl_c INT

echo "Démarrage du projet .NET..."
# Lance le projet .NET en arrière-plan
dotnet run --project ${BASEDIR}/translator.csproj &

# Attend la fin de tous les processus en arrière-plan
wait
