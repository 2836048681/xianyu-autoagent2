import argparse
import json
import os
import sys
import traceback

from dotenv import load_dotenv
from loguru import logger

from main import run_service
from utils.env_utils import (
    ensure_prompts_dir,
    get_env_path,
    load_env_file,
    set_active_app_dir,
    update_env_file,
)
from utils.log_utils import configure_worker_logging
from utils.playwright_login import fetch_cookies_via_playwright


def parse_args():
    parser = argparse.ArgumentParser(description="Xianyu AutoAgent worker entrypoint")
    subparsers = parser.add_subparsers(dest="command", required=True)

    service_parser = subparsers.add_parser("service", help="Run worker service for one account")
    service_parser.add_argument("--account-dir", required=True, help="Absolute account directory")
    service_parser.add_argument("--account-name", default="", help="Logical account name for logging")

    login_parser = subparsers.add_parser("login", help="Run fallback browser login for one account")
    login_parser.add_argument("--account-dir", required=True, help="Absolute account directory")
    login_parser.add_argument("--account-name", default="", help="Logical account name for logging")

    return parser.parse_args()


def _emit_result(ok: bool, account_name: str, cookie: str = "", error: str = "") -> None:
    payload = {
        "ok": ok,
        "account": account_name,
        "cookie": cookie,
        "error": error,
    }
    sys.stdout.write(json.dumps(payload, ensure_ascii=False) + "\n")
    sys.stdout.flush()


def _prepare_account(account_dir: str, account_name: str) -> str:
    os.makedirs(account_dir, exist_ok=True)
    set_active_app_dir(account_dir)
    ensure_prompts_dir()
    env_path = get_env_path(account_dir)
    if os.path.exists(env_path):
        load_dotenv(env_path, override=True)
    configure_worker_logging(account_dir, account_name or os.path.basename(account_dir))
    return env_path


def _validate_service_env(env_path: str) -> None:
    env = load_env_file(env_path)
    missing = [key for key in ("API_KEY", "COOKIES_STR") if not env.get(key)]
    if missing:
        raise RuntimeError(f"missing required config: {', '.join(missing)}")


def run_service_command(account_dir: str, account_name: str) -> int:
    env_path = _prepare_account(account_dir, account_name)
    _validate_service_env(env_path)
    logger.info("starting service process")
    run_service()
    return 0


def run_login_command(account_dir: str, account_name: str) -> int:
    env_path = _prepare_account(account_dir, account_name)
    logger.info("starting fallback browser login")
    cookie_str = fetch_cookies_via_playwright(fresh_profile=True)
    if not cookie_str:
        logger.warning("fallback browser login did not return cookies")
        _emit_result(False, account_name, error="未获取到 Cookie")
        return 2

    update_env_file({"COOKIES_STR": cookie_str}, env_path)
    logger.info("cookie updated from fallback browser login")
    _emit_result(True, account_name, cookie=cookie_str)
    return 0


def main() -> int:
    args = parse_args()
    try:
        if args.command == "service":
            return run_service_command(args.account_dir, args.account_name)
        if args.command == "login":
            return run_login_command(args.account_dir, args.account_name)
        raise RuntimeError(f"unsupported command: {args.command}")
    except Exception as exc:
        try:
            logger.exception("worker command failed")
        except Exception:
            pass
        _emit_result(False, getattr(args, "account_name", ""), error=str(exc))
        traceback.print_exc(file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
