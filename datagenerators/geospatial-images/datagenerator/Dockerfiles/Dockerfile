ARG PYTHON_CONTAINER_VARIANT="3.10-bullseye"
FROM mcr.microsoft.com/vscode/devcontainers/python:0-${PYTHON_CONTAINER_VARIANT}

ARG SERVICE_NAME="datagenerator-geospatial-images"
ENV SERVICE_NAME=$SERVICE_NAME
ENV PYTHONUNBUFFERED 1

WORKDIR /workspace/${SERVICE_NAME}

# GDAL needed for rasterio
RUN apt-get update \
   && apt-get -y install libgdal-dev \
   && rm -rf /var/lib/apt/lists/*

COPY ./src /workspace/${SERVICE_NAME}/src
COPY ./data /workspace/${SERVICE_NAME}/data
COPY ./config /workspace/${SERVICE_NAME}/config
COPY ./pyproject.toml /workspace/${SERVICE_NAME}
COPY ./README.md /workspace/${SERVICE_NAME}/README.md

RUN pip3 install .

ENTRYPOINT ["python3", "src/datagenerator_geospatial_images/main.py"]