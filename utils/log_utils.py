import json
import os
import sys
from datetime import datetime
from typing import Optional

from loguru import logger

from utils.env_utils import get_logs_dir


TEXT_LOG_FORMAT = (
    "{time:YYYY-MM-DD HH:mm:ss.SSS} | "
    "{level: <8} | "
    "{name}:{function}:{line} - "
    "{message}"
)


def _stdout_json_sink(message) -> None:
    record = message.record
    payload = {
        "time": record["time"].strftime("%Y-%m-%dT%H:%M:%S.%f%z"),
        "level": record["level"].name,
        "logger": record["name"],
        "function": record["function"],
        "line": record["line"],
        "message": record["message"],
        "raw": message.rstrip("\n"),
    }
    sys.stdout.write(json.dumps(payload, ensure_ascii=False) + "\n")
    sys.stdout.flush()


def configure_worker_logging(account_dir: str, account_name: Optional[str] = None) -> None:
    logs_dir = get_logs_dir(account_dir)
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    history_path = os.path.join(logs_dir, f"worker_{timestamp}.log")
    current_path = os.path.join(logs_dir, "current.log")
    log_level = os.getenv("LOG_LEVEL", "DEBUG").upper()

    logger.remove()
    logger.add(_stdout_json_sink, level=log_level, colorize=False, format="{message}")
    logger.add(
        current_path,
        level=log_level,
        format=TEXT_LOG_FORMAT,
        encoding="utf-8",
        mode="w",
    )
    logger.add(
        history_path,
        level=log_level,
        format=TEXT_LOG_FORMAT,
        encoding="utf-8",
    )

    if account_name:
        logger.info(f"[{account_name}] worker logging configured")
    else:
        logger.info("worker logging configured")
