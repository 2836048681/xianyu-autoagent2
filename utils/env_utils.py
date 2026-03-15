import os
import re
import sys
import shutil
from typing import Dict, Optional, List


def get_project_root() -> str:
    return os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def get_bundled_root() -> str:
    if getattr(sys, "frozen", False) and hasattr(sys, "_MEIPASS"):
        return sys._MEIPASS  # type: ignore[attr-defined]
    return get_project_root()


_ACTIVE_APP_DIR: Optional[str] = None


def set_active_app_dir(path: Optional[str]) -> None:
    global _ACTIVE_APP_DIR
    _ACTIVE_APP_DIR = path


def get_user_data_dir() -> str:
    if _ACTIVE_APP_DIR:
        os.makedirs(_ACTIVE_APP_DIR, exist_ok=True)
        return _ACTIVE_APP_DIR
    base = os.getenv("APPDATA") or os.path.expanduser("~")
    path = os.path.join(base, "XianyuAutoAgent")
    os.makedirs(path, exist_ok=True)
    return path


def ensure_app_dir() -> str:
    data_dir = get_user_data_dir()
    os.chdir(data_dir)
    return data_dir


def get_env_path(app_dir: Optional[str] = None) -> str:
    base_dir = app_dir or get_user_data_dir()
    return os.path.join(base_dir, ".env")


def load_env_file(env_path: Optional[str] = None) -> Dict[str, str]:
    path = env_path or get_env_path()
    if not os.path.exists(path):
        return {}
    env: Dict[str, str] = {}
    with open(path, "r", encoding="utf-8") as f:
        for line in f.read().splitlines():
            if not line or line.strip().startswith("#") or "=" not in line:
                continue
            key, val = line.split("=", 1)
            env[key.strip()] = val.strip()
    return env


def update_env_file(values: Dict[str, str], env_path: Optional[str] = None) -> None:
    path = env_path or get_env_path()
    lines = []
    if os.path.exists(path):
        with open(path, "r", encoding="utf-8") as f:
            lines = f.read().splitlines()

    key_re = re.compile(r"^([A-Za-z_][A-Za-z0-9_]*)=")
    index_by_key: Dict[str, int] = {}
    for idx, line in enumerate(lines):
        match = key_re.match(line)
        if match:
            index_by_key[match.group(1)] = idx

    used = set()
    for key, val in values.items():
        if key in index_by_key:
            lines[index_by_key[key]] = f"{key}={val}"
            used.add(key)

    for key, val in values.items():
        if key not in used:
            lines.append(f"{key}={val}")

    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")


def get_data_dir() -> str:
    path = os.path.join(get_user_data_dir(), "data")
    os.makedirs(path, exist_ok=True)
    return path


def get_prompts_dir() -> str:
    path = os.path.join(get_user_data_dir(), "prompts")
    os.makedirs(path, exist_ok=True)
    return path


def ensure_prompts_dir() -> str:
    dst = get_prompts_dir()
    src = os.path.join(get_bundled_root(), "prompts")
    if not os.path.exists(src):
        return dst
    for name in os.listdir(src):
        src_path = os.path.join(src, name)
        dst_path = os.path.join(dst, name)
        if os.path.isfile(src_path) and not os.path.exists(dst_path):
            shutil.copy2(src_path, dst_path)
    return dst


def ensure_account_dir(name: str) -> str:
    base = get_user_data_dir()
    path = os.path.join(base, "accounts", name)
    os.makedirs(path, exist_ok=True)
    os.makedirs(os.path.join(path, "data"), exist_ok=True)
    os.makedirs(os.path.join(path, "prompts"), exist_ok=True)
    return path


def list_account_dirs() -> List[str]:
    base = get_user_data_dir()
    root = os.path.join(base, "accounts")
    if not os.path.exists(root):
        return []
    names = []
    for entry in os.listdir(root):
        full = os.path.join(root, entry)
        if os.path.isdir(full):
            names.append(entry)
    return sorted(names)
