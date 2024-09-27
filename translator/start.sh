#!/usr/bin/env bash

REAL_PATH=$(readlink -f "$0")
BASEDIR=$(dirname "$REAL_PATH")

function ctrl_c() {
    echo "Interruption détectée. Arrêt..."
    pkill -P $$
}

trap ctrl_c INT

cd ${BASEDIR} # Change le répertoire de travail vers celui du script

# Passer tous les arguments à dotnet run
dotnet run --project ./translator.csproj -- "$@" &

wait