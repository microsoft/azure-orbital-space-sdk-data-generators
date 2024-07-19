#!/bin/bash
#
#  Runs after the container is started to finish configuration from within the container
#
#
#
# Example Usage:
#
#       bash ./.devcontainer/devcontainer_postStart.sh -d geospatial-images-datagenerator

# Script Flags
SCRIPT_NAME=$(basename "$0")
DEBUG_APP_NAME=""

############################################################
# Help                                                     #
############################################################
show_help() {
    # Display Help
    echo "Post start script to finalize configuration for data-generator"
    echo
    echo "Syntax: bash .devcontainer/devcontainer_postStart.sh --debug_app_name geospatial-images-datagenerator"
    echo "options:"
    echo "--debug_app_name | -d                         [REQUIRED] name of the app from the devcontainer.json"
  
    echo
    exit 1
}

############################################################
# Process the input options. Add options as needed.        #
############################################################
# Get the options

echo "START: processing parameters"
while [[ "$#" -gt 0 ]]; do
    case $1 in
    -h | --help) show_help ;;
    -d | --debug_app_name)
        shift
        DEBUG_APP_NAM=$1
        ;;
    *)
        echo "Unknown parameter passed: $1"
        show_help
        ;;
    esac
    shift
done

install_poetry() {
    echo "START: ${FUNCNAME[0]}"
    curl -sSL https://install.python-poetry.org | POETRY_HOME=/root/.local python3 -
    chmod +x /root/.local/bin/poetry
    echo "END: ${FUNCNAME[0]}"
}

install_package() {
    echo "START: ${FUNCNAME[0]}"
    /root/.local/bin/poetry config virtualenvs.create false
    rm -f -- /workspace/${DEBUG_APP_NAME}/poetry.lock
    /root/.local/bin/poetry install --with dev
    rm -f -- /workspace/${DEBUG_APP_NAME}/poetry.lock
    echo "END: ${FUNCNAME[0]}"
}

clean_up() {
    echo "START: ${FUNCNAME[0]}"
    echo  "rm -rf /workspace/${DEBUG_APP_NAME}/.devcontainer/tmp"
    rm -rf /workspace/${DEBUG_APP_NAME}/.devcontainer/tmp
    echo "END: ${FUNCNAME[0]}"
}

main() {
    echo "START: ${SCRIPT_NAME}"
    echo "------------------------------------------"
    install_poetry
    install_package
    clean_up
    mkdir /workspace/${DEBUG_APP_NAME}/output
    echo "------------------------------------------"
    echo "END: ${SCRIPT_NAME}"
}

main