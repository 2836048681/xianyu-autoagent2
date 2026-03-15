import argparse
import os
from pathlib import Path

from utils.env_utils import (
    ensure_account_dir,
    update_env_file,
    load_env_file,
    ensure_prompts_dir,
    set_active_app_dir,
    get_prompts_dir,
    get_data_dir,
)


def parse_args():
    parser = argparse.ArgumentParser(description="Multi-account isolation test (no service start).")
    parser.add_argument(
        "--accounts",
        default="account1,account2",
        help="Comma-separated account names (default: account1,account2)",
    )
    return parser.parse_args()


def main():
    args = parse_args()
    accounts = [name.strip() for name in args.accounts.split(",") if name.strip()]
    if not accounts:
        raise SystemExit("No accounts provided.")

    for idx, name in enumerate(accounts, 1):
        account_dir = ensure_account_dir(name)
        set_active_app_dir(account_dir)
        ensure_prompts_dir()

        env_path = os.path.join(account_dir, ".env")
        update_env_file(
            {
                "API_KEY": f"test_key_{idx}",
                "MODEL_BASE_URL": "http://example.local",
                "MODEL_NAME": "test-model",
                "COOKIES_STR": f"cookie_{idx}",
            },
            env_path,
        )
        env = load_env_file(env_path)
        prompts_dir = get_prompts_dir()
        data_dir = get_data_dir()
        prompt_files = list(Path(prompts_dir).glob("*"))

        print(f"[{name}] dir={account_dir}")
        print(f"[{name}] env.API_KEY={env.get('API_KEY')}")
        print(f"[{name}] env.COOKIES_STR={env.get('COOKIES_STR')}")
        print(f"[{name}] prompts_dir={prompts_dir} files={len(prompt_files)}")
        print(f"[{name}] data_dir={data_dir}")
        print("-" * 60)


if __name__ == "__main__":
    main()
