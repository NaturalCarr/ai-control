"""Download the larger GGUF language models used by llama-swap.

Run with the ComfyUI Python environment, or any Python environment that has
the huggingface_hub package installed.
"""

import os
from pathlib import Path

from huggingface_hub import hf_hub_download

MODEL_DIR = Path(r"C:\Users\Natural\llama.cpp\models")
HF_TOKEN = os.environ.get("HF_TOKEN")

DOWNLOADS = [
    {
        "label": "Qwen3.8 27B UD-Q3_K_XL (~16.4 GB)",
        "repo": "Distillio/Qwen3.8-27B-GGUF",
        "filename": "Qwen3.8-27B-Q3_K_XL.gguf",
    },
    {
        "label": "Qwen3 14B Q5_K_M (~10.5 GB)",
        "repo": "qtum/Qwen3-14B-GGUF",
        "filename": "Qwen3-14B-Q5_K_M.gguf",
    },
]


def main():
    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    print(f"Model directory: {MODEL_DIR}")
    print(f"HF token: {'set' if HF_TOKEN else 'not set (public models only)'}")

    for index, item in enumerate(DOWNLOADS, 1):
        output = MODEL_DIR / item["filename"]
        print(f"\n[{index}/{len(DOWNLOADS)}] {item['label']}")
        if output.exists():
            print(f"Already exists: {output}")
            continue
        path = hf_hub_download(
            repo_id=item["repo"],
            filename=item["filename"],
            local_dir=MODEL_DIR,
            token=HF_TOKEN,
        )
        print(f"Saved: {path}")


if __name__ == "__main__":
    main()
