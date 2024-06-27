#!/usr/bin/env bash
#-------------------------------------------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See https://go.microsoft.com/fwlink/?linkid=2090316 for license information.
#-------------------------------------------------------------------------------------------------------------
#
# Docs: https://github.com/microsoft/azure-orbital-space-sdk-setup/README.md
# Checks that the debug shim has been deployed.  If not, it will build it and deploy

#-------------------------------------------------------------------------------------------------------------
# Script initializing
set -e

# If we're running in the devcontainer with the k3s-on-host feature, source the .env file
[[ -f "/devfeature/k3s-on-host/.env" ]] && source /devfeature/k3s-on-host/.env

# Pull in the app.env file built by the feature
[[ -n "${SPACEFX_DEV_ENV}" ]] && [[ -f "${SPACEFX_DEV_ENV}" ]] && source "${SPACEFX_DEV_ENV:?}"


set +e
#-------------------------------------------------------------------------------------------------------------

############################################################
# Script variables
############################################################
DATA_GENERATOR_NAME="datagenerator-planetary-computer"
DATA_GENERATOR_NAMESPACE="platformsvc"
REGISTRY_NAME=""
DATA_GENERATOR_WORKING_DIR="/workspace/planetary-computer-vth-datagenerator"
DOCKER_FILENAME="${DATA_GENERATOR_WORKING_DIR}/Dockerfiles/Dockerfile"
DATA_GENERATOR_DEPLOYMENT_NAME="${DATA_GENERATOR_NAME}"
K3S_FILE="${DATA_GENERATOR_WORKING_DIR}/k3s/datagenerator-planetary-computer.yaml"
FORCE_DEPLOY=false


source "${SPACEFX_DIR:?}/modules/load_modules.sh" $@ --log_dir "${SPACEFX_DIR:?}/logs/${APP_NAME:?}"


############################################################
# Process the input options.
############################################################
while [[ "$#" -gt 0 ]]; do
    case $1 in
        --force-deploy)
            FORCE_DEPLOY=true
        ;;
        *) echo "Unknown parameter '$1'"; show_help ;;
    esac
    shift
done


############################################################
# Check for the debugshim pod and deploy if not found
############################################################
function build_image() {
    info_log "START: ${FUNCNAME[0]}"

    info_log "Calcuating the registry URL..."
    run_a_script "yq '.global.containerRegistryInternal' ${SPACEFX_DIR}/chart/values.yaml" _registry_url
    run_a_script "yq '.global.containerRegistry' ${SPACEFX_DIR}/chart/values.yaml" _registry_url_for_host
    info_log "Registry URL: '${_registry_url}'"

    info_log "Checking for datagenerator image '${DATA_GENERATOR_NAME}' in the registry..."
    run_a_script "regctl repo list ${_registry_url} | grep '${DATA_GENERATOR_NAME}'" _image_exists --ignore_error

    if [[ -n "${_image_exists}" ]]; then
        if [[ "${FORCE_DEPLOY}" == "false" ]]; then
            info_log "Found '${DATA_GENERATOR_NAME}' in '${_registry_url}'.  Nothing to do."
            info_log "END: ${FUNCNAME[0]}"
            return
        fi
        info_log "Found '${DATA_GENERATOR_NAME}' in '${_registry_url}', but FORCE_DEPLOY = 'True'.  Rebuilding Image"
    else
        info_log "...'${DATA_GENERATOR_NAME}' not found in '${_registry_url}'.  Building image..."
    fi


    run_a_script "docker build -t ${_registry_url_for_host}/${DATA_GENERATOR_NAME}:${SPACEFX_VERSION} -f ${DOCKER_FILENAME} ${DATA_GENERATOR_WORKING_DIR}"

    info_log "...'${DATA_GENERATOR_NAME}' built.  Pushing to '${_registry_url}'..."
    run_a_script "docker push ${_registry_url_for_host}/${DATA_GENERATOR_NAME}:${SPACEFX_VERSION}"

    info_log "...'${DATA_GENERATOR_NAME}' pushed to '${_registry_url}'"


    info_log "END: ${FUNCNAME[0]}"
}


function main() {
    info_log "Verifying deployment '${DATA_GENERATOR_DEPLOYMENT_NAME}' is found..."

    run_a_script "kubectl get deployments -A -o json | jq -r '.items[] | select(.metadata.name == \"${DATA_GENERATOR_DEPLOYMENT_NAME}\") | .status.conditions[] | select(.type == \"Available\") | .status'" _deployment_status --ignore_error


    if [[ "${_deployment_status}" == "True" ]]; then
        if [[ "${FORCE_DEPLOY}" == "false" ]]; then
            info_log "...deployment '${DATA_GENERATOR_DEPLOYMENT_NAME}' is found and available.  Nothing to do"
            info_log "------------------------------------------"
            info_log "END: ${SCRIPT_NAME}"
            return
        else
            info_log "...deployment '${DATA_GENERATOR_DEPLOYMENT_NAME}' is found and 'FORCE_DEPLOY' = TRUE.  Deleting deployment..."
            run_a_script "kubectl delete deployment/${DATA_GENERATOR_DEPLOYMENT_NAME} -n ${DATA_GENERATOR_NAMESPACE}"
            info_log "...deployment '${DATA_GENERATOR_DEPLOYMENT_NAME}' successfully deleted."
        fi
    fi

    info_log "...deployment '${DATA_GENERATOR_DEPLOYMENT_NAME}' not found.  Building and deploying..."

    build_image

    info_log "Deploying '${DATA_GENERATOR_DEPLOYMENT_NAME}' via '${K3S_FILE}'..."
    run_a_script "kubectl apply -f ${K3S_FILE}"

    wait_for_deployment --deployment ${DATA_GENERATOR_DEPLOYMENT_NAME} --namespace platformsvc

    info_log "Deployment '${DATA_GENERATOR_DEPLOYMENT_NAME}' is now available"


    info_log "------------------------------------------"
    info_log "END: ${SCRIPT_NAME}"
}


main
