# Planetary Computer

## Optional Prerequisites
1. Register for Planetary Computer Access:
    ```plaintext
    https://planetarycomputer.microsoft.com/account/request
    ```
1. Acquire Planetary Computer Primary Key:
    ```plaintext
    https://planetarycomputer.developer.azure-api.net
    ```
1. Add Planetary Computer Primary Key to `~/.bashrc`:
    ```plaintext
    export PC_SDK_SUBSCRIPTION_KEY=<Planetary Computer Primary Key>
    ```

## Endpoints

### `/<collection>/items/<float:lat>/<float:lon>, methods=["GET"]`

#### Description
Given a latitude and longitude, returns a list of Planetary Computer hrefs to geotiffs that contains that location.
Additional arguments enable broad queryability across collections, time ranges, and assets.

#### Args
- `collection (string)`: The Planetary Computer Data Collection to query.
- `lat (float)`: The latitude of the location for which items are to be found.
- `lon (float)`: The longitude of the location for which items are to be found.
- `asset (string)`: Which asset to retrieve. Can be specified multiple times to retrieve multiple assets in one query.
- `max_items (int, optional)`: The maximum number of geotiffs to consider in the query. Defaults to 500.
- `order (string, optional)`: Indicates which method should be used for search optimization, if any. One of 'ascending' or 'descending'. Defaults to None.
- `order_by (string, optional)`: Indicates which pystac.item property should be used for search optimization, if any. Defaults to None.
- `time_range (string, optional)`: A (datetime)/(datetime) string specifying the temporal bounds of geotiff to consider (ex: 2021-12-01/2022-12-31). Defaults to None.
- `top (int, optional)`: The maximum number of geotiffs to include in the response. Defaults to 1.

#### Returns
- `response (JSON string)`: A list of hyperlinks the requested Planetary Computer COG items

#### Example Landsat Collection 2 Level-2 Query
1. This query returns the red, green, and blue bands from three items from Landsat Collection 2 Level-2 containing Washington, D.C with the least amount of cloud cover.

    ```plaintext
    curl "http://127.0.0.1:8080/landsat-c2-l2/items/38.9072/-77.0369?asset=red&asset=green&asset=blue&order=ascending&order_by=eo:cloud_cover&top=3"
    ```

1. The response should appear similar to the following:

    ```plaintext
    [
        {
            "red": "https://landsateuwest.blob.core.windows.net/landsat-c2/level-2/standard/etm/2021/015/033/LE07_L2SP_015033_20210926_20211022_02_T1/LE07_L2SP_015033_20210926_20211022_02_T1_SR_B3.TIF?st=2023-02-08T19%3A53%3A42Z&se=2023-02-09T20%3A38%3A42Z&sp=rl&sv=2021-06-08&sr=c&skoid=c85c15d6-d1ae-42d4-af60-e2ca0f81359b&sktid=72f988bf-86f1-41af-91ab-2d7cd011db47&skt=2023-02-09T18%3A59%3A10Z&ske=2023-02-16T18%3A59%3A10Z&sks=b&skv=2021-06-08&sig=/AFrrWijdwxkbGVuhv4FmnTisE9Rtc4GrZDAYRP23mc%3D",
            "green": "https://landsateuwest.blob.core.windows.net/landsat-c2/level-2/standard/etm/2021/015/033/LE07_L2SP_015033_20210926_20211022_02_T1/LE07_L2SP_015033_20210926_20211022_02_T1_SR_B2.TIF?st=2023-02-08T19%3A53%3A42Z&se=2023-02-09T20%3A38%3A42Z&sp=rl&sv=2021-06-08&sr=c&skoid=c85c15d6-d1ae-42d4-af60-e2ca0f81359b&sktid=72f988bf-86f1-41af-91ab-2d7cd011db47&skt=2023-02-09T18%3A59%3A10Z&ske=2023-02-16T18%3A59%3A10Z&sks=b&skv=2021-06-08&sig=/AFrrWijdwxkbGVuhv4FmnTisE9Rtc4GrZDAYRP23mc%3D",
            "blue": "https://landsateuwest.blob.core.windows.net/landsat-c2/level-2/standard/etm/2021/015/033/LE07_L2SP_015033_20210926_20211022_02_T1/LE07_L2SP_015033_20210926_20211022_02_T1_SR_B1.TIF?st=2023-02-08T19%3A53%3A42Z&se=2023-02-09T20%3A38%3A42Z&sp=rl&sv=2021-06-08&sr=c&skoid=c85c15d6-d1ae-42d4-af60-e2ca0f81359b&sktid=72f988bf-86f1-41af-91ab-2d7cd011db47&skt=2023-02-09T18%3A59%3A10Z&ske=2023-02-16T18%3A59%3A10Z&sks=b&skv=2021-06-08&sig=/AFrrWijdwxkbGVuhv4FmnTisE9Rtc4GrZDAYRP23mc%3D"
        },
        {
            "red": "https://landsateuwest.blob.core.windows.net/landsat-c2/level-2/standard/etm/2021/015/033/LE07_L2SP_015033_20210521_20210616_02_T1/LE07_L2SP_015033_20210521_20210616_02_T1_SR_B3.TIF?st=2023-02-08T19%3A53%3A42Z&se=2023-02-09T20%3A38%3A42Z&sp=rl&sv=2021-06-08&sr=c&skoid=c85c15d6-d1ae-42d4-af60-e2ca0f81359b&sktid=72f988bf-86f1-41af-91ab-2d7cd011db47&skt=2023-02-09T18%3A59%3A10Z&ske=2023-02-16T18%3A59%3A10Z&sks=b&skv=2021-06-08&sig=/AFrrWijdwxkbGVuhv4FmnTisE9Rtc4GrZDAYRP23mc%3D",
            "green": "https://landsateuwest.blob.core.windows.net/landsat-c2/level-2/standard/etm/2021/015/033/LE07_L2SP_015033_20210521_20210616_02_T1/LE07_L2SP_015033_20210521_20210616_02_T1_SR_B2.TIF?st=2023-02-08T19%3A53%3A42Z&se=2023-02-09T20%3A38%3A42Z&sp=rl&sv=2021-06-08&sr=c&skoid=c85c15d6-d1ae-42d4-af60-e2ca0f81359b&sktid=72f988bf-86f1-41af-91ab-2d7cd011db47&skt=2023-02-09T18%3A59%3A10Z&ske=2023-02-16T18%3A59%3A10Z&sks=b&skv=2021-06-08&sig=/AFrrWijdwxkbGVuhv4FmnTisE9Rtc4GrZDAYRP23mc%3D",
            "blue": "https://landsateuwest.blob.core.windows.net/landsat-c2/level-2/standard/etm/2021/015/033/LE07_L2SP_015033_20210521_20210616_02_T1/LE07_L2SP_015033_20210521_20210616_02_T1_SR_B1.TIF?st=2023-02-08T19%3A53%3A42Z&se=2023-02-09T20%3A38%3A42Z&sp=rl&sv=2021-06-08&sr=c&skoid=c85c15d6-d1ae-42d4-af60-e2ca0f81359b&sktid=72f988bf-86f1-41af-91ab-2d7cd011db47&skt=2023-02-09T18%3A59%3A10Z&ske=2023-02-16T18%3A59%3A10Z&sks=b&skv=2021-06-08&sig=/AFrrWijdwxkbGVuhv4FmnTisE9Rtc4GrZDAYRP23mc%3D"
        },
        {
            "red": "https://landsateuwest.blob.core.windows.net/landsat-c2/level-2/standard/etm/2020/015/033/LE07_L2SP_015033_20200923_20201019_02_T1/LE07_L2SP_015033_20200923_20201019_02_T1_SR_B3.TIF?st=2023-02-08T19%3A53%3A42Z&se=2023-02-09T20%3A38%3A42Z&sp=rl&sv=2021-06-08&sr=c&skoid=c85c15d6-d1ae-42d4-af60-e2ca0f81359b&sktid=72f988bf-86f1-41af-91ab-2d7cd011db47&skt=2023-02-09T18%3A59%3A10Z&ske=2023-02-16T18%3A59%3A10Z&sks=b&skv=2021-06-08&sig=/AFrrWijdwxkbGVuhv4FmnTisE9Rtc4GrZDAYRP23mc%3D",
            "green": "https://landsateuwest.blob.core.windows.net/landsat-c2/level-2/standard/etm/2020/015/033/LE07_L2SP_015033_20200923_20201019_02_T1/LE07_L2SP_015033_20200923_20201019_02_T1_SR_B2.TIF?st=2023-02-08T19%3A53%3A42Z&se=2023-02-09T20%3A38%3A42Z&sp=rl&sv=2021-06-08&sr=c&skoid=c85c15d6-d1ae-42d4-af60-e2ca0f81359b&sktid=72f988bf-86f1-41af-91ab-2d7cd011db47&skt=2023-02-09T18%3A59%3A10Z&ske=2023-02-16T18%3A59%3A10Z&sks=b&skv=2021-06-08&sig=/AFrrWijdwxkbGVuhv4FmnTisE9Rtc4GrZDAYRP23mc%3D",
            "blue": "https://landsateuwest.blob.core.windows.net/landsat-c2/level-2/standard/etm/2020/015/033/LE07_L2SP_015033_20200923_20201019_02_T1/LE07_L2SP_015033_20200923_20201019_02_T1_SR_B1.TIF?st=2023-02-08T19%3A53%3A42Z&se=2023-02-09T20%3A38%3A42Z&sp=rl&sv=2021-06-08&sr=c&skoid=c85c15d6-d1ae-42d4-af60-e2ca0f81359b&sktid=72f988bf-86f1-41af-91ab-2d7cd011db47&skt=2023-02-09T18%3A59%3A10Z&ske=2023-02-16T18%3A59%3A10Z&sks=b&skv=2021-06-08&sig=/AFrrWijdwxkbGVuhv4FmnTisE9Rtc4GrZDAYRP23mc%3D"
        }
    ]
    ```

#### Example Sentinel-2 Level-2A Query
1. This query returns the near-infrared band from Sentinel-2 Level-2A containing Seattle, WA with the most amount of cloud cover.

    ```plaintext
    curl "http://127.0.0.1:8080/sentinel-2-l2a/items/47.619160/-122.338517?asset=B08&order=descending&order_by=eo:cloud_cover"
    ```

1. The response should appear similar to the following:

    ```plaintext
    [
        {
            "B08": "https://sentinel2l2a01.blob.core.windows.net/sentinel2-l2/10/T/ET/2020/12/16/S2A_MSIL2A_20201216T191821_N0212_R056_T10TET_20201217T122900.SAFE/GRANULE/L2A_T10TET_A028653_20201216T192046/IMG_DATA/R10m/T10TET_20201216T191821_B08_10m.tif?st=2023-02-08T19%3A50%3A40Z&se=2023-02-09T20%3A35%3A40Z&sp=rl&sv=2021-06-08&sr=c&skoid=c85c15d6-d1ae-42d4-af60-e2ca0f81359b&sktid=72f988bf-86f1-41af-91ab-2d7cd011db47&skt=2023-02-09T19%3A47%3A17Z&ske=2023-02-16T19%3A47%3A17Z&sks=b&skv=2021-06-08&sig=yppnVKNekDgfQfkxXePMsakqp4iPHY0r%2BFyKjfZddOc%3D"
        }
    ]
    ```

#### Example ESA Climate Change Initiative Land Cover Maps Query
1. This query returns the first Land Cover Class Defined in the Land Cover Classification System asset from the ESA Climate Change Initiative Land Cover Maps Data Catalog that contains Dallas, TX.

    ```plaintext
    curl "http://127.0.0.1:8080/esa-cci-lc/items/32.7767/-96.7970?asset=lccs_class&max_items=1"
    ```

1. The response should appear similar to the following:

    ```plaintext
    [
        {
            "lccs_class": "https://landcoverdata.blob.core.windows.net/esa-cci-lc/cog/v2.1.1/N00W135/2020/C3S-LC-L4-LCCS-Map-300m-P1Y-2020-v2.1.1-N00W135-lccs_class.tif?st=2023-02-08T19%3A51%3A06Z&se=2023-02-09T20%3A36%3A06Z&sp=rl&sv=2021-06-08&sr=c&skoid=c85c15d6-d1ae-42d4-af60-e2ca0f81359b&sktid=72f988bf-86f1-41af-91ab-2d7cd011db47&skt=2023-02-09T19%3A26%3A00Z&ske=2023-02-16T19%3A26%3A00Z&sks=b&skv=2021-06-08&sig=l2PbfEPCjzPdeR8jEmxS5DEYlyuS9n866aP0xQqGMVY%3D"
        }
    ]
    ```

### `/<collection>/assets, methods=["GET"]`

#### Description
Given a Planetary Computer collection string, returns a list of all assets available in that collection along with their descriptions.

#### Args
- `collection (string)`: The Planetary Computer Data Collection to query.

#### Returns
- `response (JSON string)`: A list of all assets available in that collection and their descriptions.

#### Example
1. This query returns the list of assets available from the Chloris Biomass collection.
    ```plaintext
    curl "http://127.0.0.1:8080/chloris-biomass/assets"
    ```

1. The response should appear as follows:
    ```plaintext
    [
        {
            "biomass": "Annual estimates of aboveground woody biomass."
        },
        {
            "biomass_wm": "Annual estimates of aboveground woody biomass (Web Mercator)."
        },
        {
            "biomass_change": "Annual estimates of changes (gains and losses) in aboveground woody biomass from the previous year."
        },
        {
            "biomass_change_wm": "Annual estimates of changes (gains and losses) in aboveground woody biomass from the previous year (Web Mercator)."
        },
        {
            "tilejson": "TileJSON with default rendering"
        },
        {
            "rendered_preview": "Rendered preview"
        }
    ]
    ```

### `/<collection>/item_properties, methods=["GET"]`

#### Description
Given a Planetary Computer collection string, returns a list of all properties available for items in that collection.

#### Args
- `collection (string)`: The Planetary Computer Data Collection to query.

#### Returns
- `response (JSON string)`: A list of all properties available for items in the collection.

#### Example
1. This query returns the list of assets available from the Copernicus DEM GLO-30 DSM collection.
    ```plaintext
    curl "http://127.0.0.1:8080/cop-dem-glo-30/item_properties"
    ```

1. The response should appear as follows:
    ```plaintext
    [
        "gsd",
        "datetime",
        "platform",
        "proj:epsg",
        "proj:shape",
        "proj:transform"
    ]
    ```

## Developer Instructions

### Execution
1. Open this project within the provided devcontainer
1. Launch the Planetary Computer Geotiff Application via:
    ```bash
        python3 src/tool_planetary_computer_geotiff/main.py
    ```
1. Alternatively, the Planetary Computer Geotiff Application may be launched via:
    ```bash
    tool-planetary-computer-geotiff
    ```
1. Once the webservice is available, you may execute one of the above sample queries and verify an expected response is returned.

### Running Unit Tests
1. Open this project within the provided devcontainer
1. Execute `poetry run pytest` to run the unit test suite:
    ```plaintext
    $ poetry run pytest
    Skipping virtualenv creation, as specified in config file.
    ...                                                                                                                        [100%]
    3 passed in 7.68s
    ```

## Updating Dependencies
1. To update dependencies and the poetry.lock file, execute `poetry update`