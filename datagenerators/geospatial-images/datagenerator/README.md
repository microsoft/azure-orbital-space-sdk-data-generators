# Geospatial Images

This repository provides a simple data generator that, given a location expressed as `[lat, lon]`, returns the first requested file type (image, geotiff) in the `data` directory that contains that location or returns an `HTTP 400` response is returned if no such file is found.

## Configuration

Configurable values for the Geospatial Images may be viewed and edited in `config/config.ini` as needed

~~~~
[GEOSPATIAL_IMAGES]
PORT            (Integer)                           The port that will be used to host the service
~~~~

## Installation

### Devcontainer Setup

1. Re-open this repository within the provided devcontainer.

## Execution

1. execute the image-provider via:
    ```python3 ./src/datagenerator_geospatial_images/main.py```


1. request a geotiff for a desired lat/long via:
    http://127.0.0.1:8080/get_geotiff?lat=47.6062&lon=-122.3321

    This can be done in a terminal via:
    ```curl "http://127.0.0.1:8080/get_geotiff?lat=47.6062&lon=-122.3321" --output output.tif```

    Note that your port may vary depending upon the `PORT` value specified in `config/config.ini`


1. request a image for a desired lat/long via:
    http://127.0.0.1:8080/get_image?lat=47.6062&lon=-122.3321

    This can be done in a terminal via:
    ```curl "http://127.0.0.1:8080/get_image?lat=47.6062&lon=-122.3321" --output output.jpg```

    Note that your port may vary depending upon the `PORT` value specified in `config/config.ini`


## Inputting your own Data

### Geotiffs

Place your tif/tiff files within  `data/geotiffs`.

### Images

Place your data files within `data/images`. Each file should be accomponied by a equivalent named json file formatted as such:
```json
{
    "datafile": "Filename for image file",
    "crs": "Define the Coordinate Reference System (CRS) the bbox values are listed in",
    "bbox": {
        "left", "right", "bottom", "top" bounding box coordinates from the above defined CRS
    }
}

```
if you are unfamilar with what `crs` and `bbox` your image utilizes, go ahead copy the bottom provided code and you will be able to query the image with the following query to `datagenerator-geospatial-images`:  ```curl "http://127.0.0.1:8080/get_image?lat=47.6062&lon=-122.3321"```

```json
{
    "datafile": <filename in data/images>,
    "crs": "EPSG:32610",
    "bbox": {
        "left": 471285.0,
        "right": 704415.0,
        "bottom": 5136885.0,
        "top": 5373315.0
    }
}
```


## Build

TODO:
```