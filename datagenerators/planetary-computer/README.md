# Planetary Computer Data Generator

Using the planetary computer with the Microsoft Azure Orbital Space SDK requires two components:
* [Planetary Computer Data Generator](https://github.com/microsoft/azure-orbital-space-sdk-data-generators/tree/main/datagenerators/planetary-computer/datagenerator)
* [Planetary Computer VTH Plugin](https://github.com/microsoft/azure-orbital-space-sdk-data-generators/tree/main/datagenerators/planetary-computer/plugin)

# Using the Planetary Data Generator in your app
Using the Planetary Computer Data Generator requires the Plugin, the Plguin Config, the Data Generator container, and the Data Generator Deployment yaml.  The spacefx-dev container feature can be used to automatically download and deploy these with the below configuration:
```json
	"features": {
		"ghcr.io/microsoft/azure-orbital-space-sdk/spacefx-dev:0.11.0": {
            "app_name": "MyAwesomeApp",
			"download_artifacts": "PlanetaryComputer.proto, datagenerator-planetary-computer.yaml, planetary-computer-vth-plugin.dll, planetary-computer-vth-plugin.json.spacefx_plugin",
            "pull_containers": "datagenerator-planetary-computer:0.11.0-nightly"
		}
	},
```

## Building the Planetary Computer Data Generator
1. Provision /var/spacedev
    ```bash
    # clone the azure-orbital-space-sdk-setup repo and provision /var/spacedev
    git clone https://github.com/microsoft/azure-orbital-space-sdk-setup
    cd azure-orbital-space-sdk-setup
    bash ./.vscode/copy_to_spacedev.sh
    cd -
    ```

1. Clone this repo
    ```bash
    # clone this repo
    git clone https://github.com/microsoft/azure-orbital-space-sdk-data-generators

    cd azure-orbital-space-sdk-data-generators
    ```

1. Build and push the Planetary Computer Data Generator
    ```bash
    # Trigger the build_containerImage.sh from azure-orbital-space-sdk-setup
    /var/spacedev/build/build_containerImage.sh \
        --architecture amd64 \
        --app-name datagenerator-planetary-computer \
        --image-tag 0.11.0 \
        --dockerfile Dockerfiles/Dockerfile \
        --repo-dir ${PWD}/datagenerators/planetary-computer/datagenerator \
        --annotation-config azure-orbital-space-sdk-data-generators.yaml
    ```

1. Build the Planetary Computer VTH Plugin
    ```bash
    # Trigger the build_app.sh from azure-orbital-space-sdk-setup
    /var/spacedev/build/dotnet/build_app.sh \
        --architecture amd64 \
        --app-project src/planetary-computer-vth-plugin.csproj \
        --app-version 0.11.0 \
        --output-dir /var/spacedev/tmp/planetary-computer-vth-plugin/output \
        --repo-dir ${PWD} \
        --devcontainer-json .devcontainer/planetary-computer-vth-plugin/devcontainer.json \
        --no-container-build
    ```

1. Copy the plugin and datagenerator artifacts to the vth plugin folder and then push them to the container registry
    ```bash
    # Put the dll, spacefx_config, and yaml in the destination directories
    sudo mkdir -p /var/spacedev/plugins/vth
    sudo mkdir -p /var/spacedev/yamls/deploy
    sudo mkdir -p /var/spacedev/protos/datagenerator/planetary-computer

    sudo cp /var/spacedev/tmp/planetary-computer-vth-plugin/output/amd64/app/planetary-computer-vth-plugin.dll /var/spacedev/plugins/vth/
    sudo cp /var/spacedev/tmp/planetary-computer-vth-plugin/output/amd64/app/planetary-computer-vth-plugin.json.spacefx_plugin /var/spacedev/plugins/vth/
    sudo cp ${PWD}/datagenerators/planetary-computer/datagenerator/k3s/datagenerator-planetary-computer.yaml /var/spacedev/yamls/deploy/
    sudo cp ${PWD}/datagenerators/planetary-computer/plugin/src/Protos/PlanetaryComputer.proto /var/spacedev/protos/datagenerator/planetary-computer/


    # Push dll, spacefx_plugin, and yaml files to the container registry
    /var/spacedev/build/push_build_artifact.sh --artifact /var/spacedev/plugins/vth/planetary-computer-vth-plugin.dll --annotation-config azure-orbital-space-sdk-data-generators.yaml --architecture amd64 --artifact-version 0.11.0
    /var/spacedev/build/push_build_artifact.sh --artifact /var/spacedev/plugins/vth/planetary-computer-vth-plugin.json.spacefx_plugin --annotation-config azure-orbital-space-sdk-data-generators.yaml --architecture amd64 --artifact-version 0.11.0
    /var/spacedev/build/push_build_artifact.sh --artifact /var/spacedev/yamls/deploy/datagenerator-planetary-computer.yaml --annotation-config azure-orbital-space-sdk-data-generators.yaml --architecture amd64 --artifact-version 0.11.0
    /var/spacedev/build/push_build_artifact.sh --artifact /var/spacedev/protos/datagenerator/planetary-computer/PlanetaryComputer.proto --annotation-config azure-orbital-space-sdk-data-generators.yaml --architecture amd64 --artifact-version 0.11.0
    ```

