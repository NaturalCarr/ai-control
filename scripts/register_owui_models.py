"""Register llama-swap models as editable Open WebUI Workspace models."""
import requests, sys

OWUI = "http://localhost:3000"

MODELS_TO_REGISTER = [
    {
        "id":          "local-qwen2.5-coder-14b",
        "name":        "Qwen2.5 Coder 14B",
        "base_model":  "Qwen2.5 Coder 14B",
        "description": "Qwen2.5 Coder 14B — coding-focused local model",
        "vision":      False,
    },
    {
        "id":          "local-qwen3.5-9b",
        "name":        "Qwen3.5 9B Vision",
        "base_model":  "Qwen3.5 9B Vision",
        "description": "Qwen3.5 9B — general multimodal vision/text model",
        "vision":      True,
    },
    {
        "id":          "local-qwen3-14b",
        "name":        "Qwen3 14B Abliterated",
        "base_model":  "Qwen3 14B Abliterated",
        "description": "Qwen3 14B Abliterated — balanced quality and speed",
        "vision":      False,
    },
    {
        "id":          "local-qwen3.8-27b",
        "name":        "Qwen3.8 27B",
        "base_model":  "Qwen3.8 27B",
        "description": "Qwen3.8 27B — highest-capacity local model",
        "vision":      False,
    },
]

TOOL_ID = "video_generation"


def get_token():
    try:
        r = requests.post(f"{OWUI}/api/v1/auths/signin",
                          json={"email": "admin@localhost", "password": "admin"}, timeout=10)
        return r.json().get("token", "") if r.ok else ""
    except Exception:
        return ""


def get_tools(token):
    r = requests.get(f"{OWUI}/api/v1/tools/",
                     headers={"Authorization": f"Bearer {token}"}, timeout=10)
    return r.json() if r.ok else []


def get_models(token):
    r = requests.get(f"{OWUI}/api/v1/models/list",
                     headers={"Authorization": f"Bearer {token}"}, timeout=10)
    if not r.ok:
        return []
    data = r.json()
    if isinstance(data, list):
        return data
    return data.get("items", [])


def upsert_model(token, cfg, tool_ids):
    payload = {
        "id":   cfg["id"],
        "base_model_id": cfg["base_model"],
        "name": cfg["name"],
        "meta": {
            "description": cfg["description"],
            "capabilities": {
                "vision": cfg.get("vision", False),
            },
            "tool_ids":    tool_ids,
        },
        "params": {},
    }
    headers = {"Authorization": f"Bearer {token}"}

    existing = get_models(token)
    existing_ids = {m.get("id") for m in (existing if isinstance(existing, list) else existing.get("data", []))}

    if cfg["id"] in existing_ids:
        r = requests.post(f"{OWUI}/api/v1/models/model/update",
                          json=payload, headers=headers, timeout=10)
        action = "updated"
    else:
        r = requests.post(f"{OWUI}/api/v1/models/create",
                          json=payload, headers=headers, timeout=10)
        action = "created"

    print(f"  {cfg['name']}: {action} -> {r.status_code}")
    if not r.ok:
        print(f"    {r.text[:200]}")


def main():
    print("Authenticating...")
    token = get_token()

    tools = get_tools(token)
    tool_ids = [t["id"] for t in (tools if isinstance(tools, list) else []) if t.get("id") == TOOL_ID]
    if not tool_ids:
        print(f"  Warning: tool '{TOOL_ID}' not found — models will be created without it")
    else:
        print(f"  Found tool: {tool_ids[0]}")

    for cfg in MODELS_TO_REGISTER:
        upsert_model(token, cfg, tool_ids)

    print("\nDone. Restart the swap-proxy for new models to appear in Open WebUI.")


if __name__ == "__main__":
    main()
