import configparser


config = configparser.ConfigParser()
config.read('/workspace/geospatial-images-datagenerator/config/config.ini')

PORT = int(config['IMAGE_PROVIDER']['PORT'])  # required