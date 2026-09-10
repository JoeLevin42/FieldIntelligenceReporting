from confluent_kafka import Producer
import os
import json

from json_loader import load_json


KAFKA_BROKER = os.getenv("KAFKA_BROKER", "localhost:9092")
KAFKA_TOPIC = os.getenv("KAFKA_TOPIC", "raw_data")

producer = Producer({
    "bootstrap.servers": KAFKA_BROKER
})

data = load_json("field_reports.json")

producer.produce(
    KAFKA_TOPIC,
    value=json.dumps(data)
)

producer.flush()