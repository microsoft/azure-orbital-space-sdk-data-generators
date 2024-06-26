ARG VARIANT="3.9"
FROM mcr.microsoft.com/cbl-mariner/base/python:${VARIANT}
ARG SERVICE_NAME="datagenerator-planetary-computer"
ENV SERVICE_NAME=$SERVICE_NAME
ENV PC_SDK_SUBSCRIPTION_KEY=$PC_SDK_SUBSCRIPTION_KEY
ENV PYTHONUNBUFFERED 1

WORKDIR /workspaces/${SERVICE_NAME}

COPY ./README.md /workspaces/${SERVICE_NAME}
COPY ./src /workspaces/${SERVICE_NAME}/src
COPY ./pyproject.toml /workspaces/${SERVICE_NAME}

RUN pip3 install .

ENTRYPOINT ["python3", "src/datagenerator_planetary_computer/main.py"]