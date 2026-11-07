"""Serve this skill's folder for the specimen pages, with browser caching turned off.

The specimen pages load the brand CSS and the icon sprite from ../../assets/. Python's built-in server lets the
browser cache those files, so an edit to a stylesheet or a new icon in the sprite can stay invisible after a
reload. This server sends Cache-Control: no-store on every response, so a plain reload shows the files on disk.

    python .claude/skills/brand-htmlx-pages/scripts/serve_specimens.py [port]

Then open http://localhost:5180/references/specimens/foundations.html (5180 unless a port is given).
"""
import functools
import http.server
import pathlib
import sys

SKILL_ROOT = pathlib.Path(__file__).resolve().parent.parent


class NoStoreHandler(http.server.SimpleHTTPRequestHandler):
    """Serves files as the built-in server does, and tells the browser never to keep a copy."""

    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        super().end_headers()


def main():
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 5180
    handler = functools.partial(NoStoreHandler, directory=str(SKILL_ROOT))
    with http.server.ThreadingHTTPServer(("127.0.0.1", port), handler) as server:
        print(f"Serving {SKILL_ROOT}")
        print(f"Open http://localhost:{port}/references/specimens/foundations.html")
        server.serve_forever()


if __name__ == "__main__":
    main()
