#!/usr/bin/env bash
# Modo simulador SOLO para una corrida de tests del CI de Unity (.github/workflows/ci-unity.yml, QA-V2-14 de la It. 2).
# Hace lo mismo que SimulatorDefine.EnableForTests / RestoreAfterTests (tools/unity/run-unity-tests.ps1 -Simulator), sin abrir
# Unity, en el checkout efimero del runner:
#   enable:  agrega ROWMATE_SIMULATOR a scriptingDefineSymbols (Android, Standalone, iPhone) de ProjectSettings.asset y deja
#            la marca Library/RowMate/simulator-tests.txt. Con la marca, SimulatorBuildGuard frena cualquier build, y
#            CommittedSettingsDoNotEnableTheSimulator pasa solo si Unity recibe -rowmateSimulatorTestRun.
#   restore: deja ProjectSettings.asset como en el commit y borra la marca (antes de que actions/cache guarde Library/).
# Nunca se commitea: si el define quedara versionado, el CI falla (CommittedSettingsDoNotEnableTheSimulator).
set -euo pipefail

settings="ProjectSettings/ProjectSettings.asset"
marker="Library/RowMate/simulator-tests.txt"
symbol="ROWMATE_SIMULATOR"

enable() {
    awk -v sym="$symbol" '
        function add(value) {
            if (value == "") return sym
            if (index(";" value ";", ";" sym ";") > 0) return value
            return value ";" sym
        }
        /^  scriptingDefineSymbols: *\{\} *$/ {
            print "  scriptingDefineSymbols:"
            print "    Android: " sym
            print "    Standalone: " sym
            print "    iPhone: " sym
            next
        }
        /^  scriptingDefineSymbols: *$/ { inblock = 1; print; next }
        inblock && /^    [A-Za-z]+:/ {
            line = $0
            sub(/[ \t\r]+$/, "", line)
            n = index(line, ":")
            value = substr(line, n + 1)
            sub(/^ +/, "", value)
            print substr(line, 1, n) " " add(value)
            next
        }
        { inblock = 0; print }
    ' "$settings" > "$settings.tmp"
    mv "$settings.tmp" "$settings"
    for target in Android Standalone iPhone; do
        if ! grep -Eq "^    $target: (.*;)?$symbol(;.*)?$" "$settings"; then
            echo "::error::no se pudo agregar $symbol a $target en $settings"
            exit 1
        fi
    done
    mkdir -p "$(dirname "$marker")"
    printf '%s\n' "# RowMate (QA-V2-14): corrida de tests del simulador en el CI de Unity; ninguna plataforma lo tenia antes" > "$marker"
    echo "[ci-simulator] $symbol activado solo para esta corrida (marca: $marker)"
}

restore() {
    git checkout -- "$settings"
    rm -f "$marker" 2>/dev/null || sudo rm -f "$marker"
    if grep -q "$symbol" "$settings"; then
        echo "::error::$settings sigue con $symbol"
        exit 1
    fi
    echo "[ci-simulator] $settings como en el commit y sin la marca"
}

case "${1:-}" in
    enable) enable ;;
    restore) restore ;;
    *) echo "uso: $0 enable|restore"; exit 2 ;;
esac
