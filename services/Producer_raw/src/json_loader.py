import json
from pathlib import Path


def load_json(file_name):
    project_root = Path(__file__).resolve().parent.parent
    file_path = project_root / "data" / file_name

    with open(file_path, "r", encoding="utf-8") as file:
        return json.load(file)