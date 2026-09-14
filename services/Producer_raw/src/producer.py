from confluent_kafka import Producer
import os
import json

from json_loader import load_json


KAFKA_BROKER = os.getenv("KAFKA_BROKER", "localhost:9092")
KAFKA_TOPIC = os.getenv("KAFKA_TOPIC", "raw_data")

producer = Producer({
    "bootstrap.servers": KAFKA_BROKER
})


def delivery_report(err, msg):
    if err is not None:
        print(f"FAILED: {err}")
    else:
        print(
            f"SENT: {msg.key()} "
            f"to {msg.topic()} partition {msg.partition()} offset {msg.offset()}"
        )


data = load_json("field_reports.json")

print(f"Loaded {len(data)} reports")

for report in data:
    producer.produce(
        KAFKA_TOPIC,
        value=json.dumps(report),
        callback=delivery_report
    )

producer.flush()

print("Finished")