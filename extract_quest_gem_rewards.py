#!/usr/bin/env python3
"""
Extract gem quest rewards and vendor gems per class (Acts 1-4) from
poewiki.net/wiki/Quest_Rewards and write to JSON files.
"""
import json
import re
from pathlib import Path

import requests
from bs4 import BeautifulSoup

WIKI_URL = "https://www.poewiki.net/wiki/Quest_Rewards"
# Output next to the script (GemGuide plugin root)
OUTPUT_DIR = Path(__file__).resolve().parent
QUEST_OUTPUT_JSON = OUTPUT_DIR / "quest_gem_rewards_by_class.json"
VENDOR_OUTPUT_JSON = OUTPUT_DIR / "vendor_gem_rewards_by_class.json"

# Order of class columns in the main quest reward table (from wiki layout)
CLASSES = ["Witch", "Shadow", "Ranger", "Duelist", "Marauder", "Templar", "Scion"]

# Acts we care about (1-4)
ACT_PATTERN = re.compile(r"Act\s*(\d+)", re.I)

# Reward link text patterns we exclude (not skill/support gems)
NON_GEM_PATTERNS = re.compile(
    r"^(Book of (Skill|Regrets)(\s*\(.*\))?|.*Flask|.*Ring|.*Belt|.*Key|.*Jewel|.*Amulet|"
    r"Sewer Keys|Infernal Talc|.*Shield|.*Bow|.*Club|.*Axe|.*Blade|.*Staff|"
    r"Quiver|Buckler|Foils?|Cutlass|Tomahawk|Gavel|Piledriver|Steelhead|"
    r"Walnut Spirit Shield|Redwood Spiked Shield|Oak Buckler|Maple Round Shield|"
    r"Painted Tower Shield|Etched Kite Shield|Military Staff|Coiled Wand|"
    r"Butcher Knife|Gouger|Sekhem|Barbed Club|Headsman Axe|Decurve Bow|"
    r"Blunt Arrow Quiver|Burnished Foil|Highland Blade)$",
    re.I,
)


def act_from_quest_name(text: str) -> int | None:
    """Parse act number from quest label, e.g. 'Enemy at the Gate Act 1' -> 1."""
    if not text:
        return None
    m = ACT_PATTERN.search(text)
    return int(m.group(1)) if m else None


def is_gem_name(name: str) -> bool:
    """True if this looks like a skill/support gem, not a flask/book/ring/etc."""
    if not name or len(name) < 2:
        return False
    if NON_GEM_PATTERNS.search(name):
        return False
    return True


def extract_gem_names(cell) -> list[str]:
    """From a table cell, extract gem names from wiki links (skill/support gems only)."""
    gems = []
    for a in cell.select("a"):
        href = a.get("href") or ""
        title = (a.get("title") or a.get_text(strip=True) or "").strip()
        if "/wiki/" in href and title and is_gem_name(title) and title not in gems:
            gems.append(title)
    return list(dict.fromkeys(gems))


def group_by_class(act_quest_class_gems: dict) -> dict:
    """Transform Act -> Quest -> { Class -> Gems } into Class -> Act -> Quest -> Gems."""
    out = {cls: {} for cls in CLASSES}
    for act_key, quests in act_quest_class_gems.items():
        for quest_name, class_gems in quests.items():
            for cls, gems in class_gems.items():
                if not gems:
                    continue
                if act_key not in out[cls]:
                    out[cls][act_key] = {}
                existing = out[cls][act_key].get(quest_name, [])
                out[cls][act_key][quest_name] = list(dict.fromkeys(existing + gems))
    return out


# Strip " Act 1", "Act 1Nessa", " Act 2 Yeena", etc. from first column to get quest name
QUEST_SUFFIX_PATTERN = re.compile(r"Act\s*\d+\s*\w*\s*$", re.I)


def _parse_rewards_table_rows(rows: list, max_act: int = 4) -> dict:
    """Parse table rows (skip header); return act -> quest_name -> { class: [gems] }."""
    result = {}
    for tr in rows[1:]:
        cells = tr.find_all(["td", "th"])
        if len(cells) < 2:
            continue
        quest_text = cells[0].get_text(strip=True)
        act = act_from_quest_name(quest_text)
        if act is None or act > max_act:
            continue
        quest_name = QUEST_SUFFIX_PATTERN.sub("", quest_text).strip()
        if not quest_name:
            continue

        class_gems = {c: [] for c in CLASSES}
        col_idx = 0
        for cell in cells[1:]:
            if col_idx >= len(CLASSES):
                break
            colspan = int(cell.get("colspan", 1))
            gems = extract_gem_names(cell)
            for _ in range(colspan):
                if col_idx < len(CLASSES) and gems:
                    class_gems[CLASSES[col_idx]] = gems
                col_idx += 1

        if not any(class_gems.values()):
            continue
        act_key = f"Act {act}"
        if act_key not in result:
            result[act_key] = {}
        existing = result[act_key].get(quest_name)
        if existing:
            for cls, gems in class_gems.items():
                if not gems:
                    continue
                merged = list(dict.fromkeys(existing.get(cls, []) + gems))
                existing[cls] = merged
        else:
            result[act_key][quest_name] = {k: v for k, v in class_gems.items() if v}
    return result


def parse_quest_rewards_table(soup) -> dict:
    """Parse main quest rewards table; return act -> quest_name -> { class: [gems] }."""
    tables = soup.select("table.wikitable")
    target_table = None
    for table in tables:
        header_row = table.find("tr")
        if not header_row:
            continue
        header_text = header_row.get_text().lower()
        if "witch" in header_text and "scion" in header_text:
            target_table = table
            break
    if not target_table:
        target_table = soup.select_one("table.wikitable")
    if not target_table:
        return {}
    rows = target_table.find_all("tr")
    return _parse_rewards_table_rows(rows, max_act=4) if len(rows) >= 2 else {}


def get_vendor_table(soup):
    """Return the wikitable in the 'Vendor quest rewards' section, or None."""
    for h2 in soup.find_all("h2"):
        span = h2.find("span", class_="mw-headline")
        if span and "Vendor" in (span.get_text() or ""):
            for sib in h2.find_next_siblings():
                if sib.name == "h2":
                    return None
                if sib.name == "table" and "wikitable" in (sib.get("class") or []):
                    return sib
            return None
    return None


def parse_vendor_rewards_table(soup) -> dict:
    """Parse vendor gem table (same page, Vendor section); return act -> quest_name -> { class: [gems] }."""
    table = get_vendor_table(soup)
    if not table:
        return {}
    rows = table.find_all("tr")
    return _parse_rewards_table_rows(rows, max_act=4) if len(rows) >= 2 else {}


def main() -> None:
    resp = requests.get(WIKI_URL, timeout=30)
    resp.raise_for_status()
    soup = BeautifulSoup(resp.text, "html.parser")

    quest_data = group_by_class(parse_quest_rewards_table(soup))
    vendor_data = group_by_class(parse_vendor_rewards_table(soup))

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    with open(QUEST_OUTPUT_JSON, "w", encoding="utf-8") as f:
        json.dump(quest_data, f, indent=2, ensure_ascii=False)
    print(f"Wrote {QUEST_OUTPUT_JSON}")

    with open(VENDOR_OUTPUT_JSON, "w", encoding="utf-8") as f:
        json.dump(vendor_data, f, indent=2, ensure_ascii=False)
    print(f"Wrote {VENDOR_OUTPUT_JSON}")


if __name__ == "__main__":
    main()
