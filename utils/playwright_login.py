import os
import sys
import time
import shutil
import tempfile
from typing import Optional

from playwright.sync_api import sync_playwright

from utils.env_utils import get_user_data_dir


def _get_base_dir() -> str:
    if getattr(sys, "frozen", False):
        return os.path.dirname(sys.executable)
    return os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def _get_bundled_paths(base_dir: str) -> tuple[str, str]:
    chrome_path = os.path.join(base_dir, "chrome", "chrome.exe")
    driver_path = os.path.join(base_dir, "chromedriver", "chromedriver.exe")
    return chrome_path, driver_path


def _resolve_runtime_paths() -> tuple[str, str]:
    base_dir = _get_base_dir()
    chrome_path, driver_path = _get_bundled_paths(base_dir)
    if os.path.exists(chrome_path):
        return chrome_path, driver_path
    if getattr(sys, "frozen", False) and hasattr(sys, "_MEIPASS"):
        meipass = sys._MEIPASS  # type: ignore[attr-defined]
        chrome_path, driver_path = _get_bundled_paths(meipass)
        return chrome_path, driver_path
    return chrome_path, driver_path


def fetch_cookies_via_playwright(timeout_seconds: int = 180, fresh_profile: bool = True) -> Optional[str]:
    chrome_path, _ = _resolve_runtime_paths()
    if not os.path.exists(chrome_path):
        raise FileNotFoundError(f"\u672a\u627e\u5230\u79bb\u7ebf Chrome: {chrome_path}")

    profile_dir = None
    if fresh_profile:
        profile_dir = tempfile.mkdtemp(prefix="chrome_profile_", dir=get_user_data_dir())
    else:
        profile_dir = os.path.join(get_user_data_dir(), "chrome_profile")
        os.makedirs(profile_dir, exist_ok=True)

    context = None
    try:
        with sync_playwright() as p:
            context = p.chromium.launch_persistent_context(
                user_data_dir=profile_dir,
                executable_path=chrome_path,
                headless=False,
                viewport=None,
                args=[
                    "--disable-blink-features=AutomationControlled",
                    "--no-first-run",
                    "--no-default-browser-check",
                    "--start-maximized",
                ],
            )
            page = context.new_page()
            try:
                page.goto("https://www.goofish.com/", wait_until="domcontentloaded")
            except Exception:
                pass

            start = time.time()
            while time.time() - start < timeout_seconds:
                cookies = context.cookies()
                if cookies:
                    cookie_str = "; ".join([f"{c['name']}={c['value']}" for c in cookies])
                    if "cna=" in cookie_str and "unb=" in cookie_str:
                        return cookie_str
                time.sleep(2)
            return None
    finally:
        if context is not None:
            try:
                context.close()
            except Exception:
                pass
        if fresh_profile and profile_dir:
            try:
                shutil.rmtree(profile_dir, ignore_errors=True)
            except Exception:
                pass







