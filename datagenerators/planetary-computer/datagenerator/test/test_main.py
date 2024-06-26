import json

from tool_planetary_computer_geotiff.main import APP_


def test_get_items():
    response = APP_.test_client().get('/landsat-c2-l2/items/38.9072/-77.0369?asset=red&asset=green&asset=blue&order=ascending&order_by=eo:cloud_cover&top=3')
    assert response.status_code == 200

    response_data = json.loads(response.data)
    assert len(response_data) == 3
    for item in response_data:
        assert 'red' in item.keys()
        assert 'blue' in item.keys()
        assert 'green' in item.keys()


def test_list_assets():
    response = APP_.test_client().get('chloris-biomass/assets')
    assert response.status_code == 200

    expected = [
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
    response_data = json.loads(response.data)
    assert response_data == expected


def test_list_item_properties():
    response = APP_.test_client().get('cop-dem-glo-30/item_properties')
    assert response.status_code == 200

    expected = [
        "gsd",
        "datetime",
        "platform",
        "proj:epsg",
        "proj:shape",
        "proj:transform"
    ]
    response_data = json.loads(response.data)
    assert response_data == expected
