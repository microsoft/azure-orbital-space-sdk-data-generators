#!/bin/bash
#
#  Runs after the container is started to install poetry and the local package
#
#

# Script Flags
SCRIPT_NAME=$(basename "$0")


install_poetry() {
    echo "START: ${FUNCNAME[0]}"
    curl -sSL https://install.python-poetry.org | POETRY_HOME=/root/.local python3 -
    chmod +x /root/.local/bin/poetry
    echo "END: ${FUNCNAME[0]}"
}

install_package() {
    echo "START: ${FUNCNAME[0]}"
    /root/.local/bin/poetry config virtualenvs.create false
    rm -f -- /workspace/planetary-computer-datagenerator/poetry.lock
    /root/.local/bin/poetry install --with dev
    rm -f -- /workspace/planetary-computer-datagenerator/poetry.lock
    echo "END: ${FUNCNAME[0]}"
}

clean_up() {
    echo "START: ${FUNCNAME[0]}"
    echo  "rm -rf /workspace/planetary-computer-datagenerator/.devcontainer/tmp"
    rm -rf /workspace/planetary-computer-datagenerator/.devcontainer/tmp
    echo "END: ${FUNCNAME[0]}"
}

main() {
    echo "START: ${SCRIPT_NAME}"
    echo "------------------------------------------"
    install_poetry
    install_package
    clean_up
    echo "------------------------------------------"
    echo "END: ${SCRIPT_NAME}"
}

main