import os
import re
import pathlib
import shutil
import rasterio
import json
from flask import abort, Flask, make_response, request, send_file
from pyproj import Transformer
from waitress import serve

from datagenerator_geospatial_images.config import PORT


APP_ = Flask(__name__)
DATA_DIR_ = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), 'data')


def print_tree(directory):
    for root, dirs, files in os.walk(directory):
        level = root.replace(directory, '').count(os.sep)
        indent = ' ' * 4 * (level)
        print(f"{indent}{os.path.basename(root)}/")
        subindent = ' ' * 4 * (level + 1)
        for file in files:
            print(f"{subindent}{file}")

def display_data():
    print(f"Data directory: {DATA_DIR_}")
    print_tree(DATA_DIR_)


@APP_.route('/get_geotiff')
def get_geotiff():
    lat = float(request.args.get('lat'))
    lon = float(request.args.get('lon'))
    print(f"Recieved geotiff file request at [{lat}, {lon}]")
    geotiff_data_dir = os.path.join(DATA_DIR_, 'geotiffs')

    # Greedily find the first geotiff that contains the desired lat/lon
    for filename in os.listdir(geotiff_data_dir):
        geotiff_path = os.path.join(geotiff_data_dir, filename)
        with rasterio.open(geotiff_path) as geotiff:
            print (geotiff.crs)
            transformer = Transformer.from_crs("EPSG:4326", geotiff.crs, always_xy=True)
            xx, yy = transformer.transform(lon, lat)  # lon, lat order is intentional
            bounding_box = geotiff.bounds
            print(geotiff_path)
            print(bounding_box.left, bounding_box.right, bounding_box.bottom, bounding_box.top)
            # if the transformed lat/lon location is in the geotiff
            if (xx > bounding_box.left and xx < bounding_box.right) and (yy > bounding_box.bottom and yy < bounding_box.top):
                # if there is valid data in the geotiff at the transformed lat/lon location
                if list(geotiff.sample([(xx, yy)])) != geotiff.nodatavals:
                    print(f"Sending {geotiff_path} as response for request at [{lat}, {lon}]")
                    file_name, file_extention = os.path.splitext(geotiff_path)
                    response = send_file(geotiff_path, mimetype='image/'+file_extention[1:], as_attachment=True)
                    return response

    print(f"No suitable geotiff file found at [{lat}, {lon}]")
    return abort(400, f"No suitable geotiff found at [{lat}, {lon}]")


@APP_.route('/get_image')
def get_image():
    lat = float(request.args.get('lat'))
    lon = float(request.args.get('lon'))
    print(f"Recieved image file request at [{lat}, {lon}]")
    image_data_dir = os.path.join(DATA_DIR_, 'images')

    # Greedily find the first geotiff that contains the desired lat/lon
    for filename in os.listdir(image_data_dir):

        if not filename.endswith("json"):
            continue
        
        json_path = os.path.join(image_data_dir, filename)
        with open(json_path) as json_data:
            data = json.load(json_data)
            transformer = Transformer.from_crs("EPSG:4326", data["crs"], always_xy=True)
            xx, yy = transformer.transform(lon, lat)  # lon, lat order is intentional
            bounding_box = data["bbox"]
            # if the transformed lat/lon location is in the geotiff
            if (xx > bounding_box["left"] and xx < bounding_box["right"]) and (yy > bounding_box["bottom"] and yy < bounding_box["top"]):
                image_file_path = os.path.join(image_data_dir, data["datafile"])
                # if there is valid image file assocaiated with the json
                if os.path.isfile(image_file_path):
                    print(f"Sending {image_file_path} as response for request at [{lat}, {lon}]")
                    file_name, file_extention = os.path.splitext(image_file_path)
                    response = send_file(image_file_path, mimetype='image/'+file_extention[1:], as_attachment=True)
                    return response

    print(f"No suitable image found at [{lat}, {lon}]")
    return abort(400, f"No suitable image found at [{lat}, {lon}]")

def main():
    host = '0.0.0.0'
    print(f"Running application at {host}:{PORT}")
    display_data()
    serve(APP_, host=host, port=PORT)


if __name__ == '__main__':
    main()