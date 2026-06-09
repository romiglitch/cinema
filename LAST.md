# AI-Related Changes

## 1. API keys moved out of `Web.config` → `.env` file
- `Web.config` keys (`TMDbApiKey`, `SerpApiKey`, `AIKey`) cleared to empty
- `.env` file created at repo root with real values (gitignored)
- `.env.example` committed as a template
- `Global.asax` + `Global.asax.cs` created — reads `.env` at app startup and injects keys into `ConfigurationManager.AppSettings`

## 2. AI chat: multi-provider fallback loop
**`Master.Master.cs`** — `AskAiForRecommendation` completely rewritten:
- Old code: single Gemini call using Google's native format (`contents`/`parts`)
- New code: loops through 7 providers in order, tries each until one succeeds
- All providers use the unified **OpenAI-compatible format** (`/chat/completions` + `Bearer` token)
- Provider order: Gemini → Groq → xAI → Cerebras → Mistral → OpenRouter → SambaNova
- On failure (429, 503, any error) → logs to Debug and moves to next provider
- System message replaces the old Gemini-specific conversation setup
- Chat history uses `user`/`assistant` roles instead of `user`/`model`

## 3. AI chat: Enter key sends message
**`Master.js`** — added `keydown` event delegation on `document`:
- Pressing Enter in the chat input clicks the send button
- `Shift+Enter` is ignored (reserved for newline if needed)
- Uses event delegation on `document` (not on the input directly) so it survives UpdatePanel PostBacks

## 4. AI chat: input keeps focus after response
**`Master.js`** — in `prm.add_endRequest`:
- Added `input.focus()` after clearing the input value
- After the AI response arrives the cursor returns to the text box automatically

## 5. Dead code removed
**`Master.js`** — removed `handleChatClick()` function (never called)
**`Master.Master`** — removed `OnClientClick="showLoading();"` from the send button (`showLoading` was never defined)

## 6. `.env` sent to Windows machine
Added `/write-env` endpoint to `sql-bridge.ps1` and used it to POST the `.env` file to `E:\project\Shipping\.env` over Tailscale so the Windows deployment has all keys.
