import json

import planetary_computer
from flask import abort, Flask, make_response, request
from pystac_client import Client
from waitress import serve

APP_ = Flask(__name__)


def get_pystac_client():
    print('Instantiating Planetary Computer Client...')
    return Client.open("https://planetarycomputer.microsoft.com/api/stac/v1", modifier=planetary_computer.sign_inplace)


@APP_.route('/<collection>/items/<float(signed=True):lat>/<float(signed=True):lon>', methods=["GET"])
def get_items(collection, lat, lon):
    """
    Given a latitude and longitude, returns a list of Planetary Computer hrefs to geotiffs that contains that location.
    Additional arguments enable broad queryability across collections, time ranges, and assets.

    Args:
        collection (string): The Planetary Computer Data Collection to query.
        lat (float): The latitude of the location for which items are to be found.
        lon (float): The longitude of the location for which items are to be found.
        asset (string): Which asset to retrieve. Can be specified multiple times to retrieve multiple assets in one query.
        max_items (int, optional): The maximum number of geotiffs to consider in the query. Defaults to 500.
        order (string, optional): Indicates which method should be used for search optimization, if any. One of 'ascending' or 'descending'. Defaults to None.
        order_by (string, optional): Indicates which pystac.item property should be used for search optimization, if any. Defaults to None.
        time_range (string, optional): A (datetime)/(datetime) string specifying the temporal bounds of geotiff to consider (ex: 2021-12-01/2022-12-31). Defaults to None.
        top (int, optional): The maximum number of geotiffs to include in the response. Defaults to 1.
    Returns:
        response (JSON string): A list of hyperlinks the requested Planetary Computer COG items
    """
    print("Received geotiff request:")
    client = get_pystac_client()

    # Initialize Client search kwargs
    bbox = [lon - 0.01, lat - 0.01, lon + 0.01, lat + 0.01] # search for geotiffs containing the point (lat, lon)

    search_kwargs = {'bbox': bbox}  # bbox is a search kwarg

    # Get Required Arguments - collection, lat, lon
    search_kwargs['collections'] = [str(collection)]  # collections is a search kwarg
    print(f"    collection = {collection}")
    print(f"    lat = {lat}")
    print(f"    lon = {lon}")

    # Get Required Arguments - assets
    assets = request.args.getlist('asset')  # assets is not a search kwarg
    if len(assets) == 0:
        print("No assets specified")
        return abort(400, "No asset specified")

    print(f"    assets = {assets}")

    # Get Optional Arguments - max_items
    max_items = request.args.get('max_items')
    max_items = max_items or 500
    search_kwargs['max_items'] = int(max_items)  # max_items is a search kwarg
    print(f"    max_items = {max_items}")

    # Get Optional Arguments - order
    order = request.args.get('order')
    if order:
        order = order.lower()  # order is not a search kwarg
        print(f"    order = {order}")

    # Get Optional Arguments - order_by
    order_by = request.args.get('order_by')  # order_by is not a search kwarg
    if order_by:
        print(f"    order_by = {order_by}")

    # Enforce both order and order_by being present if provided
    if not (bool(order) == bool(order_by)):
        print("Both order and oder_by must be specified if used")
        return abort(400, "Both order and oder_by must be specified if used")

    # Get Optional Arguments - time_range
    time_range = request.args.get('time_range')
    if time_range:
        # ex: 2021-12-01/2022-12-31"
        search_kwargs['datetime'] = str(time_range)  # datetime is a search kwarg
        print(f"    time_range = {time_range}")

    # Get Optional Arguments - max_items
    top = request.args.get('top')  # top is not a search kwarg
    top = top or 1
    top = int(top)
    print(f"    top = {top}")

    # Perform Geotiff Search
    print('Searching Planetary Computer...')
    search = client.search(**search_kwargs)
    items = search.item_collection()  # this is a list of pystac.item objects

    if len(items) > 0:
        print(f'{len(items)} item(s) found')

        # sort items if requested
        if order == 'ascending':
            items.items.sort(key=lambda item: item.properties[order_by], reverse=False)
        elif order == 'descending':
            items.items.sort(key=lambda item: item.properties[order_by], reverse=True)

        # truncate results if needed
        if len(items) > top:
            items = items[:top]

        # Gather all assets for all items
        signed_hrefs = []
        for item in items:
            item_dict = {}
            for asset in assets:
                signed_href = planetary_computer.sign(item).assets[asset].href
                item_dict[asset] = signed_href
            signed_hrefs.append(item_dict)

        # Return the response
        response_string = json.dumps(signed_hrefs, indent=4)
        print(f'Response:\n{response_string}')
        response = make_response(response_string, 200)
        response.mimetype = "text/plain"
        return response
    else:
        print("No items found")
        return abort(404, "No items found")


@APP_.route('/<collection>/assets', methods=["GET"])
def list_assets(collection):
    """
    Given a Planetary Computer collection string, returns a list of all assets available in that collection along with their descriptions.

    Args:
        collection (string): The Planetary Computer Data Collection to query.
    Returns:
        response (JSON string): A list of all assets available in that collection and their descriptions.
    """
    print("Received asset list request:")
    client = get_pystac_client()

    # Initialize search keyword arguments to search for the first available item. This will be used to collect the list of assets
    bbox = [-180, -90, 180, 90]
    search_kwargs = {
        'bbox': bbox,
        'max_items': 1,
    }

    # Get Required Arguments - collection
    search_kwargs['collections'] = [str(collection)]
    print(f"    collection = {collection}")

    # Perform Geotiff Search
    print('Searching Planetary Computer...')
    search = client.search(**search_kwargs)
    items = search.item_collection()

    if len(items) > 0:
        assets = []
        for asset_key, asset in items[0].assets.items():
           assets.append({asset_key: asset.title})
        response_string = json.dumps(assets, indent=4)
        print(f'Response:\n{response_string}')
        response = make_response(response_string, 200)
        response.mimetype = "text/plain"
        return response
    else:
        print("No items found")
        return abort(404, "No items found")


@APP_.route('/<collection>/item_properties', methods=["GET"])
def list_item_properties(collection):
    """
    Given a Planetary Computer collection string, returns a list of all properties available for items in that collection.

    Args:
        collection (string): The Planetary Computer Data Collection to query.
    Returns:
        response (JSON string): A list of all properties available for items in the collection.
    """
    print("Received item properties list request:")
    client = get_pystac_client()

    # Initialize search keyword arguments to search for the first available item. This will be used to collect the list of assets
    bbox = [-180, -90, 180, 90]
    search_kwargs = {
        'bbox': bbox,
        'max_items': 1,
    }

    # Get Required Arguments - collection
    search_kwargs['collections'] = [str(collection)]
    print(f"    collection = {collection}")

    # Perform Geotiff Search
    print('Searching Planetary Computer...')
    search = client.search(**search_kwargs)
    items = search.item_collection()

    if len(items) > 0:
        properties = list(items[0].properties.keys())
        response_string = json.dumps(properties, indent=4)
        print(f'Response:\n{response_string}')
        response = make_response(response_string, 200)
        response.mimetype = "text/plain"
        return response
    else:
        print("No items found")
        return abort(404, "No items found")


def main():
    host = '0.0.0.0'
    print(f"Running application at {host}:8080")
    serve(APP_, host=host, port=8080)


if __name__ == '__main__':
    main()
