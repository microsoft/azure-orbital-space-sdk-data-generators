# Geospatial Images

## Using the Geospatial Images Data Generator in your app
Using the Geospatial Images Data Generator requires the Plugin, the Plugin Config, the Data Generator container, and the Data Generator Deployment yaml.  The spacefx-dev container feature can be used to automatically download and deploy these with the below configuration:
```json
	"features": {
		"ghcr.io/microsoft/azure-orbital-space-sdk/spacefx-dev:0.11.0": {
            "app_name": "MyAwesomeApp",
            "download_artifacts": "GeospatialImages.proto, datagenerator-geospatial-images.yaml, geospatial-images-vth-plugin.dll, geospatial-images-vth-plugin.json.spacefx_plugin",
            "pull_containers": "datagenerator-geospatial-images:0.11.0-nightly"
		}
	},
```

## Geospatial Images Data Generator Source Code
The Geospatial Images Data Generator comprises two components:
* [Geospatial Images Data Generator](https://github.com/microsoft/azure-orbital-space-sdk-data-generators/tree/main/datagenerators/geospatial-images/datagenerator)
* [Geospatial Images VTH Plugin](https://github.com/microsoft/azure-orbital-space-sdk-data-generators/tree/main/datagenerators/geospatial-images/plugin)

The data generator is a Flask App that contains a repository of geospatial imagery available for querying, while the VTH Plugin is used to interact with the Flask App and download any of the geospatial images stored within the datagenerator, then send them via Link Service to the requesting Payload App.

## Building the Geospatial Images Data Generator (from source)
>:speech_balloon: The images, plugins, and artifacts are already built and pushed to the github container registry via our CI/CD process.  These steps are a reference and **not** needed to run the Geospatial Images Data Generator.  If you would like to just run the Geospatial Images Data Generator, please refer to [Geospatial Images Data Generator](https://github.com/microsoft/azure-orbital-space-sdk-data-generators/tree/main/datagenerators/geospatial-images)

1. Provision /var/spacedev
    ```bash
    # clone the azure-orbital-space-sdk-setup repo and provision /var/spacedev
    git clone https://github.com/microsoft/azure-orbital-space-sdk-setup
    cd azure-orbital-space-sdk-setup
    bash ./.vscode/copy_to_spacedev.sh
    cd -
    ```

2. Clone this repo
    ```bash
    # clone this repo
    git clone https://github.com/microsoft/azure-orbital-space-sdk-data-generators

    cd azure-orbital-space-sdk-data-generators
    ```

3. Build and push the Geospatial Images Data Generator
    ```bash
    # Trigger the build_containerImage.sh from azure-orbital-space-sdk-setup
    /var/spacedev/build/build_containerImage.sh \
        --architecture amd64 \
        --app-name datagenerator-geospatial-images \
        --image-tag 0.11.0 \
        --dockerfile Dockerfiles/Dockerfile \
        --repo-dir ${PWD}/datagenerators/geospatial-images/datagenerator \
        --no-push \
        --annotation-config azure-orbital-space-sdk-data-generators.yaml
    ```
    >:pencil2: the `--no-push` parameter will prevent build_containerImage.sh from pushing to a container registry.  We added it here to prevent accidental pushes when you copy-and-paste the command.  You will need to remove the `--no-push` if you want to push the final container image to a container registry

4. Build the Geospatial Images VTH Plugin
    ```bash
    # Trigger the build_app.sh from azure-orbital-space-sdk-setup
    /var/spacedev/build/dotnet/build_app.sh \
        --architecture amd64 \
        --app-project src/geospatial-images-vth-plugin.csproj \
        --app-version 0.11.0 \
        --output-dir /var/spacedev/tmp/geospatial-images-vth-plugin/output \
        --repo-dir ${PWD} \
        --devcontainer-json .devcontainer/geospatial-images-vth-plugin/devcontainer.json \
        --no-container-build \
        --no-push
    ```
    >:pencil2: the `--no-container-build` parameter means that we're just interested in the build artifacts and there's not a container image for this.  The `--no-push` is superflous since we aren't generating a container image, but kept it here for reference.

5. Copy the artifacts to their regular folders so it can be read by the containers
    ```bash
    # Put the dll, spacefx_config, and yaml in the destination directories
    sudo mkdir -p /var/spacedev/plugins/vth
    sudo mkdir -p /var/spacedev/yamls/deploy
    sudo mkdir -p /var/spacedev/protos/datagenerator/geospatial-images

    sudo cp /var/spacedev/tmp/geospatial-images-vth-plugin/output/amd64/app/geospatial-images-vth-plugin.dll /var/spacedev/plugins/vth/
    sudo cp /var/spacedev/tmp/geospatial-images-vth-plugin/output/amd64/app/geospatial-images-vth-plugin.json.spacefx_plugin /var/spacedev/plugins/vth/
    sudo cp ${PWD}/datagenerators/geospatial-images/datagenerator/k3s/datagenerator-geospatial-images.yaml /var/spacedev/yamls/deploy/
    sudo cp ${PWD}/datagenerators/geospatial-images/plugin/src/Protos/GeospatialImages.proto /var/spacedev/protos/datagenerator/geospatial-images/
    ```

6. (Optional) Push the build artifacts to the container registry
    >:heavy_exclamation_mark: the next step will push the build artifacts to the first container registry you have write access to, including our automated channel tagging and patching aliases.  Do not run this step unless you intend to deploy the artifacts to a container registry for others to consume.
    ```bash
    /var/spacedev/build/push_build_artifact.sh --artifact /var/spacedev/plugins/vth/geospatial-images-vth-plugin.dll --annotation-config azure-orbital-space-sdk-data-generators.yaml --architecture amd64 --artifact-version 0.11.0
    /var/spacedev/build/push_build_artifact.sh --artifact /var/spacedev/plugins/vth/geospatial-images-vth-plugin.json.spacefx_plugin --annotation-config azure-orbital-space-sdk-data-generators.yaml --architecture amd64 --artifact-version 0.11.0
    /var/spacedev/build/push_build_artifact.sh --artifact /var/spacedev/yamls/deploy/datagenerator-geospatial-images.yaml --annotation-config azure-orbital-space-sdk-data-generators.yaml --architecture amd64 --artifact-version 0.11.0
    /var/spacedev/build/push_build_artifact.sh --artifact /var/spacedev/protos/datagenerator/geospatial-images/GeospatialImages.proto --annotation-config azure-orbital-space-sdk-data-generators.yaml --architecture amd64 --artifact-version 0.11.0
    ```


