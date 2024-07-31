#!/bin/bash
mqttx pub -t "zigbee2mqtt/house_tap" -m '{ "action": "single" }'