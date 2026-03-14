import os
import sys
import threading
import subprocess
import queue
import time
import traceback
import tkinter as tk
from tkinter import ttk, messagebox

from loguru import logger
from dotenv import load_dotenv

from utils.env_utils import (
    ensure_app_dir,
    get_project_root,
    get_env_path,
    load_env_file,
    update_env_file,
)
from utils.selenium_login import fetch_cookies_via_selenium


class App:
    def __init__(self, root: tk.Tk):
        self.root = root
        self.root.title("Xianyu AutoAgent")
        self.root.geometry("760x520")
        self.root.minsize(720, 480)

        self.app_dir = ensure_app_dir()
        self.env_path = get_env_path(self.app_dir)

        self.log_queue = queue.Queue()
        self._setup_logger_bridge()

        self.proc = None
        self.proc_lock = threading.Lock()

        self._build_ui()
        self._load_env_into_fields()

        self.root.protocol("WM_DELETE_WINDOW", self.on_close)

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
        logger.add(TkLogSink(self.log_queue), level="DEBUG")
        self._drain_log_queue()

    def _drain_log_queue(self):
        while True:
            try:
                msg = self.log_queue.get_nowait()
            except queue.Empty:
                break
            self.log(msg)
        self.root.after(200, self._drain_log_queue)

    def _build_ui(self):
        self.root.columnconfigure(0, weight=1)
        self.root.rowconfigure(2, weight=1)

        header = ttk.Frame(self.root, padding=12)
        header.grid(row=0, column=0, sticky="ew")
        header.columnconfigure(1, weight=1)

        ttk.Label(header, text="Xianyu AutoAgent 控制台", font=("Microsoft YaHei UI", 14, "bold")).grid(
            row=0, column=0, sticky="w"
        )
        self.status_var = tk.StringVar(value="状态：未启动")
        ttk.Label(header, textvariable=self.status_var).grid(row=0, column=1, sticky="e")

        cfg = ttk.LabelFrame(self.root, text="运行配置", padding=12)
        cfg.grid(row=1, column=0, sticky="ew", padx=12)
        cfg.columnconfigure(1, weight=1)

        ttk.Label(cfg, text="API_KEY").grid(row=0, column=0, sticky="w")
        self.api_key_var = tk.StringVar()
        ttk.Entry(cfg, textvariable=self.api_key_var).grid(row=0, column=1, sticky="ew", padx=6)

        ttk.Label(cfg, text="MODEL_BASE_URL").grid(row=1, column=0, sticky="w")
        self.base_url_var = tk.StringVar()
        ttk.Entry(cfg, textvariable=self.base_url_var).grid(row=1, column=1, sticky="ew", padx=6)

        ttk.Label(cfg, text="MODEL_NAME").grid(row=2, column=0, sticky="w")
        self.model_name_var = tk.StringVar()
        ttk.Entry(cfg, textvariable=self.model_name_var).grid(row=2, column=1, sticky="ew", padx=6)

        ttk.Label(cfg, text="COOKIES_STR").grid(row=3, column=0, sticky="nw")
        self.cookies_text = tk.Text(cfg, height=4)
        self.cookies_text.grid(row=3, column=1, sticky="ew", padx=6)

        btns = ttk.Frame(cfg)
        btns.grid(row=4, column=1, sticky="e", pady=(8, 0))

        ttk.Button(btns, text="保存配置", command=self.save_env).grid(row=0, column=0, padx=4)
        ttk.Button(btns, text="扫码登录并更新Cookie", command=self.start_login).grid(row=0, column=1, padx=4)

        ops = ttk.LabelFrame(self.root, text="服务控制", padding=12)
        ops.grid(row=2, column=0, sticky="nsew", padx=12, pady=(8, 8))
        ops.columnconfigure(0, weight=1)
        ops.rowconfigure(1, weight=1)

        op_btns = ttk.Frame(ops)
        op_btns.grid(row=0, column=0, sticky="w")
        ttk.Button(op_btns, text="启动服务", command=self.start_service).grid(row=0, column=0, padx=(0, 6))
        ttk.Button(op_btns, text="停止服务", command=self.stop_service).grid(row=0, column=1, padx=(0, 6))
        ttk.Button(op_btns, text="重启服务", command=self.restart_service).grid(row=0, column=2)

        self.log_text = tk.Text(ops, height=10)
        self.log_text.grid(row=1, column=0, sticky="nsew", pady=(8, 0))

    def _load_env_into_fields(self):
        env = load_env_file(self.env_path)
        self.api_key_var.set(env.get("API_KEY", ""))
        self.base_url_var.set(env.get("MODEL_BASE_URL", "https://dashscope.aliyuncs.com/compatible-mode/v1"))
        self.model_name_var.set(env.get("MODEL_NAME", "qwen-max"))
        self.cookies_text.delete("1.0", tk.END)
        self.cookies_text.insert("1.0", env.get("COOKIES_STR", ""))

    def log(self, msg: str):
        ts = time.strftime("%H:%M:%S")
        self.log_text.insert(tk.END, f"[{ts}] {msg}\n")
        self.log_text.see(tk.END)

    def save_env(self):
        values = {
            "API_KEY": self.api_key_var.get().strip(),
            "MODEL_BASE_URL": self.base_url_var.get().strip(),
            "MODEL_NAME": self.model_name_var.get().strip(),
            "COOKIES_STR": self.cookies_text.get("1.0", tk.END).strip(),
        }
        update_env_file(values, self.env_path)
        self.log("配置已保存")
        messagebox.showinfo("保存成功", "配置已保存到 .env")

    def start_login(self):
        self.save_env()
        self.log("启动扫码登录流程，等待浏览器打开...")

        def run_login():
            try:
                cookie_str = fetch_cookies_via_selenium()
                if not cookie_str:
                    self._ui(lambda: messagebox.showwarning("登录失败", "未获取到Cookie"))
                    self._ui(lambda: self.log("扫码登录失败或未完成"))
                    return
                update_env_file({"COOKIES_STR": cookie_str}, self.env_path)
                self._ui(lambda: self.cookies_text.delete("1.0", tk.END))
                self._ui(lambda: self.cookies_text.insert("1.0", cookie_str))
                self._ui(lambda: self.log("Cookie已更新"))
                self._ui(lambda: messagebox.showinfo("成功", "Cookie已更新，服务将重启生效"))
                self._ui(self.restart_service)
            except Exception as e:
                err = "".join(traceback.format_exception_only(type(e), e)).strip()
                self._ui(lambda: self.log(f"扫码登录失败: {err}"))
                self._ui(lambda: messagebox.showerror("登录失败", err))

        threading.Thread(target=run_login, daemon=True).start()

    def _stream_process_output(self):
        if not self.proc or not self.proc.stdout:
            return
        for line in self.proc.stdout:
            line = line.rstrip("\r\n")
            if line:
                self._ui(lambda l=line: self.log(l))

    def start_service(self):
        with self.proc_lock:
            if self.proc and self.proc.poll() is None:
                self.log("服务已在运行")
                return

            self.save_env()
            env = os.environ.copy()
            # Force UTF-8 output and disable interactive prompts in child process.
            env["PYTHONIOENCODING"] = "utf-8"
            env["PYTHONUTF8"] = "1"
            env["PYTHONUNBUFFERED"] = "1"
            env["NON_INTERACTIVE"] = "1"
            if getattr(sys, "frozen", False):
                cmd = [sys.executable, "--run-service"]
                cwd = self.app_dir
            else:
                project_root = get_project_root()
                cmd = [sys.executable, "-u", os.path.join(project_root, "gui_app.py"), "--run-service"]
                cwd = project_root
            self.proc = subprocess.Popen(
                cmd,
                cwd=cwd,
                env=env,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                text=True,
                encoding="utf-8",
                errors="replace",
            )
            threading.Thread(target=self._stream_process_output, daemon=True).start()
            self.status_var.set("状态：运行中")
            self.log("服务已启动")

    def stop_service(self):
        with self.proc_lock:
            if not self.proc or self.proc.poll() is not None:
                self.log("服务未运行")
                return
            self.proc.terminate()
            try:
                self.proc.wait(timeout=8)
            except subprocess.TimeoutExpired:
                self.proc.kill()
            self.status_var.set("状态：已停止")
            self.log("服务已停止")

    def restart_service(self):
        self.stop_service()
        time.sleep(0.5)
        self.start_service()

    def on_close(self):
        if self.proc and self.proc.poll() is None:
            if not messagebox.askyesno("退出", "服务仍在运行，是否退出并停止服务？"):
                return
            self.stop_service()
        self.root.destroy()

    def _ui(self, fn):
        self.root.after(0, fn)


def main():
    ensure_app_dir()
    if os.path.exists(get_env_path()):
        load_dotenv(get_env_path())
    logger.remove()
    if "--run-service" in sys.argv:
        from main import run_service
        run_service()
        return
    root = tk.Tk()
    try:
        root.tk.call("tk", "scaling", 1.0)
    except Exception:
        pass
    App(root)
    root.mainloop()


if __name__ == "__main__":
    main()
