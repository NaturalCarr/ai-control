"""
Download video generation models for ComfyUI.

Run in PowerShell:
  C:/Users/Natural/ComfyUI/venv/Scripts/python.exe C:/Users/Natural/llama.cpp/download_video_models.py

Optional: set HF_TOKEN environment variable for gated models.
  $env:HF_TOKEN = "hf_..."

Models and their custom node requirements:
  Wan 2.2      -> Install: ComfyUI-WanVideoWrapper   (ComfyUI Manager)
  LTX-Video    -> Install: ComfyUI-LTXVideo           (ComfyUI Manager)
  HunyuanVideo -> Install: ComfyUI-HunyuanVideoWrapper (ComfyUI Manager)
"""

import os
import sys
from pathlib import Path

try:
    from huggingface_hub import hf_hub_download, snapshot_download
except ImportError:
    print("huggingface_hub not found. Run: pip install huggingface_hub")
    sys.exit(1)

COMFY_MODELS = Path(r"C:\Users\Natural\ComfyUI\models")
HF_TOKEN = os.environ.get("HF_TOKEN")


def dl_file(repo, filename, dest_dir, rename=None):
    dest_dir.mkdir(parents=True, exist_ok=True)
    out = dest_dir / (rename or Path(filename).name)
    if out.exists():
        print(f"  Already exists: {out.name}")
        return
    print(f"  Downloading {Path(filename).name} ...")
    path = hf_hub_download(
        repo_id=repo,
        filename=filename,
        local_dir=dest_dir,
        token=HF_TOKEN,
    )
    if rename and Path(path).name != rename:
        Path(path).rename(out)
    print(f"  Saved: {out}")


def dl_snapshot(repo, dest_dir, ignore=None):
    dest_dir.mkdir(parents=True, exist_ok=True)
    print(f"  Downloading snapshot of {repo} ...")
    snapshot_download(
        repo_id=repo,
        local_dir=dest_dir,
        token=HF_TOKEN,
        ignore_patterns=ignore or [],
    )
    print(f"  Saved to: {dest_dir}")


# ─────────────────────────────────────────────────────────────────────────────
# Model catalogue — comment out anything you don't want
# ─────────────────────────────────────────────────────────────────────────────

DOWNLOADS = [

    # ── Wan 2.2 TI2V 5B ─────────────────────────────────────────────────────
    # Text+image-to-video, mid-size. Tight on 16 GB but generally works.
    # VRAM: ~12–14 GB during generation at 480p
    {
        "label": "Wan 2.2 TI2V 5B (image-to-video)",
        "fn": lambda: dl_snapshot(
            repo="Wan-AI/Wan2.2-TI2V-5B",
            dest_dir=COMFY_MODELS / "wan_video" / "Wan2.2-TI2V-5B",
            ignore=["*.md", "*.txt", ".git*"],
        ),
    },

    # ── HunyuanVideo GGUF Q4_K_M ────────────────────────────────────────────
    # Full model is 40+ GB — use community GGUF quantization.
    # Q4_K_M is ~14 GB, just fits 16 GB VRAM (very tight; close other apps).
    # Node dir: ComfyUI/models/hunyuan_video/  (ComfyUI-HunyuanVideoWrapper)
    # VRAM: ~14–15 GB
    {
        "label": "HunyuanVideo Q4_K_M GGUF (~14 GB)",
        "fn": lambda: dl_file(
            repo="city96/HunyuanVideo-gguf",
            filename="hunyuan-video-t2v-720p-Q4_K_M.gguf",
            dest_dir=COMFY_MODELS / "hunyuan_video",
        ),
    },
    # Lighter option — Q3_K_M fits more easily but lower quality
    # {
    #     "label": "HunyuanVideo Q3_K_M GGUF (~11 GB)",
    #     "fn": lambda: dl_file(
    #         repo="city96/HunyuanVideo-gguf",
    #         filename="hunyuan-video-t2v-720p-Q3_K_M.gguf",
    #         dest_dir=COMFY_MODELS / "hunyuan_video",
    #     ),
    # },
    # HunyuanVideo text encoder (required)
    {
        "label": "HunyuanVideo text encoder (llava-llama-3-8b)",
        "fn": lambda: dl_snapshot(
            repo="Kijai/llava-llama-3-8b-text-encoder-tokenizer",
            dest_dir=COMFY_MODELS / "LLM" / "llava-llama-3-8b-text-encoder-tokenizer",
            ignore=["*.md", ".git*"],
        ),
    },
    # HunyuanVideo CLIP text encoder
    {
        "label": "HunyuanVideo CLIP (clip-vit-large-patch14)",
        "fn": lambda: dl_file(
            repo="openai/clip-vit-large-patch14",
            filename="model.safetensors",
            dest_dir=COMFY_MODELS / "clip" / "clip-vit-large-patch14",
        ),
    },

]


def main():
    print(f"\nComfyUI models directory: {COMFY_MODELS}")
    print(f"HF token: {'set' if HF_TOKEN else 'not set (public models only)'}\n")

    for i, entry in enumerate(DOWNLOADS):
        print(f"[{i+1}/{len(DOWNLOADS)}] {entry['label']}")
        try:
            entry["fn"]()
        except Exception as e:
            print(f"  ERROR: {e}")
        print()

    print("Done. Install the required custom nodes in ComfyUI Manager before using these models.")
    print("  - ComfyUI-WanVideoWrapper")
    print("  - ComfyUI-LTXVideo")
    print("  - ComfyUI-HunyuanVideoWrapper")


if __name__ == "__main__":
    main()
