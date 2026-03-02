# Last Day - Development Tools

Python utilities for asset generation, background removal, and other development tasks.

## Setup

```bash
cd Tools
python3 -m venv venv
source venv/bin/activate   # macOS/Linux
pip install -r requirements.txt
```

## API Keys

1. Copy `.env.example` to `.env`
2. Fill in the API keys you need
3. `.env` is gitignored and will never be committed

```bash
cp .env.example .env
# Edit .env with your actual keys
```

## Scripts

- `asset_generator.py` - Batch background removal for game sprites
- `music_generator.py` - Prompt templates for Suno.ai music generation

## Security Notes

- NEVER hardcode API keys in scripts
- NEVER commit `.env` to git
- All scripts load keys via `python-dotenv`
- If you accidentally commit a key, rotate it immediately
