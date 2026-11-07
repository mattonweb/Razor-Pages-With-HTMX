"""Check the HTMLX Pages color tokens for WCAG contrast, in light and dark mode.

Reads ../assets/css/tokens.css (or the path given as the first argument), resolves each light-dark() token,
and checks every text and control pair the brand uses: 4.5:1 for text, 3:1 for icons and form-control borders.
Exits 1 if any pair falls short.

    python .claude/skills/brand-htmlx-pages/scripts/check_contrast.py
"""
import pathlib
import re
import sys

DEFAULT_TOKENS = pathlib.Path(__file__).resolve().parent.parent / "assets" / "css" / "tokens.css"

# (foreground token, background token, minimum ratio)
PAIRS = [
    ("text", "surface", 4.5), ("text", "bg", 4.5),
    ("text-muted", "surface", 4.5), ("text-muted", "bg", 4.5), ("text-muted", "surface-hover", 4.5),
    ("icon", "surface", 3.0),
    ("accent", "surface", 4.5), ("accent", "bg", 4.5), ("accent", "accent-subtle", 4.5),
    ("on-accent", "accent-fill", 4.5), ("on-accent", "accent-fill-hover", 4.5), ("on-accent", "accent-fill-active", 4.5),
    ("on-accent", "danger-fill", 4.5), ("on-accent", "danger-fill-hover", 4.5),
    ("brand-text", "brand-subtle", 4.5), ("brand-text", "surface", 4.5), ("on-brand", "brand", 4.5),
    ("border-input", "surface", 3.0),
    ("on-inverse", "inverse", 4.5), ("accent-on-inverse", "inverse", 4.5),
]
PAIRS += [(tone, background, 4.5)
          for tone in ("success", "danger", "warning", "info", "neutral")
          for background in ("surface", f"{tone}-subtle")]


def read_tokens(path):
    css = path.read_text(encoding="utf-8")
    tokens = {}
    for name, light, dark in re.findall(r"--([\w-]+):\s*light-dark\((#[0-9a-fA-F]{6}),\s*(#[0-9a-fA-F]{6})\)", css):
        tokens[name] = (light, dark)
    for name, value in re.findall(r"--([\w-]+):\s*(#[0-9a-fA-F]{6});", css):
        tokens[name] = (value, value)
    return tokens


def luminance(hex_color):
    channels = [int(hex_color[i:i + 2], 16) / 255 for i in (1, 3, 5)]
    linear = [c / 12.92 if c <= 0.03928 else ((c + 0.055) / 1.055) ** 2.4 for c in channels]
    return 0.2126 * linear[0] + 0.7152 * linear[1] + 0.0722 * linear[2]


def contrast(first, second):
    lighter, darker = sorted((luminance(first), luminance(second)), reverse=True)
    return (lighter + 0.05) / (darker + 0.05)


def main():
    path = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_TOKENS
    tokens = read_tokens(path)
    failures = 0
    for mode, index in (("light", 0), ("dark", 1)):
        for foreground, background, minimum in PAIRS:
            ratio = contrast(tokens[foreground][index], tokens[background][index])
            if ratio < minimum:
                failures += 1
                print(f"LOW {mode:<5} {ratio:5.2f} < {minimum}  --{foreground} on --{background}")
    print(f"{len(PAIRS) * 2} pairs checked, {failures} below the minimum")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
