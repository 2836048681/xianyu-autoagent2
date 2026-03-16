# Xianyu AutoAgent 2.0 - �������ܿͷ�������ϵ??
����Ŀ��??https://github.com/shaxiu/XianyuAutoAgent �İ�??
[![Python Version](https://img.shields.io/badge/python-3.8%2B-blue)](https://www.python.org/) [![LLM Powered](https://img.shields.io/badge/LLM-powered-FF6F61)](https://platform.openai.com/)

רΪ����ƽ̨����� AI ֵ�ط�����֧�ֶ�ר��Эͬ��������ۡ������ĸ�֪�Ի����Զ����ظ�??
## ��Ҫ��??
- ��ר��Эͬ����ͼ·��
- �����ĸ�֪�Ի���Ự����
- ����뼼��֧�ֳ�����??- ������־��������м�??- GUI ���ӻ����С�ɨ���¼��ʵʱ��־
- ���ߴ������??Chrome + chromedriver������װ��ɶ�������

## Ŀ¼�ṹ

```
.
���� gui_app.py                # ���ӻ���??���� main.py                   # �������
���� utils/
?? ���� env_utils.py            # �����������û�����Ŀ???? ���� playwright_login.py       # ɨ���¼��ȡ Cookie
���� prompts/                   # ��ʾ��ģ�壨�Զ����Ƶ��û�Ŀ¼��
���� chrome/                    # ��������������/��װʱ���ã�
���� chromedriver/              # ������������??��װʱ���ã�
���� build_exe.ps1              # ����??EXE ����
���� build_all.ps1              # EXE + ��װ��һ����??���� installer/inno/setup.iss   # Inno Setup �ű�
```

## ���������ã�����ʹ�� Release ���°�??
������ǰ�� Release ҳ���������°�װ����ʹ�ã�  
https://github.com/2836048681/xianyu-autoagent2/releases

��װ��ɺ�ͨ�������ݷ�ʽ�������??
����Դ�뷽ʽ���У���??������;�����ٲο��·�����������˵�����롰���ߴ���밲װ��??
## ���˺Ų�??
֧����??5 ���˺�ͬʱ���С�ÿ���˺Ŷ�??`.env` / `prompts` / `data`������Ӱ��?? 
�ڽ����������˺ź����ò�������ɲ�������??
### ���˺�ʹ��˵??
1. ��� �������˺š��������˺����ƣ���??`account1`��`account2`��??2. ѡ���˺ź���??`API_KEY` / `MODEL_BASE_URL` / `MODEL_NAME` / `COOKIES_STR`??3. ��� ��ɨ���¼������ Cookie������ɵ�¼�󱣴�����??4. ��� �������ǰ�˺š������ɲ�������??5. �˺�֮�以��Ӱ�죬���������ݷֱ����ڣ�
   `%APPDATA%\XianyuAutoAgent\accounts\<account>\`

### ���˺Ÿ�����Խ�??
������֤���˺���??����·���Ƿ��������������񣩣�

```powershell
python scripts\test_multi_account.py --accounts account1,account2
```

�������ʾÿ���˺ŵ� `.env`��`prompts`��`data` ·����ʾ������??
## ��������˵��

����??- `API_KEY`��ģ��ƽ??API Key
- `COOKIES_STR`����??Web Cookie����ͨ�� GUI ɨ���ȡ??
��ѡ��
- `MODEL_BASE_URL`��ģ�ͷ����ַ��Ĭ??`https://dashscope.aliyuncs.com/compatible-mode/v1`
- `MODEL_NAME`��ģ�����ƣ�Ĭ�� `qwen-max`
- `TOGGLE_KEYWORDS`���˹��ӹ��л��ؼ��֣�Ĭ??`��`
- `SIMULATE_HUMAN_TYPING`��ģ���˹������ӳ٣�Ĭ�� `False`
- `HEARTBEAT_INTERVAL` / `HEARTBEAT_TIMEOUT`
- `TOKEN_REFRESH_INTERVAL` / `TOKEN_RETRY_INTERVAL`
- `MANUAL_MODE_TIMEOUT`
- `MESSAGE_EXPIRE_TIME`

## ���ߴ���밲װ��Windows??
### 1. ׼������ Chrome

������������ŵ��ֿ��Ŀ¼��

```
chrome\chrome.exe
chromedriver\chromedriver.exe
```

### 2. ��������??EXE

```powershell
.\build_exe.ps1
```

����??```
dist\v1.exe
```

### 3. ���ɰ�װ����Inno Setup??
```powershell
.\build_all.ps1
```

����??```
installer\inno\Output\XianyuAutoAgent_0.1_Setup.exe
```

��װ���ԣ�
- ��װĿ¼Ĭ�� `D:\rickxy`
- ���� `chrome` / `chromedriver`
- ��ѡ���������ݷ�??- ��ѡ������??
## �û�������������??
���к��Զ������û�Ŀ¼��

```
%APPDATA%\XianyuAutoAgent
```

���а���??- `.env`�������ļ���
- `prompts/`���Զ�����ģ�壩
- `data/`���Ự���ݿ�??
��� EXE ��װ���ɵ������У���Ҫ��ͬ��Ŀ¼��??`.env` ??`prompts`??
## ��ʾ��ģ??
`prompts` Ŀ¼�ڰ�����

- `classify_prompt.txt`
- `price_prompt.txt`
- `tech_prompt.txt`
- `default_prompt.txt`

�״����л��Զ����Ƶ��û�Ŀ¼�������û�Ŀ¼���޸�ģ��??
## Release ˵����v0.1??
��Ҫ����??- ���� GUI ���ӻ�����̨��ɨ���¼�����ù����ʵʱ��־��
- ֧�����ߵ�¼??Cookie �Զ����£���??Chrome + chromedriver??- ����??`v1.exe` ??Inno Setup ��װ??- �û����ݶ����洢??`%APPDATA%\XianyuAutoAgent`
- һ��������??`build_exe.ps1` / `build_all.ps1`

## ����Ԥ��

![UI](./images/ui.png)

��֪����??- ��ǰ����??Windows 64 λ���߰�
- �״������Ҫ����ģ??API

## ����

��ӭ�ύ Issue ??PR??
## ��������

����Ŀ����ѧϰ����ʹ�ã����漰��Ȩ����ϵ����ɾ��??




