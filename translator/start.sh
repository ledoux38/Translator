#!/usr/bin/env bash

# Résout le chemin réel du lien symbolique
REAL_PATH=$(readlink -f "$0")
BASEDIR=$(dirname "$REAL_PATH")

# Fonction appelée lors de l'interruption du script (CTRL+C)
function ctrl_c() {
    echo "Interruption détectée. Arrêt..."
    # Arrête tous les processus enfants du script courant
    pkill -P $$
}

# Définit la fonction à appeler lors de la réception d'un signal INT (CTRL+C)
trap ctrl_c INT

echo "Démarrage du projet .NET..."
cd ${BASEDIR} # Change le répertoire de travail vers celui du script
dotnet run --project ./translator.csproj &

# Attend la fin de tous les processus en arrière-plan
wait
