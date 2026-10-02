# AI Control

AI Control is a small Windows desktop manager for a local AI stack. It gives you one place to start, stop, open, and inspect the services that make up a local text, vision, and video workflow.

## What it manages

- **llama-swap**: the local model router on port `8080`
- **Open WebUI**: the browser chat interface on port `3000`
- **ComfyUI**: image and video generation on port `8188`
- **MCP**: local workspace bridge and configurator
- **OpenCode**: launches its WSL terminal UI in a selected project folder
- **Local models**: start a model through llama-swap, watch active services, and stop them when finished

The manager is a Windows Forms application. llama-swap and Open WebUI run under WSL2; ComfyUI runs natively on Windows.

## Current model set

The example configuration includes:

- Qwen2.5 Coder 14B
- Qwen3.5 9B Vision
- Qwen3 14B Abliterated Q5_K_M
- Qwen3.8 27B UD-Q3_K_XL

Model weights are deliberately excluded from Git. Download them separately and update the paths in `config/llama-swap-config.example.yaml` for your machine.

## Requirements

- Windows 10 or 11
- WSL2 with a Linux distribution
- .NET Framework 4.x compiler (`csc.exe`), included with a normal Windows installation
- A working llama.cpp build with `llama-server`
- llama-swap
- Python 3.11 or newer for Open WebUI and the helper scripts
- ComfyUI, if image or video generation is needed

## Build and launch

1. Copy this repository to a local folder.
2. Open `src/ai-control.cs` and change the paths and WSL working directory to match your machine.
3. Run `build.bat`.
4. Start the resulting `ai-control.exe`, or use `launch.vbs` for a window without an extra console.

The window and notification-area icons use `src/excavator.png`. See [Icon status](docs/icon-status.md) for how the background color reflects service state.

OpenCode's **Launch** button lists the last 10 folders opened from this manager. Choose one to reopen it, or click **Browse...** to select a different folder. The folder browser includes **Make New Folder**. Recent paths are stored locally at `%LOCALAPPDATA%\LocalAI\opencode-recent.txt`; they are not part of this repository. OpenCode is an interactive terminal and is not stopped by **Stop All**.

The current reference setup expects the local stack at:

```text
C:\Users\Natural\llama.cpp
C:\Users\Natural\ComfyUI
```

Those are reference paths only. They should be changed before sharing a build with another person.

## First-time setup

1. Copy `config/llama-swap-config.example.yaml` to the location used by llama-swap.
2. Replace the example model paths with real GGUF paths.
3. Start llama-swap and confirm `http://localhost:8080/v1/models` responds.
4. Create the Open WebUI Python environment and install Open WebUI.
5. Register the local models with `scripts/register_owui_models.py` after Open WebUI is running.
6. Install the ComfyUI custom nodes listed by the video downloader if video generation is enabled.

The helper scripts use public Hugging Face repositories by default. Set `HF_TOKEN` only when a repository requires authentication.

## Helper scripts

- `scripts/download_llm_models.py` downloads the Qwen GGUF files into the llama.cpp models folder.
- `scripts/download_video_models.py` downloads the video dependencies used by the configured ComfyUI workflows.
- `scripts/register_owui_models.py` creates or updates the local model presets in Open WebUI.
- `scripts/register_prefill_filter.py` installs the Open WebUI prefill filter.
- `scripts/owui_prefill_filter.py` contains the filter implementation.

## Service controls

The manager uses exact process-name matching when stopping WSL services. This avoids matching the launcher command itself and is safer than broad command-line matching.

The **Open** buttons open the local dashboards:

- llama-swap playground: `http://localhost:8080/ui/#/playground`
- Open WebUI: `http://localhost:3000`
- ComfyUI: `http://localhost:8188`

## Privacy and release notes

This repository should contain source, setup scripts, and sanitized configuration templates only. Do not commit:

- GGUF or diffusion model files
- Open WebUI databases
- `.webui_secret_key`
- Hugging Face tokens
- personal logs or generated media

The included `.gitignore` excludes the common local-only files.
