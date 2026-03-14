import os
import sys
import time
from typing import Optional

from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.chrome.service import Service

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
    if os.path.exists(chrome_path) and os.path.exists(driver_path):
        return chrome_path, driver_path
    if getattr(sys, "frozen", False) and hasattr(sys, "_MEIPASS"):
        meipass = sys._MEIPASS  # type: ignore[attr-defined]
        chrome_path, driver_path = _get_bundled_paths(meipass)
        return chrome_path, driver_path
    return chrome_path, driver_path


def fetch_cookies_via_selenium(timeout_seconds: int = 180) -> Optional[str]:
    chrome_path, driver_path = _resolve_runtime_paths()
    if not os.path.exists(chrome_path):
        raise FileNotFoundError(f"未找到离线 Chrome: {chrome_path}")
    if not os.path.exists(driver_path):
        raise FileNotFoundError(f"未找到离线 chromedriver: {driver_path}")

    options = Options()
    options.binary_location = chrome_path
    options.add_argument("--disable-blink-features=AutomationControlled")
    options.add_argument("--start-maximized")
    options.add_argument("--no-first-run")
    options.add_argument("--no-default-browser-check")
    options.add_argument(f"--user-data-dir={os.path.join(get_user_data_dir(), 'chrome_profile')}")

    service = Service(executable_path=driver_path)
    driver = webdriver.Chrome(service=service, options=options)
    try:
        driver.get("https://www.goofish.com/")
        start = time.time()
        while time.time() - start < timeout_seconds:
            cookies = driver.get_cookies()
            if cookies:
                cookie_str = "; ".join([f"{c['name']}={c['value']}" for c in cookies])
                if "cna=" in cookie_str and "unb=" in cookie_str:
                    return cookie_str
            time.sleep(2)
        return None
    finally:
        driver.quit()
