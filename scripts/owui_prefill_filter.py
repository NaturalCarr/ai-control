"""
title: Prefill Injection
description: Injects an assistant prefill so the model continues from your chosen text. Set a persistent default via Valves, or use [>text<] anywhere in your message for a one-shot override.
author: local
version: 1.0.0
"""
import re
from pydantic import BaseModel
from typing import Optional


class Filter:
    class Valves(BaseModel):
        prefill: str = ""

    def __init__(self):
        self.valves = self.Valves()
        self._active: dict = {}  # user_id → prefill currently in flight

    def _debug(self, message: str) -> None:
        try:
            with open("/tmp/prefill_filter.log", "a", encoding="utf-8") as f:
                f.write(message + "\n")
        except Exception:
            pass

    def inlet(self, body: dict, __user__: Optional[dict] = None) -> dict:
        self._debug(f"inlet called keys={list(body.keys())} model={body.get('model')!r}")
        prefill = self.valves.prefill.strip()
        messages = body.get("messages", [])
        self._debug(f"inlet messages={len(messages)} last_role={(messages[-1].get('role') if messages else None)!r}")

        # One-shot inline override: put [>your prefill text<] anywhere in the message
        if messages and messages[-1].get("role") == "user":
            content = messages[-1].get("content", "")
            # Handle both plain string and OpenAI vision list format
            if isinstance(content, list):
                text_part = next((p for p in content if isinstance(p, dict) and p.get("type") == "text"), None)
                text = text_part.get("text", "") if text_part else ""
            else:
                text = content
            m = re.search(r"\[>(.+?)<\]", text, re.DOTALL)
            if m:
                prefill = m.group(1).strip()
                cleaned = text.replace(m.group(0), "").strip()
                self._debug(f"inlet found prefill={prefill!r} cleaned={cleaned!r}")
                if isinstance(content, list) and text_part:
                    text_part["text"] = cleaned
                else:
                    messages[-1]["content"] = cleaned

        if prefill:
            uid = (__user__ or {}).get("id", "default")
            self._active[uid] = prefill
            messages.append({"role": "assistant", "content": prefill})
            body["continue_final_message"] = True
            body["add_generation_prompt"] = False
            self._debug(f"inlet appended assistant prefill for uid={uid!r}")

        return body

    def outlet(self, body: dict, __user__: Optional[dict] = None) -> dict:
        self._debug(f"outlet called keys={list(body.keys())}")
        messages = body.get("messages", [])
        self._debug(f"outlet messages count={len(messages)} roles={[m.get('role') for m in messages[-3:]]}")

        # Stateful path: prefill stored by inlet
        uid = (__user__ or {}).get("id", "default")
        prefill = self._active.pop(uid, None)

        # Stateless fallback: two consecutive assistant messages means
        # messages[-2] is the prefill we injected, messages[-1] is the response.
        if not prefill and len(messages) >= 2:
            if (messages[-1].get("role") == "assistant" and
                    messages[-2].get("role") == "assistant"):
                prefill = messages[-2].get("content", "")
                self._debug(f"outlet detected prefill from consecutive msgs: {prefill!r}")

        if not prefill:
            self._debug("outlet no prefill found, skipping")
            return body

        # Prepend prefill to the final assistant message
        for i in range(len(messages) - 1, -1, -1):
            if messages[i].get("role") == "assistant":
                content = messages[i].get("content", "")
                if isinstance(content, str):
                    messages[i]["content"] = prefill + content
                    self._debug(f"outlet prepended prefill to msg[{i}]")
                break

        # Remove the standalone prefill message that we injected in inlet
        # so the history shows one clean assistant reply, not two.
        if (len(messages) >= 2 and
                messages[-2].get("role") == "assistant" and
                messages[-2].get("content", "") == prefill):
            messages.pop(-2)
            self._debug("outlet removed standalone prefill message from history")

        return body


_filter = Filter()


Valves = Filter.Valves
valves = _filter.valves


def inlet(body: dict, __user__: Optional[dict] = None) -> dict:
    _filter.valves = valves
    return _filter.inlet(body, __user__)


def outlet(body: dict, __user__: Optional[dict] = None) -> dict:
    _filter.valves = valves
    return _filter.outlet(body, __user__)
