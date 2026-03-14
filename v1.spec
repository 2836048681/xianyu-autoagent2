# -*- mode: python ; coding: utf-8 -*-


a = Analysis(
    ['gui_app.py'],
    pathex=[],
    binaries=[],
    datas=[('prompts', 'prompts'), ('chrome', 'chrome'), ('chromedriver', 'chromedriver')],
    hiddenimports=['tkinter', 'tkinter.ttk', 'selenium'],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    noarchive=False,
)
pyz = PYZ(a.pure)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.datas,
    [],
    name='v1',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=False,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
)
