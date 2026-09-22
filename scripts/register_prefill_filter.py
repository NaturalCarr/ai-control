"""Register the prefill injection filter in Open WebUI."""
import requests

OWUI = "http://localhost:3000"
FILTER_ID = "prefill_injection"
FILTER_FILE = r"C:\Users\Natural\llama.cpp\owui_prefill_filter.py"

def get_token():
    try:
        r = requests.post(f"{OWUI}/api/v1/auths/signin",
                          json={"email": "admin@localhost", "password": "admin"}, timeout=10)
        if r.ok:
            token = r.json().get("token", "")
            if token:
                print(f"  Authenticated, got token: {token[:20]}...")
                return token
        print(f"  Auth returned {r.status_code} — proceeding without token (WEBUI_AUTH=false mode)")
        return ""
    except Exception as e:
        print(f"  Auth error: {e} — proceeding without token")
        return ""

def existing_function_ids(headers):
    try:
        r = requests.get(f"{OWUI}/api/v1/functions/", headers=headers, timeout=10)
        print(f"  List functions: {r.status_code}")
        funcs = r.json() if r.ok else []
        ids = {f.get("id") for f in (funcs if isinstance(funcs, list) else [])}
        print(f"  Existing functions: {ids or '(none)'}")
        return ids
    except Exception as e:
        print(f"  Could not list functions: {e}")
        return set()

def main():
    with open(FILTER_FILE, encoding="utf-8") as f:
        code = f.read()

    print("Authenticating...")
    token = get_token()
    h = {"Authorization": f"Bearer {token}"} if token else {}

    payload = {
        "id":        FILTER_ID,
        "name":      "Prefill Injection",
        "type":      "filter",
        "content":   code,
        "is_active": True,
        "meta": {
            "description": "Injects assistant prefill. Set a default via Valves or use [>text<] inline.",
            "manifest": {}
        },
    }

    print("Checking existing functions...")
    existing = existing_function_ids(h)

    if FILTER_ID in existing:
        url = f"{OWUI}/api/v1/functions/id/{FILTER_ID}/update"
        action = "update"
    else:
        url = f"{OWUI}/api/v1/functions/create"
        action = "create"

    print(f"Sending {action} request...")
    r = requests.post(url, json=payload, headers=h, timeout=30)
    print(f"  Response: {r.status_code}")

    if r.ok:
        print(f"\nOK: Prefill Injection filter {action}d ({FILTER_ID})")
        resp = r.json()
        print(f"  is_active: {resp.get('is_active')}")
        print("\nNext steps:")
        print("  1. Open WebUI > Admin > Functions — confirm the filter shows as enabled")
        print("  2. Use [>text<] inline in any message for a one-shot prefill")
        print('     Example: "whats the capital of france? [>the capital of France is<]"')
        print("  3. Or set a default in the filter Valves (Admin > Functions > gear icon)")
    else:
        print(f"\nFAIL {r.status_code}: {r.text[:500]}")

if __name__ == "__main__":
    main()
