# Xianyu AutoAgent 2.0 - 闂查奔鏅鸿兘瀹㈡湇鏈哄櫒浜虹郴缁?
鏈」鐩熀浜?https://github.com/shaxiu/XianyuAutoAgent 鏀圭増銆?
[![Python Version](https://img.shields.io/badge/python-3.8%2B-blue)](https://www.python.org/) [![LLM Powered](https://img.shields.io/badge/LLM-powered-FF6F61)](https://platform.openai.com/)

涓撲负闂查奔骞冲彴鎵撻€犵殑 AI 鍊煎畧鏂规锛屾敮鎸佸涓撳鍗忓悓銆佹櫤鑳借浠枫€佷笂涓嬫枃鎰熺煡瀵硅瘽涓庤嚜鍔ㄥ寲鍥炲銆?
## 涓昏鐗规€?
- 澶氫笓瀹跺崗鍚屼笌鎰忓浘璺敱
- 涓婁笅鏂囨劅鐭ュ璇濅笌浼氳瘽璁板繂
- 璁环涓庢妧鏈敮鎸佸満鏅鐞?- 鍩虹鏃ュ織杈撳嚭涓庤繍琛岀洃鎺?- GUI 鍙鍖栬繍琛屻€佹壂鐮佺櫥褰曚笌瀹炴椂鏃ュ織
- 绂荤嚎鎵撳寘锛堝唴缃?Chrome + chromedriver锛夛紝瀹夎鍚庡彲鐙珛杩愯

## 鐩綍缁撴瀯

```
.
鈹溾攢 gui_app.py                # 鍙鍖栧叆鍙?鈹溾攢 main.py                   # 鏈嶅姟鍏ュ彛
鈹溾攢 utils/
鈹? 鈹溾攢 env_utils.py            # 绂荤嚎閰嶇疆涓庣敤鎴锋暟鎹洰褰?鈹? 鈹斺攢 playwright_login.py       # 鎵爜鐧诲綍鑾峰彇 Cookie
鈹溾攢 prompts/                   # 鎻愮ず璇嶆ā鏉匡紙鑷姩澶嶅埗鍒扮敤鎴风洰褰曪級
鈹溾攢 chrome/                    # 绂荤嚎娴忚鍣紙鎵撳寘/瀹夎鏃跺唴缃級
鈹溾攢 chromedriver/              # 绂荤嚎椹卞姩锛堟墦鍖?瀹夎鏃跺唴缃級
鈹溾攢 build_exe.ps1              # 鍗曟枃浠?EXE 鏋勫缓
鈹溾攢 build_all.ps1              # EXE + 瀹夎鍖呬竴閿瀯寤?鈹斺攢 installer/inno/setup.iss   # Inno Setup 鑴氭湰
```

## 杩愯涓庨厤缃紙浼樺厛浣跨敤 Release 鏈€鏂扮増锛?
璇蜂紭鍏堝墠寰€ Release 椤甸潰涓嬭浇鏈€鏂板畨瑁呭寘骞朵娇鐢細  
https://github.com/2836048681/xianyu-autoagent2/releases

瀹夎瀹屾垚鍚庯紝閫氳繃妗岄潰蹇嵎鏂瑰紡鍚姩鍗冲彲銆?
濡傞渶婧愮爜鏂瑰紡杩愯锛堝紑鍙?璋冭瘯鐢ㄩ€旓級锛屽啀鍙傝€冧笅鏂光€滅幆澧冨彉閲忚鏄庘€濅笌鈥滅绾挎墦鍖呬笌瀹夎鈥濄€?
## 澶氳处鍙峰苟琛?
鏀寔鏈€澶?5 涓处鍙峰悓鏃惰繍琛屻€傛瘡涓处鍙风嫭绔?`.env` / `prompts` / `data`锛屼簰涓嶅奖鍝嶃€? 
鍦ㄧ晫闈腑鏂板璐﹀彿鍚庯紝閰嶇疆骞跺惎鍔ㄥ嵆鍙苟琛岃繍琛屻€?
### 澶氳处鍙蜂娇鐢ㄨ鏄?
1. 鐐瑰嚮 鈥滄柊澧炶处鍙封€濓紝杈撳叆璐﹀彿鍚嶇О锛堝缓璁?`account1`銆乣account2`锛夈€?2. 閫夋嫨璐﹀彿鍚庡～鍐?`API_KEY` / `MODEL_BASE_URL` / `MODEL_NAME` / `COOKIES_STR`銆?3. 鐐瑰嚮 鈥滄壂鐮佺櫥褰曞苟鏇存柊 Cookie鈥濓紝瀹屾垚鐧诲綍鍚庝繚瀛橀厤缃€?4. 鐐瑰嚮 鈥滃惎鍔ㄥ綋鍓嶈处鍙封€濓紝鍗冲彲骞惰杩愯銆?5. 璐﹀彿涔嬮棿浜掍笉褰卞搷锛岄厤缃笌鏁版嵁鍒嗗埆瀛樻斁鍦細
   `%APPDATA%\XianyuAutoAgent\accounts\<account>\`

### 澶氳处鍙烽殧绂绘祴璇曡剼鏈?
鐢ㄤ簬楠岃瘉鍚勮处鍙烽厤缃?鏁版嵁璺緞鏄惁鐙珛锛堜笉鍚姩鏈嶅姟锛夛細

```powershell
python scripts\test_multi_account.py --accounts account1,account2
```

杈撳嚭浼氭樉绀烘瘡涓处鍙风殑 `.env`銆乣prompts`銆乣data` 璺緞涓庣ず渚嬮厤缃€?
## 鐜鍙橀噺璇存槑

蹇呭～锛?- `API_KEY`锛氭ā鍨嬪钩鍙?API Key
- `COOKIES_STR`锛氶棽楸?Web Cookie锛堝彲閫氳繃 GUI 鎵爜鑾峰彇锛?
鍙€夛細
- `MODEL_BASE_URL`锛氭ā鍨嬫湇鍔″湴鍧€锛岄粯璁?`https://dashscope.aliyuncs.com/compatible-mode/v1`
- `MODEL_NAME`锛氭ā鍨嬪悕绉帮紝榛樿 `qwen-max`
- `TOGGLE_KEYWORDS`锛氫汉宸ユ帴绠″垏鎹㈠叧閿瓧锛岄粯璁?`銆俙
- `SIMULATE_HUMAN_TYPING`锛氭ā鎷熶汉宸ヨ緭鍏ュ欢杩燂紝榛樿 `False`
- `HEARTBEAT_INTERVAL` / `HEARTBEAT_TIMEOUT`
- `TOKEN_REFRESH_INTERVAL` / `TOKEN_RETRY_INTERVAL`
- `MANUAL_MODE_TIMEOUT`
- `MESSAGE_EXPIRE_TIME`

## 绂荤嚎鎵撳寘涓庡畨瑁咃紙Windows锛?
### 1. 鍑嗗绂荤嚎 Chrome

灏嗙绾挎祻瑙堝櫒鏀惧埌浠撳簱鏍圭洰褰曪細

```
chrome\chrome.exe
chromedriver\chromedriver.exe
```

### 2. 鏋勫缓鍗曟枃浠?EXE

```powershell
.\build_exe.ps1
```

浜х墿锛?```
dist\v1.exe
```

### 3. 鐢熸垚瀹夎鍖咃紙Inno Setup锛?
```powershell
.\build_all.ps1
```

浜х墿锛?```
installer\inno\Output\XianyuAutoAgent_0.1_Setup.exe
```

瀹夎鐗规€э細
- 瀹夎鐩綍榛樿 `D:\rickxy`
- 鍐呯疆 `chrome` / `chromedriver`
- 鍙€夊垱寤烘闈㈠揩鎹锋柟寮?- 鍙€夊紑鏈鸿嚜鍚?
## 鐢ㄦ埛鏁版嵁涓庣绾胯繍琛?
杩愯鍚庤嚜鍔ㄧ敓鎴愮敤鎴风洰褰曪細

```
%APPDATA%\XianyuAutoAgent
```

鍏朵腑鍖呭惈锛?- `.env`锛堥厤缃枃浠讹級
- `prompts/`锛堣嚜鍔ㄥ鍒舵ā鏉匡級
- `data/`锛堜細璇濇暟鎹簱锛?
鍥犳 EXE 鎴栧畨瑁呭寘鍙崟鐙繍琛岋紝涓嶈姹傚悓绾х洰褰曟斁缃?`.env` 鎴?`prompts`銆?
## 鎻愮ず璇嶆ā鏉?
`prompts` 鐩綍鍐呭寘鍚細

- `classify_prompt.txt`
- `price_prompt.txt`
- `tech_prompt.txt`
- `default_prompt.txt`

棣栨杩愯浼氳嚜鍔ㄥ鍒跺埌鐢ㄦ埛鐩綍锛屽彲鍦ㄧ敤鎴风洰褰曚腑淇敼妯℃澘銆?
## Release 璇存槑锛坴0.1锛?
涓昏鏇存柊锛?- 鏂板 GUI 鍙鍖栨帶鍒跺彴锛堟壂鐮佺櫥褰曘€侀厤缃鐞嗐€佸疄鏃舵棩蹇楋級
- 鏀寔绂荤嚎鐧诲綍涓?Cookie 鑷姩鏇存柊锛堝唴缃?Chrome + chromedriver锛?- 鍗曟枃浠?`v1.exe` 涓?Inno Setup 瀹夎鍖?- 鐢ㄦ埛鏁版嵁鐙珛瀛樺偍浜?`%APPDATA%\XianyuAutoAgent`
- 涓€閿瀯寤鸿剼鏈?`build_exe.ps1` / `build_all.ps1`

## 鐣岄潰棰勮

![UI](./images/ui.png)

宸茬煡闄愬埗锛?- 褰撳墠浠呮彁渚?Windows 64 浣嶇绾垮寘
- 棣栨鍚姩闇€瑕侀厤缃ā鍨?API

## 璐＄尞

娆㈣繋鎻愪氦 Issue 鎴?PR銆?
## 鍏嶈矗澹版槑

鏈」鐩粎渚涘涔犱氦娴佷娇鐢紝鑻ユ秹鍙婁镜鏉冭鑱旂郴浣滆€呭垹闄ゃ€?


