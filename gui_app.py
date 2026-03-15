import os
import sys
import threading
import subprocess
import queue
import time
import traceback
import webbrowser
import tkinter as tk
from tkinter import ttk, messagebox, simpledialog

from loguru import logger
from dotenv import load_dotenv

from utils.env_utils import (
    ensure_app_dir,
    get_project_root,
    get_env_path,
    load_env_file,
    update_env_file,
    get_prompts_dir,
    ensure_prompts_dir,
    ensure_account_dir,
    list_account_dirs,
)
from utils.selenium_login import fetch_cookies_via_selenium


class App:
    LOG_FORMAT = (
        "<green>{time:YYYY-MM-DD HH:mm:ss.SSS}</green> | "
        "<level>{level: <8}</level> | "
        "<cyan>{name}</cyan>:<cyan>{function}</cyan>:<cyan>{line}</cyan> - "
        "<level>{message}</level>"
    )

    def __init__(self, root: tk.Tk):
        self.root = root
        self.root.title("Xianyu AutoAgent 2.0")
        self.root.geometry("980x680")
        self.root.minsize(900, 600)

        self.app_dir = ensure_app_dir()

        self.log_queue = queue.Queue()

        self.proc_by_account = {}
        self.proc_lock = threading.Lock()

        self.selected_account = tk.StringVar()
        self.accounts = []

        self._apply_theme()
        self._build_ui()
        self._setup_logger_bridge()
        self._reload_accounts()
        self._bind_shortcuts()

        self.root.protocol("WM_DELETE_WINDOW", self.on_close)

    def _apply_theme(self):
        self.root.configure(bg="#F5F7FB")
        style = ttk.Style(self.root)
        try:
            style.theme_use("clam")
        except tk.TclError:
            pass

        default_font = ("Microsoft YaHei UI", 10)
        header_font = ("Microsoft YaHei UI", 18, "bold")
        title_font = ("Microsoft YaHei UI", 12, "bold")

        style.configure(".", font=default_font, background="#F5F7FB")
        style.configure("TFrame", background="#F5F7FB")
        style.configure("Card.TFrame", background="#FFFFFF", relief="flat")
        style.configure("Title.TLabel", background="#F5F7FB", foreground="#0F172A", font=header_font)
        style.configure("Subtitle.TLabel", background="#F5F7FB", foreground="#64748B", font=default_font)
        style.configure("CardTitle.TLabel", background="#FFFFFF", foreground="#0F172A", font=title_font)
        style.configure("TLabel", background="#FFFFFF")
        style.configure("Field.TLabel", background="#FFFFFF", foreground="#334155")
        style.configure("Status.TLabel", background="#F5F7FB", foreground="#0F172A", font=("Microsoft YaHei UI", 10, "bold"))

        style.configure(
            "Primary.TButton",
            background="#2563EB",
            foreground="#FFFFFF",
            borderwidth=0,
            padding=(14, 8),
        )
        style.map(
            "Primary.TButton",
            background=[("active", "#1D4ED8")],
            foreground=[("disabled", "#E2E8F0")],
        )

        style.configure(
            "Secondary.TButton",
            background="#E2E8F0",
            foreground="#0F172A",
            borderwidth=0,
            padding=(14, 8),
        )
        style.map(
            "Secondary.TButton",
            background=[("active", "#CBD5F5")],
        )

        style.configure(
            "Ghost.TButton",
            background="#FFFFFF",
            foreground="#0F172A",
            borderwidth=1,
            padding=(12, 6),
            relief="solid",
        )
        style.map(
            "Ghost.TButton",
            background=[("active", "#F1F5F9")],
        )

        style.configure("TLabelframe", background="#FFFFFF", borderwidth=0)
        style.configure("TLabelframe.Label", background="#FFFFFF", foreground="#0F172A", font=title_font)
        style.configure("TEntry", padding=(6, 6))

    def _setup_logger_bridge(self):
        class TkLogSink:
            def __init__(self, q):
                self.q = q
            def write(self, message):
                msg = message.strip()
                if msg:
                    self.q.put(msg)
            def flush(self):
                return
        log_level = os.getenv("LOG_LEVEL", "DEBUG").upper()
        logger.remove()
        logger.add(
            TkLogSink(self.log_queue),
            level=log_level,
            format=self.LOG_FORMAT,
            colorize=False,
        )
        logger.info(f"日志级别设置为: {log_level}")
        self._drain_log_queue()

    def _drain_log_queue(self):
        while True:
            try:
                msg = self.log_queue.get_nowait()
            except queue.Empty:
                break
            self._append_log_line(msg)
        self.root.after(200, self._drain_log_queue)

    def _build_ui(self):
        self.root.columnconfigure(0, weight=1)
        self.root.rowconfigure(1, weight=1)

        header = ttk.Frame(self.root, padding=(24, 18, 24, 10))
        header.grid(row=0, column=0, sticky="ew")
        header.columnconfigure(0, weight=1)

        ttk.Label(header, text="Xianyu AutoAgent 2.0", style="Title.TLabel").grid(row=0, column=0, sticky="w")
        ttk.Label(
            header,
            text="智能闲鱼客服 · 多账号并行 · 实时日志",
            style="Subtitle.TLabel",
        ).grid(row=1, column=0, sticky="w", pady=(2, 0))

        status_wrap = ttk.Frame(header)
        status_wrap.grid(row=0, column=1, rowspan=2, sticky="e")
        status_wrap.configure(style="TFrame")

        self.status_var = tk.StringVar(value="状态：未启动")
        ttk.Label(status_wrap, textvariable=self.status_var, style="Status.TLabel").grid(row=0, column=0, sticky="e")

        content = ttk.Frame(self.root, padding=(24, 0, 24, 24))
        content.grid(row=1, column=0, sticky="nsew")
        content.columnconfigure(0, weight=3)
        content.columnconfigure(1, weight=2)
        content.rowconfigure(1, weight=1)

        # Accounts panel
        account_card = ttk.Frame(content, style="Card.TFrame", padding=18)
        account_card.grid(row=0, column=0, sticky="nsew", padx=(0, 12), pady=(0, 12))
        account_card.columnconfigure(1, weight=1)

        ttk.Label(account_card, text="账号管理", style="CardTitle.TLabel").grid(row=0, column=0, columnspan=3, sticky="w")

        ttk.Label(account_card, text="当前账号", style="Field.TLabel").grid(row=1, column=0, sticky="w", pady=(12, 4))
        self.account_combo = ttk.Combobox(account_card, textvariable=self.selected_account, state="readonly")
        self.account_combo.grid(row=1, column=1, sticky="ew", pady=(12, 4))
        self.account_combo.bind("<<ComboboxSelected>>", lambda _e: self._load_env_into_fields())

        ttk.Button(account_card, text="新增账号", style="Secondary.TButton", command=self.add_account).grid(
            row=1, column=2, padx=(8, 0), pady=(12, 4)
        )

        ttk.Label(account_card, text="API_KEY", style="Field.TLabel").grid(row=2, column=0, sticky="w", pady=4)
        self.api_key_var = tk.StringVar()
        ttk.Entry(account_card, textvariable=self.api_key_var).grid(row=2, column=1, columnspan=2, sticky="ew", pady=4)

        ttk.Label(account_card, text="MODEL_BASE_URL", style="Field.TLabel").grid(row=3, column=0, sticky="w", pady=4)
        self.base_url_var = tk.StringVar()
        ttk.Entry(account_card, textvariable=self.base_url_var).grid(row=3, column=1, columnspan=2, sticky="ew", pady=4)

        ttk.Label(account_card, text="MODEL_NAME", style="Field.TLabel").grid(row=4, column=0, sticky="w", pady=4)
        self.model_name_var = tk.StringVar()
        ttk.Entry(account_card, textvariable=self.model_name_var).grid(row=4, column=1, columnspan=2, sticky="ew", pady=4)

        ttk.Label(account_card, text="COOKIES_STR", style="Field.TLabel").grid(row=5, column=0, sticky="nw", pady=4)
        self.cookies_text = tk.Text(account_card, height=6, wrap="word", relief="solid", borderwidth=1)
        self.cookies_text.grid(row=5, column=1, columnspan=2, sticky="ew", pady=4)

        ttk.Label(
            account_card,
            text="建议通过扫码登录自动更新 Cookie，避免手动复制出错。",
            style="Subtitle.TLabel",
        ).grid(row=6, column=0, columnspan=3, sticky="w")

        account_actions = ttk.Frame(account_card)
        account_actions.grid(row=7, column=0, columnspan=3, sticky="e", pady=(12, 0))
        ttk.Button(account_actions, text="保存配置", style="Secondary.TButton", command=self.save_env).grid(
            row=0, column=0, padx=(0, 8)
        )
        ttk.Button(account_actions, text="扫码登录并更新 Cookie", style="Primary.TButton", command=self.start_login).grid(
            row=0, column=1
        )

        # Right column: service control + settings
        right_col = ttk.Frame(content, style="TFrame")
        right_col.grid(row=0, column=1, sticky="nsew")
        right_col.columnconfigure(0, weight=1)
        right_col.rowconfigure(1, weight=1)

        control_card = ttk.Frame(right_col, style="Card.TFrame", padding=18)
        control_card.grid(row=0, column=0, sticky="nsew", pady=(0, 12))
        control_card.columnconfigure(0, weight=1)
        ttk.Label(control_card, text="服务控制", style="CardTitle.TLabel").grid(row=0, column=0, sticky="w")

        control_buttons = ttk.Frame(control_card)
        control_buttons.grid(row=1, column=0, sticky="w", pady=(12, 0))
        ttk.Button(control_buttons, text="启动当前账号", style="Primary.TButton", command=self.start_service).grid(
            row=0, column=0, padx=(0, 8)
        )
        ttk.Button(control_buttons, text="停止当前账号", style="Secondary.TButton", command=self.stop_service).grid(
            row=0, column=1, padx=(0, 8)
        )
        ttk.Button(control_buttons, text="停止全部账号", style="Ghost.TButton", command=self.stop_all).grid(
            row=0, column=2
        )

        ttk.Label(
            control_card,
            text="提示：每个账号使用独立配置与数据库，可并行运行。",
            style="Subtitle.TLabel",
        ).grid(row=2, column=0, sticky="w", pady=(12, 0))

        settings_card = ttk.Frame(right_col, style="Card.TFrame", padding=18)
        settings_card.grid(row=1, column=0, sticky="nsew")
        settings_card.columnconfigure(0, weight=1)
        ttk.Label(settings_card, text="设置", style="CardTitle.TLabel").grid(row=0, column=0, sticky="w")

        ttk.Button(
            settings_card,
            text="关于软件",
            style="Secondary.TButton",
            command=self.show_about,
        ).grid(row=1, column=0, sticky="ew", pady=(12, 8))

        ttk.Button(
            settings_card,
            text="检查更新 (Release)",
            style="Secondary.TButton",
            command=self.check_update,
        ).grid(row=2, column=0, sticky="ew", pady=(0, 8))

        ttk.Button(
            settings_card,
            text="修改 prompts（谨慎）",
            style="Ghost.TButton",
            command=self.open_prompts_folder,
        ).grid(row=3, column=0, sticky="ew")

        ttk.Label(
            settings_card,
            text="提示：非专业用户请勿随意修改此选项。",
            style="Subtitle.TLabel",
        ).grid(row=4, column=0, sticky="w", pady=(8, 0))

        # Logs
        log_card = ttk.Frame(content, style="Card.TFrame", padding=18)
        log_card.grid(row=1, column=0, columnspan=2, sticky="nsew")
        log_card.rowconfigure(1, weight=1)
        log_card.columnconfigure(0, weight=1)
        ttk.Label(log_card, text="运行日志", style="CardTitle.TLabel").grid(row=0, column=0, sticky="w")

        log_frame = ttk.Frame(log_card)
        log_frame.grid(row=1, column=0, sticky="nsew", pady=(10, 0))
        log_frame.rowconfigure(0, weight=1)
        log_frame.columnconfigure(0, weight=1)

        self.log_text = tk.Text(
            log_frame,
            height=14,
            wrap="word",
            background="#0B1220",
            foreground="#E2E8F0",
            insertbackground="#E2E8F0",
            relief="flat",
            font=("Consolas", 10),
        )
        self.log_text.grid(row=0, column=0, sticky="nsew")
        scrollbar = ttk.Scrollbar(log_frame, orient="vertical", command=self.log_text.yview)
        scrollbar.grid(row=0, column=1, sticky="ns")
        self.log_text.configure(yscrollcommand=scrollbar.set)

    def _bind_shortcuts(self):
        self.root.bind_all("<Control-s>", lambda event: self.save_env())
        self.root.bind_all("<Control-r>", lambda event: self.start_service())

    def _reload_accounts(self):
        accounts = list_account_dirs()
        if not accounts:
            ensure_account_dir("account1")
            accounts = list_account_dirs()
        self.accounts = accounts[:5]
        self.account_combo["values"] = self.accounts
        if self.selected_account.get() not in self.accounts:
            self.selected_account.set(self.accounts[0])
        self._load_env_into_fields()

    def _current_account(self):
        return self.selected_account.get()

    def _current_env_path(self):
        account = self._current_account()
        if not account:
            return get_env_path(self.app_dir)
        return get_env_path(ensure_account_dir(account))

    def _load_env_into_fields(self):
        env = load_env_file(self._current_env_path())
        self.api_key_var.set(env.get("API_KEY", ""))
        self.base_url_var.set(env.get("MODEL_BASE_URL", "https://dashscope.aliyuncs.com/compatible-mode/v1"))
        self.model_name_var.set(env.get("MODEL_NAME", "qwen-max"))
        self.cookies_text.delete("1.0", tk.END)
        self.cookies_text.insert("1.0", env.get("COOKIES_STR", ""))

    def _append_log_line(self, msg: str):
        self.log_text.insert(tk.END, f"{msg}\n")
        self.log_text.see(tk.END)

    def save_env(self):
        if not self._current_account():
            messagebox.showwarning("未选择账号", "请先选择或新增账号")
            return
        values = {
            "API_KEY": self.api_key_var.get().strip(),
            "MODEL_BASE_URL": self.base_url_var.get().strip(),
            "MODEL_NAME": self.model_name_var.get().strip(),
            "COOKIES_STR": self.cookies_text.get("1.0", tk.END).strip(),
        }
        update_env_file(values, self._current_env_path())
        logger.info(f"[{self._current_account()}] 配置已保存")
        messagebox.showinfo("保存成功", "配置已保存到账号 .env")

    def add_account(self):
        if len(self.accounts) >= 5:
            messagebox.showwarning("账号上限", "最多支持 5 个账号")
            return
        name = simpledialog.askstring("新增账号", "请输入账号名称（字母数字/下划线）：", parent=self.root)
        if not name:
            return
        name = name.strip()
        if not name:
            return
        if name in self.accounts:
            messagebox.showwarning("已存在", "该账号已存在")
            return
        ensure_account_dir(name)
        self._reload_accounts()
        self.selected_account.set(name)
        self._load_env_into_fields()

    def start_login(self):
        if not self._current_account():
            messagebox.showwarning("未选择账号", "请先选择账号")
            return
        self.save_env()
        logger.info(f"[{self._current_account()}] 启动扫码登录流程，等待浏览器打开...")

        def run_login():
            try:
                cookie_str = fetch_cookies_via_selenium(fresh_profile=True)
                if not cookie_str:
                    self._ui(lambda: messagebox.showwarning("登录失败", "未获取到 Cookie"))
                    self._ui(lambda: logger.warning(f"[{self._current_account()}] 扫码登录失败或未完成"))
                    return
                update_env_file({"COOKIES_STR": cookie_str}, self._current_env_path())
                self._ui(lambda: self.cookies_text.delete("1.0", tk.END))
                self._ui(lambda: self.cookies_text.insert("1.0", cookie_str))
                self._ui(lambda: logger.info(f"[{self._current_account()}] Cookie 已更新"))
                self._ui(lambda: messagebox.showinfo("成功", "Cookie 已更新，服务将重启生效"))
                self._ui(self.restart_service)
            except Exception as e:
                err = "".join(traceback.format_exception_only(type(e), e)).strip()
                self._ui(lambda: logger.error(f"[{self._current_account()}] 扫码登录失败: {err}"))
                self._ui(lambda: messagebox.showerror("登录失败", err))

        threading.Thread(target=run_login, daemon=True).start()

    def _stream_process_output(self, account, proc):
        if not proc or not proc.stdout:
            return
        for line in proc.stdout:
            line = line.rstrip("\r\n")
            if line:
                self._ui(lambda l=line, a=account: self._append_log_line(f"[{a}] {l}"))

    def _spawn_service(self, account):
        account_dir = ensure_account_dir(account)
        env = os.environ.copy()
        env["PYTHONIOENCODING"] = "utf-8"
        env["PYTHONUTF8"] = "1"
        env["PYTHONUNBUFFERED"] = "1"
        env["NON_INTERACTIVE"] = "1"
        env["XYA_APP_DIR"] = account_dir
        if getattr(sys, "frozen", False):
            cmd = [sys.executable, "--run-service"]
            cwd = account_dir
        else:
            project_root = get_project_root()
            cmd = [sys.executable, "-u", os.path.join(project_root, "gui_app.py"), "--run-service"]
            cwd = project_root
        proc = subprocess.Popen(
            cmd,
            cwd=cwd,
            env=env,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
            encoding="utf-8",
            errors="replace",
        )
        threading.Thread(target=self._stream_process_output, args=(account, proc), daemon=True).start()
        return proc

    def start_service(self):
        account = self._current_account()
        if not account:
            messagebox.showwarning("未选择账号", "请先选择账号")
            return
        with self.proc_lock:
            proc = self.proc_by_account.get(account)
            if proc and proc.poll() is None:
                logger.info(f"[{account}] 服务已在运行")
                return
            self.save_env()
            self.proc_by_account[account] = self._spawn_service(account)
            self.status_var.set("状态：运行中")
            logger.info(f"[{account}] 服务已启动")

    def stop_service(self):
        account = self._current_account()
        if not account:
            return
        with self.proc_lock:
            proc = self.proc_by_account.get(account)
            if not proc or proc.poll() is not None:
                logger.info(f"[{account}] 服务未运行")
                return
            proc.terminate()
            try:
                proc.wait(timeout=8)
            except subprocess.TimeoutExpired:
                proc.kill()
            logger.info(f"[{account}] 服务已停止")
            self._cleanup_proc(account)
            if not any(p and p.poll() is None for p in self.proc_by_account.values()):
                self.status_var.set("状态：已停止")

    def stop_all(self):
        with self.proc_lock:
            for account, proc in list(self.proc_by_account.items()):
                if proc and proc.poll() is None:
                    proc.terminate()
                    try:
                        proc.wait(timeout=6)
                    except subprocess.TimeoutExpired:
                        proc.kill()
                    logger.info(f"[{account}] 服务已停止")
            self.proc_by_account = {}
            self.status_var.set("状态：已停止")

    def restart_service(self):
        self.stop_service()
        time.sleep(0.5)
        self.start_service()

    def _cleanup_proc(self, account):
        proc = self.proc_by_account.get(account)
        if proc and proc.poll() is not None:
            self.proc_by_account.pop(account, None)

    def show_about(self):
        messagebox.showinfo(
            "关于软件",
            "作者：Rick\n版本：2.0\n项目：Xianyu AutoAgent 2.0",
        )

    def check_update(self):
        url = "https://github.com/2836048681/xianyu-autoagent2/releases"
        try:
            webbrowser.open(url)
        except Exception:
            messagebox.showinfo("检查更新", f"请手动打开：{url}")

    def open_prompts_folder(self):
        ensure_prompts_dir()
        path = get_prompts_dir()
        try:
            os.startfile(path)
        except Exception as e:
            messagebox.showerror("打开失败", str(e))

    def on_close(self):
        running = any(p and p.poll() is None for p in self.proc_by_account.values())
        if running:
            if not messagebox.askyesno("退出", "服务仍在运行，是否退出并停止所有账号？"):
                return
            self.stop_all()
        self.root.destroy()

    def _ui(self, fn):
        self.root.after(0, fn)


def main():
    if "--run-service" in sys.argv:
        from utils.env_utils import set_active_app_dir
        set_active_app_dir(os.getenv("XYA_APP_DIR"))
        if os.path.exists(get_env_path()):
            load_dotenv(get_env_path())
        from main import run_service
        run_service()
        return

    ensure_app_dir()
    if os.path.exists(get_env_path()):
        load_dotenv(get_env_path())
    logger.remove()
    root = tk.Tk()
    try:
        root.tk.call("tk", "scaling", 1.0)
    except Exception:
        pass
    App(root)
    root.mainloop()


if __name__ == "__main__":
    main()
